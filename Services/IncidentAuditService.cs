using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>IncidentAuditService</b> – robust service for managing incident audit records.
    /// <para>
    /// • Creates signed audit entries (SHA-256).<br/>
    /// • Reads by Id, by Incident, and by User.<br/>
    /// • 100% traceable (Annex 11 / 21 CFR Part 11 friendly).
    /// </para>
    /// </summary>
    public class IncidentAuditService : IIncidentAuditService
    {
        private readonly DatabaseService _db;

        /// <summary>
        /// Initializes a new instance of <see cref="IncidentAuditService"/>.
        /// </summary>
        /// <param name="databaseService">Database service used for persistence.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="databaseService"/> is <c>null</c>.</exception>
        public IncidentAuditService(DatabaseService databaseService)
        {
            _db = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <summary>
        /// Creates a new incident audit entry. If <see cref="IncidentAudit.ActionAt"/> is not set,
        /// it is initialized to <see cref="DateTime.UtcNow"/>. If <see cref="IncidentAudit.DigitalSignature"/> is
        /// not provided, it is computed automatically from salient fields.
        /// </summary>
        /// <param name="audit">Audit payload to persist.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="audit"/> is <c>null</c>.</exception>
        public async Task CreateAsync(IncidentAudit audit)
        {
            if (audit is null) throw new ArgumentNullException(nameof(audit));

            // Ensure action timestamp and signature are present
            if (audit.ActionAt == default) audit.ActionAt = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(audit.DigitalSignature))
                audit.DigitalSignature = GenerateDigitalSignature(audit);

            await _db.InsertIncidentAuditAsync(audit).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a single audit entry by its identifier.
        /// </summary>
        /// <param name="id">Audit id.</param>
        /// <returns>The audit entry, or <c>null</c> if not found.</returns>
        public Task<IncidentAudit?> GetByIdAsync(int id)
            => _db.GetIncidentAuditByIdAsync(id);

        /// <summary>
        /// Gets audit entries for a specific incident id (newer first).
        /// </summary>
        /// <param name="incidentId">Incident id.</param>
        /// <returns>List of audit entries.</returns>
        public Task<List<IncidentAudit>> GetByIncidentIdAsync(int incidentId)
            => _db.GetIncidentAuditsByIncidentIdAsync(incidentId);

        /// <summary>
        /// Gets audit entries performed by a specific user (newer first).
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <returns>List of audit entries.</returns>
        public Task<List<IncidentAudit>> GetByUserIdAsync(int userId)
            => _db.GetIncidentAuditsByUserIdAsync(userId);

        /// <summary>
        /// Computes a deterministic SHA-256 signature over the audit record’s salient fields.
        /// </summary>
        /// <param name="audit">Audit entry.</param>
        /// <returns>Base64 encoded signature.</returns>
        private static string GenerateDigitalSignature(IncidentAudit audit)
        {
            // Build a stable string payload
            string raw =
                $"{audit.Id}|{audit.IncidentId}|{audit.UserId}|{audit.Action}|{audit.OldValue}|{audit.NewValue}|{audit.ActionAt:O}|{audit.SourceIp}|{audit.CapaId}|{audit.WorkOrderId}";

            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(raw);
            return Convert.ToBase64String(sha.ComputeHash(bytes));
        }
    }
}
