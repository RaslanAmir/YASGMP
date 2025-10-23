using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.ViewModels;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class TrainingRecordsModuleViewModelTests
{
    [Fact]
    public async Task InitializeAsync_LoadsRecordsAndUpdatesStatus()
    {
        var records = new[]
        {
            CreateRecord(1, "TR-001", "planned", "GMP"),
            CreateRecord(2, "TR-002", "assigned", "SOP")
        };

        var trainingRecords = TrainingRecordViewModelFactory.Create();
        var service = new RecordingTrainingRecordService();
        service.SetLoadResult(new TrainingRecordLoadResult(true, "Adapter loaded.", records));
        var localization = CreateLocalization();
        var shell = new StubShellInteractionService();

        var viewModel = CreateViewModel(trainingRecords, service, localization, shell);

        await viewModel.InitializeAsync(null);

        Assert.Equal(1, service.LoadCallCount);
        Assert.Equal(2, trainingRecords.TrainingRecords.Count);
        Assert.Equal(2, trainingRecords.FilteredTrainingRecords.Count);
        Assert.Equal("Loaded 2", viewModel.StatusMessage);
        Assert.Equal("Loaded 2", shell.LastStatus);
        Assert.Collection(
            viewModel.Records,
            first => Assert.Equal("1", first.Key),
            second => Assert.Equal("2", second.Key));
    }

    [Fact]
    public async Task Filters_ProjectIntoSharedViewModelAndRefreshRecords()
    {
        var records = new[]
        {
            CreateRecord(1, "TR-001", "planned", "GMP"),
            CreateRecord(2, "TR-002", "pending_approval", "SOP"),
            CreateRecord(3, "TR-003", "completed", "SOP")
        };

        var trainingRecords = TrainingRecordViewModelFactory.Create();
        var service = new RecordingTrainingRecordService();
        service.SetLoadResult(new TrainingRecordLoadResult(true, "Adapter loaded.", records));

        var viewModel = CreateViewModel(trainingRecords, service);

        await viewModel.InitializeAsync(null);

        viewModel.StatusFilter = "planned";

        Assert.Equal("planned", trainingRecords.StatusFilter);
        Assert.All(trainingRecords.FilteredTrainingRecords, record => Assert.Equal("planned", record.Status));
        Assert.Equal(1, viewModel.Records.Count);
        Assert.Equal("1", viewModel.Records[0].Key);

        viewModel.StatusFilter = null;
        viewModel.TypeFilter = "SOP";

        Assert.Equal("SOP", trainingRecords.TypeFilter);
        Assert.All(trainingRecords.FilteredTrainingRecords, record => Assert.Equal("SOP", record.TrainingType));
        Assert.Equal(2, viewModel.Records.Count);
        Assert.Collection(
            viewModel.Records,
            first => Assert.Equal("2", first.Key),
            second => Assert.Equal("3", second.Key));
    }

    [Fact]
    public async Task InitiateCommand_InvokesAdapterAndUpdatesStatus()
    {
        var records = new[] { CreateRecord(1, "TR-001", "planned", "GMP") };
        var trainingRecords = TrainingRecordViewModelFactory.Create(records);
        var service = new RecordingTrainingRecordService();
        service.SetLoadResult(new TrainingRecordLoadResult(true, "Adapter loaded.", records));
        var localization = CreateLocalization();
        var viewModel = CreateViewModel(trainingRecords, service, localization);

        await viewModel.InitializeAsync(null);

        viewModel.Editor.Title = "New Training";
        viewModel.Editor.TrainingType = "GMP";
        viewModel.Editor.Note = "Initiation note";

        Assert.True(viewModel.InitiateCommand.CanExecute(null));

        await viewModel.InitiateCommand.ExecuteAsync(null);

        var request = Assert.Single(service.InitiateRequests);
        Assert.Equal("New Training", request.Draft.Title);
        Assert.Equal("Initiation note", request.Note);
        Assert.Equal("Initiated New Training", viewModel.StatusMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task AssignCommand_RequiresPlannedRecordAndUpdatesStatus()
    {
        var planned = CreateRecord(1, "TR-001", "planned", "GMP", assigneeId: 4);
        var records = new[] { planned };
        var trainingRecords = TrainingRecordViewModelFactory.Create(records);
        var service = new RecordingTrainingRecordService();
        service.SetLoadResult(new TrainingRecordLoadResult(true, "Adapter loaded.", records));
        var viewModel = CreateViewModel(trainingRecords, service, CreateLocalization());

        await viewModel.InitializeAsync(null);

        trainingRecords.SelectedTrainingRecord = trainingRecords.TrainingRecords[0];

        Assert.True(viewModel.AssignCommand.CanExecute(null));

        await viewModel.AssignCommand.ExecuteAsync(null);

        var request = Assert.Single(service.AssignRequests);
        Assert.Equal(planned.Id, request.Record.Id);
        Assert.Equal(planned.TraineeId, request.AssigneeId);
        Assert.Equal("Assigned TR-001", viewModel.StatusMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task AssignCommand_FailureRetainsAdapterMessage()
    {
        var planned = CreateRecord(1, "TR-001", "planned", "GMP", assigneeId: 4);
        var trainingRecords = TrainingRecordViewModelFactory.Create(new[] { planned });
        var service = new RecordingTrainingRecordService
        {
            AssignResult = new TrainingRecordOperationResult(false, "Adapter failure", planned.Id)
        };
        service.SetLoadResult(new TrainingRecordLoadResult(true, "Adapter loaded.", new[] { planned }));
        var viewModel = CreateViewModel(trainingRecords, service, CreateLocalization());

        await viewModel.InitializeAsync(null);

        trainingRecords.SelectedTrainingRecord = trainingRecords.TrainingRecords[0];
        viewModel.StatusMessage = "Previous";

        await viewModel.AssignCommand.ExecuteAsync(null);

        Assert.Equal("Adapter failure", viewModel.StatusMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task ApproveCommand_InvokesAdapterWithSelectedRecord()
    {
        var pending = CreateRecord(2, "TR-002", "pending_approval", "SOP");
        var trainingRecords = TrainingRecordViewModelFactory.Create(new[] { pending });
        var service = new RecordingTrainingRecordService();
        service.SetLoadResult(new TrainingRecordLoadResult(true, "Adapter loaded.", new[] { pending }));
        var viewModel = CreateViewModel(trainingRecords, service, CreateLocalization());

        await viewModel.InitializeAsync(null);

        trainingRecords.SelectedTrainingRecord = trainingRecords.TrainingRecords[0];
        viewModel.Editor.Note = "Approval note";

        Assert.True(viewModel.ApproveCommand.CanExecute(null));

        await viewModel.ApproveCommand.ExecuteAsync(null);

        var request = Assert.Single(service.ApproveRequests);
        Assert.Equal(pending.Id, request.Record.Id);
        Assert.Equal("Approval note", request.Note);
        Assert.Equal("Approved TR-002", viewModel.StatusMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task CompleteCommand_InvokesAdapterWithNote()
    {
        var assigned = CreateRecord(3, "TR-003", "assigned", "GMP");
        var trainingRecords = TrainingRecordViewModelFactory.Create(new[] { assigned });
        var service = new RecordingTrainingRecordService();
        service.SetLoadResult(new TrainingRecordLoadResult(true, "Adapter loaded.", new[] { assigned }));
        var viewModel = CreateViewModel(trainingRecords, service, CreateLocalization());

        await viewModel.InitializeAsync(null);

        trainingRecords.SelectedTrainingRecord = trainingRecords.TrainingRecords[0];
        viewModel.Editor.Note = "Completion note";

        Assert.True(viewModel.CompleteCommand.CanExecute(null));

        await viewModel.CompleteCommand.ExecuteAsync(null);

        var request = Assert.Single(service.CompleteRequests);
        Assert.Equal(assigned.Id, request.Record.Id);
        Assert.Equal("Completion note", request.Note);
        Assert.Null(request.Attachments);
        Assert.Equal("Completed TR-003", viewModel.StatusMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task CloseCommand_InvokesAdapterAndUpdatesStatus()
    {
        var completed = CreateRecord(4, "TR-004", "completed", "SOP");
        var trainingRecords = TrainingRecordViewModelFactory.Create(new[] { completed });
        var service = new RecordingTrainingRecordService();
        service.SetLoadResult(new TrainingRecordLoadResult(true, "Adapter loaded.", new[] { completed }));
        var viewModel = CreateViewModel(trainingRecords, service, CreateLocalization());

        await viewModel.InitializeAsync(null);

        trainingRecords.SelectedTrainingRecord = trainingRecords.TrainingRecords[0];
        viewModel.Editor.Note = "Closure note";

        Assert.True(viewModel.CloseCommand.CanExecute(null));

        await viewModel.CloseCommand.ExecuteAsync(null);

        var request = Assert.Single(service.CloseRequests);
        Assert.Equal(completed.Id, request.Record.Id);
        Assert.Equal("Closure note", request.Note);
        Assert.Null(request.Attachments);
        Assert.Equal("Closed TR-004", viewModel.StatusMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task ExportCommand_ExportsSnapshotAndUpdatesStatus()
    {
        var records = new[]
        {
            CreateRecord(1, "TR-001", "planned", "GMP"),
            CreateRecord(2, "TR-002", "assigned", "SOP")
        };

        var trainingRecords = TrainingRecordViewModelFactory.Create(records);
        var service = new RecordingTrainingRecordService
        {
            ExportResult = new TrainingRecordExportResult(true, "Exported snapshot.", "path.csv")
        };
        service.SetLoadResult(new TrainingRecordLoadResult(true, "Adapter loaded.", records));

        var viewModel = CreateViewModel(trainingRecords, service, CreateLocalization());
        viewModel.ExportFormatPromptAsync = () => Task.FromResult<string?>("csv");

        await viewModel.InitializeAsync(null);

        Assert.True(viewModel.ExportCommand.CanExecute(null));

        await viewModel.ExportCommand.ExecuteAsync(null);

        var request = Assert.Single(service.ExportRequests);
        Assert.Equal("csv", request.Format);
        Assert.Equal(2, request.Records.Count);
        Assert.Equal("Exported snapshot.", viewModel.StatusMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task ExportCommand_CancelledLeavesStatusCancelled()
    {
        var trainingRecords = TrainingRecordViewModelFactory.Create(new[]
        {
            CreateRecord(1, "TR-001", "planned", "GMP")
        });
        var service = new RecordingTrainingRecordService();
        service.SetLoadResult(new TrainingRecordLoadResult(true, "Adapter loaded.", trainingRecords.TrainingRecords.ToArray()));

        var viewModel = CreateViewModel(trainingRecords, service, CreateLocalization());
        viewModel.ExportFormatPromptAsync = () => Task.FromResult<string?>(null);

        await viewModel.InitializeAsync(null);

        Assert.True(viewModel.ExportCommand.CanExecute(null));

        await viewModel.ExportCommand.ExecuteAsync(null);

        Assert.Empty(service.ExportRequests);
        Assert.Equal("Cancelled", viewModel.StatusMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task FilteredRecordsChanged_ReprojectsRecordsAndPreservesSelection()
    {
        var records = new[]
        {
            CreateRecord(1, "TR-001", "planned", "GMP"),
            CreateRecord(2, "TR-002", "assigned", "SOP")
        };

        var trainingRecords = TrainingRecordViewModelFactory.Create(records);
        var service = new RecordingTrainingRecordService();
        service.SetLoadResult(new TrainingRecordLoadResult(true, "Adapter loaded.", records));

        var viewModel = CreateViewModel(trainingRecords, service, CreateLocalization());

        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records[1];

        var replacement = new[]
        {
            CreateRecord(2, "TR-002", "assigned", "SOP"),
            CreateRecord(3, "TR-003", "completed", "SOP")
        };

        trainingRecords.FilteredTrainingRecords = new System.Collections.ObjectModel.ObservableCollection<TrainingRecord>(replacement);

        Assert.Equal(2, viewModel.Records.Count);
        Assert.Equal("2", viewModel.SelectedRecord?.Key);
    }

    private static TrainingRecordsModuleViewModel CreateViewModel(
        TrainingRecordViewModel trainingRecords,
        RecordingTrainingRecordService service,
        ILocalizationService? localization = null,
        IShellInteractionService? shell = null)
    {
        localization ??= CreateLocalization();
        shell ??= new StubShellInteractionService();
        return new TrainingRecordsModuleViewModel(
            trainingRecords,
            service,
            localization,
            new StubCflDialogService(),
            shell,
            new StubModuleNavigationService());
    }

    private static FakeLocalizationService CreateLocalization()
    {
        var resources = new Dictionary<string, IDictionary<string, string>>
        {
            ["en"] = new Dictionary<string, string>
            {
                ["Module.Title.TrainingRecords"] = "Training Records",
                ["Module.Status.Ready"] = "Ready",
                ["Module.Status.Loading"] = "Loading {0}",
                ["Module.Status.Loaded"] = "Loaded {0}",
                ["Module.Status.Cancelled"] = "Cancelled",
                ["Module.TrainingRecords.Status.Initiated"] = "Initiated {0}",
                ["Module.TrainingRecords.Status.Assigned"] = "Assigned {0}",
                ["Module.TrainingRecords.Status.Approved"] = "Approved {0}",
                ["Module.TrainingRecords.Status.Completed"] = "Completed {0}",
                ["Module.TrainingRecords.Status.Closed"] = "Closed {0}",
                ["Module.TrainingRecords.Field.Type"] = "Type",
                ["Module.TrainingRecords.Field.AssignedTo"] = "Assigned To",
                ["Module.TrainingRecords.Field.DueDate"] = "Due",
                ["Module.TrainingRecords.Field.TrainingDate"] = "Training Date",
                ["Module.TrainingRecords.Field.ExpiryDate"] = "Expiry"
            }
        };

        return new FakeLocalizationService(resources, "en");
    }

    private static TrainingRecord CreateRecord(
        int id,
        string code,
        string status,
        string type,
        int? assigneeId = null)
        => new()
        {
            Id = id,
            Code = code,
            Title = code,
            Status = status,
            TrainingType = type,
            AssignedTo = assigneeId,
            TraineeId = assigneeId,
            AssignedToName = assigneeId?.ToString() ?? string.Empty,
            DueDate = DateTime.Today.AddDays(7),
            TrainingDate = DateTime.Today,
            ExpiryDate = DateTime.Today.AddMonths(6)
        };
}
