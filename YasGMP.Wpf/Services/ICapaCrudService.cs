using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter abstraction over <see cref="YasGMP.Services.CAPAService"/> so the WPF shell
/// can execute CAPA CRUD operations without binding directly to infrastructure types.
/// </summary>
public interface ICapaCrudService
{
    Task<IReadOnlyList<CapaCase>> GetAllAsync();

    Task<CapaCase?> TryGetByIdAsync(int id);

    Task<int> CreateAsync(CapaCase capa, CapaCrudContext context);

    Task UpdateAsync(CapaCase capa, CapaCrudContext context);

    void Validate(CapaCase capa);

    string NormalizeStatus(string? status);

    string NormalizePriority(string? priority);
}

/// <summary>
/// Captures authenticated metadata that must flow alongside CAPA saves.
/// </summary>
/// <param name="UserId">Authenticated operator identifier.</param>
/// <param name="Ip">Source IP recorded for audit.</param>
/// <param name="DeviceInfo">Client device fingerprint.</param>
/// <param name="SessionId">Logical session identifier.</param>
public readonly record struct CapaCrudContext(int UserId, string Ip, string DeviceInfo, string? SessionId)
{
    public static CapaCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
        => new(
            userId <= 0 ? 1 : userId,
            string.IsNullOrWhiteSpace(ip) ? "unknown" : ip,
            string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo,
            string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId);
}
