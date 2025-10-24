using System;
using System.Threading.Tasks;
using YasGMP.Models.DTO;
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

    public async Task<CrudSaveResult> CreateAsync(Incident incident, IncidentCrudContext context)
    {
        if (incident is null)
        {
            throw new ArgumentNullException(nameof(incident));
        }

        Validate(incident);
        var signature = ApplyContext(incident, context);
        await _service.CreateAsync(incident, context.UserId).ConfigureAwait(false);
        return new CrudSaveResult(incident.Id, CreateMetadata(context, signature));
    }

    public async Task<CrudSaveResult> UpdateAsync(Incident incident, IncidentCrudContext context)
    {
        if (incident is null)
        {
            throw new ArgumentNullException(nameof(incident));
        }

        Validate(incident);
        var signature = ApplyContext(incident, context);
        await _service.UpdateAsync(incident, context.UserId).ConfigureAwait(false);
        return new CrudSaveResult(incident.Id, CreateMetadata(context, signature));
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

    private static string ApplyContext(Incident incident, IncidentCrudContext context)
    {
        var signature = context.SignatureHash ?? incident.DigitalSignature ?? string.Empty;
        incident.DigitalSignature = signature;
        incident.LastModifiedById = context.UserId;
        incident.LastModified = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(context.Ip))
        {
            incident.SourceIp = context.Ip;
        }

        return signature;
    }

    private static SignatureMetadataDto CreateMetadata(IncidentCrudContext context, string signature)
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

