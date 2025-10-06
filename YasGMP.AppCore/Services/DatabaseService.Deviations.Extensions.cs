// ==============================================================================
// File: Services/DatabaseService.Deviations.Extensions.cs
// Purpose: Minimal Deviation CRUD used by DeviationService (schema tolerant)
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
    /// DatabaseService extensions for deviation entities and workflows.
    /// </summary>
    public static class DatabaseServiceDeviationsExtensions
    {
        public static async Task<List<Deviation>> GetAllDeviationsAsync(this DatabaseService db, CancellationToken token = default)
        {
            const string sql = @"SELECT 
    id, code, title, description, reported_at, reported_by_id, severity, is_critical, status,
    assigned_investigator_id, assigned_investigator_name, investigation_started_at, root_cause,
    linked_capa_id, closure_comment, closed_at, digital_signature, risk_score, anomaly_score,
    last_modified, last_modified_by_id, source_ip, audit_note
FROM deviations ORDER BY id DESC";
            var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
            var list = new List<Deviation>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        public static async Task<Deviation?> GetDeviationByIdAsync(this DatabaseService db, int id, CancellationToken token = default)
        {
            const string sql = @"SELECT 
    id, code, title, description, reported_at, reported_by_id, severity, is_critical, status,
    assigned_investigator_id, assigned_investigator_name, investigation_started_at, root_cause,
    linked_capa_id, closure_comment, closed_at, digital_signature, risk_score, anomaly_score,
    last_modified, last_modified_by_id, source_ip, audit_note
FROM deviations WHERE id=@id LIMIT 1";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            return dt.Rows.Count == 1 ? Map(dt.Rows[0]) : null;
        }

        public static async Task<int> InsertOrUpdateDeviationAsync(this DatabaseService db, Deviation d, bool update, int actorUserId, string? ip = null, string? device = null, string? sessionId = null, CancellationToken token = default)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));
            string insert = @"INSERT INTO deviations (code, title, description, reported_at, reported_by_id, severity, is_critical, status, assigned_investigator_id, assigned_investigator_name, investigation_started_at, root_cause, linked_capa_id, closure_comment, closed_at, digital_signature, risk_score, anomaly_score, last_modified, last_modified_by_id, source_ip, audit_note)
                             VALUES (@code,@title,@desc,@rep,@rby,@sev,@crit,@status,@assid,@assname,@invstart,@rc,@lcapa,@clos,@closed,@sig,@risk,@anom,NOW(),@lmb,@ip,@audit)";
            string updateSql = @"UPDATE deviations SET code=@code, title=@title, description=@desc, reported_at=@rep, reported_by_id=@rby, severity=@sev, is_critical=@crit, status=@status, assigned_investigator_id=@assid, assigned_investigator_name=@assname, investigation_started_at=@invstart, root_cause=@rc, linked_capa_id=@lcapa, closure_comment=@clos, closed_at=@closed, digital_signature=@sig, risk_score=@risk, anomaly_score=@anom, last_modified=NOW(), last_modified_by_id=@lmb, source_ip=@ip, audit_note=@audit WHERE id=@id";

            var pars = new List<MySqlParameter>
            {
                new("@code", (object?)d.Code ?? DBNull.Value),
                new("@title", d.Title ?? string.Empty),
                new("@desc", d.Description ?? string.Empty),
                new("@rep", d.ReportedAt),
                new("@rby", d.ReportedById),
                new("@sev", d.Severity ?? string.Empty),
                new("@crit", d.IsCritical),
                new("@status", d.Status ?? string.Empty),
                new("@assid", (object?)d.AssignedInvestigatorId ?? DBNull.Value),
                new("@assname", (object?)d.AssignedInvestigatorName ?? DBNull.Value),
                new("@invstart", (object?)d.InvestigationStartedAt ?? DBNull.Value),
                new("@rc", (object?)d.RootCause ?? DBNull.Value),
                new("@lcapa", (object?)d.LinkedCapaId ?? DBNull.Value),
                new("@clos", (object?)d.ClosureComment ?? DBNull.Value),
                new("@closed", (object?)d.ClosedAt ?? DBNull.Value),
                new("@sig", (object?)d.DigitalSignature ?? DBNull.Value),
                new("@risk", d.RiskScore),
                new("@anom", (object?)d.AnomalyScore ?? DBNull.Value),
                new("@lmb", actorUserId),
                new("@ip", (object?)ip ?? DBNull.Value),
                new("@audit", (object?)d.AuditNote ?? DBNull.Value)
            };

            if (!update)
            {
                await db.ExecuteNonQueryAsync(insert, pars, token).ConfigureAwait(false);
                var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                d.Id = Convert.ToInt32(idObj);
            }
            else
            {
                pars.Add(new MySqlParameter("@id", d.Id));
                await db.ExecuteNonQueryAsync(updateSql, pars, token).ConfigureAwait(false);
            }

            await db.LogSystemEventAsync(actorUserId, update ? "DEVIATION_UPDATE" : "DEVIATION_CREATE", "deviations", "DeviationModule", d.Id, d.Title, ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
            return d.Id;
        }

        public static async Task DeleteDeviationAsync(this DatabaseService db, int id, int actorUserId, string? ip = null, string? device = null, string? sessionId = null, CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync("DELETE FROM deviations WHERE id=@id", new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            await db.LogSystemEventAsync(actorUserId, "DEVIATION_DELETE", "deviations", "DeviationModule", id, null, ip, "audit", device, sessionId, token: token).ConfigureAwait(false);
        }

        private static Deviation Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            double? Dbl(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDouble(r[c]) : (double?)null;
            DateTime D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : DateTime.UtcNow;
            DateTime? DN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

            return new Deviation
            {
                Id = I("id"),
                Code = S("code"),
                Title = S("title"),
                Description = S("description"),
                ReportedAt = D("reported_at"),
                ReportedById = I("reported_by_id"),
                Severity = S("severity"),
                IsCritical = r.Table.Columns.Contains("is_critical") && r["is_critical"] != DBNull.Value && Convert.ToBoolean(r["is_critical"]),
                Status = S("status"),
                AssignedInvestigatorId = IN("assigned_investigator_id"),
                AssignedInvestigatorName = S("assigned_investigator_name"),
                InvestigationStartedAt = DN("investigation_started_at"),
                RootCause = S("root_cause"),
                LinkedCapaId = IN("linked_capa_id"),
                ClosureComment = S("closure_comment"),
                ClosedAt = DN("closed_at"),
                DigitalSignature = S("digital_signature"),
                RiskScore = I("risk_score"),
                AnomalyScore = Dbl("anomaly_score"),
                LastModified = DN("last_modified") ?? DateTime.UtcNow,
                LastModifiedById = IN("last_modified_by_id"),
                SourceIp = S("source_ip"),
                AuditNote = S("audit_note")
            };
        }
    }
}

