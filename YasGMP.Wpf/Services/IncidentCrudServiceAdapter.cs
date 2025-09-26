using System;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter that exposes <see cref="IncidentService"/> CRUD operations to the WPF shell.
/// </summary>
public sealed class IncidentCrudServiceAdapter : IIncidentCrudService
{
    private readonly IncidentService _service;

    public IncidentCrudServiceAdapter(IncidentService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public async Task<Incident?> TryGetByIdAsync(int id)
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

    public async Task<int> CreateAsync(Incident incident, IncidentCrudContext context)
    {
        if (incident is null)
        {
            throw new ArgumentNullException(nameof(incident));
        }

        Validate(incident);
        await _service.CreateAsync(incident, context.UserId).ConfigureAwait(false);
        return incident.Id;
    }

    public async Task UpdateAsync(Incident incident, IncidentCrudContext context)
    {
        if (incident is null)
        {
            throw new ArgumentNullException(nameof(incident));
        }

        Validate(incident);
        await _service.UpdateAsync(incident, context.UserId).ConfigureAwait(false);
    }

    public void Validate(Incident incident)
    {
        if (incident is null)
        {
            throw new ArgumentNullException(nameof(incident));
        }

        if (string.IsNullOrWhiteSpace(incident.Title))
        {
            throw new InvalidOperationException("Incident title is required.");
        }

        if (string.IsNullOrWhiteSpace(incident.Description))
        {
            throw new InvalidOperationException("Incident description is required.");
        }

        if (incident.DetectedAt == default)
        {
            throw new InvalidOperationException("Detected date must be provided.");
        }
    }

    public string NormalizeStatus(string? status)
        => string.IsNullOrWhiteSpace(status) ? "REPORTED" : status.Trim().ToUpperInvariant();
}
