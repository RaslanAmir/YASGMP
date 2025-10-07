using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Routes Component module interactions from the WPF shell into the shared MAUI
/// <see cref="YasGMP.Services.ComponentService"/> implementation.
/// </summary>
/// <remarks>
/// View models issue commands to this adapter, which forwards them to
/// <see cref="YasGMP.Services.ComponentService"/> (and the associated <see cref="YasGMP.Services.AuditService"/>)
/// so the same AppCore logic is exercised regardless of shell. Methods are awaited off the UI thread;
/// callers are expected to marshal updates via <see cref="WpfUiDispatcher"/>. The <see cref="CrudSaveResult"/>
/// payload contains identifiers, signature metadata, and status text that should be localized with
/// <see cref="LocalizationServiceExtensions"/> or <see cref="ILocalizationService"/> before being shown in the ribbon.
/// Audit entries emitted here surface within the MAUI experiences because the shared <see cref="YasGMP.Services.AuditService"/>
/// is used to capture them.
/// </remarks>
public sealed class ComponentCrudServiceAdapter : IComponentCrudService
{
    private readonly ComponentService _inner;
    /// <summary>
    /// Initializes a new instance of the ComponentCrudServiceAdapter class.
    /// </summary>

    public ComponentCrudServiceAdapter(ComponentService inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }
    /// <summary>
    /// Executes the get all async operation.
    /// </summary>

    public async Task<IReadOnlyList<Component>> GetAllAsync()
        => await _inner.GetAllAsync().ConfigureAwait(false);
    /// <summary>
    /// Executes the try get by id async operation.
    /// </summary>

    public async Task<Component?> TryGetByIdAsync(int id)
    {
        try
        {
            return await _inner.GetByIdAsync(id).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }
    /// <summary>
    /// Executes the create async operation.
    /// </summary>

    public async Task<CrudSaveResult> CreateAsync(Component component, ComponentCrudContext context)
    {
        if (component is null) throw new ArgumentNullException(nameof(component));

        var signature = ApplyContext(component, context);
        var saveContext = ComponentSaveContext.Create(
            signature,
            context.Ip,
            context.DeviceInfo,
            context.SessionId);
        await _inner.CreateAsync(component, context.UserId, saveContext).ConfigureAwait(false);

        return new CrudSaveResult(component.Id, CreateMetadata(context, signature));
    }
    /// <summary>
    /// Executes the update async operation.
    /// </summary>

    public async Task<CrudSaveResult> UpdateAsync(Component component, ComponentCrudContext context)
    {
        if (component is null) throw new ArgumentNullException(nameof(component));

        var signature = ApplyContext(component, context);
        var saveContext = ComponentSaveContext.Create(
            signature,
            context.Ip,
            context.DeviceInfo,
            context.SessionId);
        await _inner.UpdateAsync(component, context.UserId, saveContext).ConfigureAwait(false);

        return new CrudSaveResult(component.Id, CreateMetadata(context, signature));
    }
    /// <summary>
    /// Executes the validate operation.
    /// </summary>

    public void Validate(Component component)
    {
        if (component is null)
        {
            throw new ArgumentNullException(nameof(component));
        }

        if (string.IsNullOrWhiteSpace(component.Name))
        {
            throw new InvalidOperationException("Component name is required.");
        }

        if (string.IsNullOrWhiteSpace(component.Code))
        {
            throw new InvalidOperationException("Component code is required.");
        }

        if (component.MachineId <= 0)
        {
            throw new InvalidOperationException("Component must be linked to a machine.");
        }

        if (string.IsNullOrWhiteSpace(component.SopDoc))
        {
            throw new InvalidOperationException("SOP document is required.");
        }
    }
    /// <summary>
    /// Executes the normalize status operation.
    /// </summary>

    public string NormalizeStatus(string? status)
        => string.IsNullOrWhiteSpace(status) ? "active" : status.Trim().ToLowerInvariant();

    private static string ApplyContext(Component component, ComponentCrudContext context)
    {
        var signature = context.SignatureHash ?? component.DigitalSignature ?? string.Empty;
        component.DigitalSignature = signature;
        return signature;
    }

    private static SignatureMetadataDto CreateMetadata(ComponentCrudContext context, string signature)
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
