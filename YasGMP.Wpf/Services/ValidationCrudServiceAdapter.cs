using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Forwards validation module requests from the WPF shell into the shared MAUI
/// <see cref="YasGMP.Services.ValidationService"/> implementation.
/// </summary>
/// <remarks>
/// Validation view models invoke this adapter, which delegates to <see cref="YasGMP.Services.ValidationService"/> and the
/// shared <see cref="YasGMP.Services.AuditService"/> so persistence and audit behavior mirrors the MAUI experience. Await the
/// asynchronous operations away from the UI thread and marshal UI updates through <see cref="WpfUiDispatcher"/>. The
/// <see cref="CrudSaveResult"/> payload carries identifiers, status text, and signature metadata that require localization via
/// <see cref="LocalizationServiceExtensions"/> or <see cref="ILocalizationService"/> before they are surfaced in the shell.
/// </remarks>
public sealed class ValidationCrudServiceAdapter : IValidationCrudService
{
    private readonly ValidationService _inner;
    /// <summary>
    /// Initializes a new instance of the ValidationCrudServiceAdapter class.
    /// </summary>

    public ValidationCrudServiceAdapter(ValidationService inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }
    /// <summary>
    /// Executes the get all async operation.
    /// </summary>

    public async Task<IReadOnlyList<Validation>> GetAllAsync()
        => await _inner.GetAllAsync().ConfigureAwait(false);
    /// <summary>
    /// Executes the try get by id async operation.
    /// </summary>

    public async Task<Validation?> TryGetByIdAsync(int id)
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

    public async Task<CrudSaveResult> CreateAsync(Validation validation, ValidationCrudContext context)
    {
        if (validation is null)
        {
            throw new ArgumentNullException(nameof(validation));
        }

        var signature = ApplyContext(validation, context);
        await _inner.CreateAsync(validation, context.UserId).ConfigureAwait(false);
        return new CrudSaveResult(validation.Id, CreateMetadata(context, signature));
    }
    /// <summary>
    /// Executes the update async operation.
    /// </summary>

    public async Task<CrudSaveResult> UpdateAsync(Validation validation, ValidationCrudContext context)
    {
        if (validation is null)
        {
            throw new ArgumentNullException(nameof(validation));
        }

        var signature = ApplyContext(validation, context);
        await _inner.UpdateAsync(validation, context.UserId).ConfigureAwait(false);
        return new CrudSaveResult(validation.Id, CreateMetadata(context, signature));
    }
    /// <summary>
    /// Executes the validate operation.
    /// </summary>

    public void Validate(Validation validation)
    {
        if (validation is null)
        {
            throw new ArgumentNullException(nameof(validation));
        }

        if (string.IsNullOrWhiteSpace(validation.Type))
        {
            throw new InvalidOperationException("Validation type is required.");
        }

        if (string.IsNullOrWhiteSpace(validation.Code))
        {
            throw new InvalidOperationException("Protocol number/code is required.");
        }

        if (validation.MachineId is null && validation.ComponentId is null)
        {
            throw new InvalidOperationException("Select a machine or component for the validation.");
        }

        if (validation.DateStart is null)
        {
            throw new InvalidOperationException("Start date is required.");
        }

        if (validation.DateEnd is not null && validation.DateEnd < validation.DateStart)
        {
            throw new InvalidOperationException("End date must be on or after the start date.");
        }
    }

    private static string ApplyContext(Validation validation, ValidationCrudContext context)
    {
        validation.LastModifiedById = context.UserId;
        validation.SourceIp = context.Ip;
        validation.SessionId = context.SessionId ?? string.Empty;
        var signature = context.SignatureHash ?? validation.DigitalSignature ?? string.Empty;
        validation.DigitalSignature = signature;
        return signature;
    }

    private static SignatureMetadataDto CreateMetadata(ValidationCrudContext context, string signature)
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
