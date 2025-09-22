using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace YasGMP.Services
{
    /// <summary>
    /// Evaluates attachment retention policies and enforces purge decisions while
    /// recording rich audit telemetry. Invoked by the background scheduler.
    /// </summary>
    public sealed class AttachmentRetentionEnforcer
    {
        private readonly DatabaseService _db;

        private const string SchedulerDevice = "scheduler";
        private const string SchedulerIp = "system";
        private const string ModuleName = "AttachmentRetention";

        private const string SqlDuePolicies = @"
SELECT rp.id AS policy_id,
       rp.attachment_id,
       rp.retain_until,
       rp.min_retain_days,
       rp.max_retain_days,
       rp.legal_hold,
       rp.delete_mode,
       rp.review_required,
       a.file_name,
       a.status,
       a.is_deleted,
       a.soft_deleted_at,
       a.uploaded_at,
       a.tenant_id,
       t.code AS tenant_code
FROM retention_policies rp
JOIN attachments a ON a.id = rp.attachment_id
LEFT JOIN tenants t ON t.id = a.tenant_id
WHERE (
        (rp.retain_until IS NOT NULL AND rp.retain_until <= UTC_TIMESTAMP())
        OR (rp.max_retain_days IS NOT NULL AND a.uploaded_at IS NOT NULL AND a.uploaded_at <= DATE_SUB(UTC_TIMESTAMP(), INTERVAL rp.max_retain_days DAY))
      )
  AND (rp.min_retain_days IS NULL OR a.uploaded_at IS NULL OR a.uploaded_at <= DATE_SUB(UTC_TIMESTAMP(), INTERVAL rp.min_retain_days DAY));";

        private const string SqlSoftDelete = @"
UPDATE attachments
   SET is_deleted = 1,
       soft_deleted_at = COALESCE(soft_deleted_at, UTC_TIMESTAMP()),
       status = 'soft-deleted'
 WHERE id = @id;";

        private const string SqlHardDelete = @"
UPDATE attachments
   SET is_deleted = 1,
       soft_deleted_at = UTC_TIMESTAMP(),
       status = 'purged',
       file_content = NULL
 WHERE id = @id;";

        private const string SqlDropLinks = "DELETE FROM attachment_links WHERE attachment_id = @id";

        public AttachmentRetentionEnforcer(DatabaseService db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<AttachmentRetentionEnforcerResult> RunOnceAsync(CancellationToken token = default)
        {
            var summary = new AttachmentRetentionEnforcerResult();
            DataTable table;
            try
            {
                table = await _db.ExecuteSelectAsync(SqlDuePolicies, null, token).ConfigureAwait(false);
            }
            catch
            {
                return summary;
            }

            foreach (DataRow row in table.Rows)
            {
                int policyId = ToInt(row, "policy_id");
                int attachmentId = ToInt(row, "attachment_id");
                string deleteMode = ToStr(row, "delete_mode");
                bool legalHold = ToBool(row, "legal_hold");
                bool reviewRequired = ToBool(row, "review_required");
                bool alreadyDeleted = ToBool(row, "is_deleted");
                string fileName = ToStr(row, "file_name");
                string tenantCode = ToStr(row, "tenant_code");
                DateTime? retainUntil = ToDate(row, "retain_until");

                if (legalHold)
                {
                    await EmitAuditAsync("ATTACHMENT_RETENTION_LEGAL_HOLD", policyId, attachmentId, fileName, tenantCode, retainUntil, token).ConfigureAwait(false);
                    summary.HoldNotices++;
                    continue;
                }

                if (reviewRequired)
                {
                    await EmitAuditAsync("ATTACHMENT_RETENTION_REVIEW", policyId, attachmentId, fileName, tenantCode, retainUntil, token).ConfigureAwait(false);
                    summary.ReviewNotices++;
                    continue;
                }

                if (alreadyDeleted)
                {
                    summary.AlreadyDeleted++;
                    continue;
                }

                if (string.Equals(deleteMode, "hard", StringComparison.OrdinalIgnoreCase))
                {
                    await HardDeleteAsync(policyId, attachmentId, fileName, tenantCode, token).ConfigureAwait(false);
                    summary.HardPurges++;
                }
                else
                {
                    await SoftDeleteAsync(policyId, attachmentId, fileName, tenantCode, token).ConfigureAwait(false);
                    summary.SoftDeletes++;
                }
            }

            return summary;
        }

        private async Task SoftDeleteAsync(int policyId, int attachmentId, string fileName, string tenantCode, CancellationToken token)
        {
            var parameters = new[] { new MySqlParameter("@id", attachmentId) };
            await _db.ExecuteNonQueryAsync(SqlSoftDelete, parameters, token).ConfigureAwait(false);
            await EmitAuditAsync("ATTACHMENT_RETENTION_SOFT_DELETE", policyId, attachmentId, fileName, tenantCode, null, token).ConfigureAwait(false);
        }

        private async Task HardDeleteAsync(int policyId, int attachmentId, string fileName, string tenantCode, CancellationToken token)
        {
            var parameters = new[] { new MySqlParameter("@id", attachmentId) };
            await _db.ExecuteNonQueryAsync(SqlHardDelete, parameters, token).ConfigureAwait(false);
            await _db.ExecuteNonQueryAsync(SqlDropLinks, parameters, token).ConfigureAwait(false);
            await EmitAuditAsync("ATTACHMENT_RETENTION_PURGE", policyId, attachmentId, fileName, tenantCode, null, token).ConfigureAwait(false);
        }

        private async Task EmitAuditAsync(string eventType, int policyId, int attachmentId, string fileName, string tenantCode, DateTime? retainUntil, CancellationToken token)
        {
            string details = $"policy={policyId}; name={fileName}; tenant={tenantCode}; retain_until={(retainUntil.HasValue ? retainUntil.Value.ToString("yyyy-MM-dd") : "-")}";
            await _db.LogSystemEventAsync(
                userId: null,
                eventType: eventType,
                tableName: "attachments",
                module: ModuleName,
                recordId: attachmentId,
                description: details,
                ip: SchedulerIp,
                severity: "info",
                deviceInfo: SchedulerDevice,
                sessionId: null,
                token: token).ConfigureAwait(false);
        }

        private static string ToStr(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? Convert.ToString(row[column]) ?? string.Empty : string.Empty;

        private static int ToInt(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? Convert.ToInt32(row[column]) : 0;

        private static bool ToBool(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value)
                return false;

            var value = row[column];
            return value switch
            {
                bool b => b,
                byte bt => bt != 0,
                sbyte sb => sb != 0,
                short s => s != 0,
                int i => i != 0,
                long l => l != 0,
                string str => str == "1" || str.Equals("true", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        private static DateTime? ToDate(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? Convert.ToDateTime(row[column]) : (DateTime?)null;
    }

    public sealed class AttachmentRetentionEnforcerResult
    {
        public int SoftDeletes { get; set; }
        public int HardPurges { get; set; }
        public int HoldNotices { get; set; }
        public int ReviewNotices { get; set; }
        public int AlreadyDeleted { get; set; }
    }
}
