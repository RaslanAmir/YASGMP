using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.Services.Interfaces;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Adapter that exposes warehouse CRUD operations to the WPF shell while
    /// delegating to the shared database service for persistence.
    /// </summary>
    public sealed class WarehouseCrudServiceAdapter : IWarehouseCrudService
    {
        private readonly DatabaseService _database;
        private readonly AuditService _auditService;

        public WarehouseCrudServiceAdapter(DatabaseService database, AuditService auditService)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        public async Task<IReadOnlyList<Warehouse>> GetAllAsync()
        {
            var warehouses = await _database.GetWarehousesAsync().ConfigureAwait(false);
            return warehouses.AsReadOnly();
        }

        public async Task<Warehouse?> TryGetByIdAsync(int id)
        {
            var warehouses = await _database.GetWarehousesAsync().ConfigureAwait(false);
            foreach (var warehouse in warehouses)
            {
                if (warehouse.Id == id)
                {
                    return warehouse;
                }
            }

            return null;
        }

        public async Task<CrudSaveResult> CreateAsync(Warehouse warehouse, WarehouseCrudContext context)
        {
            var signature = ApplyContext(warehouse, context);
            var id = await _database.AddWarehouseAsync(warehouse.Name, warehouse.Location).ConfigureAwait(false);
            warehouse.Id = id;
            await UpdateWarehouseDetailsAsync(warehouse, context).ConfigureAwait(false);
            return new CrudSaveResult(id, CreateMetadata(context, signature));
        }

        public async Task<CrudSaveResult> UpdateAsync(Warehouse warehouse, WarehouseCrudContext context)
        {
            var signature = ApplyContext(warehouse, context);
            await UpdateWarehouseDetailsAsync(warehouse, context).ConfigureAwait(false);
            return new CrudSaveResult(warehouse.Id, CreateMetadata(context, signature));
        }

        public void Validate(Warehouse warehouse)
        {
            if (warehouse is null)
            {
                throw new ArgumentNullException(nameof(warehouse));
            }

            if (string.IsNullOrWhiteSpace(warehouse.Name))
            {
                throw new InvalidOperationException("Warehouse name is required.");
            }

            if (string.IsNullOrWhiteSpace(warehouse.Location))
            {
                throw new InvalidOperationException("Warehouse location is required.");
            }
        }

        public string NormalizeStatus(string? status)
            => string.IsNullOrWhiteSpace(status) ? "qualified" : status.Trim().ToLower(CultureInfo.InvariantCulture);

        public async Task<IReadOnlyList<WarehouseStockSnapshot>> GetStockSnapshotAsync(int warehouseId)
        {
            const string sql = @"SELECT sl.part_id,
       COALESCE(p.code, CONCAT('PART-', sl.part_id)) AS part_code,
       COALESCE(p.name, CONCAT('Part #', sl.part_id)) AS part_name,
       sl.quantity,
       sl.min_threshold,
       sl.max_threshold,
       sl.reserved,
       sl.blocked,
       sl.batch_number,
       sl.serial_number,
       sl.expiry_date
FROM stock_levels sl
LEFT JOIN parts p ON p.id = sl.part_id
WHERE sl.warehouse_id=@warehouse
ORDER BY part_name, part_code";

            var table = await _database.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@warehouse", warehouseId) })
                .ConfigureAwait(false);

            var results = new List<WarehouseStockSnapshot>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                results.Add(new WarehouseStockSnapshot(
                    WarehouseId: warehouseId,
                    PartId: SafeInt(row, "part_id"),
                    PartCode: row["part_code"]?.ToString() ?? string.Empty,
                    PartName: row["part_name"]?.ToString() ?? string.Empty,
                    Quantity: SafeInt(row, "quantity"),
                    MinThreshold: SafeNullableInt(row, "min_threshold"),
                    MaxThreshold: SafeNullableInt(row, "max_threshold"),
                    Reserved: SafeInt(row, "reserved"),
                    Blocked: SafeInt(row, "blocked"),
                    BatchNumber: row["batch_number"]?.ToString() ?? string.Empty,
                    SerialNumber: row["serial_number"]?.ToString() ?? string.Empty,
                    ExpiryDate: SafeNullableDate(row, "expiry_date")));
            }

            return results;
        }

        public async Task<IReadOnlyList<InventoryMovementEntry>> GetRecentMovementsAsync(int warehouseId, int take = 10)
        {
            var data = await _database.GetInventoryMovementPreviewAsync(warehouseId, partId: null, take: take)
                .ConfigureAwait(false);

            var items = new List<InventoryMovementEntry>(data.Rows.Count);
            foreach (DataRow row in data.Rows)
            {
                items.Add(new InventoryMovementEntry(
                    WarehouseId: warehouseId,
                    Timestamp: SafeNullableDate(row, "transaction_date") ?? DateTime.UtcNow,
                    Type: row["transaction_type"]?.ToString() ?? string.Empty,
                    Quantity: SafeInt(row, "quantity"),
                    RelatedDocument: row.Table.Columns.Contains("related_document") ? row["related_document"]?.ToString() : null,
                    Note: row.Table.Columns.Contains("note") ? row["note"]?.ToString() : null,
                    PerformedById: row.Table.Columns.Contains("performed_by_id") && row["performed_by_id"] != DBNull.Value
                        ? Convert.ToInt32(row["performed_by_id"], CultureInfo.InvariantCulture)
                        : null));
            }

            return items;
        }

        private async Task UpdateWarehouseDetailsAsync(Warehouse warehouse, WarehouseCrudContext context)
        {
            const string ensure = @"CREATE TABLE IF NOT EXISTS warehouses (
  id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  location VARCHAR(255) NULL,
  status VARCHAR(30) NULL,
  responsible VARCHAR(255) NULL,
  note VARCHAR(500) NULL,
  qr_code VARCHAR(255) NULL,
  climate_mode VARCHAR(60) NULL,
  is_qualified BIT NULL,
  last_qualified DATETIME NULL,
  digital_signature VARCHAR(128) NULL,
  source_ip VARCHAR(45) NULL,
  session_id VARCHAR(80) NULL
);";
            await _database.ExecuteNonQueryAsync(ensure, null).ConfigureAwait(false);

            const string update = @"UPDATE warehouses
SET name=@name,
    location=@location,
    status=@status,
    responsible=@responsible,
    note=@note,
    qr_code=@qr,
    climate_mode=@climate,
    is_qualified=@qualified,
    last_qualified=@lastQualified,
    digital_signature=@signature,
    source_ip=@ip,
    session_id=@session
WHERE id=@id";

            var parameters = new[]
            {
                new MySqlParameter("@name", warehouse.Name),
                new MySqlParameter("@location", warehouse.Location),
                new MySqlParameter("@status", NormalizeStatus(warehouse.Status)),
                new MySqlParameter("@responsible", warehouse.LegacyResponsibleName ?? string.Empty),
                new MySqlParameter("@note", warehouse.Note ?? string.Empty),
                new MySqlParameter("@qr", warehouse.QrCode ?? string.Empty),
                new MySqlParameter("@climate", warehouse.ClimateMode ?? string.Empty),
                new MySqlParameter("@qualified", warehouse.IsQualified),
                new MySqlParameter("@lastQualified", warehouse.LastQualified ?? (object?)DBNull.Value),
                new MySqlParameter("@signature", warehouse.DigitalSignature ?? string.Empty),
                new MySqlParameter("@ip", context.Ip ?? string.Empty),
                new MySqlParameter("@session", context.SessionId ?? string.Empty),
                new MySqlParameter("@id", warehouse.Id)
            };

            await _database.ExecuteNonQueryAsync(update, parameters).ConfigureAwait(false);
            await _auditService.LogSystemEventAsync("WAREHOUSE_SAVE", $"WarehouseCrud; sig={warehouse.DigitalSignature}", "warehouses", warehouse.Id).ConfigureAwait(false);
        }

        private static string ApplyContext(Warehouse warehouse, WarehouseCrudContext context)
        {
            var signature = context.SignatureHash ?? warehouse.DigitalSignature ?? string.Empty;
            warehouse.DigitalSignature = signature;
            return signature;
        }

        private static SignatureMetadataDto CreateMetadata(WarehouseCrudContext context, string signature)
            => new()
            {
                Id = context.SignatureId,
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.Ip
            };

        private static int SafeInt(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value
                ? Convert.ToInt32(row[column], CultureInfo.InvariantCulture)
                : 0;

        private static int? SafeNullableInt(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value
                ? Convert.ToInt32(row[column], CultureInfo.InvariantCulture)
                : null;

        private static DateTime? SafeNullableDate(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value
                ? Convert.ToDateTime(row[column], CultureInfo.InvariantCulture)
                : null;
    }
}

