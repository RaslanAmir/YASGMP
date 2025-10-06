using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Tests.TestDoubles;

public sealed class RecordingAuditService : AuditService, IAttachmentWorkflowAudit
{
    public RecordingAuditService(DatabaseService databaseService)
        : base(databaseService)
    {
    }

    public List<EntityAuditRecord> EntityAudits { get; } = new();

    public List<AttachmentAuditRecord> AttachmentAudits { get; } = new();

    public override Task LogEntityAuditAsync(string? tableName, int entityId, string? action, string? details)
    {
        EntityAudits.Add(new EntityAuditRecord(
            tableName ?? string.Empty,
            entityId,
            action ?? string.Empty,
            details ?? string.Empty));
        return Task.CompletedTask;
    }

    public Task LogAttachmentUploadAsync(
        int? actorUserId,
        string entityType,
        int entityId,
        string description,
        int attachmentId,
        CancellationToken token)
    {
        AttachmentAudits.Add(new AttachmentAuditRecord(actorUserId, entityType, entityId, description, attachmentId));
        return Task.CompletedTask;
    }

    public sealed record EntityAuditRecord(string Table, int EntityId, string Action, string Details);

    public sealed record AttachmentAuditRecord(int? ActorUserId, string EntityType, int EntityId, string Description, int AttachmentId);
}

