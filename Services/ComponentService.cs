// File: YASGMP/Services/ComponentService.cs
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using YasGMP.Models;
using YasGMP.Helpers;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>ComponentService</b> — GMP/Annex 11 &amp; 21 CFR Part 11 aligned service for managing machine components.
    /// <para>
    /// • Full CRUD with validation and digital signatures.<br/>
    /// • All actions recorded via <see cref="AuditService"/> for complete traceability.<br/>
    /// • Calls are aligned with <see cref="DatabaseService"/> Region “03 · MACHINE / COMPONENT”.
    /// </para>
    /// </summary>
    public class ComponentService
    {
        #region === Fields & Constructor ======================================================

        private readonly DatabaseService _db;
        private readonly AuditService _audit;

        /// <summary>Initializes a new <see cref="ComponentService"/>.</summary>
        /// <param name="databaseService">Data access service (must not be <c>null</c>).</param>
        /// <param name="auditService">Audit logging service (must not be <c>null</c>).</param>
        /// <exception cref="ArgumentNullException">Thrown when any dependency is <c>null</c>.</exception>
        public ComponentService(DatabaseService databaseService, AuditService auditService)
        {
            _db = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _audit = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        #endregion

        #region === CRUD Operations ===========================================================

        /// <summary>Fetches all components from the database (no filtering).</summary>
        public async Task<List<Component>> GetAllAsync()
            => ComponentMapper.ToComponentList(await _db.GetAllComponentsAsync());

        /// <summary>Fetches a single component by its primary key.</summary>
        public async Task<Component?> GetByIdAsync(int id)
        {
            var mc = await _db.GetComponentByIdAsync(id);
            return mc == null ? null : ComponentMapper.ToComponent(mc);
        }

        /// <summary>
        /// Creates a new component, generates a digital signature, persists it, and writes an audit entry.
        /// </summary>
        /// <param name="component">Domain component to create.</param>
        /// <param name="userId">Acting user id (audit trail).</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="component"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">If validation fails.</exception>
        public async Task CreateAsync(Component component, int userId, CancellationToken cancellationToken = default)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            ValidateComponent(component);

            component.DigitalSignature = GenerateDigitalSignature(component, DateTime.UtcNow);
            component.LastModified = DateTime.UtcNow;
            component.LastModifiedById = userId;

            // Call the domain overload (avoids MachineComponent binding issues).
            await _db.InsertOrUpdateComponentAsync(
                component,
                false,                 // update?
                userId,                // actorUserId
                "system",              // ip
                cancellationToken      // token
            ).ConfigureAwait(false);

            await _audit.LogEntityAuditAsync(
                "components",
                component.Id,
                "CREATE",
                $"Created component: MachineID={component.MachineId}, Name={component.Name}, By UserID={userId}"
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates an existing component, regenerates the digital signature, persists changes, and writes an audit entry.
        /// </summary>
        /// <param name="component">Domain component to update.</param>
        /// <param name="userId">Acting user id (audit trail).</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="component"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">If validation fails.</exception>
        public async Task UpdateAsync(Component component, int userId, CancellationToken cancellationToken = default)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            ValidateComponent(component);

            component.DigitalSignature = GenerateDigitalSignature(component, DateTime.UtcNow);
            component.LastModified = DateTime.UtcNow;
            component.LastModifiedById = userId;

            await _db.InsertOrUpdateComponentAsync(
                component,
                true,                  // update?
                userId,                // actorUserId
                "system",              // ip
                cancellationToken      // token
            ).ConfigureAwait(false);

            await _audit.LogEntityAuditAsync(
                "components",
                component.Id,
                "UPDATE",
                $"Updated component: MachineID={component.MachineId}, Name={component.Name}, By UserID={userId}"
            ).ConfigureAwait(false);
        }

        /// <summary>Deletes a component and writes an audit entry.</summary>
        /// <param name="componentId">Primary key of the component to delete.</param>
        /// <param name="userId">Acting user id (for audit trail).</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        public async Task DeleteAsync(int componentId, int userId, CancellationToken cancellationToken = default)
        {
            // Target the device-context delete overload (id, actorUserId, ip, deviceInfo, token)
            await _db.DeleteComponentAsync(
                componentId,
                userId,            // actorUserId
                "system",          // ip
                "server",          // deviceInfo
                cancellationToken  // token
            ).ConfigureAwait(false);

            await _audit.LogEntityAuditAsync(
                "components",
                componentId,
                "DELETE",
                $"Deleted component ID={componentId}, By UserID={userId}"
            ).ConfigureAwait(false);
        }

        #endregion

        #region === Validation =================================================================

        /// <summary>Validates required fields for a component in accordance with GMP/Annex 11.</summary>
        private static void ValidateComponent(Component c)
        {
            if (string.IsNullOrWhiteSpace(c.Name))
                throw new InvalidOperationException("❌ Component name is required.");
            if (string.IsNullOrWhiteSpace(c.Code))
                throw new InvalidOperationException("❌ Component code is required.");
            if (c.MachineId <= 0)
                throw new InvalidOperationException("❌ Component must be linked to an existing machine.");
            if (string.IsNullOrWhiteSpace(c.SopDoc))
                throw new InvalidOperationException("⚠️ SOP document is required for GMP compliance.");
        }

        #endregion

        #region === Digital Signature ===========================================================

        /// <summary>Generates a SHA-256 based digital signature for the supplied component and timestamp.</summary>
        private static string GenerateDigitalSignature(Component c, DateTime utc)
        {
            string raw = $"{c.Id}|{c.MachineId}|{c.Name}|{c.SopDoc}|{utc:O}";
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        #endregion

        #region === Extensibility Hooks =========================================================

        /// <summary>Simple risk score (illustrative).</summary>
        public int CalculateRiskScore(Component c) => string.IsNullOrWhiteSpace(c.SopDoc) ? 90 : 10;

        /// <summary>Links a CAPA record (audit trail only).</summary>
        public async Task LinkCapaRecordAsync(int componentId, int capaId)
        {
            await _audit.LogEntityAuditAsync(
                "components",
                componentId,
                "CAPA_LINK",
                $"Linked CAPA ID={capaId} to component ID={componentId}"
            ).ConfigureAwait(false);
        }

        #endregion
    }
}
