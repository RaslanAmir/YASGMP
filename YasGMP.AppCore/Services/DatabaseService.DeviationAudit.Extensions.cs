// ==============================================================================
// File: Services/DatabaseService.DeviationAudit.Extensions.cs
// Purpose: Minimal schema-tolerant Deviation Audit CRUD + queries + export shim
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// DatabaseService extensions that read and write deviation audit trails.
    /// </summary>
    public static class DatabaseServiceDeviationAuditExtensions
    {
        // Create
        public static async Task<int> InsertDeviationAuditAsync(this DatabaseService db, DeviationAudit a, CancellationToken token = default)
        {
            const string sql = @"INSERT INTO deviation_audit (deviation_id, user_id, action, details, changed_at, device_info, source_ip, session_id, digital_signature, regulatory_status, ai_anomaly_score, validated, comment, old_value, new_value, signature_type, signature_valid, export_status, export_time, exported_by, restored_from_snapshot, restoration_reference, approval_status, approval_time, approved_by, deleted, deleted_at, deleted_by, created_at, updated_at, related_file, related_photo, iot_event_id)
                                 VALUES (@dev,@uid,@act,@det,@chg,@devinfo,@ip,@sid,@sig,@reg,@ais,@val,@com,@old,@new,@sigtype,@sigok,@expst,@exptm,@expby,@restored,@restref,@apprst,@apprtm,@apprby,@del,@delat,@delby,NOW(),NOW(),@rfile,@rphoto,@iot)";
            var pars = BuildParams(a);
            await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);
            var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
            a.Id = Convert.ToInt32(idObj);
            // Also mirror into system_event_log for unified audit if desired
            await db.LogSystemEventAsync(a.UserId, $"DEV_{a.Action}", "deviation_audit", "DeviationAuditPage", a.Id, a.Details, a.SourceIp, "audit", a.DeviceInfo, a.SessionId, token: token).ConfigureAwait(false);
            return a.Id;
        }

        // Update
        public static async Task UpdateDeviationAuditAsync(this DatabaseService db, DeviationAudit a, CancellationToken token = default)
        {
            const string sql = @"UPDATE deviation_audit SET deviation_id=@dev, user_id=@uid, action=@act, details=@det, changed_at=@chg, device_info=@devinfo, source_ip=@ip, session_id=@sid, digital_signature=@sig, regulatory_status=@reg, ai_anomaly_score=@ais, validated=@val, comment=@com, old_value=@old, new_value=@new, signature_type=@sigtype, signature_valid=@sigok, export_status=@expst, export_time=@exptm, exported_by=@expby, restored_from_snapshot=@restored, restoration_reference=@restref, approval_status=@apprst, approval_time=@apprtm, approved_by=@apprby, deleted=@del, deleted_at=@delat, deleted_by=@delby, updated_at=NOW(), related_file=@rfile, related_photo=@rphoto, iot_event_id=@iot WHERE id=@id";
            var pars = BuildParams(a);
            pars.Add(new MySqlParameter("@id", a.Id));
            await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false);
            await db.LogSystemEventAsync(a.UserId, $"DEV_{a.Action}_UPDATE", "deviation_audit", "DeviationAuditPage", a.Id, a.Details, a.SourceIp, "audit", a.DeviceInfo, a.SessionId, token: token).ConfigureAwait(false);
        }

        // Delete
        public static async Task DeleteDeviationAuditAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync("DELETE FROM deviation_audit WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            await db.LogSystemEventAsync(0, "DEV_DELETE", "deviation_audit", "DeviationAuditPage", id, null, null, "audit", null, null, token: token).ConfigureAwait(false);
        }

        // Get by id
        public static async Task<DeviationAudit?> GetDeviationAuditByIdAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync(
                "SELECT id, deviation_id, user_id, action, details, changed_at, device_info, source_ip, session_id, digital_signature, regulatory_status, ai_anomaly_score, validated, comment, old_value, new_value, signature_type, signature_valid, export_status, export_time, exported_by, restored_from_snapshot, restoration_reference, approval_status, approval_time, approved_by, deleted, deleted_at, deleted_by, created_at, updated_at, related_file, related_photo, iot_event_id FROM deviation_audit WHERE id=@id LIMIT 1",
                new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            return dt.Rows.Count == 1 ? Map(dt.Rows[0]) : null;
        }

        // Queries
        public static async Task<List<DeviationAudit>> GetDeviationAuditsByDeviationIdAsync(this DatabaseService db, int deviationId, CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync(
                "SELECT id, deviation_id, user_id, action, details, changed_at, device_info, source_ip, session_id, digital_signature, regulatory_status, ai_anomaly_score, validated, comment, old_value, new_value, signature_type, signature_valid, export_status, export_time, exported_by, restored_from_snapshot, restoration_reference, approval_status, approval_time, approved_by, deleted, deleted_at, deleted_by, created_at, updated_at, related_file, related_photo, iot_event_id FROM deviation_audit WHERE deviation_id=@id ORDER BY changed_at DESC, id DESC",
                new[] { new MySqlParameter("@id", deviationId) }, token).ConfigureAwait(false);
            return MapMany(dt);
        }

        public static async Task<List<DeviationAudit>> GetDeviationAuditsByUserIdAsync(this DatabaseService db, int userId, CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync(
                "SELECT id, deviation_id, user_id, action, details, changed_at, device_info, source_ip, session_id, digital_signature, regulatory_status, ai_anomaly_score, validated, comment, old_value, new_value, signature_type, signature_valid, export_status, export_time, exported_by, restored_from_snapshot, restoration_reference, approval_status, approval_time, approved_by, deleted, deleted_at, deleted_by, created_at, updated_at, related_file, related_photo, iot_event_id FROM deviation_audit WHERE user_id=@id ORDER BY changed_at DESC, id DESC",
                new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);
            return MapMany(dt);
        }

        public static async Task<List<DeviationAudit>> GetDeviationAuditsByActionTypeAsync(this DatabaseService db, string action, CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync(
                "SELECT id, deviation_id, user_id, action, details, changed_at, device_info, source_ip, session_id, digital_signature, regulatory_status, ai_anomaly_score, validated, comment, old_value, new_value, signature_type, signature_valid, export_status, export_time, exported_by, restored_from_snapshot, restoration_reference, approval_status, approval_time, approved_by, deleted, deleted_at, deleted_by, created_at, updated_at, related_file, related_photo, iot_event_id FROM deviation_audit WHERE action=@a ORDER BY changed_at DESC, id DESC",
                new[] { new MySqlParameter("@a", action) }, token).ConfigureAwait(false);
            return MapMany(dt);
        }

        public static async Task<List<DeviationAudit>> GetDeviationAuditsByDateRangeAsync(this DatabaseService db, DateTime from, DateTime to, CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync(
                "SELECT id, deviation_id, user_id, action, details, changed_at, device_info, source_ip, session_id, digital_signature, regulatory_status, ai_anomaly_score, validated, comment, old_value, new_value, signature_type, signature_valid, export_status, export_time, exported_by, restored_from_snapshot, restoration_reference, approval_status, approval_time, approved_by, deleted, deleted_at, deleted_by, created_at, updated_at, related_file, related_photo, iot_event_id FROM deviation_audit WHERE changed_at BETWEEN @f AND @t ORDER BY changed_at DESC, id DESC",
                new[] { new MySqlParameter("@f", from), new MySqlParameter("@t", to) }, token).ConfigureAwait(false);
            return MapMany(dt);
        }

        // Export shim: creates a lightweight CSV and logs the event. Returns file path.
        public static async Task<string> ExportDeviationAuditLogsAsync(this DatabaseService db, IReadOnlyList<DeviationAudit> audits, string format = "csv", CancellationToken token = default)
        {
            format = string.IsNullOrWhiteSpace(format) ? "csv" : format.ToLowerInvariant();
            var fileName = $"deviation_audit_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{format}";
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YASGMP", "exports");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, fileName);

            // Only CSV supported in stub
            var sb = new StringBuilder();
            sb.AppendLine("id;deviation_id;user_id;action;changed_at;ip;device;details");
            foreach (var a in audits ?? Array.Empty<DeviationAudit>())
                sb.AppendLine(string.Join(';', new[]
                {
                    a.Id.ToString(), a.DeviationId.ToString(), a.UserId.ToString(), a.Action ?? string.Empty,
                    (a.ChangedAt?.ToString("O") ?? string.Empty), a.SourceIp ?? string.Empty, a.DeviceInfo ?? string.Empty,
                    (a.Details ?? string.Empty).Replace('\n', ' ').Replace('\r', ' ')
                }));
            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8, token).ConfigureAwait(false);

            await db.LogSystemEventAsync(0, "DEV_EXPORT", "deviation_audit", "DeviationAuditPage", null, $"count={audits?.Count ?? 0}; file={path}", null, "info", null, null, token: token).ConfigureAwait(false);
            return path;
        }

        // Helpers
        private static List<MySqlParameter> BuildParams(DeviationAudit a)
        {
            object? NV(object? v) => v ?? DBNull.Value;
            return new()
            {
                new("@dev", a.DeviationId),
                new("@uid", a.UserId),
                new("@act", a.Action ?? string.Empty),
                new("@det", NV(a.Details)),
                new("@chg", NV(a.ChangedAt)),
                new("@devinfo", NV(a.DeviceInfo)),
                new("@ip", NV(a.SourceIp)),
                new("@sid", NV(a.SessionId)),
                new("@sig", NV(a.DigitalSignature)),
                new("@reg", NV(a.RegulatoryStatusRaw)),
                new("@ais", NV(a.AiAnomalyScore)),
                new("@val", NV(a.Validated)),
                new("@com", NV(a.Comment)),
                new("@old", NV(a.OldValue)),
                new("@new", NV(a.NewValue)),
                new("@sigtype", NV(a.SignatureTypeRaw)),
                new("@sigok", NV(a.SignatureValid)),
                new("@expst", NV(a.ExportStatusRaw)),
                new("@exptm", NV(a.ExportTime)),
                new("@expby", NV(a.ExportedBy)),
                new("@restored", NV(a.RestoredFromSnapshot)),
                new("@restref", NV(a.RestorationReference)),
                new("@apprst", NV(a.ApprovalStatusRaw)),
                new("@apprtm", NV(a.ApprovalTime)),
                new("@apprby", NV(a.ApprovedBy)),
                new("@del", NV(a.Deleted)),
                new("@delat", NV(a.DeletedAt)),
                new("@delby", NV(a.DeletedBy)),
                new("@rfile", NV(a.RelatedFile)),
                new("@rphoto", NV(a.RelatedPhoto)),
                new("@iot", NV(a.IotEventId))
            };
        }

        private static List<DeviationAudit> MapMany(DataTable dt)
        {
            var list = new List<DeviationAudit>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        private static DeviationAudit Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            decimal? DEC(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDecimal(r[c]) : (decimal?)null;
            bool? B(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToBoolean(r[c]) : (bool?)null;
            DateTime? D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

            return new DeviationAudit
            {
                Id = I("id"),
                DeviationId = I("deviation_id"),
                UserId = I("user_id"),
                Action = S("action"),
                Details = S("details"),
                ChangedAt = D("changed_at"),
                DeviceInfo = S("device_info"),
                SourceIp = S("source_ip"),
                SessionId = S("session_id"),
                DigitalSignature = S("digital_signature"),
                RegulatoryStatusRaw = S("regulatory_status"),
                AiAnomalyScore = DEC("ai_anomaly_score"),
                Validated = B("validated"),
                Comment = S("comment"),
                OldValue = S("old_value"),
                NewValue = S("new_value"),
                SignatureTypeRaw = S("signature_type"),
                SignatureValid = B("signature_valid"),
                ExportStatusRaw = S("export_status"),
                ExportTime = D("export_time"),
                ExportedBy = IN("exported_by"),
                RestoredFromSnapshot = B("restored_from_snapshot"),
                RestorationReference = S("restoration_reference"),
                ApprovalStatusRaw = S("approval_status"),
                ApprovalTime = D("approval_time"),
                ApprovedBy = IN("approved_by"),
                Deleted = B("deleted"),
                DeletedAt = D("deleted_at"),
                DeletedBy = IN("deleted_by"),
                CreatedAt = D("created_at"),
                UpdatedAt = D("updated_at"),
                RelatedFile = S("related_file"),
                RelatedPhoto = S("related_photo"),
                IotEventId = IN("iot_event_id")
            };
        }
    }
}
