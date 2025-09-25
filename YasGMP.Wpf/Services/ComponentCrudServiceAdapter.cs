using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    public async Task<int> CreateAsync(Component component, ComponentCrudContext context)
    {
        if (component is null) throw new ArgumentNullException(nameof(component));
        await _inner.CreateAsync(component, context.UserId).ConfigureAwait(false);
        return component.Id;
    }

    public Task UpdateAsync(Component component, ComponentCrudContext context)
    {
        if (component is null) throw new ArgumentNullException(nameof(component));
        return _inner.UpdateAsync(component, context.UserId);
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
}
