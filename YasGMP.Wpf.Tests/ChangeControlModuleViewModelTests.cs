using System;
using System.Collections.Generic;
using System.Globalization;
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
using YasGMP.Models.Enums;

namespace YasGMP.Wpf.Tests;

public class ChangeControlModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsChangeControl()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        const int adapterSignatureId = 7813;
        var crud = new FakeChangeControlCrudService
        {
            SignatureMetadataIdSource = _ => adapterSignatureId
        };
        var auth = new TestAuthContext { CurrentUser = new User { Id = 7, FullName = "Quality Lead" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ChangeControlModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Title = "Validate HVAC filters";
        viewModel.Editor.Code = "CC-UNIT-100";
        viewModel.Editor.Status = "Draft";
        viewModel.Editor.Description = "Ensure replacement schedule matches SOP.";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.Equal("Electronic signature captured (QA Reason).", viewModel.StatusMessage);
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
        var capturedResult = Assert.Single(signatureDialog.CapturedResults);
        var signatureResult = Assert.NotNull(capturedResult);
        Assert.Equal(crud.Saved[0].Id, signatureResult.Signature.RecordId);
        Assert.Equal(adapterSignatureId, signatureResult.Signature.Id);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.Equal(0, signatureDialog.PersistInvocationCount);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public async Task SaveCommand_SignatureCancelled_ShowsWarningAndReenables()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var crud = new FakeChangeControlCrudService();
        var auth = new TestAuthContext { CurrentUser = new User { Id = 40, FullName = "QA Lead" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = TestElectronicSignatureDialogService.CreateCancelled();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ChangeControlModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        await viewModel.EnterAddModeCommand.ExecuteAsync(null);

        viewModel.Editor.Title = "Upgrade HVAC filters";
        viewModel.Editor.Code = "CC-2025-010";
        viewModel.Editor.Description = "Update HEPA filter replacement cadence.";

        Assert.True(viewModel.SaveCommand.CanExecute(null));

        var saveTask = InvokeSaveCommandAsync(viewModel);
        await Task.Yield();

        Assert.False(viewModel.SaveCommand.CanExecute(null));

        var saved = await saveTask;

        Assert.False(saved);
        Assert.Equal("Electronic signature cancelled. Save aborted.", viewModel.StatusMessage);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.True(viewModel.SaveCommand.CanExecute(null));
        Assert.Empty(crud.Saved);
    }

    [Fact]
    public async Task SaveCommand_SignatureFailure_ShowsErrorAndRestoresState()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var crud = new FakeChangeControlCrudService();
        var auth = new TestAuthContext { CurrentUser = new User { Id = 27, FullName = "QA Reviewer" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = TestElectronicSignatureDialogService.CreateCaptureException(new InvalidOperationException("QA credentials rejected."));
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ChangeControlModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        await viewModel.EnterAddModeCommand.ExecuteAsync(null);

        viewModel.Editor.Title = "Revise SOP";
        viewModel.Editor.Code = "CC-REV-001";
        viewModel.Editor.Description = "Update cleaning SOP per audit.";

        Assert.True(viewModel.SaveCommand.CanExecute(null));

        var saveTask = InvokeSaveCommandAsync(viewModel);
        await Task.Yield();

        Assert.False(viewModel.SaveCommand.CanExecute(null));

        var saved = await saveTask;

        Assert.False(saved);
        Assert.Equal("Electronic signature failed: QA credentials rejected.", viewModel.StatusMessage);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.True(viewModel.SaveCommand.CanExecute(null));
        Assert.Empty(crud.Saved);
    }

    [Fact]
    public async Task AddCommand_WhenValidationFails_ShowsErrorAndBlocksTransition()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var crud = new FakeChangeControlCrudService
        {
            NextValidationException = new InvalidOperationException("Change control must include a code.")
        };
        var auth = new TestAuthContext { CurrentUser = new User { Id = 12, FullName = "QA Planner" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = TestElectronicSignatureDialogService.CreateConfirmed();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ChangeControlModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        await viewModel.EnterAddModeCommand.ExecuteAsync(null);

        viewModel.Editor.Title = "Line modification";
        viewModel.Editor.Description = "Modify fill line sequence.";

        Assert.True(viewModel.AddCommand.CanExecute(null));

        await viewModel.AddCommand.ExecuteAsync(null);

        Assert.Equal("Failed to add change control: Change control must include a code.", viewModel.StatusMessage);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.True(viewModel.AddCommand.CanExecute(null));
        Assert.Empty(crud.Saved);
    }

    [Fact]
    public async Task ApproveCommand_WhenValidationFails_ShowsErrorAndKeepsStatus()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var crud = new FakeChangeControlCrudService();
        var auth = new TestAuthContext { CurrentUser = new User { Id = 33, FullName = "QA Approver" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = TestElectronicSignatureDialogService.CreateConfirmed();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ChangeControlModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        await viewModel.EnterAddModeCommand.ExecuteAsync(null);

        viewModel.Editor.Title = "Validate CIP cycle";
        viewModel.Editor.Code = "CC-CIP-002";
        viewModel.Editor.Description = "Introduce new CIP detergents.";

        await viewModel.AddCommand.ExecuteAsync(null);
        Assert.Equal(ChangeControlStatus.Submitted.ToString(), viewModel.Editor.Status);

        crud.NextValidationException = new InvalidOperationException("Approval requires QA review minutes.");

        Assert.True(viewModel.ApproveCommand.CanExecute(null));

        await viewModel.ApproveCommand.ExecuteAsync(null);

        Assert.Equal("Failed to update change control: Approval requires QA review minutes.", viewModel.StatusMessage);
        Assert.Equal(ChangeControlStatus.Submitted.ToString(), viewModel.Editor.Status);
        Assert.True(viewModel.ApproveCommand.CanExecute(null));
        Assert.Equal(1, crud.TransitionHistory.Count);
    }

    [Fact]
    public async Task SaveCommand_UpdateWithoutSelection_ShowsSelectionWarning()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var crud = new FakeChangeControlCrudService();
        var auth = new TestAuthContext { CurrentUser = new User { Id = 48, FullName = "QA Supervisor" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = TestElectronicSignatureDialogService.CreateConfirmed();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ChangeControlModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Update;
        Assert.True(viewModel.SaveCommand.CanExecute(null));

        var saveTask = InvokeSaveCommandAsync(viewModel);
        await Task.Yield();

        Assert.False(viewModel.SaveCommand.CanExecute(null));

        var saved = await saveTask;

        Assert.False(saved);
        Assert.Equal("Select a change control before saving.", viewModel.StatusMessage);
        Assert.Equal(FormMode.Update, viewModel.Mode);
        Assert.True(viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task WorkflowCommands_ProgressLifecycleAndRefreshRecords()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var crud = new FakeChangeControlCrudService();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 18, FullName = "QA Reviewer" },
            CurrentIpAddress = "10.0.0.35",
            CurrentDeviceInfo = "UnitTest"
        };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ChangeControlModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        await viewModel.EnterAddModeCommand.ExecuteAsync(null);

        viewModel.Editor.Title = "Supplier change";
        viewModel.Editor.Code = "CC-2025-001";
        viewModel.Editor.Description = "Evaluate alternative supplier";

        await viewModel.AddCommand.ExecuteAsync(null);
        Assert.Equal(FormMode.View, viewModel.Mode);
        Assert.Equal(ChangeControlStatus.Submitted.ToString(), viewModel.Editor.Status);
        var creation = Assert.Single(crud.TransitionHistory);
        Assert.Equal("Create", creation.Operation);
        Assert.Equal(ChangeControlStatus.Submitted.ToString(), creation.Entity.StatusRaw);

        await viewModel.ApproveCommand.ExecuteAsync(null);
        Assert.Equal(ChangeControlStatus.Approved.ToString(), viewModel.Editor.Status);
        Assert.Equal(ChangeControlStatus.Approved.ToString(), crud.TransitionHistory[1].Entity.StatusRaw);

        await viewModel.ExecuteCommand.ExecuteAsync(null);
        Assert.Equal(ChangeControlStatus.Implemented.ToString(), viewModel.Editor.Status);
        Assert.Equal(ChangeControlStatus.Implemented.ToString(), crud.TransitionHistory[2].Entity.StatusRaw);

        await viewModel.CloseCommand.ExecuteAsync(null);
        Assert.Equal(ChangeControlStatus.Closed.ToString(), viewModel.Editor.Status);
        Assert.Equal("Change control closed.", viewModel.StatusMessage);

        Assert.Collection(crud.TransitionHistory,
            entry => Assert.Equal(ChangeControlStatus.Submitted.ToString(), entry.Entity.StatusRaw),
            entry => Assert.Equal(ChangeControlStatus.Approved.ToString(), entry.Entity.StatusRaw),
            entry => Assert.Equal(ChangeControlStatus.Implemented.ToString(), entry.Entity.StatusRaw),
            entry => Assert.Equal(ChangeControlStatus.Closed.ToString(), entry.Entity.StatusRaw));

        var record = Assert.Single(viewModel.Records);
        Assert.Equal(ChangeControlStatus.Closed.ToString(), record.Status);
        Assert.Equal(record.Key, viewModel.SelectedRecord?.Key);
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

        var viewModel = new ChangeControlModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
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

        var viewModel = new ChangeControlModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));

        await viewModel.AttachDocumentCommand.ExecuteAsync(null);

        var request = Assert.Single(attachments.Uploads);
        Assert.Equal("change_controls", request.EntityType);
        Assert.Equal(21, request.EntityId);
        Assert.Equal("impact.txt", request.FileName);
    }

    [Fact]
    public async Task SelectingRecord_PopulatesInspectorFieldsAndTimeline()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var crud = new FakeChangeControlCrudService();

        var requested = new DateTime(2025, 1, 9, 10, 0, 0, DateTimeKind.Utc);

        crud.Seed(new ChangeControl
        {
            Id = 58,
            Code = "CC-2025-058",
            Title = "Upgrade HVAC monitoring",
            StatusRaw = ChangeControlStatus.UnderReview.ToString(),
            Description = "Evaluate sensor upgrade path.",
            DateRequested = requested,
            AssignedToId = 42
        });

        var auth = new TestAuthContext();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ChangeControlModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        var record = Assert.Single(viewModel.Records);
        viewModel.SelectedRecord = record;

        Assert.Collection(
            record.InspectorFields,
            field =>
            {
                Assert.Equal("Status", field.Label);
                Assert.Equal(ChangeControlStatus.UnderReview.ToString(), field.Value);
            },
            field =>
            {
                Assert.Equal("Requested", field.Label);
                Assert.Equal(requested.ToString("d", CultureInfo.CurrentCulture), field.Value);
            },
            field =>
            {
                Assert.Equal("Assigned To", field.Label);
                Assert.Equal("42", field.Value);
            });

        var timelineEntries = TimelineTestHelper.GetTimelineEntries(viewModel);
        Assert.NotEmpty(timelineEntries);
        Assert.Contains(timelineEntries, entry => TimelineTestHelper.GetTimestamp(entry) == requested);
    }

    private static Task<bool> InvokeSaveAsync(ChangeControlModuleViewModel viewModel)
    {
        var method = typeof(ChangeControlModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(ChangeControlModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }

    private static Task<bool> InvokeSaveCommandAsync(ChangeControlModuleViewModel viewModel)
    {
        var method = typeof(B1FormDocumentViewModel)
            .GetMethod("SaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(B1FormDocumentViewModel), "SaveAsync");
        return (Task<bool>)method.Invoke(viewModel, Array.Empty<object>())!;
    }
}
