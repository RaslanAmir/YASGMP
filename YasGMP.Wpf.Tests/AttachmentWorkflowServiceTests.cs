using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Tests;

public class AttachmentWorkflowServiceTests
{
    [Fact]
    public async Task UploadAsync_LogsAuditEntry_ForNewUpload()
    {
        var databaseService = new DatabaseService("Server=localhost;Database=unit_test;Uid=test;Pwd=test;");
        var attachmentService = new FakeAttachmentService
        {
            UploadResult = FakeAttachmentService.CreateUploadResult(11, "machines", 5)
        };
        var auditService = new RecordingAuditService(databaseService);
        var workflow = new AttachmentWorkflowService(
            attachmentService,
            databaseService,
            new AttachmentEncryptionOptions(),
            auditService);

        await using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var request = new AttachmentUploadRequest
        {
            FileName = "spec.pdf",
            EntityType = "machines",
            EntityId = 5,
            UploadedById = 42,
            Reason = "unit-test"
        };

        var result = await workflow.UploadAsync(stream, request, CancellationToken.None).ConfigureAwait(false);

        Assert.False(result.Deduplicated);
        var record = Assert.Single(auditService.Records);
        Assert.Equal(request.UploadedById, record.ActorUserId);
        Assert.Equal(request.EntityType, record.EntityType);
        Assert.Equal(request.EntityId, record.EntityId);
        Assert.Equal(result.Attachment.Id, record.AttachmentId);
        Assert.Contains("dedup=new", record.Description);
        Assert.Contains($"reason={request.Reason}", record.Description);
        Assert.Contains("ts=", record.Description);
    }

    [Fact]
    public async Task UploadAsync_LogsAuditEntry_ForDeduplicatedUpload()
    {
        var databaseService = new DatabaseService("Server=localhost;Database=unit_test;Uid=test;Pwd=test;");
        var uploadResult = FakeAttachmentService.CreateUploadResult(21, "incidents", 8);
        var attachmentService = new FakeAttachmentService
        {
            UploadResult = uploadResult,
            ExistingAttachment = uploadResult.Attachment
        };
        var auditService = new RecordingAuditService(databaseService);
        var workflow = new AttachmentWorkflowService(
            attachmentService,
            databaseService,
            new AttachmentEncryptionOptions(),
            auditService);

        await using var stream = new MemoryStream(new byte[] { 9, 8, 7, 6 });
        var request = new AttachmentUploadRequest
        {
            FileName = "dedup.txt",
            EntityType = "incidents",
            EntityId = 8,
            UploadedById = 7,
            Reason = "dedup-check"
        };

        var result = await workflow.UploadAsync(stream, request, CancellationToken.None).ConfigureAwait(false);

        Assert.True(result.Deduplicated);
        var record = Assert.Single(auditService.Records);
        Assert.Equal(request.UploadedById, record.ActorUserId);
        Assert.Equal(result.Attachment.Id, record.AttachmentId);
        Assert.Contains("dedup=deduplicated", record.Description);
        Assert.Contains("existing=21", record.Description);
    }

    private sealed class FakeAttachmentService : IAttachmentService
    {
        public AttachmentUploadResult UploadResult { get; set; } = CreateUploadResult(1, "", 0);

        public Attachment? ExistingAttachment { get; set; }

        public List<AttachmentUploadRequest> Requests { get; } = new();

        public Task<AttachmentUploadResult> UploadAsync(Stream content, AttachmentUploadRequest request, CancellationToken token = default)
        {
            Requests.Add(request);
            return Task.FromResult(UploadResult);
        }

        public Task<Attachment?> FindByHashAsync(string sha256, CancellationToken token = default)
            => Task.FromResult<Attachment?>(ExistingAttachment);

        public Task<Attachment?> FindByHashAndSizeAsync(string sha256, long fileSize, CancellationToken token = default)
            => Task.FromResult<Attachment?>(ExistingAttachment);

        public Task<AttachmentStreamResult> StreamContentAsync(int attachmentId, Stream destination, AttachmentReadRequest? request = null, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<AttachmentLinkWithAttachment>> GetLinksForEntityAsync(string entityType, int entityId, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task RemoveLinkAsync(int linkId, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task RemoveLinkAsync(string entityType, int entityId, int attachmentId, CancellationToken token = default)
            => throw new NotSupportedException();

        public static AttachmentUploadResult CreateUploadResult(int attachmentId, string entityType, int entityId)
        {
            var attachment = new Attachment
            {
                Id = attachmentId,
                EntityTable = entityType,
                EntityId = entityId,
                FileName = $"{attachmentId}.bin"
            };
            var link = new AttachmentLink
            {
                Id = attachmentId,
                AttachmentId = attachmentId,
                EntityType = entityType,
                EntityId = entityId
            };
            var retention = new RetentionPolicy
            {
                PolicyName = "default",
                RetainUntil = DateTime.UtcNow.AddDays(30)
            };
            return new AttachmentUploadResult(attachment, link, retention);
        }
    }

    private sealed record AuditRecord(int? ActorUserId, string EntityType, int EntityId, string Description, int AttachmentId);

    private sealed class RecordingAuditService : AuditService, IAttachmentWorkflowAudit
    {
        public RecordingAuditService(DatabaseService db)
            : base(db)
        {
        }

        public List<AuditRecord> Records { get; } = new();

        public Task LogAttachmentUploadAsync(int? actorUserId, string entityType, int entityId, string description, int attachmentId, CancellationToken token)
        {
            Records.Add(new AuditRecord(actorUserId, entityType, entityId, description, attachmentId));
            return Task.CompletedTask;
        }
    }
}
