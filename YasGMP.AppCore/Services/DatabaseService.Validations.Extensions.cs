using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.Common;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// Validation CRUD + audit helpers for <see cref="DatabaseService"/>.
    /// <para>Non-invasive: keeps your main <c>DatabaseService.cs</c> unchanged.</para>
    /// </summary>
    public static class DatabaseServiceValidationsExtensions
    {
        #region === SELECT ===

        /// <summary>
        /// Retrieves all validations. Extra flags are accepted for call-site compatibility.
        /// </summary>
        public static async Task<List<Validation>> GetAllValidationsAsync(
            this DatabaseService db,
            bool includeAudit = false,
            bool includeProtocols = false,
            bool includeAttachments = false,
            CancellationToken token = default)
        {
            const string sql = @"SELECT id, code, type, machine_id, component_id, date_start, date_end, documentation, comment, status, digital_signature, next_due
                                 FROM validations
                                 ORDER BY date_start IS NULL, date_start DESC, id DESC";
            var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);

            var list = new List<Validation>();
            foreach (DataRow r in dt.Rows)
                list.Add(ParseValidation(r));
            return list;
        }

        /// <summary>Retrieves one validation by its primary key.</summary>
        public static async Task<Validation?> GetValidationByIdAsync(
            this DatabaseService db,
            int id,
            CancellationToken token = default)
        {
            const string sql = @"SELECT id, code, type, machine_id, component_id, date_start, date_end, documentation, comment, status, digital_signature, next_due
                                 FROM validations WHERE id=@id LIMIT 1";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            return dt.Rows.Count == 0 ? null : ParseValidation(dt.Rows[0]);
        }

        #endregion

        #region === INSERT / UPDATE / DELETE ===

        /// <summary>
        /// Inserts a new record or updates an existing one. When <paramref name="update"/> is <c>false</c>,
        /// a new row is inserted and the generated identity is written back to <see cref="Validation.Id"/>.
        /// </summary>
        public static async Task InsertOrUpdateValidationAsync(
            this DatabaseService db,
            Validation v,
            bool update,
            int? actorUserId = null,
            CancellationToken token = default)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));

            if (!update)
            {
                const string ins = @"
INSERT INTO validations
(code, type, machine_id, component_id, date_start, date_end, documentation, comment, status, digital_signature, next_due, source_ip, session_id)
VALUES
(@code,@type,@machine,@comp,@ds,@de,@doc,@comm,@status,@sig,@next,@source_ip,@session);";
                var insPars = BuildParameters(v, includeId: false);
                await db.ExecuteNonQueryAsync(ins, insPars, token).ConfigureAwait(false);

                v.Id = Convert.ToInt32(await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));

                // System event log (device info is not a property of Validation; pass null here).
                await db.LogSystemEventAsync(
                    userId: actorUserId,
                    eventType: "VALIDATION_CREATE",
                    tableName: "validations",
                    module: "ValidationModule",
                    recordId: v.Id,
                    description: $"Created validation {v.Code} ({v.Type})",
                    ip: v.SourceIp ?? "system",
                    severity: "audit",
                    deviceInfo: "N/A",
                    sessionId: v.SessionId,
                    token: token).ConfigureAwait(false);
            }
            else
            {
                const string upd = @"
UPDATE validations SET
 code=@code, type=@type, machine_id=@machine, component_id=@comp, date_start=@ds, date_end=@de,
 documentation=@doc, comment=@comm, status=@status, digital_signature=@sig, next_due=@next,
 source_ip=@source_ip, session_id=@session
WHERE id=@id;";
                var updPars = BuildParameters(v, includeId: true);
                await db.ExecuteNonQueryAsync(upd, updPars, token).ConfigureAwait(false);

                await db.LogSystemEventAsync(
                    userId: actorUserId,
                    eventType: "VALIDATION_UPDATE",
                    tableName: "validations",
                    module: "ValidationModule",
                    recordId: v.Id,
                    description: $"Updated validation {v.Code} ({v.Type})",
                    ip: v.SourceIp ?? "system",
                    severity: "audit",
                    deviceInfo: "N/A",
                    sessionId: v.SessionId,
                    token: token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Add-only helper used by some view-models (includes audit context parameters).
        /// </summary>
        public static async Task<int> AddValidationAsync(
            this DatabaseService db,
            Validation v,
            string signatureHash,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            v.DigitalSignature = signatureHash ?? string.Empty;
            v.SourceIp = ip;                       // ip is non-null parameter
            v.SessionId = sessionId ?? string.Empty; // CS8601 fix: coalesce

            await db.InsertOrUpdateValidationAsync(v, update: false, actorUserId: null, token: token).ConfigureAwait(false);

            await db.LogValidationAuditAsync(v, "CREATE", ip, deviceInfo, sessionId, signatureHash, token).ConfigureAwait(false);
            return v.Id;
        }

        /// <summary>
        /// Update-only helper used by some view-models (includes audit context parameters).
        /// </summary>
        public static async Task UpdateValidationAsync(
            this DatabaseService db,
            Validation v,
            string signatureHash,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            v.DigitalSignature = signatureHash ?? string.Empty;
            v.SourceIp = ip;                         // ip is non-null parameter
            v.SessionId = sessionId ?? string.Empty; // CS8601 fix: coalesce

            await db.InsertOrUpdateValidationAsync(v, update: true, actorUserId: null, token: token).ConfigureAwait(false);
            await db.LogValidationAuditAsync(v, "UPDATE", ip, deviceInfo, sessionId, signatureHash, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes a validation. Optional <paramref name="actorUserId"/> is logged.
        /// </summary>
        public static async Task DeleteValidationAsync(
            this DatabaseService db,
            int id,
            int? actorUserId = null,
            CancellationToken token = default)
        {
            const string sql = @"DELETE FROM validations WHERE id=@id";
            await db.ExecuteNonQueryAsync(sql, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);

            await db.LogSystemEventAsync(
                userId: actorUserId,
                eventType: "VALIDATION_DELETE",
                tableName: "validations",
                module: "ValidationModule",
                recordId: id,
                description: $"Deleted validation ID={id}",
                ip: "system",
                severity: "audit",
                deviceInfo: "N/A",
                sessionId: null,
                token: token).ConfigureAwait(false);
        }

        #endregion

        #region === ROLLBACK / EXPORT / AUDIT ===

        /// <summary>
        /// Lightweight rollback helper. For now it logs the intent to keep the UI flow working.
        /// </summary>
        public static async Task RollbackValidationAsync(
            this DatabaseService db,
            int id,
            string? ip,
            string? deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            await db.LogSystemEventAsync(
                userId: null,
                eventType: "VALIDATION_ROLLBACK",
                tableName: "validations",
                module: "ValidationModule",
                recordId: id,
                description: $"Rollback requested for validation ID={id}",
                ip: ip ?? "system",
                severity: "audit",
                deviceInfo: deviceInfo ?? "N/A",
                sessionId: sessionId,
                token: token).ConfigureAwait(false);
        }

        /// <summary>
        /// Exports the provided validations to CSV under app data and logs the action.
        /// </summary>
        public static async Task<string> ExportValidationsAsync(
            this DatabaseService db,
            IList<Validation> items,
            string? ip,
            string? deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            var platform = ServiceLocator.GetService<IPlatformService>();
            string? baseDir = platform?.GetAppDataDirectory();

            if (string.IsNullOrWhiteSpace(baseDir))
            {
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                baseDir = string.IsNullOrWhiteSpace(local)
                    ? Path.Combine(AppContext.BaseDirectory, "AppData")
                    : Path.Combine(local, "YasGMP");
            }

            var root = Path.Combine(baseDir!, "Exports", "Validations");
            Directory.CreateDirectory(root);

            string path = Path.Combine(root, $"Validations_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            var sb = new StringBuilder();
            sb.AppendLine("Id;Code;Type;MachineId;ComponentId;DateStart;DateEnd;Status;NextDue;Signature");

            foreach (var v in items ?? Array.Empty<Validation>())
            {
                sb.AppendLine(string.Join(';', new[]
                {
                    v.Id.ToString(CultureInfo.InvariantCulture),
                    Escape(v.Code),
                    Escape(v.Type),
                    v.MachineId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    v.ComponentId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    v.DateStart?.ToString("yyyy-MM-dd") ?? string.Empty,
                    v.DateEnd?.ToString("yyyy-MM-dd") ?? string.Empty,
                    Escape(v.Status),
                    v.NextDue?.ToString("yyyy-MM-dd") ?? string.Empty,
                    Escape(v.DigitalSignature)
                }));
            }

            await File.WriteAllTextAsync(path, sb.ToString(), token).ConfigureAwait(false);

            await db.LogSystemEventAsync(
                userId: null,
                eventType: "VALIDATION_EXPORT",
                tableName: "validations",
                module: "ValidationModule",
                recordId: null,
                description: $"Exported {items?.Count ?? 0} validation rows to {path}",
                ip: ip ?? "system",
                severity: "audit",
                deviceInfo: deviceInfo ?? "N/A",
                sessionId: sessionId,
                token: token).ConfigureAwait(false);

            return path;

            static string Escape(string? s) => (s ?? string.Empty).Replace(';', ',');
        }

        /// <summary>
        /// Writes a row to the <c>validation_audit</c> table for a given action.
        /// </summary>
        public static async Task LogValidationAuditAsync(
            this DatabaseService db,
            Validation? v,
            string action,
            string? ip,
            string? deviceInfo,
            string? sessionId,
            string? signatureHash,
            CancellationToken token = default)
        {
            const string sql = @"
INSERT INTO validation_audit
(validation_id, user_id, action, changed_at, details, digital_signature, source_ip, device_info)
VALUES
(@vid,@uid,@act,UTC_TIMESTAMP(),@det,@sig,@ip,@dev);";

            var pars = new[]
            {
                new MySqlParameter("@vid", v?.Id ?? 0),
                new MySqlParameter("@uid", v?.CreatedById ?? (object)DBNull.Value),
                new MySqlParameter("@act", action ?? string.Empty),
                new MySqlParameter("@det", $"Validation {action}: {v?.Code ?? "-"} ({v?.Type ?? "-"})"),
                new MySqlParameter("@sig", signatureHash ?? v?.DigitalSignature ?? string.Empty),
                new MySqlParameter("@ip",  ip ?? v?.SourceIp ?? "unknown"),
                new MySqlParameter("@dev", deviceInfo ?? "N/A"),
            };

            await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);
        }

        #endregion

        #region === Mapping helpers ===

        /// <summary>
        /// Maps a <see cref="DataRow"/> to a <see cref="Validation"/> instance in a schema-tolerant way,
        /// coalescing nullable strings to <see cref="string.Empty"/> to avoid CS8601 warnings when model
        /// properties are non-nullable.
        /// </summary>
        private static Validation ParseValidation(DataRow r)
        {
            int? ToInt(object o) => o == DBNull.Value ? (int?)null : Convert.ToInt32(o);
            DateTime? ToDt(object o) => o == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(o);

            return new Validation
            {
                Id               = r.Table.Columns.Contains("id")            ? Convert.ToInt32(r["id"]) : 0,
                Code             = r.Table.Columns.Contains("code")          ? (r["code"]?.ToString() ?? string.Empty) : string.Empty,
                Type             = r.Table.Columns.Contains("type")          ? (r["type"]?.ToString() ?? string.Empty) : string.Empty,
                MachineId        = r.Table.Columns.Contains("machine_id")    ? ToInt(r["machine_id"]) : null,
                ComponentId      = r.Table.Columns.Contains("component_id")  ? ToInt(r["component_id"]) : null,
                DateStart        = r.Table.Columns.Contains("date_start")    ? ToDt(r["date_start"]) : null,
                DateEnd          = r.Table.Columns.Contains("date_end")      ? ToDt(r["date_end"]) : null,
                Documentation    = r.Table.Columns.Contains("documentation") ? (r["documentation"]?.ToString() ?? string.Empty) : string.Empty,
                Comment          = r.Table.Columns.Contains("comment")       ? (r["comment"]?.ToString() ?? string.Empty) : string.Empty,
                Status           = r.Table.Columns.Contains("status")        ? (r["status"]?.ToString() ?? string.Empty) : string.Empty,
                DigitalSignature = r.Table.Columns.Contains("digital_signature") ? (r["digital_signature"]?.ToString() ?? string.Empty) : string.Empty,
                NextDue          = r.Table.Columns.Contains("next_due")      ? ToDt(r["next_due"]) : null
            };
        }

        /// <summary>
        /// Builds parameter array for INSERT/UPDATE against the <c>validations</c> table.
        /// </summary>
        private static MySqlParameter[] BuildParameters(Validation v, bool includeId)
        {
            var list = new List<MySqlParameter>
            {
                new("@code",   v.Code ?? string.Empty),
                new("@type",   v.Type ?? string.Empty),
                new("@machine",v.MachineId ?? (object)DBNull.Value),
                new("@comp",   v.ComponentId ?? (object)DBNull.Value),
                new("@ds",     v.DateStart ?? (object)DBNull.Value),
                new("@de",     v.DateEnd ?? (object)DBNull.Value),
                new("@doc",    v.Documentation ?? string.Empty),
                new("@comm",   v.Comment ?? string.Empty),
                new("@status", v.Status ?? string.Empty),
                new("@sig",    v.DigitalSignature ?? string.Empty),
                new("@next",   v.NextDue ?? (object)DBNull.Value),
                new("@source_ip", string.IsNullOrWhiteSpace(v.SourceIp) ? (object)DBNull.Value : v.SourceIp),
                new("@session",   string.IsNullOrWhiteSpace(v.SessionId) ? (object)DBNull.Value : v.SessionId),
            };
            if (includeId) list.Add(new MySqlParameter("@id", v.Id));
            return list.ToArray();
        }

        #endregion
    }
}

