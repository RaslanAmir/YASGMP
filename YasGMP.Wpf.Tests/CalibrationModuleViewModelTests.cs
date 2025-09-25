using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class CalibrationModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsCalibration()
    {
        var database = new DatabaseService();
        database.Suppliers.Add(new Supplier { Id = 7, Name = "Metrologix" });

        var calibrationAdapter = new FakeCalibrationCrudService();
        var componentAdapter = new FakeComponentCrudService();

        await componentAdapter.CreateAsync(new Component
        {
            Id = 3,
            MachineId = 1,
            MachineName = "Autoclave",
            Code = "CMP-3",
            Name = "Pressure Sensor",
            SopDoc = "SOP-CAL-001",
            Status = "active"
        }, ComponentCrudContext.Create(1, "127.0.0.1", "Unit", "session"));

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 9, FullName = "Metrology" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.5"
        };
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new CalibrationModuleViewModel(
            database,
            calibrationAdapter,
            componentAdapter,
            auth,
            dialog,
            shell,
            navigation);

        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.ComponentId = componentAdapter.Saved[0].Id;
        viewModel.Editor.SupplierId = database.Suppliers[0].Id;
        viewModel.Editor.CalibrationDate = new DateTime(2025, 1, 10);
        viewModel.Editor.NextDue = new DateTime(2025, 7, 10);
        viewModel.Editor.Result = "PASS";
        viewModel.Editor.CertDoc = "CERT-001.pdf";
        viewModel.Editor.Comment = "Initial calibration";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.Single(calibrationAdapter.Saved);
        var persisted = calibrationAdapter.Saved[0];
        Assert.Equal(componentAdapter.Saved[0].Id, persisted.ComponentId);
        Assert.Equal(database.Suppliers[0].Id, persisted.SupplierId);
        Assert.Equal("PASS", persisted.Result);
        Assert.Equal("CERT-001.pdf", persisted.CertDoc);
    }

    private static Task<bool> InvokeSaveAsync(CalibrationModuleViewModel viewModel)
    {
        var method = typeof(CalibrationModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(CalibrationModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }

    private sealed class TestCflDialogService : ICflDialogService
    {
        public Task<CflResult?> ShowAsync(CflRequest request) => Task.FromResult<CflResult?>(null);
    }

    private sealed class TestShellInteractionService : IShellInteractionService
    {
        public void UpdateStatus(string message) { }

        public void UpdateInspector(InspectorContext context) { }
    }

    private sealed class TestModuleNavigationService : IModuleNavigationService
    {
        public ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null)
            => throw new NotSupportedException();

        public void Activate(ModuleDocumentViewModel document) { }
    }
}
