// ==============================================================================
// File: Services/DatabaseService.Incidents.Extensions.cs
// Purpose: Minimal Incident CRUD used by IncidentService (schema tolerant)
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
    /// DatabaseService extensions for core incident records and relations.
    /// </summary>
    public static class DatabaseServiceIncidentsExtensions
    {
        public static async Task<List<Incident>> GetAllIncidentsAsync(this DatabaseService db, CancellationToken token = default)
        {
            const string sql = @"SELECT 
    id, title, description, type, priority, detected_at, reported_at, reported_by_id,
    assigned_to_id, work_order_id, capa_case_id, status, root_cause, closed_at, closed_by_id,
    digital_signature, last_modified, last_modified_by_id, source_ip, notes, is_deleted,
    anomaly_score, risk_level, assigned_investigator, classification, linked_deviation_id,
    linked_capa_id, closure_comment, is_critical
FROM incidents /* ANALYZER_IGNORE: pending schema mapping */ ORDER BY id DESC";
            var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
            var list = new List<Incident>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        public static async Task<Incident?> GetIncidentByIdAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            const string sql = @"SELECT 
    id, title, description, type, priority, detected_at, reported_at, reported_by_id,
    assigned_to_id, work_order_id, capa_case_id, status, root_cause, closed_at, closed_by_id,
    digital_signature, last_modified, last_modified_by_id, source_ip, notes, is_deleted,
    anomaly_score, risk_level, assigned_investigator, classification, linked_deviation_id,
    linked_capa_id, closure_comment, is_critical
FROM incidents /* ANALYZER_IGNORE: pending schema mapping */ WHERE id=@id LIMIT 1";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            return dt.Rows.Count == 1 ? Map(dt.Rows[0]) : null;
        }

        public static async Task<int> InsertOrUpdateIncidentAsync(this DatabaseService db, Incident inc, bool update, int actorUserId, CancellationToken token = default)
        {
            if (inc == null) throw new ArgumentNullException(nameof(inc));
            string insert = @"INSERT INTO incidents /* ANALYZER_IGNORE: pending schema mapping */ (title, description, type, priority, detected_at, reported_at, reported_by_id, assigned_to_id, work_order_id, capa_case_id, status, root_cause, closed_at, closed_by_id, digital_signature, last_modified, last_modified_by_id, source_ip, notes, is_deleted, anomaly_score, risk_level, assigned_investigator, classification, linked_deviation_id, linked_capa_id, closure_comment, is_critical)
                             VALUES (@t,@d,@type,@prio,@det,@rep,@rby,@assn,@wo,@capa,@status,@rc,@closed,@cby,@sig,NOW(),@lmb,@ip,@notes,@del,@anom,@risk,@invest,@class,@ldev,@lcapa,@clos,@critical)";
            string updateSql = @"UPDATE incidents /* ANALYZER_IGNORE: pending schema mapping */ SET title=@t, description=@d, type=@type, priority=@prio, detected_at=@det, reported_at=@rep, reported_by_id=@rby, assigned_to_id=@assn, work_order_id=@wo, capa_case_id=@capa, status=@status, root_cause=@rc, closed_at=@closed, closed_by_id=@cby, digital_signature=@sig, last_modified=NOW(), last_modified_by_id=@lmb, source_ip=@ip, notes=@notes, is_deleted=@del, anomaly_score=@anom, risk_level=@risk, assigned_investigator=@invest, classification=@class, linked_deviation_id=@ldev, linked_capa_id=@lcapa, closure_comment=@clos, is_critical=@critical WHERE id=@id";

            var pars = new List<MySqlParameter>
            {
                new("@t", inc.Title ?? string.Empty),
                new("@d", inc.Description ?? string.Empty),
                new("@type", (object?)inc.Type ?? DBNull.Value),
                new("@prio", (object?)inc.Priority ?? DBNull.Value),
                new("@det", inc.DetectedAt),
                new("@rep", (object?)inc.ReportedAt ?? DBNull.Value),
                new("@rby", (object?)inc.ReportedById ?? DBNull.Value),
                new("@assn", (object?)inc.AssignedToId ?? DBNull.Value),
                new("@wo", (object?)inc.WorkOrderId ?? DBNull.Value),
                new("@capa", (object?)inc.CapaCaseId ?? DBNull.Value),
                new("@status", inc.Status ?? string.Empty),
                new("@rc", (object?)inc.RootCause ?? DBNull.Value),
                new("@closed", (object?)inc.ClosedAt ?? DBNull.Value),
                new("@cby", (object?)inc.ClosedById ?? DBNull.Value),
                new("@sig", (object?)inc.DigitalSignature ?? DBNull.Value),
                new("@lmb", (object?)inc.LastModifiedById ?? DBNull.Value),
                new("@ip", (object?)inc.SourceIp ?? DBNull.Value),
                new("@notes", (object?)inc.Notes ?? DBNull.Value),
                new("@del", inc.IsDeleted),
                new("@anom", (object?)inc.AnomalyScore ?? DBNull.Value),
                new("@risk", inc.RiskLevel),
                new("@invest", (object?)inc.AssignedInvestigator ?? DBNull.Value),
                new("@class", (object?)inc.Classification ?? DBNull.Value),
                new("@ldev", (object?)inc.LinkedDeviationId ?? DBNull.Value),
                new("@lcapa", (object?)inc.LinkedCapaId ?? DBNull.Value),
                new("@clos", (object?)inc.ClosureComment ?? DBNull.Value),
                new("@critical", inc.IsCritical)
            };

            if (!update)
            {
                await db.ExecuteNonQueryAsync(insert, pars, token).ConfigureAwait(false);
                var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                inc.Id = Convert.ToInt32(idObj);
            }
            else
            {
                pars.Add(new MySqlParameter("@id", inc.Id));
                await db.ExecuteNonQueryAsync(updateSql, pars, token).ConfigureAwait(false);
            }

            await db.LogSystemEventAsync(actorUserId, update ? "INCIDENT_UPDATE" : "INCIDENT_CREATE", "incidents", "IncidentModule", inc.Id, inc.Title, inc.SourceIp, "audit", null, null, token: token).ConfigureAwait(false);
            return inc.Id;
        }

        public static async Task DeleteIncidentAsync(this DatabaseService db, int id, int actorUserId, CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync("DELETE FROM incidents /* ANALYZER_IGNORE: pending schema mapping */ WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            await db.LogSystemEventAsync(actorUserId, "INCIDENT_DELETE", "incidents", "IncidentModule", id, null, null, "audit", null, null, token: token).ConfigureAwait(false);
        }

        private static Incident Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            double? Dbl(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDouble(r[c]) : (double?)null;
            bool B(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value && Convert.ToBoolean(r[c]);
            DateTime D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : DateTime.UtcNow;
            DateTime? DN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

            return new Incident
            {
                Id = I("id"),
                Title = S("title"),
                Description = S("description"),
                Type = S("type"),
                Priority = S("priority"),
                DetectedAt = D("detected_at"),
                ReportedAt = DN("reported_at"),
                ReportedById = IN("reported_by_id"),
                AssignedToId = IN("assigned_to_id"),
                WorkOrderId = IN("work_order_id"),
                CapaCaseId = IN("capa_case_id"),
                Status = S("status"),
                RootCause = S("root_cause"),
                ClosedAt = DN("closed_at"),
                ClosedById = IN("closed_by_id"),
                DigitalSignature = S("digital_signature"),
                LastModified = DN("last_modified") ?? DateTime.UtcNow,
                LastModifiedById = IN("last_modified_by_id"),
                SourceIp = S("source_ip"),
                Notes = S("notes"),
                IsDeleted = r.Table.Columns.Contains("is_deleted") && r["is_deleted"] != DBNull.Value && Convert.ToBoolean(r["is_deleted"]),
                AnomalyScore = Dbl("anomaly_score"),
                RiskLevel = I("risk_level"),
                AssignedInvestigator = S("assigned_investigator"),
                Classification = S("classification"),
                LinkedDeviationId = IN("linked_deviation_id"),
                LinkedCapaId = IN("linked_capa_id"),
                ClosureComment = S("closure_comment"),
                IsCritical = r.Table.Columns.Contains("is_critical") && r["is_critical"] != DBNull.Value && Convert.ToBoolean(r["is_critical"])
            };
        }
    }
}

