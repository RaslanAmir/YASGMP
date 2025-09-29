using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class ComponentsModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsComponentViaAdapter()
    {
        var database = new DatabaseService();
        var componentAdapter = new FakeComponentCrudService();
        var machineAdapter = new FakeMachineCrudService();

        await machineAdapter.CreateAsync(new Machine
        {
            Id = 1,
            Code = "AST-001",
            Name = "Autoclave",
            Manufacturer = "Steris",
            Location = "Suite A",
            UrsDoc = "URS-AUTO"
        }, MachineCrudContext.Create(1, "127.0.0.1", "TestRig", "unit"));

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 5, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.8"
        };
        var signatureDialog = new FakeElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ComponentsModuleViewModel(database, componentAdapter, machineAdapter, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.MachineId = machineAdapter.Saved[0].Id;
        viewModel.Editor.Code = "CMP-100";
        viewModel.Editor.Name = "Pressure Sensor";
        viewModel.Editor.Type = "sensor";
        viewModel.Editor.SopDoc = "SOP-CMP-100";
        viewModel.Editor.Status = "active";
        viewModel.Editor.SerialNumber = "SN-001";
        viewModel.Editor.Supplier = "Contoso";
        viewModel.Editor.Comments = "Initial install";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.Single(componentAdapter.Saved);
        var persisted = componentAdapter.Saved[0];
        Assert.Equal("Pressure Sensor", persisted.Name);
        Assert.Equal("CMP-100", persisted.Code);
        Assert.Equal(machineAdapter.Saved[0].Id, persisted.MachineId);
        Assert.Equal("SOP-CMP-100", persisted.SopDoc);
        Assert.Equal("test-signature", persisted.DigitalSignature);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("components", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        Assert.Single(signatureDialog.PersistedResults);
        var persistedSignature = signatureDialog.PersistedResults[0];
        Assert.Equal(componentAdapter.Saved[0].Id, persistedSignature.Signature.RecordId);
    }

    private static Task<bool> InvokeSaveAsync(ComponentsModuleViewModel viewModel)
    {
        var method = typeof(ComponentsModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(ComponentsModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }
}
