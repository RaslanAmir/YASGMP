using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MySqlConnector;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class AttachmentsModuleViewModelTests
{
    [Fact]
    public async Task ModeTransitions_ClearSelectionAndUpdateStatus()
    {
        // Arrange
        var attachments = new List<Attachment>
        {
            new()
            {
                Id = 5,
                FileName = "existing.pdf",
                EntityType = "work_orders",
                EntityId = 9,
                Notes = "Calibration",
                FileType = "pdf",
                Status = "Approved",
                UploadedAt = DateTime.UtcNow.AddDays(-1)
            }
        };
        var nonQueryLog = new List<string>();
        var database = CreateDatabaseService(attachments, nonQueryLog);
        var attachmentService = new TestAttachmentService();
        var filePicker = new TestFilePicker();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var audit = new RecordingAuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new AttachmentsModuleViewModel(database, attachmentService, filePicker, signatureDialog, audit, cfl, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        viewModel.SelectedAttachment = viewModel.AttachmentRows.First();
        viewModel.StagedUploads.Add(new AttachmentsModuleViewModel.StagedAttachmentUploadViewModel { FileName = "pending.txt" });

        // Act
        viewModel.Mode = FormMode.Add;

        // Assert
        Assert.Equal("Stage attachments, capture a signature, then save to commit.", viewModel.StatusMessage);
        Assert.Null(viewModel.SelectedAttachment);
        Assert.Null(viewModel.SelectedRecord);
        Assert.False(viewModel.HasStagedUploads);
        Assert.Empty(viewModel.StagedUploads);

        // Act 2
        viewModel.Mode = FormMode.Update;

        // Assert 2
        Assert.Equal("Stage additional files or remove attachments, then save to commit.", viewModel.StatusMessage);

        // Act 3
        viewModel.Mode = FormMode.Find;

        // Assert 3
        Assert.Equal("Enter search text to find attachments.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task UploadAsync_StagesFilesAndSkipsDuplicates()
    {
        // Arrange
        var attachments = new List<Attachment>
        {
            new()
            {
                Id = 7,
                FileName = "existing.bin",
                EntityType = "work_orders",
                EntityId = 22,
                Notes = "Historical",
                UploadedAt = DateTime.UtcNow.AddDays(-7)
            }
        };
        var nonQueryLog = new List<string>();
        var database = CreateDatabaseService(attachments, nonQueryLog);
        var innerAttachmentService = new TestAttachmentService();
        var dedupeService = new ConfigurableAttachmentService(innerAttachmentService);
        var filePicker = new TestFilePicker();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var audit = new RecordingAuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new AttachmentsModuleViewModel(database, dedupeService, filePicker, signatureDialog, audit, cfl, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        viewModel.SelectedAttachment = viewModel.AttachmentRows.First();
        viewModel.Mode = FormMode.Add;

        var firstContent = new byte[] { 1, 2, 3, 4 };
        var duplicateContent = new byte[] { 4, 3, 2, 1 };
        var duplicateHash = Convert.ToHexString(SHA256.HashData(duplicateContent));
        dedupeService.SetDuplicate(duplicateHash, duplicateContent.LongLength, new Attachment
        {
            Id = 99,
            FileName = "duplicate.bin",
            EntityType = "work_orders",
            EntityId = 22
        });

        filePicker.Files = new[]
        {
            CreatePickedFile("first.txt", firstContent),
            CreatePickedFile("second.txt", duplicateContent)
        };

        // Act
        await viewModel.UploadCommand.ExecuteAsync(null);

        // Assert
        Assert.Single(viewModel.StagedUploads);
        var staged = viewModel.StagedUploads.First();
        Assert.Equal("first.txt", staged.FileName);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(firstContent)), staged.Sha256);
        Assert.True(viewModel.HasStagedUploads);
        Assert.Contains("Staged 1 file(s)", viewModel.StatusMessage);
        Assert.Contains("skipped 1 duplicate(s)", viewModel.StatusMessage);
        Assert.Contains("Use Save to commit staged uploads.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task OnSaveAsync_CommitsStagedUploadsWithSignatureAndAudit()
    {
        // Arrange
        var attachments = new List<Attachment>();
        var nonQueryLog = new List<string>();
        var database = CreateDatabaseService(attachments, nonQueryLog);
        var attachmentService = new TestAttachmentService();
        var filePicker = new TestFilePicker();
        var signatureDialog = new TestElectronicSignatureDialogService();
        signatureDialog.QueueResult(signatureDialog.DefaultResult);
        var audit = new RecordingAuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new AttachmentsModuleViewModel(database, attachmentService, filePicker, signatureDialog, audit, cfl, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        filePicker.Files = new[] { CreatePickedFile("commit.pdf", new byte[] { 9, 9, 9 }) };

        await viewModel.UploadCommand.ExecuteAsync(null);

        // Act
        var saved = await InvokeSaveAsync(viewModel);

        // Assert
        Assert.True(saved);
        Assert.Equal("Committed 1 attachment(s). Use Save to commit staged uploads.", viewModel.StatusMessage);
        Assert.False(viewModel.HasStagedUploads);
        Assert.Empty(viewModel.StagedUploads);

        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("attachments", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        Assert.Single(signatureDialog.CapturedResults);
        Assert.True(signatureDialog.WasPersistInvoked);
        Assert.Single(signatureDialog.PersistedResults);

        var uploadRequest = Assert.Single(attachmentService.Uploads);
        Assert.Equal("attachments", uploadRequest.EntityType);
        Assert.Equal(0, uploadRequest.EntityId);
        Assert.Equal("commit.pdf", uploadRequest.FileName);

        var auditEntry = Assert.Single(audit.EntityAudits);
        Assert.Equal("attachments", auditEntry.Table);
        Assert.Equal("UPLOAD", auditEntry.Action);
        Assert.Contains("file=commit.pdf", auditEntry.Details);
    }

    [Fact]
    public async Task CancelCommand_DiscardsStagedUploadsAndResetsStatus()
    {
        // Arrange
        var attachments = new List<Attachment>();
        var nonQueryLog = new List<string>();
        var database = CreateDatabaseService(attachments, nonQueryLog);
        var attachmentService = new TestAttachmentService();
        var filePicker = new TestFilePicker();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var audit = new RecordingAuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new AttachmentsModuleViewModel(database, attachmentService, filePicker, signatureDialog, audit, cfl, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        filePicker.Files = new[]
        {
            CreatePickedFile("first.bin", new byte[] { 1, 1, 1 }),
            CreatePickedFile("second.bin", new byte[] { 2, 2, 2 })
        };

        await viewModel.UploadCommand.ExecuteAsync(null);

        // Act
        viewModel.CancelCommand.Execute(null);

        // Assert
        Assert.False(viewModel.HasStagedUploads);
        Assert.Empty(viewModel.StagedUploads);
        Assert.Equal("Attachments changes cancelled.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task DeleteCommand_RemovesAttachmentAndRefreshes()
    {
        // Arrange
        var attachments = new List<Attachment>
        {
            new()
            {
                Id = 17,
                FileName = "obsolete.txt",
                EntityType = "incidents",
                EntityId = 14,
                Notes = "Old",
                UploadedAt = DateTime.UtcNow
            }
        };
        var nonQueryLog = new List<string>();
        var database = CreateDatabaseService(attachments, nonQueryLog);
        var attachmentService = new TestAttachmentService();
        var filePicker = new TestFilePicker();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var audit = new RecordingAuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new AttachmentsModuleViewModel(database, attachmentService, filePicker, signatureDialog, audit, cfl, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        viewModel.SelectedAttachment = viewModel.AttachmentRows.First();
        viewModel.Mode = FormMode.Update;

        // Act
        await viewModel.DeleteCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(nonQueryLog, sql => sql.Contains("DELETE FROM attachments", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Attachment 'obsolete.txt' deleted.", viewModel.StatusMessage);
        Assert.Empty(viewModel.Records);
        Assert.Empty(viewModel.AttachmentRows);
    }

    [Fact]
    public async Task DownloadCommand_EnablementReflectsSelectionAndBusyState()
    {
        // Arrange
        var attachments = new List<Attachment>
        {
            new()
            {
                Id = 3,
                FileName = "report.docx",
                EntityType = "capa_cases",
                EntityId = 2,
                UploadedAt = DateTime.UtcNow
            }
        };
        var database = CreateDatabaseService(attachments, new List<string>());
        var attachmentService = new TestAttachmentService();
        var filePicker = new TestFilePicker();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var audit = new RecordingAuditService(database);
        var cfl = new StubCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new StubModuleNavigationService();

        var viewModel = new AttachmentsModuleViewModel(database, attachmentService, filePicker, signatureDialog, audit, cfl, shell, navigation);
        await viewModel.InitializeAsync(null);

        // Act / Assert
        Assert.False(viewModel.DownloadCommand.CanExecute(null));

        viewModel.SelectedRecord = viewModel.Records.First();
        viewModel.SelectedAttachment = viewModel.AttachmentRows.First();
        viewModel.Mode = FormMode.View;

        Assert.True(viewModel.DownloadCommand.CanExecute(null));

        viewModel.Mode = FormMode.Add;
        Assert.False(viewModel.DownloadCommand.CanExecute(null));

        viewModel.Mode = FormMode.View;
        viewModel.IsBusy = true;
        Assert.False(viewModel.DownloadCommand.CanExecute(null));
    }

    private static PickedFile CreatePickedFile(string name, byte[] content)
        => new(name, "application/octet-stream", () => Task.FromResult<Stream>(new MemoryStream(content, writable: false)), content.LongLength);

    private static Task<bool> InvokeSaveAsync(AttachmentsModuleViewModel viewModel)
    {
        var method = typeof(AttachmentsModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(AttachmentsModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }

    private static DatabaseService CreateDatabaseService(List<Attachment> attachments, List<string> nonQueryLog)
    {
        var db = new DatabaseService("Server=localhost;Database=unit_test;Uid=test;Pwd=test;");
        SetExecuteSelectOverride(db, async (sql, parameters, token) =>
        {
            await Task.Yield();
            if (sql.IndexOf("FROM attachments", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return CreateAttachmentDataTable(attachments);
            }

            return new DataTable();
        });

        SetExecuteNonQueryOverride(db, async (sql, parameters, token) =>
        {
            await Task.Yield();
            nonQueryLog.Add(sql);

            if (sql.IndexOf("DELETE FROM attachments", StringComparison.OrdinalIgnoreCase) >= 0 && parameters is not null)
            {
                var idParam = parameters.FirstOrDefault(p => string.Equals(p.ParameterName, "@id", StringComparison.OrdinalIgnoreCase));
                if (idParam is not null && idParam.Value is not null)
                {
                    var id = Convert.ToInt32(idParam.Value);
                    attachments.RemoveAll(a => a.Id == id);
                }
            }

            return 1;
        });

        return db;
    }

    private static void SetExecuteSelectOverride(DatabaseService database, Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<DataTable>> factory)
    {
        var property = typeof(DatabaseService)
            .GetProperty("ExecuteSelectOverride", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(nameof(DatabaseService), "ExecuteSelectOverride");
        property.SetValue(database, factory);
    }

    private static void SetExecuteNonQueryOverride(DatabaseService database, Func<string, IEnumerable<MySqlParameter>?, CancellationToken, Task<int>> factory)
    {
        var property = typeof(DatabaseService)
            .GetProperty("ExecuteNonQueryOverride", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMemberException(nameof(DatabaseService), "ExecuteNonQueryOverride");
        property.SetValue(database, factory);
    }

    private static DataTable CreateAttachmentDataTable(IEnumerable<Attachment> attachments)
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("file_name", typeof(string));
        table.Columns.Add("entity_type", typeof(string));
        table.Columns.Add("entity_id", typeof(int));
        table.Columns.Add("file_type", typeof(string));
        table.Columns.Add("notes", typeof(string));
        table.Columns.Add("uploaded_by_id", typeof(int));
        table.Columns.Add("uploaded_at", typeof(DateTime));
        table.Columns.Add("file_size", typeof(long));
        table.Columns.Add("sha256", typeof(string));
        table.Columns.Add("status", typeof(string));
        table.Columns.Add("retention_policy_name", typeof(string));
        table.Columns.Add("retention_retain_until", typeof(DateTime));
        table.Columns.Add("retention_legal_hold", typeof(bool));
        table.Columns.Add("retention_review_required", typeof(bool));
        table.Columns.Add("retention_notes", typeof(string));

        foreach (var attachment in attachments)
        {
            var row = table.NewRow();
            row["id"] = attachment.Id;
            row["file_name"] = attachment.FileName ?? string.Empty;
            row["entity_type"] = attachment.EntityType ?? string.Empty;
            row["entity_id"] = attachment.EntityId ?? 0;
            row["file_type"] = attachment.FileType ?? string.Empty;
            row["notes"] = attachment.Notes ?? string.Empty;
            row["uploaded_by_id"] = attachment.UploadedById ?? 0;
            row["uploaded_at"] = attachment.UploadedAt == default ? DateTime.UtcNow : attachment.UploadedAt;
            row["file_size"] = attachment.FileSize ?? 0L;
            row["sha256"] = attachment.Sha256 ?? string.Empty;
            row["status"] = attachment.Status ?? string.Empty;
            row["retention_policy_name"] = attachment.RetentionPolicyName ?? string.Empty;
            row["retention_retain_until"] = attachment.RetainUntil ?? DateTime.UtcNow;
            row["retention_legal_hold"] = attachment.RetentionLegalHold;
            row["retention_review_required"] = attachment.RetentionReviewRequired;
            row["retention_notes"] = attachment.RetentionNotes ?? string.Empty;
            table.Rows.Add(row);
        }

        return table;
    }

    private sealed class ConfigurableAttachmentService : IAttachmentService
    {
        private readonly TestAttachmentService _inner;
        private readonly Dictionary<(string Hash, long Size), Attachment> _duplicates = new();

        public ConfigurableAttachmentService(TestAttachmentService inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public void SetDuplicate(string hash, long size, Attachment attachment)
        {
            _duplicates[(hash, size)] = attachment;
        }

        public Task<AttachmentUploadResult> UploadAsync(Stream content, AttachmentUploadRequest request, CancellationToken token = default)
            => _inner.UploadAsync(content, request, token);

        public Task<Attachment?> FindByHashAsync(string sha256, CancellationToken token = default)
            => Task.FromResult<Attachment?>(null);

        public Task<Attachment?> FindByHashAndSizeAsync(string sha256, long fileSize, CancellationToken token = default)
        {
            return Task.FromResult(_duplicates.TryGetValue((sha256, fileSize), out var attachment) ? attachment : null);
        }

        public Task<AttachmentStreamResult> StreamContentAsync(int attachmentId, Stream destination, AttachmentReadRequest? request = null, CancellationToken token = default)
            => _inner.StreamContentAsync(attachmentId, destination, request, token);

        public Task<IReadOnlyList<AttachmentLinkWithAttachment>> GetLinksForEntityAsync(string entityType, int entityId, CancellationToken token = default)
            => _inner.GetLinksForEntityAsync(entityType, entityId, token);

        public Task RemoveLinkAsync(int linkId, CancellationToken token = default)
            => _inner.RemoveLinkAsync(linkId, token);

        public Task RemoveLinkAsync(string entityType, int entityId, int attachmentId, CancellationToken token = default)
            => _inner.RemoveLinkAsync(entityType, entityId, attachmentId, token);
    }

    private sealed class TestShellInteractionService : IShellInteractionService
    {
        public List<InspectorContext> InspectorUpdates { get; } = new();
        public List<string> StatusUpdates { get; } = new();

        public void UpdateInspector(InspectorContext context) => InspectorUpdates.Add(context);

        public void UpdateStatus(string message) => StatusUpdates.Add(message);
    }
}
