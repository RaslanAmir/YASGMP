using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services;

public interface IChangeControlCrudService
{
    Task<IReadOnlyList<ChangeControl>> GetAllAsync();

    Task<ChangeControl?> TryGetByIdAsync(int id);

    Task<int> CreateAsync(ChangeControl changeControl, ChangeControlCrudContext context);

    Task UpdateAsync(ChangeControl changeControl, ChangeControlCrudContext context);

    void Validate(ChangeControl changeControl);

    string NormalizeStatus(string? status);
}

public readonly record struct ChangeControlCrudContext(int UserId, string IpAddress, string DeviceInfo, string? SessionId)
{
    public static ChangeControlCrudContext Create(int userId, string? ip, string? device, string? sessionId)
        => new(
            userId <= 0 ? 1 : userId,
            string.IsNullOrWhiteSpace(ip) ? "unknown" : ip!,
            string.IsNullOrWhiteSpace(device) ? "WPF" : device!,
            string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId);
}
