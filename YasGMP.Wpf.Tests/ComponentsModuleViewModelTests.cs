using System;
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
        var audit = new AuditService(database);
        var audit = new AuditService(database);
        var audit = new AuditService(database);
        var componentAdapter = new FakeComponentCrudService();
        var machineAdapter = new FakeMachineCrudService();

        _ = await machineAdapter.CreateAsync(new Machine
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
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ComponentsModuleViewModel(database, audit, componentAdapter, machineAdapter, auth, signatureDialog, dialog, shell, navigation);
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
        var context = Assert.Single(componentAdapter.SavedContexts);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("components", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        Assert.Single(signatureDialog.PersistedResults);
        var persistedSignature = signatureDialog.PersistedResults[0];
        Assert.Equal(componentAdapter.Saved[0].Id, persistedSignature.Signature.RecordId);
        Assert.Equal(signatureDialog.LastPersistedSignatureId, persistedSignature.Signature.Id);
        Assert.True(persistedSignature.Signature.Id > 0);
        var persistedMetadata = Assert.Single(signatureDialog.PersistedSignatureRecords);
        Assert.Equal(persistedSignature.Signature.Id, persistedMetadata.SignatureId);
        Assert.Equal(componentAdapter.Saved[0].Id, persistedMetadata.RecordId);
        Assert.Equal("test-signature", persistedMetadata.SignatureHash);
        Assert.Equal("password", persistedMetadata.Method);
        Assert.Equal("valid", persistedMetadata.Status);
        Assert.Equal("Automated test", persistedMetadata.Note);
    }

    [Fact]
    public async Task OnSaveAsync_AddMode_SignatureCancelled_LeavesEditorDirtyAndSkipsPersist()
    {
        var database = new DatabaseService();
        var componentAdapter = new FakeComponentCrudService();
        var machineAdapter = new FakeMachineCrudService();

        _ = await machineAdapter.CreateAsync(new Machine
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
        var signatureDialog = new TestElectronicSignatureDialogService();
        signatureDialog.QueueCancellation();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ComponentsModuleViewModel(database, audit, componentAdapter, machineAdapter, auth, signatureDialog, dialog, shell, navigation);
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

        Assert.False(saved);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.Equal("Electronic signature cancelled. Save aborted.", viewModel.StatusMessage);
        Assert.Empty(componentAdapter.Saved);
        Assert.Empty(signatureDialog.PersistedResults);
    }

    [Fact]
    public async Task OnSaveAsync_AddMode_SignatureCaptureThrows_SetsStatusAndSkipsPersist()
    {
        var database = new DatabaseService();
        var componentAdapter = new FakeComponentCrudService();
        var machineAdapter = new FakeMachineCrudService();

        _ = await machineAdapter.CreateAsync(new Machine
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
        var signatureDialog = new TestElectronicSignatureDialogService();
        signatureDialog.QueueCaptureException(new InvalidOperationException("Dialog offline"));
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ComponentsModuleViewModel(database, audit, componentAdapter, machineAdapter, auth, signatureDialog, dialog, shell, navigation);
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

        Assert.False(saved);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.Equal("Electronic signature failed: Dialog offline", viewModel.StatusMessage);
        Assert.Empty(componentAdapter.Saved);
        Assert.Empty(signatureDialog.PersistedResults);
    }

    private static Task<bool> InvokeSaveAsync(ComponentsModuleViewModel viewModel)
    {
        var method = typeof(ComponentsModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(ComponentsModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }
}
