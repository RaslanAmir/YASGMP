using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Helpers;
using YasGMP.Models;
using YasGMP.Models.Enums;
using YasGMP.Services;
using YasGMP.Services.Interfaces;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Implements <see cref="IPpmAuditService"/> on top of the shared <see cref="AuditService"/> and
/// <see cref="DatabaseService"/> so WPF modules read and write PPM audit records through the
/// same infrastructure that powers the MAUI client.
/// </summary>
public sealed class PpmAuditServiceAdapter : IPpmAuditService
{
    private const string EntityName = "preventive_maintenance_plans";

    private readonly DatabaseService _database;
    private readonly AuditService _audit;

    public PpmAuditServiceAdapter(DatabaseService database, AuditService audit)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    /// <inheritdoc />
    public async Task CreateAsync(PpmAudit audit, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(audit);
        await _audit.LogEntityAuditAsync(EntityName, audit.PpmPlanId, FormatAction(audit.Action), FormatDetails(audit))
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> CreateBatchAsync(IEnumerable<PpmAudit> audits, CancellationToken token = default)
    {
        if (audits is null)
        {
            return 0;
        }

        int count = 0;
        foreach (var audit in audits)
        {
            await CreateAsync(audit, token).ConfigureAwait(false);
            count++;
        }

        return count;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PpmAudit>> GetByPlanIdAsync(int ppmPlanId, CancellationToken token = default)
        => await QueryAsync("AND entity_id=@pid", new[] { new MySqlParameter("@pid", ppmPlanId) }, token).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PpmAudit>> GetByUserIdAsync(int userId, CancellationToken token = default)
        => await QueryAsync("AND user_id=@uid", new[] { new MySqlParameter("@uid", userId) }, token).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PpmAudit>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken token = default)
        => await QueryAsync("AND `timestamp` BETWEEN @from AND @to",
            new[]
            {
                new MySqlParameter("@from", from),
                new MySqlParameter("@to", to)
            }, token).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<PpmAudit> GetByIdAsync(int auditId, CancellationToken token = default)
    {
        var items = await QueryAsync("AND id=@id", new[] { new MySqlParameter("@id", auditId) }, token).ConfigureAwait(false);
        return items.FirstOrDefault() ?? new PpmAudit { Id = auditId };
    }

    /// <inheritdoc />
    public Task UpdateAsync(PpmAudit audit, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(audit);
        // Immutable audit trail: record the correction as a new entry instead of mutating history.
        var details = $"Correction for audit #{audit.Id}: {FormatDetails(audit)}";
        return _audit.LogEntityAuditAsync(EntityName, audit.PpmPlanId, "MANUAL_UPDATE", details);
    }

    /// <inheritdoc />
    public Task DeleteAsync(int id, CancellationToken token = default)
        => _audit.LogEntityAuditAsync(EntityName, id, "MANUAL_DELETE", $"Requested deletion marker for audit #{id}");

    /// <inheritdoc />
    public async Task<string> ExportAsync(IEnumerable<PpmAudit> audits, string format = "pdf", CancellationToken token = default)
    {
        var list = (audits ?? Array.Empty<PpmAudit>()).ToList();
        string path = CsvExportHelper.WriteCsv(list, "ppm_audit", new (string Header, Func<PpmAudit, object?> Selector)[]
        {
            ("Id", a => a.Id),
            ("PlanId", a => a.PpmPlanId),
            ("UserId", a => a.UserId),
            ("Action", a => a.Action.ToString()),
            ("ChangedAt", a => a.ChangedAt),
            ("Details", a => a.Details ?? string.Empty),
            ("SourceIp", a => a.SourceIp ?? string.Empty),
            ("Device", a => a.DeviceInfo ?? string.Empty),
            ("Session", a => a.SessionId ?? string.Empty)
        });

        await _audit.LogSystemEventAsync("PPM_AUDIT_EXPORT",
            $"count={list.Count}; fmt={format}; file={path}", "ppm_audit", null).ConfigureAwait(false);

        return path;
    }

    private async Task<IReadOnlyList<PpmAudit>> QueryAsync(string filter, IEnumerable<MySqlParameter> parameters, CancellationToken token)
    {
        var sql = $@"SELECT id, `timestamp`, user_id, entity_id, `action`, `details`, source_ip, device_info, session_id
FROM entity_audit_log
WHERE entity=@entity {filter}
ORDER BY `timestamp` DESC";

        var allParameters = new List<MySqlParameter> { new("@entity", EntityName) };
        if (parameters != null)
        {
            allParameters.AddRange(parameters);
        }

        var table = await _database.ExecuteSelectAsync(sql, allParameters, token).ConfigureAwait(false);
        var result = new List<PpmAudit>(table.Rows.Count);
        foreach (DataRow row in table.Rows)
        {
            result.Add(Map(row));
        }
        return result;
    }

    private static PpmAudit Map(DataRow row)
    {
        var actionString = row.Table.Columns.Contains("action") ? row["action"]?.ToString() ?? string.Empty : string.Empty;
        var timestamp = row.Table.Columns.Contains("timestamp") && row["timestamp"] != DBNull.Value
            ? Convert.ToDateTime(row["timestamp"], CultureInfo.InvariantCulture)
            : DateTime.UtcNow;

        return new PpmAudit
        {
            Id = row.Table.Columns.Contains("id") && row["id"] != DBNull.Value ? Convert.ToInt32(row["id"], CultureInfo.InvariantCulture) : 0,
            PpmPlanId = row.Table.Columns.Contains("entity_id") && row["entity_id"] != DBNull.Value ? Convert.ToInt32(row["entity_id"], CultureInfo.InvariantCulture) : 0,
            UserId = row.Table.Columns.Contains("user_id") && row["user_id"] != DBNull.Value ? Convert.ToInt32(row["user_id"], CultureInfo.InvariantCulture) : 0,
            Action = ParseAction(actionString),
            Details = row.Table.Columns.Contains("details") ? row["details"]?.ToString() : string.Empty,
            ChangedAt = timestamp,
            SourceIp = row.Table.Columns.Contains("source_ip") ? row["source_ip"]?.ToString() : null,
            DeviceInfo = row.Table.Columns.Contains("device_info") ? row["device_info"]?.ToString() : null,
            SessionId = row.Table.Columns.Contains("session_id") ? row["session_id"]?.ToString() : null,
            DigitalSignature = null,
            AiAnomalyScore = null,
            RegulatoryStatus = null,
            Validated = null,
            RelatedFile = null,
            RelatedPhoto = null,
            IotEventId = null,
            ApprovalStatus = null,
            ApprovalTime = null,
            ApprovedBy = null,
            Deleted = null,
            DeletedAt = null,
            DeletedBy = null,
            RestorationReference = null,
            ExportStatus = null,
            ExportTime = null,
            ExportedBy = null,
            Comment = null
        };
    }

    private static string FormatAction(PpmActionType action)
        => action.ToString().ToUpperInvariant();

    private static string FormatDetails(PpmAudit audit)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(audit.Details))
        {
            parts.Add(audit.Details!);
        }

        if (!string.IsNullOrWhiteSpace(audit.DigitalSignature))
        {
            parts.Add($"sig={audit.DigitalSignature}");
        }

        if (!string.IsNullOrWhiteSpace(audit.DeviceInfo))
        {
            parts.Add($"device={audit.DeviceInfo}");
        }

        if (!string.IsNullOrWhiteSpace(audit.SourceIp))
        {
            parts.Add($"ip={audit.SourceIp}");
        }

        if (!string.IsNullOrWhiteSpace(audit.SessionId))
        {
            parts.Add($"session={audit.SessionId}");
        }

        if (audit.AiAnomalyScore.HasValue)
        {
            parts.Add($"ai={audit.AiAnomalyScore.Value:F2}");
        }

        if (!string.IsNullOrWhiteSpace(audit.RegulatoryStatus))
        {
            parts.Add($"reg={audit.RegulatoryStatus}");
        }

        return string.Join(" | ", parts);
    }

    private static PpmActionType ParseAction(string action)
    {
        if (Enum.TryParse<PpmActionType>(action, true, out var parsed))
        {
            return parsed;
        }

        return PpmActionType.UPDATE;
    }
}
