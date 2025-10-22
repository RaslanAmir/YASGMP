using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Xunit;
using YasGMP.Models;
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class DocumentControlModuleViewModelTests
{
    [Theory]
    [InlineData("Approve")]
    [InlineData("Publish")]
    [InlineData("Expire")]
    public async Task LifecycleCommand_Success_ReloadsAndUpdatesStatus(string command)
    {
        var (viewModel, service, reloadTracker, busyTransitions) = CreateSystemUnderTest();
        var expectedMessage = $"{command} completed.";
        service.SetResult(command, new DocumentLifecycleResult(true, expectedMessage));

        await ExecuteLifecycleCommandAsync(viewModel, command);

        Assert.Equal(1, service.GetCallCount(command));
        Assert.Equal(expectedMessage, viewModel.StatusMessage);
        Assert.Equal(1, reloadTracker.Count);
        Assert.Contains(true, busyTransitions);
        Assert.False(viewModel.IsBusy);
    }

    [Theory]
    [InlineData("Approve")]
    [InlineData("Publish")]
    [InlineData("Expire")]
    public async Task LifecycleCommand_Failure_ShowsMessageWithoutReload(string command)
    {
        var (viewModel, service, reloadTracker, busyTransitions) = CreateSystemUnderTest();
        var expectedMessage = $"{command} denied.";
        service.SetResult(command, new DocumentLifecycleResult(false, expectedMessage));

        await ExecuteLifecycleCommandAsync(viewModel, command);

        Assert.Equal(1, service.GetCallCount(command));
        Assert.Equal(expectedMessage, viewModel.StatusMessage);
        Assert.Equal(0, reloadTracker.Count);
        Assert.Contains(true, busyTransitions);
        Assert.False(viewModel.IsBusy);
    }

    private static async Task ExecuteLifecycleCommandAsync(DocumentControlModuleViewModel viewModel, string command)
    {
        IAsyncRelayCommand lifecycleCommand = command switch
        {
            "Approve" => viewModel.ApproveDocumentCommand,
            "Publish" => viewModel.PublishDocumentCommand,
            "Expire" => viewModel.ExpireDocumentCommand,
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
        };

        await lifecycleCommand.ExecuteAsync(null);
    }

    private static (DocumentControlModuleViewModel ViewModel, RecordingDocumentControlService Service, ReloadTracker ReloadTracker, List<bool> BusyTransitions) CreateSystemUnderTest()
    {
        var localization = CreateLocalizationService();
        var documentControl = CreateDocumentControlViewModelStub();
        var service = new RecordingDocumentControlService();
        var reloadTracker = new ReloadTracker();
        var viewModel = new DocumentControlModuleViewModel(
            documentControl,
            localization,
            new StubCflDialogService(),
            new StubShellInteractionService(),
            new StubModuleNavigationService(),
            service,
            reloadTracker.InvokeAsync);

        var busyTransitions = new List<bool>();
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(DocumentControlModuleViewModel.IsBusy))
            {
                busyTransitions.Add(viewModel.IsBusy);
            }
        };

        return (viewModel, service, reloadTracker, busyTransitions);
    }

    private static FakeLocalizationService CreateLocalizationService()
    {
        var resources = new Dictionary<string, IDictionary<string, string>>
        {
            ["en"] = new Dictionary<string, string>
            {
                ["Module.Title.DocumentControl"] = "Document Control",
                ["Module.Toolbar.Button.Attach.ToolTip"] = "Attach",
                ["Module.Toolbar.Button.Attach.Content"] = "Attach"
            },
            ["neutral"] = new Dictionary<string, string>()
        };

        return new FakeLocalizationService(resources, "en");
    }

    private static DocumentControlViewModel CreateDocumentControlViewModelStub()
    {
        var instance = (DocumentControlViewModel)FormatterServices.GetUninitializedObject(typeof(DocumentControlViewModel));

        var documents = new ObservableCollection<SopDocument>();
        var filtered = new ObservableCollection<SopDocument>();
        var changeControls = new ObservableCollection<ChangeControlSummaryDto>();

        SetField(instance, "_documents", documents);
        SetField(instance, "_filteredDocuments", filtered);
        SetField(instance, "_availableChangeControls", changeControls);
        SetField(instance, "_currentSessionId", "test-session");
        SetField(instance, "_currentDeviceInfo", "test-device");
        SetField(instance, "_currentIpAddress", "127.0.0.1");

        SetCommand(instance, "OpenChangeControlPickerCommand", new RelayCommand(() => { }));
        SetCommand(instance, "CancelChangeControlPickerCommand", new RelayCommand(() => { }));
        SetCommand(instance, "LinkChangeControlCommand", new AsyncRelayCommand(() => Task.CompletedTask));
        SetCommand(instance, "LoadDocumentsCommand", new AsyncRelayCommand(() => Task.CompletedTask));

        var document = new SopDocument
        {
            Id = 1,
            Code = "DOC-001",
            Name = "Test Document",
            Status = "draft"
        };

        documents.Add(document);
        filtered.Add(document);
        instance.SelectedDocument = document;

        return instance;
    }

    private static void SetField<T>(object target, string fieldName, T value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                   ?? throw new MissingFieldException(target.GetType().FullName, fieldName);
        field.SetValue(target, value);
    }

    private static void SetCommand(object target, string propertyName, object command)
    {
        var field = target.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                   ?? throw new MissingFieldException(target.GetType().FullName, propertyName);
        field.SetValue(target, command);
    }

    private sealed class ReloadTracker
    {
        public int Count { get; private set; }

        public Task InvokeAsync()
        {
            Count++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingDocumentControlService : IDocumentControlService
    {
        private DocumentLifecycleResult _approveResult = new(true, "Approved.");
        private DocumentLifecycleResult _publishResult = new(true, "Published.");
        private DocumentLifecycleResult _expireResult = new(true, "Expired.");

        public int ApproveCallCount { get; private set; }
        public int PublishCallCount { get; private set; }
        public int ExpireCallCount { get; private set; }

        public void SetResult(string command, DocumentLifecycleResult result)
        {
            switch (command)
            {
                case "Approve":
                    _approveResult = result;
                    break;
                case "Publish":
                    _publishResult = result;
                    break;
                case "Expire":
                    _expireResult = result;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command), command, null);
            }
        }

        public int GetCallCount(string command) => command switch
        {
            "Approve" => ApproveCallCount,
            "Publish" => PublishCallCount,
            "Expire" => ExpireCallCount,
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
        };

        public Task<DocumentLifecycleResult> ApproveDocumentAsync(SopDocument document, CancellationToken cancellationToken = default)
        {
            ApproveCallCount++;
            return Task.FromResult(_approveResult);
        }

        public Task<DocumentLifecycleResult> PublishDocumentAsync(SopDocument document, CancellationToken cancellationToken = default)
        {
            PublishCallCount++;
            return Task.FromResult(_publishResult);
        }

        public Task<DocumentLifecycleResult> ExpireDocumentAsync(SopDocument document, CancellationToken cancellationToken = default)
        {
            ExpireCallCount++;
            return Task.FromResult(_expireResult);
        }

        public Task<DocumentLifecycleResult> InitiateDocumentAsync(SopDocument draft, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<DocumentLifecycleResult> ReviseDocumentAsync(SopDocument existing, SopDocument revision, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<DocumentLifecycleResult> LinkChangeControlAsync(SopDocument document, ChangeControlSummaryDto changeControl, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<DocumentExportResult> ExportDocumentsAsync(IReadOnlyCollection<SopDocument> documents, string format, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<DocumentAttachmentUploadResult> UploadAttachmentsAsync(SopDocument document, IEnumerable<DocumentAttachmentUpload> attachments, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<AttachmentLinkWithAttachment>> GetAttachmentManifestAsync(int documentId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
