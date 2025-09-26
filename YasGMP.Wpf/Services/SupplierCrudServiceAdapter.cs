using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter that allows the WPF shell to reuse the shared <see cref="SupplierService"/>
/// while capturing the extra audit metadata required by desktop saves.
/// </summary>
public sealed class SupplierCrudServiceAdapter : ISupplierCrudService
{
    private readonly SupplierService _supplierService;
    private readonly DatabaseService _databaseService;

    public SupplierCrudServiceAdapter(SupplierService supplierService, DatabaseService databaseService)
    {
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
    }

    public async Task<IReadOnlyList<Supplier>> GetAllAsync()
    {
        var suppliers = await _supplierService.GetAllAsync().ConfigureAwait(false);
        return suppliers.AsReadOnly();
    }

    public Task<Supplier?> TryGetByIdAsync(int id) => _supplierService.GetByIdAsync(id);

    public async Task<int> CreateAsync(Supplier supplier, SupplierCrudContext context)
    {
        if (supplier is null)
        {
            throw new ArgumentNullException(nameof(supplier));
        }

        await _supplierService.CreateAsync(supplier, context.UserId).ConfigureAwait(false);
        await StampAsync(supplier, context, "CREATE").ConfigureAwait(false);
        return supplier.Id;
    }

    public async Task UpdateAsync(Supplier supplier, SupplierCrudContext context)
    {
        if (supplier is null)
        {
            throw new ArgumentNullException(nameof(supplier));
        }

        await _supplierService.UpdateAsync(supplier, context.UserId).ConfigureAwait(false);
        await StampAsync(supplier, context, "UPDATE").ConfigureAwait(false);
    }

    public void Validate(Supplier supplier)
    {
        if (supplier is null)
        {
            throw new ArgumentNullException(nameof(supplier));
        }

        if (string.IsNullOrWhiteSpace(supplier.Name))
        {
            throw new InvalidOperationException("Supplier name is required.");
        }

        if (string.IsNullOrWhiteSpace(supplier.Email))
        {
            throw new InvalidOperationException("Supplier email address is required.");
        }

        if (string.IsNullOrWhiteSpace(supplier.VatNumber))
        {
            throw new InvalidOperationException("VAT number is required.");
        }

        if (supplier.CooperationEnd is not null && supplier.CooperationStart is not null
            && supplier.CooperationEnd < supplier.CooperationStart)
        {
            throw new InvalidOperationException("Contract end cannot precede its start date.");
        }
    }

    public string NormalizeStatus(string? status) => SupplierCrudExtensions.NormalizeStatusDefault(status);

    private async Task StampAsync(Supplier supplier, SupplierCrudContext context, string action)
    {
        const string ensureSql = @"CREATE TABLE IF NOT EXISTS suppliers (
  id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  vat_number VARCHAR(40) NULL,
  address VARCHAR(255) NULL,
  city VARCHAR(80) NULL,
  country VARCHAR(80) NULL,
  email VARCHAR(100) NULL,
  phone VARCHAR(50) NULL,
  website VARCHAR(200) NULL,
  supplier_type VARCHAR(40) NULL,
  notes TEXT NULL,
  contract_file VARCHAR(255) NULL,
  status VARCHAR(40) NULL,
  source_ip VARCHAR(45) NULL,
  session_id VARCHAR(80) NULL,
  last_modified DATETIME NULL,
  last_modified_by_id INT NULL
);";
        await _databaseService.ExecuteNonQueryAsync(ensureSql, null).ConfigureAwait(false);

        const string updateSql = @"UPDATE suppliers
SET source_ip=@ip,
    session_id=@session,
    last_modified=@modified,
    last_modified_by_id=@user,
    status=@status
WHERE id=@id";

        var parameters = new[]
        {
            new MySqlParameter("@ip", context.Ip),
            new MySqlParameter("@session", context.SessionId ?? string.Empty),
            new MySqlParameter("@modified", DateTime.UtcNow),
            new MySqlParameter("@user", context.UserId),
            new MySqlParameter("@status", NormalizeStatus(supplier.Status)),
            new MySqlParameter("@id", supplier.Id)
        };

        await _databaseService.ExecuteNonQueryAsync(updateSql, parameters).ConfigureAwait(false);

        var details = string.Format(CultureInfo.InvariantCulture, "status={0}; country={1}", supplier.Status, supplier.Country);
        await _databaseService.LogSupplierAuditAsync(
            supplier.Id,
            action,
            context.UserId,
            details,
            context.Ip,
            context.DeviceInfo,
            context.SessionId).ConfigureAwait(false);
    }
}
