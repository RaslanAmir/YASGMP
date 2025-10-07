using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Bridges supplier workflows in the WPF shell to the shared MAUI
/// <see cref="YasGMP.Services.SupplierService"/> and supporting persistence services.
/// </summary>
/// <remarks>
/// Supplier module view models issue CRUD commands through this adapter; requests are then forwarded to
/// <see cref="YasGMP.Services.SupplierService"/> and <see cref="YasGMP.Services.DatabaseService"/> so both shells share the
/// same persistence and audit story. Await operations off the UI thread and dispatch UI updates via
/// <see cref="WpfUiDispatcher"/>. The returned <see cref="CrudSaveResult"/> contains identifiers, status, and signature data that
/// callers must localize with <see cref="LocalizationServiceExtensions"/> or <see cref="ILocalizationService"/> before
/// presenting to operators. Audit information is written through <see cref="YasGMP.Services.DatabaseServiceSuppliersExtensions.LogSupplierAuditAsync(YasGMP.Services.DatabaseService,int,string,int,string?,string,string,string?,System.Threading.CancellationToken)"/>,
/// which feeds the shared <see cref="YasGMP.Services.AuditService"/> surfaced inside MAUI.
/// </remarks>
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

    public async Task<CrudSaveResult> CreateAsync(Supplier supplier, SupplierCrudContext context)
    {
        if (supplier is null)
        {
            throw new ArgumentNullException(nameof(supplier));
        }

        var signature = ApplyContext(supplier, context);
        var metadata = CreateMetadata(context, signature);

        await _supplierService.CreateAsync(supplier, context.UserId, metadata).ConfigureAwait(false);

        supplier.DigitalSignature = signature;
        await StampAsync(supplier, context, "CREATE", signature).ConfigureAwait(false);
        return new CrudSaveResult(supplier.Id, metadata);
    }

    public async Task<CrudSaveResult> UpdateAsync(Supplier supplier, SupplierCrudContext context)
    {
        if (supplier is null)
        {
            throw new ArgumentNullException(nameof(supplier));
        }

        var signature = ApplyContext(supplier, context);
        var metadata = CreateMetadata(context, signature);

        await _supplierService.UpdateAsync(supplier, context.UserId, metadata).ConfigureAwait(false);

        supplier.DigitalSignature = signature;
        await StampAsync(supplier, context, "UPDATE", signature).ConfigureAwait(false);
        return new CrudSaveResult(supplier.Id, metadata);
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

    private async Task StampAsync(Supplier supplier, SupplierCrudContext context, string action, string signature)
    {
        var modifiedAt = DateTime.UtcNow;
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
  digital_signature VARCHAR(128) NULL,
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
    status=@status,
    digital_signature=@signature
WHERE id=@id";

        var parameters = new[]
        {
            new MySqlParameter("@ip", context.Ip),
            new MySqlParameter("@session", context.SessionId ?? string.Empty),
            new MySqlParameter("@modified", modifiedAt),
            new MySqlParameter("@user", context.UserId),
            new MySqlParameter("@status", NormalizeStatus(supplier.Status)),
            new MySqlParameter("@signature", signature ?? string.Empty),
            new MySqlParameter("@id", supplier.Id)
        };

        try
        {
            await _databaseService.ExecuteNonQueryAsync(updateSql, parameters).ConfigureAwait(false);
        }
        catch (MySqlException ex) when (ex.Number == 1054)
        {
            const string legacySql = @"UPDATE suppliers
SET source_ip=@ip,
    session_id=@session,
    last_modified=@modified,
    last_modified_by_id=@user,
    status=@status
WHERE id=@id";

            var legacyParameters = new[]
            {
                new MySqlParameter("@ip", context.Ip),
                new MySqlParameter("@session", context.SessionId ?? string.Empty),
                new MySqlParameter("@modified", modifiedAt),
                new MySqlParameter("@user", context.UserId),
                new MySqlParameter("@status", NormalizeStatus(supplier.Status)),
                new MySqlParameter("@id", supplier.Id)
            };

            await _databaseService.ExecuteNonQueryAsync(legacySql, legacyParameters).ConfigureAwait(false);
        }

        var signatureHash = string.IsNullOrWhiteSpace(signature)
            ? context.SignatureHash ?? supplier.DigitalSignature ?? string.Empty
            : signature;

        var details = string.Format(
            CultureInfo.InvariantCulture,
            "status={0}; country={1}; sigId={2}; sigHash={3}; sigMethod={4}; sigStatus={5}; sigNote={6}",
            supplier.Status ?? string.Empty,
            supplier.Country ?? string.Empty,
            context.SignatureId?.ToString(CultureInfo.InvariantCulture) ?? "-",
            signatureHash,
            context.SignatureMethod ?? "-",
            context.SignatureStatus ?? "-",
            string.IsNullOrWhiteSpace(context.SignatureNote) ? "-" : context.SignatureNote);
        await _databaseService.LogSupplierAuditAsync(
            supplier.Id,
            action,
            context.UserId,
            details,
            context.Ip,
            context.DeviceInfo,
            context.SessionId).ConfigureAwait(false);
    }

    private static string ApplyContext(Supplier supplier, SupplierCrudContext context)
    {
        var signature = context.SignatureHash ?? supplier.DigitalSignature ?? string.Empty;
        supplier.DigitalSignature = signature;

        if (context.UserId > 0)
        {
            supplier.LastModifiedById = context.UserId;
        }

        if (!string.IsNullOrWhiteSpace(context.Ip))
        {
            supplier.SourceIp = context.Ip;
        }

        if (!string.IsNullOrWhiteSpace(context.SessionId))
        {
            supplier.SessionId = context.SessionId!;
        }

        return signature;
    }

    private static SignatureMetadataDto CreateMetadata(SupplierCrudContext context, string signature)
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
}
