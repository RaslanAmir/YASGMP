using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Tests;

public sealed class TrainingRecordServiceAdapterTests
{
    [Fact]
    public async Task LoadAsync_PopulatesRecordsAndAttachments()
    {
        var context = CreateContext();
        var recordWithManifest = CreateRecord(5, "TR-001", "Approved record");
        var draftRecord = CreateRecord(0, "TR-002", "Draft");
        context.Database.LoadRecords.AddRange(new[] { recordWithManifest, draftRecord });

        var manifest = new[]
        {
            CreateAttachmentLink(11, "sop.pdf"),
            CreateAttachmentLink(12, "certificate.jpg")
        };
        context.AttachmentService.SetManifest(recordWithManifest.Id, manifest);

        var result = await context.Adapter.LoadAsync();

        Assert.True(result.Success);
        Assert.Equal("Loaded 2 training records.", result.Message);
        Assert.Equal(1, context.Database.LoadCallCount);
        var hydrated = Assert.Single(result.Records, r => r.Id == recordWithManifest.Id);
        Assert.Equal(context.Auth.CurrentIpAddress, hydrated.IpAddress);
        Assert.Equal(context.Auth.CurrentDeviceInfo, hydrated.DeviceInfo);
        Assert.Equal(context.Auth.CurrentSessionId, hydrated.SessionId);
        Assert.Equal(manifest.Select(m => m.Attachment), hydrated.Attachments);
        Assert.DoesNotContain(result.Records, r => r.Id == draftRecord.Id && r.Attachments?.Count > 0);

        var request = Assert.Single(context.AttachmentService.GetLinksRequests);
        Assert.Equal("user_training", request.EntityType);
        Assert.Equal(recordWithManifest.Id, request.EntityId);
    }

    [Fact]
    public async Task LoadAsync_WhenDatabaseThrows_ReturnsFailure()
    {
        var context = CreateContext();
        context.Database.LoadException = new InvalidOperationException("boom");

        var result = await context.Adapter.LoadAsync();

        Assert.False(result.Success);
        Assert.Contains("Failed to load training records", result.Message, StringComparison.Ordinal);
        Assert.Contains("boom", result.Message, StringComparison.Ordinal);
        Assert.Empty(result.Records);
        Assert.Empty(context.AttachmentService.GetLinksRequests);
    }

    [Fact]
    public async Task InitiateAsync_Success_PersistsRecordAndLogsAudit()
    {
        var context = CreateContext();
        var draft = new TrainingRecord
        {
            Title = "GMP Orientation",
            PlannedBy = string.Empty
        };

        var result = await context.Adapter.InitiateAsync(draft, "Initial schedule");

        Assert.True(result.Success);
        Assert.Equal("Training record 'GMP Orientation' initiated.", result.Message);
        var initiated = Assert.Single(context.Database.InitiatedRecords);
        Assert.Equal(context.Auth.CurrentIpAddress, initiated.IpAddress);
        Assert.Equal(context.Auth.CurrentDeviceInfo, initiated.DeviceInfo);
        Assert.Equal(context.Auth.CurrentSessionId, initiated.SessionId);
        Assert.Equal(context.Auth.CurrentUser?.UserName, initiated.PlannedBy);
        Assert.True(initiated.PlannedAt != default);

        var audit = Assert.Single(context.Database.AuditLogs);
        Assert.Equal("INITIATE", audit.Action);
        Assert.Equal("Initial schedule", audit.Description);
    }

    [Fact]
    public async Task InitiateAsync_WhenDatabaseThrows_ReturnsFailure()
    {
        var context = CreateContext();
        context.Database.InitiateException = new InvalidOperationException("locked");
        var draft = CreateRecord(0, "TR-010", "Pending");

        var result = await context.Adapter.InitiateAsync(draft, "note");

        Assert.False(result.Success);
        Assert.Contains("Initiation failed", result.Message, StringComparison.Ordinal);
        Assert.Contains("locked", result.Message, StringComparison.Ordinal);
        Assert.Empty(context.Database.AuditLogs);
    }

    [Fact]
    public async Task AssignAsync_Success_RefreshesAttachments()
    {
        var context = CreateContext();
        var record = CreateRecord(42, "TR-042", "Assignment");
        var manifest = new[] { CreateAttachmentLink(51, "evidence.txt") };
        context.AttachmentService.SetManifest(record.Id, manifest);

        var result = await context.Adapter.AssignAsync(record, 99, "Assign trainee");

        Assert.True(result.Success);
        Assert.Equal("Training record 42 assigned to user 99.", result.Message);
        var assign = Assert.Single(context.Database.AssignCalls);
        Assert.Equal(42, assign.RecordId);
        Assert.Equal(99, assign.AssignedTo);
        Assert.Equal("Assign trainee", assign.Note);
        Assert.Equal(context.Auth.CurrentIpAddress, assign.IpAddress);
        Assert.Equal(context.Auth.CurrentDeviceInfo, assign.DeviceInfo);
        Assert.Equal(context.Auth.CurrentSessionId, assign.SessionId);

        var linksRequest = Assert.Single(context.AttachmentService.GetLinksRequests);
        Assert.Equal("user_training", linksRequest.EntityType);
        Assert.Equal(42, linksRequest.EntityId);
        Assert.Equal(manifest.Select(m => m.Attachment), record.Attachments);
    }

    [Fact]
    public async Task AssignAsync_WhenRecordNotPersisted_ReturnsFailure()
    {
        var context = CreateContext();
        var record = CreateRecord(0, "TR-000", "New");

        var result = await context.Adapter.AssignAsync(record, 5, "note");

        Assert.False(result.Success);
        Assert.Equal("Select a training record before assigning.", result.Message);
        Assert.Empty(context.Database.AssignCalls);
    }

    [Fact]
    public async Task AssignAsync_WhenAssigneeMissing_ReturnsFailure()
    {
        var context = CreateContext();
        var record = CreateRecord(3, "TR-003", "Need assignee");
        record.TraineeId = null;
        context.Auth.CurrentUser = null;

        var result = await context.Adapter.AssignAsync(record, null, null);

        Assert.False(result.Success);
        Assert.Equal("A valid assignee is required.", result.Message);
        Assert.Empty(context.Database.AssignCalls);
        Assert.Empty(context.AttachmentService.GetLinksRequests);
    }

    [Fact]
    public async Task AssignAsync_WhenDatabaseThrows_ReturnsFailure()
    {
        var context = CreateContext();
        context.Database.AssignException = new InvalidOperationException("fail");
        var record = CreateRecord(15, "TR-015", "Failure");

        var result = await context.Adapter.AssignAsync(record, 20, "note");

        Assert.False(result.Success);
        Assert.Contains("Assignment failed", result.Message, StringComparison.Ordinal);
        Assert.Empty(record.Attachments);
        Assert.Empty(context.AttachmentService.GetLinksRequests);
    }

    [Fact]
    public async Task ApproveAsync_Success_PersistsSignatureAndRefreshes()
    {
        var context = CreateContext();
        var record = CreateRecord(7, "TR-007", "Approval");
        var manifest = new[] { CreateAttachmentLink(80, "approval.txt") };
        context.AttachmentService.SetManifest(record.Id, manifest);
        context.Signature.QueueCapture(CreateSignature("hash-approve"));

        var result = await context.Adapter.ApproveAsync(record);

        Assert.True(result.Success);
        Assert.Equal("Training record 7 approved.", result.Message);
        Assert.Equal(1, context.Database.InsertDigitalSignatureCallCount);
        var inserted = Assert.Single(context.Database.InsertedSignatures);
        Assert.Equal("user_training", inserted.TableName);
        Assert.Equal(7, inserted.RecordId);
        Assert.Equal(context.Auth.CurrentUser?.Id, inserted.UserId);
        Assert.Equal(context.Auth.CurrentDeviceInfo, inserted.DeviceInfo);
        Assert.Equal(context.Auth.CurrentIpAddress, inserted.IpAddress);
        Assert.Equal(context.Auth.CurrentSessionId, inserted.SessionId);

        var approve = Assert.Single(context.Database.ApproveCalls);
        Assert.Equal(7, approve.RecordId);
        Assert.Equal(context.Auth.CurrentUser?.Id ?? 0, approve.ActorId);

        Assert.Equal(1, context.Signature.LogCallCount);
        var linksRequest = Assert.Single(context.AttachmentService.GetLinksRequests);
        Assert.Equal(7, linksRequest.EntityId);
        Assert.Equal(manifest.Select(m => m.Attachment), record.Attachments);
    }

    [Fact]
    public async Task ApproveAsync_WhenSignatureCancelled_ReturnsFailure()
    {
        var context = CreateContext();
        context.Signature.QueueCapture(null);
        var record = CreateRecord(9, "TR-009", "Approval");

        var result = await context.Adapter.ApproveAsync(record);

        Assert.False(result.Success);
        Assert.Equal("Electronic signature cancelled.", result.Message);
        Assert.Empty(context.Database.InsertedSignatures);
        Assert.Empty(context.Database.ApproveCalls);
        Assert.Equal(0, context.Signature.LogCallCount);
        Assert.Empty(context.AttachmentService.GetLinksRequests);
    }

    [Fact]
    public async Task ApproveAsync_WhenDatabaseThrows_ReturnsFailure()
    {
        var context = CreateContext();
        var record = CreateRecord(10, "TR-010", "Approval failure");
        context.Signature.QueueCapture(CreateSignature("hash-fail"));
        context.Database.ApproveException = new InvalidOperationException("denied");

        var result = await context.Adapter.ApproveAsync(record);

        Assert.False(result.Success);
        Assert.Contains("Approval failed", result.Message, StringComparison.Ordinal);
        Assert.Contains("denied", result.Message, StringComparison.Ordinal);
        Assert.Equal(1, context.Database.InsertDigitalSignatureCallCount);
        Assert.Empty(context.Signature.LoggedResults);
        Assert.Empty(context.AttachmentService.GetLinksRequests);
    }

    [Fact]
    public async Task CompleteAsync_Success_UploadsAttachments()
    {
        var context = CreateContext();
        var record = CreateRecord(21, "TR-021", "Completion");
        var manifest = new[] { CreateAttachmentLink(201, "final.txt") };
        context.AttachmentService.SetManifest(record.Id, manifest);
        context.Signature.QueueCapture(CreateSignature("hash-complete"));
        var uploads = new[]
        {
            CreateUpload("evidence.pdf", "PDF content"),
            CreateUpload("report.txt", "Text content")
        };

        var result = await context.Adapter.CompleteAsync(record, "done", uploads);

        Assert.True(result.Success);
        Assert.Equal("Training record 21 completed.", result.Message);
        var complete = Assert.Single(context.Database.CompleteCalls);
        Assert.Equal(21, complete.RecordId);
        Assert.Equal(context.Auth.CurrentUser?.Id ?? 0, complete.ActorId);
        Assert.Equal("done", complete.Note);
        Assert.Equal(context.Auth.CurrentIpAddress, complete.IpAddress);
        Assert.Equal(context.Auth.CurrentDeviceInfo, complete.DeviceInfo);
        Assert.Equal(context.Auth.CurrentSessionId, complete.SessionId);

        Assert.Equal(2, context.Workflow.UploadCalls.Count);
        Assert.All(context.Workflow.UploadCalls, call =>
        {
            Assert.Equal("user_training", call.Request.EntityType);
            Assert.Equal(record.Id, call.Request.EntityId);
            Assert.Equal(context.Auth.CurrentUser?.Id, call.Request.UploadedById);
            Assert.Equal(context.Auth.CurrentDeviceInfo, call.Request.SourceHost);
            Assert.Equal(context.Auth.CurrentIpAddress, call.Request.SourceIp);
            Assert.Equal($"training_record:{record.Id}", call.Request.Notes);
            Assert.Equal($"training_record:{record.Id}", call.Request.Reason);
        });

        Assert.Equal(1, context.Signature.LogCallCount);
        Assert.Equal(manifest.Select(m => m.Attachment), record.Attachments);
    }

    [Fact]
    public async Task CompleteAsync_WhenSignatureCancelled_ShortCircuits()
    {
        var context = CreateContext();
        context.Signature.QueueCapture(null);
        var record = CreateRecord(30, "TR-030", "Completion");

        var result = await context.Adapter.CompleteAsync(record, "note", Array.Empty<TrainingRecordAttachmentUpload>());

        Assert.False(result.Success);
        Assert.Equal("Electronic signature cancelled.", result.Message);
        Assert.Empty(context.Database.CompleteCalls);
        Assert.Equal(0, context.Workflow.UploadCalls.Count);
        Assert.Equal(0, context.Signature.LogCallCount);
    }

    [Fact]
    public async Task CompleteAsync_WithNullAttachments_DoesNotUpload()
    {
        var context = CreateContext();
        context.Signature.QueueCapture(CreateSignature("hash"));
        var record = CreateRecord(31, "TR-031", "Completion");

        var result = await context.Adapter.CompleteAsync(record, null, null);

        Assert.True(result.Success);
        Assert.Equal(0, context.Workflow.UploadCalls.Count);
    }

    [Fact]
    public async Task CloseAsync_Success_RefreshesAttachments()
    {
        var context = CreateContext();
        var record = CreateRecord(55, "TR-055", "Closure");
        var manifest = new[] { CreateAttachmentLink(303, "closure.pdf") };
        context.AttachmentService.SetManifest(record.Id, manifest);
        context.Signature.QueueCapture(CreateSignature("hash-close"));
        var uploads = new[] { CreateUpload("evidence.docx", "Doc content") };

        var result = await context.Adapter.CloseAsync(record, "closed", uploads);

        Assert.True(result.Success);
        Assert.Equal("Training record 55 closed.", result.Message);
        var close = Assert.Single(context.Database.CloseCalls);
        Assert.Equal(55, close.RecordId);
        Assert.Equal("closed", close.Note);
        Assert.Equal(context.Auth.CurrentUser?.Id ?? 0, close.ActorId);

        Assert.Single(context.Workflow.UploadCalls);
        var request = context.Workflow.UploadCalls[0].Request;
        Assert.Equal("user_training", request.EntityType);
        Assert.Equal(55, request.EntityId);

        Assert.Equal(1, context.Signature.LogCallCount);
        Assert.Equal(manifest.Select(m => m.Attachment), record.Attachments);
    }

    [Fact]
    public async Task CloseAsync_WhenSignatureCancelled_ReturnsFailure()
    {
        var context = CreateContext();
        context.Signature.QueueCapture(null);
        var record = CreateRecord(60, "TR-060", "Closure");

        var result = await context.Adapter.CloseAsync(record, "closed", Array.Empty<TrainingRecordAttachmentUpload>());

        Assert.False(result.Success);
        Assert.Equal("Electronic signature cancelled.", result.Message);
        Assert.Empty(context.Database.CloseCalls);
        Assert.Equal(0, context.Workflow.UploadCalls.Count);
    }

    [Fact]
    public async Task CloseAsync_WithNullAttachments_DoesNotUpload()
    {
        var context = CreateContext();
        context.Signature.QueueCapture(CreateSignature("hash-close"));
        var record = CreateRecord(61, "TR-061", "Closure");

        var result = await context.Adapter.CloseAsync(record, null, null);

        Assert.True(result.Success);
        Assert.Equal(0, context.Workflow.UploadCalls.Count);
    }

    [Fact]
    public async Task ExportAsync_Success_InvokesDatabase()
    {
        var context = CreateContext();
        var records = new[] { CreateRecord(1, "TR-1", "Export") };

        var result = await context.Adapter.ExportAsync(records, " csv ");

        Assert.True(result.Success);
        Assert.Equal("Training records exported (CSV).", result.Message);
        var export = Assert.Single(context.Database.ExportCalls);
        Assert.Equal(records, export.Records);
        Assert.Equal("csv", export.Format);
        Assert.Equal(context.Auth.CurrentIpAddress, export.IpAddress);
        Assert.Equal(context.Auth.CurrentDeviceInfo, export.DeviceInfo);
        Assert.Equal(context.Auth.CurrentSessionId, export.SessionId);
    }

    [Fact]
    public async Task ExportAsync_WhenDatabaseThrows_ReturnsFailure()
    {
        var context = CreateContext();
        context.Database.ExportException = new InvalidOperationException("offline");

        var result = await context.Adapter.ExportAsync(Array.Empty<TrainingRecord>(), "csv");

        Assert.False(result.Success);
        Assert.Contains("Export failed", result.Message, StringComparison.Ordinal);
        Assert.Contains("offline", result.Message, StringComparison.Ordinal);
    }

    private static AdapterContext CreateContext()
    {
        var database = new RecordingTrainingDatabaseService();
        var auth = new RecordingAuthContext
        {
            CurrentUser = new User { Id = 77, UserName = "tester" },
            CurrentSessionId = "sess-123",
            CurrentDeviceInfo = "QA-LAB",
            CurrentIpAddress = "10.0.0.5"
        };
        var attachmentService = new RecordingAttachmentService();
        var workflow = new RecordingAttachmentWorkflowService();
        var signature = new RecordingSignatureDialogService();
        var adapter = new TrainingRecordServiceAdapter(database, auth, workflow, attachmentService, signature);
        return new AdapterContext(adapter, database, auth, workflow, attachmentService, signature);
    }

    private static TrainingRecord CreateRecord(int id, string code, string title)
        => new()
        {
            Id = id,
            Code = code,
            Title = title,
            Name = title,
            Attachments = new List<Attachment>()
        };

    private static ElectronicSignatureDialogResult CreateSignature(string hash)
    {
        return new ElectronicSignatureDialogResult(
            "password",
            "QA",
            "Approved",
            "QA Approval",
            new DigitalSignature
            {
                SignatureHash = hash,
                Status = "valid",
                Method = "password",
                SignedAt = DateTime.UtcNow
            });
    }

    private static AttachmentLinkWithAttachment CreateAttachmentLink(int attachmentId, string fileName)
    {
        return new AttachmentLinkWithAttachment(
            new AttachmentLink { Id = attachmentId, AttachmentId = attachmentId },
            new Attachment { Id = attachmentId, FileName = fileName });
    }

    private static TrainingRecordAttachmentUpload CreateUpload(string fileName, string content)
    {
        return new TrainingRecordAttachmentUpload(
            fileName,
            "application/octet-stream",
            async token =>
            {
                await Task.Yield();
                return new MemoryStream(Encoding.UTF8.GetBytes(content));
            });
    }

    private sealed record AdapterContext(
        TrainingRecordServiceAdapter Adapter,
        RecordingTrainingDatabaseService Database,
        RecordingAuthContext Auth,
        RecordingAttachmentWorkflowService Workflow,
        RecordingAttachmentService AttachmentService,
        RecordingSignatureDialogService Signature);

    private sealed class RecordingTrainingDatabaseService : DatabaseService
    {
        private int _nextSignatureId = 1;

        public RecordingTrainingDatabaseService()
            : base("Server=localhost;Database=unit_test;Uid=test;Pwd=test;")
        {
        }

        public List<TrainingRecord> LoadRecords { get; } = new();
        public Exception? LoadException { get; set; }
        public int LoadCallCount { get; private set; }
        public List<TrainingRecord> InitiatedRecords { get; } = new();
        public Exception? InitiateException { get; set; }
        public List<(int RecordId, int AssignedTo, string IpAddress, string DeviceInfo, string? SessionId, string? Note)> AssignCalls { get; } = new();
        public Exception? AssignException { get; set; }
        public List<(int RecordId, int ActorId)> ApproveCalls { get; } = new();
        public Exception? ApproveException { get; set; }
        public List<(int RecordId, int ActorId, string? Note, string IpAddress, string DeviceInfo, string? SessionId)> CompleteCalls { get; } = new();
        public Exception? CompleteException { get; set; }
        public List<(int RecordId, int ActorId, string? Note, string IpAddress, string DeviceInfo, string? SessionId)> CloseCalls { get; } = new();
        public Exception? CloseException { get; set; }
        public List<(IList<TrainingRecord> Records, string Format, string IpAddress, string DeviceInfo, string SessionId)> ExportCalls { get; } = new();
        public Exception? ExportException { get; set; }
        public List<(TrainingRecord? Record, string Action, string IpAddress, string DeviceInfo, string SessionId, string? Description)> AuditLogs { get; } = new();
        public List<DigitalSignature> InsertedSignatures { get; } = new();
        public Exception? InsertSignatureException { get; set; }
        public int InsertDigitalSignatureCallCount { get; private set; }

        public Task<List<TrainingRecord>> GetAllTrainingRecordsFullAsync(CancellationToken cancellationToken = default)
        {
            LoadCallCount++;
            if (LoadException is not null)
            {
                throw LoadException;
            }

            return Task.FromResult(LoadRecords.Select(CloneRecord).ToList());
        }

        public Task InitiateTrainingRecordAsync(TrainingRecord record, CancellationToken cancellationToken = default)
        {
            if (InitiateException is not null)
            {
                throw InitiateException;
            }

            InitiatedRecords.Add(CloneRecord(record));
            return Task.CompletedTask;
        }

        public Task AssignTrainingRecordAsync(
            int trainingRecordId,
            int assignedToUserId,
            string ipAddress,
            string deviceInfo,
            string? sessionId,
            string? note = null,
            CancellationToken cancellationToken = default)
        {
            if (AssignException is not null)
            {
                throw AssignException;
            }

            AssignCalls.Add((trainingRecordId, assignedToUserId, ipAddress, deviceInfo, sessionId, note));
            return Task.CompletedTask;
        }

        public Task ApproveTrainingRecordAsync(
            int trainingRecordId,
            int approverUserId,
            string ipAddress,
            string deviceInfo,
            string? sessionId,
            CancellationToken cancellationToken = default)
        {
            if (ApproveException is not null)
            {
                throw ApproveException;
            }

            ApproveCalls.Add((trainingRecordId, approverUserId));
            return Task.CompletedTask;
        }

        public Task CompleteTrainingRecordAsync(
            int trainingRecordId,
            int userId,
            string ipAddress,
            string deviceInfo,
            string? sessionId,
            string? note,
            CancellationToken cancellationToken = default)
        {
            if (CompleteException is not null)
            {
                throw CompleteException;
            }

            CompleteCalls.Add((trainingRecordId, userId, note, ipAddress, deviceInfo, sessionId));
            return Task.CompletedTask;
        }

        public Task CloseTrainingRecordAsync(
            int trainingRecordId,
            int userId,
            string ipAddress,
            string deviceInfo,
            string? sessionId,
            string? note,
            CancellationToken cancellationToken = default)
        {
            if (CloseException is not null)
            {
                throw CloseException;
            }

            CloseCalls.Add((trainingRecordId, userId, note, ipAddress, deviceInfo, sessionId));
            return Task.CompletedTask;
        }

        public Task ExportTrainingRecordsAsync(
            IList<TrainingRecord> records,
            string format,
            string ipAddress,
            string deviceInfo,
            string sessionId,
            CancellationToken cancellationToken = default)
        {
            if (ExportException is not null)
            {
                throw ExportException;
            }

            ExportCalls.Add((records, format, ipAddress, deviceInfo, sessionId));
            return Task.CompletedTask;
        }

        public Task<int> InsertDigitalSignatureAsync(
            DigitalSignature signature,
            CancellationToken cancellationToken = default)
        {
            if (InsertSignatureException is not null)
            {
                throw InsertSignatureException;
            }

            InsertDigitalSignatureCallCount++;
            if (signature.Id <= 0)
            {
                signature.Id = _nextSignatureId++;
            }

            InsertedSignatures.Add(CloneSignature(signature));
            return Task.FromResult(signature.Id);
        }

        public Task LogTrainingRecordAuditAsync(
            TrainingRecord? record,
            string action,
            string ipAddress,
            string deviceInfo,
            string? sessionId,
            string? description = null,
            CancellationToken cancellationToken = default)
        {
            AuditLogs.Add((record is null ? null : CloneRecord(record), action, ipAddress, deviceInfo, sessionId ?? string.Empty, description));
            return Task.CompletedTask;
        }

        private static TrainingRecord CloneRecord(TrainingRecord source)
        {
            return new TrainingRecord
            {
                Id = source.Id,
                Code = source.Code,
                Title = source.Title,
                Name = source.Name,
                PlannedBy = source.PlannedBy,
                PlannedAt = source.PlannedAt,
                TraineeId = source.TraineeId,
                DeviceInfo = source.DeviceInfo,
                SessionId = source.SessionId,
                IpAddress = source.IpAddress,
                Attachments = source.Attachments?.Select(CloneAttachment).ToList() ?? new List<Attachment>()
            };
        }

        private static Attachment CloneAttachment(Attachment attachment)
            => new()
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                FileType = attachment.FileType,
                FileSize = attachment.FileSize
            };

        private static DigitalSignature CloneSignature(DigitalSignature signature)
            => new()
            {
                Id = signature.Id,
                TableName = signature.TableName,
                RecordId = signature.RecordId,
                UserId = signature.UserId,
                SignatureHash = signature.SignatureHash,
                Method = signature.Method,
                Status = signature.Status,
                SignedAt = signature.SignedAt,
                DeviceInfo = signature.DeviceInfo,
                IpAddress = signature.IpAddress,
                SessionId = signature.SessionId,
                Note = signature.Note,
                UserName = signature.UserName,
                PublicKey = signature.PublicKey
            };
    }

    private sealed class RecordingAuthContext : IAuthContext
    {
        public User? CurrentUser { get; set; }
        public string CurrentSessionId { get; set; } = string.Empty;
        public string CurrentDeviceInfo { get; set; } = string.Empty;
        public string CurrentIpAddress { get; set; } = string.Empty;
    }

    private sealed class RecordingAttachmentWorkflowService : IAttachmentWorkflowService
    {
        public List<(byte[] Content, AttachmentUploadRequest Request)> UploadCalls { get; } = new();

        public bool IsEncryptionEnabled => false;
        public string EncryptionKeyId => "test";

        public Task<AttachmentWorkflowUploadResult> UploadAsync(Stream content, AttachmentUploadRequest request, CancellationToken token = default)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using var buffer = new MemoryStream();
            content.CopyTo(buffer);
            UploadCalls.Add((buffer.ToArray(), CloneRequest(request)));

            var attachment = new Attachment { Id = UploadCalls.Count, FileName = request.FileName };
            var link = new AttachmentLink { Id = UploadCalls.Count, AttachmentId = attachment.Id, EntityId = request.EntityId, EntityType = request.EntityType };
            var result = new AttachmentWorkflowUploadResult(
                new AttachmentUploadResult(attachment, link, new RetentionPolicy()),
                string.Empty,
                buffer.Length,
                false,
                null);
            return Task.FromResult(result);
        }

        public Task<AttachmentStreamResult> DownloadAsync(int attachmentId, Stream destination, AttachmentReadRequest? request = null, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<AttachmentLinkWithAttachment>> GetLinksForEntityAsync(string entityType, int entityId, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<Attachment>> GetAttachmentSummariesAsync(string? entityFilter, string? typeFilter, string? searchTerm, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task RemoveLinkAsync(int linkId, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task RemoveLinkAsync(string entityType, int entityId, int attachmentId, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task<Attachment?> FindByHashAsync(string sha256, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task<Attachment?> FindByHashAndSizeAsync(string sha256, long fileSize, CancellationToken token = default)
            => throw new NotSupportedException();

        private static AttachmentUploadRequest CloneRequest(AttachmentUploadRequest request)
        {
            return new AttachmentUploadRequest
            {
                FileName = request.FileName,
                ContentType = request.ContentType,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                UploadedById = request.UploadedById,
                DisplayName = request.DisplayName,
                RetainUntil = request.RetainUntil,
                RetentionPolicyName = request.RetentionPolicyName,
                Notes = request.Notes,
                TenantId = request.TenantId,
                MinRetainDays = request.MinRetainDays,
                MaxRetainDays = request.MaxRetainDays,
                LegalHold = request.LegalHold,
                DeleteMode = request.DeleteMode,
                ReviewRequired = request.ReviewRequired,
                Reason = request.Reason,
                SourceIp = request.SourceIp,
                SourceHost = request.SourceHost
            };
        }
    }

    private sealed class RecordingAttachmentService : IAttachmentService
    {
        private readonly Dictionary<int, IReadOnlyList<AttachmentLinkWithAttachment>> _manifests = new();

        public List<(string EntityType, int EntityId)> GetLinksRequests { get; } = new();

        public void SetManifest(int recordId, IReadOnlyList<AttachmentLinkWithAttachment> manifest)
        {
            _manifests[recordId] = manifest;
        }

        public Task<IReadOnlyList<AttachmentLinkWithAttachment>> GetLinksForEntityAsync(string entityType, int entityId, CancellationToken token = default)
        {
            GetLinksRequests.Add((entityType, entityId));
            if (_manifests.TryGetValue(entityId, out var manifest))
            {
                return Task.FromResult(manifest);
            }

            return Task.FromResult<IReadOnlyList<AttachmentLinkWithAttachment>>(Array.Empty<AttachmentLinkWithAttachment>());
        }

        public Task<AttachmentUploadResult> UploadAsync(Stream content, AttachmentUploadRequest request, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task<Attachment?> FindByHashAsync(string sha256, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task<Attachment?> FindByHashAndSizeAsync(string sha256, long fileSize, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task<AttachmentStreamResult> StreamContentAsync(int attachmentId, Stream destination, AttachmentReadRequest? request = null, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task RemoveLinkAsync(int linkId, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task RemoveLinkAsync(string entityType, int entityId, int attachmentId, CancellationToken token = default)
            => throw new NotSupportedException();
    }

    private sealed class RecordingSignatureDialogService : IElectronicSignatureDialogService
    {
        private readonly Queue<ElectronicSignatureDialogResult?> _captures = new();

        public List<ElectronicSignatureContext> CaptureContexts { get; } = new();
        public List<ElectronicSignatureDialogResult> PersistedResults { get; } = new();
        public List<ElectronicSignatureDialogResult> LoggedResults { get; } = new();
        public int PersistCallCount => PersistedResults.Count;
        public int LogCallCount => LoggedResults.Count;

        public void QueueCapture(ElectronicSignatureDialogResult? result)
        {
            _captures.Enqueue(result);
        }

        public Task<ElectronicSignatureDialogResult?> CaptureSignatureAsync(ElectronicSignatureContext context, CancellationToken cancellationToken = default)
        {
            CaptureContexts.Add(context);
            if (_captures.Count > 0)
            {
                return Task.FromResult(_captures.Dequeue());
            }

            return Task.FromResult<ElectronicSignatureDialogResult?>(null);
        }

        public Task PersistSignatureAsync(ElectronicSignatureDialogResult result, CancellationToken cancellationToken = default)
        {
            PersistedResults.Add(result);
            return Task.CompletedTask;
        }

        public Task LogPersistedSignatureAsync(ElectronicSignatureDialogResult result, CancellationToken cancellationToken = default)
        {
            LoggedResults.Add(result);
            return Task.CompletedTask;
        }
    }
}
