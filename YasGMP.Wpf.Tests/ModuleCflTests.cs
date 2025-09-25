using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class ModuleCflTests
{
    [Fact]
    public async Task AssetsModule_ShowCflCommand_UsesLookupAndUpdatesSelection()
    {
        var db = new DatabaseService();
        db.Assets.Add(new Asset
        {
            Id = 123,
            Name = "Autoclave",
            Code = "AST-001",
            Description = "Steam autoclave",
            Status = "Active",
            Location = "Suite 1",
            Model = "Model X",
            Manufacturer = "Steris",
            InstallDate = DateTime.UtcNow
        });

        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var machineCrud = new FakeMachineCrudService();
        machineCrud.Saved.AddRange(db.Assets.Select(a => new Machine
        {
            Id = a.Id,
            Name = a.Name,
            Code = a.Code ?? string.Empty,
            Description = a.Description,
            Manufacturer = a.Manufacturer,
            Location = a.Location,
            Status = a.Status,
            InstallDate = a.InstallDate
        }));
        var auth = new TestAuthContext();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var viewModel = new AssetsModuleViewModel(db, machineCrud, auth, filePicker, attachments, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        Assert.NotEmpty(viewModel.Records);

        dialog.Callback = request =>
        {
            Assert.Equal("Select Asset", request.Title);
            Assert.NotEmpty(request.Items);
            var first = request.Items.First();
            Assert.Equal("Autoclave", first.Label);
            Assert.Contains("AST-001", first.Description, StringComparison.OrdinalIgnoreCase);
            return new CflResult(first);
        };

        await viewModel.ExecuteShowCflAsync(dialog);

        Assert.Equal("Autoclave", viewModel.SearchText);
        Assert.Equal("123", viewModel.SelectedRecord?.Key);
        Assert.Equal("Filtered Assets by \"Autoclave\".", viewModel.StatusMessage);
    }

    [Fact]
    public async Task WorkOrdersModule_ShowCflCommand_UsesLookupAndUpdatesSelection()
    {
        var db = new DatabaseService();
        db.WorkOrders.Add(new WorkOrder
        {
            Id = 77,
            Title = "Preventive maintenance",
            Description = "Monthly PM",
            TaskDescription = "Check gaskets",
            Type = "PM",
            Priority = "High",
            Status = "Open",
            DateOpen = DateTime.UtcNow.AddDays(-1),
            DueDate = DateTime.UtcNow.AddDays(7),
            RequestedById = 1,
            CreatedById = 2,
            AssignedToId = 3,
            MachineId = 123,
            Result = "Pending",
            Notes = "Notes",
            Machine = new Machine { Id = 123, Name = "Autoclave" },
            AssignedTo = new User { FullName = "Technician" }
        });

        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new WorkOrdersModuleViewModel(db, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        Assert.NotEmpty(viewModel.Records);

        dialog.Callback = request =>
        {
            Assert.Equal("Select Work Order", request.Title);
            var first = Assert.Single(request.Items);
            Assert.Equal("Preventive maintenance", first.Label);
            return new CflResult(first);
        };

        await viewModel.ExecuteShowCflAsync(dialog);

        Assert.Equal("Preventive maintenance", viewModel.SearchText);
        Assert.Equal("77", viewModel.SelectedRecord?.Key);
        Assert.Equal("Filtered Work Orders by \"Preventive maintenance\".", viewModel.StatusMessage);
    }

    [Fact]
    public async Task CalibrationModule_ShowCflCommand_UsesLookupAndUpdatesSelection()
    {
        var db = new DatabaseService();
        db.Calibrations.Add(new Calibration
        {
            Id = 555,
            ComponentId = 123,
            SupplierId = 42,
            CalibrationDate = DateTime.UtcNow.AddDays(-10),
            NextDue = DateTime.UtcNow.AddDays(20),
            CertDoc = "CERT-555",
            Result = "Passed",
            Comment = "All good"
        });

        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new CalibrationModuleViewModel(db, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        Assert.NotEmpty(viewModel.Records);

        dialog.Callback = request =>
        {
            Assert.Equal("Select Calibration", request.Title);
            var first = Assert.Single(request.Items);
            Assert.Equal("Calibration #555", first.Label);
            Assert.Contains("CERT-555", first.Description, StringComparison.OrdinalIgnoreCase);
            return new CflResult(first);
        };

        await viewModel.ExecuteShowCflAsync(dialog);

        Assert.Equal("Calibration #555", viewModel.SearchText);
        Assert.Equal("555", viewModel.SelectedRecord?.Key);
        Assert.Equal("Filtered Calibration by \"Calibration #555\".", viewModel.StatusMessage);
    }

    private sealed class TestCflDialogService : ICflDialogService
    {
        public Func<CflRequest, CflResult?>? Callback { get; set; }

        public Task<CflResult?> ShowAsync(CflRequest request)
            => Task.FromResult(Callback?.Invoke(request));
    }

    private sealed class TestShellInteractionService : IShellInteractionService
    {
        public void UpdateStatus(string message)
        {
        }

        public void UpdateInspector(InspectorContext context)
        {
        }
    }

    private sealed class TestModuleNavigationService : IModuleNavigationService
    {
        public ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null)
            => throw new NotSupportedException();

        public void Activate(ModuleDocumentViewModel document)
        {
        }
    }
}
