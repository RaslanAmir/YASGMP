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

public class WorkOrdersModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsWorkOrderThroughAdapter()
    {
        var database = new DatabaseService();
        var workOrders = new FakeWorkOrderCrudService();
        var auth = new TestAuthContext { CurrentUser = new User { Id = 9, FullName = "QA" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new WorkOrdersModuleViewModel(database, workOrders, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Title = "Replace gaskets";
        viewModel.Editor.Description = "Replace gaskets on pump";
        viewModel.Editor.TaskDescription = "Shutdown, lockout, replace";
        viewModel.Editor.Type = "MAINTENANCE";
        viewModel.Editor.Priority = "High";
        viewModel.Editor.Status = "OPEN";
        viewModel.Editor.MachineId = 12;
        viewModel.Editor.RequestedById = 9;
        viewModel.Editor.CreatedById = 9;
        viewModel.Editor.AssignedToId = 10;
        viewModel.Editor.Result = "Pending";
        viewModel.Editor.Notes = "Initial";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        var persisted = Assert.Single(workOrders.Saved);
        Assert.Equal("Replace gaskets", persisted.Title);
        Assert.Equal(12, persisted.MachineId);
        Assert.Equal(9, persisted.CreatedById);
        Assert.Equal("test-signature", persisted.DigitalSignature);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("work_orders", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        Assert.Single(signatureDialog.PersistedResults);
        var persistedSignature = signatureDialog.PersistedResults[0];
        Assert.Equal(workOrders.Saved[0].Id, persistedSignature.Signature.RecordId);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public async Task AttachDocumentCommand_UploadsAttachmentViaService()
    {
        var database = new DatabaseService();
        database.WorkOrders.Add(new WorkOrder
        {
            Id = 42,
            Title = "Inspect autoclave",
            Description = "Routine inspection",
            TaskDescription = "Visual check",
            Type = "MAINTENANCE",
            Priority = "Medium",
            Status = "OPEN",
            DateOpen = System.DateTime.UtcNow,
            RequestedById = 6,
            CreatedById = 6,
            AssignedToId = 7,
            MachineId = 5,
            Result = "Pending",
            Notes = "Initial"
        });

        var workOrders = new FakeWorkOrderCrudService();
        workOrders.Saved.AddRange(database.WorkOrders);

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 6, FullName = "Tech" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.25"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var bytes = Encoding.UTF8.GetBytes("work-order evidence");
        filePicker.Files = new[]
        {
            new PickedFile("evidence.txt", "text/plain", () => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)), bytes.Length)
        };

        var viewModel = new WorkOrdersModuleViewModel(database, workOrders, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));
        await viewModel.AttachDocumentCommand.ExecuteAsync(null);

        var upload = Assert.Single(attachments.Uploads);
        Assert.Equal("work_orders", upload.EntityType);
        Assert.Equal(42, upload.EntityId);
        Assert.Equal("evidence.txt", upload.FileName);
    }

    private static Task<bool> InvokeSaveAsync(WorkOrdersModuleViewModel viewModel)
    {
        var method = typeof(WorkOrdersModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(WorkOrdersModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }
}
