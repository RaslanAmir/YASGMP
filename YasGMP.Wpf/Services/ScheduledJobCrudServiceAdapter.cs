using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Connects the WPF Scheduled Job module to the shared database-backed MAUI services exposed through
/// <see cref="YasGMP.Services.DatabaseService"/>.
/// </summary>
/// <remarks>
/// Module view models invoke this adapter which executes the same SQL routines as the MAUI app via
/// <see cref="YasGMP.Services.DatabaseService"/> and surfaces audit metadata that ultimately feeds
/// <see cref="YasGMP.Services.AuditService"/>. Calls should be awaited off the UI thread with UI updates marshalled via
/// <see cref="WpfUiDispatcher"/>. <see cref="CrudSaveResult"/> values carry identifiers, scheduling status, and signature data;
/// callers are responsible for localizing the status/comment text using <see cref="LocalizationServiceExtensions"/> or
/// <see cref="ILocalizationService"/> before displaying it to operators.
/// </remarks>
public sealed class ScheduledJobCrudServiceAdapter : IScheduledJobCrudService
{
    private readonly DatabaseService _database;
    private readonly IPreventiveMaintenanceService? _preventiveMaintenanceService;
    /// <summary>
    /// Initializes a new instance of the ScheduledJobCrudServiceAdapter class.
    /// </summary>

    public ScheduledJobCrudServiceAdapter(DatabaseService database, IPreventiveMaintenanceService? preventiveMaintenanceService = null)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _preventiveMaintenanceService = preventiveMaintenanceService;
    }
    /// <summary>
    /// Executes the try get by id async operation.
    /// </summary>

    public async Task<ScheduledJob?> TryGetByIdAsync(int id)
    {
        var jobs = await _database.GetAllScheduledJobsFullAsync().ConfigureAwait(false);
        return jobs.FirstOrDefault(j => j.Id == id);
    }
    /// <summary>
    /// Executes the create async operation.
    /// </summary>

    public async Task<CrudSaveResult> CreateAsync(ScheduledJob job, ScheduledJobCrudContext context)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        Validate(job);

        job.Status = NormalizeStatus(job.Status);
        job.JobType = NormalizeJobType(job.JobType);

        var signature = ApplyContext(job, context);
        var parameters = CreateCommonParameters(job, context, includeId: false);

        const string sql = @"INSERT INTO scheduled_jobs
(name, job_type, status, next_due, recurrence_pattern, entity_type, entity_id, cron_expression, comment,
 is_critical, needs_acknowledgment, alert_on_failure, retries, max_retries, last_error, last_result, escalation_note,
 created_by, created_by_id, created_at, last_modified_by, last_modified_by_id, last_modified_at,
 device_info, session_id, ip_address, extra_params)
VALUES
(@name, @jobType, @status, @nextDue, @recurrence, @entityType, @entityId, @cronExpression, @comment,
 @isCritical, @needsAck, @alertOnFailure, @retries, @maxRetries, @lastError, @lastResult, @escalationNote,
 @createdBy, @createdById, UTC_TIMESTAMP(), @lastModifiedBy, @lastModifiedById, UTC_TIMESTAMP(),
 @deviceInfo, @sessionId, @ipAddress, @extraParams);
SELECT LAST_INSERT_ID();";

        var result = await _database.ExecuteScalarAsync(sql, parameters).ConfigureAwait(false);
        var id = Convert.ToInt32(result, CultureInfo.InvariantCulture);
        job.Id = id;

        await _database.LogScheduledJobAuditAsync(job, "CREATE", context.Ip, context.DeviceInfo, context.SessionId, null)
            .ConfigureAwait(false);

        return new CrudSaveResult(id, CreateMetadata(context, signature));
    }
    /// <summary>
    /// Executes the update async operation.
    /// </summary>

    public async Task<CrudSaveResult> UpdateAsync(ScheduledJob job, ScheduledJobCrudContext context)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        Validate(job);

        job.Status = NormalizeStatus(job.Status);
        job.JobType = NormalizeJobType(job.JobType);

        var signature = ApplyContext(job, context);
        var parameters = CreateCommonParameters(job, context, includeId: true);

        const string sql = @"UPDATE scheduled_jobs SET
 name=@name,
 job_type=@jobType,
 status=@status,
 next_due=@nextDue,
 recurrence_pattern=@recurrence,
 entity_type=@entityType,
 entity_id=@entityId,
 cron_expression=@cronExpression,
 comment=@comment,
 is_critical=@isCritical,
 needs_acknowledgment=@needsAck,
 alert_on_failure=@alertOnFailure,
 retries=@retries,
 max_retries=@maxRetries,
 last_error=@lastError,
 last_result=@lastResult,
 escalation_note=@escalationNote,
 last_modified_by=@lastModifiedBy,
 last_modified_by_id=@lastModifiedById,
 last_modified_at=UTC_TIMESTAMP(),
 device_info=@deviceInfo,
 session_id=@sessionId,
 ip_address=@ipAddress,
 extra_params=@extraParams
WHERE id=@id";

        await _database.ExecuteNonQueryAsync(sql, parameters).ConfigureAwait(false);
        await _database.LogScheduledJobAuditAsync(job, "UPDATE", context.Ip, context.DeviceInfo, context.SessionId, null)
            .ConfigureAwait(false);

        return new CrudSaveResult(job.Id, CreateMetadata(context, signature));
    }
    /// <summary>
    /// Executes the execute async operation.
    /// </summary>

    public async Task ExecuteAsync(int jobId, ScheduledJobCrudContext context)
    {
        var parameters = new[]
        {
            new MySqlParameter("@id", jobId),
            new MySqlParameter("@userId", context.UserId),
            new MySqlParameter("@userName", context.UserName),
            new MySqlParameter("@deviceInfo", context.DeviceInfo),
            new MySqlParameter("@sessionId", (object?)context.SessionId ?? DBNull.Value),
            new MySqlParameter("@ip", context.Ip)
        };

        const string sql = @"UPDATE scheduled_jobs SET
 status='in_progress',
 last_executed=UTC_TIMESTAMP(),
 last_modified_at=UTC_TIMESTAMP(),
 last_modified_by=@userName,
 last_modified_by_id=@userId
WHERE id=@id";

        await _database.ExecuteNonQueryAsync(sql, parameters).ConfigureAwait(false);
        await _database.LogScheduledJobAuditAsync(new ScheduledJob { Id = jobId }, "EXECUTE", context.Ip, context.DeviceInfo, context.SessionId, $"user={context.UserId}")
            .ConfigureAwait(false);

        await TryAdvancePreventiveMaintenanceAsync(jobId, context).ConfigureAwait(false);
    }
    /// <summary>
    /// Executes the acknowledge async operation.
    /// </summary>

    public async Task AcknowledgeAsync(int jobId, ScheduledJobCrudContext context)
    {
        var parameters = new[]
        {
            new MySqlParameter("@id", jobId),
            new MySqlParameter("@userId", context.UserId),
            new MySqlParameter("@userName", context.UserName)
        };

        const string sql = @"UPDATE scheduled_jobs SET
 status='acknowledged',
 acknowledged_by=@userId,
 acknowledged_at=UTC_TIMESTAMP(),
 last_modified_at=UTC_TIMESTAMP(),
 last_modified_by=@userName,
 last_modified_by_id=@userId
WHERE id=@id";

        await _database.ExecuteNonQueryAsync(sql, parameters).ConfigureAwait(false);
        await _database.LogScheduledJobAuditAsync(new ScheduledJob { Id = jobId }, "ACK", context.Ip, context.DeviceInfo, context.SessionId, $"user={context.UserId}")
            .ConfigureAwait(false);
    }

    private async Task TryAdvancePreventiveMaintenanceAsync(int jobId, ScheduledJobCrudContext context)
    {
        if (_preventiveMaintenanceService is null || context.UserId <= 0)
        {
            return;
        }

        try
        {
            var job = await TryGetByIdAsync(jobId).ConfigureAwait(false);
            if (job?.EntityId is int planId && !string.IsNullOrWhiteSpace(job.EntityType) && IsPreventiveMaintenanceEntity(job.EntityType))
            {
                await _preventiveMaintenanceService.MarkExecutedAsync(planId, context.UserId).ConfigureAwait(false);
            }
        }
        catch
        {
            // Swallow failures so scheduler persistence remains resilient; audit trail already captures the execute attempt.
        }
    }

    private static bool IsPreventiveMaintenanceEntity(string entityType)
        => entityType.Equals("ppm_plan", StringComparison.OrdinalIgnoreCase)
            || entityType.Equals("preventive_maintenance_plan", StringComparison.OrdinalIgnoreCase)
            || entityType.Equals("preventive_maintenance", StringComparison.OrdinalIgnoreCase);
    /// <summary>
    /// Executes the validate operation.
    /// </summary>

    public void Validate(ScheduledJob job)
    {
        if (job is null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        if (string.IsNullOrWhiteSpace(job.Name))
        {
            throw new InvalidOperationException("Scheduled job name is required.");
        }

        if (string.IsNullOrWhiteSpace(job.JobType))
        {
            throw new InvalidOperationException("Scheduled job type is required.");
        }

        if (string.IsNullOrWhiteSpace(job.Status))
        {
            throw new InvalidOperationException("Scheduled job status is required.");
        }

        if (string.IsNullOrWhiteSpace(job.RecurrencePattern))
        {
            throw new InvalidOperationException("Recurrence pattern is required.");
        }
    }
    /// <summary>
    /// Executes the normalize status operation.
    /// </summary>

    public string NormalizeStatus(string? status)
        => string.IsNullOrWhiteSpace(status)
            ? "scheduled"
            : status.Trim().ToLowerInvariant();

    private static string NormalizeJobType(string? jobType)
        => string.IsNullOrWhiteSpace(jobType)
            ? "custom"
            : jobType.Trim().ToLowerInvariant();

    private static IEnumerable<MySqlParameter> CreateCommonParameters(ScheduledJob job, ScheduledJobCrudContext context, bool includeId)
    {
        var createdById = job.CreatedById ?? context.UserId;
        var createdBy = string.IsNullOrWhiteSpace(job.CreatedBy) ? context.UserName : job.CreatedBy;

        var parameters = new List<MySqlParameter>
        {
            new("@name", job.Name),
            new("@jobType", job.JobType),
            new("@status", job.Status),
            new("@nextDue", job.NextDue == default ? DateTime.UtcNow : job.NextDue),
            new("@recurrence", string.IsNullOrWhiteSpace(job.RecurrencePattern) ? (object)string.Empty : job.RecurrencePattern),
            new("@entityType", string.IsNullOrWhiteSpace(job.EntityType) ? (object)DBNull.Value : job.EntityType),
            new("@entityId", job.EntityId is null ? DBNull.Value : job.EntityId),
            new("@cronExpression", string.IsNullOrWhiteSpace(job.CronExpression) ? (object)DBNull.Value : job.CronExpression),
            new("@comment", string.IsNullOrWhiteSpace(job.Comment) ? (object)string.Empty : job.Comment),
            new("@isCritical", job.IsCritical),
            new("@needsAck", job.NeedsAcknowledgment),
            new("@alertOnFailure", job.AlertOnFailure),
            new("@retries", job.Retries),
            new("@maxRetries", job.MaxRetries),
            new("@lastError", string.IsNullOrWhiteSpace(job.LastError) ? (object)DBNull.Value : job.LastError),
            new("@lastResult", string.IsNullOrWhiteSpace(job.LastResult) ? (object)DBNull.Value : job.LastResult),
            new("@escalationNote", string.IsNullOrWhiteSpace(job.EscalationNote) ? (object)DBNull.Value : job.EscalationNote),
            new("@createdBy", createdBy),
            new("@createdById", createdById),
            new("@lastModifiedBy", context.UserName),
            new("@lastModifiedById", context.UserId),
            new("@deviceInfo", string.IsNullOrWhiteSpace(job.DeviceInfo) ? context.DeviceInfo : job.DeviceInfo),
            new("@sessionId", string.IsNullOrWhiteSpace(job.SessionId) ? (object?)context.SessionId ?? DBNull.Value : job.SessionId),
            new("@ipAddress", string.IsNullOrWhiteSpace(job.IpAddress) ? context.Ip : job.IpAddress),
            new("@extraParams", string.IsNullOrWhiteSpace(job.ExtraParams) ? (object)DBNull.Value : job.ExtraParams)
        };

        if (includeId)
        {
            parameters.Add(new MySqlParameter("@id", job.Id));
        }

        return parameters;
    }

    private static string ApplyContext(ScheduledJob job, ScheduledJobCrudContext context)
    {
        var signature = context.SignatureHash ?? job.DigitalSignature ?? string.Empty;
        job.DigitalSignature = signature;
        job.LastModifiedById = context.UserId;
        job.LastModified = DateTime.UtcNow;
        job.LastModifiedAt = DateTime.UtcNow;
        job.DeviceInfo = string.IsNullOrWhiteSpace(job.DeviceInfo) ? context.DeviceInfo : job.DeviceInfo;
        job.SessionId = string.IsNullOrWhiteSpace(job.SessionId) ? context.SessionId ?? string.Empty : job.SessionId;
        job.IpAddress = string.IsNullOrWhiteSpace(job.IpAddress) ? context.Ip : job.IpAddress;
        return signature;
    }

    private static SignatureMetadataDto CreateMetadata(ScheduledJobCrudContext context, string signature)
        => new()
        {
            Id = context.SignatureId,
            Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
            Method = context.SignatureMethod,
            Status = context.SignatureStatus,
            Note = context.SignatureNote,
            Session = context.SessionId,
            Device = context.DeviceInfo,
            IpAddress = context.Ip
        };
}
