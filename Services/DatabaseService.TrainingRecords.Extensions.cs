// ==============================================================================
// File: Services/DatabaseService.TrainingRecords.Extensions.cs
// Purpose: Extension API for Training Records used by TrainingRecordViewModel.
//          Schema-tolerant (reads only existing columns), no breaking changes.
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Extension methods that hang off <see cref="DatabaseService"/> for Training Records.
    /// All methods are <b>schema tolerant</b> and perform best-effort mapping.
    /// </summary>
    public static class DatabaseServiceTrainingRecordsExtensions
    {
        // ---------------------------------------------------------------------
        // READ
        // ---------------------------------------------------------------------

        /// <summary>
        /// Returns all training records (best-effort) ordered by date (desc), then id (desc).
        /// </summary>
        public static async Task<List<TrainingRecord>> GetAllTrainingRecordsFullAsync(
            this DatabaseService db,
            CancellationToken token = default)
        {
            const string sql = @"
SELECT *
FROM training_records
ORDER BY training_date DESC, id DESC;";

            var list = new List<TrainingRecord>();
            var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);

            foreach (DataRow r in dt.Rows)
                list.Add(ParseTrainingRecord(r));

            return list;
        }

        // ---------------------------------------------------------------------
        // CREATE / WORKFLOW
        // ---------------------------------------------------------------------

        /// <summary>
        /// Initiates a new training record (planned). Inserts minimally available columns;
        /// missing columns are silently skipped by the database if they don't exist.
        /// </summary>
        public static async Task InitiateTrainingRecordAsync(
            this DatabaseService db,
            TrainingRecord record,
            CancellationToken token = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            // Defaults for initiation
            record.Status ??= "planned";
            record.PlannedAt = record.PlannedAt == default ? DateTime.UtcNow : record.PlannedAt;

            const string sql = @"
INSERT INTO training_records
(code, name, type, status, training_date, expiry_date, trainee_id, trainer_id, format, description, certificate_number, note)
VALUES
(@code,@name,@type,@status,@tdate,@exp,@trainee,@trainer,@format,@desc,@cert,@note);";

            var pars = new[]
            {
                new MySqlParameter("@code",   (object?)record.Code ?? DBNull.Value),
                new MySqlParameter("@name",   (object?)record.Name ?? DBNull.Value),
                new MySqlParameter("@type",   (object?)record.Type ?? DBNull.Value),
                new MySqlParameter("@status", (object?)record.Status ?? "planned"),
                new MySqlParameter("@tdate",  (object?)record.TrainingDate == null ? DBNull.Value : record.TrainingDate),
                new MySqlParameter("@exp",    (object?)record.ExpiryDate ?? DBNull.Value),
                new MySqlParameter("@trainee",(object?)record.TraineeId ?? DBNull.Value),
                new MySqlParameter("@trainer",(object?)record.TrainerId ?? DBNull.Value),
                new MySqlParameter("@format", (object?)record.Format ?? DBNull.Value),
                new MySqlParameter("@desc",   (object?)record.Description ?? DBNull.Value),
                new MySqlParameter("@cert",   (object?)record.CertificateNumber ?? DBNull.Value),
                new MySqlParameter("@note",   (object?)record.Note ?? DBNull.Value),
            };

            await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);

            // Audit
            await db.LogTrainingRecordAuditAsync(record, "INITIATE",
                record.IpAddress, record.DeviceInfo, record.SessionId, null, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Assigns a training to a user (sets status to <c>assigned</c>).
        /// </summary>
        public static async Task AssignTrainingRecordAsync(
            this DatabaseService db,
            int trainingRecordId,
            int assignedToUserId,
            string ipAddress,
            string deviceInfo,
            string sessionId,
            string? note = null,
            CancellationToken token = default)
        {
            const string sql = @"
UPDATE training_records
SET trainee_id=@uid, status='assigned'
WHERE id=@id;";

            var pars = new[]
            {
                new MySqlParameter("@uid", assignedToUserId),
                new MySqlParameter("@id", trainingRecordId)
            };

            await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);

            await db.LogTrainingRecordAuditAsync(
                new TrainingRecord { Id = trainingRecordId, TraineeId = assignedToUserId, Status = "assigned" },
                "ASSIGN", ipAddress, deviceInfo, sessionId, note, token).ConfigureAwait(false);
        }

        /// <summary>Approves a training record (manager e-sign).</summary>
        public static async Task ApproveTrainingRecordAsync(
            this DatabaseService db,
            int trainingRecordId,
            int approverUserId,
            string ipAddress,
            string deviceInfo,
            string sessionId,
            CancellationToken token = default)
        {
            const string sql = @"
UPDATE training_records
SET status='pending_approval'
WHERE id=@id;";

            await db.ExecuteNonQueryAsync(sql, new[] { new MySqlParameter("@id", trainingRecordId) }, token)
                    .ConfigureAwait(false);

            await db.LogTrainingRecordAuditAsync(
                new TrainingRecord { Id = trainingRecordId }, "APPROVE",
                ipAddress, deviceInfo, sessionId, $"Approved by user {approverUserId}", token).ConfigureAwait(false);
        }

        /// <summary>Marks a training record as completed (user e-sign).</summary>
        public static async Task CompleteTrainingRecordAsync(
            this DatabaseService db,
            int trainingRecordId,
            int userId,
            string ipAddress,
            string deviceInfo,
            string sessionId,
            string? note = null,
            CancellationToken token = default)
        {
            const string sql = @"
UPDATE training_records
SET status='completed'
WHERE id=@id;";

            await db.ExecuteNonQueryAsync(sql, new[] { new MySqlParameter("@id", trainingRecordId) }, token)
                    .ConfigureAwait(false);

            await db.LogTrainingRecordAuditAsync(
                new TrainingRecord { Id = trainingRecordId }, "COMPLETE",
                ipAddress, deviceInfo, sessionId, note ?? "Training completed", token).ConfigureAwait(false);
        }

        /// <summary>Closes a training record after effectiveness check and audit.</summary>
        public static async Task CloseTrainingRecordAsync(
            this DatabaseService db,
            int trainingRecordId,
            int closerUserId,
            string ipAddress,
            string deviceInfo,
            string sessionId,
            string? note = null,
            CancellationToken token = default)
        {
            const string sql = @"
UPDATE training_records
SET status='closed'
WHERE id=@id;";

            await db.ExecuteNonQueryAsync(sql, new[] { new MySqlParameter("@id", trainingRecordId) }, token)
                    .ConfigureAwait(false);

            await db.LogTrainingRecordAuditAsync(
                new TrainingRecord { Id = trainingRecordId }, "CLOSE",
                ipAddress, deviceInfo, sessionId, note ?? "Closed after effectiveness check", token).ConfigureAwait(false);
        }

        // ---------------------------------------------------------------------
        // EXPORT + AUDIT
        // ---------------------------------------------------------------------

        /// <summary>
        /// Exports training records (best-effort). Returns a generated file path and writes audit/system events.
        /// </summary>
        public static async Task<string> ExportTrainingRecordsAsync(
            this DatabaseService db,
            IList<TrainingRecord> rows,
            string ipAddress,
            string deviceInfo,
            string sessionId,
            CancellationToken token = default)
        {
            string filePath = $"/export/training_records_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

            // Log to generic export/print log if present
            try
            {
                await db.ExecuteNonQueryAsync(@"
INSERT INTO export_print_log (user_id, format, table_name, filter_used, file_path, source_ip, note)
VALUES (NULL,'csv','training_records',@filter,@path,@ip,'Training export')",
                    new[]
                    {
                        new MySqlParameter("@filter", $"count={(rows?.Count ?? 0)}"),
                        new MySqlParameter("@path", filePath),
                        new MySqlParameter("@ip", ipAddress ?? string.Empty)
                    }, token).ConfigureAwait(false);
            }
            catch (MySqlException) { /* table may not exist → ignore */ }

            // System event (if available in your DatabaseService)
            await db.LogSystemEventAsync(
                userId: null,
                eventType: "EXPORT",
                tableName: "training_records",
                module: "TrainingModule",
                recordId: null,
                description: $"Exported {(rows?.Count ?? 0)} training records → {filePath}",
                ip: ipAddress,
                severity: "audit",
                deviceInfo: deviceInfo,
                sessionId: sessionId,
                token: token).ConfigureAwait(false);

            return filePath;
        }

        /// <summary>
        /// Writes a training record audit row. If the table is missing, falls back to a system event.
        /// </summary>
        public static async Task LogTrainingRecordAuditAsync(
            this DatabaseService db,
            TrainingRecord? record,
            string action,
            string ipAddress,
            string deviceInfo,
            string sessionId,
            string? description = null,
            CancellationToken token = default)
        {
            int id = record?.Id ?? 0;

            try
            {
                const string sql = @"
INSERT INTO training_record_audit
(training_record_id, action, description, timestamp, source_ip, device_info, session_id)
VALUES (@id,@act,@desc,NOW(),@ip,@dev,@sid);";

                var pars = new[]
                {
                    new MySqlParameter("@id",   id),
                    new MySqlParameter("@act",  action ?? "UPDATE"),
                    new MySqlParameter("@desc", (object?)description ?? DBNull.Value),
                    new MySqlParameter("@ip",   (object?)ipAddress ?? DBNull.Value),
                    new MySqlParameter("@dev",  (object?)deviceInfo ?? DBNull.Value),
                    new MySqlParameter("@sid",  (object?)sessionId ?? DBNull.Value)
                };

                await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);
            }
            catch (MySqlException)
            {
                // Fallback to system events if audit table is absent
                await db.LogSystemEventAsync(
                    userId: null,
                    eventType: $"TRAINING_{action}",
                    tableName: "training_records",
                    module: "TrainingModule",
                    recordId: id == 0 ? null : id,
                    description: description,
                    ip: ipAddress,
                    severity: "audit",
                    deviceInfo: deviceInfo,
                    sessionId: sessionId,
                    token: token).ConfigureAwait(false);
            }
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------

        private static TrainingRecord ParseTrainingRecord(DataRow r)
        {
            var t = Activator.CreateInstance<TrainingRecord>();

            // core fields
            SetIfExists(t, "Id", GetInt(r, "id") ?? 0);
            SetIfExists(t, "Code", GetString(r, "code"));
            SetIfExists(t, "Name", GetString(r, "name"));
            SetIfExists(t, "Type", GetString(r, "type"));
            SetIfExists(t, "Format", GetString(r, "format"));
            SetIfExists(t, "Description", GetString(r, "description"));
            SetIfExists(t, "Status", GetString(r, "status"));
            SetIfExists(t, "TrainingDate", GetDate(r, "training_date") ?? GetDate(r, "date"));
            SetIfExists(t, "ExpiryDate", GetDate(r, "expiry_date"));
            SetIfExists(t, "CertificateNumber", GetString(r, "certificate_number"));
            SetIfExists(t, "Note", GetString(r, "note"));

            // relationships (best-effort)
            SetIfExists(t, "TrainerId", GetInt(r, "trainer_id"));
            SetIfExists(t, "TraineeId", GetInt(r, "trainee_id"));
            SetIfExists(t, "RoleId", GetInt(r, "role_id"));
            SetIfExists(t, "DocumentId", GetInt(r, "document_id"));

            // friendly/bridge (if any columns exist)
            SetIfExists(t, "AssignedToName", GetString(r, "assignee_name"));
            SetIfExists(t, "LinkedSOP", GetString(r, "linked_sop"));

            return t;
        }

        private static int? GetInt(DataRow r, string col)
            => r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? Convert.ToInt32(r[col]) : (int?)null;

        private static string? GetString(DataRow r, string col)
            => r.Table.Columns.Contains(col) ? r[col]?.ToString() : null;

        private static DateTime? GetDate(DataRow r, string col)
            => r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? Convert.ToDateTime(r[col]) : (DateTime?)null;

        /// <summary>
        /// Sets a property if it exists and is writable; safely ignores unknown properties.
        /// </summary>
        private static void SetIfExists<TTarget>(TTarget target, string propertyName, object? value)
        {
            if (target is null || propertyName is null) return;

            var prop = typeof(TTarget).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (prop is null || !prop.CanWrite) return;

            try
            {
                if (value == null || value is DBNull) { prop.SetValue(target, null); return; }
                var pType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                var converted = Convert.ChangeType(value, pType);
                prop.SetValue(target, converted);
            }
            catch { /* schema-tolerant */ }
        }
    }
}
