// ==============================================================================
// File: Services/DatabaseService.Scheduler.Extensions.cs
// Purpose: Minimal scheduler ops (ack/execute/export + audit) used by SchedulerViewModel
// ==============================================================================

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// DatabaseService extensions for scheduled job definitions and execution logs.
    /// </summary>
    public static class DatabaseServiceSchedulerExtensions
    {
        public static async Task<List<ScheduledJob>> GetAllScheduledJobsFullAsync(this DatabaseService db, CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync("SELECT id, name, job_type, status, next_due, recurrence_pattern, entity_type, entity_id, created_by, created_at, last_modified_at, device_info, session_id, ip_address, digital_signature, escalation_note, comment FROM scheduled_jobs ORDER BY next_due, id", null, token).ConfigureAwait(false);
            var list = new List<ScheduledJob>(dt.Rows.Count);
            foreach (System.Data.DataRow r in dt.Rows)
            {
                string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
                int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != System.DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
                System.DateTime D(string c) => r.Table.Columns.Contains(c) && r[c] != System.DBNull.Value ? Convert.ToDateTime(r[c]) : System.DateTime.UtcNow;

                list.Add(new ScheduledJob
                {
                    Id = Convert.ToInt32(r["id"]),
                    Name = S("name"),
                    JobType = S("job_type"),
                    Status = S("status"),
                    NextDue = D("next_due"),
                    RecurrencePattern = S("recurrence_pattern"),
                    EntityType = S("entity_type"),
                    EntityId = IN("entity_id"),
                    CreatedBy = S("created_by"),
                    CreatedAt = D("created_at"),
                    LastModifiedAt = r.Table.Columns.Contains("last_modified_at") && r["last_modified_at"] != System.DBNull.Value ? Convert.ToDateTime(r["last_modified_at"]) : null,
                    DeviceInfo = S("device_info"),
                    SessionId = S("session_id"),
                    IpAddress = S("ip_address"),
                    DigitalSignature = S("digital_signature"),
                    EscalationNote = S("escalation_note"),
                    Comment = S("comment")
                });
            }
            return list;
        }
        public static Task AddScheduledJobAsync(this DatabaseService db, ScheduledJob job, CancellationToken token = default)
            => db.LogSystemEventAsync(0, "SCHED_CREATE", "scheduled_jobs", "SchedulerModule", null, job?.Name, job?.IpAddress, "audit", job?.DeviceInfo, job?.SessionId, token: token);

        public static Task UpdateScheduledJobAsync(this DatabaseService db, ScheduledJob job, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "SCHED_UPDATE", "scheduled_jobs", "SchedulerModule", job?.Id, job?.Name, ip, "audit", deviceInfo, sessionId, token: token);

        public static Task DeleteScheduledJobAsync(this DatabaseService db, int jobId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.LogSystemEventAsync(0, "SCHED_DELETE", "scheduled_jobs", "SchedulerModule", jobId, null, ip, "audit", deviceInfo, sessionId, token: token);
        public static Task AcknowledgeScheduledJobAsync(this DatabaseService db, int jobId, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "SCHED_ACK", "scheduled_jobs", "SchedulerModule", jobId, null, ip, "audit", deviceInfo, sessionId, token: token);

        public static Task ExecuteScheduledJobAsync(this DatabaseService db, int jobId, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "SCHED_EXECUTE", "scheduled_jobs", "SchedulerModule", jobId, null, ip, "audit", deviceInfo, sessionId, token: token);

        public static Task ExportScheduledJobsAsync(this DatabaseService db, List<ScheduledJob> items, string ip, string deviceInfo, string? sessionId, string format = "csv", CancellationToken token = default)
        {
            string? path = null;
            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                path = YasGMP.Helpers.CsvExportHelper.WriteCsv(items ?? new List<ScheduledJob>(), "scheduled_jobs",
                    new (string, Func<ScheduledJob, object?>)[]
                    {
                        ("Id", j => j.Id),
                        ("Name", j => j.Name),
                        ("JobType", j => j.JobType),
                        ("Status", j => j.Status),
                        ("NextDue", j => j.NextDue),
                        ("EntityType", j => j.EntityType),
                        ("EntityId", j => j.EntityId)
                    });
            }
            else if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
            {
                path = YasGMP.Helpers.XlsxExporter.WriteSheet(items ?? new List<ScheduledJob>(), "scheduled_jobs",
                    new (string, Func<ScheduledJob, object?>)[]
                    {
                        ("Id", j => j.Id),
                        ("Name", j => j.Name),
                        ("JobType", j => j.JobType),
                        ("Status", j => j.Status),
                        ("NextDue", j => j.NextDue),
                        ("EntityType", j => j.EntityType),
                        ("EntityId", j => j.EntityId)
                    });
            }
            else if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
            {
                path = YasGMP.Helpers.PdfExporter.WriteTable(items ?? new List<ScheduledJob>(), "scheduled_jobs",
                    new (string, Func<ScheduledJob, object?>)[]
                    {
                        ("Id", j => j.Id),
                        ("Name", j => j.Name),
                        ("JobType", j => j.JobType),
                        ("Status", j => j.Status),
                        ("NextDue", j => j.NextDue)
                    }, title: "Scheduled Jobs Export");
            }
            return db.LogSystemEventAsync(0, "SCHED_EXPORT", "scheduled_jobs", "SchedulerModule", null, $"count={items?.Count ?? 0}; fmt={format}; file={path}", ip, "info", deviceInfo, sessionId, token: token);
        }

        public static Task LogScheduledJobAuditAsync(this DatabaseService db, ScheduledJob? job, string action, string ip, string deviceInfo, string? sessionId, string? details, CancellationToken token = default)
            => db.LogSystemEventAsync(0, $"SCHED_{action}", "scheduled_jobs", "SchedulerModule", job?.Id, details, ip, "audit", deviceInfo, sessionId, token: token);
    }
}


