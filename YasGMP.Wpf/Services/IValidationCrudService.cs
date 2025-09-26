using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter abstraction over <see cref="YasGMP.Services.ValidationService"/> so the WPF shell can
/// coordinate validation CRUD flows without binding directly to infrastructure types during tests.
/// </summary>
public interface IValidationCrudService
{
    Task<IReadOnlyList<Validation>> GetAllAsync();

    Task<Validation?> TryGetByIdAsync(int id);

    Task<int> CreateAsync(Validation validation, ValidationCrudContext context);

    Task UpdateAsync(Validation validation, ValidationCrudContext context);

    void Validate(Validation validation);
}

/// <summary>
/// Context metadata captured during validation persistence operations.
/// </summary>
/// <param name="UserId">Authenticated user identifier.</param>
/// <param name="Ip">Source IP captured from the current auth context.</param>
/// <param name="DeviceInfo">Machine fingerprint/hostname.</param>
/// <param name="SessionId">Logical application session identifier.</param>
public readonly record struct ValidationCrudContext(int UserId, string Ip, string DeviceInfo, string? SessionId)
{
    public static ValidationCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
        => new(
            userId <= 0 ? 1 : userId,
            string.IsNullOrWhiteSpace(ip) ? "unknown" : ip,
            string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo,
            string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId);
}
