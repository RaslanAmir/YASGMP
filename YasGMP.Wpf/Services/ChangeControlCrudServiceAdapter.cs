using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    public async Task<int> CreateAsync(ChangeControl changeControl, ChangeControlCrudContext context)
    {
        if (changeControl is null)
        {
            throw new ArgumentNullException(nameof(changeControl));
        }

        Validate(changeControl);
        return await _service
            .CreateAsync(
                changeControl,
                context.UserId,
                context.IpAddress,
                context.DeviceInfo,
                context.SessionId)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(ChangeControl changeControl, ChangeControlCrudContext context)
    {
        if (changeControl is null)
        {
            throw new ArgumentNullException(nameof(changeControl));
        }

        Validate(changeControl);
        await _service
            .UpdateAsync(
                changeControl,
                context.UserId,
                context.IpAddress,
                context.DeviceInfo,
                context.SessionId)
            .ConfigureAwait(false);
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
}
