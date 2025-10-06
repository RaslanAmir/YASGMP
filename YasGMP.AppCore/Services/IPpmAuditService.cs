using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services.Interfaces
{
    /// <summary>
    /// <b>IPpmAuditService</b> — Ultra-robust, GMP/21 CFR Part 11-compliant interface for logging all Preventive Maintenance Plan (PPM) audit actions.
    /// <para>Supports immutable audit trails, digital signatures, role-based approvals, AI anomaly scoring, and full forensic traceability.</para>
    /// <para>Integrates with <see cref="PpmAudit"/> and supports batch, advanced filtering, regulatory export, rollback, and IoT-linked events.</para>
    /// </summary>
    public interface IPpmAuditService
    {
        /// <summary>
        /// Creates a new PPM audit log entry (single action).
        /// </summary>
        /// <param name="audit">PPM audit entry to insert (must include digital signature).</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>Awaitable Task.</returns>
        Task CreateAsync(PpmAudit audit, CancellationToken token = default);

        /// <summary>
        /// Creates multiple audit entries in a batch (bulk operations, import/export).
        /// </summary>
        /// <param name="audits">Collection of audit records.</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>Number of records created.</returns>
        Task<int> CreateBatchAsync(IEnumerable<PpmAudit> audits, CancellationToken token = default);

        /// <summary>
        /// Gets all audit records for a given PPM plan.
        /// </summary>
        /// <param name="ppmPlanId">ID of the PPM plan.</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>List of PPM audit records.</returns>
        Task<IReadOnlyList<PpmAudit>> GetByPlanIdAsync(int ppmPlanId, CancellationToken token = default);

        /// <summary>
        /// Gets all audit records for a given user (regulatory, analytics).
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>List of PPM audit records.</returns>
        Task<IReadOnlyList<PpmAudit>> GetByUserIdAsync(int userId, CancellationToken token = default);

        /// <summary>
        /// Gets all audit records in a date/time window (regulatory, PDF export).
        /// </summary>
        /// <param name="from">From date/time.</param>
        /// <param name="to">To date/time.</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>List of PPM audit records.</returns>
        Task<IReadOnlyList<PpmAudit>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken token = default);

        /// <summary>
        /// Gets a single audit entry by its ID.
        /// </summary>
        /// <param name="auditId">Audit record ID.</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>PpmAudit record or null.</returns>
        Task<PpmAudit> GetByIdAsync(int auditId, CancellationToken token = default);

        /// <summary>
        /// Updates an existing audit entry (only allowed for regulatory corrections with full trace).
        /// </summary>
        /// <param name="audit">Audit record to update (must include ID).</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>Awaitable Task.</returns>
        Task UpdateAsync(PpmAudit audit, CancellationToken token = default);

        /// <summary>
        /// Hard-deletes an audit entry (admin/forensic only, with full trace).
        /// </summary>
        /// <param name="id">Audit record ID.</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>Awaitable Task.</returns>
        Task DeleteAsync(int id, CancellationToken token = default);

        /// <summary>
        /// Exports PPM audit logs to a specified format (PDF/Excel/CSV), returns file path.
        /// </summary>
        /// <param name="audits">Audit records to export.</param>
        /// <param name="format">Export format (pdf, excel, csv).</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>Path to exported file.</returns>
        Task<string> ExportAsync(IEnumerable<PpmAudit> audits, string format = "pdf", CancellationToken token = default);
    }
}

