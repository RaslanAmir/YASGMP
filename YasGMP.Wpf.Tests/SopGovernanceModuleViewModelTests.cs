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
using YasGMP.Services;
using YasGMP.ViewModels;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class SopGovernanceModuleViewModelTests
{
    [Fact]
    public async Task InitializeAsync_LoadsAdapterDocumentsAndProjectsRecords()
    {
        var initialDocuments = new[]
        {
            CreateDocument(101, "SOP-QUAL-001", "Deviation Handling", "published", "Quality", DateTime.Today.AddDays(-15)),
            CreateDocument(102, "SOP-VAL-004", "Equipment Qualification", "under review", "Validation", DateTime.Today.AddDays(-7))
        };

        var harness = SopViewModelHarness.Create();
        var service = new RecordingSopGovernanceService();
        service.EnqueueLoadResult(new(true, "Adapter loaded.", initialDocuments));
        var localization = CreateLocalization();
        var shell = new StubShellInteractionService();

        var viewModel = CreateViewModel(harness.ViewModel, service, localization, shell);

        await viewModel.InitializeAsync(null);

        Assert.Equal(1, service.LoadCallCount);
        Assert.Equal(initialDocuments.Length, harness.ViewModel.Documents.Count);
        Assert.Equal(initialDocuments.Length, harness.ViewModel.FilteredDocuments.Count);
        Assert.Equal(initialDocuments.Length, viewModel.Records.Count);
        Assert.Equal("Loaded 2", viewModel.StatusMessage);
        Assert.Equal("Loaded 2", shell.LastStatus);
    }

    [Fact]
    public async Task FilterProperties_ProxyToSharedViewModelAndRefreshRecords()
    {
        var documents = new[]
        {
            CreateDocument(201, "SOP-QA-001", "Quality Manual", "published", "Quality", DateTime.Today.AddDays(-30)),
            CreateDocument(202, "SOP-QA-050", "Quality Checklist", "draft", "Quality", DateTime.Today.AddDays(-5)),
            CreateDocument(203, "SOP-PR-101", "Production Startup", "active", "Production", DateTime.Today.AddDays(-2))
        };

        var harness = SopViewModelHarness.Create(documents);
        var service = new RecordingSopGovernanceService();
        service.EnqueueLoadResult(new(true, "Adapter loaded.", documents));
        var viewModel = CreateViewModel(harness.ViewModel, service);

        await viewModel.InitializeAsync(null);

        viewModel.SearchTerm = "Quality";

        Assert.Equal("Quality", harness.ViewModel.SearchTerm);
        Assert.Equal(2, viewModel.Records.Count);
        Assert.All(viewModel.Records, record => Assert.Contains("Quality", record.Title, StringComparison.OrdinalIgnoreCase));

        viewModel.StatusFilter = "published";

        Assert.Equal("published", harness.ViewModel.StatusFilter);
        Assert.Single(viewModel.Records);
        Assert.All(viewModel.Records, record => Assert.Equal("published", record.Status));

        viewModel.StatusFilter = null;

        viewModel.ProcessFilter = "Production";

        Assert.Equal("Production", harness.ViewModel.ProcessFilter);
        Assert.Collection(viewModel.Records, record => Assert.Equal("203", record.Key));

        var issuedFrom = DateTime.Today.AddDays(-10);
        viewModel.IssuedFrom = issuedFrom;

        Assert.Equal(issuedFrom, harness.ViewModel.IssuedFrom);
        Assert.Collection(viewModel.Records, record => Assert.Equal("203", record.Key));

        var issuedTo = DateTime.Today.AddDays(-1);
        viewModel.IssuedTo = issuedTo;

        Assert.Equal(issuedTo, harness.ViewModel.IssuedTo);
        Assert.All(viewModel.Records, record => Assert.Equal("203", record.Key));

        viewModel.IncludeOnlyActive = true;

        Assert.True(harness.ViewModel.IncludeOnlyActive);
        Assert.Collection(viewModel.Records, record => Assert.Equal("203", record.Key));
    }

    [Fact]
    public async Task CreateCommand_SuccessCallsAdapterAndRefreshesRecords()
    {
        var initial = new[]
        {
            CreateDocument(301, "SOP-QA-010", "Investigations", "active", "Quality", DateTime.Today.AddDays(-20))
        };

        var created = CreateDocument(305, "SOP-QA-011", "Complaint Handling", "active", "Quality", DateTime.Today);

        var harness = SopViewModelHarness.Create(initial);
        harness.SetDraftDocument(new SopDocument
        {
            Name = "Complaint Handling",
            FilePath = "complaint.pdf",
            Status = "draft",
            Process = "Quality",
            DateIssued = DateTime.Today,
            VersionNo = 1
        });

        var service = new RecordingSopGovernanceService();
        service.EnqueueLoadResult(new(true, "Loaded", initial));
        service.EnqueueLoadResult(new(true, "Loaded", new[] { initial[0], created }));
        service.CreateResult = new(true, "Created", created.Id);

        var viewModel = CreateViewModel(harness.ViewModel, service, CreateLocalization());

        await viewModel.InitializeAsync(null);

        await viewModel.CreateCommand.ExecuteAsync(null);

        var request = Assert.Single(service.CreatedDocuments);
        Assert.Equal("Complaint Handling", request.Name);
        Assert.Equal(2, service.LoadCallCount);
        Assert.Equal("Loaded 2", viewModel.StatusMessage);
        Assert.Equal("305", viewModel.SelectedRecord?.Key);
    }

    [Fact]
    public async Task CreateCommand_FailurePreservesRecordsAndBusyState()
    {
        var initial = new[]
        {
            CreateDocument(401, "SOP-QA-020", "Deviation Workflow", "active", "Quality", DateTime.Today.AddDays(-45))
        };

        var harness = SopViewModelHarness.Create(initial);
        harness.SetDraftDocument(new SopDocument
        {
            Name = "New SOP",
            FilePath = "new.pdf",
            Status = "draft",
            Process = "Quality",
            DateIssued = DateTime.Today,
            VersionNo = 1
        });

        var service = new RecordingSopGovernanceService();
        service.EnqueueLoadResult(new(true, "Loaded", initial));
        service.CreateResult = new(false, "Create failed", null);

        var viewModel = CreateViewModel(harness.ViewModel, service, CreateLocalization());

        await viewModel.InitializeAsync(null);

        await viewModel.CreateCommand.ExecuteAsync(null);

        Assert.Equal("Create failed", viewModel.StatusMessage);
        Assert.False(viewModel.IsBusy);
        Assert.Equal(1, viewModel.Records.Count);
        Assert.Equal("401", viewModel.Records[0].Key);
    }

    [Fact]
    public async Task UpdateCommand_SuccessRefreshesSelection()
    {
        var original = CreateDocument(501, "SOP-QA-030", "Calibration", "active", "Quality", DateTime.Today.AddDays(-15));
        var updated = CreateDocument(501, "SOP-QA-030", "Calibration", "published", "Quality", DateTime.Today);

        var harness = SopViewModelHarness.Create(new[] { original });
        harness.ViewModel.SelectedDocument = harness.ViewModel.Documents[0];
        var service = new RecordingSopGovernanceService();
        service.EnqueueLoadResult(new(true, "Loaded", new[] { original }));
        service.EnqueueLoadResult(new(true, "Loaded", new[] { updated }));
        service.UpdateResult = new(true, "Updated", updated.Id);

        var viewModel = CreateViewModel(harness.ViewModel, service, CreateLocalization());

        await viewModel.InitializeAsync(null);
        await viewModel.EnterUpdateModeCommand.ExecuteAsync(null);
        viewModel.Editor.Status = "published";

        await viewModel.UpdateCommand.ExecuteAsync(null);

        var request = Assert.Single(service.UpdatedDocuments);
        Assert.Equal("published", request.Status);
        Assert.Equal(2, service.LoadCallCount);
        Assert.Equal("501", viewModel.SelectedRecord?.Key);
        Assert.Equal("Loaded 1", viewModel.StatusMessage);
    }

    [Fact]
    public async Task UpdateCommand_FailureMaintainsSelectionAndStatus()
    {
        var original = CreateDocument(601, "SOP-QA-040", "Change Control", "active", "Quality", DateTime.Today.AddDays(-1));
        var harness = SopViewModelHarness.Create(new[] { original });
        harness.ViewModel.SelectedDocument = harness.ViewModel.Documents[0];

        var service = new RecordingSopGovernanceService();
        service.EnqueueLoadResult(new(true, "Loaded", new[] { original }));
        service.UpdateResult = new(false, "Update failed", null);

        var viewModel = CreateViewModel(harness.ViewModel, service, CreateLocalization());

        await viewModel.InitializeAsync(null);
        await viewModel.EnterUpdateModeCommand.ExecuteAsync(null);
        viewModel.Editor.Status = "published";

        await viewModel.UpdateCommand.ExecuteAsync(null);

        Assert.Equal("Update failed", viewModel.StatusMessage);
        Assert.False(viewModel.IsBusy);
        Assert.Equal("601", viewModel.SelectedRecord?.Key);
        Assert.Equal("active", harness.ViewModel.SelectedDocument?.Status);
    }

    [Fact]
    public async Task DeleteCommand_SuccessClearsSelectionAndResetsDraft()
    {
        var original = CreateDocument(701, "SOP-QA-050", "Record Retention", "active", "Quality", DateTime.Today.AddDays(-10));

        var harness = SopViewModelHarness.Create(new[] { original });
        harness.ViewModel.SelectedDocument = harness.ViewModel.Documents[0];

        var service = new RecordingSopGovernanceService();
        service.EnqueueLoadResult(new(true, "Loaded", new[] { original }));
        service.EnqueueLoadResult(new(true, "Loaded", Array.Empty<SopDocument>()));
        service.DeleteResult = new(true, "Deleted", null);

        var viewModel = CreateViewModel(harness.ViewModel, service, CreateLocalization());

        await viewModel.InitializeAsync(null);
        await viewModel.EnterUpdateModeCommand.ExecuteAsync(null);

        await viewModel.DeleteCommand.ExecuteAsync(null);

        var request = Assert.Single(service.DeletedDocuments);
        Assert.Equal(701, request.Id);
        Assert.Equal(2, service.LoadCallCount);
        Assert.Null(viewModel.SelectedRecord);
        Assert.Equal(1, harness.ResetDraftCallCount);
        Assert.Equal("Loaded 0", viewModel.StatusMessage);
    }

    [Fact]
    public async Task DeleteCommand_FailureLeavesRecordsIntact()
    {
        var original = CreateDocument(801, "SOP-QA-060", "Supplier Audits", "active", "Quality", DateTime.Today.AddDays(-8));

        var harness = SopViewModelHarness.Create(new[] { original });
        harness.ViewModel.SelectedDocument = harness.ViewModel.Documents[0];

        var service = new RecordingSopGovernanceService();
        service.EnqueueLoadResult(new(true, "Loaded", new[] { original }));
        service.DeleteResult = new(false, "Delete failed", null);

        var viewModel = CreateViewModel(harness.ViewModel, service, CreateLocalization());

        await viewModel.InitializeAsync(null);
        await viewModel.EnterUpdateModeCommand.ExecuteAsync(null);

        await viewModel.DeleteCommand.ExecuteAsync(null);

        Assert.Equal("Delete failed", viewModel.StatusMessage);
        Assert.False(viewModel.IsBusy);
        Assert.Equal(1, viewModel.Records.Count);
        Assert.Equal("801", viewModel.SelectedRecord?.Key);
    }

    [Fact]
    public async Task FormModesAndBusyStateToggleCommandsAndEditor()
    {
        var documents = new[] { CreateDocument(901, "SOP-QA-070", "CAPA", "active", "Quality", DateTime.Today.AddDays(-3)) };
        var harness = SopViewModelHarness.Create(documents);
        harness.ViewModel.SelectedDocument = harness.ViewModel.Documents[0];

        var service = new RecordingSopGovernanceService();
        service.EnqueueLoadResult(new(true, "Loaded", documents));

        var viewModel = CreateViewModel(harness.ViewModel, service, CreateLocalization());

        await viewModel.InitializeAsync(null);

        Assert.False(viewModel.IsEditorEnabled);
        Assert.True(viewModel.CreateCommand.CanExecute(null));
        Assert.False(viewModel.UpdateCommand.CanExecute(null));
        Assert.False(viewModel.DeleteCommand.CanExecute(null));

        await viewModel.EnterAddModeCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsEditorEnabled);
        Assert.True(viewModel.CreateCommand.CanExecute(null));
        Assert.False(viewModel.UpdateCommand.CanExecute(null));

        await viewModel.EnterViewModeCommand.ExecuteAsync(null);
        Assert.False(viewModel.IsEditorEnabled);

        await viewModel.EnterUpdateModeCommand.ExecuteAsync(null);
        Assert.True(viewModel.IsEditorEnabled);
        Assert.True(viewModel.UpdateCommand.CanExecute(null));
        Assert.True(viewModel.DeleteCommand.CanExecute(null));

        viewModel.IsBusy = true;

        Assert.False(viewModel.CreateCommand.CanExecute(null));
        Assert.False(viewModel.UpdateCommand.CanExecute(null));
        Assert.False(viewModel.DeleteCommand.CanExecute(null));

        viewModel.IsBusy = false;
        harness.ViewModel.SelectedDocument = harness.ViewModel.Documents[0];
        viewModel.UpdateCommand.NotifyCanExecuteChanged();
        viewModel.DeleteCommand.NotifyCanExecuteChanged();

        Assert.True(viewModel.UpdateCommand.CanExecute(null));
        Assert.True(viewModel.DeleteCommand.CanExecute(null));
    }

    [Fact]
    public void SharedViewModelPropertyChangesPropagateToShell()
    {
        var documents = new[]
        {
            CreateDocument(1001, "SOP-QA-080", "Investigations", "active", "Quality", DateTime.Today.AddDays(-6))
        };

        var harness = SopViewModelHarness.Create(documents);
        var shell = new StubShellInteractionService();
        var service = new RecordingSopGovernanceService();
        service.EnqueueLoadResult(new(true, "Loaded", documents));

        var viewModel = CreateViewModel(harness.ViewModel, service, CreateLocalization(), shell);

        // Trigger property changed wiring
        harness.SetFilteredDocuments(documents);
        harness.SetStatusMessage("Initial");

        harness.SetStatusMessage("Shared message");

        Assert.Equal("Shared message", viewModel.StatusMessage);
        Assert.Equal("Shared message", shell.LastStatus);

        harness.SetIsBusy(true);
        Assert.True(viewModel.IsBusy);
        Assert.False(viewModel.CreateCommand.CanExecute(null));

        harness.ViewModel.SelectedDocument = harness.ViewModel.Documents[0];
        harness.RaiseSelectedDocumentChanged();
        Assert.Equal(documents[0].Name, viewModel.Editor.Name);

        var filtered = new[]
        {
            CreateDocument(1002, "SOP-QA-081", "CAPA", "active", "Quality", DateTime.Today.AddDays(-1))
        };
        harness.SetFilteredDocuments(filtered);

        Assert.Collection(viewModel.Records, record => Assert.Equal("1002", record.Key));
    }

    private static SopGovernanceModuleViewModel CreateViewModel(
        SopViewModel sop,
        RecordingSopGovernanceService service,
        ILocalizationService? localization = null,
        IShellInteractionService? shell = null)
    {
        localization ??= CreateLocalization();
        shell ??= new StubShellInteractionService();
        return new SopGovernanceModuleViewModel(
            sop,
            service,
            localization,
            new StubCflDialogService(),
            shell,
            new StubModuleNavigationService());
    }

    private static SopDocument CreateDocument(
        int id,
        string code,
        string name,
        string status,
        string process,
        DateTime issued)
        => new()
        {
            Id = id,
            Code = code,
            Name = name,
            Status = status,
            Process = process,
            DateIssued = issued,
            DateExpiry = issued.AddMonths(6),
            FilePath = $"{code}.pdf",
            Description = $"Description for {name}",
            VersionNo = 1
        };

    private static ILocalizationService CreateLocalization()
    {
        var resources = new Dictionary<string, IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Module.Title.SopGovernance"] = "SOP Governance",
                ["Module.Status.Ready"] = "Ready",
                ["Module.Status.Loading"] = "Loading {0}",
                ["Module.Status.Loaded"] = "Loaded {0}",
                ["Module.SopGovernance.Field.Process"] = "Process",
                ["Module.SopGovernance.Field.Issued"] = "Issued",
                ["Module.SopGovernance.Field.Expiry"] = "Expiry",
                ["Module.SopGovernance.Field.Owner"] = "Owner"
            }
        };

        return new FakeLocalizationService(resources, "en");
    }

    private sealed class RecordingSopGovernanceService : ISopGovernanceService
    {
        private readonly Queue<SopGovernanceLoadResult> _loadResults = new();

        public List<SopDocument> CreatedDocuments { get; } = new();
        public List<SopDocument> UpdatedDocuments { get; } = new();
        public List<SopDocument> DeletedDocuments { get; } = new();

        public SopGovernanceLoadResult DefaultLoadResult { get; set; }
            = new(true, "Loaded", Array.Empty<SopDocument>());

        public SopGovernanceOperationResult CreateResult { get; set; }
            = new(true, "Created", null);

        public SopGovernanceOperationResult UpdateResult { get; set; }
            = new(true, "Updated", null);

        public SopGovernanceOperationResult DeleteResult { get; set; }
            = new(true, "Deleted", null);

        public SopGovernanceExportResult ExportResult { get; set; }
            = new(true, "Exported", null);

        public int LoadCallCount { get; private set; }

        public void EnqueueLoadResult(SopGovernanceLoadResult result)
            => _loadResults.Enqueue(result);

        public Task<SopGovernanceLoadResult> LoadAsync(CancellationToken cancellationToken = default)
        {
            LoadCallCount++;
            return Task.FromResult(_loadResults.Count > 0 ? _loadResults.Dequeue() : DefaultLoadResult);
        }

        public Task<SopGovernanceOperationResult> CreateAsync(
            SopDocument draft,
            IEnumerable<SopAttachmentUpload>? attachments = null,
            CancellationToken cancellationToken = default)
        {
            CreatedDocuments.Add(Clone(draft));
            return Task.FromResult(CreateResult);
        }

        public Task<SopGovernanceOperationResult> UpdateAsync(
            SopDocument document,
            IEnumerable<SopAttachmentUpload>? attachments = null,
            CancellationToken cancellationToken = default)
        {
            UpdatedDocuments.Add(Clone(document));
            return Task.FromResult(UpdateResult);
        }

        public Task<SopGovernanceOperationResult> DeleteAsync(
            SopDocument document,
            string? reason = null,
            CancellationToken cancellationToken = default)
        {
            DeletedDocuments.Add(Clone(document));
            return Task.FromResult(DeleteResult);
        }

        public Task<SopGovernanceExportResult> ExportAsync(
            IList<SopDocument> documents,
            string format,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ExportResult);

        private static SopDocument Clone(SopDocument source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.DeepCopy();
        }
    }

    private sealed class SopViewModelHarness
    {
        private readonly PropertyInfo _documentsProperty;
        private readonly PropertyInfo _filteredDocumentsProperty;
        private readonly PropertyInfo _draftProperty;
        private readonly PropertyInfo _statusProperty;
        private readonly PropertyInfo _isBusyProperty;
        private readonly FieldInfo _authField;
        private readonly FieldInfo _sessionField;
        private readonly FieldInfo _deviceField;
        private readonly FieldInfo _ipField;

        private SopViewModelHarness(SopViewModel viewModel)
        {
            ViewModel = viewModel;
            var type = typeof(SopViewModel);

            _documentsProperty = type.GetProperty("Documents", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                ?? throw new InvalidOperationException("Documents property not found.");
            _filteredDocumentsProperty = type.GetProperty("FilteredDocuments", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                ?? throw new InvalidOperationException("FilteredDocuments property not found.");
            _draftProperty = type.GetProperty("DraftDocument", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                ?? throw new InvalidOperationException("DraftDocument property not found.");
            _statusProperty = type.GetProperty("StatusMessage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                ?? throw new InvalidOperationException("StatusMessage property not found.");
            _isBusyProperty = type.GetProperty("IsBusy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                ?? throw new InvalidOperationException("IsBusy property not found.");

            _authField = type.GetField("_authService", BindingFlags.Instance | BindingFlags.NonPublic)!
                ?? throw new InvalidOperationException("_authService field not found.");
            _sessionField = type.GetField("_currentSessionId", BindingFlags.Instance | BindingFlags.NonPublic)!
                ?? throw new InvalidOperationException("_currentSessionId field not found.");
            _deviceField = type.GetField("_currentDeviceInfo", BindingFlags.Instance | BindingFlags.NonPublic)!
                ?? throw new InvalidOperationException("_currentDeviceInfo field not found.");
            _ipField = type.GetField("_currentIpAddress", BindingFlags.Instance | BindingFlags.NonPublic)!
                ?? throw new InvalidOperationException("_currentIpAddress field not found.");
        }

        public SopViewModel ViewModel { get; }

        public int ResetDraftCallCount { get; private set; }

        public static SopViewModelHarness Create(IEnumerable<SopDocument>? documents = null)
        {
            var instance = (SopViewModel)FormatterServices.GetUninitializedObject(typeof(SopViewModel));
            var harness = new SopViewModelHarness(instance);
            harness.Initialize(documents ?? Array.Empty<SopDocument>());
            return harness;
        }

        public void SetDraftDocument(SopDocument draft)
        {
            var setter = _draftProperty.GetSetMethod(true)!;
            setter.Invoke(ViewModel, new object[] { draft });
        }

        public void SetStatusMessage(string message)
        {
            var setter = _statusProperty.GetSetMethod(true)!;
            setter.Invoke(ViewModel, new object[] { message });
        }

        public void SetIsBusy(bool value)
        {
            var setter = _isBusyProperty.GetSetMethod(true)!;
            setter.Invoke(ViewModel, new object[] { value });
        }

        public void SetFilteredDocuments(IEnumerable<SopDocument> documents)
        {
            var collection = new ObservableCollection<SopDocument>(documents.Select(Clone));
            var setter = _filteredDocumentsProperty.GetSetMethod(true)!;
            setter.Invoke(ViewModel, new object[] { collection });
        }

        public void RaiseSelectedDocumentChanged()
            => ViewModel.PropertyChanged?.Invoke(ViewModel, new PropertyChangedEventArgs(nameof(SopViewModel.SelectedDocument)));

        private void Initialize(IEnumerable<SopDocument> documents)
        {
            var documentList = new ObservableCollection<SopDocument>(documents.Select(Clone));
            var filtered = new ObservableCollection<SopDocument>(documentList.Select(Clone));

            var documentsSetter = _documentsProperty.GetSetMethod(true)!;
            documentsSetter.Invoke(ViewModel, new object[] { documentList });

            var filteredSetter = _filteredDocumentsProperty.GetSetMethod(true)!;
            filteredSetter.Invoke(ViewModel, new object[] { filtered });

            var draftSetter = _draftProperty.GetSetMethod(true)!;
            draftSetter.Invoke(ViewModel, new object[] { new SopDocument { Name = "Draft", FilePath = "draft.pdf", Status = "draft", VersionNo = 1 } });

            var statusSetter = _statusProperty.GetSetMethod(true)!;
            statusSetter.Invoke(ViewModel, new object[] { string.Empty });

            var busySetter = _isBusyProperty.GetSetMethod(true)!;
            busySetter.Invoke(ViewModel, new object[] { false });

            _authField.SetValue(ViewModel, CreateAuthService());
            _sessionField.SetValue(ViewModel, "session");
            _deviceField.SetValue(ViewModel, "device");
            _ipField.SetValue(ViewModel, "127.0.0.1");

            SetCommand("LoadDocumentsCommand", new AsyncRelayCommand(() => Task.CompletedTask));
            SetCommand("CreateDocumentCommand", new AsyncRelayCommand(() => Task.CompletedTask));
            SetCommand("UpdateDocumentCommand", new AsyncRelayCommand(() => Task.CompletedTask));
            SetCommand("DeleteDocumentCommand", new AsyncRelayCommand(() => Task.CompletedTask));
            SetCommand("FilterChangedCommand", new RelayCommand(() => { }));
            SetCommand("ResetDraftCommand", new RelayCommand(() =>
            {
                ResetDraftCallCount++;
                var setter = _draftProperty.GetSetMethod(true)!;
                setter.Invoke(ViewModel, new object[] { new SopDocument { Name = "Draft", FilePath = "draft.pdf", Status = "draft", VersionNo = 1 } });
            }));
        }

        private void SetCommand(string propertyName, object command)
        {
            var field = typeof(SopViewModel).GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Backing field for {propertyName} not found.");
            field.SetValue(ViewModel, command);
        }

        private static AuthService CreateAuthService()
        {
            var auth = (AuthService)FormatterServices.GetUninitializedObject(typeof(AuthService));
            var user = new User { Id = 42, FullName = "QA" };
            var currentUserField = typeof(AuthService).GetProperty(nameof(AuthService.CurrentUser), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
            currentUserField.GetSetMethod(true)!.Invoke(auth, new object?[] { user });
            var sessionField = typeof(AuthService).GetProperty(nameof(AuthService.CurrentSessionId), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
            sessionField.GetSetMethod(true)!.Invoke(auth, new object?[] { Guid.NewGuid().ToString() });
            return auth;
        }

        private static SopDocument Clone(SopDocument document)
            => document?.DeepCopy() ?? throw new ArgumentNullException(nameof(document));
    }
}
