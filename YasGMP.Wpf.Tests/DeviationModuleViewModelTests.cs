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

public class DeviationModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsDeviationThroughAdapter()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        const int signatureMetadataId = 4321;
        var deviations = new FakeDeviationCrudService
        {
            SignatureMetadataIdSource = _ => signatureMetadataId
        };
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 8, FullName = "QA Manager" },
            CurrentIpAddress = "10.0.0.8",
            CurrentDeviceInfo = "UnitTest",
            CurrentSessionId = "session-123"
        };
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var localization = new LocalizationService();

        var viewModel = new DeviationModuleViewModel(database, audit, deviations, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation, localization);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Title = "Temperature excursion";
        viewModel.Editor.Description = "Fridge exceeded threshold";
        viewModel.Editor.Severity = "CRITICAL";
        viewModel.Editor.IsCritical = true;
        viewModel.Editor.LinkedCapaId = 42;
        viewModel.Editor.RiskScore = 85;

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.Equal("Electronic signature captured (QA Reason).", viewModel.StatusMessage);
        var persisted = Assert.Single(deviations.Saved);
        Assert.Equal("Temperature excursion", persisted.Title);
        Assert.Equal("CRITICAL", persisted.Severity);
        Assert.Equal("test-signature", persisted.DigitalSignature);
        Assert.Equal(42, persisted.LinkedCapaId);
        var context = Assert.Single(deviations.SavedContexts);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("deviations", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        var capturedResult = Assert.Single(signatureDialog.CapturedResults);
        Assert.NotNull(capturedResult);
        Assert.Equal(deviations.Saved[0].Id, capturedResult!.Signature.RecordId);
        Assert.Equal(signatureMetadataId, capturedResult.Signature.Id);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public async Task AttachEvidenceCommand_UploadsEvidenceViaAttachmentService()
    {
        var database = new DatabaseService();
        database.Deviations.Add(new Deviation
        {
            Id = 5,
            Title = "Filter deviation",
            Description = "Filter pressure dropped",
            Severity = "HIGH",
            Status = "OPEN"
        });

        var deviations = new FakeDeviationCrudService();
        deviations.Saved.Add(new Deviation
        {
            Id = 5,
            Title = "Filter deviation",
            Description = "Filter pressure dropped",
            Severity = "HIGH",
            Status = "OPEN"
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 7, FullName = "QA" },
            CurrentIpAddress = "10.1.0.5",
            CurrentDeviceInfo = "UnitTest"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var localization = new LocalizationService();
        var audit = new AuditService(database);

        var bytes = Encoding.UTF8.GetBytes("deviation evidence");
        filePicker.Files = new[]
        {
            new PickedFile("evidence.txt", "text/plain", () => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)), bytes.Length)
        };

        var viewModel = new DeviationModuleViewModel(database, audit, deviations, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation, localization);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.RecordsView.Cast<ModuleRecord>().First();
        viewModel.Mode = FormMode.View;

        Assert.True(viewModel.AttachEvidenceCommand.CanExecute(null));
        await viewModel.AttachEvidenceCommand.ExecuteAsync(null);

        var upload = Assert.Single(attachments.Uploads);
        Assert.Equal("deviations", upload.EntityType);
        Assert.Equal(5, upload.EntityId);
        Assert.Equal("evidence.txt", upload.FileName);
    }

    [Fact]
    public async Task CreateCflRequestAsync_ReturnsCapaChoices()
    {
        var database = new DatabaseService();
        database.CapaCases.Add(new CapaCase
        {
            Id = 21,
            Title = "Investigation",
            Status = "OPEN",
            Priority = "High",
            DateOpen = DateTime.UtcNow.AddDays(-1)
        });

        var deviations = new FakeDeviationCrudService();
        var auth = new TestAuthContext();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var localization = new LocalizationService();
        var audit = new AuditService(database);

        var viewModel = new DeviationModuleViewModel(database, audit, deviations, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation, localization);
        await viewModel.InitializeAsync(null);
        viewModel.Mode = FormMode.Add;

        var request = await InvokeCreateCflRequestAsync(viewModel);

        Assert.NotNull(request);
        Assert.Contains(request!.Items, item => item.Key == "CAPA:21");
    }

    [Fact]
    public async Task OnCflSelectionAsync_SetsLinkedCapaId()
    {
        var database = new DatabaseService();
        var deviations = new FakeDeviationCrudService();
        var auth = new TestAuthContext();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var localization = new LocalizationService();
        var audit = new AuditService(database);

        var viewModel = new DeviationModuleViewModel(database, audit, deviations, auth, filePicker, attachments, signatureDialog, dialog, shell, navigation, localization);
        await viewModel.InitializeAsync(null);
        viewModel.Mode = FormMode.Add;

        await InvokeOnCflSelectionAsync(viewModel, new CflResult(new CflItem("CAPA:77", "CAPA-00077", string.Empty)));

        Assert.Equal(77, viewModel.Editor.LinkedCapaId);
        Assert.True(viewModel.IsDirty);
    }

    private static Task<bool> InvokeSaveAsync(DeviationModuleViewModel viewModel)
    {
        var method = typeof(DeviationModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(DeviationModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }

    private static Task<CflRequest?> InvokeCreateCflRequestAsync(DeviationModuleViewModel viewModel)
    {
        var method = typeof(DeviationModuleViewModel)
            .GetMethod("CreateCflRequestAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(DeviationModuleViewModel), "CreateCflRequestAsync");
        return (Task<CflRequest?>)method.Invoke(viewModel, null)!;
    }

    private static Task InvokeOnCflSelectionAsync(DeviationModuleViewModel viewModel, CflResult result)
    {
        var method = typeof(DeviationModuleViewModel)
            .GetMethod("OnCflSelectionAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(DeviationModuleViewModel), "OnCflSelectionAsync");
        return (Task)method.Invoke(viewModel, new object[] { result })!;
    }
}
