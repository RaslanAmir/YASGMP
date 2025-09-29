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
        var capaCrud = new FakeCapaCrudService();
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

        var viewModel = new CapaModuleViewModel(database, capaCrud, componentCrud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
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
        var persisted = Assert.Single(capaCrud.Saved);
        Assert.Equal("Supplier qualification", persisted.Title);
        Assert.Equal("High", persisted.Priority);
        Assert.Equal("test-signature", persisted.DigitalSignature);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("capa_cases", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        Assert.Single(signatureDialog.PersistedResults);
        var persistedSignature = signatureDialog.PersistedResults[0];
        Assert.Equal(capaCrud.Saved[0].Id, persistedSignature.Signature.RecordId);
        Assert.False(viewModel.IsDirty);
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

        var viewModel = new CapaModuleViewModel(database, capaCrud, componentCrud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
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
