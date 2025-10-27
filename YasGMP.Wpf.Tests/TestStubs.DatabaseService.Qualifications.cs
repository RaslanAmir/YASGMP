using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services;

public partial class DatabaseService
{
    public List<Qualification> Qualifications { get; } = new();

    public Func<bool, bool, bool, CancellationToken, IEnumerable<Qualification>>? QualificationsProvider { get; set; }

    public Exception? QualificationsException { get; set; }

    public Exception? QualificationsWorkflowException { get; set; }

    public List<(bool IncludeAudit, bool IncludeCertificates, bool IncludeAttachments)> GetAllQualificationsCalls { get; } = new();

    public List<(Qualification Qualification, string Signature, string Ip, string DeviceInfo, string? SessionId)> QualificationAddCalls { get; } = new();

    public List<(Qualification Qualification, string Signature, string Ip, string DeviceInfo, string? SessionId)> QualificationUpdateCalls { get; } = new();

    public List<(int QualificationId, string Ip, string DeviceInfo, string? SessionId)> QualificationDeleteCalls { get; } = new();

    public List<(int QualificationId, string Ip, string DeviceInfo, string? SessionId)> QualificationRollbackCalls { get; } = new();

    public List<(List<Qualification> Items, string Ip, string DeviceInfo, string? SessionId)> QualificationExportCalls { get; } = new();

    public List<(Qualification? Qualification, string Action, string Ip, string DeviceInfo, string? SessionId, string? Details)> QualificationAuditEntries { get; } = new();

    public Task<List<Qualification>> GetAllQualificationsAsync(
        bool includeAudit = true,
        bool includeCertificates = true,
        bool includeAttachments = true,
        CancellationToken token = default)
    {
        if (QualificationsException is not null)
        {
            throw QualificationsException;
        }

        GetAllQualificationsCalls.Add((includeAudit, includeCertificates, includeAttachments));

        var source = QualificationsProvider?.Invoke(includeAudit, includeCertificates, includeAttachments, token) ?? Qualifications;
        return Task.FromResult(source
            .Select(CloneQualification)
            .OrderBy(qualification => qualification.Id)
            .ToList());
    }

    public Task AddQualificationAsync(
        Qualification qualification,
        string signature,
        string ip,
        string deviceInfo,
        string? sessionId,
        CancellationToken token = default)
    {
        if (qualification is null)
        {
            throw new ArgumentNullException(nameof(qualification));
        }

        if (QualificationsWorkflowException is not null)
        {
            throw QualificationsWorkflowException;
        }

        var stored = CloneQualification(qualification);
        if (stored.Id == 0)
        {
            stored.Id = Qualifications.Count == 0 ? 1 : Qualifications.Max(item => item.Id) + 1;
        }

        UpsertQualification(stored);
        QualificationAddCalls.Add((CloneQualification(stored), signature, ip, deviceInfo, sessionId));
        return Task.CompletedTask;
    }

    public Task UpdateQualificationAsync(
        Qualification qualification,
        string signature,
        string ip,
        string deviceInfo,
        string? sessionId,
        CancellationToken token = default)
    {
        if (qualification is null)
        {
            throw new ArgumentNullException(nameof(qualification));
        }

        if (QualificationsWorkflowException is not null)
        {
            throw QualificationsWorkflowException;
        }

        var stored = CloneQualification(qualification);
        UpsertQualification(stored);
        QualificationUpdateCalls.Add((CloneQualification(stored), signature, ip, deviceInfo, sessionId));
        return Task.CompletedTask;
    }

    public Task DeleteQualificationAsync(
        int id,
        string ip,
        string deviceInfo,
        string? sessionId,
        CancellationToken token = default)
    {
        if (QualificationsWorkflowException is not null)
        {
            throw QualificationsWorkflowException;
        }

        QualificationDeleteCalls.Add((id, ip, deviceInfo, sessionId));
        var existing = Qualifications.FirstOrDefault(item => item.Id == id);
        if (existing is not null)
        {
            Qualifications.Remove(existing);
        }

        return Task.CompletedTask;
    }

    public Task RollbackQualificationAsync(
        int id,
        string ip,
        string deviceInfo,
        string? sessionId,
        CancellationToken token = default)
    {
        if (QualificationsWorkflowException is not null)
        {
            throw QualificationsWorkflowException;
        }

        QualificationRollbackCalls.Add((id, ip, deviceInfo, sessionId));
        return Task.CompletedTask;
    }

    public Task ExportQualificationsAsync(
        List<Qualification>? items,
        string ip,
        string deviceInfo,
        string? sessionId,
        CancellationToken token = default)
    {
        if (QualificationsWorkflowException is not null)
        {
            throw QualificationsWorkflowException;
        }

        var snapshot = items?.Select(CloneQualification).ToList() ?? new List<Qualification>();
        QualificationExportCalls.Add((snapshot, ip, deviceInfo, sessionId));
        return Task.CompletedTask;
    }

    public Task LogQualificationAuditAsync(
        Qualification? qualification,
        string action,
        string ip,
        string deviceInfo,
        string? sessionId,
        string? details,
        CancellationToken token = default)
    {
        QualificationAuditEntries.Add((qualification is null ? null : CloneQualification(qualification), action, ip, deviceInfo, sessionId, details));
        return Task.CompletedTask;
    }

    private void UpsertQualification(Qualification source)
    {
        var existing = Qualifications.FirstOrDefault(item => item.Id == source.Id);
        if (existing is null)
        {
            Qualifications.Add(CloneQualification(source));
            return;
        }

        CopyQualification(source, existing);
    }

    private static Qualification CloneQualification(Qualification source)
        => new()
        {
            Id = source.Id,
            Code = source.Code,
            Type = source.Type,
            Description = source.Description,
            Date = source.Date,
            ExpiryDate = source.ExpiryDate,
            Status = source.Status,
            MachineId = source.MachineId,
            Machine = CloneMachine(source.Machine),
            ComponentId = source.ComponentId,
            Component = CloneComponent(source.Component),
            SupplierId = source.SupplierId,
            Supplier = CloneSupplier(source.Supplier),
            QualifiedById = source.QualifiedById,
            QualifiedBy = CloneUser(source.QualifiedBy),
            ApprovedById = source.ApprovedById,
            ApprovedBy = CloneUser(source.ApprovedBy),
            ApprovedAt = source.ApprovedAt,
            DigitalSignature = source.DigitalSignature,
            CertificateNumber = source.CertificateNumber,
            Documents = source.Documents?.Select(CloneDocument).ToList() ?? new List<DocumentControl>(),
            AuditLogs = source.AuditLogs?.Select(CloneAuditLog).ToList() ?? new List<QualificationAuditLog>(),
            Note = source.Note
        };

    private static void CopyQualification(Qualification source, Qualification destination)
    {
        destination.Code = source.Code;
        destination.Type = source.Type;
        destination.Description = source.Description;
        destination.Date = source.Date;
        destination.ExpiryDate = source.ExpiryDate;
        destination.Status = source.Status;
        destination.MachineId = source.MachineId;
        destination.Machine = CloneMachine(source.Machine);
        destination.ComponentId = source.ComponentId;
        destination.Component = CloneComponent(source.Component);
        destination.SupplierId = source.SupplierId;
        destination.Supplier = CloneSupplier(source.Supplier);
        destination.QualifiedById = source.QualifiedById;
        destination.QualifiedBy = CloneUser(source.QualifiedBy);
        destination.ApprovedById = source.ApprovedById;
        destination.ApprovedBy = CloneUser(source.ApprovedBy);
        destination.ApprovedAt = source.ApprovedAt;
        destination.DigitalSignature = source.DigitalSignature;
        destination.CertificateNumber = source.CertificateNumber;
        destination.Documents = source.Documents?.Select(CloneDocument).ToList() ?? new List<DocumentControl>();
        destination.AuditLogs = source.AuditLogs?.Select(CloneAuditLog).ToList() ?? new List<QualificationAuditLog>();
        destination.Note = source.Note;
    }

    private static Machine? CloneMachine(Machine? source)
        => source is null
            ? null
            : new Machine
            {
                Id = source.Id,
                Name = source.Name,
                Code = source.Code,
                SerialNumber = source.SerialNumber,
                Model = source.Model,
                Manufacturer = source.Manufacturer,
                Location = source.Location,
                Status = source.Status,
                Note = source.Note
            };

    private static MachineComponent? CloneComponent(MachineComponent? source)
        => source is null
            ? null
            : new MachineComponent
            {
                Id = source.Id,
                MachineId = source.MachineId,
                Name = source.Name,
                Code = source.Code,
                Status = source.Status,
                Note = source.Note
            };

    private static Supplier? CloneSupplier(Supplier? source)
        => source is null
            ? null
            : new Supplier
            {
                Id = source.Id,
                Name = source.Name,
                Code = source.Code,
                Status = source.Status,
                Note = source.Note
            };

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
            DocumentType = source.DocumentType,
            Revision = source.Revision,
            Status = source.Status,
            FilePath = source.FilePath,
            Description = source.Description,
            Attachments = source.Attachments,
            RevisionHistory = source.RevisionHistory,
            StatusHistory = source.StatusHistory,
            LinkedChangeControls = source.LinkedChangeControls,
            ExpiryDate = source.ExpiryDate,
            DeviceInfo = source.DeviceInfo,
            SessionId = source.SessionId,
            IpAddress = source.IpAddress,
            CreatedById = source.CreatedById,
            CreatedBy = CloneUser(source.CreatedBy),
            CreatedAt = source.CreatedAt,
            UpdatedById = source.UpdatedById,
            UpdatedBy = CloneUser(source.UpdatedBy),
            UpdatedAt = source.UpdatedAt
        };

    private static QualificationAuditLog CloneAuditLog(QualificationAuditLog source)
        => new()
        {
            Id = source.Id,
            QualificationId = source.QualificationId,
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
