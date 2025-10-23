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

public sealed class SopGovernanceServiceAdapterTests
{
    [Fact]
    public async Task LoadAsync_PopulatesDocumentsAndHydratesAttachments()
    {
        var context = CreateContext();
        var withAttachments = CreateDocument(10, "SOP-010", "Calibration Process");
        var draft = CreateDocument(0, "SOP-000", "Draft");
        context.Database.LoadDocuments.AddRange(new[] { withAttachments, draft });

        var manifest = new[]
        {
            CreateAttachmentLink(1, "policy.pdf"),
            CreateAttachmentLink(2, "Checklist.docx")
        };
        context.Attachments.SetManifest(withAttachments.Id, manifest);

        var result = await context.Adapter.LoadAsync();

        Assert.True(result.Success);
        Assert.Equal("Loaded 2 SOP document(s).", result.Message);
        Assert.Equal(1, context.Database.LoadCallCount);
        var hydrated = Assert.Single(result.Documents, d => d.Id == withAttachments.Id);
        Assert.Equal(manifest.Select(m => m.Attachment.FileName), hydrated.Attachments);
        Assert.DoesNotContain(result.Documents, d => d.Id == draft.Id && d.Attachments?.Count > 0);

        var request = Assert.Single(context.Attachments.GetLinksRequests);
        Assert.Equal("sop_documents", request.EntityType);
        Assert.Equal(withAttachments.Id, request.EntityId);
    }

    [Fact]
    public async Task LoadAsync_WhenDatabaseThrows_ReturnsFailure()
    {
        var context = CreateContext();
        context.Database.LoadException = new InvalidOperationException("offline");

        var result = await context.Adapter.LoadAsync();

        Assert.False(result.Success);
        Assert.Contains("Failed to load SOP documents", result.Message, StringComparison.Ordinal);
        Assert.Contains("offline", result.Message, StringComparison.Ordinal);
        Assert.Empty(result.Documents);
        Assert.Empty(context.Attachments.GetLinksRequests);
    }

    [Fact]
    public async Task CreateAsync_Success_PersistsSignatureUploadsAttachmentsAndHydrates()
    {
        var context = CreateContext();
        context.Database.NextCreatedId = 55;
        var draft = CreateDocument(0, "SOP-055", "Record Handling");
        context.Signatures.QueueCapture(CreateSignatureResult("hash-create"));
        var manifest = new[] { CreateAttachmentLink(11, "RecordHandling.pdf") };
        context.Attachments.SetManifest(55, manifest);
        var uploads = new[]
        {
            CreateUpload("evidence.txt", "text/plain", "Evidence"),
            CreateUpload("diagram.png", "image/png", "Diagram")
        };

        var result = await context.Adapter.CreateAsync(draft, uploads);

        Assert.True(result.Success);
        Assert.Equal(55, result.DocumentId);
        Assert.Equal("Created SOP 'Record Handling' (ID=55).", result.Message);
        Assert.Equal(1, context.Database.CreateCalls.Count);
        var createCall = context.Database.CreateCalls[0];
        Assert.Equal("Record Handling", createCall.Document.Name);
        Assert.Equal(context.Auth.CurrentUser?.Id, createCall.ActorId);
        Assert.Equal(context.Auth.CurrentIpAddress, createCall.IpAddress);
        Assert.Equal(context.Auth.CurrentDeviceInfo, createCall.DeviceInfo);
        Assert.Equal(context.Auth.CurrentSessionId, createCall.SessionId);

        var capture = Assert.Single(context.Signatures.CaptureContexts);
        Assert.Equal("sop_documents", capture.EntityType);
        Assert.Equal(0, capture.RecordId);
        Assert.Equal(1, context.Database.InsertedSignatures.Count);
        var signature = context.Database.InsertedSignatures[0];
        Assert.Equal("sop_documents", signature.TableName);
        Assert.Equal(55, signature.RecordId);
        Assert.Equal(context.Auth.CurrentUser?.Id, signature.UserId);
        Assert.Equal("hash-create", signature.SignatureHash);
        Assert.Equal(context.Auth.CurrentIpAddress, signature.IpAddress);
        Assert.Equal(context.Auth.CurrentDeviceInfo, signature.DeviceInfo);
        Assert.Equal(context.Auth.CurrentSessionId, signature.SessionId);

        Assert.Equal(1, context.Signatures.LoggedResults.Count);
        Assert.Equal(2, context.AttachmentWorkflow.UploadCalls.Count);
        Assert.All(context.AttachmentWorkflow.UploadCalls, call =>
        {
            Assert.Equal("sop_documents", call.Request.EntityType);
            Assert.Equal(55, call.Request.EntityId);
            Assert.Equal(context.Auth.CurrentUser?.Id, call.Request.UploadedById);
            Assert.Equal(context.Auth.CurrentIpAddress, call.Request.SourceIp);
            Assert.Equal(context.Auth.CurrentDeviceInfo, call.Request.SourceHost);
        });

        Assert.Equal(55, draft.Id);
        Assert.Equal(manifest.Select(m => m.Attachment.FileName), draft.Attachments);
        var hydrate = Assert.Contains(context.Attachments.GetLinksRequests, r => r.EntityId == 55);
        Assert.Equal("sop_documents", hydrate.EntityType);
    }

    [Fact]
    public async Task CreateAsync_WhenSignatureCancelled_ReturnsFailureWithoutSideEffects()
    {
        var context = CreateContext();
        context.Signatures.QueueCapture(null);
        var draft = CreateDocument(0, "SOP-060", "Cancelled");

        var result = await context.Adapter.CreateAsync(draft);

        Assert.False(result.Success);
        Assert.Equal("Electronic signature cancelled.", result.Message);
        Assert.Null(result.DocumentId);
        Assert.Empty(context.Database.CreateCalls);
        Assert.Empty(context.Database.InsertedSignatures);
        Assert.Empty(context.Signatures.LoggedResults);
        Assert.Empty(context.AttachmentWorkflow.UploadCalls);
        Assert.Empty(context.Attachments.GetLinksRequests);
    }

    [Fact]
    public async Task CreateAsync_WhenDatabaseThrows_ReturnsFailure()
    {
        var context = CreateContext();
        context.Signatures.QueueCapture(CreateSignatureResult("hash-fail"));
        context.Database.CreateException = new InvalidOperationException("constraint");
        var draft = CreateDocument(0, "SOP-061", "Failure");

        var result = await context.Adapter.CreateAsync(draft);

        Assert.False(result.Success);
        Assert.Contains("Failed to create SOP", result.Message, StringComparison.Ordinal);
        Assert.Contains("constraint", result.Message, StringComparison.Ordinal);
        Assert.Null(result.DocumentId);
        Assert.Empty(context.AttachmentWorkflow.UploadCalls);
        Assert.Empty(context.Attachments.GetLinksRequests);
    }

    [Fact]
    public async Task CreateAsync_WhenAttachmentUploadThrows_ReturnsFailure()
    {
        var context = CreateContext();
        context.Database.NextCreatedId = 99;
        context.Signatures.QueueCapture(CreateSignatureResult("hash-upload"));
        context.AttachmentWorkflow.UploadException = new InvalidOperationException("upload failed");
        var draft = CreateDocument(0, "SOP-099", "Upload Failure");
        var uploads = new[] { CreateUpload("file.bin", "application/octet-stream", "Bin") };

        var result = await context.Adapter.CreateAsync(draft, uploads);

        Assert.False(result.Success);
        Assert.Contains("Failed to create SOP", result.Message, StringComparison.Ordinal);
        Assert.Contains("upload failed", result.Message, StringComparison.Ordinal);
        Assert.Empty(context.Attachments.GetLinksRequests);
    }

    [Fact]
    public async Task UpdateAsync_Success_PersistsSignatureUploadsAttachmentsAndHydrates()
    {
        var context = CreateContext();
        var document = CreateDocument(42, "SOP-042", "Update Procedure");
        context.Signatures.QueueCapture(CreateSignatureResult("hash-update"));
        var manifest = new[] { CreateAttachmentLink(21, "Update.pdf") };
        context.Attachments.SetManifest(42, manifest);
        var uploads = new[] { CreateUpload("evidence.pdf", "application/pdf", "Evidence") };

        var result = await context.Adapter.UpdateAsync(document, uploads);

        Assert.True(result.Success);
        Assert.Equal(42, result.DocumentId);
        Assert.Equal("Updated SOP 'Update Procedure'.", result.Message);
        var updateCall = Assert.Single(context.Database.UpdateCalls);
        Assert.Equal(42, updateCall.Document.Id);
        Assert.Equal(context.Auth.CurrentUser?.Id, updateCall.ActorId);
        Assert.Equal(context.Auth.CurrentIpAddress, updateCall.IpAddress);
        Assert.Equal(context.Auth.CurrentDeviceInfo, updateCall.DeviceInfo);
        Assert.Equal(context.Auth.CurrentSessionId, updateCall.SessionId);

        var capture = Assert.Single(context.Signatures.CaptureContexts);
        Assert.Equal(42, capture.RecordId);
        Assert.Equal("sop_documents", capture.EntityType);
        var inserted = Assert.Single(context.Database.InsertedSignatures);
        Assert.Equal(42, inserted.RecordId);
        Assert.Equal("hash-update", inserted.SignatureHash);
        Assert.Equal(context.Auth.CurrentUser?.Id, inserted.UserId);

        var upload = Assert.Single(context.AttachmentWorkflow.UploadCalls);
        Assert.Equal(42, upload.Request.EntityId);
        Assert.Equal(context.Auth.CurrentUser?.Id, upload.Request.UploadedById);
        Assert.Equal(context.Auth.CurrentIpAddress, upload.Request.SourceIp);
        Assert.Equal(context.Auth.CurrentDeviceInfo, upload.Request.SourceHost);

        Assert.Equal(manifest.Select(m => m.Attachment.FileName), document.Attachments);
        var hydration = Assert.Single(context.Attachments.GetLinksRequests);
        Assert.Equal(42, hydration.EntityId);
    }

    [Fact]
    public async Task UpdateAsync_WhenDocumentIdMissing_ReturnsFailure()
    {
        var context = CreateContext();
        var document = CreateDocument(0, "SOP-000", "Invalid");

        var result = await context.Adapter.UpdateAsync(document);

        Assert.False(result.Success);
        Assert.Equal("Select a SOP document before updating.", result.Message);
        Assert.Null(result.DocumentId);
        Assert.Empty(context.Database.UpdateCalls);
        Assert.Empty(context.Signatures.CaptureContexts);
        Assert.Empty(context.AttachmentWorkflow.UploadCalls);
        Assert.Empty(context.Attachments.GetLinksRequests);
    }

    [Fact]
    public async Task UpdateAsync_WhenSignatureCancelled_ReturnsFailureWithoutSideEffects()
    {
        var context = CreateContext();
        context.Signatures.QueueCapture(null);
        var document = CreateDocument(7, "SOP-007", "Cancelled Update");

        var result = await context.Adapter.UpdateAsync(document);

        Assert.False(result.Success);
        Assert.Equal("Electronic signature cancelled.", result.Message);
        Assert.Equal(7, result.DocumentId);
        Assert.Empty(context.Database.UpdateCalls);
        Assert.Empty(context.Database.InsertedSignatures);
        Assert.Empty(context.Signatures.LoggedResults);
        Assert.Empty(context.AttachmentWorkflow.UploadCalls);
        Assert.Empty(context.Attachments.GetLinksRequests);
    }

    [Fact]
    public async Task UpdateAsync_WhenDatabaseThrows_ReturnsFailure()
    {
        var context = CreateContext();
        context.Signatures.QueueCapture(CreateSignatureResult("hash-error"));
        context.Database.UpdateException = new InvalidOperationException("conflict");
        var document = CreateDocument(8, "SOP-008", "Conflict");

        var result = await context.Adapter.UpdateAsync(document);

        Assert.False(result.Success);
        Assert.Contains("Failed to update SOP", result.Message, StringComparison.Ordinal);
        Assert.Contains("conflict", result.Message, StringComparison.Ordinal);
        Assert.Equal(8, result.DocumentId);
        Assert.Empty(context.AttachmentWorkflow.UploadCalls);
        Assert.Empty(context.Attachments.GetLinksRequests);
    }

    [Fact]
    public async Task DeleteAsync_Success_PersistsSignatureRemovesAttachments()
    {
        var context = CreateContext();
        context.Signatures.QueueCapture(CreateSignatureResult("hash-delete"));
        var document = CreateDocument(30, "SOP-030", "Retire");
        var manifest = new[]
        {
            CreateAttachmentLink(90, "old.pdf"),
            CreateAttachmentLink(91, "draft.docx")
        };
        context.Attachments.SetManifest(30, manifest);

        var result = await context.Adapter.DeleteAsync(document, "Retirement");

        Assert.True(result.Success);
        Assert.Equal(30, result.DocumentId);
        Assert.Equal("Deleted SOP 'Retire' (ID=30).", result.Message);
        var delete = Assert.Single(context.Database.DeleteCalls);
        Assert.Equal(30, delete.DocumentId);
        Assert.Equal(context.Auth.CurrentUser?.Id, delete.ActorId);
        Assert.Equal(context.Auth.CurrentIpAddress, delete.IpAddress);
        Assert.Equal(context.Auth.CurrentDeviceInfo, delete.DeviceInfo);
        Assert.Equal(context.Auth.CurrentSessionId, delete.SessionId);

        var capture = Assert.Single(context.Signatures.CaptureContexts);
        Assert.Equal(30, capture.RecordId);
        var inserted = Assert.Single(context.Database.InsertedSignatures);
        Assert.Equal(30, inserted.RecordId);
        Assert.Equal("hash-delete", inserted.SignatureHash);
        Assert.Equal(context.Auth.CurrentUser?.Id, inserted.UserId);
        Assert.Equal(2, context.Attachments.RemovedLinkIds.Count);
        Assert.Equal(manifest.Select(m => m.Link.Id), context.Attachments.RemovedLinkIds);
    }

    [Fact]
    public async Task DeleteAsync_WhenDocumentIdMissing_ReturnsFailure()
    {
        var context = CreateContext();
        var document = CreateDocument(0, "SOP-000", "Invalid");

        var result = await context.Adapter.DeleteAsync(document);

        Assert.False(result.Success);
        Assert.Equal("Select a SOP document before deleting.", result.Message);
        Assert.Null(result.DocumentId);
        Assert.Empty(context.Database.DeleteCalls);
        Assert.Empty(context.Signatures.CaptureContexts);
    }

    [Fact]
    public async Task DeleteAsync_WhenSignatureCancelled_ReturnsFailureWithoutSideEffects()
    {
        var context = CreateContext();
        context.Signatures.QueueCapture(null);
        var document = CreateDocument(16, "SOP-016", "Cancel");

        var result = await context.Adapter.DeleteAsync(document, "Cancel");

        Assert.False(result.Success);
        Assert.Equal("Electronic signature cancelled.", result.Message);
        Assert.Equal(16, result.DocumentId);
        Assert.Empty(context.Database.DeleteCalls);
        Assert.Empty(context.Database.InsertedSignatures);
        Assert.Empty(context.Signatures.LoggedResults);
        Assert.Empty(context.Attachments.RemovedLinkIds);
    }

    [Fact]
    public async Task DeleteAsync_WhenDatabaseThrows_ReturnsFailure()
    {
        var context = CreateContext();
        context.Signatures.QueueCapture(CreateSignatureResult("hash-db"));
        context.Database.DeleteException = new InvalidOperationException("lock");
        var document = CreateDocument(44, "SOP-044", "Locked");

        var result = await context.Adapter.DeleteAsync(document, "Lock");

        Assert.False(result.Success);
        Assert.Contains("Failed to delete SOP", result.Message, StringComparison.Ordinal);
        Assert.Contains("lock", result.Message, StringComparison.Ordinal);
        Assert.Equal(44, result.DocumentId);
        Assert.Empty(context.Attachments.RemovedLinkIds);
    }

    [Fact]
    public async Task DeleteAsync_WhenAttachmentRemovalThrows_ReturnsFailure()
    {
        var context = CreateContext();
        context.Signatures.QueueCapture(CreateSignatureResult("hash-remove"));
        var document = CreateDocument(12, "SOP-012", "Remove Failure");
        var manifest = new[] { CreateAttachmentLink(70, "doc.pdf") };
        context.Attachments.SetManifest(12, manifest);
        context.Attachments.RemoveException = new InvalidOperationException("removal error");

        var result = await context.Adapter.DeleteAsync(document, "Remove");

        Assert.False(result.Success);
        Assert.Contains("Failed to delete SOP", result.Message, StringComparison.Ordinal);
        Assert.Contains("removal error", result.Message, StringComparison.Ordinal);
        Assert.Empty(context.Attachments.RemovedLinkIds);
    }

    [Fact]
    public async Task ExportAsync_Success_ReturnsPathAndMessage()
    {
        var context = CreateContext();
        context.Database.ExportPath = "C:/exports/sops.xlsx";
        var documents = new List<SopDocument>
        {
            CreateDocument(1, "SOP-001", "Doc 1"),
            CreateDocument(2, "SOP-002", "Doc 2")
        };

        var result = await context.Adapter.ExportAsync(documents, "xlsx");

        Assert.True(result.Success);
        Assert.Equal("Export completed (xlsx).", result.Message);
        Assert.Equal("C:/exports/sops.xlsx", result.FilePath);
        var exportCall = Assert.Single(context.Database.ExportCalls);
        Assert.Equal(2, exportCall.Documents.Count);
        Assert.Equal("xlsx", exportCall.Format);
        Assert.Equal(context.Auth.CurrentUser?.Id, exportCall.ActorId);
        Assert.Equal(context.Auth.CurrentIpAddress, exportCall.IpAddress);
        Assert.Equal(context.Auth.CurrentDeviceInfo, exportCall.DeviceInfo);
        Assert.Equal(context.Auth.CurrentSessionId, exportCall.SessionId);
    }

    [Fact]
    public async Task ExportAsync_WhenFormatMissing_ReturnsFailure()
    {
        var context = CreateContext();
        var documents = new List<SopDocument> { CreateDocument(1, "SOP-001", "Doc 1") };

        var result = await context.Adapter.ExportAsync(documents, string.Empty);

        Assert.False(result.Success);
        Assert.Equal("Select an export format.", result.Message);
        Assert.Null(result.FilePath);
        Assert.Empty(context.Database.ExportCalls);
    }

    [Fact]
    public async Task ExportAsync_WhenDatabaseThrows_ReturnsFailure()
    {
        var context = CreateContext();
        context.Database.ExportException = new InvalidOperationException("disk full");
        var documents = new List<SopDocument> { CreateDocument(5, "SOP-005", "Doc 5") };

        var result = await context.Adapter.ExportAsync(documents, "pdf");

        Assert.False(result.Success);
        Assert.Contains("Failed to export SOPs", result.Message, StringComparison.Ordinal);
        Assert.Contains("disk full", result.Message, StringComparison.Ordinal);
        Assert.Null(result.FilePath);
    }

    private static TestContext CreateContext() => new();

    private static SopDocument CreateDocument(int id, string code, string name)
    {
        return new SopDocument
        {
            Id = id,
            Code = code,
            Name = name,
            FilePath = $"{code}.pdf",
            DateIssued = DateTime.UtcNow,
            Attachments = new List<string>()
        };
    }

    private static ElectronicSignatureDialogResult CreateSignatureResult(string hash)
    {
        return new ElectronicSignatureDialogResult(
            "password",
            "SOP",
            "Approval",
            "Approve SOP",
            new DigitalSignature
            {
                SignatureHash = hash,
                Status = "valid",
                Method = "password",
                SignedAt = DateTime.UtcNow
            });
    }

    private static SopAttachmentUpload CreateUpload(string fileName, string contentType, string displayName)
    {
        return new SopAttachmentUpload(
            fileName,
            contentType,
            token => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes($"{fileName}-content"))),
            displayName,
            "Reason");
    }

    private static AttachmentLinkWithAttachment CreateAttachmentLink(int attachmentId, string fileName)
    {
        var attachment = new Attachment
        {
            Id = attachmentId,
            FileName = fileName,
            Name = fileName
        };
        var link = new AttachmentLink
        {
            Id = attachmentId + 1000,
            AttachmentId = attachmentId,
            Attachment = attachment,
            EntityType = "sop_documents"
        };
        return new AttachmentLinkWithAttachment(link, attachment);
    }

    private sealed class TestContext
    {
        public TestContext()
        {
            Auth = new RecordingAuthContext
            {
                CurrentUser = new User { Id = 77, Username = "tester", FullName = "Test User" },
                CurrentDeviceInfo = "QA-Workstation",
                CurrentIpAddress = "10.0.0.5",
                CurrentSessionId = "session-123"
            };
            Database = new RecordingDatabaseService();
            AttachmentWorkflow = new RecordingAttachmentWorkflowService();
            Attachments = new RecordingAttachmentService();
            Signatures = new RecordingSignatureDialogService();
            Adapter = new SopGovernanceServiceAdapter(Database, Auth, AttachmentWorkflow, Attachments, Signatures);
        }

        public RecordingDatabaseService Database { get; }
        public RecordingAuthContext Auth { get; }
        public RecordingAttachmentWorkflowService AttachmentWorkflow { get; }
        public RecordingAttachmentService Attachments { get; }
        public RecordingSignatureDialogService Signatures { get; }
        public SopGovernanceServiceAdapter Adapter { get; }
    }

    private sealed class RecordingDatabaseService : DatabaseService
    {
        private int _nextSignatureId = 1;

        public RecordingDatabaseService()
            : base("Server=localhost;Database=unit;Uid=test;Pwd=test;")
        {
        }

        public List<SopDocument> LoadDocuments { get; } = new();
        public int LoadCallCount { get; private set; }
        public Exception? LoadException { get; set; }
        public List<(SopDocument Document, int ActorId, string? IpAddress, string? DeviceInfo, string? SessionId)> CreateCalls { get; } = new();
        public List<(SopDocument Document, int ActorId, string? IpAddress, string? DeviceInfo, string? SessionId)> UpdateCalls { get; } = new();
        public List<(int DocumentId, int ActorId, string? IpAddress, string? DeviceInfo, string? SessionId)> DeleteCalls { get; } = new();
        public List<DigitalSignature> InsertedSignatures { get; } = new();
        public List<(List<SopDocument> Documents, string Format, int ActorId, string? IpAddress, string? DeviceInfo, string? SessionId)> ExportCalls { get; } = new();
        public int NextCreatedId { get; set; } = 100;
        public string ExportPath { get; set; } = "C:/exports/sop.zip";
        public Exception? CreateException { get; set; }
        public Exception? UpdateException { get; set; }
        public Exception? DeleteException { get; set; }
        public Exception? InsertSignatureException { get; set; }
        public Exception? ExportException { get; set; }

        public Task<IReadOnlyList<SopDocument>> GetSopDocumentsAsync(CancellationToken cancellationToken = default)
        {
            LoadCallCount++;
            if (LoadException is not null)
            {
                throw LoadException;
            }

            var clones = LoadDocuments.Select(CloneDocument).ToList();
            return Task.FromResult<IReadOnlyList<SopDocument>>(clones);
        }

        public Task<int> CreateSopDocumentAsync(
            SopDocument document,
            int actorId,
            string ipAddress,
            string deviceInfo,
            string? sessionId,
            CancellationToken cancellationToken = default)
        {
            if (CreateException is not null)
            {
                throw CreateException;
            }

            CreateCalls.Add((CloneDocument(document), actorId, ipAddress, deviceInfo, sessionId));
            return Task.FromResult(NextCreatedId);
        }

        public Task UpdateSopDocumentAsync(
            SopDocument document,
            int actorId,
            string ipAddress,
            string deviceInfo,
            string? sessionId,
            CancellationToken cancellationToken = default)
        {
            if (UpdateException is not null)
            {
                throw UpdateException;
            }

            UpdateCalls.Add((CloneDocument(document), actorId, ipAddress, deviceInfo, sessionId));
            return Task.CompletedTask;
        }

        public Task DeleteSopDocumentAsync(
            int documentId,
            int actorId,
            string ipAddress,
            string deviceInfo,
            string? sessionId,
            CancellationToken cancellationToken = default)
        {
            if (DeleteException is not null)
            {
                throw DeleteException;
            }

            DeleteCalls.Add((documentId, actorId, ipAddress, deviceInfo, sessionId));
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

            if (signature.Id <= 0)
            {
                signature.Id = _nextSignatureId++;
            }

            InsertedSignatures.Add(CloneSignature(signature));
            return Task.FromResult(signature.Id);
        }

        public Task<string> ExportDocumentsAsync(
            List<SopDocument> documents,
            string format,
            int actorId,
            string ipAddress,
            string deviceInfo,
            string? sessionId,
            CancellationToken cancellationToken = default)
        {
            if (ExportException is not null)
            {
                throw ExportException;
            }

            ExportCalls.Add((documents.Select(CloneDocument).ToList(), format, actorId, ipAddress, deviceInfo, sessionId));
            return Task.FromResult(ExportPath);
        }

        private static SopDocument CloneDocument(SopDocument source)
        {
            return source.DeepCopy();
        }

        private static DigitalSignature CloneSignature(DigitalSignature signature)
        {
            return new DigitalSignature
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
        public Exception? UploadException { get; set; }
        public bool IsEncryptionEnabled => false;
        public string EncryptionKeyId => string.Empty;

        public Task<AttachmentWorkflowUploadResult> UploadAsync(Stream content, AttachmentUploadRequest request, CancellationToken token = default)
        {
            if (UploadException is not null)
            {
                throw UploadException;
            }

            using var buffer = new MemoryStream();
            content.CopyTo(buffer);
            UploadCalls.Add((buffer.ToArray(), CloneRequest(request)));
            var upload = new AttachmentUploadResult(
                new Attachment { Id = 1, FileName = request.FileName, Name = request.DisplayName ?? request.FileName },
                new AttachmentLink { Id = 1, AttachmentId = 1, EntityType = request.EntityType, EntityId = request.EntityId },
                new RetentionPolicy());
            return Task.FromResult(new AttachmentWorkflowUploadResult(upload, string.Empty, buffer.Length, false, null));
        }
    }

    private sealed class RecordingAttachmentService : IAttachmentService
    {
        private readonly Dictionary<int, IReadOnlyList<AttachmentLinkWithAttachment>> _manifests = new();

        public List<(string EntityType, int EntityId)> GetLinksRequests { get; } = new();
        public List<int> RemovedLinkIds { get; } = new();
        public Exception? RemoveException { get; set; }

        public void SetManifest(int documentId, IReadOnlyList<AttachmentLinkWithAttachment> manifest)
        {
            _manifests[documentId] = manifest;
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

        public Task RemoveLinkAsync(int linkId, CancellationToken token = default)
        {
            if (RemoveException is not null)
            {
                throw RemoveException;
            }

            RemovedLinkIds.Add(linkId);
            return Task.CompletedTask;
        }

        public Task RemoveLinkAsync(string entityType, int entityId, int attachmentId, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task<AttachmentUploadResult> UploadAsync(Stream content, AttachmentUploadRequest request, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task<Attachment?> FindByHashAsync(string sha256, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task<Attachment?> FindByHashAndSizeAsync(string sha256, long fileSize, CancellationToken token = default)
            => throw new NotSupportedException();

        public Task<AttachmentStreamResult> StreamContentAsync(int attachmentId, Stream destination, AttachmentReadRequest? request = null, CancellationToken token = default)
            => throw new NotSupportedException();
    }

    private sealed class RecordingSignatureDialogService : IElectronicSignatureDialogService
    {
        private readonly Queue<ElectronicSignatureDialogResult?> _captures = new();

        public List<ElectronicSignatureContext> CaptureContexts { get; } = new();
        public List<ElectronicSignatureDialogResult> LoggedResults { get; } = new();

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
            => Task.CompletedTask;

        public Task LogPersistedSignatureAsync(ElectronicSignatureDialogResult result, CancellationToken cancellationToken = default)
        {
            LoggedResults.Add(result);
            return Task.CompletedTask;
        }
    }

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
