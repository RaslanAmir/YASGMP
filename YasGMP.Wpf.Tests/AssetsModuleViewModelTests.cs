using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class AssetsModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsMachineThroughAdapter()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var audit = new AuditService(database);
        var audit = new AuditService(database);
        var audit = new AuditService(database);
        const int adapterSignatureId = 4321;
        var machineAdapter = new FakeMachineCrudService
        {
            SignatureMetadataIdSource = _ => adapterSignatureId
        };
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 7, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.25"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var viewModel = new AssetsModuleViewModel(database, audit, machineAdapter, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Code = "AST-500";
        viewModel.Editor.Name = "Lyophilizer";
        viewModel.Editor.Description = "Freeze dryer";
        viewModel.Editor.Manufacturer = "Contoso";
        viewModel.Editor.Model = "LX-10";
        viewModel.Editor.Location = "Suite A";
        viewModel.Editor.Status = "maintenance";
        viewModel.Editor.UrsDoc = "URS-LYO-01";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.Equal("Electronic signature captured (QA Reason).", viewModel.StatusMessage);
        Assert.False(viewModel.IsDirty);
        Assert.Single(machineAdapter.Saved);
        var persisted = machineAdapter.Saved[0];
        Assert.Equal("Lyophilizer", persisted.Name);
        Assert.Equal("maintenance", persisted.Status);
        Assert.Equal("URS-LYO-01", persisted.UrsDoc);
        Assert.Equal("test-signature", persisted.DigitalSignature);
        Assert.Equal(7, persisted.LastModifiedById);
        Assert.NotEqual(default, persisted.LastModified);
        var context = Assert.Single(machineAdapter.SavedContexts);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("machines", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        var capturedResult = Assert.Single(signatureDialog.CapturedResults);
        var signatureResult = Assert.NotNull(capturedResult);
        Assert.Equal(machineAdapter.Saved[0].Id, signatureResult.Signature.RecordId);
        Assert.Equal(adapterSignatureId, signatureResult.Signature.Id);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.Equal(0, signatureDialog.PersistInvocationCount);
    }

    [Fact]
    public async Task OnSaveAsync_AddMode_SignatureCancelled_StaysInEditModeAndSkipsPersist()
    {
        var database = new DatabaseService();
        var machineAdapter = new FakeMachineCrudService();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 7, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.25"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        signatureDialog.QueueCancellation();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var viewModel = new AssetsModuleViewModel(database, audit, machineAdapter, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Code = "AST-500";
        viewModel.Editor.Name = "Lyophilizer";
        viewModel.Editor.Description = "Freeze dryer";
        viewModel.Editor.Manufacturer = "Contoso";
        viewModel.Editor.Model = "LX-10";
        viewModel.Editor.Location = "Suite A";
        viewModel.Editor.Status = "maintenance";
        viewModel.Editor.UrsDoc = "URS-LYO-01";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.False(saved);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.Equal("Electronic signature cancelled. Save aborted.", viewModel.StatusMessage);
        Assert.Empty(machineAdapter.Saved);
        Assert.Empty(signatureDialog.PersistedResults);
    }

    [Fact]
    public async Task OnSaveAsync_AddMode_SignatureCaptureThrows_SurfacesStatusAndSkipsPersist()
    {
        var database = new DatabaseService();
        var machineAdapter = new FakeMachineCrudService();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 7, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.25"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        signatureDialog.QueueCaptureException(new InvalidOperationException("Dialog offline"));
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var viewModel = new AssetsModuleViewModel(database, audit, machineAdapter, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Code = "AST-500";
        viewModel.Editor.Name = "Lyophilizer";
        viewModel.Editor.Description = "Freeze dryer";
        viewModel.Editor.Manufacturer = "Contoso";
        viewModel.Editor.Model = "LX-10";
        viewModel.Editor.Location = "Suite A";
        viewModel.Editor.Status = "maintenance";
        viewModel.Editor.UrsDoc = "URS-LYO-01";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.False(saved);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.Equal("Electronic signature failed: Dialog offline", viewModel.StatusMessage);
        Assert.Empty(machineAdapter.Saved);
        Assert.Empty(signatureDialog.PersistedResults);
    }

    [Fact]
    public async Task InitializeAsync_TargetId_SelectsRecordAndAppliesGoldenArrowFilter()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var machineAdapter = new FakeMachineCrudService();
        machineAdapter.Saved.AddRange(new[]
        {
            new Machine
            {
                Id = 101,
                Code = "AST-101",
                Name = "Buffer Tank",
                Status = "active",
                Manufacturer = "Fabrikam",
                Location = "Suite 100"
            },
            new Machine
            {
                Id = 202,
                Code = "AST-202",
                Name = "Filling Line",
                Status = "maintenance",
                Manufacturer = "Contoso",
                Location = "Suite 200"
            }
        });

        var target = new Machine
        {
            Id = 303,
            Code = "AST-303",
            Name = "Bioreactor",
            Status = "active",
            Manufacturer = "Tailspin",
            Location = "Suite 300"
        };
        machineAdapter.Saved.Add(target);

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var viewModel = new AssetsModuleViewModel(database, audit, machineAdapter, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        await viewModel.InitializeAsync(target.Id);

        Assert.Equal(target.Id.ToString(), viewModel.SelectedRecord?.Key);
        Assert.Equal(target.Name, viewModel.SearchText);
        Assert.Equal(FormMode.View, viewModel.Mode);
        Assert.Equal("Filtered Assets by \"Bioreactor\".", viewModel.StatusMessage);
    }

    [Fact]
    public async Task AttachDocumentCommand_UploadsAttachmentViaService()
    {
        var database = new DatabaseService();
        var machineAdapter = new FakeMachineCrudService();
        machineAdapter.Saved.Add(new Machine
        {
            Id = 5,
            Code = "M-100",
            Name = "Mixer",
            Manufacturer = "Globex",
            Location = "Suite 2",
            UrsDoc = "URS-MIX-01"
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 12, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.42"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var bytes = Encoding.UTF8.GetBytes("hello asset");
        filePicker.Files = new[]
        {
            new PickedFile("hello.txt", "text/plain", () => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)), bytes.Length)
        };

        var viewModel = new AssetsModuleViewModel(database, audit, machineAdapter, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));
        await viewModel.AttachDocumentCommand.ExecuteAsync(null);

        Assert.Single(attachments.Uploads);
        var upload = attachments.Uploads[0];
        Assert.Equal("machines", upload.EntityType);
        Assert.Equal(5, upload.EntityId);
        Assert.Equal("hello.txt", upload.FileName);
    }

    [Fact]
    public async Task OnActivatedAsync_FromEditMode_ReselectsRecordAndRefreshesAttachments()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var machineAdapter = new FakeMachineCrudService();
        machineAdapter.Saved.Add(new Machine
        {
            Id = 5,
            Code = "M-100",
            Name = "Mixer",
            Manufacturer = "Globex",
            Location = "Suite 2",
            UrsDoc = "URS-MIX-01"
        });

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var viewModel = new AssetsModuleViewModel(database, audit, machineAdapter, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);

        await viewModel.InitializeAsync(5);
        await Task.Yield();

        Assert.Equal("Mixer", viewModel.Editor.Name);
        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));

        var canExecuteRaised = 0;
        viewModel.AttachDocumentCommand.CanExecuteChanged += (_, _) => canExecuteRaised++;

        viewModel.Mode = FormMode.Update;
        Assert.True(viewModel.IsEditorEnabled);
        Assert.False(viewModel.AttachDocumentCommand.CanExecute(null));

        canExecuteRaised = 0;

        machineAdapter.Saved[0].Name = "Mixer Reloaded";
        machineAdapter.Saved[0].Location = "Suite 9";

        await viewModel.InitializeAsync(5);
        await Task.Yield();

        Assert.Equal(FormMode.View, viewModel.Mode);
        Assert.False(viewModel.IsEditorEnabled);
        Assert.Equal("Mixer Reloaded", viewModel.Editor.Name);
        Assert.Equal("Suite 9", viewModel.Editor.Location);
        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));
        Assert.True(canExecuteRaised > 0);
    }

    private static Task<bool> InvokeSaveAsync(AssetsModuleViewModel viewModel)
    {
        var method = typeof(AssetsModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(AssetsModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }
}
