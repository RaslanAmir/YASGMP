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

public class WorkOrdersModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsWorkOrderThroughAdapter()
    {
        var database = new DatabaseService();
        var audit = new RecordingAuditService(database);
        var workOrders = new FakeWorkOrderCrudService();
        var auth = new TestAuthContext { CurrentUser = new User { Id = 9, FullName = "QA" } };
        auth.CurrentSessionId = "SESSION-123";
        auth.CurrentDeviceInfo = "UnitTestRig";
        auth.CurrentIpAddress = "10.0.0.42";
        var filePicker = new TestFilePicker();
        var attachmentService = new TestAttachmentService();
        var attachments = new AttachmentWorkflowService(attachmentService, database, new AttachmentEncryptionOptions(), audit);
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new WorkOrdersModuleViewModel(database, audit, workOrders, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
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
        var context = Assert.Single(workOrders.SavedContexts);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("work_orders", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        Assert.Single(signatureDialog.PersistedResults);
        var persistedSignature = signatureDialog.PersistedResults[0];
        Assert.Equal(workOrders.Saved[0].Id, persistedSignature.Signature.RecordId);
        Assert.Equal(signatureDialog.LastPersistedSignatureId, persistedSignature.Signature.Id);
        Assert.True(persistedSignature.Signature.Id > 0);
        var persistedMetadata = Assert.Single(signatureDialog.PersistedSignatureRecords);
        Assert.Equal(persistedSignature.Signature.Id, persistedMetadata.SignatureId);
        Assert.Equal(workOrders.Saved[0].Id, persistedMetadata.RecordId);
        Assert.Equal("test-signature", persistedMetadata.SignatureHash);
        Assert.Equal("password", persistedMetadata.Method);
        Assert.Equal("valid", persistedMetadata.Status);
        Assert.Equal("Automated test", persistedMetadata.Note);
        var auditEntry = Assert.Single(audit.EntityAudits);
        Assert.Equal("work_orders", auditEntry.Table);
        Assert.Equal(workOrders.Saved[0].Id, auditEntry.EntityId);
        Assert.Equal("CREATE", auditEntry.Action);
        Assert.Equal(
            "user=9, reason=QA Reason, status=OPEN, signature=test-signature, method=password, outcome=valid, ip=10.0.0.42, device=UnitTestRig, session=SESSION-123",
            auditEntry.Details);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public async Task OnSaveAsync_AddMode_SignatureCancelled_LeavesWorkOrderUnsaved()
    {
        var database = new DatabaseService();
        var audit = new RecordingAuditService(database);
        var workOrders = new FakeWorkOrderCrudService();
        var auth = new TestAuthContext { CurrentUser = new User { Id = 9, FullName = "QA" } };
        var filePicker = new TestFilePicker();
        var attachmentService = new TestAttachmentService();
        var attachments = new AttachmentWorkflowService(attachmentService, database, new AttachmentEncryptionOptions(), audit);
        var signatureDialog = new TestElectronicSignatureDialogService();
        signatureDialog.QueueCancellation();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new WorkOrdersModuleViewModel(database, audit, workOrders, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
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

        Assert.False(saved);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.Equal("Electronic signature cancelled. Save aborted.", viewModel.StatusMessage);
        Assert.Empty(workOrders.Saved);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.Empty(audit.EntityAudits);
    }

    [Fact]
    public async Task OnSaveAsync_AddMode_SignatureCaptureThrows_SetsStatusAndSkipsPersist()
    {
        var database = new DatabaseService();
        var audit = new RecordingAuditService(database);
        var workOrders = new FakeWorkOrderCrudService();
        var auth = new TestAuthContext { CurrentUser = new User { Id = 9, FullName = "QA" } };
        var filePicker = new TestFilePicker();
        var attachmentService = new TestAttachmentService();
        var attachments = new AttachmentWorkflowService(attachmentService, database, new AttachmentEncryptionOptions(), audit);
        var signatureDialog = new TestElectronicSignatureDialogService();
        signatureDialog.QueueCaptureException(new InvalidOperationException("Dialog offline"));
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new WorkOrdersModuleViewModel(database, audit, workOrders, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
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

        Assert.False(saved);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.Equal("Electronic signature failed: Dialog offline", viewModel.StatusMessage);
        Assert.Empty(workOrders.Saved);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.Empty(audit.EntityAudits);
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

        var audit = new RecordingAuditService(database);
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
        var attachmentService = new TestAttachmentService();
        var attachments = new AttachmentWorkflowService(attachmentService, database, new AttachmentEncryptionOptions(), audit);

        var bytes = Encoding.UTF8.GetBytes("work-order evidence");
        filePicker.Files = new[]
        {
            new PickedFile("evidence.txt", "text/plain", () => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)), bytes.Length)
        };

        var viewModel = new WorkOrdersModuleViewModel(database, audit, workOrders, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));
        await viewModel.AttachDocumentCommand.ExecuteAsync(null);

        var upload = Assert.Single(attachmentService.Uploads);
        Assert.Equal("work_orders", upload.EntityType);
        Assert.Equal(42, upload.EntityId);
        Assert.Equal("evidence.txt", upload.FileName);
    }

    [Fact]
    public async Task AttachDocumentCommand_RecordsAuditEventForUpload()
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

        var audit = new RecordingAuditService(database);
        var workOrders = new FakeWorkOrderCrudService();
        workOrders.Saved.AddRange(database.WorkOrders);

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 6, FullName = "Tech" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.25",
            CurrentSessionId = "SESSION-UPLOAD"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var bytes = Encoding.UTF8.GetBytes("work-order evidence");
        filePicker.Files = new[]
        {
            new PickedFile("evidence.txt", "text/plain", () => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)), bytes.Length)
        };

        var attachmentService = new TestAttachmentService();
        var attachments = new AttachmentWorkflowService(attachmentService, database, new AttachmentEncryptionOptions(), audit);

        var viewModel = new WorkOrdersModuleViewModel(database, audit, workOrders, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));
        await viewModel.AttachDocumentCommand.ExecuteAsync(null);

        var upload = Assert.Single(attachmentService.Uploads);
        var auditEntry = Assert.Single(audit.AttachmentAudits);
        Assert.Equal(upload.EntityType, auditEntry.EntityType);
        Assert.Equal(upload.EntityId, auditEntry.EntityId);
        Assert.Equal(auth.CurrentUser?.Id, auditEntry.ActorUserId);
        Assert.Equal(1, auditEntry.AttachmentId);
        Assert.Contains("actor=6", auditEntry.Description);
        Assert.Contains("entity=work_orders:42", auditEntry.Description);
        Assert.Contains("dedup=new", auditEntry.Description);
    }

    private static Task<bool> InvokeSaveAsync(WorkOrdersModuleViewModel viewModel)
    {
        var method = typeof(WorkOrdersModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(WorkOrdersModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }
}
