using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.Enums;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// Validation domain service that encapsulates CRUD operations, signature handling,
    /// and audit logging for the <see cref="Validation"/> aggregate.
    /// </summary>
    public class ValidationService
    {
        private readonly DatabaseService _db;
        private readonly IValidationAuditService _audit;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationService"/> class.
        /// </summary>
        /// <param name="databaseService">Database abstraction.</param>
        /// <param name="auditService">Validation audit writer.</param>
        /// <exception cref="ArgumentNullException">Thrown if a dependency is null.</exception>
        public ValidationService(DatabaseService databaseService, IValidationAuditService auditService)
        {
            _db = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _audit = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        #region CRUD

        /// <summary>Retrieves all validations.</summary>
        public async Task<List<Validation>> GetAllAsync() =>
            await _db.GetAllValidationsAsync().ConfigureAwait(false);

        /// <summary>
        /// Retrieves a single validation by its identifier.
        /// Throws <see cref="KeyNotFoundException"/> if the record is missing (eliminates CS8603).
        /// </summary>
        public async Task<Validation> GetByIdAsync(int id)
        {
            var v = await _db.GetValidationByIdAsync(id).ConfigureAwait(false);
            if (v == null) throw new KeyNotFoundException($"Validation #{id} not found.");
            return v;
        }

        /// <summary>Creates a new validation and records an audit entry.</summary>
        public async Task CreateAsync(Validation validation, int userId)
        {
            Validate(validation);
            EnsureDigitalSignature(validation);
            await _db.InsertOrUpdateValidationAsync(validation, update: false).ConfigureAwait(false);

            await LogAudit(
                validation.Id,
                userId,
                ValidationActionType.Create,
                $"Created validation {validation.Type} for machine {validation.MachineId}"
            ).ConfigureAwait(false);
        }

        /// <summary>Updates an existing validation and records an audit entry.</summary>
        public async Task UpdateAsync(Validation validation, int userId)
        {
            Validate(validation);
            EnsureDigitalSignature(validation);
            await _db.InsertOrUpdateValidationAsync(validation, update: true).ConfigureAwait(false);

            await LogAudit(
                validation.Id,
                userId,
                ValidationActionType.Update,
                $"Updated validation ID={validation.Id}"
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// Marks a validation as executed, computes next due date, and records an audit entry.
        /// </summary>
        public async Task MarkExecutedAsync(int validationId, int userId)
        {
            var val = await _db.GetValidationByIdAsync(validationId).ConfigureAwait(false);
            if (val == null) throw new InvalidOperationException("Validation not found.");

            val.Status = "EXECUTED";
            val.DateEnd = DateTime.UtcNow;
            val.NextDue = CalculateNextDue(DateTime.UtcNow, "1y");
            EnsureDigitalSignature(val, forceRefresh: true);

            await _db.InsertOrUpdateValidationAsync(val, update: true).ConfigureAwait(false);

            await LogAudit(
                val.Id,
                userId,
                ValidationActionType.Execute,
                $"Executed validation ID={val.Id}"
            ).ConfigureAwait(false);
        }

        /// <summary>Deletes a validation and records an audit entry.</summary>
        public async Task DeleteAsync(int validationId, int userId)
        {
            await _db.DeleteValidationAsync(validationId).ConfigureAwait(false);

            await LogAudit(
                validationId,
                userId,
                ValidationActionType.Delete,
                $"Deleted validation ID={validationId}"
            ).ConfigureAwait(false);
        }

        #endregion

        #region Status & Monitoring

        /// <summary>Returns validations with NextDue in the past.</summary>
        public async Task<List<Validation>> GetExpiredValidationsAsync()
        {
            var list = await _db.GetAllValidationsAsync().ConfigureAwait(false);
            return list.Where(v => v.NextDue != null && v.NextDue < DateTime.UtcNow).ToList();
        }

        /// <summary>Returns validations due within <paramref name="days"/> days.</summary>
        public async Task<List<Validation>> GetDueSoonValidationsAsync(int days = 30)
        {
            var list = await _db.GetAllValidationsAsync().ConfigureAwait(false);
            return list.Where(v =>
                v.NextDue != null &&
                v.NextDue <= DateTime.UtcNow.AddDays(days) &&
                v.NextDue > DateTime.UtcNow).ToList();
        }

        #endregion

        #region Domain validation

        private static void Validate(Validation val)
        {
            if (val == null) throw new ArgumentNullException(nameof(val));
            if (string.IsNullOrWhiteSpace(val.Type))
                throw new InvalidOperationException("Validation type is required.");
            if (val.MachineId == null && val.ComponentId == null)
                throw new InvalidOperationException("Validation must be associated with a machine or component.");
        }

        private static DateTime? CalculateNextDue(DateTime? baseDate, string interval)
        {
            DateTime start = baseDate ?? DateTime.UtcNow;
            return interval.ToLower() switch
            {
                var f when f.EndsWith("m") && int.TryParse(f[..^1], out int months) => start.AddMonths(months),
                var f when f.EndsWith("y") && int.TryParse(f[..^1], out int years)  => start.AddYears(years),
                var f when f.EndsWith("d") && int.TryParse(f[..^1], out int days)   => start.AddDays(days),
                _ => start.AddYears(1)
            };
        }

        #endregion

        #region Signatures

        private static void EnsureDigitalSignature(Validation val, bool forceRefresh = false)
        {
            if (val == null) throw new ArgumentNullException(nameof(val));
            if (forceRefresh || string.IsNullOrWhiteSpace(val.DigitalSignature))
            {
                val.DigitalSignature = GenerateDigitalSignature(val);
            }
        }

        private static string GenerateDigitalSignature(Validation val)
        {
            string raw = $"{val.Id}|{val.Type}|{val.MachineId}|{val.ComponentId}|{val.Status}|{DateTime.UtcNow:O}";
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        private static string GenerateDigitalSignature(string payload)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes($"{payload}|{Guid.NewGuid()}")));
        }

        #endregion

        #region Audit

        /// <summary>Writes a row to the validation audit sink using the provided <see cref="IValidationAuditService"/>.</summary>
        private async Task LogAudit(int validationId, int userId, ValidationActionType action, string details)
        {
            await _audit.CreateAsync(new ValidationAudit
            {
                ValidationId = validationId,
                UserId = userId,
                Action = action,
                Details = details,
                ChangedAt = DateTime.UtcNow,
                DigitalSignature = GenerateDigitalSignature(details)
            }).ConfigureAwait(false);
        }

        #endregion

        #region Future hooks

        public Task<double> PredictValidationFailureRiskAsync(int validationId) =>
            Task.FromResult(new Random().NextDouble());

        public Task<string> GetIoTSensorDataAsync(int validationId) =>
            Task.FromResult("IoT Data: Device Calibration OK");

        #endregion
    }
}

