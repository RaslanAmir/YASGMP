using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Abstraction around warehouse CRUD for the WPF shell.
    /// </summary>
    public interface IWarehouseCrudService
    {
        Task<IReadOnlyList<Warehouse>> GetAllAsync();

        Task<Warehouse?> TryGetByIdAsync(int id);

        Task<int> CreateAsync(Warehouse warehouse, WarehouseCrudContext context);

        Task UpdateAsync(Warehouse warehouse, WarehouseCrudContext context);

        void Validate(Warehouse warehouse);

        string NormalizeStatus(string? status);

        Task<IReadOnlyList<WarehouseStockSnapshot>> GetStockSnapshotAsync(int warehouseId);

        Task<IReadOnlyList<InventoryMovementEntry>> GetRecentMovementsAsync(int warehouseId, int take = 10);
    }

    /// <summary>
    /// Ambient metadata propagated with warehouse saves for auditing.
    /// </summary>
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
        public bool IsBelowMinimum => MinThreshold.HasValue && Quantity - Reserved - Blocked < MinThreshold.Value;

        public bool IsAboveMaximum => MaxThreshold.HasValue && Quantity > MaxThreshold.Value;
    }

    public sealed record InventoryMovementEntry(
        int WarehouseId,
        DateTime Timestamp,
        string Type,
        int Quantity,
        string? RelatedDocument,
        string? Note,
        int? PerformedById);
}
