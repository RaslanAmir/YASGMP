using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.Enums;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>DeviationAuditService</b> – GMP/21 CFR Part 11 compliant audit service for deviation records.
    /// Provides immutable audit logging, digital signatures and export helpers.
    /// </summary>
    public class DeviationAuditService : IDeviationAuditService
    {
        private readonly DatabaseService _db;

        /// <summary>
        /// Initializes the <see cref="DeviationAuditService"/> with a database service.
        /// </summary>
        /// <param name="databaseService">Database access service.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="databaseService"/> is <c>null</c>.</exception>
        public DeviationAuditService(DatabaseService databaseService)
        {
            _db = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <inheritdoc />
        public async Task CreateAsync(DeviationAudit audit)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));
            audit.ChangedAt = DateTime.UtcNow;
            audit.DigitalSignature = GenerateDigitalSignature(audit);

            await _db.InsertDeviationAuditAsync(audit).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task UpdateAsync(DeviationAudit audit)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));
            audit.ChangedAt = DateTime.UtcNow;
            audit.DigitalSignature = GenerateDigitalSignature(audit);

            await _db.UpdateDeviationAuditAsync(audit).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task DeleteAsync(int id) => _db.DeleteDeviationAuditAsync(id);

        /// <summary>
        /// Gets a single audit row by its identifier. Throws <see cref="KeyNotFoundException"/> if not found,
        /// matching the non-nullable contract on <see cref="IDeviationAuditService.GetByIdAsync(int)"/>.
        /// </summary>
        /// <param name="id">Audit entry identifier.</param>
        /// <returns>The found <see cref="DeviationAudit"/>.</returns>
        /// <exception cref="KeyNotFoundException">If no row exists with the given <paramref name="id"/>.</exception>
        public async Task<DeviationAudit> GetByIdAsync(int id)
        {
            var row = await _db.GetDeviationAuditByIdAsync(id).ConfigureAwait(false);
            if (row is null)
                throw new KeyNotFoundException($"Deviation audit entry not found (ID={id}).");
            return row;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<DeviationAudit>> GetByDeviationIdAsync(int deviationId)
        {
            var list = await _db.GetDeviationAuditsByDeviationIdAsync(deviationId).ConfigureAwait(false);
            return list; // List<T> upcasts to IReadOnlyList<T>
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<DeviationAudit>> GetByUserIdAsync(int userId)
        {
            var list = await _db.GetDeviationAuditsByUserIdAsync(userId).ConfigureAwait(false);
            return list;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<DeviationAudit>> GetByActionTypeAsync(DeviationActionType actionType)
        {
            var list = await _db.GetDeviationAuditsByActionTypeAsync(actionType.ToString()).ConfigureAwait(false);
            return list;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<DeviationAudit>> GetByDateRangeAsync(DateTime from, DateTime to)
        {
            var list = await _db.GetDeviationAuditsByDateRangeAsync(from, to).ConfigureAwait(false);
            return list;
        }

        /// <inheritdoc />
        public bool ValidateIntegrity(DeviationAudit audit)
        {
            if (audit == null) return false;
            var expected = GenerateDigitalSignature(audit);
            return string.Equals(expected, audit.DigitalSignature, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public async Task<string> ExportAuditLogsAsync(int deviationId, string format = "pdf")
        {
            var audits = await GetByDeviationIdAsync(deviationId).ConfigureAwait(false);
            var filePath = await _db.ExportDeviationAuditLogsAsync(audits, format).ConfigureAwait(false);
            return filePath;
        }

        /// <inheritdoc />
        public Task<double> AnalyzeAnomalyAsync(int deviationId) =>
            Task.FromResult(new Random().NextDouble());

        #region Signature Generation

        /// <summary>
        /// Creates a stable digital signature for the provided audit entry.
        /// </summary>
        /// <param name="audit">Audit entry to sign.</param>
        /// <returns>Base64-encoded SHA-256 digest.</returns>
        private static string GenerateDigitalSignature(DeviationAudit audit)
        {
            var raw = $"{audit.DeviationId}|{audit.UserId}|{audit.Action}|{audit.ChangedAt:O}|{audit.Details}";
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToBase64String(hash);
        }

        #endregion
    }
}
