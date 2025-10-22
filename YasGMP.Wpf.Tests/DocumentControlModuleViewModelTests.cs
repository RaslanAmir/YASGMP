using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Xunit;
using YasGMP.Models;
using YasGMP.Models.DTO;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.Tests.TestStubs;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class DocumentControlModuleViewModelTests
{
    [Fact]
    public async Task InitializeAsync_ProjectsFilteredDocumentsAndSynchronizesSelection()
    {
        var documents = new[]
        {
            new SopDocument { Id = 1, Code = "DOC-001", Name = "First", Status = "draft" },
            new SopDocument { Id = 2, Code = "DOC-002", Name = "Second", Status = "approved" }
        };

        var context = CreateContext(control =>
        {
            control.SetDocuments(documents);
            control.ReplaceFilteredDocuments(documents);
            control.SelectDocument(documents[1]);
        });

        await context.ViewModel.InitializeAsync();

        Assert.Equal(1, context.ReloadTracker.Count);
        Assert.Equal(2, context.ViewModel.Records.Count);
        Assert.Equal("2", context.ViewModel.SelectedRecord?.Key);

        var newDocument = new SopDocument { Id = 3, Code = "DOC-003", Name = "Third", Status = "published" };
        context.Control.ReplaceFilteredDocuments(newDocument);

        Assert.Equal(1, context.ViewModel.Records.Count);
        Assert.Equal("3", context.ViewModel.SelectedRecord?.Key);
    }

    [Fact]
    public async Task AttachDocumentCommand_Success_UpdatesManifestAndStatus()
    {
        var document = new SopDocument { Id = 5, Code = "DOC-005", Name = "Attach" };
        var manifest = new[] { CreateAttachmentLink(1, "a.txt"), CreateAttachmentLink(2, "b.txt") };

        var context = CreateContext(
            control =>
            {
                control.SetDocuments(document);
                control.ReplaceFilteredDocuments(document);
                control.SelectDocument(document);
            },
            () => (true, new[] { "file-a.txt", "file-b.txt" }));

        var expectedMessage = "Attachments uploaded.";
        context.Service.SetAttachmentResult(new DocumentAttachmentUploadResult(true, expectedMessage, 2, 0, manifest));

        await context.ViewModel.AttachDocumentCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.UploadCallCount);
        Assert.Equal(new[] { "file-a.txt", "file-b.txt" }, context.Service.LastUploadedFileNames);
        Assert.Equal(expectedMessage, context.ViewModel.StatusMessage);
        Assert.Equal(manifest, context.ViewModel.AttachmentManifest);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    [Fact]
    public async Task AttachDocumentCommand_Failure_SurfacesMessageAndClearsBusy()
    {
        var document = new SopDocument { Id = 6, Code = "DOC-006", Name = "Attach" };
        var context = CreateContext(
            control =>
            {
                control.SetDocuments(document);
                control.ReplaceFilteredDocuments(document);
                control.SelectDocument(document);
            },
            () => (true, new[] { "file.txt" }));

        var manifest = new[] { CreateAttachmentLink(10, "existing.txt") };
        var expectedMessage = "Upload failed.";
        context.Service.SetAttachmentResult(new DocumentAttachmentUploadResult(false, expectedMessage, 1, 0, manifest));

        await context.ViewModel.AttachDocumentCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.UploadCallCount);
        Assert.Equal(expectedMessage, context.ViewModel.StatusMessage);
        Assert.Equal(manifest, context.ViewModel.AttachmentManifest);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    [Fact]
    public async Task AttachDocumentCommand_Exception_SurfacesErrorMessage()
    {
        var document = new SopDocument { Id = 7, Code = "DOC-007", Name = "Attach" };
        var context = CreateContext(
            control =>
            {
                control.SetDocuments(document);
                control.ReplaceFilteredDocuments(document);
                control.SelectDocument(document);
            },
            () => (true, new[] { "file.txt" }));

        context.Service.SetAttachmentException(new InvalidOperationException("disk full"));

        await context.ViewModel.AttachDocumentCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.UploadCallCount);
        Assert.Equal("Attachment upload failed: disk full", context.ViewModel.StatusMessage);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    [Fact]
    public async Task LinkChangeControlCommand_Success_ReloadsDocumentsAndCancelsPicker()
    {
        var document = new SopDocument { Id = 8, Code = "DOC-008", Name = "Link" };
        var changeControl = new ChangeControlSummaryDto { Id = 12, Title = "CC-12" };
        var context = CreateContext(control =>
        {
            control.SetDocuments(document);
            control.ReplaceFilteredDocuments(document);
            control.SelectDocument(document);
            control.SetSelectedChangeControl(changeControl);
        });

        var expectedMessage = "Linked.";
        context.Service.SetLifecycleResult(DocumentLifecycleOperation.Link, new DocumentLifecycleResult(true, expectedMessage));

        await context.ViewModel.LinkChangeControlCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.LinkCallCount);
        Assert.True(context.Control.CancelPickerExecuted);
        Assert.Equal(expectedMessage, context.ViewModel.StatusMessage);
        Assert.Equal(1, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    [Fact]
    public async Task LinkChangeControlCommand_Failure_DoesNotReload()
    {
        var document = new SopDocument { Id = 9, Code = "DOC-009", Name = "Link" };
        var changeControl = new ChangeControlSummaryDto { Id = 99, Title = "CC-99" };
        var context = CreateContext(control =>
        {
            control.SetDocuments(document);
            control.ReplaceFilteredDocuments(document);
            control.SelectDocument(document);
            control.SetSelectedChangeControl(changeControl);
        });

        var expectedMessage = "Link denied.";
        context.Service.SetLifecycleResult(DocumentLifecycleOperation.Link, new DocumentLifecycleResult(false, expectedMessage));

        await context.ViewModel.LinkChangeControlCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.LinkCallCount);
        Assert.Equal(expectedMessage, context.ViewModel.StatusMessage);
        Assert.Equal(0, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    [Fact]
    public async Task LinkChangeControlCommand_Exception_SurfacesError()
    {
        var document = new SopDocument { Id = 10, Code = "DOC-010", Name = "Link" };
        var changeControl = new ChangeControlSummaryDto { Id = 45, Title = "CC-45" };
        var context = CreateContext(control =>
        {
            control.SetDocuments(document);
            control.ReplaceFilteredDocuments(document);
            control.SelectDocument(document);
            control.SetSelectedChangeControl(changeControl);
        });

        context.Service.SetLifecycleException(DocumentLifecycleOperation.Link, new InvalidOperationException("service unavailable"));

        await context.ViewModel.LinkChangeControlCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.LinkCallCount);
        Assert.Equal("Linking failed: service unavailable", context.ViewModel.StatusMessage);
        Assert.Equal(0, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    [Fact]
    public async Task ExportDocumentsCommand_Success_UsesFilteredDocuments()
    {
        var documents = new[]
        {
            new SopDocument { Id = 11, Code = "DOC-011", Name = "Export" },
            new SopDocument { Id = 12, Code = "DOC-012", Name = "Export" }
        };

        var context = CreateContext(control =>
        {
            control.SetDocuments(documents);
            control.ReplaceFilteredDocuments(documents);
            control.SelectDocument(documents[0]);
        });

        var expectedMessage = "Export complete.";
        context.Service.SetExportResult(new DocumentExportResult(true, expectedMessage, "export.zip"));

        await context.ViewModel.ExportDocumentsCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.ExportCallCount);
        Assert.Equal(expectedMessage, context.ViewModel.StatusMessage);
        Assert.Equal(0, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    [Fact]
    public async Task ExportDocumentsCommand_Failure_SurfacesMessage()
    {
        var document = new SopDocument { Id = 13, Code = "DOC-013", Name = "Export" };
        var context = CreateContext(control =>
        {
            control.SetDocuments(document);
            control.ReplaceFilteredDocuments(document);
            control.SelectDocument(document);
        });

        var expectedMessage = "Export failed.";
        context.Service.SetExportResult(new DocumentExportResult(false, expectedMessage, null));

        await context.ViewModel.ExportDocumentsCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.ExportCallCount);
        Assert.Equal(expectedMessage, context.ViewModel.StatusMessage);
        Assert.Equal(0, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    [Fact]
    public async Task ExportDocumentsCommand_Exception_SurfacesError()
    {
        var document = new SopDocument { Id = 14, Code = "DOC-014", Name = "Export" };
        var context = CreateContext(control =>
        {
            control.SetDocuments(document);
            control.ReplaceFilteredDocuments(document);
            control.SelectDocument(document);
        });

        context.Service.SetExportException(new InvalidOperationException("network"));

        await context.ViewModel.ExportDocumentsCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.ExportCallCount);
        Assert.Equal("Export failed: network", context.ViewModel.StatusMessage);
        Assert.Equal(0, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    [Fact]
    public async Task InitiateDocument_SaveSuccess_ReloadsDocuments()
    {
        var context = CreateContext();
        await context.ViewModel.InitializeAsync();

        await context.ViewModel.EnterAddModeCommand.ExecuteAsync(null);
        context.ViewModel.Editor.Title = "New SOP";
        context.ViewModel.Editor.Code = "DOC-100";

        var expectedMessage = "Initiated.";
        context.Service.SetLifecycleResult(DocumentLifecycleOperation.Initiate, new DocumentLifecycleResult(true, expectedMessage));

        await context.ViewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.InitiateCallCount);
        Assert.Equal(expectedMessage, context.ViewModel.StatusMessage);
        Assert.Equal(2, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
        Assert.Equal(FormMode.View, context.ViewModel.Mode);
    }

    [Fact]
    public async Task InitiateDocument_SaveFailure_StaysInAddModeWithoutReload()
    {
        var context = CreateContext();
        await context.ViewModel.InitializeAsync();

        await context.ViewModel.EnterAddModeCommand.ExecuteAsync(null);
        context.ViewModel.Editor.Title = "New SOP";
        context.ViewModel.Editor.Code = "DOC-101";

        var expectedMessage = "Initiate blocked.";
        context.Service.SetLifecycleResult(DocumentLifecycleOperation.Initiate, new DocumentLifecycleResult(false, expectedMessage));

        var saved = await context.ViewModel.SaveCommand.ExecuteAsync(null);

        Assert.False(saved);
        Assert.Equal(1, context.Service.InitiateCallCount);
        Assert.Equal(expectedMessage, context.ViewModel.StatusMessage);
        Assert.Equal(1, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
        Assert.Equal(FormMode.Add, context.ViewModel.Mode);
    }

    [Fact]
    public async Task InitiateDocument_SaveException_UsesFailureStatus()
    {
        var context = CreateContext();
        await context.ViewModel.InitializeAsync();

        await context.ViewModel.EnterAddModeCommand.ExecuteAsync(null);
        context.ViewModel.Editor.Title = "New SOP";
        context.ViewModel.Editor.Code = "DOC-102";

        context.Service.SetLifecycleException(DocumentLifecycleOperation.Initiate, new InvalidOperationException("validation"));

        var saved = await context.ViewModel.SaveCommand.ExecuteAsync(null);

        Assert.False(saved);
        Assert.Equal(1, context.Service.InitiateCallCount);
        Assert.Equal("Save failed: validation", context.ViewModel.StatusMessage);
        Assert.Equal(1, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
        Assert.Equal(FormMode.Add, context.ViewModel.Mode);
    }

    [Fact]
    public async Task ReviseDocument_SaveSuccess_ReloadsDocuments()
    {
        var existing = new SopDocument { Id = 21, Code = "DOC-021", Name = "Existing" };
        var context = CreateContext(control =>
        {
            control.SetDocuments(existing);
            control.ReplaceFilteredDocuments(existing);
            control.SelectDocument(existing);
        });

        await context.ViewModel.InitializeAsync();
        await context.ViewModel.EnterUpdateModeCommand.ExecuteAsync(null);
        context.ViewModel.Editor.Title = "Updated";

        var expectedMessage = "Revised.";
        context.Service.SetLifecycleResult(DocumentLifecycleOperation.Revise, new DocumentLifecycleResult(true, expectedMessage));

        await context.ViewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.ReviseCallCount);
        Assert.Equal(expectedMessage, context.ViewModel.StatusMessage);
        Assert.Equal(2, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
        Assert.Equal(FormMode.View, context.ViewModel.Mode);
    }

    [Fact]
    public async Task ReviseDocument_SaveFailure_DoesNotReload()
    {
        var existing = new SopDocument { Id = 22, Code = "DOC-022", Name = "Existing" };
        var context = CreateContext(control =>
        {
            control.SetDocuments(existing);
            control.ReplaceFilteredDocuments(existing);
            control.SelectDocument(existing);
        });

        await context.ViewModel.InitializeAsync();
        await context.ViewModel.EnterUpdateModeCommand.ExecuteAsync(null);
        context.ViewModel.Editor.Title = "Updated";

        var expectedMessage = "Revision rejected.";
        context.Service.SetLifecycleResult(DocumentLifecycleOperation.Revise, new DocumentLifecycleResult(false, expectedMessage));

        var saved = await context.ViewModel.SaveCommand.ExecuteAsync(null);

        Assert.False(saved);
        Assert.Equal(1, context.Service.ReviseCallCount);
        Assert.Equal(expectedMessage, context.ViewModel.StatusMessage);
        Assert.Equal(1, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
        Assert.Equal(FormMode.Update, context.ViewModel.Mode);
    }

    [Fact]
    public async Task ReviseDocument_SaveException_UsesFailureStatus()
    {
        var existing = new SopDocument { Id = 23, Code = "DOC-023", Name = "Existing" };
        var context = CreateContext(control =>
        {
            control.SetDocuments(existing);
            control.ReplaceFilteredDocuments(existing);
            control.SelectDocument(existing);
        });

        await context.ViewModel.InitializeAsync();
        await context.ViewModel.EnterUpdateModeCommand.ExecuteAsync(null);
        context.ViewModel.Editor.Title = "Updated";

        context.Service.SetLifecycleException(DocumentLifecycleOperation.Revise, new InvalidOperationException("conflict"));

        var saved = await context.ViewModel.SaveCommand.ExecuteAsync(null);

        Assert.False(saved);
        Assert.Equal(1, context.Service.ReviseCallCount);
        Assert.Equal("Save failed: conflict", context.ViewModel.StatusMessage);
        Assert.Equal(1, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
        Assert.Equal(FormMode.Update, context.ViewModel.Mode);
    }

    [Fact]
    public async Task ApproveDocumentCommand_Success_ReloadsDocuments()
    {
        var document = new SopDocument { Id = 30, Code = "DOC-030", Name = "Approve" };
        var context = CreateContext(control =>
        {
            control.SetDocuments(document);
            control.ReplaceFilteredDocuments(document);
            control.SelectDocument(document);
        });

        var expectedMessage = "Approved.";
        context.Service.SetLifecycleResult(DocumentLifecycleOperation.Approve, new DocumentLifecycleResult(true, expectedMessage));

        await context.ViewModel.ApproveDocumentCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.ApproveCallCount);
        Assert.Equal(document, context.Service.LastApprovedDocument);
        Assert.Equal(expectedMessage, context.ViewModel.StatusMessage);
        Assert.Equal(1, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    [Fact]
    public async Task ApproveDocumentCommand_Failure_DoesNotReload()
    {
        var document = new SopDocument { Id = 31, Code = "DOC-031", Name = "Approve" };
        var context = CreateContext(control =>
        {
            control.SetDocuments(document);
            control.ReplaceFilteredDocuments(document);
            control.SelectDocument(document);
        });

        var expectedMessage = "Approval blocked.";
        context.Service.SetLifecycleResult(DocumentLifecycleOperation.Approve, new DocumentLifecycleResult(false, expectedMessage));

        await context.ViewModel.ApproveDocumentCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.ApproveCallCount);
        Assert.Equal(expectedMessage, context.ViewModel.StatusMessage);
        Assert.Equal(0, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    [Fact]
    public async Task ApproveDocumentCommand_Exception_SurfacesError()
    {
        var document = new SopDocument { Id = 32, Code = "DOC-032", Name = "Approve" };
        var context = CreateContext(control =>
        {
            control.SetDocuments(document);
            control.ReplaceFilteredDocuments(document);
            control.SelectDocument(document);
        });

        context.Service.SetLifecycleException(DocumentLifecycleOperation.Approve, new InvalidOperationException("signature"));

        await context.ViewModel.ApproveDocumentCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.ApproveCallCount);
        Assert.Equal("Approval failed: signature", context.ViewModel.StatusMessage);
        Assert.Equal(0, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    [Fact]
    public async Task PublishDocumentCommand_Success_ReloadsDocuments()
    {
        var document = new SopDocument { Id = 33, Code = "DOC-033", Name = "Publish" };
        var context = CreateContext(control =>
        {
            control.SetDocuments(document);
            control.ReplaceFilteredDocuments(document);
            control.SelectDocument(document);
        });

        var expectedMessage = "Published.";
        context.Service.SetLifecycleResult(DocumentLifecycleOperation.Publish, new DocumentLifecycleResult(true, expectedMessage));

        await context.ViewModel.PublishDocumentCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.PublishCallCount);
        Assert.Equal(document, context.Service.LastPublishedDocument);
        Assert.Equal(expectedMessage, context.ViewModel.StatusMessage);
        Assert.Equal(1, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    [Fact]
    public async Task PublishDocumentCommand_Failure_DoesNotReload()
    {
        var document = new SopDocument { Id = 34, Code = "DOC-034", Name = "Publish" };
        var context = CreateContext(control =>
        {
            control.SetDocuments(document);
            control.ReplaceFilteredDocuments(document);
            control.SelectDocument(document);
        });

        var expectedMessage = "Publish denied.";
        context.Service.SetLifecycleResult(DocumentLifecycleOperation.Publish, new DocumentLifecycleResult(false, expectedMessage));

        await context.ViewModel.PublishDocumentCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.PublishCallCount);
        Assert.Equal(expectedMessage, context.ViewModel.StatusMessage);
        Assert.Equal(0, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    [Fact]
    public async Task PublishDocumentCommand_Exception_SurfacesError()
    {
        var document = new SopDocument { Id = 35, Code = "DOC-035", Name = "Publish" };
        var context = CreateContext(control =>
        {
            control.SetDocuments(document);
            control.ReplaceFilteredDocuments(document);
            control.SelectDocument(document);
        });

        context.Service.SetLifecycleException(DocumentLifecycleOperation.Publish, new InvalidOperationException("routing"));

        await context.ViewModel.PublishDocumentCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.PublishCallCount);
        Assert.Equal("Publishing failed: routing", context.ViewModel.StatusMessage);
        Assert.Equal(0, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    [Fact]
    public async Task ExpireDocumentCommand_Success_ReloadsDocuments()
    {
        var document = new SopDocument { Id = 36, Code = "DOC-036", Name = "Expire" };
        var context = CreateContext(control =>
        {
            control.SetDocuments(document);
            control.ReplaceFilteredDocuments(document);
            control.SelectDocument(document);
        });

        var expectedMessage = "Expired.";
        context.Service.SetLifecycleResult(DocumentLifecycleOperation.Expire, new DocumentLifecycleResult(true, expectedMessage));

        await context.ViewModel.ExpireDocumentCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.ExpireCallCount);
        Assert.Equal(document, context.Service.LastExpiredDocument);
        Assert.Equal(expectedMessage, context.ViewModel.StatusMessage);
        Assert.Equal(1, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    [Fact]
    public async Task ExpireDocumentCommand_Failure_DoesNotReload()
    {
        var document = new SopDocument { Id = 37, Code = "DOC-037", Name = "Expire" };
        var context = CreateContext(control =>
        {
            control.SetDocuments(document);
            control.ReplaceFilteredDocuments(document);
            control.SelectDocument(document);
        });

        var expectedMessage = "Expiration blocked.";
        context.Service.SetLifecycleResult(DocumentLifecycleOperation.Expire, new DocumentLifecycleResult(false, expectedMessage));

        await context.ViewModel.ExpireDocumentCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.ExpireCallCount);
        Assert.Equal(expectedMessage, context.ViewModel.StatusMessage);
        Assert.Equal(0, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    [Fact]
    public async Task ExpireDocumentCommand_Exception_SurfacesError()
    {
        var document = new SopDocument { Id = 38, Code = "DOC-038", Name = "Expire" };
        var context = CreateContext(control =>
        {
            control.SetDocuments(document);
            control.ReplaceFilteredDocuments(document);
            control.SelectDocument(document);
        });

        context.Service.SetLifecycleException(DocumentLifecycleOperation.Expire, new InvalidOperationException("policy"));

        await context.ViewModel.ExpireDocumentCommand.ExecuteAsync(null);

        Assert.Equal(1, context.Service.ExpireCallCount);
        Assert.Equal("Expiration failed: policy", context.ViewModel.StatusMessage);
        Assert.Equal(0, context.ReloadTracker.Count);
        Assert.Contains(true, context.BusyTransitions);
        Assert.False(context.ViewModel.IsBusy);
    }

    private static DocumentControlModuleTestContext CreateContext(
        Action<ControlledDocumentControlViewModel>? configureControl = null,
        Func<(bool Accepted, IReadOnlyList<string> Files)>? filePicker = null)
    {
        var localization = CreateLocalizationService();
        var control = new ControlledDocumentControlViewModel();
        configureControl?.Invoke(control);

        var service = new RecordingDocumentControlService();
        var reloadTracker = new ReloadTracker();
        var viewModel = new DocumentControlModuleViewModel(
            control.Instance,
            localization,
            new StubCflDialogService(),
            new StubShellInteractionService(),
            new StubModuleNavigationService(),
            service,
            reloadTracker.InvokeAsync,
            filePicker);

        var busyTransitions = new List<bool>();
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(DocumentControlModuleViewModel.IsBusy))
            {
                busyTransitions.Add(viewModel.IsBusy);
            }
        };

        return new DocumentControlModuleTestContext(viewModel, control, service, reloadTracker, busyTransitions);
    }

    private static FakeLocalizationService CreateLocalizationService()
    {
        var resources = new Dictionary<string, IDictionary<string, string>>
        {
            ["en"] = new Dictionary<string, string>
            {
                ["Module.Title.DocumentControl"] = "Document Control",
                ["Module.Toolbar.Button.Attach.ToolTip"] = "Attach Files",
                ["Module.Toolbar.Button.Attach.Content"] = "Attach",
                ["Module.Status.Ready"] = "Ready.",
                ["Module.Status.Loading"] = "Loading {0}.",
                ["Module.Status.Loaded"] = "Loaded {0}.",
                ["Module.Status.NotInEditMode"] = "Not in edit mode: {0}.",
                ["Module.Status.ValidationIssues"] = "Validation issues: {1}.",
                ["Module.Status.SaveSuccess"] = "Save succeeded.",
                ["Module.Status.NoChanges"] = "No changes saved.",
                ["Module.Status.SaveFailure"] = "Save failed: {1}",
                ["Module.Status.Cancelled"] = "Cancelled {0}."
            },
            ["neutral"] = new Dictionary<string, string>()
        };

        return new FakeLocalizationService(resources, "en");
    }

    private static AttachmentLinkWithAttachment CreateAttachmentLink(int id, string fileName)
        => new(
            new AttachmentLink
            {
                Id = id,
                AttachmentId = id,
                EntityType = "documents",
                EntityId = 1
            },
            new Attachment
            {
                Id = id,
                Name = fileName,
                FileName = fileName,
                FileHash = "hash"
            });

    private sealed record DocumentControlModuleTestContext(
        DocumentControlModuleViewModel ViewModel,
        ControlledDocumentControlViewModel Control,
        RecordingDocumentControlService Service,
        ReloadTracker ReloadTracker,
        List<bool> BusyTransitions);

    private sealed class ReloadTracker
    {
        public int Count { get; private set; }

        public Task InvokeAsync()
        {
            Count++;
            return Task.CompletedTask;
        }
    }

    private sealed class ControlledDocumentControlViewModel
    {
        private static readonly PropertyChangedEventArgs FilteredDocumentsChangedArgs = new(nameof(DocumentControlViewModel.FilteredDocuments));
        private static readonly PropertyChangedEventArgs SelectedDocumentChangedArgs = new(nameof(DocumentControlViewModel.SelectedDocument));
        private static readonly PropertyChangedEventArgs SelectedChangeControlChangedArgs = new(nameof(DocumentControlViewModel.SelectedChangeControlForLink));

        private readonly ObservableCollection<SopDocument> _documents;
        private ObservableCollection<SopDocument> _filteredDocuments;
        private readonly ObservableCollection<ChangeControlSummaryDto> _changeControls;
        private readonly RelayCommand _cancelPickerCommand;

        public ControlledDocumentControlViewModel()
        {
            Instance = (DocumentControlViewModel)FormatterServices.GetUninitializedObject(typeof(DocumentControlViewModel));

            _documents = new ObservableCollection<SopDocument>();
            _filteredDocuments = new ObservableCollection<SopDocument>();
            _changeControls = new ObservableCollection<ChangeControlSummaryDto>();

            SetField("_documents", _documents);
            SetField("_filteredDocuments", _filteredDocuments);
            SetField("_availableChangeControls", _changeControls);
            SetField("_currentSessionId", "session");
            SetField("_currentDeviceInfo", "device");
            SetField("_currentIpAddress", "127.0.0.1");

            SetCommand("OpenChangeControlPickerCommand", new AsyncRelayCommand(() => Task.CompletedTask));
            _cancelPickerCommand = new RelayCommand(() => CancelPickerExecuted = true);
            SetCommand("CancelChangeControlPickerCommand", _cancelPickerCommand);
            SetCommand("LinkChangeControlCommand", new AsyncRelayCommand(() => Task.CompletedTask));
            SetCommand("LoadDocumentsCommand", new AsyncRelayCommand(() => Task.CompletedTask));
        }

        public DocumentControlViewModel Instance { get; }

        public bool CancelPickerExecuted { get; private set; }

        public void SetDocuments(params SopDocument[] documents)
        {
            _documents.Clear();
            foreach (var document in documents)
            {
                _documents.Add(document);
            }
        }

        public void ReplaceFilteredDocuments(params SopDocument[] documents)
        {
            _filteredDocuments = new ObservableCollection<SopDocument>(documents);
            SetField("_filteredDocuments", _filteredDocuments);
            RaisePropertyChanged(FilteredDocumentsChangedArgs);
        }

        public void SelectDocument(SopDocument document)
        {
            Instance.SelectedDocument = document;
            RaisePropertyChanged(SelectedDocumentChangedArgs);
        }

        public void SetSelectedChangeControl(ChangeControlSummaryDto? changeControl)
        {
            Instance.SelectedChangeControlForLink = changeControl;
            RaisePropertyChanged(SelectedChangeControlChangedArgs);
        }

        private void SetField<T>(string fieldName, T value)
        {
            var field = typeof(DocumentControlViewModel).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? throw new MissingFieldException(typeof(DocumentControlViewModel).FullName, fieldName);
            field.SetValue(Instance, value);
        }

        private void SetCommand(string propertyName, object command)
        {
            var field = typeof(DocumentControlViewModel)
                .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(typeof(DocumentControlViewModel).FullName, propertyName);
            field.SetValue(Instance, command);
        }

        private void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            var handlerField = typeof(DocumentControlViewModel).GetField("PropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            var handler = (PropertyChangedEventHandler?)handlerField?.GetValue(Instance);
            handler?.Invoke(Instance, args);
        }
    }

}
