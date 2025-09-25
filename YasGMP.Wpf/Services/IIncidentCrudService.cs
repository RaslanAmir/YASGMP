using System;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Abstraction over <see cref="YasGMP.Services.IncidentService"/> so the WPF shell can
/// run CRUD logic without binding directly to the infrastructure layer.
/// </summary>
public interface IIncidentCrudService
{
    Task<Incident?> TryGetByIdAsync(int id);

    Task<int> CreateAsync(Incident incident, IncidentCrudContext context);

    Task UpdateAsync(Incident incident, IncidentCrudContext context);

    void Validate(Incident incident);

    string NormalizeStatus(string? status);
}

/// <summary>
/// Captures the authenticated context required when persisting incident changes.
/// </summary>
/// <param name="UserId">Authenticated user identifier.</param>
/// <param name="Ip">Source IP address recorded for audit trails.</param>
/// <param name="DeviceInfo">Client device identifier.</param>
/// <param name="SessionId">Logical session id.</param>
public readonly record struct IncidentCrudContext(int UserId, string Ip, string DeviceInfo, string? SessionId)
{
    public static IncidentCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
        => new(
            userId <= 0 ? 1 : userId,
            string.IsNullOrWhiteSpace(ip) ? "unknown" : ip,
            string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo,
            string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId);
}
