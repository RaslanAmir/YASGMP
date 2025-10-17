using System;
using System.Collections.Generic;
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
        var localization = CreateLocalizationService();
        const int adapterSignatureId = 9135;
        var crud = new FakeScheduledJobCrudService
        {
            SignatureMetadataIdSource = _ => adapterSignatureId
        };
        var auth = new TestAuthContext { CurrentUser = new User { Id = 5, Username = "scheduler" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new SchedulingModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation, localization);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Name = "Weekly Digest";
        viewModel.Editor.JobType = "REPORT";
        viewModel.Editor.Status = "scheduled";
        viewModel.Editor.RecurrencePattern = "0 6 * * MON";
        viewModel.Editor.NextDue = DateTime.UtcNow.AddDays(1);

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.Equal("Electronic signature captured (QA Reason).", viewModel.StatusMessage);
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
        var capturedResult = Assert.Single(signatureDialog.CapturedResults);
        var signatureResult = Assert.NotNull(capturedResult);
        Assert.Equal(created.Id, signatureResult.Signature.RecordId);
        Assert.Equal(adapterSignatureId, signatureResult.Signature.Id);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.Equal(0, signatureDialog.PersistInvocationCount);
    }

    [Fact]
    public async Task Cancel_RevertsEditorInUpdateMode()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var localization = CreateLocalizationService();
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

        var viewModel = new SchedulingModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation, localization);
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
        var audit = new AuditService(database);
        var localization = CreateLocalizationService();
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

        var viewModel = new SchedulingModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation, localization);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        Assert.True(viewModel.ExecuteJobCommand.CanExecute(null));

        await viewModel.ExecuteJobCommand.ExecuteAsync(null);

        Assert.Contains(21, crud.Executed);
    }

    [Fact]
    public async Task OnSaveAsync_UpdateMode_PersistsExistingJobWithMetadata()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var localization = CreateLocalizationService();
        var job = new ScheduledJob
        {
            Id = 34,
            Name = "Preventive Maintenance Sweep",
            JobType = "maintenance",
            Status = "scheduled",
            RecurrencePattern = "0 5 * * SUN",
            NextDue = DateTime.UtcNow.AddDays(3),
            NeedsAcknowledgment = true,
            Comment = "Initial schedule",
            CreatedById = 2,
            CreatedBy = "system"
        };
        const int adapterSignatureId = 7821;
        var crud = new FakeScheduledJobCrudService
        {
            SignatureMetadataIdSource = _ => adapterSignatureId
        };
        crud.Seed(job);
        database.ScheduledJobs.Add(job);

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 18, Username = "pm.tech" },
            CurrentDeviceInfo = "BenchStation",
            CurrentIpAddress = "10.0.0.42",
            CurrentSessionId = "session-update"
        };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new SchedulingModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation, localization);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.Single(r => r.Key == job.Id.ToString());
        viewModel.Mode = FormMode.Update;
        viewModel.Editor.NextDue = job.NextDue.AddDays(7);
        viewModel.Editor.Comment = "Rescheduled for extended downtime";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.False(viewModel.IsDirty);
        Assert.True(signatureDialog.WasLogPersistInvoked);
        var loggedSignature = Assert.Single(signatureDialog.LoggedSignatureRecords);
        Assert.Equal(job.Id, loggedSignature.RecordId);
        Assert.Equal(adapterSignatureId, loggedSignature.SignatureId);

        var updateSnapshot = Assert.Single(crud.UpdatedWithContext);
        Assert.Equal(job.Id, updateSnapshot.Entity.Id);
        Assert.Equal("Rescheduled for extended downtime", updateSnapshot.Entity.Comment);

        var context = updateSnapshot.Context;
        Assert.Equal(auth.CurrentUser!.Id, context.UserId);
        Assert.Equal(auth.CurrentDeviceInfo, context.DeviceInfo);
        Assert.Equal(auth.CurrentIpAddress, context.Ip);
        Assert.Equal(auth.CurrentSessionId, context.SessionId);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
    }

    [Fact]
    public async Task OnSaveAsync_UpdateMode_DisablesJobThroughAdapter()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var localization = CreateLocalizationService();
        var job = new ScheduledJob
        {
            Id = 48,
            Name = "Calibration Window",
            JobType = "calibration",
            Status = "scheduled",
            RecurrencePattern = "0 8 * * MON",
            NextDue = DateTime.UtcNow.AddDays(5),
            NeedsAcknowledgment = false,
            Comment = "Active"
        };
        var crud = new FakeScheduledJobCrudService();
        crud.Seed(job);
        database.ScheduledJobs.Add(job);

        var auth = new TestAuthContext { CurrentUser = new User { Id = 7, Username = "planner" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new SchedulingModuleViewModel(database, audit, crud, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation, localization);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.Single(r => r.Key == job.Id.ToString());
        viewModel.Mode = FormMode.Update;
        viewModel.Editor.Status = "disabled";
        viewModel.Editor.Comment = "Temporarily disabled while equipment is offline";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.Equal("disabled", viewModel.Editor.Status);
        var updateSnapshot = Assert.Single(crud.UpdatedWithContext);
        Assert.Equal("disabled", updateSnapshot.Entity.Status);
        Assert.Equal("Temporarily disabled while equipment is offline", updateSnapshot.Entity.Comment);
        Assert.False(viewModel.IsDirty);
    }

    private static FakeLocalizationService CreateLocalizationService()
        => new(
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Module.Title.ScheduledJobs"] = "Scheduled Jobs"
                }
            },
            initialLanguage: "en");

    private static Task<bool> InvokeSaveAsync(SchedulingModuleViewModel viewModel)
    {
        var method = typeof(SchedulingModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(SchedulingModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }
}
