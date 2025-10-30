// ==============================================================================
// File: Services/DatabaseService.RiskAssessments.Extensions.cs
// Purpose: Minimal Risk Assessment list/CRUD/workflow/audit/export shims
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
    /// DatabaseService extensions dealing with risk assessment records and schedules.
    /// </summary>
    public static class DatabaseServiceRiskAssessmentsExtensions
    {
        /// <summary>
        /// Executes the get all risk assessments full async operation.
        /// </summary>
        public static async Task<List<RiskAssessment>> GetAllRiskAssessmentsFullAsync(this DatabaseService db, CancellationToken token = default)
        {
            const string sqlPreferred = @"SELECT
    ra.id,
    ra.code,
    ra.title,
    ra.description,
    ra.category,
    ra.area,
    ra.status,
    ra.assessed_by,
    ra.assessed_at,
    ra.severity,
    ra.probability,
    ra.detection,
    ra.risk_score,
    ra.risk_level,
    ra.mitigation,
    ra.action_plan,
    ra.owner_id,
    owner.username AS owner_username,
    owner.full_name AS owner_full_name,
    ra.approved_by_id,
    approver.username AS approved_by_username,
    approver.full_name AS approved_by_full_name,
    ra.approved_at,
    ra.review_date,
    ra.digital_signature,
    ra.note,
    ra.device_info,
    ra.session_id,
    ra.ip_address
FROM risk_assessments ra
LEFT JOIN users owner ON owner.id = ra.owner_id
LEFT JOIN users approver ON approver.id = ra.approved_by_id
ORDER BY
    CASE WHEN ra.review_date IS NULL THEN 1 ELSE 0 END,
    ra.review_date DESC,
    ra.id DESC";

            const string sqlLegacy = @"SELECT
    ra.*
FROM risk_assessments ra
ORDER BY ra.id DESC";

            System.Data.DataTable dt;
            try
            {
                dt = await db.ExecuteSelectAsync(sqlPreferred, null, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                dt = await db.ExecuteSelectAsync(sqlLegacy, null, token).ConfigureAwait(false);
            }
            var list = new List<RiskAssessment>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }
        /// <summary>
        /// Executes the initiate risk assessment async operation.
        /// </summary>

        public static Task InitiateRiskAssessmentAsync(this DatabaseService db, RiskAssessment risk, CancellationToken token = default)
            => db.LogRiskAssessmentAuditAsync(risk, "INITIATE", risk.IpAddress ?? string.Empty, risk.DeviceInfo ?? string.Empty, risk.SessionId, null, token);
        /// <summary>
        /// Executes the update risk assessment async operation.
        /// </summary>

        public static Task UpdateRiskAssessmentAsync(this DatabaseService db, RiskAssessment risk, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.LogRiskAssessmentAuditAsync(risk, "UPDATE", ip, deviceInfo, sessionId, null, token);
        /// <summary>
        /// Executes the approve risk assessment async operation.
        /// </summary>

        public static Task ApproveRiskAssessmentAsync(this DatabaseService db, int riskId, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.LogRiskAssessmentAuditAsync(new RiskAssessment { Id = riskId }, "APPROVE", ip, deviceInfo, sessionId, null, token);
        /// <summary>
        /// Executes the close risk assessment async operation.
        /// </summary>

        public static Task CloseRiskAssessmentAsync(this DatabaseService db, int riskId, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.LogRiskAssessmentAuditAsync(new RiskAssessment { Id = riskId }, "CLOSE", ip, deviceInfo, sessionId, null, token);

        // Overload with note parameter to match VM call
        /// <summary>
        /// Executes the close risk assessment async operation.
        /// </summary>
        public static Task CloseRiskAssessmentAsync(this DatabaseService db, int riskId, int actorUserId, string ip, string deviceInfo, string? sessionId, string? note, CancellationToken token = default)
            => db.LogRiskAssessmentAuditAsync(new RiskAssessment { Id = riskId }, "CLOSE", ip, deviceInfo, sessionId, note, token);
        /// <summary>
        /// Executes the export risk assessments async operation.
        /// </summary>

        public static Task ExportRiskAssessmentsAsync(this DatabaseService db, List<RiskAssessment> items, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.LogRiskAssessmentAuditAsync(null, "EXPORT", ip, deviceInfo, sessionId, $"count={items?.Count ?? 0}", token);
        /// <summary>
        /// Executes the log risk assessment audit async operation.
        /// </summary>

        public static Task LogRiskAssessmentAuditAsync(this DatabaseService db, RiskAssessment? risk, string action, string ip, string deviceInfo, string? sessionId, string? details, CancellationToken token = default)
            => db.LogSystemEventAsync(null, $"RA_{action}", "risk_assessments", "RiskAssessment", risk?.Id, details ?? risk?.Code, ip, "audit", deviceInfo, sessionId, token: token);

        private static RiskAssessment Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            DateTime? D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

            var risk = new RiskAssessment
            {
                Id = I("id"),
                Code = S("code"),
                Title = S("title"),
                Description = S("description"),
                Category = S("category"),
                Area = S("area"),
                Status = S("status"),
                AssessedBy = S("assessed_by"),
                AssessedAt = D("assessed_at"),
                Severity = I("severity"),
                Probability = I("probability"),
                Detection = I("detection"),
                RiskScore = IN("risk_score"),
                RiskLevel = S("risk_level"),
                Mitigation = S("mitigation"),
                ActionPlan = S("action_plan"),
                OwnerId = IN("owner_id"),
                ApprovedById = IN("approved_by_id"),
                ApprovedAt = D("approved_at"),
                ReviewDate = D("review_date"),
                DigitalSignature = S("digital_signature"),
                Note = S("note"),
                DeviceInfo = S("device_info"),
                SessionId = S("session_id"),
                IpAddress = S("ip_address")
            };

            if (risk.OwnerId.HasValue)
            {
                var ownerFullName = S("owner_full_name");
                var ownerUsername = S("owner_username");
                if (!string.IsNullOrWhiteSpace(ownerFullName) || !string.IsNullOrWhiteSpace(ownerUsername))
                {
                    risk.Owner = new User
                    {
                        Id = risk.OwnerId.Value,
                        FullName = string.IsNullOrWhiteSpace(ownerFullName) ? ownerUsername : ownerFullName,
                        Username = ownerUsername
                    };
                }
            }

            if (risk.ApprovedById.HasValue)
            {
                var approverFullName = S("approved_by_full_name");
                var approverUsername = S("approved_by_username");
                if (!string.IsNullOrWhiteSpace(approverFullName) || !string.IsNullOrWhiteSpace(approverUsername))
                {
                    risk.ApprovedBy = new User
                    {
                        Id = risk.ApprovedById.Value,
                        FullName = string.IsNullOrWhiteSpace(approverFullName) ? approverUsername : approverFullName,
                        Username = approverUsername
                    };
                }
            }

            return risk;
        }
    }
}
