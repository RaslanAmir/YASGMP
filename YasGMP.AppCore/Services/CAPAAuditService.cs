using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.Enums;
using YasGMP.Services.Interfaces;
using MySqlConnector;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>CapaAuditService</b> – Ultra robust, GMP/21 CFR Part 11 compliant CAPA audit service.
    /// Provides full digital signature, audit trail, advanced querying, and data integrity checks.
    /// </summary>
    public class CapaAuditService : ICapaAuditService
    {
        private readonly DatabaseService _db;

        /// <summary>
        /// Initializes a new instance of <see cref="CapaAuditService"/>.
        /// </summary>
        /// <param name="databaseService">Database service dependency.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="databaseService"/> is <c>null</c>.</exception>
        public CapaAuditService(DatabaseService databaseService)
        {
            _db = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <summary>
        /// Inserts a new CAPA audit log entry with digital signature.
        /// </summary>
        /// <param name="audit">CAPA audit object.</param>
        public async Task CreateAsync(CapaAudit audit)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));
            if (string.IsNullOrWhiteSpace(audit.Details)) throw new InvalidOperationException("Detalji audita su obavezni.");
            if (!Enum.IsDefined(typeof(CapaActionType), audit.Action)) throw new InvalidOperationException("Nevažeći tip CAPA akcije.");

            audit.ChangedAt = DateTime.UtcNow;
            audit.DigitalSignature = GenerateDigitalSignature(audit);

            const string sql = @"INSERT INTO capa_audit_log /* ANALYZER_IGNORE: audit table */ 
                           (capa_id, user_id, action, details, changed_at, digital_signature) 
                           VALUES (@capa, @user, @action, @details, @changed, @sig)";

            var pars = new MySqlParameter[]
            {
                new("@capa", audit.CapaId),
                new("@user", audit.UserId),
                new("@action", audit.Action.ToString()),
                new("@details", audit.Details ?? string.Empty),
                new("@changed", audit.ChangedAt),
                new("@sig", audit.DigitalSignature ?? string.Empty)
            };
            await _db.ExecuteNonQueryAsync(sql, pars);
        }

        /// <summary>
        /// Updates an existing CAPA audit log entry.
        /// </summary>
        /// <param name="audit">CAPA audit object.</param>
        public async Task UpdateAsync(CapaAudit audit)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));
            audit.ChangedAt = DateTime.UtcNow;
            audit.DigitalSignature = GenerateDigitalSignature(audit);

            const string sql = @"UPDATE capa_audit_log /* ANALYZER_IGNORE: audit table */ SET 
                            capa_id=@capa, user_id=@user, action=@action, details=@details, changed_at=@changed, digital_signature=@sig
                            WHERE id=@id";

            var pars = new MySqlParameter[]
            {
                new("@id", audit.Id),
                new("@capa", audit.CapaId),
                new("@user", audit.UserId),
                new("@action", audit.Action.ToString()),
                new("@details", audit.Details ?? string.Empty),
                new("@changed", audit.ChangedAt),
                new("@sig", audit.DigitalSignature ?? string.Empty)
            };
            await _db.ExecuteNonQueryAsync(sql, pars);
        }

        /// <summary>
        /// Deletes a CAPA audit log entry by ID.
        /// </summary>
        /// <param name="id">Audit log ID.</param>
        public async Task DeleteAsync(int id)
        {
            const string sql = @"DELETE FROM capa_audit_log /* ANALYZER_IGNORE: audit table */ WHERE id=@id";
            var pars = new MySqlParameter[] { new("@id", id) };
            await _db.ExecuteNonQueryAsync(sql, pars);
        }

        /// <summary>
        /// Gets a single audit log by its ID.
        /// </summary>
        /// <param name="id">Audit log ID.</param>
        /// <returns>The <see cref="CapaAudit"/> if found; otherwise throws.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the record is not found.</exception>
        public async Task<CapaAudit> GetByIdAsync(int id)
        {
            var result = await _db.GetCapaAuditByIdAsync(id);
            if (result == null)
                throw new InvalidOperationException($"CapaAudit with ID={id} was not found.");
            return result;
        }

        /// <summary>
        /// Gets all audits for a specific CAPA by CAPA ID.
        /// </summary>
        /// <param name="capaId">CAPA ID.</param>
        public async Task<IReadOnlyList<CapaAudit>> GetByCapaIdAsync(int capaId)
        {
            var list = await _db.GetCapaAuditsByCapaIdAsync(capaId);
            return list.AsReadOnly();
        }

        /// <summary>
        /// Gets all audits for a specific user.
        /// </summary>
        /// <param name="userId">User ID.</param>
        public async Task<IReadOnlyList<CapaAudit>> GetByUserIdAsync(int userId)
        {
            var list = await _db.GetCapaAuditsByUserIdAsync(userId);
            return list.AsReadOnly();
        }

        /// <summary>
        /// Gets all audits for a specific action type.
        /// </summary>
        /// <param name="actionType">CAPA action type.</param>
        public async Task<IReadOnlyList<CapaAudit>> GetByActionTypeAsync(CapaActionType actionType)
        {
            var list = await _db.GetCapaAuditsByActionAsync(actionType);
            return list.AsReadOnly();
        }

        /// <summary>
        /// Gets all audits in a specific date range.
        /// </summary>
        /// <param name="from">Start date (inclusive).</param>
        /// <param name="to">End date (inclusive).</param>
        public async Task<IReadOnlyList<CapaAudit>> GetByDateRangeAsync(DateTime from, DateTime to)
        {
            var list = await _db.GetCapaAuditsByDateRangeAsync(from, to);
            return list.AsReadOnly();
        }

        /// <summary>
        /// Validates the digital signature of an audit entry.
        /// </summary>
        /// <param name="audit">CAPA audit object.</param>
        /// <returns><c>true</c> if the entry's digital signature matches; otherwise <c>false</c>.</returns>
        public bool ValidateIntegrity(CapaAudit audit)
        {
            if (audit == null) return false;
            var expected = GenerateDigitalSignature(audit);
            return string.Equals(audit.DigitalSignature, expected, StringComparison.Ordinal);
        }

        /// <summary>
        /// Generates a digital signature for a CAPA audit log entry.
        /// </summary>
        /// <param name="audit">CAPA audit object.</param>
        /// <returns>Digital signature string (Base64 SHA-256).</returns>
        private static string GenerateDigitalSignature(CapaAudit audit)
        {
            string raw = $"{audit.Id}|{audit.CapaId}|{audit.Action}|{audit.ChangedAt:O}";
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        /// <summary>
        /// Generates a digital signature for a string payload (utility).
        /// </summary>
        /// <param name="payload">String to sign.</param>
        /// <returns>Digital signature string (Base64 SHA-256).</returns>
        private static string GenerateDigitalSignature(string payload)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes($"{payload}|{Guid.NewGuid()}")));
        }
    }
}
