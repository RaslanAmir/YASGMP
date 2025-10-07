using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Common;
using YasGMP.Models.DTO;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>AuditService</b> – Centralized GMP/21 CFR Part 11 compliant service for managing audit logs across all YasGMP modules.
    /// Tracks every user/system action with full forensic details: user, IP, device, timestamp, action, and context.
    /// </summary>
    public class AuditService
    {
        private readonly DatabaseService _dbService;

        // ——— Cached environment info so we don’t recompute at every write ———
        private static string? _cachedLocalIpv4;
        private static string? _cachedLocalIpv6;
        private static string? _cachedMac;
        private static string? _cachedNicName;
        private static string? _cachedGateway;
        private static string? _cachedDns;
        private static string? _cachedDeviceInfo; // 255-safe summarized line
        private static string? _cachedPublicIp;
        private static DateTime _publicIpLastFetchUtc;
        private static readonly TimeSpan PublicIpTtl = TimeSpan.FromMinutes(10);
        private static int _publicIpFetchInFlight; // 0/1

        /// <summary>
        /// Initializes a new instance of <see cref="AuditService"/>.
        /// </summary>
        /// <param name="dbService">Database service for executing audit queries.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="dbService"/> is null.</exception>
        public AuditService(DatabaseService dbService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        }

        #region === 🔍 HELPER PROPERTIES: Current User & Device/Network Info ===

        /// <summary>
        /// Gets the ID of the currently logged-in user, or 0 if none.
        /// </summary>
        private static IAuthContext? TryResolveAuthContext() => ServiceLocator.GetService<IAuthContext>();

        private static IPlatformService? TryResolvePlatformService() => ServiceLocator.GetService<IPlatformService>();

        private int CurrentUserId => TryResolveAuthContext()?.CurrentUser?.Id ?? 0;

        /// <summary>
        /// Best-effort local IPv4. Never returns "unknown".
        /// </summary>
        private string CurrentIp
        {
            get
            {
                var auth = TryResolveAuthContext();
                if (!string.IsNullOrWhiteSpace(auth?.CurrentIpAddress))
                {
                    return auth!.CurrentIpAddress;
                }

                return _cachedLocalIpv4 ??= ResolveBestIpv4();
            }
        }

        /// <summary>
        /// Builds a compact, 255-safe forensic string (key=value pairs delimited by ';').
        /// Includes OS/platform, host/user, app version, session, device model,
        /// NIC name/MAC, IPv4/IPv6, gateway, DNS, and <b>public IP</b> (cached, background-fetched).
        /// </summary>
        private string DeviceForensics
        {
            get
            {
                var auth = TryResolveAuthContext();
                if (!string.IsNullOrWhiteSpace(auth?.CurrentDeviceInfo))
                {
                    return auth!.CurrentDeviceInfo;
                }

                return _cachedDeviceInfo ??= BuildDeviceInfoString();
            }
        }

        #endregion

        #region === 🗂️ SYSTEM EVENT LOG (canonical writers) ===

        /// <summary>
        /// Logs a system-wide event (e.g., login, logout, configuration changes).
        /// </summary>
        public Task LogSystemEventAsync(string? action, string? details)
            => LogSystemEventAsync(action, details, tableName: null, recordId: null);

        /// <summary>
        /// Logs a system event and optionally associates it with a specific table and record.
        /// Normalizes null/empty inputs to safe defaults.
        /// </summary>
        public async Task LogSystemEventAsync(string? action, string? details, string? tableName, int? recordId)
        {
            var normalizedAction  = string.IsNullOrWhiteSpace(action) ? "EVENT" : action!;
            var normalizedDetails = details ?? string.Empty;
            var normalizedTable   = string.IsNullOrWhiteSpace(tableName) ? "system" : tableName!;

            await _dbService.LogSystemEventAsync(
                userId: CurrentUserId,
                eventType: normalizedAction,
                tableName: normalizedTable,
                module: null,
                recordId: recordId,
                description: normalizedDetails,
                ip: CurrentIp,
                severity: "info",
                deviceInfo: DeviceForensics,
                sessionId: TryResolveAuthContext()?.CurrentSessionId
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// Logs a system event explicitly on behalf of a specific user (e.g., during login before <see cref="App.LoggedUser"/> is set).
        /// </summary>
        public async Task LogSystemEventForUserAsync(int? userId, string? action, string? details, string? tableName, int? recordId)
        {
            var normalizedAction  = string.IsNullOrWhiteSpace(action) ? "EVENT" : action!;
            var normalizedDetails = details ?? string.Empty;
            var normalizedTable   = string.IsNullOrWhiteSpace(tableName) ? "system" : tableName!;

            await _dbService.LogSystemEventAsync(
                userId: userId,
                eventType: normalizedAction,
                tableName: normalizedTable,
                module: null,
                recordId: recordId,
                description: normalizedDetails,
                ip: CurrentIp,
                severity: "info",
                deviceInfo: DeviceForensics,
                sessionId: TryResolveAuthContext()?.CurrentSessionId
            ).ConfigureAwait(false);
        }

        #endregion

        #region === 📝 GENERIC ENTITY AUDIT ===

        /// <summary>
        /// Logs an audit entry for any entity (table). Also mirrors to system log for dashboards.
        /// </summary>
        public virtual async Task LogEntityAuditAsync(string? tableName, int entityId, string? action, string? details)
        {
            var t = string.IsNullOrWhiteSpace(tableName) ? "entity" : tableName!;
            var a = string.IsNullOrWhiteSpace(action) ? "EVENT" : action!;
            var d = details ?? string.Empty;

            // Write to entity_audit_log if present
            if (await TableExistsAsync("entity_audit_log").ConfigureAwait(false))
            {
                const string sql = @"
INSERT INTO entity_audit_log
    (`timestamp`, user_id, source_ip, device_info, session_id, `entity`, entity_id, `action`, `details`)
VALUES
    (NOW(), @uid, @ip, @device, @sid, @entity, @eid, @action, @details);";

                await _dbService.ExecuteNonQueryAsync(sql, new[]
                {
                    new MySqlParameter("@uid", CurrentUserId == 0 ? (object)DBNull.Value : CurrentUserId),
                    new MySqlParameter("@ip", CurrentIp),
                    new MySqlParameter("@device", DeviceForensics),
                    new MySqlParameter("@sid", TryResolveAuthContext()?.CurrentSessionId ?? string.Empty),
                    new MySqlParameter("@entity", t),
                    new MySqlParameter("@eid", entityId),
                    new MySqlParameter("@action", a),
                    new MySqlParameter("@details", d)
                }).ConfigureAwait(false);
            }

            // Mirror to canonical system log
            await LogSystemEventAsync(a, d, t, entityId).ConfigureAwait(false);
        }

        #endregion

        #region === 🔧 CALIBRATION / EXPORT HELPERS ===
        /// <summary>
        /// Executes the log calibration audit async operation.
        /// </summary>

        public Task LogCalibrationAuditAsync(int calibrationId, string action, string details)
            => LogEntityAuditAsync("calibrations", calibrationId, action, details);
        /// <summary>
        /// Executes the log calibration audit async operation.
        /// </summary>

        public Task LogCalibrationAuditAsync(string action, string details)
            => LogSystemEventAsync($"CALIBRATION_{action}", details);
        /// <summary>
        /// Executes the log export async operation.
        /// </summary>

        public async Task LogExportAsync(string exportType, string filePath, string filterUsed = "")
        {
            if (await TableExistsAsync("export_audit_log").ConfigureAwait(false))
            {
                const string sql = @"
INSERT INTO export_audit_log
    (`timestamp`, user_id, source_ip, device_info, export_type, file_path, filter_criteria)
VALUES
    (NOW(), @uid, @ip, @device, @etype, @file, @filter);";

                await _dbService.ExecuteNonQueryAsync(sql, new[]
                {
                    new MySqlParameter("@uid", CurrentUserId == 0 ? (object)DBNull.Value : CurrentUserId),
                    new MySqlParameter("@ip", CurrentIp),
                    new MySqlParameter("@device", DeviceForensics),
                    new MySqlParameter("@etype", exportType ?? string.Empty),
                    new MySqlParameter("@file", filePath ?? string.Empty),
                    new MySqlParameter("@filter", filterUsed ?? string.Empty)
                }).ConfigureAwait(false);
            }

            await LogSystemEventAsync("EXPORT",
                $"type={exportType}; file={filePath}; filter={filterUsed}", "export", null).ConfigureAwait(false);
        }
        /// <summary>
        /// Executes the log calibration export async operation.
        /// </summary>

        public Task LogCalibrationExportAsync(string exportType, string filePath, string filterUsed = "")
            => LogExportAsync(exportType, filePath, filterUsed);

        #endregion

        #region === 📊 DASHBOARD QUERIES ===
        /// <summary>
        /// Executes the get filtered audits operation.
        /// </summary>

        public async Task<List<AuditEntryDto>> GetFilteredAudits(string user, string entity, string action, DateTime from, DateTime to)
        {
            if (await TableExistsAsync("entity_audit_log").ConfigureAwait(false))
                return await QueryEntityAuditAsync(user, entity, action, from, to).ConfigureAwait(false);

            return await QuerySystemAuditAsEntityDtosAsync(user, entity, action, from, to).ConfigureAwait(false);
        }

        private async Task<List<AuditEntryDto>> QueryEntityAuditAsync(string user, string entity, string action, DateTime from, DateTime to)
        {
            var select = @"
                SELECT
                    a.id,
                    a.entity AS Entity,
                    a.entity_id AS EntityId,
                    a.action AS Action,
                    a.`timestamp` AS Timestamp,
                    a.user_id AS UserId,
                    u.username AS Username,
                    a.source_ip AS IpAddress,
                    a.device_info AS DeviceInfo,";

            bool hasSession = await ColumnExistsAsync("entity_audit_log", "session_id").ConfigureAwait(false);
            bool hasStatus  = await ColumnExistsAsync("entity_audit_log", "status").ConfigureAwait(false);
            bool hasDigSig  = await ColumnExistsAsync("entity_audit_log", "digital_signature").ConfigureAwait(false);
            bool hasSigHash = await ColumnExistsAsync("entity_audit_log", "signature_hash").ConfigureAwait(false);

            select += hasSession ? " a.session_id AS SessionId," : " NULL AS SessionId,";
            select += " a.details AS Note,";
            select += hasStatus  ? " a.status AS Status," : " NULL AS Status,";
            select += hasDigSig  ? " a.digital_signature AS DigitalSignature," : " NULL AS DigitalSignature,";
            select += hasSigHash ? " a.signature_hash AS SignatureHash" : " NULL AS SignatureHash";

            string sql = $@"
                {select}
                FROM entity_audit_log a
                LEFT JOIN users u ON u.id = a.user_id
                WHERE a.`timestamp` BETWEEN @from AND @to";

            var paramList = new List<MySqlParameter> { new("@from", from), new("@to", to) };

            if (!string.IsNullOrWhiteSpace(user))
            {
                sql += " AND (u.username LIKE @user OR u.full_name LIKE @user OR a.user_id = @userId) ";
                paramList.Add(new MySqlParameter("@user", "%" + user + "%"));
                if (int.TryParse(user, out int uid))
                    paramList.Add(new MySqlParameter("@userId", uid));
                else
                    paramList.Add(new MySqlParameter("@userId", -1));
            }
            if (!string.IsNullOrWhiteSpace(entity))
            {
                sql += " AND a.entity LIKE @entity ";
                paramList.Add(new MySqlParameter("@entity", "%" + entity + "%"));
            }
            if (!string.IsNullOrWhiteSpace(action) && !string.Equals(action, "All", StringComparison.OrdinalIgnoreCase))
            {
                sql += " AND a.action = @action ";
                paramList.Add(new MySqlParameter("@action", action));
            }
            sql += " ORDER BY a.`timestamp` DESC LIMIT 500;";

            var results = new List<AuditEntryDto>();
            try
            {
                var reader = await _dbService.ExecuteReaderAsync(sql, paramList.ToArray()).ConfigureAwait(false);
                if (reader == null) return results;

                using (reader)
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        var dto = new AuditEntryDto
                        {
                            Id = reader["id"] is int i ? i : Convert.ToInt32(reader["id"]),
                            Entity = reader["Entity"]?.ToString(),
                            EntityId = reader["EntityId"]?.ToString(),
                            Action = reader["Action"]?.ToString(),
                            Timestamp = reader["Timestamp"] != DBNull.Value ? Convert.ToDateTime(reader["Timestamp"]) : DateTime.MinValue,
                            UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : (int?)null,
                            Username = reader["Username"]?.ToString(),
                            IpAddress = reader["IpAddress"]?.ToString(),
                            DeviceInfo = reader["DeviceInfo"]?.ToString(),
                            SessionId = reader["SessionId"]?.ToString(),
                            Note = reader["Note"]?.ToString(),
                            Status = reader["Status"]?.ToString(),
                            DigitalSignature = reader["DigitalSignature"]?.ToString(),
                            SignatureHash = reader["SignatureHash"]?.ToString(),
                        };
                        results.Add(dto);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[AuditService.GetFilteredAudits] ERROR: " + ex);
                results.Add(new AuditEntryDto
                {
                    Action = "ERROR",
                    Note = "Greška kod dohvaćanja audita: " + ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }

            return results;
        }

        private async Task<List<AuditEntryDto>> QuerySystemAuditAsEntityDtosAsync(string user, string entity, string action, DateTime from, DateTime to)
        {
            string sql = @"
                SELECT 
                    s.id,
                    s.table_name AS Entity,
                    s.record_id AS EntityId,
                    s.event_type AS Action,
                    s.event_time AS Timestamp,
                    s.user_id AS UserId,
                    u.username AS Username,
                    s.source_ip AS IpAddress,
                    s.device_info AS DeviceInfo,
                    s.session_id AS SessionId,
                    s.description AS Note
                FROM system_event_log s
                LEFT JOIN users u ON u.id = s.user_id
                WHERE s.event_time BETWEEN @from AND @to";

            var paramList = new List<MySqlParameter> { new("@from", from), new("@to", to) };

            if (!string.IsNullOrWhiteSpace(user))
            {
                sql += " AND (u.username LIKE @user OR u.full_name LIKE @user OR s.user_id = @userId) ";
                paramList.Add(new MySqlParameter("@user", "%" + user + "%"));
                if (int.TryParse(user, out int uid))
                    paramList.Add(new MySqlParameter("@userId", uid));
                else
                    paramList.Add(new MySqlParameter("@userId", -1));
            }
            if (!string.IsNullOrWhiteSpace(entity))
            {
                sql += " AND s.table_name LIKE @entity ";
                paramList.Add(new MySqlParameter("@entity", "%" + entity + "%"));
            }
            if (!string.IsNullOrWhiteSpace(action) && !string.Equals(action, "All", StringComparison.OrdinalIgnoreCase))
            {
                sql += " AND s.event_type = @action ";
                paramList.Add(new MySqlParameter("@action", action));
            }
            sql += " ORDER BY s.event_time DESC LIMIT 500;";

            var results = new List<AuditEntryDto>();
            try
            {
                var reader = await _dbService.ExecuteReaderAsync(sql, paramList.ToArray()).ConfigureAwait(false);
                if (reader == null) return results;

                using (reader)
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        var dto = new AuditEntryDto
                        {
                            Id = reader["id"] is int i ? i : Convert.ToInt32(reader["id"]),
                            Entity = reader["Entity"]?.ToString(),
                            EntityId = reader["EntityId"]?.ToString(),
                            Action = reader["Action"]?.ToString(),
                            Timestamp = reader["Timestamp"] != DBNull.Value ? Convert.ToDateTime(reader["Timestamp"]) : DateTime.MinValue,
                            UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : (int?)null,
                            Username = reader["Username"]?.ToString(),
                            IpAddress = reader["IpAddress"]?.ToString(),
                            DeviceInfo = reader["DeviceInfo"]?.ToString(),
                            SessionId = reader["SessionId"]?.ToString(),
                            Note = reader["Note"]?.ToString()
                        };
                        results.Add(dto);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[AuditService.GetFilteredAudits:FALLBACK] ERROR: " + ex);
                results.Add(new AuditEntryDto
                {
                    Action = "ERROR",
                    Note = "Greška kod dohvaćanja audita (fallback): " + ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }

            return results;
        }

        #endregion

        #region === 🌐 API AUDIT TRAIL ===

        /// <summary>
        /// Retrieves API audit entries joined with API key and user metadata.
        /// </summary>
        /// <param name="apiKey">Partial API key filter (matches id, description, key value, or owner).</param>
        /// <param name="user">Partial user filter (username/full name/id).</param>
        /// <param name="action">Exact action filter (verb/endpoint); pass "All" or empty to disable.</param>
        /// <param name="from">Inclusive lower bound (UTC/local accepted).</param>
        /// <param name="to">Inclusive upper bound (UTC/local accepted).</param>
        /// <param name="limit">Maximum number of rows to return (capped at 1000).</param>
        public async Task<List<ApiAuditEntryDto>> GetApiAuditEntriesAsync(
            string apiKey,
            string user,
            string action,
            DateTime from,
            DateTime to,
            int limit = 500)
        {
            var results = new List<ApiAuditEntryDto>();

            if (!await TableExistsAsync("api_audit_log").ConfigureAwait(false))
            {
                results.Add(new ApiAuditEntryDto
                {
                    Action = "MISSING_TABLE",
                    Details = "Tabela api_audit_log nije pronađena u bazi.",
                    Timestamp = DateTime.UtcNow
                });
                return results;
            }

            var normalizedLimit = Math.Clamp(limit, 1, 1000);

            bool hasTimestamp = await ColumnExistsAsync("api_audit_log", "timestamp").ConfigureAwait(false);
            bool hasCreatedAt = await ColumnExistsAsync("api_audit_log", "created_at").ConfigureAwait(false);
            bool hasUpdatedAt = await ColumnExistsAsync("api_audit_log", "updated_at").ConfigureAwait(false);
            bool hasRequestDetails = await ColumnExistsAsync("api_audit_log", "request_details").ConfigureAwait(false);
            bool hasDetails = await ColumnExistsAsync("api_audit_log", "details").ConfigureAwait(false);

            bool hasApiKeysTable = await TableExistsAsync("api_keys").ConfigureAwait(false);
            bool hasUsersTable = await TableExistsAsync("users").ConfigureAwait(false);
            bool hasOwnerColumn = hasApiKeysTable && await ColumnExistsAsync("api_keys", "owner_id").ConfigureAwait(false);
            bool hasKeyUpdated = hasApiKeysTable && await ColumnExistsAsync("api_keys", "updated_at").ConfigureAwait(false);
            bool hasKeyLastUsed = hasApiKeysTable && await ColumnExistsAsync("api_keys", "last_used_at").ConfigureAwait(false);

            var dateColumns = new List<string>();
            if (hasTimestamp) dateColumns.Add("a.`timestamp`");
            if (hasCreatedAt) dateColumns.Add("a.created_at");
            if (hasUpdatedAt) dateColumns.Add("a.updated_at");

            string rangeExpression = dateColumns.Count switch
            {
                0 => "a.id",
                1 => dateColumns[0],
                _ => $"COALESCE({string.Join(", ", dateColumns)})"
            };

            string orderExpression = hasTimestamp
                ? "a.`timestamp`"
                : hasCreatedAt
                    ? "a.created_at"
                    : hasUpdatedAt
                        ? "a.updated_at"
                        : "a.id";

            var columns = new List<string>
            {
                "a.id AS Id",
                "a.api_key_id AS ApiKeyId",
                "a.user_id AS UserId",
                "a.action AS Action",
                hasTimestamp ? "a.`timestamp` AS Timestamp" : "NULL AS Timestamp",
                hasCreatedAt ? "a.created_at AS CreatedAt" : "NULL AS CreatedAt",
                hasUpdatedAt ? "a.updated_at AS UpdatedAt" : "NULL AS UpdatedAt",
                "a.ip_address AS IpAddress",
                hasRequestDetails ? "a.request_details AS RequestDetails" : "NULL AS RequestDetails",
                hasDetails ? "a.details AS Details" : "NULL AS Details"
            };

            if (hasApiKeysTable)
            {
                columns.Add("k.`key` AS ApiKeyValue");
                columns.Add("k.description AS ApiKeyDescription");
                columns.Add("k.is_active AS ApiKeyIsActive");
                columns.Add("k.created_at AS ApiKeyCreatedAt");
                columns.Add(hasKeyUpdated ? "k.updated_at AS ApiKeyUpdatedAt" : "NULL AS ApiKeyUpdatedAt");
                columns.Add(hasKeyLastUsed ? "k.last_used_at AS ApiKeyLastUsedAt" : "NULL AS ApiKeyLastUsedAt");
                columns.Add(hasOwnerColumn ? "k.owner_id AS ApiKeyOwnerId" : "NULL AS ApiKeyOwnerId");
            }
            else
            {
                columns.Add("NULL AS ApiKeyValue");
                columns.Add("NULL AS ApiKeyDescription");
                columns.Add("NULL AS ApiKeyIsActive");
                columns.Add("NULL AS ApiKeyCreatedAt");
                columns.Add("NULL AS ApiKeyUpdatedAt");
                columns.Add("NULL AS ApiKeyLastUsedAt");
                columns.Add("NULL AS ApiKeyOwnerId");
            }

            if (hasUsersTable)
            {
                columns.Add("u.username AS Username");
                columns.Add("u.full_name AS UserFullName");
            }
            else
            {
                columns.Add("NULL AS Username");
                columns.Add("NULL AS UserFullName");
            }

            if (hasApiKeysTable && hasUsersTable && hasOwnerColumn)
            {
                columns.Add("owner.username AS ApiKeyOwnerUsername");
                columns.Add("owner.full_name AS ApiKeyOwnerFullName");
            }
            else
            {
                columns.Add("NULL AS ApiKeyOwnerUsername");
                columns.Add("NULL AS ApiKeyOwnerFullName");
            }

            var joins = new List<string>();
            if (hasApiKeysTable)
            {
                joins.Add("LEFT JOIN api_keys k ON k.id = a.api_key_id");
            }

            if (hasUsersTable)
            {
                joins.Add("LEFT JOIN users u ON u.id = a.user_id");
            }

            if (hasApiKeysTable && hasUsersTable && hasOwnerColumn)
            {
                joins.Add("LEFT JOIN users owner ON owner.id = k.owner_id");
            }

            var sql = $@"
SELECT
    {string.Join(",\n    ", columns)}
FROM api_audit_log a
    {string.Join("\n    ", joins)}
WHERE {rangeExpression} BETWEEN @from AND @to";

            var parameters = new List<MySqlParameter>
            {
                new("@from", from),
                new("@to", to)
            };

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                var trimmed = apiKey.Trim();
                var apiKeyConditions = new List<string>();

                if (hasApiKeysTable)
                {
                    apiKeyConditions.Add("k.description LIKE @apiKey");
                    apiKeyConditions.Add("k.`key` LIKE @apiKey");
                }

                if (hasApiKeysTable && hasUsersTable && hasOwnerColumn)
                {
                    apiKeyConditions.Add("owner.username LIKE @apiKey");
                    apiKeyConditions.Add("owner.full_name LIKE @apiKey");
                }

                apiKeyConditions.Add("a.api_key_id = @apiKeyId");

                sql += $" AND ({string.Join(" OR ", apiKeyConditions)})";

                parameters.Add(new MySqlParameter("@apiKey", "%" + trimmed + "%"));
                if (int.TryParse(trimmed, out var apiKeyId))
                {
                    parameters.Add(new MySqlParameter("@apiKeyId", apiKeyId));
                }
                else
                {
                    parameters.Add(new MySqlParameter("@apiKeyId", -1));
                }
            }

            if (!string.IsNullOrWhiteSpace(user))
            {
                if (hasUsersTable)
                {
                    sql += " AND (u.username LIKE @user OR u.full_name LIKE @user OR a.user_id = @userId)";
                }
                else
                {
                    sql += " AND a.user_id = @userId";
                }

                parameters.Add(new MySqlParameter("@user", "%" + user.Trim() + "%"));
                if (int.TryParse(user, out var userId))
                {
                    parameters.Add(new MySqlParameter("@userId", userId));
                }
                else
                {
                    parameters.Add(new MySqlParameter("@userId", -1));
                }
            }

            if (!string.IsNullOrWhiteSpace(action) && !string.Equals(action, "All", StringComparison.OrdinalIgnoreCase))
            {
                sql += " AND a.action = @action";
                parameters.Add(new MySqlParameter("@action", action));
            }

            sql += $" ORDER BY {orderExpression} DESC LIMIT {normalizedLimit};";

            try
            {
                var reader = await _dbService.ExecuteReaderAsync(sql, parameters).ConfigureAwait(false);
                if (reader == null)
                {
                    return results;
                }

                using (reader)
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        var entry = new ApiAuditEntryDto
                        {
                            Id = reader.IsDBNull(reader.GetOrdinal("Id")) ? 0 : Convert.ToInt32(reader["Id"]),
                            ApiKeyId = SafeGetNullableInt(reader, "ApiKeyId"),
                            ApiKeyValue = SafeGetString(reader, "ApiKeyValue"),
                            ApiKeyDescription = SafeGetString(reader, "ApiKeyDescription"),
                            ApiKeyCreatedAt = SafeGetDateTime(reader, "ApiKeyCreatedAt"),
                            ApiKeyUpdatedAt = SafeGetDateTime(reader, "ApiKeyUpdatedAt"),
                            ApiKeyLastUsedAt = SafeGetDateTime(reader, "ApiKeyLastUsedAt"),
                            ApiKeyOwnerId = SafeGetNullableInt(reader, "ApiKeyOwnerId"),
                            ApiKeyOwnerUsername = SafeGetString(reader, "ApiKeyOwnerUsername"),
                            ApiKeyOwnerFullName = SafeGetString(reader, "ApiKeyOwnerFullName"),
                            UserId = SafeGetNullableInt(reader, "UserId"),
                            Username = SafeGetString(reader, "Username"),
                            UserFullName = SafeGetString(reader, "UserFullName"),
                            Action = SafeGetString(reader, "Action"),
                            Timestamp = SafeGetDateTime(reader, "Timestamp"),
                            CreatedAt = SafeGetDateTime(reader, "CreatedAt"),
                            UpdatedAt = SafeGetDateTime(reader, "UpdatedAt"),
                            IpAddress = SafeGetString(reader, "IpAddress"),
                            RequestDetails = SafeGetString(reader, "RequestDetails"),
                            Details = SafeGetString(reader, "Details")
                        };

                        if (!reader.IsDBNull(reader.GetOrdinal("ApiKeyIsActive")))
                        {
                            entry.ApiKeyIsActive = Convert.ToBoolean(reader["ApiKeyIsActive"]);
                        }

                        results.Add(entry);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[AuditService.GetApiAuditEntriesAsync] ERROR: " + ex);
                results.Add(new ApiAuditEntryDto
                {
                    Action = "ERROR",
                    Details = "Greška kod dohvaćanja API audita: " + ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }

            return results;

            static string? SafeGetString(MySqlDataReader reader, string column)
            {
                var ordinal = reader.GetOrdinal(column);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }

            static DateTime? SafeGetDateTime(MySqlDataReader reader, string column)
            {
                var ordinal = reader.GetOrdinal(column);
                return reader.IsDBNull(ordinal) ? (DateTime?)null : reader.GetDateTime(ordinal);
            }

            static int? SafeGetNullableInt(MySqlDataReader reader, string column)
            {
                var ordinal = reader.GetOrdinal(column);
                return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
            }
        }

        #endregion

        #region === Information_schema helpers ===

        private async Task<bool> TableExistsAsync(string table)
        {
            const string sql = @"SELECT COUNT(*) FROM information_schema.TABLES
                                 WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME=@t;";
            var obj = await _dbService.ExecuteScalarAsync(sql, new[] { new MySqlParameter("@t", table) }).ConfigureAwait(false);
            return Convert.ToInt32(obj) > 0;
        }

        private async Task<bool> ColumnExistsAsync(string table, string column)
        {
            const string sql = @"SELECT COUNT(*) FROM information_schema.COLUMNS
                                 WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME=@t AND COLUMN_NAME=@c;";
            var obj = await _dbService.ExecuteScalarAsync(sql, new[]
            {
                new MySqlParameter("@t", table),
                new MySqlParameter("@c", column)
            }).ConfigureAwait(false);

            return Convert.ToInt32(obj) > 0;
        }

        #endregion

        #region === Network & device forensics (core) ===

        /// <summary>
        /// Robust local IPv4 resolution:
        /// 1) IPlatformService if available and non-empty; 2) NIC enumeration (Up, non-virtual);
        /// 3) DNS host entry; 4) UDP local endpoint trick; fallback 127.0.0.1.
        /// </summary>
        private static string ResolveBestIpv4()
        {
            try
            {
                var plat = TryResolvePlatformService();
                var svcIp = plat?.GetLocalIpAddress();
                if (!string.IsNullOrWhiteSpace(svcIp) && !svcIp.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                    return svcIp.Trim();
            }
            catch { /* ignore */ }

            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;
                    if (IsVirtual(ni)) continue;

                    var ipProps = ni.GetIPProperties();
                    foreach (var ua in ipProps.UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address))
                        {
                            // cache Nic name/MAC/gateway/dns while we’re here
                            _cachedNicName ??= ni.Name;
                            _cachedMac ??= GetMac(ni);
                            _cachedGateway ??= ipProps.GatewayAddresses?.FirstOrDefault(g => g?.Address?.AddressFamily == AddressFamily.InterNetwork)?.Address?.ToString();
                            _cachedDns ??= string.Join(',', ipProps.DnsAddresses?.Where(d => d.AddressFamily == AddressFamily.InterNetwork).Select(d => d.ToString()) ?? Array.Empty<string>());
                            // IPv6 (primary) if present
                            _cachedLocalIpv6 ??= ipProps.UnicastAddresses?.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6)?.Address?.ToString();

                            return ua.Address.ToString();
                        }
                    }
                }
            }
            catch { /* ignore */ }

            try
            {
                var host = Dns.GetHostName();
                var entry = Dns.GetHostEntry(host);
                foreach (var addr in entry.AddressList)
                    if (addr.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addr))
                        return addr.ToString();
            }
            catch { /* ignore */ }

            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530); // no traffic is actually sent
                if (socket.LocalEndPoint is IPEndPoint ep)
                    return ep.Address.ToString();
            }
            catch { /* ignore */ }

            return "127.0.0.1";
        }

        /// <summary>
        /// Composes a 255-safe device line. Also kicks off a background fetch for public IP (cached for 10 minutes).
        /// </summary>
        private static string BuildDeviceInfoString()
        {
            // Ensure IPv4 is resolved (and IPv6/NIC metadata probably filled too)
            var ipv4 = _cachedLocalIpv4 ?? ResolveBestIpv4();
            var ipv6 = _cachedLocalIpv6;

            // Try to obtain a cached public IP or schedule one
            var publicIp = GetOrStartPublicIp();

            var auth = TryResolveAuthContext();
            var platformSvc = TryResolvePlatformService();

            // Platform metadata (non-throwing)
            string platform = platformSvc?.GetOsVersion() ?? RuntimeInformation.OSDescription;
            string manufacturer = platformSvc?.GetManufacturer() ?? string.Empty;
            string model = platformSvc?.GetModel() ?? string.Empty;

            // App version + build
            string appVer = Safe(() => typeof(AuditService).Assembly.GetName().Version?.ToString());

            // OS / runtime / host / user / domain
            string os = Environment.OSVersion.ToString();
            string fw = RuntimeInformation.FrameworkDescription;
            string arch = $"{RuntimeInformation.OSArchitecture}/{RuntimeInformation.ProcessArchitecture}";
            string host = platformSvc?.GetHostName() ?? Safe(() => Environment.MachineName);
            string user = platformSvc?.GetUserName() ?? Safe(() => Environment.UserName);
            string domain = Safe(() => Environment.UserDomainName);
            string sessionId = auth?.CurrentSessionId ?? string.Empty;

            // Network extras
            string nic = _cachedNicName ?? string.Empty;
            string mac = _cachedMac ?? string.Empty;
            string gw  = _cachedGateway ?? string.Empty;
            string dns = _cachedDns ?? string.Empty;

            // Compose key=value chunks; keep it under 255
            var parts = new[]
            {
                $"OS={Short(os, 40)}",
                $"FW={Short(fw, 20)}",
                $"Arch={arch}",
                $"Platform={platform}",
                string.IsNullOrEmpty(manufacturer) ? null : $"Mfr={Short(manufacturer, 16)}",
                string.IsNullOrEmpty(model) ? null : $"Model={Short(model, 18)}",
                $"Host={Short(host, 20)}",
                $"User={Short(user, 20)}",
                string.IsNullOrEmpty(domain) ? null : $"Domain={Short(domain, 20)}",
                string.IsNullOrEmpty(appVer) ? null : $"App={Short(appVer, 20)}",
                string.IsNullOrEmpty(sessionId) ? null : $"Sess={Short(sessionId, 24)}",
                $"IPv4={ipv4}",
                string.IsNullOrEmpty(ipv6) ? null : $"IPv6={Short(ipv6, 32)}",
                string.IsNullOrEmpty(publicIp) ? null : $"Pub={publicIp}",
                string.IsNullOrEmpty(nic) ? null : $"NIC={Short(nic, 20)}",
                string.IsNullOrEmpty(mac) ? null : $"MAC={mac}",
                string.IsNullOrEmpty(gw)  ? null : $"GW={gw}",
                string.IsNullOrEmpty(dns) ? null : $"DNS={Short(dns, 40)}"
            }.Where(s => !string.IsNullOrWhiteSpace(s));

            // Join and hard-cap to 255 (DB column)
            var line = string.Join("; ", parts);
            return line.Length <= 255 ? line : line.Substring(0, 255);
        }
        /// <summary>
        /// Returns cached public IP if fresh; otherwise schedules a background fetch and returns empty for now.
        /// </summary>
        private static string GetOrStartPublicIp()
        {
            try
            {
                var now = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(_cachedPublicIp) && now - _publicIpLastFetchUtc <= PublicIpTtl)
                    return _cachedPublicIp!;

                // Throttle to one background fetch at a time
                if (Interlocked.CompareExchange(ref _publicIpFetchInFlight, 1, 0) == 0)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                            var ip = (await http.GetStringAsync("https://api.ipify.org", cts.Token).ConfigureAwait(false)).Trim();
                            if (IPAddress.TryParse(ip, out _))
                            {
                                _cachedPublicIp = ip;
                                _publicIpLastFetchUtc = DateTime.UtcNow;
                            }
                        }
                        catch
                        {
                            // ignore; we’ll try again later
                        }
                        finally
                        {
                            Interlocked.Exchange(ref _publicIpFetchInFlight, 0);
                        }
                    });
                }
            }
            catch
            {
                // ignore
            }
            return string.Empty;
        }

        private static bool IsVirtual(NetworkInterface ni)
        {
            var name = (ni.Description + " " + ni.Name).ToLowerInvariant();
            return name.Contains("virtual") || name.Contains("vmware") || name.Contains("hyper-v") || name.Contains("loopback");
        }

        private static string GetMac(NetworkInterface ni)
        {
            try
            {
                var bytes = ni.GetPhysicalAddress()?.GetAddressBytes();
                if (bytes is { Length: > 0 })
                    return string.Join("-", bytes.Select(b => b.ToString("X2")));
            }
            catch { /* ignore */ }
            return string.Empty;
        }

        private static string Short(string value, int max) => string.IsNullOrEmpty(value) ? value : (value.Length <= max ? value : value.Substring(0, max));
        private static string Safe(Func<string> f) { try { return f() ?? string.Empty; } catch { return string.Empty; } }

        #endregion
    }
}
