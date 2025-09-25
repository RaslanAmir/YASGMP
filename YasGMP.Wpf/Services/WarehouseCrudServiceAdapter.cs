using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;
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

        public async Task<int> CreateAsync(Warehouse warehouse, WarehouseCrudContext context)
        {
            var id = await _database.AddWarehouseAsync(warehouse.Name, warehouse.Location).ConfigureAwait(false);
            warehouse.Id = id;
            await UpdateWarehouseDetailsAsync(warehouse, context).ConfigureAwait(false);
            return id;
        }

        public async Task UpdateAsync(Warehouse warehouse, WarehouseCrudContext context)
        {
            await UpdateWarehouseDetailsAsync(warehouse, context).ConfigureAwait(false);
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
            await _auditService.LogSystemEventAsync(
                context.UserId,
                "WAREHOUSE_SAVE",
                "warehouses",
                "WarehouseCrud",
                warehouse.Id,
                warehouse.DigitalSignature,
                context.Ip,
                "wpf",
                context.DeviceInfo,
                context.SessionId).ConfigureAwait(false);
        }
    }
}
