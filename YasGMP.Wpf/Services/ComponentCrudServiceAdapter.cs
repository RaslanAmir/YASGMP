using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Default <see cref="IComponentCrudService"/> implementation backed by the
/// shared <see cref="ComponentService"/> from <c>YasGMP.AppCore</c>.
/// </summary>
public sealed class ComponentCrudServiceAdapter : IComponentCrudService
{
    private readonly ComponentService _inner;

    public ComponentCrudServiceAdapter(ComponentService inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<IReadOnlyList<Component>> GetAllAsync()
        => await _inner.GetAllAsync().ConfigureAwait(false);

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

    public async Task<CrudSaveResult> CreateAsync(Component component, ComponentCrudContext context)
    {
        if (component is null) throw new ArgumentNullException(nameof(component));

        var signature = ApplyContext(component, context);
        await _inner.CreateAsync(component, context.UserId).ConfigureAwait(false);

        return new CrudSaveResult(component.Id, CreateMetadata(context, signature));
    }

    public async Task<CrudSaveResult> UpdateAsync(Component component, ComponentCrudContext context)
    {
        if (component is null) throw new ArgumentNullException(nameof(component));

        var signature = ApplyContext(component, context);
        await _inner.UpdateAsync(component, context.UserId).ConfigureAwait(false);

        return new CrudSaveResult(component.Id, CreateMetadata(context, signature));
    }

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
