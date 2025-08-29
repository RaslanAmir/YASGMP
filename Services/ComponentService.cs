using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Helpers;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>ComponentService</b> – Ultra robust, GMP-compliant service for managing machine components.
    /// <para>
    /// ✅ Full CRUD, deep validation, digital signatures, and audit logging.<br/>
    /// ✅ All actions logged via <see cref="AuditService.LogEntityAuditAsync"/> for full traceability and 21 CFR Part 11/Annex 11 compliance.<br/>
    /// ✅ Extensible: CAPA link, risk scoring, bonus hooks.
    /// </para>
    /// </summary>
    public class ComponentService
    {
        #region === Fields & Constructor ===

        private readonly DatabaseService _db;
        private readonly AuditService _audit;

        /// <summary>
        /// Initializes <see cref="ComponentService"/> with required dependencies.
        /// </summary>
        /// <param name="databaseService">Injected database service (must not be null).</param>
        /// <param name="auditService">Injected audit service (must not be null).</param>
        /// <exception cref="ArgumentNullException">Thrown if any dependency is null.</exception>
        public ComponentService(DatabaseService databaseService, AuditService auditService)
        {
            _db = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _audit = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        #endregion

        #region === CRUD Operations ===

        /// <summary>
        /// Fetches all components from the database (raw list, no filters).
        /// </summary>
        /// <returns>List of <see cref="Component"/>.</returns>
        public async Task<List<Component>> GetAllAsync()
            => ComponentMapper.ToComponentList(await _db.GetAllComponentsAsync());

        /// <summary>
        /// Fetches a component by its unique ID.
        /// </summary>
        /// <param name="id">Component ID.</param>
        /// <returns>The found <see cref="Component"/> or <c>null</c> if not found.</returns>
        public async Task<Component?> GetByIdAsync(int id)
        {
            var mc = await _db.GetComponentByIdAsync(id);
            return mc == null ? null : ComponentMapper.ToComponent(mc);
        }

        /// <summary>
        /// Creates a new component and logs an audit entry.
        /// </summary>
        /// <param name="component">The <see cref="Component"/> to create.</param>
        /// <param name="userId">User ID performing the action (for audit and as actor).</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="component"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if validation fails.</exception>
        public async Task CreateAsync(Component component, int userId, CancellationToken cancellationToken = default)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            ValidateComponent(component);

            component.DigitalSignature = GenerateDigitalSignature(component, DateTime.UtcNow);
            component.LastModified = DateTime.UtcNow;
            component.LastModifiedById = userId;

            // Align with DatabaseService: update/actorUserId/ip/token (no sessionId/note)
            await _db.InsertOrUpdateComponentAsync(
                ComponentMapper.ToMachineComponent(component),
                update: false,
                actorUserId: userId,
                ip: "system",
                token: cancellationToken
            );

            await _audit.LogEntityAuditAsync(
                "components",
                component.Id,
                "CREATE",
                $"Created component: MachineID={component.MachineId}, Name={component.Name}, By UserID={userId}"
            );
        }

        /// <summary>
        /// Updates a component and logs an audit entry.
        /// </summary>
        /// <param name="component">The <see cref="Component"/> to update.</param>
        /// <param name="userId">User ID performing the update.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="component"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if validation fails.</exception>
        public async Task UpdateAsync(Component component, int userId, CancellationToken cancellationToken = default)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            ValidateComponent(component);

            component.DigitalSignature = GenerateDigitalSignature(component, DateTime.UtcNow);
            component.LastModified = DateTime.UtcNow;
            component.LastModifiedById = userId;

            await _db.InsertOrUpdateComponentAsync(
                ComponentMapper.ToMachineComponent(component),
                update: true,
                actorUserId: userId,
                ip: "system",
                token: cancellationToken
            );

            await _audit.LogEntityAuditAsync(
                "components",
                component.Id,
                "UPDATE",
                $"Updated component: MachineID={component.MachineId}, Name={component.Name}, By UserID={userId}"
            );
        }

        /// <summary>
        /// Deletes a component and logs an audit entry.
        /// </summary>
        /// <param name="componentId">ID of the component to delete.</param>
        /// <param name="userId">User ID performing the delete.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        public async Task DeleteAsync(int componentId, int userId, CancellationToken cancellationToken = default)
        {
            await _db.DeleteComponentAsync(componentId, actorUserId: userId, ip: "system", token: cancellationToken);

            await _audit.LogEntityAuditAsync(
                "components",
                componentId,
                "DELETE",
                $"Deleted component ID={componentId}, By UserID={userId}"
            );
        }

        #endregion

        #region === Validation ===

        /// <summary>
        /// Validates all required GMP/Annex 11 fields for a component.
        /// Throws exception on any invalid/missing field.
        /// </summary>
        /// <param name="c">Component to validate.</param>
        /// <exception cref="InvalidOperationException">Thrown if validation fails.</exception>
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

        #region === Digital Signature ===

        /// <summary>
        /// Generates a digital signature (SHA-256) for a component and timestamp.
        /// </summary>
        /// <param name="c">Component to sign.</param>
        /// <param name="utc">Timestamp (UTC) to ensure consistent signatures for update/audit.</param>
        /// <returns>Base64 string digital signature.</returns>
        private static string GenerateDigitalSignature(Component c, DateTime utc)
        {
            string raw = $"{c.Id}|{c.MachineId}|{c.Name}|{c.SopDoc}|{utc:O}";
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        #endregion

        #region === Future Extensibility Hooks ===

        /// <summary>
        /// Calculates a risk score for a component (future extensibility, placeholder logic).
        /// </summary>
        /// <param name="c">Component to score.</param>
        /// <returns>Risk score (int).</returns>
        public int CalculateRiskScore(Component c)
            => string.IsNullOrWhiteSpace(c.SopDoc) ? 90 : 10;

        /// <summary>
        /// Links a CAPA record to a component (audit trail only; real implementation in future).
        /// </summary>
        /// <param name="componentId">Component ID.</param>
        /// <param name="capaId">CAPA record ID.</param>
        /// <returns>Task representing the async operation.</returns>
        public async Task LinkCapaRecordAsync(int componentId, int capaId)
        {
            await _audit.LogEntityAuditAsync(
                "components",
                componentId,
                "CAPA_LINK",
                $"Linked CAPA ID={capaId} to component ID={componentId}"
            );
        }

        #endregion
    }
}
