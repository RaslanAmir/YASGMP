using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.Enums;

namespace YasGMP.Services.Interfaces
{
    /// <summary>
    /// <b>IDeviationAuditService</b> – GMP/21 CFR Part 11 compliant interface for deviation audit logging and analytics.
    /// <para>
    /// ✔ Defines contract for all traceability of deviation (non-conformance) actions: create, update, investigation, closure, CAPA, export, and more.<br/>
    /// ✔ Supports forensic data, digital signatures, AI/ML anomaly, advanced queries for regulatory/analytics use.<br/>
    /// ✔ Ensures every deviation event is logged for full compliance, audit, and business intelligence.
    /// </para>
    /// </summary>
    public interface IDeviationAuditService
    {
        /// <summary>
        /// Creates a new deviation audit log entry.
        /// </summary>
        /// <param name="audit">Fully-populated audit entry.</param>
        Task CreateAsync(DeviationAudit audit);

        /// <summary>
        /// Updates an existing deviation audit log entry.
        /// </summary>
        /// <param name="audit">Audit entry with updated values.</param>
        Task UpdateAsync(DeviationAudit audit);

        /// <summary>
        /// Deletes a deviation audit entry by identifier.
        /// </summary>
        /// <param name="id">Audit entry identifier.</param>
        Task DeleteAsync(int id);

        /// <summary>
        /// Retrieves a deviation audit record by its ID.
        /// Implementations may throw if the record is not found.
        /// </summary>
        /// <param name="id">Audit entry identifier.</param>
        /// <returns>The matching <see cref="DeviationAudit"/>.</returns>
        Task<DeviationAudit> GetByIdAsync(int id);

        /// <summary>Retrieves the audit trail for a deviation (newest first).</summary>
        Task<IReadOnlyList<DeviationAudit>> GetByDeviationIdAsync(int deviationId);

        /// <summary>Retrieves audit entries created by a specific user.</summary>
        Task<IReadOnlyList<DeviationAudit>> GetByUserIdAsync(int userId);

        /// <summary>Retrieves audit entries by action type.</summary>
        Task<IReadOnlyList<DeviationAudit>> GetByActionTypeAsync(DeviationActionType actionType);

        /// <summary>Retrieves audit entries within a UTC date range.</summary>
        Task<IReadOnlyList<DeviationAudit>> GetByDateRangeAsync(DateTime from, DateTime to);

        /// <summary>Validates the integrity of an audit entry by recomputing its signature.</summary>
        bool ValidateIntegrity(DeviationAudit audit);

        /// <summary>Exports the audit trail for a deviation to a file.</summary>
        Task<string> ExportAuditLogsAsync(int deviationId, string format = "pdf");

        /// <summary>AI/ML anomaly score placeholder.</summary>
        Task<double> AnalyzeAnomalyAsync(int deviationId);
    }
}
