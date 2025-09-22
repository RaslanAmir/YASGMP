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
        public static async Task<List<RiskAssessment>> GetAllRiskAssessmentsFullAsync(this DatabaseService db, CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync("SELECT id FROM risk_assessments ORDER BY id DESC", null, token).ConfigureAwait(false);
            var list = new List<RiskAssessment>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(Map(r));
            return list;
        }

        public static Task InitiateRiskAssessmentAsync(this DatabaseService db, RiskAssessment risk, CancellationToken token = default)
            => db.LogRiskAssessmentAuditAsync(risk, "INITIATE", risk.IpAddress ?? string.Empty, risk.DeviceInfo ?? string.Empty, risk.SessionId, null, token);

        public static Task UpdateRiskAssessmentAsync(this DatabaseService db, RiskAssessment risk, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.LogRiskAssessmentAuditAsync(risk, "UPDATE", ip, deviceInfo, sessionId, null, token);

        public static Task ApproveRiskAssessmentAsync(this DatabaseService db, int riskId, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.LogRiskAssessmentAuditAsync(new RiskAssessment { Id = riskId }, "APPROVE", ip, deviceInfo, sessionId, null, token);

        public static Task CloseRiskAssessmentAsync(this DatabaseService db, int riskId, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.LogRiskAssessmentAuditAsync(new RiskAssessment { Id = riskId }, "CLOSE", ip, deviceInfo, sessionId, null, token);

        // Overload with note parameter to match VM call
        public static Task CloseRiskAssessmentAsync(this DatabaseService db, int riskId, int actorUserId, string ip, string deviceInfo, string? sessionId, string? note, CancellationToken token = default)
            => db.LogRiskAssessmentAuditAsync(new RiskAssessment { Id = riskId }, "CLOSE", ip, deviceInfo, sessionId, note, token);

        public static Task ExportRiskAssessmentsAsync(this DatabaseService db, List<RiskAssessment> items, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
            => db.LogRiskAssessmentAuditAsync(null, "EXPORT", ip, deviceInfo, sessionId, $"count={items?.Count ?? 0}", token);

        public static Task LogRiskAssessmentAuditAsync(this DatabaseService db, RiskAssessment? risk, string action, string ip, string deviceInfo, string? sessionId, string? details, CancellationToken token = default)
            => db.LogSystemEventAsync(null, $"RA_{action}", "risk_assessments", "RiskAssessment", risk?.Id, details ?? risk?.Code, ip, "audit", deviceInfo, sessionId, token: token);

        private static RiskAssessment Map(DataRow r)
        {
            string S(string c) => r.Table.Columns.Contains(c) ? (r[c]?.ToString() ?? string.Empty) : string.Empty;
            int I(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            int? IN(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : (int?)null;
            DateTime? D(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToDateTime(r[c]) : (DateTime?)null;

            return new RiskAssessment
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
        }
    }
}
