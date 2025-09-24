// ==============================================================================
// File: Services/DatabaseService.ContractorInterventionExtensions.cs
// Purpose: Historical shim only. Methods here are NON-extension and suffixed
//          with "Legacy" so they DO NOT collide with the real extension API
//          in DatabaseServiceContractorInterventionsApi.
//          They simply forward to the implemented API to keep compatibility.
// ==============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Legacy shim for old contractor-intervention calls.
    /// <para>
    /// <b>Important:</b> This class intentionally exposes <b>non-extension</b> methods
    /// with a <c>Legacy</c> suffix so they don’t conflict with the real extension
    /// methods in <see cref="DatabaseServiceContractorInterventionsApi"/>.
    /// </para>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use DatabaseServiceContractorInterventionsApi extension methods instead.", false)]
    public static class DatabaseServiceContractorInterventionExtensions
    {
        // ----------------------------------------------------------------------
        // READ (LEGACY FORWARDS)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Legacy helper: forwards to
        /// <see cref="DatabaseServiceContractorInterventionsApi.GetAllContractorInterventionsAsync(DatabaseService, CancellationToken)"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use db.GetAllContractorInterventionsAsync() extension instead.", false)]
        public static Task<List<ContractorIntervention>> GetAllContractorInterventionsLegacyAsync(
            DatabaseService db,
            CancellationToken cancellationToken = default)
            => db.GetAllContractorInterventionsAsync(cancellationToken);

        /// <summary>
        /// Legacy helper: forwards to
        /// <see cref="DatabaseServiceContractorInterventionsApi.GetContractorInterventionAuditAsync(DatabaseService, int, CancellationToken)"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use db.GetContractorInterventionAuditAsync(id) extension instead.", false)]
        public static Task<List<ContractorInterventionAudit>> GetContractorInterventionAuditLegacyAsync(
            DatabaseService db,
            int interventionId,
            CancellationToken cancellationToken = default)
            => db.GetContractorInterventionAuditAsync(interventionId, cancellationToken);

        // ----------------------------------------------------------------------
        // CREATE (LEGACY FORWARDS)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Legacy helper: forwards to
        /// <see cref="DatabaseServiceContractorInterventionsApi.AddContractorInterventionAsync(DatabaseService, ContractorIntervention, int, string, string?, CancellationToken)"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use db.AddContractorInterventionAsync(entity, userId, actor, …) extension instead.", false)]
        public static Task<int> AddContractorInterventionLegacyAsync(
            DatabaseService db,
            ContractorIntervention intervention,
            int userId,
            string actor,
            string? comment = null,
            CancellationToken cancellationToken = default)
            => db.AddContractorInterventionAsync(intervention, userId, actor, comment, cancellationToken);

        /// <summary>
        /// Legacy helper: forwards to
        /// <see cref="DatabaseServiceContractorInterventionsApi.AddContractorInterventionAsync(DatabaseService, ContractorIntervention, int, string, string, string?, CancellationToken)"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use db.AddContractorInterventionAsync(entity, userId, ip, device, …) extension instead.", false)]
        public static Task<int> AddContractorInterventionLegacyAsync(
            DatabaseService db,
            ContractorIntervention intervention,
            int userId,
            string ipAddress,
            string deviceInfo,
            string? comment = null,
            CancellationToken cancellationToken = default)
            => db.AddContractorInterventionAsync(intervention, userId, ipAddress, deviceInfo, comment, cancellationToken);

        // ----------------------------------------------------------------------
        // UPDATE (LEGACY FORWARDS)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Legacy helper: forwards to
        /// <see cref="DatabaseServiceContractorInterventionsApi.UpdateContractorInterventionAsync(DatabaseService, ContractorIntervention, int, string, string?, CancellationToken)"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use db.UpdateContractorInterventionAsync(entity, userId, actor, …) extension instead.", false)]
        public static Task UpdateContractorInterventionLegacyAsync(
            DatabaseService db,
            ContractorIntervention intervention,
            int userId,
            string actor,
            string? comment = null,
            CancellationToken cancellationToken = default)
            => db.UpdateContractorInterventionAsync(intervention, userId, actor, comment, cancellationToken);

        /// <summary>
        /// Legacy helper: forwards to
        /// <see cref="DatabaseServiceContractorInterventionsApi.UpdateContractorInterventionAsync(DatabaseService, ContractorIntervention, int, string, string, string?, CancellationToken)"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use db.UpdateContractorInterventionAsync(entity, userId, ip, device, …) extension instead.", false)]
        public static Task UpdateContractorInterventionLegacyAsync(
            DatabaseService db,
            ContractorIntervention intervention,
            int userId,
            string ipAddress,
            string deviceInfo,
            string? comment = null,
            CancellationToken cancellationToken = default)
            => db.UpdateContractorInterventionAsync(intervention, userId, ipAddress, deviceInfo, comment, cancellationToken);

        // ----------------------------------------------------------------------
        // DELETE (LEGACY FORWARDS)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Legacy helper: forwards to
        /// <see cref="DatabaseServiceContractorInterventionsApi.DeleteContractorInterventionAsync(DatabaseService, int, int, string, string?, CancellationToken)"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use db.DeleteContractorInterventionAsync(id, userId, actor, …) extension instead.", false)]
        public static Task DeleteContractorInterventionLegacyAsync(
            DatabaseService db,
            int interventionId,
            int userId,
            string actor,
            string? comment = null,
            CancellationToken cancellationToken = default)
            => db.DeleteContractorInterventionAsync(interventionId, userId, actor, comment, cancellationToken);

        /// <summary>
        /// Legacy helper: forwards to
        /// <see cref="DatabaseServiceContractorInterventionsApi.DeleteContractorInterventionAsync(DatabaseService, int, int, string, string, string?, CancellationToken)"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use db.DeleteContractorInterventionAsync(id, userId, ip, device, …) extension instead.", false)]
        public static Task DeleteContractorInterventionLegacyAsync(
            DatabaseService db,
            int interventionId,
            int userId,
            string ipAddress,
            string deviceInfo,
            string? comment = null,
            CancellationToken cancellationToken = default)
            => db.DeleteContractorInterventionAsync(interventionId, userId, ipAddress, deviceInfo, comment, cancellationToken);

        /// <summary>
        /// Legacy helper: forwards to
        /// <see cref="DatabaseServiceContractorInterventionsApi.DeleteContractorInterventionAsync(DatabaseService, int, CancellationToken)"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use db.DeleteContractorInterventionAsync(id) extension instead.", false)]
        public static Task DeleteContractorInterventionLegacyAsync(
            DatabaseService db,
            int interventionId,
            CancellationToken cancellationToken = default)
            => db.DeleteContractorInterventionAsync(interventionId, cancellationToken);

        // ----------------------------------------------------------------------
        // ROLLBACK (LEGACY FORWARDS)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Legacy helper: forwards to
        /// <see cref="DatabaseServiceContractorInterventionsApi.RollbackContractorInterventionAsync(DatabaseService, int, string, int, string, string?, CancellationToken)"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use db.RollbackContractorInterventionAsync(id, snapshot, userId, actor, …) extension instead.", false)]
        public static Task RollbackContractorInterventionLegacyAsync(
            DatabaseService db,
            int interventionId,
            string previousSnapshot,
            int userId,
            string actor,
            string? comment = null,
            CancellationToken cancellationToken = default)
            => db.RollbackContractorInterventionAsync(interventionId, previousSnapshot, userId, actor, comment, cancellationToken);

        /// <summary>
        /// Legacy helper: forwards to
        /// <see cref="DatabaseServiceContractorInterventionsApi.RollbackContractorInterventionAsync(DatabaseService, int, string, int, string, string, string?, CancellationToken)"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use db.RollbackContractorInterventionAsync(id, snapshot, userId, ip, device, …) extension instead.", false)]
        public static Task RollbackContractorInterventionLegacyAsync(
            DatabaseService db,
            int interventionId,
            string previousSnapshot,
            int userId,
            string ipAddress,
            string deviceInfo,
            string? comment = null,
            CancellationToken cancellationToken = default)
            => db.RollbackContractorInterventionAsync(interventionId, previousSnapshot, userId, ipAddress, deviceInfo, comment, cancellationToken);

        // ----------------------------------------------------------------------
        // EXPORT (LEGACY FORWARDS)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Legacy helper: forwards to
        /// <see cref="DatabaseServiceContractorInterventionsApi.ExportContractorInterventionsAsync(DatabaseService, CancellationToken)"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use db.ExportContractorInterventionsAsync() extension instead.", false)]
        public static Task<string> ExportContractorInterventionsLegacyAsync(
            DatabaseService db,
            CancellationToken cancellationToken = default)
            => db.ExportContractorInterventionsAsync(cancellationToken);

        /// <summary>
        /// Legacy helper: forwards to
        /// <see cref="DatabaseServiceContractorInterventionsApi.ExportContractorInterventionsAsync(DatabaseService, string, CancellationToken)"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use db.ExportContractorInterventionsAsync(format) extension instead.", false)]
        public static Task<string> ExportContractorInterventionsLegacyAsync(
            DatabaseService db,
            string format,
            CancellationToken cancellationToken = default)
            => db.ExportContractorInterventionsAsync(format, cancellationToken);

        // ----------------------------------------------------------------------
        // AUDIT (LEGACY FORWARDS)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Legacy helper: forwards to
        /// <see cref="DatabaseServiceContractorInterventionsApi.LogContractorInterventionAuditAsync(DatabaseService, ContractorInterventionAudit, CancellationToken)"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use db.LogContractorInterventionAuditAsync(audit) extension instead.", false)]
        public static Task LogContractorInterventionAuditLegacyAsync(
            DatabaseService db,
            ContractorInterventionAudit audit,
            CancellationToken cancellationToken = default)
            => db.LogContractorInterventionAuditAsync(audit, cancellationToken);

        /// <summary>
        /// Legacy helper: forwards to
        /// <see cref="DatabaseServiceContractorInterventionsApi.LogContractorInterventionAuditAsync(DatabaseService, int, int, string, string?, CancellationToken)"/>.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use db.LogContractorInterventionAuditAsync(id, userId, action, …) extension instead.", false)]
        public static Task LogContractorInterventionAuditLegacyAsync(
            DatabaseService db,
            int interventionId,
            int userId,
            string action,
            string? details = null,
            CancellationToken cancellationToken = default)
            => db.LogContractorInterventionAuditAsync(interventionId, userId, action, details, cancellationToken);
    }
}
