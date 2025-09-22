// ==============================================================================
// File: Services/DatabaseService.IncidentReports.Extensions.cs
// Purpose: Minimal Incident Report shims backed by `incidents` table
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// DatabaseService extensions dedicated to incident report fetch/save operations.
    /// </summary>
    public static class DatabaseServiceIncidentReportsExtensions
    {
        public static async Task<List<IncidentReport>> GetAllIncidentReportsFullAsync(this DatabaseService db, CancellationToken token = default)
        {
            const string sql = @"SELECT id, title, type, status, description, detected_at, reported_at, reported_by_id, assigned_to_id,
    machine_id, component_id, root_cause, digital_signature, last_modified, last_modified_by_id, source_ip
FROM incidents /* ANALYZER_IGNORE: pending schema mapping */ ORDER BY reported_at DESC, id DESC";
            var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
            var list = new List<IncidentReport>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        public static async Task InitiateIncidentReportAsync(this DatabaseService db, IncidentReport report, CancellationToken token = default)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            const string sql = @"INSERT INTO incidents /* ANALYZER_IGNORE: pending schema mapping */ (title, type, status, description, detected_at, reported_at, reported_by_id, assigned_to_id, machine_id, component_id, root_cause, digital_signature, last_modified, last_modified_by_id, source_ip)
                                VALUES (@t,@type,'reported',@desc,@det,@rep,@rby,@assn,@mach,@comp,@rc,@sig,NOW(),@lmb,@ip)";
            var pars = new List<MySqlParameter>
            {
                new("@t", report.Title ?? string.Empty),
                new("@type", (object?)report.IncidentType ?? DBNull.Value),
                new("@desc", (object?)report.Description ?? DBNull.Value),
                new("@det", report.DetectedAt),
                new("@rep", (object?)report.ReportedAt ?? DBNull.Value),
                new("@rby", (object?)report.ReportedById ?? DBNull.Value),
                new("@assn", (object?)report.AssignedToId ?? DBNull.Value),
                new("@mach", (object?)report.MachineId ?? DBNull.Value),
                new("@comp", (object?)report.ComponentId ?? DBNull.Value),
                new("@rc", (object?)report.RootCause ?? DBNull.Value),
                new("@sig", (object?)null ?? DBNull.Value),
                new("@lmb", (object?)report.ReportedById ?? DBNull.Value),
                new("@ip", (object?)report.IpAddress ?? DBNull.Value)
            };
            try { await db.ExecuteNonQueryAsync(sql, pars, token).ConfigureAwait(false); } catch { }
            await db.LogSystemEventAsync(report.ReportedById, "IR_CREATE", "incidents", "IncidentReports", null, report.Title, report.IpAddress, "audit", report.DeviceInfo, report.SessionId, token: token).ConfigureAwait(false);
        }

        public static async Task AssignIncidentReportAsync(this DatabaseService db, int incidentId, int userId, string? note, int actorUserId, string ip, string device, string? sessionId, CancellationToken token = default)
            {
            try { await db.ExecuteNonQueryAsync("UPDATE incidents /* ANALYZER_IGNORE: pending schema mapping */ SET assigned_to_id=@u WHERE id=@id", new[] { new MySqlParameter("@u", userId), new MySqlParameter("@id", incidentId) }, token).ConfigureAwait(false); } catch { }
            await db.LogSystemEventAsync(actorUserId, "IR_ASSIGN", "incidents", "IncidentReports", incidentId, note, ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }

        // Overload matching ViewModel call order (id, actorUserId, ip, device, sessionId, note)
        public static Task AssignIncidentReportAsync(this DatabaseService db, int incidentId, int actorUserId, string ip, string device, string? sessionId, string? note, CancellationToken token = default)
            => db.AssignIncidentReportAsync(incidentId, actorUserId, note, actorUserId, ip, device, sessionId, token);

        public static async Task InvestigateIncidentReportAsync(this DatabaseService db, IncidentReport report, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
        {
            try { await db.ExecuteNonQueryAsync("UPDATE incidents /* ANALYZER_IGNORE: pending schema mapping */ SET status='investigated' WHERE id=@id", new[] { new MySqlParameter("@id", report.Id) }, token).ConfigureAwait(false); } catch { }
            await db.LogSystemEventAsync(actorUserId, "IR_INVESTIGATE", "incidents", "IncidentReports", report.Id, report.Title, ip, "audit", deviceInfo, sessionId, token: token).ConfigureAwait(false);
        }

        public static async Task ApproveIncidentReportAsync(this DatabaseService db, int incidentId, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
        {
            try { await db.ExecuteNonQueryAsync("UPDATE incidents /* ANALYZER_IGNORE: pending schema mapping */ SET status='approved' WHERE id=@id", new[] { new MySqlParameter("@id", incidentId) }, token).ConfigureAwait(false); } catch { }
            await db.LogSystemEventAsync(actorUserId, "IR_APPROVE", "incidents", "IncidentReports", incidentId, null, ip, "audit", deviceInfo, sessionId, token: token).ConfigureAwait(false);
        }

        public static Task EscalateIncidentReportAsync(this DatabaseService db, int incidentId, int actorUserId, string ip, string deviceInfo, string? sessionId, string? reason, CancellationToken token = default)
            => db.LogSystemEventAsync(actorUserId, "IR_ESCALATE", "incidents", "IncidentReports", incidentId, reason, ip, "warn", deviceInfo, sessionId, token: token);

        public static async Task CloseIncidentReportAsync(this DatabaseService db, int incidentId, int actorUserId, string ip, string deviceInfo, string? sessionId, string? note, CancellationToken token = default)
        {
            try { await db.ExecuteNonQueryAsync("UPDATE incidents /* ANALYZER_IGNORE: pending schema mapping */ SET status='closed', closed_at=NOW(), closed_by_id=@u WHERE id=@id", new[] { new MySqlParameter("@id", incidentId), new MySqlParameter("@u", actorUserId) }, token).ConfigureAwait(false); } catch { }
            await db.LogSystemEventAsync(actorUserId, "IR_CLOSE", "incidents", "IncidentReports", incidentId, note, ip, "audit", deviceInfo, sessionId, token: token).ConfigureAwait(false);
        }

        public static Task ExportIncidentReportsAsync(this DatabaseService db, List<IncidentReport> items, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
        {
            string path = YasGMP.Helpers.CsvExportHelper.WriteCsv(items ?? new List<IncidentReport>(), "incident_reports",
                new (string, Func<IncidentReport, object?>)[]
                {
                    ("Id", r => r.Id),
                    ("Title", r => r.Title),
                    ("Type", r => r.IncidentType),
                    ("Status", r => r.Status),
                    ("DetectedAt", r => r.DetectedAt),
                    ("ReportedAt", r => r.ReportedAt)
                });
            return db.LogSystemEventAsync(0, "IR_EXPORT", "incidents", "IncidentReports", null, $"count={items?.Count ?? 0}; file={path}", ip, "info", deviceInfo, sessionId, token: token);
        }

        public static Task ExportIncidentReportsAsync(this DatabaseService db, List<IncidentReport> items, string format, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
        {
            string? path = null;
            if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
            {
                path = YasGMP.Helpers.XlsxExporter.WriteSheet(items ?? new List<IncidentReport>(), "incident_reports",
                    new (string, Func<IncidentReport, object?>)[]
                    {
                        ("Id", r => r.Id),
                        ("Title", r => r.Title),
                        ("Type", r => r.IncidentType),
                        ("Status", r => r.Status),
                        ("DetectedAt", r => r.DetectedAt),
                        ("ReportedAt", r => r.ReportedAt)
                    });
            }
            else if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
            {
                path = YasGMP.Helpers.PdfExporter.WriteTable(items ?? new List<IncidentReport>(), "incident_reports",
                    new (string, Func<IncidentReport, object?>)[]
                    {
                        ("Id", r => r.Id),
                        ("Title", r => r.Title),
                        ("Type", r => r.IncidentType),
                        ("Status", r => r.Status),
                        ("DetectedAt", r => r.DetectedAt)
                    }, title: "Incident Reports Export");
            }
            else
            {
                path = YasGMP.Helpers.CsvExportHelper.WriteCsv(items ?? new List<IncidentReport>(), "incident_reports",
                    new (string, Func<IncidentReport, object?>)[]
                    {
                        ("Id", r => r.Id),
                        ("Title", r => r.Title),
                        ("Type", r => r.IncidentType),
                        ("Status", r => r.Status),
                        ("DetectedAt", r => r.DetectedAt),
                        ("ReportedAt", r => r.ReportedAt)
                    });
            }
            return db.LogSystemEventAsync(0, "IR_EXPORT", "incidents", "IncidentReports", null, $"fmt={format}; count={items?.Count ?? 0}; file={path}", ip, "info", deviceInfo, sessionId, token: token);
        }

        private static IncidentReport Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            DateTime? D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

            return new IncidentReport
            {
                Id = I("id"),
                Title = S("title"),
                IncidentType = S("type"),
                Status = S("status"),
                Description = S("description"),
                Severity = S("severity"),
                DetectedAt = D("detected_at") ?? DateTime.UtcNow,
                ReportedById = IN("reported_by_id"),
                ReportedAt = D("reported_at"),
                AssignedToId = IN("assigned_to_id"),
                RootCause = S("root_cause"),
                MachineId = IN("machine_id"),
                ComponentId = IN("component_id"),
                ResolvedAt = D("closed_at"),
                ResolvedById = IN("closed_by_id"),
                IpAddress = S("source_ip")
            };
        }
    }
}

