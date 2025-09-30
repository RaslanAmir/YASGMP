using System;
using System.IO;
using System.Linq;
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

public class ChangeControlModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsChangeControl()
    {
        var database = new DatabaseService();
        var crud = new FakeChangeControlCrudService();
        var auth = new TestAuthContext { CurrentUser = new User { Id = 7, FullName = "Quality Lead" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ChangeControlModuleViewModel(database, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Title = "Validate HVAC filters";
        viewModel.Editor.Code = "CC-UNIT-100";
        viewModel.Editor.Status = "Draft";
        viewModel.Editor.Description = "Ensure replacement schedule matches SOP.";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        var created = Assert.Single(crud.Saved);
        Assert.Equal("Validate HVAC filters", created.Title);
        Assert.Equal("CC-UNIT-100", created.Code);
        Assert.True(created.LastModified.HasValue);
        var context = Assert.Single(crud.SavedContexts);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("change_controls", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        Assert.Single(signatureDialog.PersistedResults);
        var persistedSignature = signatureDialog.PersistedResults[0];
        Assert.Equal(crud.Saved[0].Id, persistedSignature.Signature.RecordId);
        Assert.Equal(signatureDialog.LastPersistedSignatureId, persistedSignature.Signature.Id);
        Assert.True(persistedSignature.Signature.Id > 0);
        var persistedMetadata = Assert.Single(signatureDialog.PersistedSignatureRecords);
        Assert.Equal(persistedSignature.Signature.Id, persistedMetadata.SignatureId);
        Assert.Equal(crud.Saved[0].Id, persistedMetadata.RecordId);
        Assert.Equal("test-signature", persistedMetadata.SignatureHash);
        Assert.Equal("password", persistedMetadata.Method);
        Assert.Equal("valid", persistedMetadata.Status);
        Assert.Equal("Automated test", persistedMetadata.Note);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public async Task Cancel_RestoresSnapshotWhenEditing()
    {
        var database = new DatabaseService();
        var crud = new FakeChangeControlCrudService();
        crud.Seed(new ChangeControl
        {
            Id = 10,
            Code = "CC-2024-010",
            Title = "Update cleaning procedure",
            StatusRaw = "UnderReview",
            Description = "QA requested tweak to detergent." 
        });

        var auth = new TestAuthContext { CurrentUser = new User { Id = 3 } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ChangeControlModuleViewModel(database, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        viewModel.Mode = FormMode.Update;

        viewModel.Editor.Title = "Edited title";
        Assert.True(viewModel.IsDirty);

        viewModel.CancelCommand.Execute(null);

        Assert.Equal("Update cleaning procedure", viewModel.Editor.Title);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public async Task AttachDocumentCommand_UploadsAttachment()
    {
        var database = new DatabaseService();
        var crud = new FakeChangeControlCrudService();
        crud.Seed(new ChangeControl
        {
            Id = 21,
            Code = "CC-2024-021",
            Title = "Replace gasket material",
            StatusRaw = "Draft"
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 9 },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "192.168.10.15"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var bytes = Encoding.UTF8.GetBytes("change control attachment");
        filePicker.Files = new[]
        {
            new PickedFile("impact.txt", "text/plain", () => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)), bytes.Length)
        };

        var viewModel = new ChangeControlModuleViewModel(database, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));

        await viewModel.AttachDocumentCommand.ExecuteAsync(null);

        var request = Assert.Single(attachments.Uploads);
        Assert.Equal("change_controls", request.EntityType);
        Assert.Equal(21, request.EntityId);
        Assert.Equal("impact.txt", request.FileName);
    }

    private static Task<bool> InvokeSaveAsync(ChangeControlModuleViewModel viewModel)
    {
        var method = typeof(ChangeControlModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(ChangeControlModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }
}
