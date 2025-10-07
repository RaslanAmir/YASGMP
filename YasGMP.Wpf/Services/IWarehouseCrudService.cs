using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Models;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Shared contract for routing warehouse CRUD/ledger operations through
    /// <see cref="YasGMP.Services.DatabaseService"/> and the shared MAUI audit services.
    /// </summary>
    /// <remarks>
    /// Module view models call into this interface on the dispatcher thread; adapters forward the work to
    /// <see cref="YasGMP.Services.DatabaseService"/> and <see cref="YasGMP.Services.AuditService"/> so warehouse persistence,
    /// stock snapshots, and audit logs remain unified across MAUI and WPF. After awaiting, callers should marshal UI updates via
    /// <see cref="WpfUiDispatcher"/>. Returned <see cref="CrudSaveResult"/> values carry identifiers, signature context, and
    /// localization-ready status strings so <see cref="LocalizationServiceExtensions"/> or <see cref="ILocalizationService"/>
    /// can translate them before presentation.
    /// </remarks>
    public interface IWarehouseCrudService
    {
        Task<IReadOnlyList<Warehouse>> GetAllAsync();

        Task<Warehouse?> TryGetByIdAsync(int id);

        /// <summary>
        /// Persists a new warehouse and returns the saved identifier together with signature metadata.
        /// </summary>
        Task<CrudSaveResult> CreateAsync(Warehouse warehouse, WarehouseCrudContext context);

        /// <summary>
        /// Updates an existing warehouse and returns the signature metadata captured during persistence.
        /// </summary>
        Task<CrudSaveResult> UpdateAsync(Warehouse warehouse, WarehouseCrudContext context);

        void Validate(Warehouse warehouse);

        string NormalizeStatus(string? status);

        Task<IReadOnlyList<WarehouseStockSnapshot>> GetStockSnapshotAsync(int warehouseId);

        Task<IReadOnlyList<InventoryMovementEntry>> GetRecentMovementsAsync(int warehouseId, int take = 10);
    }

    /// <summary>
    /// Ambient metadata propagated with warehouse saves for auditing. Each value flows into
    /// <see cref="CrudSaveResult.SignatureMetadata"/> via <see cref="SignatureMetadataDto"/> to preserve the accepted signature
    /// manifest for compliance pipelines.
    /// </summary>
    /// <remarks>
    /// Adapters hydrate <see cref="SignatureMetadataDto"/> from this record before returning <see cref="CrudSaveResult"/>.
    /// WPF shell consumers must persist and surface the DTO beside warehouse records, and MAUI experiences should propagate the
    /// same payload when presenting or synchronizing warehouses to keep the shared audit history aligned.
    /// </remarks>
    /// <param name="UserId">Authenticated user id.</param>
    /// <param name="Ip">Source IP address.</param>
    /// <param name="DeviceInfo">Device or workstation info.</param>
    /// <param name="SessionId">Logical session id.</param>
    /// <param name="SignatureId">Database identifier for the captured signature.</param>
    /// <param name="SignatureHash">Hash associated with the captured signature.</param>
    /// <param name="SignatureMethod">Method used to authenticate the signature.</param>
    /// <param name="SignatureStatus">Status of the signature at capture.</param>
    /// <param name="SignatureNote">Operator reason captured during signing.</param>
    public readonly record struct WarehouseCrudContext(
        int UserId,
        string Ip,
        string DeviceInfo,
        string? SessionId,
        int? SignatureId,
        string? SignatureHash,
        string? SignatureMethod,
        string? SignatureStatus,
        string? SignatureNote)
    {
        private const string DefaultSignatureMethod = "password";
        private const string DefaultSignatureStatus = "valid";
        /// <summary>
        /// Executes the create operation.
        /// </summary>

        public static WarehouseCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
            => new(userId <= 0 ? 1 : userId,
                   string.IsNullOrWhiteSpace(ip) ? "unknown" : ip,
                   string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo,
                   string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId,
                   null,
                   null,
                   DefaultSignatureMethod,
                   DefaultSignatureStatus,
                   null);
        /// <summary>
        /// Executes the create operation.
        /// </summary>

        public static WarehouseCrudContext Create(
            int userId,
            string ip,
            string deviceInfo,
            string? sessionId,
            ElectronicSignatureDialogResult signatureResult)
        {
            ArgumentNullException.ThrowIfNull(signatureResult);
            ArgumentNullException.ThrowIfNull(signatureResult.Signature);

            var context = Create(userId, ip, deviceInfo, sessionId);
            var signature = signatureResult.Signature;

            return context with
            {
                SignatureId = signature.Id > 0 ? signature.Id : null,
                SignatureHash = string.IsNullOrWhiteSpace(signature.SignatureHash) ? null : signature.SignatureHash,
                SignatureMethod = string.IsNullOrWhiteSpace(signature.Method) ? DefaultSignatureMethod : signature.Method,
                SignatureStatus = string.IsNullOrWhiteSpace(signature.Status) ? DefaultSignatureStatus : signature.Status,
                SignatureNote = !string.IsNullOrWhiteSpace(signature.Note)
                    ? signature.Note
                    : !string.IsNullOrWhiteSpace(signatureResult.ReasonDetail)
                        ? signatureResult.ReasonDetail
                        : signatureResult.ReasonCode
            };
        }
    }
    /// <summary>
    /// Represents the Warehouse Stock Snapshot record.
    /// </summary>

    public sealed record WarehouseStockSnapshot(
        int WarehouseId,
        int PartId,
        string PartCode,
        string PartName,
        int Quantity,
        int? MinThreshold,
        int? MaxThreshold,
        int Reserved,
        int Blocked,
        string BatchNumber,
        string SerialNumber,
        DateTime? ExpiryDate)
    {
        /// <summary>
        /// Gets or sets the is below minimum.
        /// </summary>
        public bool IsBelowMinimum => MinThreshold.HasValue && Quantity - Reserved - Blocked < MinThreshold.Value;
        /// <summary>
        /// Gets or sets the is above maximum.
        /// </summary>

        public bool IsAboveMaximum => MaxThreshold.HasValue && Quantity > MaxThreshold.Value;
    }
    /// <summary>
    /// Represents the Inventory Movement Entry record.
    /// </summary>

    public sealed record InventoryMovementEntry(
        int WarehouseId,
        DateTime Timestamp,
        string Type,
        int Quantity,
        string? RelatedDocument,
        string? Note,
        int? PerformedById);
}
