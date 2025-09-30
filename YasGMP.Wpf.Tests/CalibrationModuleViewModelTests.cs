using System;
using System.IO;
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

public class CalibrationModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsCalibration()
    {
        var database = new DatabaseService();
        database.Suppliers.Add(new Supplier { Id = 7, Name = "Metrologix" });

        var calibrationAdapter = new FakeCalibrationCrudService();
        var componentAdapter = new FakeComponentCrudService();

        await componentAdapter.CreateAsync(new Component
        {
            Id = 3,
            MachineId = 1,
            MachineName = "Autoclave",
            Code = "CMP-3",
            Name = "Pressure Sensor",
            SopDoc = "SOP-CAL-001",
            Status = "active"
        }, ComponentCrudContext.Create(1, "127.0.0.1", "Unit", "session"));

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 9, FullName = "Metrology" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.5"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachmentService = new TestAttachmentService();
        var audit = new AuditService(database);

        var viewModel = new CalibrationModuleViewModel(
            database,
            audit,
            calibrationAdapter,
            componentAdapter,
            auth,
            filePicker,
            attachmentService,
            signatureDialog,
            dialog,
            shell,
            navigation);

        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.ComponentId = componentAdapter.Saved[0].Id;
        viewModel.Editor.SupplierId = database.Suppliers[0].Id;
        viewModel.Editor.CalibrationDate = new DateTime(2025, 1, 10);
        viewModel.Editor.NextDue = new DateTime(2025, 7, 10);
        viewModel.Editor.Result = "PASS";
        viewModel.Editor.CertDoc = "CERT-001.pdf";
        viewModel.Editor.Comment = "Initial calibration";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.Single(calibrationAdapter.Saved);
        var persisted = calibrationAdapter.Saved[0];
        Assert.Equal(componentAdapter.Saved[0].Id, persisted.ComponentId);
        Assert.Equal(database.Suppliers[0].Id, persisted.SupplierId);
        Assert.Equal("PASS", persisted.Result);
        Assert.Equal("CERT-001.pdf", persisted.CertDoc);
        Assert.Equal("test-signature", persisted.DigitalSignature);
        var context = Assert.Single(calibrationAdapter.SavedContexts);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("calibrations", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        Assert.Single(signatureDialog.PersistedResults);
        var persistedSignature = signatureDialog.PersistedResults[0];
        Assert.Equal(calibrationAdapter.Saved[0].Id, persistedSignature.Signature.RecordId);
        Assert.Equal(signatureDialog.LastPersistedSignatureId, persistedSignature.Signature.Id);
        Assert.True(persistedSignature.Signature.Id > 0);
        var persistedMetadata = Assert.Single(signatureDialog.PersistedSignatureRecords);
        Assert.Equal(persistedSignature.Signature.Id, persistedMetadata.SignatureId);
        Assert.Equal(calibrationAdapter.Saved[0].Id, persistedMetadata.RecordId);
        Assert.Equal("test-signature", persistedMetadata.SignatureHash);
        Assert.Equal("password", persistedMetadata.Method);
        Assert.Equal("valid", persistedMetadata.Status);
        Assert.Equal("Automated test", persistedMetadata.Note);
    }

    [Fact]
    public async Task OnSaveAsync_CancelledSignature_SkipsPersistence()
    {
        var database = new DatabaseService();
        database.Suppliers.Add(new Supplier { Id = 8, Name = "Precision Labs" });

        var calibrationAdapter = new FakeCalibrationCrudService();
        var componentAdapter = new FakeComponentCrudService();

        await componentAdapter.CreateAsync(new Component
        {
            Id = 21,
            MachineId = 4,
            MachineName = "Sterilizer",
            Code = "CMP-21",
            Name = "Gauge",
            SopDoc = "SOP-CAL-021",
            Status = "active"
        }, ComponentCrudContext.Create(2, "127.0.0.1", "Unit", "session"));

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 11, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.10.0.5"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        signatureDialog.QueueCancellation();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachmentService = new TestAttachmentService();
        var audit = new AuditService(database);

        var viewModel = new CalibrationModuleViewModel(
            database,
            audit,
            calibrationAdapter,
            componentAdapter,
            auth,
            filePicker,
            attachmentService,
            signatureDialog,
            dialog,
            shell,
            navigation);

        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.ComponentId = componentAdapter.Saved[0].Id;
        viewModel.Editor.SupplierId = database.Suppliers[0].Id;
        viewModel.Editor.CalibrationDate = new DateTime(2025, 5, 5);
        viewModel.Editor.NextDue = new DateTime(2025, 11, 5);
        viewModel.Editor.Result = "PASS";
        viewModel.Editor.CertDoc = "CERT-2021.pdf";
        viewModel.Editor.Comment = "Cancelled flow";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.False(saved);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.Equal("Electronic signature cancelled. Save aborted.", viewModel.StatusMessage);
        Assert.Empty(calibrationAdapter.Saved);
        Assert.Empty(calibrationAdapter.SavedContexts);
        var request = Assert.Single(signatureDialog.Requests);
        Assert.Equal("calibrations", request.TableName);
        Assert.Equal(0, request.RecordId);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.Empty(signatureDialog.PersistedSignatureRecords);
    }

    [Fact]
    public async Task OnSaveAsync_CaptureException_SurfacesError()
    {
        var database = new DatabaseService();
        database.Suppliers.Add(new Supplier { Id = 12, Name = "Metrology Experts" });

        var calibrationAdapter = new FakeCalibrationCrudService();
        var componentAdapter = new FakeComponentCrudService();

        await componentAdapter.CreateAsync(new Component
        {
            Id = 31,
            MachineId = 6,
            MachineName = "Lyophilizer",
            Code = "CMP-31",
            Name = "Thermocouple",
            SopDoc = "SOP-CAL-031",
            Status = "active"
        }, ComponentCrudContext.Create(3, "127.0.0.1", "Unit", "session"));

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 13, FullName = "QA Supervisor" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.10.0.6"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        signatureDialog.QueueCaptureException(new InvalidOperationException("Capture failure"));
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachmentService = new TestAttachmentService();
        var audit = new AuditService(database);

        var viewModel = new CalibrationModuleViewModel(
            database,
            audit,
            calibrationAdapter,
            componentAdapter,
            auth,
            filePicker,
            attachmentService,
            signatureDialog,
            dialog,
            shell,
            navigation);

        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.ComponentId = componentAdapter.Saved[0].Id;
        viewModel.Editor.SupplierId = database.Suppliers[0].Id;
        viewModel.Editor.CalibrationDate = new DateTime(2025, 6, 6);
        viewModel.Editor.NextDue = new DateTime(2025, 12, 6);
        viewModel.Editor.Result = "PASS";
        viewModel.Editor.CertDoc = "CERT-2031.pdf";
        viewModel.Editor.Comment = "Exception flow";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.False(saved);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.Equal("Electronic signature failed: Capture failure", viewModel.StatusMessage);
        Assert.Empty(calibrationAdapter.Saved);
        Assert.Empty(calibrationAdapter.SavedContexts);
        var request = Assert.Single(signatureDialog.Requests);
        Assert.Equal("calibrations", request.TableName);
        Assert.Equal(0, request.RecordId);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.Empty(signatureDialog.PersistedSignatureRecords);
    }

    [Fact]
    public async Task AttachDocumentCommand_UploadsFiles_WhenCalibrationPersisted()
    {
        var database = new DatabaseService();
        database.Suppliers.Add(new Supplier { Id = 5, Name = "Calibrate Co" });

        var calibrationAdapter = new FakeCalibrationCrudService();
        var componentAdapter = new FakeComponentCrudService();

        await componentAdapter.CreateAsync(new Component
        {
            Id = 11,
            MachineId = 2,
            MachineName = "Bioreactor",
            Code = "CMP-11",
            Name = "Temperature Probe",
            SopDoc = "SOP-CAL-002",
            Status = "active"
        }, ComponentCrudContext.Create(1, "127.0.0.1", "Unit", "session"));

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 4, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.15"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var attachmentService = new TestAttachmentService();
        var filePicker = new TestFilePicker
        {
            Files = new[]
            {
                new PickedFile(
                    "calibration-cert.pdf",
                    "application/pdf",
                    () => Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2, 3 })))
            }
        };
        var audit = new AuditService(database);

        var viewModel = new CalibrationModuleViewModel(
            database,
            audit,
            calibrationAdapter,
            componentAdapter,
            auth,
            filePicker,
            attachmentService,
            signatureDialog,
            dialog,
            shell,
            navigation);

        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.ComponentId = componentAdapter.Saved[0].Id;
        viewModel.Editor.SupplierId = database.Suppliers[0].Id;
        viewModel.Editor.CalibrationDate = new DateTime(2025, 3, 10);
        viewModel.Editor.NextDue = new DateTime(2025, 9, 10);
        viewModel.Editor.Result = "PASS";
        viewModel.Editor.CertDoc = "CERT-1002.pdf";
        viewModel.Editor.Comment = "Periodic verification";

        var saved = await InvokeSaveAsync(viewModel);
        Assert.True(saved);
        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));

        await viewModel.AttachDocumentCommand.ExecuteAsync(null);

        Assert.Single(attachmentService.Uploads);
        var upload = attachmentService.Uploads[0];
        Assert.Equal("calibrations", upload.EntityType);
        Assert.Equal(calibrationAdapter.Saved[0].Id, upload.EntityId);
        Assert.Equal("calibration-cert.pdf", upload.FileName);
    }

    private static Task<bool> InvokeSaveAsync(CalibrationModuleViewModel viewModel)
    {
        var method = typeof(CalibrationModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(CalibrationModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }

    private sealed class TestCflDialogService : ICflDialogService
    {
        public Task<CflResult?> ShowAsync(CflRequest request) => Task.FromResult<CflResult?>(null);
    }

    private sealed class TestShellInteractionService : IShellInteractionService
    {
        public void UpdateStatus(string message) { }

        public void UpdateInspector(InspectorContext context) { }
    }

    private sealed class TestModuleNavigationService : IModuleNavigationService
    {
        public ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null)
            => throw new NotSupportedException();

        public void Activate(ModuleDocumentViewModel document) { }
    }
}
