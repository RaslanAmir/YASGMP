using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models.DTO;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

public sealed class ChangeControlCrudServiceAdapter : IChangeControlCrudService
{
    private readonly ChangeControlService _service;

    public ChangeControlCrudServiceAdapter(ChangeControlService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public async Task<IReadOnlyList<ChangeControl>> GetAllAsync()
        => await _service.GetAllAsync().ConfigureAwait(false);

    public async Task<ChangeControl?> TryGetByIdAsync(int id)
        => await _service.TryGetByIdAsync(id).ConfigureAwait(false);

    public async Task<CrudSaveResult> CreateAsync(ChangeControl changeControl, ChangeControlCrudContext context)
    {
        if (changeControl is null)
        {
            throw new ArgumentNullException(nameof(changeControl));
        }

        Validate(changeControl);
        var signature = ApplyContext(changeControl, context);
        var metadata = CreateMetadata(context, signature);
        var id = await _service
            .CreateAsync(
                changeControl,
                context.UserId,
                context.IpAddress,
                context.DeviceInfo,
                context.SessionId)
            .ConfigureAwait(false);
        changeControl.Id = id;
        return new CrudSaveResult(id, metadata);
    }

    public async Task<CrudSaveResult> UpdateAsync(ChangeControl changeControl, ChangeControlCrudContext context)
    {
        if (changeControl is null)
        {
            throw new ArgumentNullException(nameof(changeControl));
        }

        Validate(changeControl);
        var signature = ApplyContext(changeControl, context);
        var metadata = CreateMetadata(context, signature);
        await _service
            .UpdateAsync(
                changeControl,
                context.UserId,
                context.IpAddress,
                context.DeviceInfo,
                context.SessionId)
            .ConfigureAwait(false);
        return new CrudSaveResult(changeControl.Id, metadata);
    }

    public void Validate(ChangeControl changeControl)
    {
        if (changeControl is null)
        {
            throw new ArgumentNullException(nameof(changeControl));
        }

        _service.Validate(changeControl);
    }

    public string NormalizeStatus(string? status) => _service.NormalizeStatus(status);

    private static string ApplyContext(ChangeControl changeControl, ChangeControlCrudContext context)
    {
        var signature = context.SignatureHash ?? changeControl.DigitalSignature ?? string.Empty;
        changeControl.DigitalSignature = signature;
        changeControl.LastModifiedById = context.UserId;
        changeControl.LastModified = DateTime.UtcNow;
        return signature;
    }

    private static SignatureMetadataDto CreateMetadata(ChangeControlCrudContext context, string signature)
        => new()
        {
            Id = context.SignatureId,
            Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
            Method = context.SignatureMethod,
            Status = context.SignatureStatus,
            Note = context.SignatureNote,
            Session = context.SessionId,
            Device = context.DeviceInfo,
            IpAddress = context.IpAddress
        };
}

