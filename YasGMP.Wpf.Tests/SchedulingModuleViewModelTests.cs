using System;
using System.Linq;
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

public class SchedulingModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsJob()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var audit = new AuditService(database);
        var audit = new AuditService(database);
        var crud = new FakeScheduledJobCrudService();
        var auth = new TestAuthContext { CurrentUser = new User { Id = 5, Username = "scheduler" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new SchedulingModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Name = "Weekly Digest";
        viewModel.Editor.JobType = "REPORT";
        viewModel.Editor.Status = "scheduled";
        viewModel.Editor.RecurrencePattern = "0 6 * * MON";
        viewModel.Editor.NextDue = DateTime.UtcNow.AddDays(1);

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        var created = Assert.Single(crud.Saved);
        Assert.Equal("weekly digest", created.JobType);
        Assert.False(viewModel.IsDirty);
        var context = Assert.Single(crud.SavedContexts);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("scheduled_jobs", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        Assert.Single(signatureDialog.PersistedResults);
        var persistedSignature = signatureDialog.PersistedResults[0];
        Assert.Equal(created.Id, persistedSignature.Signature.RecordId);
        Assert.Equal(signatureDialog.LastPersistedSignatureId, persistedSignature.Signature.Id);
        Assert.True(persistedSignature.Signature.Id > 0);
        var persistedMetadata = Assert.Single(signatureDialog.PersistedSignatureRecords);
        Assert.Equal(persistedSignature.Signature.Id, persistedMetadata.SignatureId);
        Assert.Equal(created.Id, persistedMetadata.RecordId);
        Assert.Equal("test-signature", persistedMetadata.SignatureHash);
        Assert.Equal("password", persistedMetadata.Method);
        Assert.Equal("valid", persistedMetadata.Status);
        Assert.Equal("Automated test", persistedMetadata.Note);
    }

    [Fact]
    public async Task Cancel_RevertsEditorInUpdateMode()
    {
        var database = new DatabaseService();
        var crud = new FakeScheduledJobCrudService();
        var job = new ScheduledJob
        {
            Id = 12,
            Name = "Calibration Sweep",
            JobType = "notification",
            Status = "scheduled",
            RecurrencePattern = "0 7 * * *",
            NextDue = DateTime.UtcNow.AddDays(2)
        };
        crud.Seed(job);
        database.ScheduledJobs.Add(job);

        var auth = new TestAuthContext { CurrentUser = new User { Id = 7 } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new SchedulingModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        viewModel.Mode = FormMode.Update;

        viewModel.Editor.Comment = "modified";
        Assert.True(viewModel.IsDirty);

        viewModel.CancelCommand.Execute(null);

        Assert.Equal(string.Empty, viewModel.Editor.Comment);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public async Task ExecuteCommand_TriggersService()
    {
        var database = new DatabaseService();
        var crud = new FakeScheduledJobCrudService();
        var job = new ScheduledJob
        {
            Id = 21,
            Name = "Work Order Digest",
            JobType = "report",
            Status = "scheduled",
            RecurrencePattern = "0 6 * * MON",
            NextDue = DateTime.UtcNow.AddDays(1)
        };
        crud.Seed(job);
        database.ScheduledJobs.Add(job);

        var auth = new TestAuthContext { CurrentUser = new User { Id = 9 } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new SchedulingModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        Assert.True(viewModel.ExecuteJobCommand.CanExecute(null));

        await viewModel.ExecuteJobCommand.ExecuteAsync(null);

        Assert.Contains(21, crud.Executed);
    }

    private static Task<bool> InvokeSaveAsync(SchedulingModuleViewModel viewModel)
    {
        var method = typeof(SchedulingModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(SchedulingModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }
}
