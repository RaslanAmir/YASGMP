using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services;

public partial class DatabaseService
{
    public Func<Exception?>? WarehousesExceptionFactory { get; set; }

    public Exception? WarehousesException { get; set; }

    public Func<int, List<(int warehouseId, string warehouseName, int quantity, int? min, int? max)>>? StockLevelsProvider { get; set; }

    public Dictionary<int, List<(int warehouseId, string warehouseName, int quantity, int? min, int? max)>> StockLevelsByPart { get; } = new();

    public Func<int, Exception?>? StockLevelsExceptionFactory { get; set; }

    public Exception? StockLevelsException { get; set; }

    public Func<int, int, DataTable>? InventoryHistoryProvider { get; set; }

    public Dictionary<(int partId, int take), DataTable> InventoryHistoryByPart { get; } = new();

    public Func<int, int, Exception?>? InventoryHistoryExceptionFactory { get; set; }

    public Exception? InventoryHistoryException { get; set; }

    public Func<int, DataTable>? InventoryZoneDashboardProvider { get; set; }

    public DataTable? InventoryZoneDashboardTable { get; set; }

    public Exception? InventoryZoneDashboardException { get; set; }

    public Func<int?, int?, int, DataTable>? InventoryMovementPreviewProvider { get; set; }

    public DataTable? InventoryMovementPreviewTable { get; set; }

    public Exception? InventoryMovementPreviewException { get; set; }

    public Task EnsureInventorySchemaAsync(CancellationToken token = default)
        => Task.CompletedTask;

    public Task<List<(int warehouseId, string warehouseName, int quantity, int? min, int? max)>> GetStockLevelsForPartAsync(
        int partId,
        CancellationToken token = default)
    {
        if (StockLevelsExceptionFactory?.Invoke(partId) is { } dynamicException)
        {
            throw dynamicException;
        }

        if (StockLevelsException is not null)
        {
            throw StockLevelsException;
        }

        if (StockLevelsByPart.TryGetValue(partId, out var configuredLevels))
        {
            return Task.FromResult(CloneStockLevels(configuredLevels));
        }

        if (StockLevelsProvider is not null)
        {
            var provided = StockLevelsProvider(partId) ?? new List<(int, string, int, int?, int?)>();
            return Task.FromResult(CloneStockLevels(provided));
        }

        return Task.FromResult(new List<(int warehouseId, string warehouseName, int quantity, int? min, int? max)>());
    }

    public Task<DataTable> GetInventoryTransactionsForPartAsync(
        int partId,
        int take = 200,
        CancellationToken token = default)
    {
        if (InventoryHistoryExceptionFactory?.Invoke(partId, take) is { } dynamicException)
        {
            throw dynamicException;
        }

        if (InventoryHistoryException is not null)
        {
            throw InventoryHistoryException;
        }

        if (InventoryHistoryByPart.TryGetValue((partId, take), out var configured))
        {
            return Task.FromResult(configured.Copy());
        }

        if (InventoryHistoryByPart.TryGetValue((partId, -1), out var fallback))
        {
            return Task.FromResult(fallback.Copy());
        }

        if (InventoryHistoryProvider is not null)
        {
            var provided = InventoryHistoryProvider(partId, take) ?? CreateEmptyInventoryHistory();
            return Task.FromResult(provided.Copy());
        }

        return Task.FromResult(CreateEmptyInventoryHistory());
    }

    public Task<DataTable> GetInventoryZoneDashboardAsync(int take = 25, CancellationToken token = default)
    {
        if (InventoryZoneDashboardException is not null)
        {
            throw InventoryZoneDashboardException;
        }

        if (InventoryZoneDashboardProvider is not null)
        {
            var provided = InventoryZoneDashboardProvider(take) ?? CreateEmptyInventoryZoneDashboard();
            return Task.FromResult(provided.Copy());
        }

        if (InventoryZoneDashboardTable is not null)
        {
            return Task.FromResult(InventoryZoneDashboardTable.Copy());
        }

        return Task.FromResult(CreateEmptyInventoryZoneDashboard());
    }

    public Task<DataTable> GetInventoryMovementPreviewAsync(
        int? warehouseId,
        int? partId,
        int take = 20,
        CancellationToken token = default)
    {
        if (InventoryMovementPreviewException is not null)
        {
            throw InventoryMovementPreviewException;
        }

        if (InventoryMovementPreviewProvider is not null)
        {
            var provided = InventoryMovementPreviewProvider(warehouseId, partId, take) ?? CreateEmptyInventoryHistory();
            return Task.FromResult(provided.Copy());
        }

        if (InventoryMovementPreviewTable is not null)
        {
            return Task.FromResult(InventoryMovementPreviewTable.Copy());
        }

        return Task.FromResult(CreateEmptyInventoryHistory());
    }

    private static List<(int warehouseId, string warehouseName, int quantity, int? min, int? max)> CloneStockLevels(
        IEnumerable<(int warehouseId, string warehouseName, int quantity, int? min, int? max)> source)
        => source.Select(level => (level.warehouseId, level.warehouseName, level.quantity, level.min, level.max)).ToList();

    private static DataTable CreateEmptyInventoryHistory()
    {
        var table = new DataTable();
        table.Columns.Add("transaction_date", typeof(DateTime));
        table.Columns.Add("transaction_type", typeof(string));
        table.Columns.Add("quantity", typeof(int));
        table.Columns.Add("warehouse_id", typeof(int));
        table.Columns.Add("performed_by_id", typeof(int));
        table.Columns.Add("related_document", typeof(string));
        table.Columns.Add("note", typeof(string));
        return table;
    }

    private static DataTable CreateEmptyInventoryZoneDashboard()
    {
        var table = new DataTable();
        table.Columns.Add("part_id", typeof(int));
        table.Columns.Add("part_name", typeof(string));
        table.Columns.Add("part_code", typeof(string));
        table.Columns.Add("warehouse_id", typeof(int));
        table.Columns.Add("warehouse_name", typeof(string));
        table.Columns.Add("quantity", typeof(int));
        table.Columns.Add("min_threshold", typeof(int));
        table.Columns.Add("max_threshold", typeof(int));
        table.Columns.Add("zone", typeof(string));
        return table;
    }

    private static Warehouse CloneWarehouse(Warehouse warehouse)
        => new()
        {
            Id = warehouse.Id,
            Name = warehouse.Name,
            Location = warehouse.Location,
            Status = warehouse.Status,
            LegacyResponsibleName = warehouse.LegacyResponsibleName,
            Note = warehouse.Note,
            QrCode = warehouse.QrCode,
            ClimateMode = warehouse.ClimateMode,
            IsQualified = warehouse.IsQualified,
            LastQualified = warehouse.LastQualified,
            DigitalSignature = warehouse.DigitalSignature
        };
}
