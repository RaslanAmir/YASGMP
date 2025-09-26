using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter-friendly abstraction exposing external servicer CRUD operations to the WPF shell.
/// </summary>
public interface IExternalServicerCrudService
{
    Task<IReadOnlyList<ExternalServicer>> GetAllAsync();

    Task<ExternalServicer?> TryGetByIdAsync(int id);

    Task<int> CreateAsync(ExternalServicer servicer, ExternalServicerCrudContext context);

    Task UpdateAsync(ExternalServicer servicer, ExternalServicerCrudContext context);

    Task DeleteAsync(int id, ExternalServicerCrudContext context);

    void Validate(ExternalServicer servicer);

    string NormalizeStatus(string? status);
}

/// <summary>
/// Metadata captured when persisting external servicer edits to feed audit/trace data.
/// </summary>
/// <param name="UserId">Authenticated operator identifier.</param>
/// <param name="Ip">Source IP captured from the current session.</param>
/// <param name="DeviceInfo">Device or workstation fingerprint.</param>
/// <param name="SessionId">Logical session identifier.</param>
public readonly record struct ExternalServicerCrudContext(int UserId, string Ip, string DeviceInfo, string? SessionId)
{
    public static ExternalServicerCrudContext Create(int userId, string? ip, string? deviceInfo, string? sessionId)
        => new(
            userId <= 0 ? 1 : userId,
            string.IsNullOrWhiteSpace(ip) ? "unknown" : ip!,
            string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo!,
            string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) : sessionId);
}

/// <summary>Helper extensions for external servicer metadata transformations.</summary>
public static class ExternalServicerCrudExtensions
{
    /// <summary>Normalises external servicer status strings to lower-case tokens.</summary>
    public static string NormalizeStatusDefault(string? status)
        => string.IsNullOrWhiteSpace(status)
            ? "active"
            : status.Trim().ToLower(CultureInfo.InvariantCulture);
}
