using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;

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
    public readonly record struct WarehouseCrudContext(int UserId, string Ip, string DeviceInfo, string? SessionId)
    {
        public static WarehouseCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
            => new(userId <= 0 ? 1 : userId,
                   string.IsNullOrWhiteSpace(ip) ? "unknown" : ip,
                   string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo,
                   string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId);
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
