using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.Enums;

namespace YasGMP.Services.Interfaces
{
    /// <summary>
    /// <b>ICapaAuditService</b> – GMP/21 CFR Part 11 compliant interface for CAPA audit logging services.
    /// <para>✔ Defines contract for full traceability of CAPA actions (CREATE, UPDATE, CLOSE, DELETE, etc.).</para>
    /// <para>✔ Supports forensic data, digital signatures, and advanced queries for inspections.</para>
    /// <para>✔ Ensures every CAPA-related event is captured for regulatory compliance and analytics.</para>
    /// </summary>
    public interface ICapaAuditService
    {
        /// <summary>
        /// Creates a new CAPA audit log entry.
        /// </summary>
        /// <param name="audit">The <see cref="CapaAudit"/> entity containing all details of the audit record.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task CreateAsync(CapaAudit audit);

        /// <summary>
        /// Updates an existing CAPA audit record (if modifications are required for corrective purposes).
        /// </summary>
        /// <param name="audit">The updated <see cref="CapaAudit"/> object.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task UpdateAsync(CapaAudit audit);

        /// <summary>
        /// Deletes a CAPA audit record by its unique ID (administrative use only).
        /// <para>⚠ Deletions must also be logged separately for compliance.</para>
        /// </summary>
        /// <param name="id">Unique ID of the audit entry to delete.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeleteAsync(int id);

        /// <summary>
        /// Retrieves a CAPA audit record by its ID.
        /// </summary>
        /// <param name="id">The unique ID of the CAPA audit entry.</param>
        /// <returns>The corresponding <see cref="CapaAudit"/> or <c>null</c> if not found.</returns>
        Task<CapaAudit> GetByIdAsync(int id);

        /// <summary>
        /// Retrieves all audit logs for a specific CAPA case.
        /// </summary>
        /// <param name="capaId">The ID of the CAPA case.</param>
        /// <returns>A read-only list of <see cref="CapaAudit"/> entries.</returns>
        Task<IReadOnlyList<CapaAudit>> GetByCapaIdAsync(int capaId);

        /// <summary>
        /// Retrieves all CAPA audit records associated with a specific user.
        /// </summary>
        /// <param name="userId">ID of the user who performed the action.</param>
        /// <returns>A list of <see cref="CapaAudit"/> records.</returns>
        Task<IReadOnlyList<CapaAudit>> GetByUserIdAsync(int userId);

        /// <summary>
        /// Retrieves CAPA audit records filtered by a specific action type (CREATE, UPDATE, CLOSE, etc.).
        /// </summary>
        /// <param name="actionType">The CAPA action type to filter by.</param>
        /// <returns>A list of <see cref="CapaAudit"/> entries matching the filter.</returns>
        Task<IReadOnlyList<CapaAudit>> GetByActionTypeAsync(CapaActionType actionType);

        /// <summary>
        /// Retrieves CAPA audit records in a given date range.
        /// </summary>
        /// <param name="from">Start date of the range (UTC).</param>
        /// <param name="to">End date of the range (UTC).</param>
        /// <returns>A list of <see cref="CapaAudit"/> records within the specified period.</returns>
        Task<IReadOnlyList<CapaAudit>> GetByDateRangeAsync(DateTime from, DateTime to);

        /// <summary>
        /// Validates the integrity of a CAPA audit record by comparing stored and regenerated hashes.
        /// </summary>
        /// <param name="audit">The audit entry to validate.</param>
        /// <returns><c>true</c> if the integrity is intact, otherwise <c>false</c>.</returns>
        bool ValidateIntegrity(CapaAudit audit);
    }
}

