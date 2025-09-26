using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Abstraction over <see cref="YasGMP.Services.ComponentService"/> so the WPF
/// shell can execute CRUD operations without pulling in the full database runtime
/// during unit tests.
/// </summary>
public interface IComponentCrudService
{
    Task<IReadOnlyList<Component>> GetAllAsync();

    Task<Component?> TryGetByIdAsync(int id);

    Task<int> CreateAsync(Component component, ComponentCrudContext context);

    Task UpdateAsync(Component component, ComponentCrudContext context);

    void Validate(Component component);

    string NormalizeStatus(string? status);
}

/// <summary>
/// Context metadata captured when persisting component edits so audit trails
/// and downstream services receive consistent identifiers.
/// </summary>
/// <param name="UserId">Authenticated user identifier.</param>
/// <param name="Ip">Source IP captured from the current session.</param>
/// <param name="DeviceInfo">Machine fingerprint or hostname.</param>
/// <param name="SessionId">Logical application session identifier.</param>
public readonly record struct ComponentCrudContext(int UserId, string Ip, string DeviceInfo, string? SessionId)
{
    public static ComponentCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
        => new(
            userId <= 0 ? 1 : userId,
            string.IsNullOrWhiteSpace(ip) ? "unknown" : ip,
            string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo,
            string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId);
}
