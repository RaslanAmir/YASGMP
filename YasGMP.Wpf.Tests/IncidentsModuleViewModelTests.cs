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

namespace YasGMP.Wpf.Tests;

public class IncidentsModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsIncidentThroughAdapter()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        const int adapterSignatureId = 9087;
        var incidents = new FakeIncidentCrudService
        {
            SignatureMetadataIdSource = _ => adapterSignatureId
        };
        var auth = new TestAuthContext { CurrentUser = new User { Id = 5, FullName = "QA Manager" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new IncidentsModuleViewModel(database, audit, incidents, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Title = "Temperature deviation";
        viewModel.Editor.Description = "Fridge deviated from range.";
        viewModel.Editor.Type = "Deviation";
        viewModel.Editor.Priority = "High";
        viewModel.Editor.AssignedInvestigator = "QA";
        viewModel.Editor.DetectedAt = DateTime.UtcNow;
        viewModel.Editor.Status = "REPORTED";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.Equal("Electronic signature captured (QA Reason).", viewModel.StatusMessage);
        var persisted = Assert.Single(incidents.Saved);
        Assert.Equal("Temperature deviation", persisted.Title);
        Assert.Equal("Deviation", persisted.Type);
        Assert.Equal("test-signature", persisted.DigitalSignature);
        var context = Assert.Single(incidents.SavedContexts);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("incidents", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        var capturedResult = Assert.Single(signatureDialog.CapturedResults);
        var signatureResult = Assert.NotNull(capturedResult);
        Assert.Equal(incidents.Saved[0].Id, signatureResult.Signature.RecordId);
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
        var incidents = new FakeIncidentCrudService();
        var auth = new TestAuthContext { CurrentUser = new User { Id = 61, FullName = "QA Manager" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = TestElectronicSignatureDialogService.CreateCancelled();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new IncidentsModuleViewModel(database, audit, incidents, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        await viewModel.EnterAddModeCommand.ExecuteAsync(null);

        viewModel.Editor.Title = "HVAC deviation";
        viewModel.Editor.Description = "Room temperature exceeded limits.";
        viewModel.Editor.Type = "Deviation";
        viewModel.Editor.Priority = "High";
        viewModel.Editor.AssignedInvestigator = "QA";

        Assert.True(viewModel.SaveCommand.CanExecute(null));

        var saveTask = InvokeSaveCommandAsync(viewModel);
        await Task.Yield();

        Assert.False(viewModel.SaveCommand.CanExecute(null));

        var saved = await saveTask;

        Assert.False(saved);
        Assert.Equal("Electronic signature cancelled. Save aborted.", viewModel.StatusMessage);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.True(viewModel.SaveCommand.CanExecute(null));
        Assert.Empty(incidents.Saved);
    }

    [Fact]
    public async Task SaveCommand_SignatureFailure_ShowsErrorAndRestoresState()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var incidents = new FakeIncidentCrudService();
        var auth = new TestAuthContext { CurrentUser = new User { Id = 52, FullName = "QA Investigator" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = TestElectronicSignatureDialogService.CreateCaptureException(new InvalidOperationException("Investigator credentials rejected."));
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new IncidentsModuleViewModel(database, audit, incidents, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        await viewModel.EnterAddModeCommand.ExecuteAsync(null);

        viewModel.Editor.Title = "Audit trail alert";
        viewModel.Editor.Description = "Multiple failed login attempts.";
        viewModel.Editor.Type = "Security";
        viewModel.Editor.Priority = "Medium";
        viewModel.Editor.AssignedInvestigator = "QA";

        Assert.True(viewModel.SaveCommand.CanExecute(null));

        var saveTask = InvokeSaveCommandAsync(viewModel);
        await Task.Yield();

        Assert.False(viewModel.SaveCommand.CanExecute(null));

        var saved = await saveTask;

        Assert.False(saved);
        Assert.Equal("Electronic signature failed: Investigator credentials rejected.", viewModel.StatusMessage);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.True(viewModel.SaveCommand.CanExecute(null));
        Assert.Empty(incidents.Saved);
    }

    [Fact]
    public async Task AddCommand_WhenValidationFails_ShowsErrorAndStaysInAddMode()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var incidents = new FakeIncidentCrudService
        {
            NextValidationException = new InvalidOperationException("Incident requires a detailed description.")
        };
        var auth = new TestAuthContext { CurrentUser = new User { Id = 71, FullName = "QA Reporter" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = TestElectronicSignatureDialogService.CreateConfirmed();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new IncidentsModuleViewModel(database, audit, incidents, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        await viewModel.EnterAddModeCommand.ExecuteAsync(null);

        viewModel.Editor.Title = "Unlabelled sample";

        Assert.True(viewModel.AddCommand.CanExecute(null));

        await viewModel.AddCommand.ExecuteAsync(null);

        Assert.Equal("Failed to add incident: Incident requires a detailed description.", viewModel.StatusMessage);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.True(viewModel.AddCommand.CanExecute(null));
        Assert.Empty(incidents.Saved);
    }

    [Fact]
    public async Task ExecuteCommand_WhenValidationFails_ShowsErrorAndKeepsStatus()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var incidents = new FakeIncidentCrudService();
        var auth = new TestAuthContext { CurrentUser = new User { Id = 81, FullName = "QA Approver" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = TestElectronicSignatureDialogService.CreateConfirmed();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new IncidentsModuleViewModel(database, audit, incidents, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        await viewModel.EnterAddModeCommand.ExecuteAsync(null);

        viewModel.Editor.Title = "Temperature excursion";
        viewModel.Editor.Description = "Refrigerator above limit.";
        viewModel.Editor.Type = "Deviation";
        viewModel.Editor.Priority = "High";
        viewModel.Editor.AssignedInvestigator = "QA";

        await viewModel.AddCommand.ExecuteAsync(null);
        await viewModel.ApproveCommand.ExecuteAsync(null);
        Assert.Equal("CLASSIFIED", viewModel.Editor.Status);

        incidents.NextValidationException = new InvalidOperationException("Link CAPA before execution.");

        Assert.True(viewModel.ExecuteCommand.CanExecute(null));

        await viewModel.ExecuteCommand.ExecuteAsync(null);

        Assert.Equal("Failed to update incident: Link CAPA before execution.", viewModel.StatusMessage);
        Assert.Equal("CLASSIFIED", viewModel.Editor.Status);
        Assert.True(viewModel.ExecuteCommand.CanExecute(null));
        Assert.Equal(2, incidents.TransitionHistory.Count);
    }

    [Fact]
    public async Task SaveCommand_UpdateWithoutSelection_ShowsSelectionWarning()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var incidents = new FakeIncidentCrudService();
        var auth = new TestAuthContext { CurrentUser = new User { Id = 91, FullName = "QA Supervisor" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = TestElectronicSignatureDialogService.CreateConfirmed();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new IncidentsModuleViewModel(database, audit, incidents, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Update;
        Assert.True(viewModel.SaveCommand.CanExecute(null));

        var saveTask = InvokeSaveCommandAsync(viewModel);
        await Task.Yield();

        Assert.False(viewModel.SaveCommand.CanExecute(null));

        var saved = await saveTask;

        Assert.False(saved);
        Assert.Equal("Select an incident before saving.", viewModel.StatusMessage);
        Assert.Equal(FormMode.Update, viewModel.Mode);
        Assert.True(viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task WorkflowCommands_SynchronizeStatusAndLinks()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var incidents = new FakeIncidentCrudService();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 15, FullName = "QA Investigator" },
            CurrentIpAddress = "10.0.0.40",
            CurrentDeviceInfo = "UnitTest"
        };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new IncidentsModuleViewModel(database, audit, incidents, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        await viewModel.EnterAddModeCommand.ExecuteAsync(null);

        viewModel.Editor.Title = "Line pressure deviation";
        viewModel.Editor.Description = "Pressure exceeded limits";
        viewModel.Editor.Type = "Deviation";
        viewModel.Editor.Priority = "High";
        viewModel.Editor.AssignedInvestigator = "QA";

        await viewModel.AddCommand.ExecuteAsync(null);

        Assert.Equal(FormMode.View, viewModel.Mode);
        Assert.Equal("INVESTIGATION", viewModel.Editor.Status);
        var creation = Assert.Single(incidents.TransitionHistory);
        Assert.Equal("Create", creation.Operation);
        Assert.Equal("INVESTIGATION", creation.Entity.Status);

        await viewModel.ApproveCommand.ExecuteAsync(null);
        Assert.Equal("CLASSIFIED", viewModel.Editor.Status);
        Assert.Equal("CLASSIFIED", incidents.TransitionHistory[1].Entity.Status);

        viewModel.Editor.CapaCaseId = 42;
        viewModel.Editor.WorkOrderId = 9;
        await viewModel.ExecuteCommand.ExecuteAsync(null);
        Assert.Equal("CAPA_LINKED", viewModel.Editor.Status);
        Assert.Equal(42, viewModel.Editor.LinkedCapaId);
        Assert.Equal("CAPA_LINKED", incidents.TransitionHistory[2].Entity.Status);

        await viewModel.CloseCommand.ExecuteAsync(null);
        Assert.Equal("CLOSED", viewModel.Editor.Status);
        Assert.Equal("Incident closed.", viewModel.StatusMessage);

        Assert.Collection(incidents.TransitionHistory,
            entry => Assert.Equal("INVESTIGATION", entry.Entity.Status),
            entry => Assert.Equal("CLASSIFIED", entry.Entity.Status),
            entry => Assert.Equal("CAPA_LINKED", entry.Entity.Status),
            entry => Assert.Equal("CLOSED", entry.Entity.Status));

        var record = Assert.Single(viewModel.Records);
        Assert.Equal("CLOSED", record.Status);
        Assert.Equal(record.Key, viewModel.SelectedRecord?.Key);
    }

    [Fact]
    public async Task AttachEvidenceCommand_UploadsEvidenceViaAttachmentService()
    {
        var database = new DatabaseService();
        database.Incidents.Add(new Incident
        {
            Id = 3,
            Title = "Audit trail alert",
            Description = "Multiple failed logins detected",
            Type = "Security",
            Priority = "Medium",
            DetectedAt = DateTime.UtcNow.AddHours(-2),
            Status = "REPORTED"
        });

        var incidents = new FakeIncidentCrudService();
        incidents.Saved.Add(new Incident
        {
            Id = 3,
            Title = "Audit trail alert",
            Description = "Multiple failed logins detected",
            Type = "Security",
            Priority = "Medium",
            DetectedAt = database.Incidents[0].DetectedAt,
            Status = "REPORTED"
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 4, FullName = "Auditor" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.5"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var bytes = Encoding.UTF8.GetBytes("incident evidence");
        filePicker.Files = new[]
        {
            new PickedFile("evidence.txt", "text/plain", () => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)), bytes.Length)
        };

        var viewModel = new IncidentsModuleViewModel(database, audit, incidents, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        Assert.True(viewModel.AttachEvidenceCommand.CanExecute(null));
        await viewModel.AttachEvidenceCommand.ExecuteAsync(null);

        var upload = Assert.Single(attachments.Uploads);
        Assert.Equal("incidents", upload.EntityType);
        Assert.Equal(3, upload.EntityId);
        Assert.Equal("evidence.txt", upload.FileName);
    }

    [Fact]
    public async Task SelectingRecord_PopulatesInspectorFieldsAndTimelineWithNavigationUpdates()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var incidents = new FakeIncidentCrudService();

        var detected = new DateTime(2025, 2, 20, 6, 45, 0, DateTimeKind.Utc);

        var incidentEntity = new Incident
        {
            Id = 301,
            Title = "Temperature excursion",
            Description = "Cold room exceeded limits.",
            Type = "Deviation",
            Priority = "High",
            DetectedAt = detected,
            Status = "INVESTIGATION",
            AssignedInvestigator = "QA Ops",
            Classification = "Major",
            LinkedCapaId = 91,
            WorkOrderId = 27
        };

        database.Incidents.Add(new Incident
        {
            Id = incidentEntity.Id,
            Title = incidentEntity.Title,
            Description = incidentEntity.Description,
            Type = incidentEntity.Type,
            Priority = incidentEntity.Priority,
            DetectedAt = incidentEntity.DetectedAt,
            Status = incidentEntity.Status,
            AssignedInvestigator = incidentEntity.AssignedInvestigator,
            Classification = incidentEntity.Classification,
            LinkedCapaId = incidentEntity.LinkedCapaId,
            WorkOrderId = incidentEntity.WorkOrderId
        });

        incidents.Saved.Add(incidentEntity);

        var auth = new TestAuthContext();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new RecordingModuleNavigationService();

        var viewModel = new IncidentsModuleViewModel(database, audit, incidents, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        var record = Assert.Single(viewModel.Records);
        viewModel.SelectedRecord = record;

        Assert.Collection(
            record.InspectorFields,
            field =>
            {
                Assert.Equal("Type", field.Label);
                Assert.Equal("Deviation", field.Value);
            },
            field =>
            {
                Assert.Equal("Priority", field.Label);
                Assert.Equal("High", field.Value);
            },
            field =>
            {
                Assert.Equal("Detected", field.Label);
                Assert.Equal(detected.ToString("g", CultureInfo.CurrentCulture), field.Value);
            },
            field =>
            {
                Assert.Equal("Investigator", field.Label);
                Assert.Equal("QA Ops", field.Value);
            },
            field =>
            {
                Assert.Equal("Classification", field.Label);
                Assert.Equal("Major", field.Value);
            },
            field =>
            {
                Assert.Equal("Linked CAPA", field.Label);
                Assert.Equal("91", field.Value);
            },
            field =>
            {
                Assert.Equal("Work Order", field.Label);
                Assert.Equal("27", field.Value);
            });

        var timelineEntries = TimelineTestHelper.GetTimelineEntries(viewModel);
        Assert.NotEmpty(timelineEntries);
        Assert.Contains(timelineEntries, entry => TimelineTestHelper.GetTimestamp(entry) == detected);

        viewModel.GoldenArrowCommand.Execute(null);
        Assert.Contains(navigation.OpenedModules, item => item.ModuleKey == CapaModuleViewModel.ModuleKey && Equals(item.Parameter, 91));

        var updatedDetected = detected.AddHours(3);
        incidentEntity.LinkedCapaId = null;
        incidentEntity.WorkOrderId = 42;
        incidentEntity.AssignedInvestigator = "Manufacturing";
        incidentEntity.Classification = "Minor";
        incidentEntity.Status = "CAPA_LINKED";
        incidentEntity.DetectedAt = updatedDetected;

        var databaseIncident = database.Incidents.Single(i => i.Id == incidentEntity.Id);
        databaseIncident.LinkedCapaId = incidentEntity.LinkedCapaId;
        databaseIncident.WorkOrderId = incidentEntity.WorkOrderId;
        databaseIncident.AssignedInvestigator = incidentEntity.AssignedInvestigator;
        databaseIncident.Classification = incidentEntity.Classification;
        databaseIncident.Status = incidentEntity.Status;
        databaseIncident.DetectedAt = incidentEntity.DetectedAt;

        await viewModel.RefreshCommand.ExecuteAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();

        var refreshed = viewModel.Records.First();
        Assert.Collection(
            refreshed.InspectorFields,
            field =>
            {
                Assert.Equal("Type", field.Label);
                Assert.Equal("Deviation", field.Value);
            },
            field =>
            {
                Assert.Equal("Priority", field.Label);
                Assert.Equal("High", field.Value);
            },
            field =>
            {
                Assert.Equal("Detected", field.Label);
                Assert.Equal(updatedDetected.ToString("g", CultureInfo.CurrentCulture), field.Value);
            },
            field =>
            {
                Assert.Equal("Investigator", field.Label);
                Assert.Equal("Manufacturing", field.Value);
            },
            field =>
            {
                Assert.Equal("Classification", field.Label);
                Assert.Equal("Minor", field.Value);
            },
            field =>
            {
                Assert.Equal("Work Order", field.Label);
                Assert.Equal("42", field.Value);
            });

        Assert.DoesNotContain(refreshed.InspectorFields, field => field.Label == "Linked CAPA");

        var refreshedTimeline = TimelineTestHelper.GetTimelineEntries(viewModel);
        Assert.Contains(refreshedTimeline, entry => TimelineTestHelper.GetTimestamp(entry) == updatedDetected);
        Assert.DoesNotContain(refreshedTimeline, entry => TimelineTestHelper.GetTimestamp(entry) == detected);
    }

    [Fact]
    public async Task CreateCflRequestAsync_ReturnsWorkOrderAndCapaChoices()
    {
        var database = new DatabaseService();
        database.WorkOrders.Add(new WorkOrder
        {
            Id = 21,
            Title = "Filter change",
            Type = "Corrective",
            Status = "Open",
            DateOpen = DateTime.UtcNow.AddDays(-2)
        });
        database.CapaCases.Add(new CapaCase
        {
            Id = 7,
            Title = "Investigation",
            Priority = "High",
            Status = "IN_PROGRESS",
            DateOpen = DateTime.UtcNow.AddDays(-10)
        });

        var incidents = new FakeIncidentCrudService();
        var auth = new TestAuthContext();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new IncidentsModuleViewModel(database, audit, incidents, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        viewModel.Mode = FormMode.Add;

        var request = await InvokeCreateCflRequestAsync(viewModel);

        Assert.NotNull(request);
        Assert.Contains(request!.Items, item => item.Key == "WO:21");
        Assert.Contains(request.Items, item => item.Key == "CAPA:7");
    }

    [Fact]
    public async Task OnCflSelectionAsync_UpdatesEditorLinks()
    {
        var database = new DatabaseService();
        database.WorkOrders.Add(new WorkOrder { Id = 9, Title = "Calibration follow-up" });
        database.CapaCases.Add(new CapaCase { Id = 12, Title = "Root cause analysis" });

        var incidents = new FakeIncidentCrudService();
        var auth = new TestAuthContext();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new IncidentsModuleViewModel(database, audit, incidents, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);
        viewModel.Mode = FormMode.Add;

        await InvokeOnCflSelectionAsync(viewModel, new CflResult(new CflItem("WO:9", "WO-00009", string.Empty)));
        Assert.Equal(9, viewModel.Editor.WorkOrderId);
        Assert.True(viewModel.IsDirty);

        await InvokeOnCflSelectionAsync(viewModel, new CflResult(new CflItem("CAPA:12", "CAPA-00012", string.Empty)));
        Assert.Equal(12, viewModel.Editor.CapaCaseId);
        Assert.Equal(12, viewModel.Editor.LinkedCapaId);
    }

    private static Task<bool> InvokeSaveAsync(IncidentsModuleViewModel viewModel)
    {
        var method = typeof(IncidentsModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(IncidentsModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }

    private static Task<bool> InvokeSaveCommandAsync(IncidentsModuleViewModel viewModel)
    {
        var method = typeof(B1FormDocumentViewModel)
            .GetMethod("SaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(B1FormDocumentViewModel), "SaveAsync");
        return (Task<bool>)method.Invoke(viewModel, Array.Empty<object>())!;
    }

    private static Task<CflRequest?> InvokeCreateCflRequestAsync(IncidentsModuleViewModel viewModel)
    {
        var method = typeof(IncidentsModuleViewModel)
            .GetMethod("CreateCflRequestAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(IncidentsModuleViewModel), "CreateCflRequestAsync");
        return (Task<CflRequest?>)method.Invoke(viewModel, null)!;
    }

    private static Task InvokeOnCflSelectionAsync(IncidentsModuleViewModel viewModel, CflResult result)
    {
        var method = typeof(IncidentsModuleViewModel)
            .GetMethod("OnCflSelectionAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(IncidentsModuleViewModel), "OnCflSelectionAsync");
        return (Task)method.Invoke(viewModel, new object[] { result })!;
    }
}
