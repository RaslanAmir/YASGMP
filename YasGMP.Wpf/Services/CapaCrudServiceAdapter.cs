using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Exposes CAPA module workflows in the WPF shell by forwarding them to the shared MAUI
/// <see cref="YasGMP.Services.CAPAService"/> implementation.
/// </summary>
/// <remarks>
/// CAPA view models invoke this adapter, which uses <see cref="YasGMP.Services.CAPAService"/> and
/// the cross-platform <see cref="YasGMP.Services.AuditService"/> to keep persistence and logging in sync with the MAUI app.
/// Await operations off the UI thread and dispatch UI updates through <see cref="WpfUiDispatcher"/>. The
/// <see cref="CrudSaveResult"/> returned by create/update calls conveys identifiers, signature metadata, and status text that
/// callers localize with <see cref="LocalizationServiceExtensions"/> or <see cref="ILocalizationService"/> prior to presentation.
/// </remarks>
public sealed class CapaCrudServiceAdapter : ICapaCrudService
{
    private readonly CAPAService _service;
    /// <summary>
    /// Initializes a new instance of the CapaCrudServiceAdapter class.
    /// </summary>

    public CapaCrudServiceAdapter(CAPAService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }
    /// <summary>
    /// Executes the get all async operation.
    /// </summary>

    public async Task<IReadOnlyList<CapaCase>> GetAllAsync()
        => await _service.GetAllAsync().ConfigureAwait(false);
    /// <summary>
    /// Executes the try get by id async operation.
    /// </summary>

    public async Task<CapaCase?> TryGetByIdAsync(int id)
    {
        try
        {
            return await _service.GetByIdAsync(id).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }
    /// <summary>
    /// Executes the create async operation.
    /// </summary>

    public async Task<CrudSaveResult> CreateAsync(CapaCase capa, CapaCrudContext context)
    {
        if (capa is null)
        {
            throw new ArgumentNullException(nameof(capa));
        }

        Validate(capa);
        var metadata = CreateMetadata(context, capa.DigitalSignature);
        await _service.CreateAsync(capa, context.UserId, metadata).ConfigureAwait(false);
        return new CrudSaveResult(capa.Id, metadata);
    }
    /// <summary>
    /// Executes the update async operation.
    /// </summary>

    public async Task<CrudSaveResult> UpdateAsync(CapaCase capa, CapaCrudContext context)
    {
        if (capa is null)
        {
            throw new ArgumentNullException(nameof(capa));
        }

        Validate(capa);
        var metadata = CreateMetadata(context, capa.DigitalSignature);
        await _service.UpdateAsync(capa, context.UserId, metadata).ConfigureAwait(false);
        return new CrudSaveResult(capa.Id, metadata);
    }
    /// <summary>
    /// Executes the validate operation.
    /// </summary>

    public void Validate(CapaCase capa)
    {
        if (capa is null)
        {
            throw new ArgumentNullException(nameof(capa));
        }

        _service.ValidateCapa(capa);
    }
    /// <summary>
    /// Executes the normalize status operation.
    /// </summary>

    public string NormalizeStatus(string? status)
        => string.IsNullOrWhiteSpace(status)
            ? "OPEN"
            : status.Trim().ToUpperInvariant();
    /// <summary>
    /// Executes the normalize priority operation.
    /// </summary>

    public string NormalizePriority(string? priority)
        => string.IsNullOrWhiteSpace(priority)
            ? "Medium"
            : priority.Trim();

    private static SignatureMetadataDto? CreateMetadata(CapaCrudContext context, string? signature)
        => new SignatureMetadataDto
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
