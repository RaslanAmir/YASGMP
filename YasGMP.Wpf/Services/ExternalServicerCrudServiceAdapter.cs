using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter that allows the WPF shell to reuse the shared external servicer services while capturing
/// the extra audit metadata required by desktop saves.
/// </summary>
public sealed class ExternalServicerCrudServiceAdapter : IExternalServicerCrudService
{
    private readonly ExternalServicerService _externalServicerService;
    private readonly DatabaseService _databaseService;

    public ExternalServicerCrudServiceAdapter(ExternalServicerService externalServicerService, DatabaseService databaseService)
    {
        _externalServicerService = externalServicerService ?? throw new ArgumentNullException(nameof(externalServicerService));
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
    }

    public async Task<IReadOnlyList<ExternalServicer>> GetAllAsync()
        => await _externalServicerService.GetAllAsync().ConfigureAwait(false);

    public async Task<ExternalServicer?> TryGetByIdAsync(int id)
        => await _externalServicerService.TryGetByIdAsync(id).ConfigureAwait(false);

    public async Task<int> CreateAsync(ExternalServicer servicer, ExternalServicerCrudContext context)
    {
        if (servicer is null)
        {
            throw new ArgumentNullException(nameof(servicer));
        }

        Validate(servicer);
        await _externalServicerService.CreateAsync(servicer, context.UserId).ConfigureAwait(false);
        await StampAsync(servicer, context, "CREATE").ConfigureAwait(false);
        return servicer.Id;
    }

    public async Task UpdateAsync(ExternalServicer servicer, ExternalServicerCrudContext context)
    {
        if (servicer is null)
        {
            throw new ArgumentNullException(nameof(servicer));
        }

        Validate(servicer);
        await _externalServicerService.UpdateAsync(servicer, context.UserId).ConfigureAwait(false);
        await StampAsync(servicer, context, "UPDATE").ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, ExternalServicerCrudContext context)
    {
        await _externalServicerService.DeleteAsync(id, context.UserId).ConfigureAwait(false);
    }

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

}
