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

public class ValidationsModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsValidation()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var audit = new AuditService(database);
        var audit = new AuditService(database);
        const int adapterSignatureId = 2890;
        var crud = new FakeValidationCrudService
        {
            SignatureMetadataIdSource = _ => adapterSignatureId
        };
        var auth = new TestAuthContext { CurrentUser = new User { Id = 4 } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ValidationsModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Code = "VAL-100";
        viewModel.Editor.Type = "IQ";
        viewModel.Editor.MachineId = 5;
        viewModel.Editor.DateStart = DateTime.UtcNow.Date;

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.Equal("Electronic signature captured (QA Reason).", viewModel.StatusMessage);
        var created = Assert.Single(crud.Saved);
        Assert.Equal("VAL-100", created.Code);
        Assert.Equal("IQ", created.Type);
        Assert.Equal("test-signature", created.DigitalSignature);
        var context = Assert.Single(crud.SavedContexts);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("validations", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        var capturedResult = Assert.Single(signatureDialog.CapturedResults);
        var signatureResult = Assert.NotNull(capturedResult);
        Assert.Equal(crud.Saved[0].Id, signatureResult.Signature.RecordId);
        Assert.Equal(adapterSignatureId, signatureResult.Signature.Id);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.Equal(0, signatureDialog.PersistInvocationCount);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public async Task Cancel_RevertsEditorWhenUpdating()
    {
        var database = new DatabaseService();
        var crud = new FakeValidationCrudService();
        crud.Seed(new Validation
        {
            Id = 10,
            Code = "VAL-10",
            Type = "OQ",
            MachineId = 7,
            DateStart = DateTime.UtcNow.Date.AddDays(-3),
            Status = "Draft"
        });

        var auth = new TestAuthContext { CurrentUser = new User { Id = 9 } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ValidationsModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        viewModel.Mode = FormMode.Update;

        viewModel.Editor.Comment = "Edited comment";
        Assert.True(viewModel.IsDirty);

        viewModel.CancelCommand.Execute(null);

        Assert.Equal(string.Empty, viewModel.Editor.Comment);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public async Task AttachDocumentCommand_UploadsAttachment()
    {
        var database = new DatabaseService();
        var crud = new FakeValidationCrudService();
        crud.Seed(new Validation
        {
            Id = 21,
            Code = "VAL-021",
            Type = "PQ",
            MachineId = 3,
            DateStart = DateTime.UtcNow.Date,
            Status = "InProgress"
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 11 },
            CurrentIpAddress = "10.0.0.21",
            CurrentDeviceInfo = "UnitTest"
        };

        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var attachments = new TestAttachmentService();
        var filePicker = new TestFilePicker();

        var bytes = Encoding.UTF8.GetBytes("validation attachment");
        filePicker.Files = new[]
        {
            new PickedFile("protocol.pdf", "application/pdf", () => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)), bytes.Length)
        };

        var viewModel = new ValidationsModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));

        await viewModel.AttachDocumentCommand.ExecuteAsync(null);

        var upload = Assert.Single(attachments.Uploads);
        Assert.Equal("validations", upload.EntityType);
        Assert.Equal(21, upload.EntityId);
        Assert.Equal("protocol.pdf", upload.FileName);
    }

    private static Task<bool> InvokeSaveAsync(ValidationsModuleViewModel viewModel)
    {
        var method = typeof(ValidationsModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(ValidationsModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }
}
