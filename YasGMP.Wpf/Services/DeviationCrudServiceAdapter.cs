using System;
using System.Threading.Tasks;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Models;
using YasGMP.Models.Enums;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Bridges deviation CRUD workflows from the WPF shell into the shared MAUI
/// <see cref="YasGMP.Services.DeviationService"/> implementation so audit and signature
/// flows stay unified across shells.
/// </summary>
/// <remarks>
/// Deviation editor view models call this adapter on the dispatcher thread. All asynchronous
/// work is awaited off-thread, and callers marshal UI updates through <see cref="WpfUiDispatcher"/>.
/// Each persistence call produces a <see cref="CrudSaveResult"/> that surfaces the saved identifier
/// and the <see cref="SignatureMetadataDto"/> manifest captured for audit pipelines.
/// </remarks>
public sealed class DeviationCrudServiceAdapter : IDeviationCrudService
{
    private readonly DeviationService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviationCrudServiceAdapter"/> class.
    /// </summary>
    /// <param name="service">Shared deviation domain service.</param>
    public DeviationCrudServiceAdapter(DeviationService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <inheritdoc />
    public async Task<Deviation?> TryGetByIdAsync(int id)
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

    /// <inheritdoc />
    public async Task<CrudSaveResult> CreateAsync(Deviation deviation, DeviationCrudContext context)
    {
        if (deviation is null)
        {
            throw new ArgumentNullException(nameof(deviation));
        }

        Validate(deviation);
        deviation.Status = NormalizeStatus(deviation.Status);
        var signature = ApplyContext(deviation, context);
        var metadata = CreateMetadata(context, signature);
        var id = await _service
            .CreateAsync(
                deviation,
                context.UserId,
                context.Ip,
                context.DeviceInfo,
                context.SessionId,
                context.SignatureId,
                signature)
            .ConfigureAwait(false);
        deviation.Id = id;
        return new CrudSaveResult(id, metadata);
    }

    /// <inheritdoc />
    public async Task<CrudSaveResult> UpdateAsync(Deviation deviation, DeviationCrudContext context)
    {
        if (deviation is null)
        {
            throw new ArgumentNullException(nameof(deviation));
        }

        Validate(deviation);
        deviation.Status = NormalizeStatus(deviation.Status);
        var signature = ApplyContext(deviation, context);
        var metadata = CreateMetadata(context, signature);
        await _service
            .UpdateAsync(
                deviation,
                context.UserId,
                context.Ip,
                context.DeviceInfo,
                context.SessionId,
                context.SignatureId,
                signature)
            .ConfigureAwait(false);
        return new CrudSaveResult(deviation.Id, metadata);
    }

    /// <inheritdoc />
    public void Validate(Deviation deviation)
    {
        if (deviation is null)
        {
            throw new ArgumentNullException(nameof(deviation));
        }

        if (string.IsNullOrWhiteSpace(deviation.Title))
        {
            throw new InvalidOperationException("Deviation title is required.");
        }

        if (string.IsNullOrWhiteSpace(deviation.Description))
        {
            throw new InvalidOperationException("Deviation description is required.");
        }

        if (string.IsNullOrWhiteSpace(deviation.Severity))
        {
            throw new InvalidOperationException("Deviation severity is required.");
        }

        deviation.Title = deviation.Title.Trim();
        deviation.Description = deviation.Description.Trim();
        deviation.Severity = deviation.Severity.Trim().ToUpperInvariant();
        deviation.ReportedAt ??= DateTime.UtcNow;
    }

    /// <inheritdoc />
    public string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return DeviationStatus.OPEN.ToString();
        }

        var normalized = status.Trim().ToUpperInvariant();
        return Enum.TryParse(typeof(DeviationStatus), normalized, out var parsed)
            ? ((DeviationStatus)parsed).ToString()
            : normalized;
    }

    private static string ApplyContext(Deviation deviation, DeviationCrudContext context)
    {
        var signature = context.SignatureHash ?? deviation.DigitalSignature ?? string.Empty;
        deviation.DigitalSignature = signature;
        deviation.LastModified = DateTime.UtcNow;
        deviation.LastModifiedById = context.UserId;

        if (!string.IsNullOrWhiteSpace(context.Ip))
        {
            deviation.SourceIp = context.Ip;
        }

        return signature;
    }

    private static SignatureMetadataDto CreateMetadata(DeviationCrudContext context, string signature)
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
