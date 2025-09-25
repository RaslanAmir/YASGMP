using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter-friendly abstraction over <see cref="YasGMP.Services.CalibrationService"/> so the
/// WPF shell can execute CRUD operations without pulling in the full database runtime during tests.
/// </summary>
public interface ICalibrationCrudService
{
    Task<IReadOnlyList<Calibration>> GetAllAsync();

    Task<Calibration?> TryGetByIdAsync(int id);

    Task<int> CreateAsync(Calibration calibration, CalibrationCrudContext context);

    Task UpdateAsync(Calibration calibration, CalibrationCrudContext context);

    void Validate(Calibration calibration);
}

/// <summary>
/// Context metadata captured when persisting calibrations to support downstream audit logging.
/// </summary>
/// <param name="UserId">Authenticated user identifier.</param>
/// <param name="Ip">Source IP captured from the current auth context.</param>
/// <param name="DeviceInfo">Machine fingerprint/hostname.</param>
/// <param name="SessionId">Logical application session identifier.</param>
public readonly record struct CalibrationCrudContext(int UserId, string Ip, string DeviceInfo, string? SessionId)
{
    public static CalibrationCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
        => new(
            userId <= 0 ? 1 : userId,
            string.IsNullOrWhiteSpace(ip) ? "unknown" : ip,
            string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo,
            string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId);
}
