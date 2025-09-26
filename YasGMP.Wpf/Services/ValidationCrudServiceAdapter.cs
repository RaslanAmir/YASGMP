using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Default <see cref="IValidationCrudService"/> implementation backed by the shared
/// <see cref="ValidationService"/>.
/// </summary>
public sealed class ValidationCrudServiceAdapter : IValidationCrudService
{
    private readonly ValidationService _inner;

    public ValidationCrudServiceAdapter(ValidationService inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<IReadOnlyList<Validation>> GetAllAsync()
        => await _inner.GetAllAsync().ConfigureAwait(false);

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

    public async Task<int> CreateAsync(Validation validation, ValidationCrudContext context)
    {
        if (validation is null)
        {
            throw new ArgumentNullException(nameof(validation));
        }

        ApplyContext(validation, context);
        await _inner.CreateAsync(validation, context.UserId).ConfigureAwait(false);
        return validation.Id;
    }

    public Task UpdateAsync(Validation validation, ValidationCrudContext context)
    {
        if (validation is null)
        {
            throw new ArgumentNullException(nameof(validation));
        }

        ApplyContext(validation, context);
        return _inner.UpdateAsync(validation, context.UserId);
    }

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

    private static void ApplyContext(Validation validation, ValidationCrudContext context)
    {
        validation.LastModifiedById = context.UserId;
        validation.SourceIp = context.Ip;
        validation.SessionId = context.SessionId ?? string.Empty;
    }
}
