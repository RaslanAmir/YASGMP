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
        var machineAdapter = new FakeMachineCrudService();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 7, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.25"
        };
        var signatureDialog = new FakeElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var viewModel = new AssetsModuleViewModel(database, machineAdapter, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
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
        Assert.False(viewModel.IsDirty);
        Assert.Single(machineAdapter.Saved);
        var persisted = machineAdapter.Saved[0];
        Assert.Equal("Lyophilizer", persisted.Name);
        Assert.Equal("maintenance", persisted.Status);
        Assert.Equal("URS-LYO-01", persisted.UrsDoc);
        Assert.Equal("test-signature", persisted.DigitalSignature);
        Assert.Equal(7, persisted.LastModifiedById);
        Assert.NotEqual(default, persisted.LastModified);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("machines", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
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
        var signatureDialog = new FakeElectronicSignatureDialogService();
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

        var viewModel = new AssetsModuleViewModel(database, machineAdapter, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));
        await viewModel.AttachDocumentCommand.ExecuteAsync(null);

        Assert.Single(attachments.Uploads);
        var upload = attachments.Uploads[0];
        Assert.Equal("machines", upload.EntityType);
        Assert.Equal(5, upload.EntityId);
        Assert.Equal("hello.txt", upload.FileName);
    }

    private static Task<bool> InvokeSaveAsync(AssetsModuleViewModel viewModel)
    {
        var method = typeof(AssetsModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(AssetsModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }
}
