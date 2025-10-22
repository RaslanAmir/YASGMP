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

public class CapaModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsCapaThroughAdapter()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        const int adapterSignatureId = 5120;
        var capaCrud = new FakeCapaCrudService
        {
            SignatureMetadataIdSource = _ => adapterSignatureId
        };
        var componentCrud = new FakeComponentCrudService();
        componentCrud.Saved.Add(new Component
        {
            Id = 1,
            Code = "CMP-001",
            Name = "Autoclave Valve"
        });
        var auth = new TestAuthContext { CurrentUser = new User { Id = 8, FullName = "QA Lead" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new CapaModuleViewModel(database, audit, capaCrud, componentCrud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Title = "Supplier qualification";
        viewModel.Editor.Description = "Address external audit finding.";
        viewModel.Editor.ComponentId = 1;
        viewModel.Editor.Priority = "High";
        viewModel.Editor.Status = "OPEN";
        viewModel.Editor.RootCause = "Missing annual review";
        viewModel.Editor.CorrectiveAction = "Perform vendor audit";
        viewModel.Editor.PreventiveAction = "Add to quality calendar";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.Equal("Electronic signature captured (QA Reason).", viewModel.StatusMessage);
        var persisted = Assert.Single(capaCrud.Saved);
        Assert.Equal("Supplier qualification", persisted.Title);
        Assert.Equal("High", persisted.Priority);
        Assert.Equal("test-signature", persisted.DigitalSignature);
        var context = Assert.Single(capaCrud.SavedContexts);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("capa_cases", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        var capturedResult = Assert.Single(signatureDialog.CapturedResults);
        var signatureResult = Assert.NotNull(capturedResult);
        Assert.Equal(capaCrud.Saved[0].Id, signatureResult.Signature.RecordId);
        Assert.Equal(adapterSignatureId, signatureResult.Signature.Id);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.Equal(0, signatureDialog.PersistInvocationCount);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public async Task WorkflowCommands_AdvanceLifecycleAndUpdateRecords()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var capaCrud = new FakeCapaCrudService();
        var componentCrud = new FakeComponentCrudService();
        componentCrud.Saved.Add(new Component { Id = 1, Code = "CMP-001", Name = "Autoclave Valve" });
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 21, FullName = "QA Analyst" },
            CurrentIpAddress = "10.0.0.25",
            CurrentDeviceInfo = "UnitTest"
        };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new CapaModuleViewModel(database, audit, capaCrud, componentCrud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        await viewModel.EnterAddModeCommand.ExecuteAsync(null);

        viewModel.Editor.Title = "Batch record review";
        viewModel.Editor.Description = "Investigate batch deviation";
        viewModel.Editor.ComponentId = 1;
        viewModel.Editor.Priority = "High";

        await viewModel.AddCommand.ExecuteAsync(null);

        Assert.Equal(FormMode.View, viewModel.Mode);
        Assert.Equal("ACTION_DEFINED", viewModel.Editor.Status);
        var creation = Assert.Single(capaCrud.TransitionHistory);
        Assert.Equal("Create", creation.Operation);
        Assert.Equal("ACTION_DEFINED", creation.Entity.Status);

        await viewModel.ApproveCommand.ExecuteAsync(null);
        Assert.Equal("ACTION_APPROVED", viewModel.Editor.Status);
        Assert.Equal(2, capaCrud.TransitionHistory.Count);
        Assert.Equal("ACTION_APPROVED", capaCrud.TransitionHistory[1].Entity.Status);

        await viewModel.ExecuteCommand.ExecuteAsync(null);
        Assert.Equal("ACTION_EXECUTED", viewModel.Editor.Status);
        Assert.Equal(3, capaCrud.TransitionHistory.Count);
        Assert.Equal("ACTION_EXECUTED", capaCrud.TransitionHistory[2].Entity.Status);

        await viewModel.CloseCommand.ExecuteAsync(null);
        Assert.Equal("CLOSED", viewModel.Editor.Status);
        Assert.Equal(FormMode.View, viewModel.Mode);
        Assert.Equal("CAPA closed.", viewModel.StatusMessage);

        Assert.Collection(capaCrud.TransitionHistory,
            entry => Assert.Equal("ACTION_DEFINED", entry.Entity.Status),
            entry => Assert.Equal("ACTION_APPROVED", entry.Entity.Status),
            entry => Assert.Equal("ACTION_EXECUTED", entry.Entity.Status),
            entry => Assert.Equal("CLOSED", entry.Entity.Status));

        var record = Assert.Single(viewModel.Records);
        Assert.Equal("CLOSED", record.Status);
        Assert.Equal(record.Key, viewModel.SelectedRecord?.Key);
    }

    [Fact]
    public async Task AttachDocumentCommand_UploadsAttachmentsViaService()
    {
        var database = new DatabaseService();
        var capaCrud = new FakeCapaCrudService();
        var componentCrud = new FakeComponentCrudService();
        componentCrud.Saved.Add(new Component
        {
            Id = 1,
            Code = "CMP-001",
            Name = "Autoclave Valve"
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 10, FullName = "Quality Manager" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.10"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        capaCrud.Seed(new CapaCase
        {
            Id = 100,
            Title = "Calibration CAPA",
            Description = "Follow up on calibration deviation.",
            ComponentId = 1,
            Priority = "Medium",
            Status = "OPEN",
            DateOpen = DateTime.UtcNow.AddDays(-5)
        });

        var bytes = Encoding.UTF8.GetBytes("capa attachment");
        filePicker.Files = new[]
        {
            new PickedFile("plan.txt", "text/plain", () => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)), bytes.Length)
        };

        var viewModel = new CapaModuleViewModel(database, audit, capaCrud, componentCrud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        await viewModel.EnterViewModeCommand.ExecuteAsync(null);

        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));
        await viewModel.AttachDocumentCommand.ExecuteAsync(null);

        var upload = Assert.Single(attachments.Uploads);
        Assert.Equal("capa_cases", upload.EntityType);
        Assert.Equal(100, upload.EntityId);
        Assert.Equal("plan.txt", upload.FileName);
    }

    private static Task<bool> InvokeSaveAsync(CapaModuleViewModel viewModel)
    {
        var method = typeof(CapaModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(CapaModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }
}
