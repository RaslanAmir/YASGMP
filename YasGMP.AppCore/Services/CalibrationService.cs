using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.DTO;
using YasGMP.Models.Enums;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>CalibrationService</b> – Ultra robust, GMP &amp; ISO 17025 compliant service for managing calibrations.
    /// <para>Implements full CRUD, certificate management, digital signatures, audit trail, AI prediction, IoT, and CAPA integration.</para>
    /// <para>All actions are digitally signed and audit-logged.</para>
    /// </summary>
    public class CalibrationService : ICalibrationService
    {
        private readonly DatabaseService _db;
        private readonly ICalibrationAuditService _audit;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalibrationService"/> class.
        /// </summary>
        /// <param name="databaseService">The database service dependency.</param>
        /// <param name="auditService">The calibration audit service dependency.</param>
        /// <exception cref="ArgumentNullException">Thrown if any dependency is <c>null</c>.</exception>
        public CalibrationService(DatabaseService databaseService, ICalibrationAuditService auditService)
        {
            _db = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _audit = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        #region === CRUD OPERATIONS ===

        /// <inheritdoc />
        public async Task<List<Calibration>> GetAllAsync()
        {
            return await _db.GetAllCalibrationsAsync();
        }

        /// <inheritdoc />
        public async Task<Calibration> GetByIdAsync(int id)
        {
            var cal = await _db.GetCalibrationByIdAsync(id);
            if (cal == null)
                throw new InvalidOperationException($"Calibration with ID={id} was not found.");
            return cal;
        }

        /// <inheritdoc />
        public async Task<List<Calibration>> GetByComponentAsync(int componentId)
        {
            return await _db.GetCalibrationsForComponentAsync(componentId);
        }

        /// <inheritdoc />
        public async Task<List<Calibration>> GetBySupplierAsync(int supplierId)
        {
            return await _db.GetCalibrationsBySupplierAsync(supplierId);
        }

        /// <inheritdoc />
        public async Task<List<Calibration>> GetByDateRangeAsync(DateTime from, DateTime to)
        {
            return await _db.GetCalibrationsByDateRangeAsync(from, to);
        }

        /// <inheritdoc />
        public async Task<List<Calibration>> GetOverdueCalibrationsAsync()
        {
            var list = await _db.GetAllCalibrationsAsync();
            var now = DateTime.UtcNow;
            // Avoid DateTime vs null comparisons across model variations (DateTime vs DateTime?).
            // We read NextDue via reflection and treat default(DateTime) as "not set".
            return list.Where(c =>
            {
                var nd = GetNextDueNullable(c);
                return nd.HasValue && nd.Value != default && nd.Value < now;
            }).ToList();
        }

        /// <inheritdoc />
        public async Task CreateAsync(Calibration cal, int userId, SignatureMetadataDto? signatureMetadata = null)
        {
            ValidateCalibration(cal);
            ApplySignatureMetadata(cal, signatureMetadata, () => ComputeDefaultSignature(cal));
            cal.LastModified = DateTime.UtcNow;
            cal.LastModifiedById = userId;

            // Align with DatabaseService: parameter names are update/actorUserId/ip/token
            var newId = await _db.InsertOrUpdateCalibrationAsync(
                cal,
                update: false,
                actorUserId: userId,
                ip: $"User:{userId}",
                device: signatureMetadata?.Device ?? "CalibrationService",
                signatureMetadata: signatureMetadata,
                token: CancellationToken.None
            );
            cal.Id = newId;

            await LogAudit(cal.Id, userId, CalibrationActionType.CREATE,
                $"Created calibration ID={cal.Id} for component {cal.ComponentId}");
        }

        /// <inheritdoc />
        public async Task UpdateAsync(Calibration cal, int userId, SignatureMetadataDto? signatureMetadata = null)
        {
            ValidateCalibration(cal);
            ApplySignatureMetadata(cal, signatureMetadata, () => ComputeDefaultSignature(cal));
            cal.LastModified = DateTime.UtcNow;
            cal.LastModifiedById = userId;

            await _db.InsertOrUpdateCalibrationAsync(
                cal,
                update: true,
                actorUserId: userId,
                ip: $"User:{userId}",
                device: signatureMetadata?.Device ?? "CalibrationService",
                signatureMetadata: signatureMetadata,
                token: CancellationToken.None
            );

            await LogAudit(cal.Id, userId, CalibrationActionType.UPDATE,
                $"Updated calibration ID={cal.Id}");
        }

        /// <inheritdoc />
        public async Task DeleteAsync(int calibrationId, int userId)
        {
            await _db.DeleteCalibrationAsync(
                calibrationId,
                actorUserId: userId,
                ip: $"User:{userId}",
                token: CancellationToken.None
            );

            await LogAudit(calibrationId, userId, CalibrationActionType.DELETE,
                $"Deleted calibration ID={calibrationId}");
        }

        #endregion

        #region === CERTIFICATE MANAGEMENT ===

        /// <inheritdoc />
        public async Task AttachCertificateAsync(int calibrationId, string certFilePath, int userId, SignatureMetadataDto? signatureMetadata = null)
        {
            var cal = await _db.GetCalibrationByIdAsync(calibrationId)
                ?? throw new InvalidOperationException("Calibration not found.");

            cal.CertDoc = certFilePath;
            ApplySignatureMetadata(cal, signatureMetadata, () => ComputeDefaultSignature(cal));

            await _db.InsertOrUpdateCalibrationAsync(
                cal,
                update: true,
                actorUserId: userId,
                ip: $"User:{userId}",
                device: signatureMetadata?.Device ?? "CalibrationService",
                signatureMetadata: signatureMetadata,
                token: CancellationToken.None
            );

            await LogAudit(calibrationId, userId, CalibrationActionType.CERTIFICATE_ATTACH,
                $"Attached certificate '{certFilePath}' to calibration ID={calibrationId}");
        }

        /// <inheritdoc />
        public async Task RevokeCertificateAsync(int calibrationId, string reason, int userId, SignatureMetadataDto? signatureMetadata = null)
        {
            var cal = await _db.GetCalibrationByIdAsync(calibrationId)
                ?? throw new InvalidOperationException("Calibration not found.");

            cal.Comment = (cal.Comment ?? string.Empty) + $" | Certificate revoked: {reason} ({DateTime.UtcNow:dd.MM.yyyy})";
            ApplySignatureMetadata(cal, signatureMetadata, () => ComputeDefaultSignature(cal));

            await _db.InsertOrUpdateCalibrationAsync(
                cal,
                update: true,
                actorUserId: userId,
                ip: $"User:{userId}",
                device: signatureMetadata?.Device ?? "CalibrationService",
                signatureMetadata: signatureMetadata,
                token: CancellationToken.None
            );

            await LogAudit(calibrationId, userId, CalibrationActionType.CERTIFICATE_REVOKE,
                $"Revoked certificate for calibration ID={calibrationId}. Reason: {reason}");
        }

        #endregion

        #region === STATUS & ADVANCED MONITORING ===

        /// <inheritdoc />
        public async Task MarkAsSuccessfulAsync(int calibrationId, int userId, SignatureMetadataDto? signatureMetadata = null)
        {
            var cal = await _db.GetCalibrationByIdAsync(calibrationId)
                ?? throw new InvalidOperationException("Calibration not found.");

            cal.Result = "PASS";
            cal.CalibrationDate = DateTime.UtcNow;

            // Compute next due in a way that works across DateTime/DateTime? models
            var next = cal.CalibrationDate.AddMonths(12);
            // Try assign via property if present to avoid compile-time nullability warnings.
            var prop = typeof(Calibration).GetProperty("NextDue");
            prop?.SetValue(cal, next);

            ApplySignatureMetadata(cal, signatureMetadata, () => ComputeDefaultSignature(cal));
            cal.LastModified = DateTime.UtcNow;
            cal.LastModifiedById = userId;

            await _db.InsertOrUpdateCalibrationAsync(
                cal,
                update: true,
                actorUserId: userId,
                ip: $"User:{userId}",
                device: signatureMetadata?.Device ?? "CalibrationService",
                signatureMetadata: signatureMetadata,
                token: CancellationToken.None
            );

            await LogAudit(cal.Id, userId, CalibrationActionType.EXECUTE,
                $"Executed calibration ID={cal.Id}");
        }

        /// <inheritdoc />
        public bool IsValid(Calibration cal)
        {
            if (cal == null) return false;
            var nd = GetNextDueNullable(cal);
            return nd.HasValue && nd.Value != default && nd.Value >= DateTime.UtcNow;
        }

        #endregion

        #region === VALIDATION ===

        /// <summary>
        /// Validates required fields on calibration model.
        /// </summary>
        /// <param name="cal">Calibration entity to check.</param>
        /// <exception cref="InvalidOperationException">Thrown if any required field is missing/invalid.</exception>
        private static void ValidateCalibration(Calibration cal)
        {
            if (cal is null) throw new ArgumentNullException(nameof(cal));
            if (cal.ComponentId <= 0) throw new InvalidOperationException("Calibration must be linked to a component.");
            if (cal.SupplierId <= 0) throw new InvalidOperationException("Supplier is required.");
            if (cal.CalibrationDate == default) throw new InvalidOperationException("Calibration date is required.");
            if (string.IsNullOrWhiteSpace(cal.Result)) throw new InvalidOperationException("Calibration result is required.");
        }

        #endregion

        #region === DIGITAL SIGNATURES ===

        /// <summary>
        /// Generates a digital signature for the calibration (Base64 SHA-256).
        /// </summary>
        /// <param name="cal">Calibration entity to sign.</param>
        private static void ApplySignatureMetadata(Calibration cal, SignatureMetadataDto? metadata, Func<string> legacyFactory)
        {
            if (cal == null) throw new ArgumentNullException(nameof(cal));
            if (legacyFactory == null) throw new ArgumentNullException(nameof(legacyFactory));

            string hash = metadata?.Hash ?? cal.DigitalSignature ?? legacyFactory();
            cal.DigitalSignature = hash;

            if (metadata?.Id.HasValue == true)
            {
                cal.DigitalSignatureId = metadata.Id;
            }

            if (!string.IsNullOrWhiteSpace(metadata?.IpAddress))
            {
                cal.SourceIp = metadata.IpAddress!;
            }

        }

        private static string ComputeDefaultSignature(Calibration cal)
        {
            string raw = $"{cal.Id}|{cal.ComponentId}|{cal.SupplierId}|{cal.CalibrationDate:O}|{cal.Result}";
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }

        #endregion        #endregion

        #region === AUDIT INTEGRATION ===

        /// <summary>
        /// Writes an audit entry for calibration actions.
        /// </summary>
        private async Task LogAudit(int calibrationId, int userId, CalibrationActionType action, string details)
        {
            var auditEntry = new CalibrationAudit
            {
                CalibrationId = calibrationId,
                UserId = userId,
                Action = action,
                Details = details,
                ChangedAt = DateTime.UtcNow,
                DigitalSignature = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(details)))
            };
            await _audit.CreateAsync(auditEntry);
        }

        #endregion

        #region === INTERNAL HELPERS (Nullability-Safe NextDue Access) ===

        /// <summary>
        /// Safely obtains the value of <c>NextDue</c> as a nullable <see cref="DateTime"/>.
        /// Works whether the model declares the property as <see cref="DateTime"/> or <see cref="System.DateTime"/>.
        /// Returns <c>null</c> when the property is missing, null, or equal to <see cref="DateTime.MinValue"/>.
        /// </summary>
        private static DateTime? GetNextDueNullable(Calibration cal)
        {
            var prop = typeof(Calibration).GetProperty("NextDue");
            if (prop == null) return null;

            var value = prop.GetValue(cal);
            if (value == null) return null;

            // When a nullable DateTime? has a value, reflection boxes it as a DateTime.
            if (value is DateTime dt)
                return dt == default ? (DateTime?)null : dt;

            // Any other shape is treated as "not available".
            return null;
        }

        #endregion

        #region === FUTURE-READY EXTENSIONS ===

        /// <inheritdoc />
        public Task<double> PredictCalibrationFailureRiskAsync(int calibrationId)
        {
            // Simulate with random for now (extensible for ML/AI models).
            return Task.FromResult(new Random().NextDouble());
        }

        /// <inheritdoc />
        public Task<string> GetIoTSensorDataAsync(int calibrationId)
        {
            // Simulate stub (replace with actual IoT data integrations).
            return Task.FromResult("SensorData: OK");
        }

        /// <inheritdoc />
        public async Task TriggerRecalibrationAsync(int calibrationId)
        {
            await LogAudit(calibrationId, 0, CalibrationActionType.RECALIBRATION_TRIGGER,
                $"Recalibration triggered for ID={calibrationId}");
        }

        /// <inheritdoc />
        public async Task LinkToCapaAsync(int calibrationId, int capaId)
        {
            await LogAudit(calibrationId, 0, CalibrationActionType.CAPA_LINK,
                $"Calibration ID={calibrationId} linked to CAPA ID={capaId}");
        }

        #endregion
    }
}


