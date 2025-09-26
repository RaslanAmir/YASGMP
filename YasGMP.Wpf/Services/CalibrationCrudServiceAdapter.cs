using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Default <see cref="ICalibrationCrudService"/> implementation backed by the shared
/// <see cref="CalibrationService"/>.
/// </summary>
public sealed class CalibrationCrudServiceAdapter : ICalibrationCrudService
{
    private readonly CalibrationService _inner;

    public CalibrationCrudServiceAdapter(CalibrationService inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task<IReadOnlyList<Calibration>> GetAllAsync()
        => await _inner.GetAllAsync().ConfigureAwait(false);

    public async Task<Calibration?> TryGetByIdAsync(int id)
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

    public async Task<int> CreateAsync(Calibration calibration, CalibrationCrudContext context)
    {
        if (calibration is null)
        {
            throw new ArgumentNullException(nameof(calibration));
        }

        ApplyContext(calibration, context);
        await _inner.CreateAsync(calibration, context.UserId).ConfigureAwait(false);
        return calibration.Id;
    }

    public Task UpdateAsync(Calibration calibration, CalibrationCrudContext context)
    {
        if (calibration is null)
        {
            throw new ArgumentNullException(nameof(calibration));
        }

        ApplyContext(calibration, context);
        return _inner.UpdateAsync(calibration, context.UserId);
    }

    public void Validate(Calibration calibration)
    {
        if (calibration is null)
        {
            throw new ArgumentNullException(nameof(calibration));
        }

        if (calibration.ComponentId <= 0)
        {
            throw new InvalidOperationException("Calibration must be linked to a component.");
        }

        if (!calibration.SupplierId.HasValue || calibration.SupplierId.Value <= 0)
        {
            throw new InvalidOperationException("Supplier is required.");
        }

        if (calibration.CalibrationDate == default)
        {
            throw new InvalidOperationException("Calibration date is required.");
        }

        if (calibration.NextDue == default)
        {
            throw new InvalidOperationException("Next due date is required.");
        }

        if (calibration.NextDue < calibration.CalibrationDate)
        {
            throw new InvalidOperationException("Next due date must be on or after the calibration date.");
        }

        if (string.IsNullOrWhiteSpace(calibration.Result))
        {
            throw new InvalidOperationException("Calibration result is required.");
        }
    }

    private static void ApplyContext(Calibration calibration, CalibrationCrudContext context)
    {
        calibration.LastModifiedById = context.UserId;
        calibration.SourceIp = context.Ip;
    }
}
