using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.Enums;

namespace YasGMP.Services.Interfaces
{
    /// <summary>
    /// <b>IWorkOrderAuditService</b> – GMP/21 CFR Part 11 compliant interface for handling 
    /// forensic audit logs of work orders.
    /// <para>✔ Defines asynchronous CRUD operations and advanced filtering.</para>
    /// <para>✔ Supports integrity validation, rollback inspection, and digital signature verification.</para>
    /// </summary>
    public interface IWorkOrderAuditService
    {
        #region === CRUD OPERATIONS ===

        /// <summary>
        /// Retrieves a single audit entry by its unique ID.
        /// </summary>
        /// <param name="id">Audit entry ID.</param>
        /// <returns>Instance of <see cref="WorkOrderAudit"/> or null if not found.</returns>
        Task<WorkOrderAudit> GetByIdAsync(int id);

        /// <summary>
        /// Creates a new audit entry for a work order.
        /// </summary>
        /// <param name="audit">Audit entry containing full forensic data.</param>
        Task CreateAsync(WorkOrderAudit audit);

        /// <summary>
        /// Updates an existing audit entry (recalculates integrity hash).
        /// </summary>
        /// <param name="audit">Audit entry with modifications.</param>
        Task UpdateAsync(WorkOrderAudit audit);

        /// <summary>
        /// Deletes an audit entry by its ID.
        /// </summary>
        /// <param name="id">Audit ID to be deleted.</param>
        Task DeleteAsync(int id);

        #endregion

        #region === FILTERED QUERIES ===

        /// <summary>
        /// Retrieves all audits for a specific work order.
        /// </summary>
        /// <param name="workOrderId">Work order ID.</param>
        Task<IReadOnlyList<WorkOrderAudit>> GetByWorkOrderIdAsync(int workOrderId);

        /// <summary>
        /// Retrieves audit entries filtered by action type.
        /// </summary>
        /// <param name="actionType">Action type (CREATE, UPDATE, DELETE, CLOSE, etc.).</param>
        Task<IReadOnlyList<WorkOrderAudit>> GetByActionTypeAsync(WorkOrderActionType actionType);

        /// <summary>
        /// Retrieves audit entries filtered by user ID.
        /// </summary>
        /// <param name="userId">User ID who performed the action.</param>
        Task<IReadOnlyList<WorkOrderAudit>> GetByUserIdAsync(int userId);

        /// <summary>
        /// Retrieves audits performed within a specific date range.
        /// </summary>
        /// <param name="from">Start date (UTC).</param>
        /// <param name="to">End date (UTC).</param>
        Task<IReadOnlyList<WorkOrderAudit>> GetByDateRangeAsync(DateTime from, DateTime to);

        /// <summary>
        /// Retrieves audits associated with a specific incident.
        /// </summary>
        /// <param name="incidentId">Incident ID.</param>
        Task<IReadOnlyList<WorkOrderAudit>> GetByIncidentIdAsync(int incidentId);

        /// <summary>
        /// Retrieves audits associated with a specific CAPA case.
        /// </summary>
        /// <param name="capaId">CAPA case ID.</param>
        Task<IReadOnlyList<WorkOrderAudit>> GetByCapaIdAsync(int capaId);

        #endregion

        #region === FORENSIC & INTEGRITY OPERATIONS ===

        /// <summary>
        /// Validates whether the integrity hash of a given audit record matches its calculated value.
        /// </summary>
        /// <param name="audit">Audit entry to validate.</param>
        /// <returns>True if the record is intact, false if it has been tampered with.</returns>
        bool ValidateIntegrity(WorkOrderAudit audit);

        /// <summary>
        /// Verifies the digital signature of an audit entry for non-repudiation.
        /// </summary>
        /// <param name="audit">Audit entry to verify.</param>
        /// <returns>True if signature is valid, otherwise false.</returns>
        bool VerifyDigitalSignature(WorkOrderAudit audit);

        /// <summary>
        /// Retrieves a previous state snapshot (OldValue) to support rollback inspection.
        /// </summary>
        /// <param name="auditId">Audit entry ID.</param>
        /// <returns>Serialized snapshot of the previous state.</returns>
        Task<string> GetPreviousStateSnapshotAsync(int auditId);

        #endregion

        #region === QUICK LOGGING HELPERS ===

        /// <summary>
        /// Quickly logs an audit event without requiring a full <see cref="WorkOrderAudit"/> object.
        /// </summary>
        /// <param name="workOrderId">Related work order ID.</param>
        /// <param name="userId">ID of the user performing the action.</param>
        /// <param name="action">Type of action performed.</param>
        /// <param name="note">Optional context or reason for the action.</param>
        Task LogQuickAsync(int workOrderId, int userId, WorkOrderActionType action, string note);

        #endregion
    }
}
