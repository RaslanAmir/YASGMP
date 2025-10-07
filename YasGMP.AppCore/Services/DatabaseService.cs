using System;
using System.Collections.Generic;
using System.Globalization;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Diagnostics;
using YasGMP.Services.Database;
using Microsoft.Extensions.Configuration;

namespace YasGMP.Services
{
    /// <summary>
    /// Core ADO.NET gateway for MySQL with parameterized helpers and canonical audit writer.
    /// Partial class; other functional areas live in separate extension/partial files.
    /// </summary>
    public sealed partial class DatabaseService
    {
        private readonly string _connectionString;
        private DiagnosticContext? _diagCtx;
        private ITrace? _trace;
        private ShadowReplicator? _shadow;

        /// <summary>Default command timeout (seconds) applied to all SQL commands.</summary>
        public int CommandTimeoutSeconds { get; set; } = 15;

        /// <summary>Create a new DatabaseService using the given MySQL connection string.</summary>
        public DatabaseService(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string is required", nameof(connectionString));
            _connectionString = connectionString;
        }

        private MySqlConnection CreateConnection() => new MySqlConnection(_connectionString);

        /// <summary>Attach diagnostics context and tracer for SQL telemetry.</summary>
        public void SetDiagnostics(DiagnosticContext ctx, ITrace trace)
        {
            _diagCtx = ctx; _trace = trace;
            var shadowConn = TryGetShadowConnFromConfig();
            _shadow = new ShadowReplicator(_connectionString, shadowConn, ctx, trace);
        }

        /// <summary>Global fallbacks for components created outside DI.</summary>
        public static DiagnosticContext? GlobalDiagnosticContext { get; set; }
        /// <summary>
        /// Gets or sets the global trace.
        /// </summary>
        public static ITrace? GlobalTrace { get; set; }
        /// <summary>
        /// Gets or sets the global configuration.
        /// </summary>
        public static IConfiguration? GlobalConfiguration { get; set; }

        private string? TryGetShadowConnFromConfig()
        {
            try
            {
                var cfg = GlobalConfiguration ?? TryResolveMauiConfiguration();
                var en = cfg? ["Diagnostics:DbShadow:Enabled"]; bool enabled = false;
                if (!string.IsNullOrWhiteSpace(en)) bool.TryParse(en, out enabled);
                if (!enabled) return null;
                var direct = cfg? ["Diagnostics:DbShadow:ConnectionString"];
                if (!string.IsNullOrWhiteSpace(direct)) return direct;
                var shadowDb = cfg? ["Diagnostics:DbShadow:Database"];
                if (string.IsNullOrWhiteSpace(shadowDb)) return null;
                // Replace Database=... in primary connection string
                var parts = _connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    var kv = parts[i].Split('=', 2, StringSplitOptions.TrimEntries);
                    if (kv.Length == 2 && kv[0].Equals("Database", StringComparison.OrdinalIgnoreCase))
                        parts[i] = $"Database={shadowDb}";
                }
                return string.Join(';', parts);
            }
            catch { return null; }
        }

        private static IConfiguration? TryResolveMauiConfiguration()
        {
            try
            {
                var appType = Type.GetType("Microsoft.Maui.Controls.Application, Microsoft.Maui.Controls");
                if (appType == null) return null;
                var current = appType.GetProperty("Current")?.GetValue(null);
                if (current == null) return null;
                var handler = current.GetType().GetProperty("Handler")?.GetValue(current);
                var mauiContext = handler?.GetType().GetProperty("MauiContext")?.GetValue(handler);
                var services = mauiContext?.GetType().GetProperty("Services")?.GetValue(mauiContext) as IServiceProvider;
                return services?.GetService(typeof(IConfiguration)) as IConfiguration;
            }
            catch
            {
                return null;
            }
        }

        // ==============================================================
        // Low-level SQL helpers (parameterized, async, schema-agnostic)
        // ==============================================================

        /// <summary>Executes a non-query (INSERT/UPDATE/DELETE). Returns affected rows.</summary>
        public async Task<int> ExecuteNonQueryAsync(
            string sql,
            IEnumerable<MySqlParameter>? parameters = null,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentNullException(nameof(sql));
            if (ExecuteNonQueryOverride != null)
                return await ExecuteNonQueryOverride(sql, parameters, token).ConfigureAwait(false);
            await using var conn = CreateConnection();
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.CommandTimeout = CommandTimeoutSeconds;
            var ctx = _diagCtx ?? GlobalDiagnosticContext;
            var tr  = _trace   ?? GlobalTrace;
            if (ctx != null && tr != null)
            {
                var rows = await DbCommandWrapper.ExecuteNonQueryAsync(cmd, ctx, tr, sql, parameters, token).ConfigureAwait(false);
                // Shadow replicate (debug-only if configured)
                if (_shadow?.Enabled == true)
                {
                    try { _ = _shadow.TryShadowExecuteNonQueryAsync(sql, parameters, rows, token); } catch { }
                }
                return rows;
            }
            if (parameters != null)
                foreach (var p in parameters) cmd.Parameters.Add(p);
            return await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }

        /// <summary>Executes a scalar command and returns the first column of the first row.</summary>
        public async Task<object?> ExecuteScalarAsync(
            string sql,
            IEnumerable<MySqlParameter>? parameters = null,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentNullException(nameof(sql));
            if (ExecuteScalarOverride != null)
                return await ExecuteScalarOverride(sql, parameters, token).ConfigureAwait(false);
            await using var conn = CreateConnection();
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.CommandTimeout = CommandTimeoutSeconds;
            var ctx = _diagCtx ?? GlobalDiagnosticContext;
            var tr  = _trace   ?? GlobalTrace;
            if (ctx != null && tr != null)
            {
                return await DbCommandWrapper.ExecuteScalarAsync(cmd, ctx, tr, sql, parameters, token).ConfigureAwait(false);
            }
            if (parameters != null)
                foreach (var p in parameters) cmd.Parameters.Add(p);
            return await cmd.ExecuteScalarAsync(token).ConfigureAwait(false);
        }

        /// <summary>Executes a SELECT and returns a DataTable (loaded from a reader).</summary>
        public async Task<DataTable> ExecuteSelectAsync(
            string sql,
            IEnumerable<MySqlParameter>? parameters = null,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentNullException(nameof(sql));
            if (ExecuteSelectOverride != null)
                return await ExecuteSelectOverride(sql, parameters, token).ConfigureAwait(false);
            await using var conn = CreateConnection();
            await conn.OpenAsync(token).ConfigureAwait(false);
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.CommandTimeout = CommandTimeoutSeconds;
            var ctx = _diagCtx ?? GlobalDiagnosticContext;
            var tr  = _trace   ?? GlobalTrace;
            if (ctx != null && tr != null)
            {
                return await DbCommandWrapper.ExecuteSelectAsync(cmd, ctx, tr, sql, parameters, token).ConfigureAwait(false);
            }
            if (parameters != null)
                foreach (var p in parameters) cmd.Parameters.Add(p);

            var dt = new DataTable();
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, token).ConfigureAwait(false);
            dt.Load(reader);
            return dt;
        }

        /// <summary>
        /// Executes a reader and returns an open <see cref="MySqlDataReader"/>. Caller should dispose it.
        /// Uses CommandBehavior.CloseConnection so disposing the reader closes the connection.
        /// </summary>
        public async Task<MySqlDataReader?> ExecuteReaderAsync(
            string sql,
            IEnumerable<MySqlParameter>? parameters = null,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(sql)) throw new ArgumentNullException(nameof(sql));
            var conn = CreateConnection();
            await conn.OpenAsync(token).ConfigureAwait(false);
            var cmd = new MySqlCommand(sql, conn);
            cmd.CommandTimeout = CommandTimeoutSeconds;
            if (parameters != null)
                foreach (var p in parameters) cmd.Parameters.Add(p);
            try
            {
                return (MySqlDataReader)await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection, token).ConfigureAwait(false);
            }
            catch
            {
                await conn.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        // ==============================================================
        // Reflection helpers used by partials (schema-tolerant mapping)
        // ==============================================================

        /// <summary>Safe property getter for value types; returns nullable when missing.</summary>
        private static T? TryGet<T>(object source, string propertyName) where T : struct
        {
            if (source == null || string.IsNullOrWhiteSpace(propertyName)) return null;
            try
            {
                var prop = source.GetType().GetProperty(propertyName);
                if (prop == null) return null;
                var val = prop.GetValue(source);
                if (val == null || val is DBNull) return null;
                var dest = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                if (val is IConvertible) return (T)Convert.ChangeType(val, dest);
                return (T)val;
            }
            catch { return null; }
        }

        /// <summary>Safe property getter for reference types; returns null when missing.</summary>
        private static T? TryGetRef<T>(object source, string propertyName) where T : class
        {
            if (source == null || string.IsNullOrWhiteSpace(propertyName)) return null;
            try
            {
                var prop = source.GetType().GetProperty(propertyName);
                if (prop == null) return null;
                var val = prop.GetValue(source);
                return val as T;
            }
            catch { return null; }
        }

        /// <summary>Convenience getter for string properties via <see cref="TryGetRef{T}(object,string)"/>.</summary>
        private static string? TryGetString(object source, string propertyName) => TryGetRef<string>(source, propertyName);

        // ==============================================================
        // Canonical system event logger (21 CFR Part 11 / Annex 11)
        // ==============================================================

        /// <summary>
        /// Canonical, schema-explicit system event logger. Inserts an Annex 11 / 21 CFR Part 11 style entry into
        /// system_event_log with rich context (table, record, field, before/after values, device, session, severity).
        /// Backward compatible with legacy named arguments (eventCode/table) and the positional overload.
        /// </summary>
        /// <param name="userId">User id associated with the event (nullable).</param>
        /// <param name="eventType">Event type (e.g., CREATE, UPDATE, DELETE, LOGIN). If null/empty and <paramref name="eventCode"/> is provided, that value is used.</param>
        /// <param name="tableName">Logical/DB table affected (nullable).</param>
        /// <param name="module">Module or sub-system that triggered the event (nullable).</param>
        /// <param name="recordId">Primary key of the affected record (nullable).</param>
        /// <param name="description">Detailed description of the event (nullable).</param>
        /// <param name="ip">Source IP address (nullable).</param>
        /// <param name="severity">Severity level (info, warn, error, critical). Default is info.</param>
        /// <param name="deviceInfo">Device information string (nullable).</param>
        /// <param name="sessionId">Session identifier (nullable).</param>
        /// <param name="fieldName">Affected field name (nullable).</param>
        /// <param name="oldValue">Previous field value (nullable).</param>
        /// <param name="newValue">New field value (nullable).</param>
        /// <param name="eventCode">Legacy alias for eventType used if eventType is null/empty.</param>
        /// <param name="table">Legacy alias for tableName used if tableName is null.</param>
        /// <param name="token">Cancellation token for the async operation.</param>

        public async Task LogSystemEventAsync(
            int? userId,
            string? eventType,
            string? tableName,
            string? module,
            int? recordId,
            string? description,
            string? ip,
            string? severity,
            string? deviceInfo,
            string? sessionId,
            string? fieldName = null,
            string? oldValue = null,
            string? newValue = null,
            string? eventCode = null,
            string? table = null,
            int? signatureId = null,
            string? signatureHash = null,
            CancellationToken token = default)
        {
            // Normalize + legacy fallbacks
            var type  = string.IsNullOrWhiteSpace(eventType) ? (eventCode ?? "EVENT") : eventType;
            var tableResolved = string.IsNullOrWhiteSpace(tableName) ? (table ?? "system") : tableName;
            var sev   = string.IsNullOrWhiteSpace(severity) ? "info" : severity;
            var normalizedSignatureHash = string.IsNullOrWhiteSpace(signatureHash) ? null : signatureHash;

            string? descriptionWithSignature = description;
            if (signatureId.HasValue || !string.IsNullOrWhiteSpace(normalizedSignatureHash))
            {
                var idPart = signatureId.HasValue
                    ? signatureId.Value.ToString(CultureInfo.InvariantCulture)
                    : "-";
                var hashPart = normalizedSignatureHash ?? "-";
                var suffix = $"sigId={idPart}; sigHash={hashPart}";
                descriptionWithSignature = string.IsNullOrWhiteSpace(descriptionWithSignature)
                    ? suffix
                    : $"{descriptionWithSignature} [{suffix}]";
            }

            // Preferred full insert including device/session/field deltas
            const string sqlFull = @"
INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity, digital_signature_id, digital_signature)
VALUES
    (@uid, @etype, @table, @module, @rid, @field,
     @old, @new, @desc, @ip, @dev, @sid, @sev, @sigId, @sigHash);";

            const string sqlFullHashOnly = @"
INSERT INTO system_event_log
    (user_id, event_type, table_name, related_module, record_id, field_name,
     old_value, new_value, description, source_ip, device_info, session_id, severity, digital_signature)
VALUES
    (@uid, @etype, @table, @module, @rid, @field,
     @old, @new, @desc, @ip, @dev, @sid, @sev, @sigHash);";

            var pars = new List<MySqlParameter>
            {
                new("@uid",   (object?)userId ?? DBNull.Value),
                new("@etype", type ?? "EVENT"),
                new("@table", tableResolved ?? "system"),
                new("@module", (object?)module ?? DBNull.Value),
                new("@rid",   (object?)recordId ?? DBNull.Value),
                new("@field", (object?)fieldName ?? DBNull.Value),
                new("@old",   (object?)oldValue ?? DBNull.Value),
                new("@new",   (object?)newValue ?? DBNull.Value),
                new("@desc",  (object?)descriptionWithSignature ?? DBNull.Value),
                new("@ip",    (object?)ip ?? DBNull.Value),
                new("@dev",   (object?)deviceInfo ?? DBNull.Value),
                new("@sid",   (object?)sessionId ?? DBNull.Value),
                new("@sev",   sev ?? "info"),
            };

            try
            {
                var fullPars = new List<MySqlParameter>(pars)
                {
                    new("@sigId", (object?)signatureId ?? DBNull.Value),
                    new("@sigHash", (object?)normalizedSignatureHash ?? DBNull.Value),
                };
                _ = await ExecuteNonQueryAsync(sqlFull, fullPars, token).ConfigureAwait(false);
                return;
            }
            catch (MySqlException ex) when (ex.Number == 1054 || ex.Number == 1146)
            {
                // Unknown column or missing table -> try minimal legacy shape
            }
            catch
            {
                // Last resort: swallow to avoid cascading failures in UI flows
                return;
            }

            try
            {
                var hashOnlyPars = new List<MySqlParameter>(pars)
                {
                    new("@sigHash", (object?)normalizedSignatureHash ?? DBNull.Value),
                };
                _ = await ExecuteNonQueryAsync(sqlFullHashOnly, hashOnlyPars, token).ConfigureAwait(false);
                return;
            }
            catch (MySqlException ex) when (ex.Number == 1054 || ex.Number == 1146)
            {
                // Unknown column or missing table -> try minimal legacy shape
            }
            catch
            {
                // Last resort: swallow to avoid cascading failures in UI flows
                return;
            }

            try
            {
                const string sqlMinimal = @"
INSERT INTO system_event_log (user_id, event_type, table_name, related_module, record_id, description, source_ip, severity)
VALUES (@uid, @etype, @table, @module, @rid, @desc, @ip, @sev);";

                var minPars = new List<MySqlParameter>
                {
                    new("@uid",   (object?)userId ?? DBNull.Value),
                    new("@etype", type ?? "EVENT"),
                    new("@table", tableResolved ?? "system"),
                    new("@module", (object?)module ?? DBNull.Value),
                    new("@rid",   (object?)recordId ?? DBNull.Value),
                    new("@desc",  (object?)descriptionWithSignature ?? DBNull.Value),
                    new("@ip",    (object?)ip ?? DBNull.Value),
                    new("@sev",   sev ?? "info"),
                };
                _ = await ExecuteNonQueryAsync(sqlMinimal, minPars, token).ConfigureAwait(false);
            }
            catch
            {
                // Give up silently; audit tables may not exist in dev databases.
            }
        }
        /// <summary>
        /// Writes a permission-change style audit entry (used by RBAC shims).
        /// Schema-tolerant: emits to system_event_log with details payload.
        /// </summary>
        public Task LogPermissionChangeAsync(
            int? userId,
            int? roleId,
            int? permissionId,
            string changeType,
            int changedByUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            string action,
            string? details,
            string? reason,
            CancellationToken token = default)
        {
            var desc = $"change={changeType}; action={action}; roleId={roleId}; permId={permissionId}; by={changedByUserId}; reason={reason}; details={details}";
            return LogSystemEventAsync(
                userId: userId ?? changedByUserId,
                eventType: $"PERMISSION_{action}",
                tableName: "permissions",
                module: "RBAC",
                recordId: permissionId,
                description: desc,
                ip: ip,
                severity: "audit",
                deviceInfo: deviceInfo,
                sessionId: sessionId,
                token: token
            );
        }
    }
}
