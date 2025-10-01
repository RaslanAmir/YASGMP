using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.AppCore.Models.Signatures;
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

        public async Task<CrudSaveResult> CreateAsync(Calibration calibration, CalibrationCrudContext context)
        {
            if (calibration is null)
            {
                throw new ArgumentNullException(nameof(calibration));
            }

            var signature = ApplyContext(calibration, context);
            var metadata = CreateMetadata(context, signature);
            await _inner.CreateAsync(calibration, context.UserId, metadata).ConfigureAwait(false);
            calibration.DigitalSignature = signature;
            return new CrudSaveResult(calibration.Id, metadata);
        }

        public async Task<CrudSaveResult> UpdateAsync(Calibration calibration, CalibrationCrudContext context)
        {
            if (calibration is null)
            {
                throw new ArgumentNullException(nameof(calibration));
            }

            var signature = ApplyContext(calibration, context);
            var metadata = CreateMetadata(context, signature);
            await _inner.UpdateAsync(calibration, context.UserId, metadata).ConfigureAwait(false);
            calibration.DigitalSignature = signature;
            return new CrudSaveResult(calibration.Id, metadata);
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

    private static string ApplyContext(Calibration calibration, CalibrationCrudContext context)
    {
        var signature = context.SignatureHash ?? calibration.DigitalSignature ?? string.Empty;
        calibration.DigitalSignature = signature;

        if (context.UserId > 0)
        {
            calibration.LastModifiedById = context.UserId;
        }

        if (!string.IsNullOrWhiteSpace(context.Ip))
        {
            calibration.SourceIp = context.Ip;
        }

        return signature;
    }

    private static SignatureMetadataDto CreateMetadata(CalibrationCrudContext context, string signature)
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
