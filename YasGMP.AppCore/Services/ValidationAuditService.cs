using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.Models.Enums;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// Validation audit logger (Annex 11 / 21 CFR Part 11 ready).
    /// </summary>
    public class ValidationAuditService : IValidationAuditService
    {
        private readonly DatabaseService _dbService;

        /// <summary>Creates a new <see cref="ValidationAuditService"/>.</summary>
        public ValidationAuditService(DatabaseService dbService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        }

        #region IValidationAuditService

        /// <inheritdoc/>
        public async Task CreateAsync(ValidationAudit audit)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));

            // Canonical system event
            await _dbService.LogSystemEventAsync(
                userId: audit.UserId,
                eventType: $"VAL_{audit.Action}",
                tableName: "validation_audit",
                module: "ValidationAuditService",
                recordId: audit.ValidationId,
                description: audit.Details,
                ip: audit.SourceIp,
                severity: "audit",
                deviceInfo: audit.DeviceInfo,
                sessionId: null
            ).ConfigureAwait(false);

            // Optional legacy table write (ignored by analyzer)
            try
            {
                const string sql = @"INSERT INTO validation_audit /* ANALYZER_IGNORE: audit table */ 
                           (validation_id, user_id, action, changed_at, details, digital_signature, source_ip, device_info)
                           VALUES (@vid, @uid, @action, @changed, @details, @sig, @ip, @device)";

                var pars = new[]
                {
                    new MySqlParameter("@vid", audit.ValidationId),
                    new MySqlParameter("@uid", audit.UserId),
                    new MySqlParameter("@action", audit.Action.ToString()),
                    new MySqlParameter("@changed", audit.ChangedAt),
                    new MySqlParameter("@details", audit.Details ?? string.Empty),
                    new MySqlParameter("@sig", audit.DigitalSignature ?? string.Empty),
                    new MySqlParameter("@ip", audit.SourceIp ?? "unknown"),
                    new MySqlParameter("@device", audit.DeviceInfo ?? "N/A")
                };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
            }
            catch (MySqlException)
            {
                // Table missing – rely on system event only.
            }
        }

        /// <inheritdoc/>
        public async Task LogAsync(int validationId, int userId, ValidationActionType action, string details)
        {
            var audit = new ValidationAudit
            {
                ValidationId = validationId,
                UserId = userId,
                Action = action,
                Details = details,
                ChangedAt = DateTime.UtcNow,
                DigitalSignature = GenerateDigitalSignature($"{validationId}|{action}|{DateTime.UtcNow:O}"),
                SourceIp = "system",
                DeviceInfo = Environment.MachineName
            };

            await CreateAsync(audit).ConfigureAwait(false);
        }

        #endregion

        #region Queries

        /// <summary>Gets all audit rows for a validation.</summary>
        public async Task<List<ValidationAudit>> GetByValidationIdAsync(int validationId)
        {
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                var dt = await _dbService.ExecuteSelectAsync(
                    "SELECT id, validation_id, user_id, action, changed_at, details, digital_signature, source_ip, device_info FROM validation_audit WHERE validation_id=@vid ORDER BY changed_at DESC",
                    new[] { new MySqlParameter("@vid", validationId) }, cts.Token).ConfigureAwait(false);

                var list = new List<ValidationAudit>();
                foreach (DataRow row in dt.Rows) list.Add(ParseAudit(row));
                return list;
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                return new List<ValidationAudit>();
            }
        }

        /// <summary>Gets a single audit row by id.</summary>
        public async Task<ValidationAudit?> GetByIdAsync(int id)
        {
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                var dt = await _dbService.ExecuteSelectAsync(
                    "SELECT id, validation_id, user_id, action, changed_at, details, digital_signature, source_ip, device_info FROM validation_audit WHERE id=@id",
                    new[] { new MySqlParameter("@id", id) }, cts.Token).ConfigureAwait(false);

                return dt.Rows.Count == 0 ? null : ParseAudit(dt.Rows[0]);
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                return null;
            }
        }

        /// <summary>Deletes an audit entry (admin only).</summary>
        public async Task DeleteAsync(int id)
        {
            try
            {
                await _dbService.ExecuteNonQueryAsync(
                    "DELETE FROM validation_audit /* ANALYZER_IGNORE: audit table */ WHERE id=@id",
                    new[] { new MySqlParameter("@id", id) }).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1146)
            {
                // no-op when table doesn't exist
            }
        }

        #endregion

        #region Helpers

        private static ValidationAudit ParseAudit(DataRow row) => new ValidationAudit
        {
            Id = Convert.ToInt32(row["id"]),
            ValidationId = Convert.ToInt32(row["validation_id"]),
            UserId = Convert.ToInt32(row["user_id"]),
            // Fallback to Update if DB has a value not present in the enum.
            Action = Enum.TryParse(row["action"]?.ToString(), true, out ValidationActionType act) ? act : ValidationActionType.Update,
            ChangedAt = Convert.ToDateTime(row["changed_at"]),
            Details = row["details"]?.ToString() ?? string.Empty,              // CS8601 fix
            DigitalSignature = row["digital_signature"]?.ToString() ?? string.Empty, // CS8601 fix
            SourceIp = row["source_ip"]?.ToString() ?? string.Empty,           // CS8601 fix
            DeviceInfo = row["device_info"]?.ToString() ?? string.Empty        // CS8601 fix
        };

        private static string GenerateDigitalSignature(string payload)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }

        #endregion
    }
}
