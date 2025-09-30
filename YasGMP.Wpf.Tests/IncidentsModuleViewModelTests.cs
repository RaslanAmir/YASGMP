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

public class IncidentsModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsIncidentThroughAdapter()
    {
        var database = new DatabaseService();
        var incidents = new FakeIncidentCrudService();
        var auth = new TestAuthContext { CurrentUser = new User { Id = 5, FullName = "QA Manager" } };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new IncidentsModuleViewModel(database, incidents, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
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
        Assert.Single(signatureDialog.PersistedResults);
        var persistedSignature = signatureDialog.PersistedResults[0];
        Assert.Equal(incidents.Saved[0].Id, persistedSignature.Signature.RecordId);
        Assert.Equal(signatureDialog.LastPersistedSignatureId, persistedSignature.Signature.Id);
        Assert.True(persistedSignature.Signature.Id > 0);
        var persistedMetadata = Assert.Single(signatureDialog.PersistedSignatureRecords);
        Assert.Equal(persistedSignature.Signature.Id, persistedMetadata.SignatureId);
        Assert.Equal(incidents.Saved[0].Id, persistedMetadata.RecordId);
        Assert.Equal("test-signature", persistedMetadata.SignatureHash);
        Assert.Equal("password", persistedMetadata.Method);
        Assert.Equal("valid", persistedMetadata.Status);
        Assert.Equal("Automated test", persistedMetadata.Note);
        Assert.False(viewModel.IsDirty);
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

        var viewModel = new IncidentsModuleViewModel(database, incidents, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        Assert.True(viewModel.AttachEvidenceCommand.CanExecute(null));
        await viewModel.AttachEvidenceCommand.ExecuteAsync(null);

        var upload = Assert.Single(attachments.Uploads);
        Assert.Equal("incidents", upload.EntityType);
        Assert.Equal(3, upload.EntityId);
        Assert.Equal("evidence.txt", upload.FileName);
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

        var viewModel = new IncidentsModuleViewModel(database, incidents, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
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

        var viewModel = new IncidentsModuleViewModel(database, incidents, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation);
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
