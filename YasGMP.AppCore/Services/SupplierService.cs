// Services/SupplierService.cs

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.DTO;
using YasGMP.Models.Enums;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// Provides CRUD and contract-management operations for <see cref="Supplier"/> entities,
    /// with GMP/ISO-compliant auditing and digital signatures.
    /// </summary>
    public class SupplierService
    {
        private readonly DatabaseService _db;
        private readonly ISupplierAuditService _audit;

        #region Constructors

        public SupplierService(DatabaseService databaseService, ISupplierAuditService auditService)
        {
            _db    = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _audit = auditService    ?? throw new ArgumentNullException(nameof(auditService));
        }

        #endregion

        #region CRUD Operations

        public Task<List<Supplier>> GetAllAsync() => _db.GetAllSuppliersAsync();

        public Task<Supplier?> GetByIdAsync(int id) => _db.GetSupplierByIdAsync(id);

        public async Task CreateAsync(Supplier supplier, int userId, SignatureMetadataDto? signatureMetadata = null)
        {
            ValidateSupplier(supplier);

            ApplySignatureMetadata(supplier, signatureMetadata, () => ComputeDefaultSignature(supplier));
            await _db.InsertOrUpdateSupplierAsync(
                supplier,
                update: false,
                signatureMetadata: signatureMetadata,
                actorUserId: userId,
                ip: signatureMetadata?.IpAddress ?? string.Empty,
                device: signatureMetadata?.Device ?? string.Empty,
                sessionId: signatureMetadata?.Session);

            await LogAudit(
                supplier.Id,
                userId,
                SupplierActionType.CREATE,
                $"Created new supplier: {supplier.Name}");
        }

        public async Task UpdateAsync(Supplier supplier, int userId, SignatureMetadataDto? signatureMetadata = null)
        {
            ValidateSupplier(supplier);

            ApplySignatureMetadata(supplier, signatureMetadata, () => ComputeDefaultSignature(supplier));
            await _db.InsertOrUpdateSupplierAsync(
                supplier,
                update: true,
                signatureMetadata: signatureMetadata,
                actorUserId: userId,
                ip: signatureMetadata?.IpAddress ?? string.Empty,
                device: signatureMetadata?.Device ?? string.Empty,
                sessionId: signatureMetadata?.Session);

            await LogAudit(
                supplier.Id,
                userId,
                SupplierActionType.UPDATE,
                $"Updated supplier ID={supplier.Id}");
        }

        public async Task DeleteAsync(int supplierId, int userId)
        {
            await _db.DeleteSupplierAsync(supplierId);

            await LogAudit(
                supplierId,
                userId,
                SupplierActionType.DELETE,
                $"Deleted supplier ID={supplierId}");
        }

        #endregion

        #region Contract Management

        /// <summary>Determines whether the specified supplier's cooperation/contract is currently active.</summary>
        public bool IsContractActive(Supplier supplier)
        {
            if (supplier == null) return false;
            var now = DateTime.UtcNow;
            return supplier.CooperationStart <= now
                && (supplier.CooperationEnd == null || supplier.CooperationEnd >= now);
        }

        /// <summary>Suspends a supplier and appends a reason to <see cref="Supplier.Notes"/>.</summary>
        public async Task SuspendSupplierAsync(int supplierId, int userId, string reason, SignatureMetadataDto? signatureMetadata = null)
        {
            var supplier = await _db.GetSupplierByIdAsync(supplierId)
                ?? throw new InvalidOperationException($"Supplier ID={supplierId} not found.");

            supplier.Status = "SUSPENDED";
            supplier.Notes  = string.Concat(
                supplier.Notes ?? string.Empty,
                " | Suspended: ", reason, " (", DateTime.UtcNow.ToString("dd.MM.yyyy"), ")");

            ApplySignatureMetadata(supplier, signatureMetadata, () => ComputeDefaultSignature(supplier));
            await _db.InsertOrUpdateSupplierAsync(
                supplier,
                update: true,
                signatureMetadata: signatureMetadata,
                actorUserId: userId,
                ip: signatureMetadata?.IpAddress ?? string.Empty,
                device: signatureMetadata?.Device ?? string.Empty,
                sessionId: signatureMetadata?.Session);

            await LogAudit(
                supplierId,
                userId,
                SupplierActionType.SUSPEND,
                $"Suspended supplier ID={supplierId}, reason: {reason}");
        }

        /// <summary>Determines whether the specified supplier's cooperation/contract has expired as of now.</summary>
        public bool IsContractExpired(Supplier supplier)
            => supplier != null && supplier.CooperationEnd != null && supplier.CooperationEnd < DateTime.UtcNow;

        #endregion

        #region Validation

        /// <summary>
        /// Validates that required fields exist and dates are logical against your current <see cref="Supplier"/> model.
        /// </summary>
        private static void ValidateSupplier(Supplier s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (string.IsNullOrWhiteSpace(s.Name))
                throw new InvalidOperationException("Supplier name is required.");
            if (string.IsNullOrWhiteSpace(s.Email))
                throw new InvalidOperationException("Supplier email address is required.");

            // VAT/OIB equivalent in your current model.
            if (string.IsNullOrWhiteSpace(s.VatNumber))
                throw new InvalidOperationException("VAT number is required for compliance audit.");

            if (s.CooperationStart == default)
                throw new InvalidOperationException("Cooperation/contract start date must be specified.");
            if (s.CooperationEnd != null && s.CooperationEnd < s.CooperationStart)
                throw new InvalidOperationException("Cooperation end date cannot precede its start date.");
        }

        #endregion

        #region Digital Signatures

        private static void ApplySignatureMetadata(Supplier supplier, SignatureMetadataDto? metadata, Func<string> legacyFactory)
        {
            if (supplier == null) throw new ArgumentNullException(nameof(supplier));
            if (legacyFactory == null) throw new ArgumentNullException(nameof(legacyFactory));

            string hash = metadata?.Hash ?? supplier.DigitalSignature ?? legacyFactory();
            supplier.DigitalSignature = hash;

            if (metadata?.Id.HasValue == true)
            {
                supplier.DigitalSignatureId = metadata.Id;
            }

            if (!string.IsNullOrWhiteSpace(metadata?.IpAddress))
            {
                supplier.SourceIp = metadata.IpAddress!;
            }

            if (!string.IsNullOrWhiteSpace(metadata?.Session))
            {
                supplier.SessionId = metadata.Session!;
            }
        }

        private static string ComputeDefaultSignature(Supplier s)
        {
            var raw = new StringBuilder()
                .Append(s.Id).Append('|')
                .Append(s.Name).Append('|')
                .Append(s.Status).Append('|')
                .Append(s.CooperationStart?.ToString("O")).Append('|')
                .Append(s.CooperationEnd?.ToString("O"))
                .ToString();

            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToBase64String(bytes);
        }

        #endregion

        #region Audit Integration

        /// <summary>
        /// Writes a <see cref="SupplierAudit"/> record describing the action taken.
        /// Maps the enum to the string field names used by your current <c>SupplierAudit</c> model.
        /// </summary>
        private async Task LogAudit(int supplierId, int userId, SupplierActionType action, string details)
        {
            await _audit.CreateAsync(new SupplierAudit
            {
                SupplierId       = supplierId,
                UserId           = userId,
                ActionType       = action.ToString(),
                Details          = details,
                ActionTimestamp  = DateTime.UtcNow,
                DigitalSignature = Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(details)))
            });
        }

        #endregion

        #region Future Extensibility Hooks

        public async Task TriggerRequalificationAsync(int supplierId)
        {
            await LogAudit(
                supplierId,
                userId: 0,
                SupplierActionType.REQUALIFICATION,
                $"Triggered re-qualification for supplier ID={supplierId}");
        }

        public async Task LinkToCapaAsync(int supplierId, int capaId)
        {
            await LogAudit(
                supplierId,
                userId: 0,
                SupplierActionType.CAPA_LINK,
                $"Linked supplier ID={supplierId} to CAPA ID={capaId}");
        }

        #endregion
    }
}
