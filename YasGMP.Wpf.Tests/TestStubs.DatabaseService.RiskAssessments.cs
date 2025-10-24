using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services;

public partial class DatabaseService
{
    public List<RiskAssessment> RiskAssessments { get; } = new()
    {
        new RiskAssessment
        {
            Id = 101,
            Code = "RA-2026-001",
            Title = "Supplier onboarding risk",
            Category = "supplier",
            Status = "pending_approval",
            RiskLevel = "High",
            RiskScore = 72,
            Owner = new User { Id = 7, FullName = "QA Lead", Username = "qa.lead" },
            ReviewDate = DateTime.Today.AddMonths(3),
            AssessedAt = DateTime.UtcNow.AddDays(-10),
            Mitigation = "Vendor audit scheduled"
        },
        new RiskAssessment
        {
            Id = 102,
            Code = "RA-2026-002",
            Title = "Equipment maintenance backlog",
            Category = "equipment",
            Status = "effectiveness_check",
            RiskLevel = "Medium",
            RiskScore = 36,
            Owner = new User { Id = 12, FullName = "Maintenance", Username = "maintenance" },
            ReviewDate = DateTime.Today.AddMonths(1),
            AssessedAt = DateTime.UtcNow.AddDays(-5),
            Mitigation = "Preventive maintenance plan in place"
        },
        new RiskAssessment
        {
            Id = 103,
            Code = "RA-2026-003",
            Title = "Process deviation trend",
            Category = "process",
            Status = "in_progress",
            RiskLevel = "Critical",
            RiskScore = 90,
            Owner = new User { Id = 18, FullName = "Quality", Username = "quality" },
            ReviewDate = DateTime.Today.AddMonths(2),
            AssessedAt = DateTime.UtcNow.AddDays(-20),
            Mitigation = "Root cause analysis running"
        }
    };

    public Func<CancellationToken, IEnumerable<RiskAssessment>>? RiskAssessmentProvider { get; set; }

    public Exception? RiskAssessmentsException { get; set; }

    public Exception? RiskAssessmentsWorkflowException { get; set; }

    public List<(RiskAssessment Risk, string Ip, string DeviceInfo, string? SessionId)> RiskAssessmentInitiateCalls { get; } = new();

    public List<(RiskAssessment Risk, int ActorUserId, string Ip, string DeviceInfo, string? SessionId)> RiskAssessmentUpdateCalls { get; } = new();

    public List<(int RiskId, int ActorUserId, string Ip, string DeviceInfo, string? SessionId)> RiskAssessmentApproveCalls { get; } = new();

    public List<(int RiskId, int ActorUserId, string Ip, string DeviceInfo, string? SessionId, string? Note)> RiskAssessmentCloseCalls { get; } = new();

    public List<(List<RiskAssessment> Items, string Ip, string DeviceInfo, string? SessionId)> RiskAssessmentExportCalls { get; } = new();

    public List<(RiskAssessment? Risk, string Action, string Ip, string DeviceInfo, string? SessionId, string? Details)> RiskAssessmentAuditEntries { get; } = new();

    public Task<List<RiskAssessment>> GetAllRiskAssessmentsFullAsync(CancellationToken token = default)
    {
        if (RiskAssessmentsException is not null)
        {
            throw RiskAssessmentsException;
        }

        var source = RiskAssessmentProvider?.Invoke(token) ?? RiskAssessments;
        return Task.FromResult(source
            .OrderBy(risk => risk.Id)
            .Select(CloneRiskAssessment)
            .ToList());
    }

    public Task InitiateRiskAssessmentAsync(RiskAssessment risk, CancellationToken token = default)
    {
        if (risk is null)
        {
            throw new ArgumentNullException(nameof(risk));
        }

        if (RiskAssessmentsWorkflowException is not null)
        {
            throw RiskAssessmentsWorkflowException;
        }

        var stored = CloneRiskAssessment(risk);
        if (stored.Id == 0)
        {
            stored.Id = RiskAssessments.Count == 0 ? 1 : RiskAssessments.Max(item => item.Id) + 1;
        }

        UpsertRiskAssessment(stored);
        RiskAssessmentInitiateCalls.Add((CloneRiskAssessment(stored), risk.IpAddress ?? string.Empty, risk.DeviceInfo ?? string.Empty, risk.SessionId));
        return Task.CompletedTask;
    }

    public Task UpdateRiskAssessmentAsync(
        RiskAssessment risk,
        int actorUserId,
        string ip,
        string deviceInfo,
        string? sessionId,
        CancellationToken token = default)
    {
        if (risk is null)
        {
            throw new ArgumentNullException(nameof(risk));
        }

        if (RiskAssessmentsWorkflowException is not null)
        {
            throw RiskAssessmentsWorkflowException;
        }

        var stored = CloneRiskAssessment(risk);
        UpsertRiskAssessment(stored);
        RiskAssessmentUpdateCalls.Add((CloneRiskAssessment(stored), actorUserId, ip, deviceInfo, sessionId));
        return Task.CompletedTask;
    }

    public Task ApproveRiskAssessmentAsync(
        int riskId,
        int actorUserId,
        string ip,
        string deviceInfo,
        string? sessionId,
        CancellationToken token = default)
    {
        if (RiskAssessmentsWorkflowException is not null)
        {
            throw RiskAssessmentsWorkflowException;
        }

        RiskAssessmentApproveCalls.Add((riskId, actorUserId, ip, deviceInfo, sessionId));
        var existing = RiskAssessments.FirstOrDefault(item => item.Id == riskId);
        if (existing is not null)
        {
            existing.Status = "effectiveness_check";
            existing.ApprovedById = actorUserId;
            existing.ApprovedAt ??= DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task CloseRiskAssessmentAsync(
        int riskId,
        int actorUserId,
        string ip,
        string deviceInfo,
        string? sessionId,
        string? note,
        CancellationToken token = default)
    {
        if (RiskAssessmentsWorkflowException is not null)
        {
            throw RiskAssessmentsWorkflowException;
        }

        RiskAssessmentCloseCalls.Add((riskId, actorUserId, ip, deviceInfo, sessionId, note));
        var existing = RiskAssessments.FirstOrDefault(item => item.Id == riskId);
        if (existing is not null)
        {
            existing.Status = "closed";
            existing.Note = note;
        }

        return Task.CompletedTask;
    }

    public Task CloseRiskAssessmentAsync(
        int riskId,
        int actorUserId,
        string ip,
        string deviceInfo,
        string? sessionId,
        CancellationToken token = default)
        => CloseRiskAssessmentAsync(riskId, actorUserId, ip, deviceInfo, sessionId, null, token);

    public Task ExportRiskAssessmentsAsync(
        List<RiskAssessment>? items,
        string ip,
        string deviceInfo,
        string? sessionId,
        CancellationToken token = default)
    {
        if (RiskAssessmentsWorkflowException is not null)
        {
            throw RiskAssessmentsWorkflowException;
        }

        var snapshot = items?.Select(CloneRiskAssessment).ToList() ?? new List<RiskAssessment>();
        RiskAssessmentExportCalls.Add((snapshot, ip, deviceInfo, sessionId));
        return Task.CompletedTask;
    }

    public Task LogRiskAssessmentAuditAsync(
        RiskAssessment? risk,
        string action,
        string ip,
        string deviceInfo,
        string? sessionId,
        string? details,
        CancellationToken token = default)
    {
        RiskAssessmentAuditEntries.Add((risk is null ? null : CloneRiskAssessment(risk), action, ip, deviceInfo, sessionId, details));
        return Task.CompletedTask;
    }

    private void UpsertRiskAssessment(RiskAssessment source)
    {
        var existing = RiskAssessments.FirstOrDefault(item => item.Id == source.Id);
        if (existing is null)
        {
            RiskAssessments.Add(CloneRiskAssessment(source));
            return;
        }

        CopyRiskAssessment(source, existing);
    }

    private static RiskAssessment CloneRiskAssessment(RiskAssessment source)
        => new()
        {
            Id = source.Id,
            Code = source.Code,
            Title = source.Title,
            Description = source.Description,
            Category = source.Category,
            Area = source.Area,
            Status = source.Status,
            AssessedBy = source.AssessedBy,
            AssessedAt = source.AssessedAt,
            Severity = source.Severity,
            Probability = source.Probability,
            Detection = source.Detection,
            RiskScore = source.RiskScore,
            RiskLevel = source.RiskLevel,
            Mitigation = source.Mitigation,
            ActionPlan = source.ActionPlan,
            OwnerId = source.OwnerId,
            Owner = CloneUser(source.Owner),
            ApprovedById = source.ApprovedById,
            ApprovedBy = CloneUser(source.ApprovedBy),
            ApprovedAt = source.ApprovedAt,
            ReviewDate = source.ReviewDate,
            DigitalSignature = source.DigitalSignature,
            Note = source.Note,
            DeviceInfo = source.DeviceInfo,
            SessionId = source.SessionId,
            IpAddress = source.IpAddress,
            Documents = source.Documents?.Select(CloneDocument).ToList() ?? new List<DocumentControl>(),
            Attachments = source.Attachments?.ToList() ?? new List<string>(),
            WorkflowHistory = source.WorkflowHistory?.Select(CloneWorkflowEntry).ToList() ?? new List<RiskWorkflowEntry>(),
            AuditLogs = source.AuditLogs?.Select(CloneAuditLog).ToList() ?? new List<RiskAssessmentAuditLog>()
        };

    private static void CopyRiskAssessment(RiskAssessment source, RiskAssessment destination)
    {
        destination.Code = source.Code;
        destination.Title = source.Title;
        destination.Description = source.Description;
        destination.Category = source.Category;
        destination.Area = source.Area;
        destination.Status = source.Status;
        destination.AssessedBy = source.AssessedBy;
        destination.AssessedAt = source.AssessedAt;
        destination.Severity = source.Severity;
        destination.Probability = source.Probability;
        destination.Detection = source.Detection;
        destination.RiskScore = source.RiskScore;
        destination.RiskLevel = source.RiskLevel;
        destination.Mitigation = source.Mitigation;
        destination.ActionPlan = source.ActionPlan;
        destination.OwnerId = source.OwnerId;
        destination.Owner = CloneUser(source.Owner);
        destination.ApprovedById = source.ApprovedById;
        destination.ApprovedBy = CloneUser(source.ApprovedBy);
        destination.ApprovedAt = source.ApprovedAt;
        destination.ReviewDate = source.ReviewDate;
        destination.DigitalSignature = source.DigitalSignature;
        destination.Note = source.Note;
        destination.DeviceInfo = source.DeviceInfo;
        destination.SessionId = source.SessionId;
        destination.IpAddress = source.IpAddress;
        destination.Attachments = source.Attachments?.ToList() ?? new List<string>();
        destination.Documents = source.Documents?.Select(CloneDocument).ToList() ?? new List<DocumentControl>();
        destination.WorkflowHistory = source.WorkflowHistory?.Select(CloneWorkflowEntry).ToList() ?? new List<RiskWorkflowEntry>();
        destination.AuditLogs = source.AuditLogs?.Select(CloneAuditLog).ToList() ?? new List<RiskAssessmentAuditLog>();
    }

    private static User? CloneUser(User? source)
        => source is null
            ? null
            : new User
            {
                Id = source.Id,
                Username = source.Username,
                FullName = source.FullName,
                Role = source.Role,
                Email = source.Email,
                Active = source.Active,
                Phone = source.Phone
            };

    private static DocumentControl CloneDocument(DocumentControl source)
        => new()
        {
            Id = source.Id,
            Code = source.Code,
            Title = source.Title,
            Status = source.Status,
            Version = source.Version,
            EffectiveDate = source.EffectiveDate,
            AuthorId = source.AuthorId,
            OwnerId = source.OwnerId
        };

    private static RiskWorkflowEntry CloneWorkflowEntry(RiskWorkflowEntry source)
        => new()
        {
            Action = source.Action,
            Note = source.Note,
            PerformedBy = source.PerformedBy,
            Timestamp = source.Timestamp
        };

    private static RiskAssessmentAuditLog CloneAuditLog(RiskAssessmentAuditLog source)
        => new()
        {
            Id = source.Id,
            RiskAssessmentId = source.RiskAssessmentId,
            Action = source.Action,
            ActorUserId = source.ActorUserId,
            ActorUsername = source.ActorUsername,
            Timestamp = source.Timestamp,
            Details = source.Details,
            IpAddress = source.IpAddress,
            DeviceInfo = source.DeviceInfo,
            SessionId = source.SessionId
        };
}
