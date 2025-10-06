// ============================================================================
// File: Services/DatabaseService.ChangeControls.Crud.cs
// Purpose: CRUD helpers for change control records consumed by ChangeControlService
// ============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;

namespace YasGMP.Services;

public static class DatabaseServiceChangeControlsCrudExtensions
{
    public static async Task<List<ChangeControl>> GetAllChangeControlsAsync(
        this DatabaseService db,
        CancellationToken token = default)
    {
        const string sql = @"SELECT
    id, description, title, date_requested, code, status,
    requested_by_id, assigned_to_id, date_assigned, last_modified,
    last_modified_by_id, created_at, updated_at, digital_signature,
    source_ip, session_id, device_info
FROM change_controls
ORDER BY id DESC";

        var table = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);
        var list = new List<ChangeControl>(table.Rows.Count);
        foreach (DataRow row in table.Rows)
        {
            list.Add(Map(row));
        }

        return list;
    }

    public static async Task<ChangeControl?> GetChangeControlByIdAsync(
        this DatabaseService db,
        int id,
        CancellationToken token = default)
    {
        const string sql = @"SELECT
    id, description, title, date_requested, code, status,
    requested_by_id, assigned_to_id, date_assigned, last_modified,
    last_modified_by_id, created_at, updated_at, digital_signature,
    source_ip, session_id, device_info
FROM change_controls
WHERE id = @id
LIMIT 1";

        var table = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", id) }, token)
            .ConfigureAwait(false);

        return table.Rows.Count == 0 ? null : Map(table.Rows[0]);
    }

    public static async Task<int> InsertChangeControlAsync(
        this DatabaseService db,
        ChangeControl changeControl,
        int actorUserId,
        string ip,
        string deviceInfo,
        string? sessionId,
        CancellationToken token = default)
    {
        if (changeControl is null)
        {
            throw new ArgumentNullException(nameof(changeControl));
        }

        const string sql = @"INSERT INTO change_controls
    (description, title, date_requested, code, status, requested_by_id, assigned_to_id,
     date_assigned, last_modified, last_modified_by_id, created_at, updated_at,
     digital_signature, source_ip, session_id, device_info)
VALUES
    (@description, @title, @date_requested, @code, @status, @requested_by_id, @assigned_to_id,
     @date_assigned, @last_modified, @last_modified_by_id, @created_at, @updated_at,
     @digital_signature, @source_ip, @session_id, @device_info);";

        var parameters = BuildParameterList(changeControl);
        await db.ExecuteNonQueryAsync(sql, parameters, token).ConfigureAwait(false);

        var newId = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID();", null, token)
            .ConfigureAwait(false);
        changeControl.Id = Convert.ToInt32(newId);

        await db.LogSystemEventAsync(
            actorUserId,
            "CHANGE_CONTROL_CREATE",
            "change_controls",
            "ChangeControl",
            changeControl.Id,
            changeControl.Title,
            ip,
            "audit",
            deviceInfo,
            sessionId,
            token: token).ConfigureAwait(false);

        return changeControl.Id;
    }

    public static async Task UpdateChangeControlAsync(
        this DatabaseService db,
        ChangeControl changeControl,
        int actorUserId,
        string ip,
        string deviceInfo,
        string? sessionId,
        CancellationToken token = default)
    {
        if (changeControl is null)
        {
            throw new ArgumentNullException(nameof(changeControl));
        }

        const string sql = @"UPDATE change_controls SET
    description = @description,
    title = @title,
    date_requested = @date_requested,
    code = @code,
    status = @status,
    requested_by_id = @requested_by_id,
    assigned_to_id = @assigned_to_id,
    date_assigned = @date_assigned,
    last_modified = @last_modified,
    last_modified_by_id = @last_modified_by_id,
    updated_at = @updated_at,
    digital_signature = @digital_signature,
    source_ip = @source_ip,
    session_id = @session_id,
    device_info = @device_info
WHERE id = @id";

        var parameters = BuildParameterList(changeControl);
        parameters.Add(new MySqlParameter("@id", changeControl.Id));
        await db.ExecuteNonQueryAsync(sql, parameters, token).ConfigureAwait(false);

        await db.LogSystemEventAsync(
            actorUserId,
            "CHANGE_CONTROL_UPDATE",
            "change_controls",
            "ChangeControl",
            changeControl.Id,
            changeControl.Title,
            ip,
            "audit",
            deviceInfo,
            sessionId,
            token: token).ConfigureAwait(false);
    }

    private static ChangeControl Map(DataRow row)
    {
        var changeControl = new ChangeControl
        {
            Id = row.Field<int>("id"),
            Description = row.Field<string?>("description"),
            Title = row.Field<string?>("title"),
            DateRequestedRaw = row.Field<string?>("date_requested"),
            Code = row.Field<string?>("code"),
            StatusRaw = row.Field<string?>("status"),
            RequestedById = row.Field<int?>("requested_by_id"),
            AssignedToId = row.Field<int?>("assigned_to_id"),
            DateAssigned = row.Field<DateTime?>("date_assigned"),
            LastModified = row.Field<DateTime?>("last_modified"),
            LastModifiedById = row.Field<int?>("last_modified_by_id"),
            CreatedAt = row.Field<DateTime?>("created_at"),
            UpdatedAt = row.Field<DateTime?>("updated_at"),
            DigitalSignature = row.Field<string?>("digital_signature"),
            SourceIp = row.Field<string?>("source_ip"),
            SessionId = row.Field<string?>("session_id"),
            DeviceInfo = row.Field<string?>("device_info")
        };

        return changeControl;
    }

    private static List<MySqlParameter> BuildParameterList(ChangeControl changeControl)
    {
        var now = DateTime.UtcNow;
        changeControl.LastModified ??= now;
        changeControl.UpdatedAt ??= now;
        changeControl.CreatedAt ??= now;

        var dateRequested = changeControl.DateRequestedRaw;
        if (string.IsNullOrWhiteSpace(dateRequested) && changeControl.DateRequested.HasValue)
        {
            dateRequested = changeControl.DateRequested!.Value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        return new List<MySqlParameter>
        {
            new("@description", changeControl.Description ?? string.Empty),
            new("@title", changeControl.Title ?? string.Empty),
            new("@date_requested", dateRequested is null ? (object)DBNull.Value : dateRequested),
            new("@code", changeControl.Code ?? string.Empty),
            new("@status", changeControl.StatusRaw ?? string.Empty),
            new("@requested_by_id", changeControl.RequestedById ?? (object)DBNull.Value),
            new("@assigned_to_id", changeControl.AssignedToId ?? (object)DBNull.Value),
            new("@date_assigned", changeControl.DateAssigned ?? (object)DBNull.Value),
            new("@last_modified", changeControl.LastModified ?? (object)DBNull.Value),
            new("@last_modified_by_id", changeControl.LastModifiedById ?? (object)DBNull.Value),
            new("@created_at", changeControl.CreatedAt ?? (object)DBNull.Value),
            new("@updated_at", changeControl.UpdatedAt ?? (object)DBNull.Value),
            new("@digital_signature", changeControl.DigitalSignature ?? (object)DBNull.Value),
            new("@source_ip", changeControl.SourceIp ?? (object)DBNull.Value),
            new("@session_id", changeControl.SessionId ?? (object)DBNull.Value),
            new("@device_info", changeControl.DeviceInfo ?? (object)DBNull.Value)
        };
    }
}

