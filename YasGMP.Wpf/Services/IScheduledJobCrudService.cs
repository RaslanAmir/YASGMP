using System;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter-friendly abstraction for scheduled job persistence so the WPF shell can
/// operate without binding directly to database-specific infrastructure.
/// </summary>
public interface IScheduledJobCrudService
{
    Task<ScheduledJob?> TryGetByIdAsync(int id);

    Task<int> CreateAsync(ScheduledJob job, ScheduledJobCrudContext context);

    Task UpdateAsync(ScheduledJob job, ScheduledJobCrudContext context);

    Task ExecuteAsync(int jobId, ScheduledJobCrudContext context);

    Task AcknowledgeAsync(int jobId, ScheduledJobCrudContext context);

    void Validate(ScheduledJob job);

    string NormalizeStatus(string? status);
}

/// <summary>
/// Ambient metadata required for auditing scheduled job operations.
/// </summary>
/// <param name="UserId">Authenticated user identifier.</param>
/// <param name="UserName">Display/user name captured for audit manifest.</param>
/// <param name="Ip">Source IP address.</param>
/// <param name="DeviceInfo">Originating device information.</param>
/// <param name="SessionId">Logical session identifier.</param>
public readonly record struct ScheduledJobCrudContext(int UserId, string UserName, string Ip, string DeviceInfo, string? SessionId)
{
    public static ScheduledJobCrudContext Create(int userId, string? userName, string ip, string deviceInfo, string? sessionId)
        => new(
            userId <= 0 ? 1 : userId,
            string.IsNullOrWhiteSpace(userName) ? $"user:{(userId <= 0 ? 1 : userId)}" : userName.Trim(),
            string.IsNullOrWhiteSpace(ip) ? "unknown" : ip,
            string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo,
            string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId);
}
