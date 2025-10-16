using System;
using System.Collections.Generic;
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

public class ComponentsModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsComponentViaAdapter()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        const int adapterSignatureId = 9325;
        var componentAdapter = new FakeComponentCrudService
        {
            SignatureMetadataIdSource = _ => adapterSignatureId
        };
        var machineAdapter = new FakeMachineCrudService();

        await machineAdapter.CreateAsync(new Machine
        {
            Id = 1,
            Code = "AST-001",
            Name = "Autoclave",
            Manufacturer = "Steris",
            Location = "Suite A",
            UrsDoc = "URS-AUTO"
        }, MachineCrudContext.Create(1, "127.0.0.1", "TestRig", "unit"));

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 5, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.8"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = CreateViewModel(
            database,
            audit,
            componentAdapter,
            machineAdapter,
            auth,
            signatureDialog,
            dialog,
            shell,
            navigation);
        await viewModel.InitializeAsync(null).ConfigureAwait(false);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.MachineId = machineAdapter.Saved[0].Id;
        viewModel.Editor.Code = "CMP-100";
        viewModel.Editor.Name = "Pressure Sensor";
        viewModel.Editor.Type = "sensor";
        viewModel.Editor.SopDoc = "SOP-CMP-100";
        viewModel.Editor.Status = "active";
        viewModel.Editor.SerialNumber = "SN-001";
        viewModel.Editor.Supplier = "Contoso";
        viewModel.Editor.Comments = "Initial install";

        var saved = await InvokeSaveAsync(viewModel).ConfigureAwait(false);

        Assert.True(saved);
        Assert.Equal("Electronic signature captured (QA Reason).", viewModel.StatusMessage);
        Assert.Single(componentAdapter.Saved);
        var persisted = componentAdapter.Saved[0];
        Assert.Equal("Pressure Sensor", persisted.Name);
        Assert.Equal("CMP-100", persisted.Code);
        Assert.Equal(machineAdapter.Saved[0].Id, persisted.MachineId);
        Assert.Equal("SOP-CMP-100", persisted.SopDoc);
        Assert.Equal("test-signature", persisted.DigitalSignature);
        var context = Assert.Single(componentAdapter.SavedContexts);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("components", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        var capturedResult = Assert.Single(signatureDialog.CapturedResults);
        var signatureResult = Assert.NotNull(capturedResult);
        Assert.Equal(componentAdapter.Saved[0].Id, signatureResult.Signature.RecordId);
        Assert.Equal(adapterSignatureId, signatureResult.Signature.Id);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.Equal(0, signatureDialog.PersistInvocationCount);
    }

    [Fact]
    public async Task OnSaveAsync_AddMode_SignatureCancelled_LeavesEditorDirtyAndSkipsPersist()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var componentAdapter = new CapturingComponentCrudService();
        var machineAdapter = new FakeMachineCrudService();

        await machineAdapter.CreateAsync(new Machine
        {
            Id = 1,
            Code = "AST-001",
            Name = "Autoclave",
            Manufacturer = "Steris",
            Location = "Suite A",
            UrsDoc = "URS-AUTO"
        }, MachineCrudContext.Create(1, "127.0.0.1", "TestRig", "unit"));

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 5, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.8"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        signatureDialog.QueueCancellation();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = CreateViewModel(
            database,
            audit,
            componentAdapter,
            machineAdapter,
            auth,
            signatureDialog,
            dialog,
            shell,
            navigation);
        await viewModel.InitializeAsync(null).ConfigureAwait(false);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.MachineId = machineAdapter.Saved[0].Id;
        viewModel.Editor.Code = "CMP-100";
        viewModel.Editor.Name = "Pressure Sensor";
        viewModel.Editor.Type = "sensor";
        viewModel.Editor.SopDoc = "SOP-CMP-100";
        viewModel.Editor.Status = "active";
        viewModel.Editor.SerialNumber = "SN-001";
        viewModel.Editor.Supplier = "Contoso";
        viewModel.Editor.Comments = "Initial install";

        var saved = await InvokeSaveAsync(viewModel).ConfigureAwait(false);

        Assert.False(saved);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.Equal("Electronic signature cancelled. Save aborted.", viewModel.StatusMessage);
        Assert.Empty(componentAdapter.Saved);
        Assert.Empty(signatureDialog.PersistedResults);
    }

    [Fact]
    public async Task OnSaveAsync_AddMode_SignatureCaptureThrows_SetsStatusAndSkipsPersist()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var componentAdapter = new CapturingComponentCrudService();
        var machineAdapter = new FakeMachineCrudService();

        await machineAdapter.CreateAsync(new Machine
        {
            Id = 1,
            Code = "AST-001",
            Name = "Autoclave",
            Manufacturer = "Steris",
            Location = "Suite A",
            UrsDoc = "URS-AUTO"
        }, MachineCrudContext.Create(1, "127.0.0.1", "TestRig", "unit"));

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 5, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.8"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        signatureDialog.QueueCaptureException(new InvalidOperationException("Dialog offline"));
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = CreateViewModel(
            database,
            audit,
            componentAdapter,
            machineAdapter,
            auth,
            signatureDialog,
            dialog,
            shell,
            navigation);
        await viewModel.InitializeAsync(null).ConfigureAwait(false);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.MachineId = machineAdapter.Saved[0].Id;
        viewModel.Editor.Code = "CMP-100";
        viewModel.Editor.Name = "Pressure Sensor";
        viewModel.Editor.Type = "sensor";
        viewModel.Editor.SopDoc = "SOP-CMP-100";
        viewModel.Editor.Status = "active";
        viewModel.Editor.SerialNumber = "SN-001";
        viewModel.Editor.Supplier = "Contoso";
        viewModel.Editor.Comments = "Initial install";

        var saved = await InvokeSaveAsync(viewModel).ConfigureAwait(false);

        Assert.False(saved);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.Equal("Electronic signature failed: Dialog offline", viewModel.StatusMessage);
        Assert.Empty(componentAdapter.Saved);
        Assert.Empty(signatureDialog.PersistedResults);
    }

    [Fact]
    public async Task OnSaveAsync_AddMode_RegeneratesIdentifiersBeforePersist()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var componentAdapter = new CapturingComponentCrudService();
        var machineAdapter = new FakeMachineCrudService();

        await machineAdapter.CreateAsync(new Machine
        {
            Id = 2,
            Code = "AST-010",
            Name = "Autoclave",
            Manufacturer = "Steris",
            Location = "Suite B",
            UrsDoc = "URS-AUTO"
        }, MachineCrudContext.Create(7, "127.0.0.1", "TestRig", "unit"));

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 9, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.9"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachmentService = new TestAttachmentService();
        var attachments = new AttachmentWorkflowService(attachmentService, database, new AttachmentEncryptionOptions(), audit);
        var codeGenerator = new StubCodeGeneratorService();
        var qrCodeService = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = CreateViewModel(
            database,
            audit,
            componentAdapter,
            machineAdapter,
            auth,
            signatureDialog,
            dialog,
            shell,
            navigation,
            filePicker,
            attachments,
            localization: new StubLocalizationService(),
            codeGeneratorService: codeGenerator,
            qrCodeService: qrCodeService,
            platformService: platformService);

        await viewModel.InitializeAsync(null).ConfigureAwait(false);

        viewModel.Mode = FormMode.Add;
        await Task.Yield();

        viewModel.Editor.Name = "Pressure Sensor";
        viewModel.Editor.MachineId = machineAdapter.Saved[0].Id;
        viewModel.Editor.SopDoc = "SOP-CMP-200";
        viewModel.Editor.Status = "active";
        viewModel.Editor.Code = string.Empty;
        viewModel.Editor.QrPayload = string.Empty;
        viewModel.Editor.QrCode = string.Empty;

        var saved = await InvokeSaveAsync(viewModel).ConfigureAwait(false);

        Assert.True(saved);
        var captured = Assert.Single(componentAdapter.CapturedEntities);
        Assert.False(string.IsNullOrWhiteSpace(captured.Code));
        Assert.False(string.IsNullOrWhiteSpace(captured.QrPayload));
        Assert.False(string.IsNullOrWhiteSpace(captured.QrCode));
        Assert.Equal(captured.Code, viewModel.Editor.Code);
        Assert.Equal(captured.QrPayload, viewModel.Editor.QrPayload);
        Assert.Equal(captured.QrCode, viewModel.Editor.QrCode);
        var expectedPayload = $"yasgmp://component/{Uri.EscapeDataString(captured.Code)}?machine={captured.MachineId}";
        Assert.Equal(expectedPayload, qrCodeService.LastPayload);
        Assert.True(File.Exists(viewModel.Editor.QrCode));
    }

    [Fact]
    public async Task OnSaveAsync_UpdateMode_RegeneratesIdentifiersBeforePersist()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var componentAdapter = new CapturingComponentCrudService();
        var machineAdapter = new FakeMachineCrudService();

        await machineAdapter.CreateAsync(new Machine
        {
            Id = 3,
            Code = "AST-020",
            Name = "Lyophilizer",
            Manufacturer = "Contoso",
            Location = "Suite C",
            UrsDoc = "URS-LYO"
        }, MachineCrudContext.Create(11, "127.0.0.1", "TestRig", "unit"));

        componentAdapter.Saved.Add(new Component
        {
            Id = 10,
            MachineId = machineAdapter.Saved[0].Id,
            MachineName = "Lyophilizer",
            Name = "Temperature Probe",
            Code = "CMP-777",
            SopDoc = "SOP-CMP-777",
            Status = "active",
            QrPayload = "yasgmp://component/CMP-777?machine=3",
            QrCode = Path.Combine(Path.GetTempPath(), "Components", "QrCodes", "CMP-777.png")
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 12, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.12"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachmentService = new TestAttachmentService();
        var attachments = new AttachmentWorkflowService(attachmentService, database, new AttachmentEncryptionOptions(), audit);
        var codeGenerator = new StubCodeGeneratorService();
        var qrCodeService = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = CreateViewModel(
            database,
            audit,
            componentAdapter,
            machineAdapter,
            auth,
            signatureDialog,
            dialog,
            shell,
            navigation,
            filePicker,
            attachments,
            localization: new StubLocalizationService(),
            codeGeneratorService: codeGenerator,
            qrCodeService: qrCodeService,
            platformService: platformService);

        await viewModel.InitializeAsync(null).ConfigureAwait(false);
        var record = Assert.Single(viewModel.Records);
        await InvokeRecordSelectedAsync(viewModel, record).ConfigureAwait(false);

        viewModel.Mode = FormMode.Update;
        await Task.Yield();

        viewModel.Editor.Code = string.Empty;
        viewModel.Editor.QrPayload = string.Empty;
        viewModel.Editor.QrCode = string.Empty;

        var saved = await InvokeSaveAsync(viewModel).ConfigureAwait(false);

        Assert.True(saved);
        var captured = Assert.Single(componentAdapter.CapturedEntities);
        Assert.Equal(10, captured.Id);
        Assert.False(string.IsNullOrWhiteSpace(captured.Code));
        Assert.False(string.IsNullOrWhiteSpace(captured.QrPayload));
        Assert.False(string.IsNullOrWhiteSpace(captured.QrCode));
        Assert.Equal(captured.Code, viewModel.Editor.Code);
        Assert.Equal(captured.QrPayload, viewModel.Editor.QrPayload);
        Assert.Equal(captured.QrCode, viewModel.Editor.QrCode);
        var expectedPayload = $"yasgmp://component/{Uri.EscapeDataString(captured.Code)}?machine={captured.MachineId}";
        Assert.Equal(expectedPayload, qrCodeService.LastPayload);
        Assert.True(File.Exists(viewModel.Editor.QrCode));
    }

    [Fact]
    public async Task GenerateCodeCommand_RegeneratesIdentifiers_WhenExecuted()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var componentAdapter = new FakeComponentCrudService();
        var machineAdapter = new FakeMachineCrudService();

        await machineAdapter.CreateAsync(new Machine
        {
            Id = 1,
            Name = "Autoclave",
            Manufacturer = "Steris"
        }, MachineCrudContext.Create(1, "127.0.0.1", "TestRig", "unit"));

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachmentService = new TestAttachmentService();
        var attachments = new AttachmentWorkflowService(attachmentService, database, new AttachmentEncryptionOptions(), audit);
        var codeGenerator = new StubCodeGeneratorService();
        var qrCodeService = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = CreateViewModel(
            database,
            audit,
            componentAdapter,
            machineAdapter,
            auth,
            signatureDialog,
            dialog,
            shell,
            navigation,
            filePicker,
            attachments,
            localization: new StubLocalizationService(),
            codeGeneratorService: codeGenerator,
            qrCodeService: qrCodeService,
            platformService: platformService);

        await viewModel.InitializeAsync(null).ConfigureAwait(false);

        viewModel.Mode = FormMode.Add;
        await Task.Yield();

        viewModel.Editor.Name = "Temperature Probe";
        viewModel.Editor.MachineId = machineAdapter.Saved[0].Id;
        viewModel.Editor.Code = string.Empty;
        viewModel.Editor.QrPayload = string.Empty;
        viewModel.Editor.QrCode = string.Empty;

        Assert.True(viewModel.GenerateCodeCommand.CanExecute(null));
        await viewModel.GenerateCodeCommand.ExecuteAsync(null);

        Assert.Equal("Temperature Probe", codeGenerator.LastName);
        Assert.Equal("Autoclave", codeGenerator.LastManufacturer);
        Assert.False(string.IsNullOrWhiteSpace(viewModel.Editor.Code));
        var expectedPayload = $"yasgmp://component/{Uri.EscapeDataString(viewModel.Editor.Code)}?machine={viewModel.Editor.MachineId}";
        Assert.Equal(expectedPayload, viewModel.Editor.QrPayload);
        Assert.True(File.Exists(viewModel.Editor.QrCode));
    }

    [Fact]
    public async Task GenerateCodeCommand_AddMode_PersistsGeneratedIdentifiersOnSave()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var componentAdapter = new FakeComponentCrudService();
        var machineAdapter = new FakeMachineCrudService();

        await machineAdapter.CreateAsync(new Machine
        {
            Id = 15,
            Name = "Lyophilizer",
            Manufacturer = "Fabrikam"
        }, MachineCrudContext.Create(5, "127.0.0.1", "TestRig", "unit"));

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 3, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.44"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachmentService = new TestAttachmentService();
        var attachments = new AttachmentWorkflowService(attachmentService, database, new AttachmentEncryptionOptions(), audit);
        var codeGenerator = new StubCodeGeneratorService();
        var qrCodeService = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = CreateViewModel(
            database,
            audit,
            componentAdapter,
            machineAdapter,
            auth,
            signatureDialog,
            dialog,
            shell,
            navigation,
            filePicker,
            attachments,
            localization: new StubLocalizationService(),
            codeGeneratorService: codeGenerator,
            qrCodeService: qrCodeService,
            platformService: platformService);

        await viewModel.InitializeAsync(null).ConfigureAwait(false);

        viewModel.Mode = FormMode.Add;
        await Task.Yield();

        viewModel.Editor.Name = "Temp Probe";
        viewModel.Editor.MachineId = machineAdapter.Saved[0].Id;
        viewModel.Editor.Code = string.Empty;
        viewModel.Editor.QrPayload = string.Empty;
        viewModel.Editor.QrCode = string.Empty;

        await viewModel.GenerateCodeCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsDirty);
        Assert.False(string.IsNullOrWhiteSpace(viewModel.Editor.Code));
        Assert.False(string.IsNullOrWhiteSpace(viewModel.Editor.QrPayload));
        Assert.True(File.Exists(viewModel.Editor.QrCode));

        var saved = await InvokeSaveAsync(viewModel).ConfigureAwait(false);

        Assert.True(saved);
        var persisted = Assert.Single(componentAdapter.Saved);
        Assert.Equal(viewModel.Editor.Code, persisted.Code);
        Assert.Equal(viewModel.Editor.QrPayload, persisted.QrPayload);
        Assert.Equal(viewModel.Editor.QrCode, persisted.QrCode);
        var expectedPayload = $"yasgmp://component/{Uri.EscapeDataString(persisted.Code)}?machine={persisted.MachineId}";
        Assert.Equal(expectedPayload, persisted.QrPayload);
    }

    [Fact]
    public async Task PreviewQrCommand_PreviewsGeneratedImage()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var componentAdapter = new FakeComponentCrudService();
        var machineAdapter = new FakeMachineCrudService();

        await machineAdapter.CreateAsync(new Machine
        {
            Id = 11,
            Name = "Filling Line",
            Manufacturer = "Fabrikam"
        }, MachineCrudContext.Create(2, "127.0.0.1", "TestRig", "unit"));

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachmentService = new TestAttachmentService();
        var attachments = new AttachmentWorkflowService(attachmentService, database, new AttachmentEncryptionOptions(), audit);
        var codeGenerator = new StubCodeGeneratorService();
        var qrCodeService = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = CreateViewModel(
            database,
            audit,
            componentAdapter,
            machineAdapter,
            auth,
            signatureDialog,
            dialog,
            shell,
            navigation,
            filePicker,
            attachments,
            localization: new StubLocalizationService(),
            codeGeneratorService: codeGenerator,
            qrCodeService: qrCodeService,
            platformService: platformService);

        await viewModel.InitializeAsync(null).ConfigureAwait(false);

        viewModel.Mode = FormMode.Add;
        await Task.Yield();

        viewModel.Editor.Name = "Flow Meter";
        viewModel.Editor.MachineId = machineAdapter.Saved[0].Id;

        await viewModel.GenerateCodeCommand.ExecuteAsync(null);
        var expectedPath = viewModel.Editor.QrCode;
        shell.PreviewedDocuments.Clear();

        Assert.True(viewModel.PreviewQrCommand.CanExecute(null));
        await viewModel.PreviewQrCommand.ExecuteAsync(null);

        Assert.Contains(expectedPath, shell.PreviewedDocuments);
        Assert.Equal($"QR generated at {expectedPath}.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task GenerateCodeCommand_UsesPlatformDirectoryAndUpdatesStatusMessage()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var componentAdapter = new FakeComponentCrudService();
        var machineAdapter = new FakeMachineCrudService();

        await machineAdapter.CreateAsync(new Machine
        {
            Id = 15,
            Name = "Autoclave",
            Manufacturer = "Steris"
        }, MachineCrudContext.Create(4, "127.0.0.1", "TestRig", "unit"));

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachmentService = new TestAttachmentService();
        var attachments = new AttachmentWorkflowService(attachmentService, database, new AttachmentEncryptionOptions(), audit);
        var codeGenerator = new StubCodeGeneratorService();
        var qrCodeService = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = CreateViewModel(
            database,
            audit,
            componentAdapter,
            machineAdapter,
            auth,
            signatureDialog,
            dialog,
            shell,
            navigation,
            filePicker,
            attachments,
            localization: new StubLocalizationService(),
            codeGeneratorService: codeGenerator,
            qrCodeService: qrCodeService,
            platformService: platformService);

        await viewModel.InitializeAsync(null).ConfigureAwait(false);

        viewModel.Mode = FormMode.Add;
        await Task.Yield();

        viewModel.Editor.Name = "Temperature Probe";
        viewModel.Editor.MachineId = machineAdapter.Saved[0].Id;
        viewModel.Editor.Code = string.Empty;
        viewModel.Editor.QrPayload = string.Empty;
        viewModel.Editor.QrCode = string.Empty;

        Assert.True(viewModel.GenerateCodeCommand.CanExecute(null));
        await viewModel.GenerateCodeCommand.ExecuteAsync(null);

        Assert.Equal("Temperature Probe", codeGenerator.LastName);
        Assert.Equal("Steris", codeGenerator.LastManufacturer);
        var expectedPayload = $"yasgmp://component/{Uri.EscapeDataString(viewModel.Editor.Code)}?machine={viewModel.Editor.MachineId}";
        Assert.Equal(expectedPayload, qrCodeService.LastPayload);
        var directory = Path.GetDirectoryName(viewModel.Editor.QrCode);
        Assert.Equal(Path.Combine(platformService.GetAppDataDirectory(), "Components", "QrCodes"), directory);
        Assert.True(File.Exists(viewModel.Editor.QrCode));
        Assert.Equal($"Generated component code {viewModel.Editor.Code} and QR image at {viewModel.Editor.QrCode}.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task PreviewQrCommand_RegeneratesImageWhenEditorPathMissing()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var componentAdapter = new FakeComponentCrudService();
        var machineAdapter = new FakeMachineCrudService();

        await machineAdapter.CreateAsync(new Machine
        {
            Id = 16,
            Name = "Filling Line",
            Manufacturer = "Fabrikam"
        }, MachineCrudContext.Create(5, "127.0.0.1", "TestRig", "unit"));

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachmentService = new TestAttachmentService();
        var attachments = new AttachmentWorkflowService(attachmentService, database, new AttachmentEncryptionOptions(), audit);
        var codeGenerator = new StubCodeGeneratorService();
        var qrCodeService = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = CreateViewModel(
            database,
            audit,
            componentAdapter,
            machineAdapter,
            auth,
            signatureDialog,
            dialog,
            shell,
            navigation,
            filePicker,
            attachments,
            localization: new StubLocalizationService(),
            codeGeneratorService: codeGenerator,
            qrCodeService: qrCodeService,
            platformService: platformService);

        await viewModel.InitializeAsync(null).ConfigureAwait(false);

        viewModel.Mode = FormMode.Add;
        await Task.Yield();

        viewModel.Editor.Name = "Flow Meter";
        viewModel.Editor.MachineId = machineAdapter.Saved[0].Id;

        await viewModel.GenerateCodeCommand.ExecuteAsync(null);
        var payload = viewModel.Editor.QrPayload;

        viewModel.Editor.QrCode = string.Empty;
        shell.PreviewedDocuments.Clear();

        Assert.True(viewModel.PreviewQrCommand.CanExecute(null));
        await viewModel.PreviewQrCommand.ExecuteAsync(null);

        Assert.False(string.IsNullOrWhiteSpace(viewModel.Editor.QrCode));
        Assert.Equal(payload, qrCodeService.LastPayload);
        Assert.Contains(viewModel.Editor.QrCode, shell.PreviewedDocuments);
        Assert.Equal($"QR generated at {viewModel.Editor.QrCode}.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task CommandCanExecute_TransitionsAcrossModesAndBusyStates()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var componentAdapter = new FakeComponentCrudService();
        var machineAdapter = new FakeMachineCrudService();

        await machineAdapter.CreateAsync(new Machine
        {
            Id = 25,
            Name = "Reactor",
            Manufacturer = "Northwind"
        }, MachineCrudContext.Create(6, "127.0.0.1", "TestRig", "unit"));

        componentAdapter.Saved.Add(new Component
        {
            Id = 30,
            MachineId = machineAdapter.Saved[0].Id,
            MachineName = "Reactor",
            Name = "Control Valve",
            Code = "CMP-VALVE",
            SopDoc = "SOP-VALVE",
            Status = "active",
            QrPayload = "yasgmp://component/CMP-VALVE?machine=25",
            QrCode = Path.Combine(Path.GetTempPath(), "Components", "QrCodes", "CMP-VALVE.png")
        });

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = CreateViewModel(
            database,
            audit,
            componentAdapter,
            machineAdapter,
            auth,
            signatureDialog,
            dialog,
            shell,
            navigation,
            localization: new StubLocalizationService());

        await viewModel.InitializeAsync(null).ConfigureAwait(false);
        var record = Assert.Single(viewModel.Records);
        await InvokeRecordSelectedAsync(viewModel, record).ConfigureAwait(false);
        InvokeUpdateCommandStates(viewModel);

        Assert.False(viewModel.GenerateCodeCommand.CanExecute(null));
        Assert.True(viewModel.PreviewQrCommand.CanExecute(null));
        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));

        viewModel.IsBusy = true;
        InvokeUpdateCommandStates(viewModel);
        Assert.False(viewModel.GenerateCodeCommand.CanExecute(null));
        Assert.False(viewModel.PreviewQrCommand.CanExecute(null));
        Assert.False(viewModel.AttachDocumentCommand.CanExecute(null));

        viewModel.IsBusy = false;
        viewModel.Mode = FormMode.Add;
        InvokeUpdateCommandStates(viewModel);
        Assert.True(viewModel.GenerateCodeCommand.CanExecute(null));
        Assert.False(viewModel.PreviewQrCommand.CanExecute(null));
        Assert.False(viewModel.AttachDocumentCommand.CanExecute(null));

        viewModel.Mode = FormMode.View;
        await InvokeRecordSelectedAsync(viewModel, record).ConfigureAwait(false);
        InvokeUpdateCommandStates(viewModel);
        Assert.False(viewModel.GenerateCodeCommand.CanExecute(null));
        Assert.True(viewModel.PreviewQrCommand.CanExecute(null));
        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));

        viewModel.Mode = FormMode.Update;
        InvokeUpdateCommandStates(viewModel);
        Assert.True(viewModel.GenerateCodeCommand.CanExecute(null));
        Assert.True(viewModel.PreviewQrCommand.CanExecute(null));
        Assert.False(viewModel.AttachDocumentCommand.CanExecute(null));

        viewModel.IsBusy = true;
        InvokeUpdateCommandStates(viewModel);
        Assert.False(viewModel.GenerateCodeCommand.CanExecute(null));
        Assert.False(viewModel.PreviewQrCommand.CanExecute(null));
        Assert.False(viewModel.AttachDocumentCommand.CanExecute(null));
    }

    [Fact]
    public async Task AttachDocumentCommand_UploadsAttachmentViaWorkflow()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var componentAdapter = new FakeComponentCrudService();
        var machineAdapter = new FakeMachineCrudService();

        await machineAdapter.CreateAsync(new Machine
        {
            Id = 21,
            Name = "Mixer",
            Manufacturer = "Contoso"
        }, MachineCrudContext.Create(3, "127.0.0.1", "TestRig", "unit"));

        componentAdapter.Saved.Add(new Component
        {
            Id = 5,
            MachineId = machineAdapter.Saved[0].Id,
            Name = "Pressure Sensor",
            Code = "CMP-500",
            SopDoc = "SOP-CMP-500",
            Status = "active"
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 17, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.42"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachmentService = new TestAttachmentService();
        var attachments = new AttachmentWorkflowService(attachmentService, database, new AttachmentEncryptionOptions(), audit);

        var viewModel = CreateViewModel(
            database,
            audit,
            componentAdapter,
            machineAdapter,
            auth,
            signatureDialog,
            dialog,
            shell,
            navigation,
            filePicker,
            attachments);

        await viewModel.InitializeAsync(null).ConfigureAwait(false);
        var record = Assert.Single(viewModel.Records);
        await InvokeRecordSelectedAsync(viewModel, record).ConfigureAwait(false);

        var bytes = Encoding.UTF8.GetBytes("component attachment");
        filePicker.Files = new[]
        {
            new PickedFile(
                "component.txt",
                "text/plain",
                () => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)),
                bytes.Length)
        };

        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));
        await viewModel.AttachDocumentCommand.ExecuteAsync(null);

        var upload = Assert.Single(attachmentService.Uploads);
        Assert.Equal("components", upload.EntityType);
        Assert.Equal(componentAdapter.Saved[0].Id, upload.EntityId);
        Assert.Equal($"component:{componentAdapter.Saved[0].Id}", upload.Reason);
    }

    private static ComponentsModuleViewModel CreateViewModel(
        DatabaseService database,
        AuditService audit,
        FakeComponentCrudService componentAdapter,
        FakeMachineCrudService machineAdapter,
        TestAuthContext auth,
        TestElectronicSignatureDialogService signatureDialog,
        TestCflDialogService dialog,
        TestShellInteractionService shell,
        TestModuleNavigationService navigation,
        IFilePicker? filePicker = null,
        IAttachmentWorkflowService? attachmentWorkflow = null,
        ILocalizationService? localization = null,
        ICodeGeneratorService? codeGeneratorService = null,
        IQRCodeService? qrCodeService = null,
        IPlatformService? platformService = null)
        => CreateViewModel(
            database,
            audit,
            (IComponentCrudService)componentAdapter,
            machineAdapter,
            auth,
            signatureDialog,
            dialog,
            shell,
            navigation,
            filePicker,
            attachmentWorkflow,
            localization,
            codeGeneratorService,
            qrCodeService,
            platformService);

    private static ComponentsModuleViewModel CreateViewModel(
        DatabaseService database,
        AuditService audit,
        IComponentCrudService componentAdapter,
        FakeMachineCrudService machineAdapter,
        TestAuthContext auth,
        TestElectronicSignatureDialogService signatureDialog,
        TestCflDialogService dialog,
        TestShellInteractionService shell,
        TestModuleNavigationService navigation,
        IFilePicker? filePicker = null,
        IAttachmentWorkflowService? attachmentWorkflow = null,
        ILocalizationService? localization = null,
        ICodeGeneratorService? codeGeneratorService = null,
        IQRCodeService? qrCodeService = null,
        IPlatformService? platformService = null)
    {
        localization ??= new StubLocalizationService();
        filePicker ??= new TestFilePicker();
        codeGeneratorService ??= new StubCodeGeneratorService();
        qrCodeService ??= new StubQrCodeService();
        platformService ??= new StubPlatformService();
        attachmentWorkflow ??= new AttachmentWorkflowService(
            new TestAttachmentService(),
            database,
            new AttachmentEncryptionOptions(),
            audit);

        return new ComponentsModuleViewModel(
            database,
            audit,
            componentAdapter,
            machineAdapter,
            auth,
            filePicker,
            attachmentWorkflow,
            signatureDialog,
            dialog,
            shell,
            navigation,
            localization,
            codeGeneratorService,
            qrCodeService,
            platformService);
    }

    private static Task<bool> InvokeSaveAsync(ComponentsModuleViewModel viewModel)
    {
        var method = typeof(ComponentsModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(ComponentsModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }

    private static void InvokeUpdateCommandStates(ComponentsModuleViewModel viewModel)
    {
        var method = typeof(ComponentsModuleViewModel)
            .GetMethod("UpdateCommandStates", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(ComponentsModuleViewModel), "UpdateCommandStates");
        method.Invoke(viewModel, null);
    }

    private static Task InvokeRecordSelectedAsync(ComponentsModuleViewModel viewModel, ModuleRecord? record)
    {
        var method = typeof(ComponentsModuleViewModel)
            .GetMethod("OnRecordSelectedAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(ComponentsModuleViewModel), "OnRecordSelectedAsync");
        return (Task)method.Invoke(viewModel, new object?[] { record })!;
    }

    private sealed class CapturingComponentCrudService : IComponentCrudService
    {
        private readonly FakeComponentCrudService _inner = new();

        public List<Component> CapturedEntities { get; } = new();

        public List<ComponentCrudContext> CapturedContexts { get; } = new();

        public List<Component> Saved => _inner.Saved;

        public IEnumerable<ComponentCrudContext> SavedContexts => _inner.SavedContexts;

        public Task<IReadOnlyList<Component>> GetAllAsync()
            => _inner.GetAllAsync();

        public Task<Component?> TryGetByIdAsync(int id)
            => _inner.TryGetByIdAsync(id);

        public Task<CrudSaveResult> CreateAsync(Component component, ComponentCrudContext context)
        {
            Capture(component, context);
            return _inner.CreateAsync(component, context);
        }

        public Task<CrudSaveResult> UpdateAsync(Component component, ComponentCrudContext context)
        {
            Capture(component, context);
            return _inner.UpdateAsync(component, context);
        }

        public void Validate(Component component)
            => _inner.Validate(component);

        public string NormalizeStatus(string? status)
            => _inner.NormalizeStatus(status);

        private void Capture(Component component, ComponentCrudContext context)
        {
            CapturedEntities.Add(Clone(component));
            CapturedContexts.Add(context);
        }

        private static Component Clone(Component component)
            => new()
            {
                Id = component.Id,
                MachineId = component.MachineId,
                MachineName = component.MachineName,
                Code = component.Code,
                Name = component.Name,
                Type = component.Type,
                SopDoc = component.SopDoc,
                Status = component.Status,
                InstallDate = component.InstallDate,
                SerialNumber = component.SerialNumber,
                Supplier = component.Supplier,
                WarrantyUntil = component.WarrantyUntil,
                Comments = component.Comments,
                LifecycleState = component.LifecycleState,
                QrCode = component.QrCode,
                QrPayload = component.QrPayload,
                CodeOverride = component.CodeOverride,
                IsCodeOverrideEnabled = component.IsCodeOverrideEnabled
            };
    }

    private sealed class TestCflDialogService : ICflDialogService
    {
        public Task<CflResult?> ShowAsync(CflRequest request)
            => Task.FromResult<CflResult?>(null);
    }

    private sealed class TestModuleNavigationService : IModuleNavigationService
    {
        public void Activate(ModuleDocumentViewModel document)
        {
        }

        public ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null)
            => throw new NotSupportedException();
    }

    private sealed class StubLocalizationService : ILocalizationService
    {
        private static readonly Dictionary<string, string> Resources = new()
        {
            ["Module.Status.Filtered"] = "Filtered {0} by \"{1}\".",
            ["Module.Components.Status.ComponentNotFound"] = "Unable to locate component #{0}.",
            ["Module.Components.Status.CodeAndQrGenerated"] = "Generated component code {0} and QR image at {1}.",
            ["Module.Components.Status.QrGenerated"] = "QR generated at {0}.",
            ["Module.Components.Status.QrPathUnavailable"] = "QR path unavailable.",
            ["Module.Components.Status.QrGenerationFailed"] = "QR generation failed: {0}",
            ["Module.Components.Status.SelectBeforeSave"] = "Select a component before saving.",
            ["Module.Components.Status.SignatureFailed"] = "Electronic signature failed: {0}",
            ["Module.Components.Status.SignatureCancelled"] = "Electronic signature cancelled. Save aborted.",
            ["Module.Components.Status.SignatureNotCaptured"] = "Electronic signature was not captured.",
            ["Module.Components.Status.SignaturePersistenceFailed"] = "Failed to persist electronic signature: {0}",
            ["Module.Components.Status.SignatureCaptured"] = "Electronic signature captured ({0}).",
            ["Module.Components.Status.SaveBeforeAttachment"] = "Save the component before adding attachments.",
            ["Module.Components.Status.AttachmentCancelled"] = "Attachment upload cancelled.",
            ["Module.Components.Status.AttachmentFailed"] = "Attachment upload failed: {0}"
        };

        public string CurrentLanguage { get; private set; } = "en";

        public event EventHandler? LanguageChanged;

        public string GetString(string key)
            => Resources.TryGetValue(key, out var value) ? value : key;

        public void SetLanguage(string language)
        {
            CurrentLanguage = language;
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
