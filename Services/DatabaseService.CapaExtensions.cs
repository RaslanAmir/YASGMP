// ==============================================================================
// File: Services/DatabaseService.CapaExtensions.cs
// Purpose: CAPA audit helpers as extension methods (no partial/virtual).
// NOTE: Formerly stubbed implementations are now wired to capa_action_log.
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.Models.Enums;

namespace YasGMP.Services
{
    /// <summary>
    /// DatabaseService extensions providing general CAPA queries and updates.
    /// </summary>
    public static class DatabaseServiceCapaExtensions
    {
        private const string BaseAuditSelect = @"
            SELECT
                id,
                capa_case_id,
                action_type,
                description,
                performed_by,
                performed_by_id,
                performed_at,
                status,
                note,
                digital_signature,
                source_ip,
                change_version,
                is_deleted,
                capa_case,
                created_at,
                updated_at
            FROM capa_action_log";

        private static readonly IReadOnlyDictionary<string, CapaActionType> SpecialActionTypeMappings =
            new Dictionary<string, CapaActionType>(StringComparer.OrdinalIgnoreCase)
            {
                ["korektivna"] = CapaActionType.CorrectiveAction,
                ["preventivna"] = CapaActionType.PreventiveAction,
            };

        // ---- CAPA AUDIT QUERIES ------------------------------------------------

        public static async Task<CapaAudit?> GetCapaAuditByIdAsync(
            this DatabaseService db,
            int id,
            CancellationToken cancellationToken = default)
        {
            const string sql = BaseAuditSelect + @"
            WHERE id = @id
            LIMIT 1;";

            var parameters = new[]
            {
                new MySqlParameter("@id", MySqlDbType.Int32) { Value = id },
            };

            var table = await db.ExecuteSelectAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
            return table.Rows.Count > 0 ? MapCapaAudit(table.Rows[0]) : null;
        }

        public static async Task<List<CapaAudit>> GetCapaAuditsByCapaIdAsync(
            this DatabaseService db,
            int capaId,
            CancellationToken cancellationToken = default)
        {
            const string sql = BaseAuditSelect + @"
            WHERE capa_case_id = @capaId
            ORDER BY COALESCE(performed_at, updated_at, created_at) DESC, id DESC;";

            var parameters = new[]
            {
                new MySqlParameter("@capaId", MySqlDbType.Int32) { Value = capaId },
            };

            var table = await db.ExecuteSelectAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
            return MapAudits(table);
        }

        public static async Task<List<CapaAudit>> GetCapaAuditsByUserIdAsync(
            this DatabaseService db,
            int userId,
            CancellationToken cancellationToken = default)
        {
            const string sql = BaseAuditSelect + @"
            WHERE (performed_by = @userId OR performed_by_id = @userId)
            ORDER BY COALESCE(performed_at, updated_at, created_at) DESC, id DESC;";

            var parameters = new[]
            {
                new MySqlParameter("@userId", MySqlDbType.Int32) { Value = userId },
            };

            var table = await db.ExecuteSelectAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
            return MapAudits(table);
        }

        public static async Task<List<CapaAudit>> GetCapaAuditsByActionAsync(
            this DatabaseService db,
            CapaActionType action,
            CancellationToken cancellationToken = default)
        {
            const string sql = BaseAuditSelect + @"
            WHERE LOWER(REPLACE(REPLACE(REPLACE(TRIM(action_type), '_', ''), '-', ''), ' ', '')) = @action
            ORDER BY COALESCE(performed_at, updated_at, created_at) DESC, id DESC;";

            var parameters = new[]
            {
                new MySqlParameter("@action", MySqlDbType.VarChar) { Value = NormalizeActionForQuery(action) },
            };

            var table = await db.ExecuteSelectAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
            return MapAudits(table);
        }

        public static async Task<List<CapaAudit>> GetCapaAuditsByDateRangeAsync(
            this DatabaseService db,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            const string sql = BaseAuditSelect + @"
            WHERE COALESCE(performed_at, updated_at, created_at) BETWEEN @from AND @to
            ORDER BY COALESCE(performed_at, updated_at, created_at) DESC, id DESC;";

            var parameters = new[]
            {
                new MySqlParameter("@from", MySqlDbType.DateTime) { Value = from },
                new MySqlParameter("@to", MySqlDbType.DateTime) { Value = to },
            };

            var table = await db.ExecuteSelectAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
            return MapAudits(table);
        }

        private static List<CapaAudit> MapAudits(DataTable table)
        {
            var results = new List<CapaAudit>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                results.Add(MapCapaAudit(row));
            }

            return results;
        }

        private static CapaAudit MapCapaAudit(DataRow row)
        {
            var status = GetString(row, "status");

            var audit = new CapaAudit
            {
                Id = GetInt32(row, "id"),
                CapaId = GetInt32(row, "capa_case_id"),
                UserId = DetermineUserId(row),
                Action = ParseActionType(GetString(row, "action_type"), status),
                ChangedAt = GetDateTime(row, "performed_at", "updated_at", "created_at"),
                Details = GetString(row, "description"),
                OldValue = string.Empty,
                NewValue = status,
                DigitalSignature = GetString(row, "digital_signature"),
                IntegrityHash = string.Empty,
                SourceIp = GetString(row, "source_ip"),
                DeviceInfo = string.Empty,
                Note = GetString(row, "note"),
                ChangeVersion = GetInt32(row, "change_version"),
                IsDeleted = GetBoolean(row, "is_deleted"),
            };

            if (audit.ChangeVersion <= 0)
            {
                audit.ChangeVersion = 1;
            }

            return audit;
        }

        private static int DetermineUserId(DataRow row)
        {
            var userId = GetInt32(row, "performed_by");
            if (userId != 0)
            {
                return userId;
            }

            userId = GetInt32(row, "performed_by_id");
            return userId;
        }

        private static int GetInt32(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName))
            {
                return 0;
            }

            var value = row[columnName];
            if (value == null || value == DBNull.Value)
            {
                return 0;
            }

            if (value is int i)
            {
                return i;
            }

            if (value is long l)
            {
                return unchecked((int)l);
            }

            if (value is short s)
            {
                return s;
            }

            if (value is byte b)
            {
                return b;
            }

            if (value is decimal dec)
            {
                return (int)dec;
            }

            if (int.TryParse(value.ToString(), out var parsed))
            {
                return parsed;
            }

            return 0;
        }

        private static bool GetBoolean(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName))
            {
                return false;
            }

            var value = row[columnName];
            if (value == null || value == DBNull.Value)
            {
                return false;
            }

            return value switch
            {
                bool b => b,
                byte by => by != 0,
                sbyte sb => sb != 0,
                short s => s != 0,
                ushort us => us != 0,
                int i => i != 0,
                uint ui => ui != 0,
                long l => l != 0,
                ulong ul => ul != 0,
                string str when bool.TryParse(str, out var parsed) => parsed,
                _ => false,
            };
        }

        private static string GetString(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName))
            {
                return string.Empty;
            }

            var value = row[columnName];
            return value == null || value == DBNull.Value
                ? string.Empty
                : Convert.ToString(value) ?? string.Empty;
        }

        private static DateTime GetDateTime(DataRow row, params string[] columnNames)
        {
            foreach (var column in columnNames)
            {
                if (!row.Table.Columns.Contains(column))
                {
                    continue;
                }

                var value = row[column];
                if (value == null || value == DBNull.Value)
                {
                    continue;
                }

                if (value is DateTime dt)
                {
                    return dt;
                }

                if (DateTime.TryParse(value.ToString(), out var parsed))
                {
                    return parsed;
                }
            }

            return DateTime.UtcNow;
        }

        private static CapaActionType ParseActionType(string? actionValue, string statusValue)
       {
            if (!string.IsNullOrWhiteSpace(actionValue))
            {
                var normalized = NormalizeActionKey(actionValue);
                if (SpecialActionTypeMappings.TryGetValue(normalized, out var mapped))
                {
                    return mapped;
                }

                if (Enum.TryParse<CapaActionType>(normalized, true, out var parsed))
                {
                    return parsed;
                }
            }

            return string.IsNullOrWhiteSpace(statusValue)
                ? CapaActionType.Custom
                : CapaActionType.StatusChange;
        }

        private static string NormalizeActionForQuery(CapaActionType action)
        {
            var canonical = Enum.GetName(typeof(CapaActionType), action) ?? action.ToString();
            var raw = action switch
            {
                CapaActionType.CorrectiveAction or CapaActionType.ACTION_EXECUTED => "korektivna",
                CapaActionType.PreventiveAction or CapaActionType.PreventiveActionPlanning => "preventivna",
                _ => canonical,
            };

            return NormalizeActionKey(raw);
        }

        private static string NormalizeActionKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = value.Trim();
            trimmed = trimmed.Replace("_", string.Empty)
                             .Replace("-", string.Empty)
                             .Replace(" ", string.Empty);
            return trimmed.ToLowerInvariant();
        }
    }
}
