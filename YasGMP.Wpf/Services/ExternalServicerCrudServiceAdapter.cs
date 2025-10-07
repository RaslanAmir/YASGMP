using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Bridges the External Servicer module in the WPF shell with the shared MAUI
/// <see cref="YasGMP.Services.ExternalServicerService"/> and persistence stack.
/// </summary>
/// <remarks>
/// Module view models call into this adapter before delegating to <see cref="YasGMP.Services.ExternalServicerService"/> and
/// <see cref="YasGMP.Services.DatabaseService"/>, mirroring the MAUI call flow while allowing the WPF shell to stamp
/// additional metadata. Methods are awaited away from the UI thread; callers should marshal UI updates using
/// <see cref="WpfUiDispatcher"/>. The <see cref="CrudSaveResult"/> conveys identifiers, status text, and signature context that
/// must be localized with <see cref="LocalizationServiceExtensions"/> or <see cref="ILocalizationService"/> before display.
/// Audit details are persisted via <see cref="YasGMP.Services.DatabaseServiceSuppliersExtensions.LogSupplierAuditAsync(YasGMP.Services.DatabaseService,int,string,int,string?,string,string,string?,System.Threading.CancellationToken)"/>,
/// ensuring the shared <see cref="YasGMP.Services.AuditService"/> surfaces the same events inside the MAUI experience.
/// </remarks>
public sealed class ExternalServicerCrudServiceAdapter : IExternalServicerCrudService
{
    private readonly ExternalServicerService _externalServicerService;
    private readonly DatabaseService _databaseService;
    /// <summary>
    /// Initializes a new instance of the ExternalServicerCrudServiceAdapter class.
    /// </summary>

    public ExternalServicerCrudServiceAdapter(ExternalServicerService externalServicerService, DatabaseService databaseService)
    {
        _externalServicerService = externalServicerService ?? throw new ArgumentNullException(nameof(externalServicerService));
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
    }
    /// <summary>
    /// Executes the get all async operation.
    /// </summary>

    public async Task<IReadOnlyList<ExternalServicer>> GetAllAsync()
        => await _externalServicerService.GetAllAsync().ConfigureAwait(false);
    /// <summary>
    /// Executes the try get by id async operation.
    /// </summary>

    public async Task<ExternalServicer?> TryGetByIdAsync(int id)
        => await _externalServicerService.TryGetByIdAsync(id).ConfigureAwait(false);
    /// <summary>
    /// Executes the create async operation.
    /// </summary>

    public async Task<CrudSaveResult> CreateAsync(ExternalServicer servicer, ExternalServicerCrudContext context)
    {
        if (servicer is null)
        {
            throw new ArgumentNullException(nameof(servicer));
        }

        Validate(servicer);
        var signature = ApplyContext(servicer, context);
        var metadata = CreateMetadata(context, signature);
        await _externalServicerService.CreateAsync(servicer, context.UserId).ConfigureAwait(false);
        await StampAsync(servicer, context, "CREATE").ConfigureAwait(false);
        return new CrudSaveResult(servicer.Id, metadata);
    }
    /// <summary>
    /// Executes the update async operation.
    /// </summary>

    public async Task<CrudSaveResult> UpdateAsync(ExternalServicer servicer, ExternalServicerCrudContext context)
    {
        if (servicer is null)
        {
            throw new ArgumentNullException(nameof(servicer));
        }

        Validate(servicer);
        var signature = ApplyContext(servicer, context);
        var metadata = CreateMetadata(context, signature);
        await _externalServicerService.UpdateAsync(servicer, context.UserId).ConfigureAwait(false);
        await StampAsync(servicer, context, "UPDATE").ConfigureAwait(false);
        return new CrudSaveResult(servicer.Id, metadata);
    }
    /// <summary>
    /// Executes the delete async operation.
    /// </summary>

    public async Task DeleteAsync(int id, ExternalServicerCrudContext context)
    {
        await _externalServicerService.DeleteAsync(id, context.UserId).ConfigureAwait(false);
    }
    /// <summary>
    /// Executes the validate operation.
    /// </summary>

    public void Validate(ExternalServicer servicer)
    {
        if (servicer is null)
        {
            throw new ArgumentNullException(nameof(servicer));
        }

        if (string.IsNullOrWhiteSpace(servicer.Name))
        {
            throw new InvalidOperationException("External servicer name is required.");
        }

        if (string.IsNullOrWhiteSpace(servicer.Email))
        {
            throw new InvalidOperationException("External servicer email is required.");
        }

        if (servicer.CooperationEnd is not null && servicer.CooperationStart is not null
            && servicer.CooperationEnd < servicer.CooperationStart)
        {
            throw new InvalidOperationException("Cooperation end cannot precede its start date.");
        }
    }
    /// <summary>
    /// Executes the normalize status operation.
    /// </summary>

    public string NormalizeStatus(string? status) => ExternalServicerCrudExtensions.NormalizeStatusDefault(status);

    private async Task StampAsync(ExternalServicer servicer, ExternalServicerCrudContext context, string action)
    {
        const string ensureSql = @"CREATE TABLE IF NOT EXISTS external_contractors (
  id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(255) NULL,
  code VARCHAR(255) NULL,
  registration_number VARCHAR(255) NULL,
  contact_person VARCHAR(255) NULL,
  email VARCHAR(255) NULL,
  phone VARCHAR(255) NULL,
  address VARCHAR(255) NULL,
  type VARCHAR(255) NULL,
  status VARCHAR(255) NULL,
  cooperation_start VARCHAR(255) NULL,
  cooperation_end VARCHAR(255) NULL,
  comment VARCHAR(255) NULL,
  digital_signature VARCHAR(255) NULL,
  note VARCHAR(255) NULL
);";

        await _databaseService.ExecuteNonQueryAsync(ensureSql, null).ConfigureAwait(false);

        const string updateSql = @"UPDATE external_contractors
SET status=@status,
    digital_signature=@signature,
    note=@note
WHERE id=@id";

        var parameters = new[]
        {
            new MySqlParameter("@status", NormalizeStatus(servicer.Status)),
            new MySqlParameter("@signature", servicer.DigitalSignature ?? string.Empty),
            new MySqlParameter("@note", servicer.ExtraNotes ?? servicer.Comment ?? string.Empty),
            new MySqlParameter("@id", servicer.Id)
        };

        await _databaseService.ExecuteNonQueryAsync(updateSql, parameters).ConfigureAwait(false);

        var details = string.Format(CultureInfo.InvariantCulture, "status={0}; type={1}", servicer.Status, servicer.Type);
        await _databaseService.LogSupplierAuditAsync(
            servicer.Id,
            action,
            context.UserId,
            details,
            context.Ip,
            context.DeviceInfo,
            context.SessionId).ConfigureAwait(false);
    }

    private static string ApplyContext(ExternalServicer servicer, ExternalServicerCrudContext context)
    {
        var signature = context.SignatureHash ?? servicer.DigitalSignature ?? string.Empty;
        servicer.DigitalSignature = signature;
        return signature;
    }

    private static SignatureMetadataDto CreateMetadata(ExternalServicerCrudContext context, string signature)
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
