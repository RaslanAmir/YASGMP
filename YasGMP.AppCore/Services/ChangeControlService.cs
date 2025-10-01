using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.Enums;

namespace YasGMP.Services;

/// <summary>
/// Provides CRUD operations and validation for change control records while
/// emitting audit telemetry via <see cref="AuditService"/>.
/// </summary>
public class ChangeControlService
{
    private readonly DatabaseService _database;
    private readonly AuditService _audit;

    public ChangeControlService(DatabaseService databaseService, AuditService auditService)
    {
        _database = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _audit = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public Task<List<ChangeControl>> GetAllAsync(CancellationToken token = default)
        => _database.GetAllChangeControlsAsync(token);

    public Task<ChangeControl?> TryGetByIdAsync(int id, CancellationToken token = default)
        => _database.GetChangeControlByIdAsync(id, token);

    public Task<int> CreateAsync(ChangeControl changeControl, int userId, CancellationToken token = default)
        => CreateAsync(changeControl, userId, ip: "system", deviceInfo: Environment.MachineName, sessionId: null, token);

    public async Task<int> CreateAsync(
        ChangeControl changeControl,
        int userId,
        string ip,
        string deviceInfo,
        string? sessionId,
        CancellationToken token = default)
    {
        if (changeControl is null)
        {
            throw new ArgumentNullException(nameof(changeControl));
        }

        Validate(changeControl);
        Normalize(changeControl);

        changeControl.RequestedById ??= userId;
        changeControl.DateRequested ??= DateTime.UtcNow;
        changeControl.LastModifiedById = userId;
        changeControl.LastModified = DateTime.UtcNow;
        changeControl.SourceIp = ip;
        changeControl.DeviceInfo = deviceInfo;
        changeControl.SessionId = sessionId;

        var id = await _database.InsertChangeControlAsync(
            changeControl,
            actorUserId: userId,
            ip: ip,
            deviceInfo: deviceInfo,
            sessionId: sessionId,
            token: token).ConfigureAwait(false);

        await _audit.LogEntityAuditAsync("change_controls", id, "CREATE", changeControl.Title ?? string.Empty)
            .ConfigureAwait(false);
        return id;
    }

    public Task UpdateAsync(ChangeControl changeControl, int userId, CancellationToken token = default)
        => UpdateAsync(changeControl, userId, ip: "system", deviceInfo: Environment.MachineName, sessionId: null, token);

    public async Task UpdateAsync(
        ChangeControl changeControl,
        int userId,
        string ip,
        string deviceInfo,
        string? sessionId,
        CancellationToken token = default)
    {
        if (changeControl is null)
        {
            throw new ArgumentNullException(nameof(changeControl));
        }

        Validate(changeControl);
        Normalize(changeControl);

        changeControl.LastModifiedById = userId;
        changeControl.LastModified = DateTime.UtcNow;
        changeControl.SourceIp = ip;
        changeControl.DeviceInfo = deviceInfo;
        changeControl.SessionId = sessionId;

        await _database.UpdateChangeControlAsync(
            changeControl,
            actorUserId: userId,
            ip: ip,
            deviceInfo: deviceInfo,
            sessionId: sessionId,
            token: token).ConfigureAwait(false);

        await _audit.LogEntityAuditAsync("change_controls", changeControl.Id, "UPDATE", changeControl.Title ?? string.Empty)
            .ConfigureAwait(false);
    }

    public void Validate(ChangeControl changeControl)
    {
        if (changeControl is null)
        {
            throw new ArgumentNullException(nameof(changeControl));
        }

        if (string.IsNullOrWhiteSpace(changeControl.Title))
        {
            throw new InvalidOperationException("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(changeControl.Code))
        {
            throw new InvalidOperationException("Code is required.");
        }

        if (changeControl.Status == ChangeControlStatus.Cancelled)
        {
            if (string.IsNullOrWhiteSpace(changeControl.Description))
            {
                throw new InvalidOperationException("Cancellation requires a description explaining the reason.");
            }
        }
    }

    public string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return ChangeControlStatus.Draft.ToString();
        }

        var match = Enum.GetValues(typeof(ChangeControlStatus))
            .Cast<ChangeControlStatus>()
            .FirstOrDefault(s => string.Equals(s.ToString(), status, StringComparison.OrdinalIgnoreCase));
        return match.ToString();
    }

    private void Normalize(ChangeControl changeControl)
    {
        changeControl.Code = (changeControl.Code ?? string.Empty).Trim();
        changeControl.Title = (changeControl.Title ?? string.Empty).Trim();
        changeControl.Description = changeControl.Description?.Trim();
        changeControl.StatusRaw = NormalizeStatus(changeControl.StatusRaw);
    }
}
