using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter that exposes <see cref="CAPAService"/> CRUD workflows to the WPF shell.
/// </summary>
public sealed class CapaCrudServiceAdapter : ICapaCrudService
{
    private readonly CAPAService _service;

    public CapaCrudServiceAdapter(CAPAService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public async Task<IReadOnlyList<CapaCase>> GetAllAsync()
        => await _service.GetAllAsync().ConfigureAwait(false);

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

    public async Task<int> CreateAsync(CapaCase capa, CapaCrudContext context)
    {
        if (capa is null)
        {
            throw new ArgumentNullException(nameof(capa));
        }

        Validate(capa);
        await _service.CreateAsync(capa, context.UserId).ConfigureAwait(false);
        return capa.Id;
    }

    public async Task UpdateAsync(CapaCase capa, CapaCrudContext context)
    {
        if (capa is null)
        {
            throw new ArgumentNullException(nameof(capa));
        }

        Validate(capa);
        await _service.UpdateAsync(capa, context.UserId).ConfigureAwait(false);
    }

    public void Validate(CapaCase capa)
    {
        if (capa is null)
        {
            throw new ArgumentNullException(nameof(capa));
        }

        _service.ValidateCapa(capa);
    }

    public string NormalizeStatus(string? status)
        => string.IsNullOrWhiteSpace(status)
            ? "OPEN"
            : status.Trim().ToUpperInvariant();

    public string NormalizePriority(string? priority)
        => string.IsNullOrWhiteSpace(priority)
            ? "Medium"
            : priority.Trim();
}
