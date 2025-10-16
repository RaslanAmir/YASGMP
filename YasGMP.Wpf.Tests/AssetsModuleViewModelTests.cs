using System;
using System.Collections.Generic;
using System.Globalization;
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

public class AssetsModuleViewModelTests : IDisposable
{
    private readonly LocalizationService _localization = new();
    private readonly string _originalLanguage;

    public AssetsModuleViewModelTests()
    {
        _originalLanguage = _localization.CurrentLanguage;
        _localization.SetLanguage("en");
    }

    [Fact]
    public async Task EnterAddMode_AutoGeneratesIdentifiers()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var machineAdapter = new FakeMachineCrudService();
        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var codeGenerator = new StubCodeGeneratorService();
        var qrCode = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = new AssetsModuleViewModel(
            database,
            audit,
            machineAdapter,
            auth,
            filePicker,
            attachments,
            signatureDialog,
            dialog,
            shell,
            navigation,
            _localization,
            codeGenerator,
            qrCode,
            platformService);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        await Task.Delay(50);

        Assert.False(string.IsNullOrWhiteSpace(viewModel.Editor.Code));
        Assert.False(string.IsNullOrWhiteSpace(viewModel.Editor.QrPayload));
        Assert.False(string.IsNullOrWhiteSpace(viewModel.Editor.QrCode));
        Assert.True(File.Exists(viewModel.Editor.QrCode));

        var expectedPayload = $"yasgmp://machine/{Uri.EscapeDataString(viewModel.Editor.Code)}";
        Assert.Equal(expectedPayload, viewModel.Editor.QrPayload);
        Assert.Equal(expectedPayload, qrCode.LastPayload);
        var expectedStatus = _localization.GetString("Module.Assets.Status.CodeAndQrGenerated", viewModel.Editor.Code, viewModel.Editor.QrCode);
        Assert.Equal(expectedStatus, viewModel.StatusMessage);
    }

    [Fact]
    public async Task GenerateCodeCommand_RegeneratesIdentifiersAndMarksDirty()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var machineAdapter = new FakeMachineCrudService();
        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var codeGenerator = new StubCodeGeneratorService();
        var qrCode = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = new AssetsModuleViewModel(
            database,
            audit,
            machineAdapter,
            auth,
            filePicker,
            attachments,
            signatureDialog,
            dialog,
            shell,
            navigation,
            _localization,
            codeGenerator,
            qrCode,
            platformService);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        await Task.Delay(50);
        var originalCode = viewModel.Editor.Code;

        viewModel.Editor.Name = "Lyophilizer";
        viewModel.Editor.Manufacturer = "Contoso";

        await viewModel.GenerateCodeCommand.ExecuteAsync(null);

        Assert.NotEqual(originalCode, viewModel.Editor.Code);
        Assert.True(viewModel.IsDirty);
        Assert.Equal("Lyophilizer", codeGenerator.LastName);
        Assert.Equal("Contoso", codeGenerator.LastManufacturer);

        var expectedPayload = $"yasgmp://machine/{Uri.EscapeDataString(viewModel.Editor.Code)}";
        Assert.Equal(expectedPayload, viewModel.Editor.QrPayload);
        Assert.Equal(expectedPayload, qrCode.LastPayload);
        Assert.True(File.Exists(viewModel.Editor.QrCode));

        var expectedStatus = _localization.GetString("Module.Assets.Status.CodeAndQrGenerated", viewModel.Editor.Code, viewModel.Editor.QrCode);
        Assert.Equal(expectedStatus, viewModel.StatusMessage);
    }

    [Fact]
    public async Task PreviewQrCommand_UsesExistingCodeAndUpdatesPayload()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var machineAdapter = new FakeMachineCrudService();
        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var codeGenerator = new StubCodeGeneratorService();
        var qrCode = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = new AssetsModuleViewModel(
            database,
            audit,
            machineAdapter,
            auth,
            filePicker,
            attachments,
            signatureDialog,
            dialog,
            shell,
            navigation,
            _localization,
            codeGenerator,
            qrCode,
            platformService);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        await Task.Delay(50);

        viewModel.Editor.Code = "AST-CUSTOM";
        viewModel.Editor.QrPayload = string.Empty;
        viewModel.Editor.QrCode = string.Empty;

        await viewModel.PreviewQrCommand.ExecuteAsync(null);

        Assert.Equal("AST-CUSTOM", viewModel.Editor.Code);
        var expectedPayload = $"yasgmp://machine/{Uri.EscapeDataString(viewModel.Editor.Code)}";
        Assert.Equal(expectedPayload, viewModel.Editor.QrPayload);
        Assert.Equal(expectedPayload, qrCode.LastPayload);
        Assert.True(File.Exists(viewModel.Editor.QrCode));

        var expectedStatus = _localization.GetString("Module.Assets.Status.QrGenerated", viewModel.Editor.QrCode);
        Assert.Equal(expectedStatus, viewModel.StatusMessage);
    }

    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsMachineThroughAdapter()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        const int adapterSignatureId = 4321;
        var machineAdapter = new FakeMachineCrudService
        {
            SignatureMetadataIdSource = _ => adapterSignatureId
        };
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 7, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.25"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var codeGenerator = new StubCodeGeneratorService();
        var qrCode = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = new AssetsModuleViewModel(
            database,
            audit,
            machineAdapter,
            auth,
            filePicker,
            attachments,
            signatureDialog,
            dialog,
            shell,
            navigation,
            _localization,
            codeGenerator,
            qrCode,
            platformService);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Code = "AST-500";
        viewModel.Editor.Name = "Lyophilizer";
        viewModel.Editor.Description = "Freeze dryer";
        viewModel.Editor.Manufacturer = "Contoso";
        viewModel.Editor.Model = "LX-10";
        viewModel.Editor.Location = "Suite A";
        viewModel.Editor.Status = "maintenance";
        viewModel.Editor.UrsDoc = "URS-LYO-01";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.Equal(_localization.GetString("Module.Assets.Status.SignatureCaptured", "QA Reason"), viewModel.StatusMessage);
        Assert.False(viewModel.IsDirty);
        Assert.Single(machineAdapter.Saved);
        var persisted = machineAdapter.Saved[0];
        Assert.Equal("Lyophilizer", persisted.Name);
        Assert.Equal("maintenance", persisted.Status);
        Assert.Equal("URS-LYO-01", persisted.UrsDoc);
        Assert.Equal("test-signature", persisted.DigitalSignature);
        Assert.Equal(7, persisted.LastModifiedById);
        Assert.NotEqual(default, persisted.LastModified);
        var context = Assert.Single(machineAdapter.SavedContexts);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("machines", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        var capturedResult = Assert.Single(signatureDialog.CapturedResults);
        var signatureResult = Assert.NotNull(capturedResult);
        Assert.Equal(machineAdapter.Saved[0].Id, signatureResult.Signature.RecordId);
        Assert.Equal(adapterSignatureId, signatureResult.Signature.Id);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.Equal(0, signatureDialog.PersistInvocationCount);
    }

    [Fact]
    public async Task OnSaveAsync_AddMode_SignatureCancelled_StaysInEditModeAndSkipsPersist()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var machineAdapter = new FakeMachineCrudService();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 7, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.25"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        signatureDialog.QueueCancellation();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var codeGenerator = new StubCodeGeneratorService();
        var qrCode = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = new AssetsModuleViewModel(
            database,
            audit,
            machineAdapter,
            auth,
            filePicker,
            attachments,
            signatureDialog,
            dialog,
            shell,
            navigation,
            _localization,
            codeGenerator,
            qrCode,
            platformService);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Code = "AST-500";
        viewModel.Editor.Name = "Lyophilizer";
        viewModel.Editor.Description = "Freeze dryer";
        viewModel.Editor.Manufacturer = "Contoso";
        viewModel.Editor.Model = "LX-10";
        viewModel.Editor.Location = "Suite A";
        viewModel.Editor.Status = "maintenance";
        viewModel.Editor.UrsDoc = "URS-LYO-01";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.False(saved);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.Equal(_localization.GetString("Module.Assets.Status.SignatureCancelled"), viewModel.StatusMessage);
        Assert.Empty(machineAdapter.Saved);
        Assert.Empty(signatureDialog.PersistedResults);
    }

    [Fact]
    public async Task OnSaveAsync_AddMode_SignatureCaptureThrows_SurfacesStatusAndSkipsPersist()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var machineAdapter = new FakeMachineCrudService();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 7, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "10.0.0.25"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        signatureDialog.QueueCaptureException(new InvalidOperationException("Dialog offline"));
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var codeGenerator = new StubCodeGeneratorService();
        var qrCode = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = new AssetsModuleViewModel(
            database,
            audit,
            machineAdapter,
            auth,
            filePicker,
            attachments,
            signatureDialog,
            dialog,
            shell,
            navigation,
            _localization,
            codeGenerator,
            qrCode,
            platformService);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Code = "AST-500";
        viewModel.Editor.Name = "Lyophilizer";
        viewModel.Editor.Description = "Freeze dryer";
        viewModel.Editor.Manufacturer = "Contoso";
        viewModel.Editor.Model = "LX-10";
        viewModel.Editor.Location = "Suite A";
        viewModel.Editor.Status = "maintenance";
        viewModel.Editor.UrsDoc = "URS-LYO-01";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.False(saved);
        Assert.Equal(FormMode.Add, viewModel.Mode);
        Assert.Equal(_localization.GetString("Module.Assets.Status.SignatureFailed", "Dialog offline"), viewModel.StatusMessage);
        Assert.Empty(machineAdapter.Saved);
        Assert.Empty(signatureDialog.PersistedResults);
    }

    [Fact]
    public async Task OpenModule_WithNavigationParameter_UsesParameterForFilter()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var machineAdapter = new FakeMachineCrudService();
        machineAdapter.Saved.AddRange(new[]
        {
            new Machine
            {
                Id = 101,
                Code = "AST-101",
                Name = "Buffer Tank",
                Status = "active",
                Manufacturer = "Fabrikam",
                Location = "Suite 100"
            },
            new Machine
            {
                Id = 202,
                Code = "AST-202",
                Name = "Filling Line",
                Status = "maintenance",
                Manufacturer = "Contoso",
                Location = "Suite 200"
            }
        });

        var target = new Machine
        {
            Id = 303,
            Code = "AST-303",
            Name = "Bioreactor",
            Status = "active",
            Manufacturer = "Tailspin",
            Location = "Suite 300"
        };
        machineAdapter.Saved.Add(target);

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new RecordingModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var codeGenerator = new StubCodeGeneratorService();
        var qrCode = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = new AssetsModuleViewModel(
            database,
            audit,
            machineAdapter,
            auth,
            filePicker,
            attachments,
            signatureDialog,
            dialog,
            shell,
            navigation,
            _localization,
            codeGenerator,
            qrCode,
            platformService);
        navigation.Resolver = (moduleKey, parameter) =>
        {
            Assert.Equal(AssetsModuleViewModel.ModuleKey, moduleKey);
            return viewModel;
        };

        var parameter = target.Code ?? throw new InvalidOperationException("Target code required for navigation test.");
        var opened = (AssetsModuleViewModel)navigation.OpenModule(AssetsModuleViewModel.ModuleKey, parameter);
        var initializeTask = navigation.LastInitializationTask ?? throw new InvalidOperationException("InitializeAsync was not invoked.");
        await initializeTask.ConfigureAwait(false);

        Assert.Same(viewModel, opened);
        Assert.Equal(target.Id.ToString(), opened.SelectedRecord?.Key);
        Assert.Equal(parameter, opened.SearchText);
        Assert.Equal(FormMode.View, opened.Mode);
        Assert.Equal(_localization.GetString("Module.Status.Filtered", opened.Title, parameter), opened.StatusMessage);
    }

    [Fact]
    public async Task OpenModule_WithNavigationIdParameter_SelectsMatchingRecord()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var machineAdapter = new FakeMachineCrudService();
        machineAdapter.Saved.AddRange(new[]
        {
            new Machine
            {
                Id = 11,
                Code = "AST-011",
                Name = "Autoclave",
                Status = "active",
                Manufacturer = "Contoso",
                Location = "Suite 100"
            },
            new Machine
            {
                Id = 22,
                Code = "AST-022",
                Name = "Filling Line",
                Status = "maintenance",
                Manufacturer = "Fabrikam",
                Location = "Suite 200"
            }
        });

        var target = new Machine
        {
            Id = 33,
            Code = "AST-033",
            Name = "Bioreactor",
            Status = "active",
            Manufacturer = "Tailspin",
            Location = "Suite 300"
        };
        machineAdapter.Saved.Add(target);

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new RecordingModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var codeGenerator = new StubCodeGeneratorService();
        var qrCode = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = new AssetsModuleViewModel(
            database,
            audit,
            machineAdapter,
            auth,
            filePicker,
            attachments,
            signatureDialog,
            dialog,
            shell,
            navigation,
            _localization,
            codeGenerator,
            qrCode,
            platformService);
        navigation.Resolver = (moduleKey, parameter) =>
        {
            Assert.Equal(AssetsModuleViewModel.ModuleKey, moduleKey);
            return viewModel;
        };

        var opened = (AssetsModuleViewModel)navigation.OpenModule(AssetsModuleViewModel.ModuleKey, target.Id);
        var initializeTask = navigation.LastInitializationTask ?? throw new InvalidOperationException("InitializeAsync was not invoked.");
        await initializeTask.ConfigureAwait(false);

        Assert.Same(viewModel, opened);
        Assert.Equal(target.Id.ToString(), opened.SelectedRecord?.Key);
        Assert.Equal(target.Id.ToString(CultureInfo.InvariantCulture), opened.SearchText);
        Assert.Equal(FormMode.View, opened.Mode);
        Assert.Equal(_localization.GetString("Module.Status.Filtered", opened.Title, target.Id.ToString(CultureInfo.InvariantCulture)), opened.StatusMessage);
    }

    [Fact]
    public async Task OpenModule_WithNavigationDictionaryMachineId_SelectsMatchingRecord()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var machineAdapter = new FakeMachineCrudService();
        machineAdapter.Saved.AddRange(new[]
        {
            new Machine
            {
                Id = 77,
                Code = "AST-077",
                Name = "Tablet Press",
                Status = "active",
                Manufacturer = "Contoso",
                Location = "Suite 700"
            },
            new Machine
            {
                Id = 88,
                Code = "AST-088",
                Name = "Granulator",
                Status = "maintenance",
                Manufacturer = "Fabrikam",
                Location = "Suite 720"
            }
        });

        var target = new Machine
        {
            Id = 99,
            Code = "AST-099",
            Name = "Film Coater",
            Status = "active",
            Manufacturer = "Tailspin",
            Location = "Suite 740"
        };
        machineAdapter.Saved.Add(target);

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new RecordingModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var codeGenerator = new StubCodeGeneratorService();
        var qrCode = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = new AssetsModuleViewModel(
            database,
            audit,
            machineAdapter,
            auth,
            filePicker,
            attachments,
            signatureDialog,
            dialog,
            shell,
            navigation,
            _localization,
            codeGenerator,
            qrCode,
            platformService);
        navigation.Resolver = (moduleKey, parameter) =>
        {
            Assert.Equal(AssetsModuleViewModel.ModuleKey, moduleKey);
            return viewModel;
        };

        var parameter = new Dictionary<string, object?>
        {
            ["assetId"] = target.Id
        };

        var opened = (AssetsModuleViewModel)navigation.OpenModule(AssetsModuleViewModel.ModuleKey, parameter);
        var initializeTask = navigation.LastInitializationTask ?? throw new InvalidOperationException("InitializeAsync was not invoked.");
        await initializeTask.ConfigureAwait(false);

        Assert.Same(viewModel, opened);
        Assert.Equal(target.Id.ToString(CultureInfo.InvariantCulture), opened.SelectedRecord?.Key);
        Assert.Equal(target.Id.ToString(CultureInfo.InvariantCulture), opened.SearchText);
        Assert.Equal(FormMode.View, opened.Mode);
        Assert.Equal(_localization.GetString("Module.Status.Filtered", opened.Title, target.Id.ToString(CultureInfo.InvariantCulture)), opened.StatusMessage);
        Assert.True(opened.EnterUpdateModeCommand.CanExecute(null));
    }

    [Fact]
    public async Task InitializeAsync_WithMachineId_ReappliesSelectionAndSearch()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var machineAdapter = new FakeMachineCrudService();
        machineAdapter.Saved.AddRange(new[]
        {
            new Machine
            {
                Id = 41,
                Code = "AST-041",
                Name = "Chromatography Skid",
                Status = "active",
                Manufacturer = "Contoso",
                Location = "Suite 400"
            },
            new Machine
            {
                Id = 42,
                Code = "AST-042",
                Name = "Filling Line",
                Status = "maintenance",
                Manufacturer = "Fabrikam",
                Location = "Suite 420"
            }
        });

        var target = new Machine
        {
            Id = 43,
            Code = "AST-043",
            Name = "Lyophilizer",
            Status = "active",
            Manufacturer = "Tailspin",
            Location = "Suite 430"
        };
        machineAdapter.Saved.Add(target);

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var codeGenerator = new StubCodeGeneratorService();
        var qrCode = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = new AssetsModuleViewModel(
            database,
            audit,
            machineAdapter,
            auth,
            filePicker,
            attachments,
            signatureDialog,
            dialog,
            shell,
            navigation,
            _localization,
            codeGenerator,
            qrCode,
            platformService);

        await viewModel.InitializeAsync(null);
        await Task.Yield();

        Assert.NotNull(viewModel.SelectedRecord);
        Assert.NotEqual(target.Id.ToString(CultureInfo.InvariantCulture), viewModel.SelectedRecord?.Key);

        await viewModel.InitializeAsync(target.Id);
        await Task.Yield();

        Assert.Equal(target.Id.ToString(CultureInfo.InvariantCulture), viewModel.SelectedRecord?.Key);
        Assert.Equal(target.Id.ToString(CultureInfo.InvariantCulture), viewModel.SearchText);
        Assert.Equal(_localization.GetString("Module.Status.Filtered", viewModel.Title, target.Id.ToString(CultureInfo.InvariantCulture)), viewModel.StatusMessage);
    }

    [Fact]
    public async Task AttachDocumentCommand_UploadsAttachmentViaService()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var machineAdapter = new FakeMachineCrudService();
        machineAdapter.Saved.Add(new Machine
        {
            Id = 5,
            Code = "M-100",
            Name = "Mixer",
            Manufacturer = "Globex",
            Location = "Suite 2",
            UrsDoc = "URS-MIX-01"
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 12, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.42"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var bytes = Encoding.UTF8.GetBytes("hello asset");
        filePicker.Files = new[]
        {
            new PickedFile("hello.txt", "text/plain", () => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)), bytes.Length)
        };

        var codeGenerator = new StubCodeGeneratorService();
        var qrCode = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = new AssetsModuleViewModel(
            database,
            audit,
            machineAdapter,
            auth,
            filePicker,
            attachments,
            signatureDialog,
            dialog,
            shell,
            navigation,
            _localization,
            codeGenerator,
            qrCode,
            platformService);
        await viewModel.InitializeAsync(null);

        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));
        await viewModel.AttachDocumentCommand.ExecuteAsync(null);

        Assert.Single(attachments.Uploads);
        var upload = attachments.Uploads[0];
        Assert.Equal("machines", upload.EntityType);
        Assert.Equal(5, upload.EntityId);
        Assert.Equal("hello.txt", upload.FileName);
    }

    [Fact]
    public async Task OnActivatedAsync_FromEditMode_ReselectsRecordAndRefreshesAttachments()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var machineAdapter = new FakeMachineCrudService();
        machineAdapter.Saved.Add(new Machine
        {
            Id = 5,
            Code = "M-100",
            Name = "Mixer",
            Manufacturer = "Globex",
            Location = "Suite 2",
            UrsDoc = "URS-MIX-01"
        });

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var filePicker = new TestFilePicker();
        var attachments = new TestAttachmentService();

        var codeGenerator = new StubCodeGeneratorService();
        var qrCode = new StubQrCodeService();
        var platformService = new StubPlatformService();

        var viewModel = new AssetsModuleViewModel(
            database,
            audit,
            machineAdapter,
            auth,
            filePicker,
            attachments,
            signatureDialog,
            dialog,
            shell,
            navigation,
            _localization,
            codeGenerator,
            qrCode,
            platformService);

        await viewModel.InitializeAsync(5);
        await Task.Yield();

        Assert.Equal("Mixer", viewModel.Editor.Name);
        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));

        var canExecuteRaised = 0;
        viewModel.AttachDocumentCommand.CanExecuteChanged += (_, _) => canExecuteRaised++;

        viewModel.Mode = FormMode.Update;
        Assert.True(viewModel.IsEditorEnabled);
        Assert.False(viewModel.AttachDocumentCommand.CanExecute(null));

        canExecuteRaised = 0;

        machineAdapter.Saved[0].Name = "Mixer Reloaded";
        machineAdapter.Saved[0].Location = "Suite 9";

        await viewModel.InitializeAsync(5);
        await Task.Yield();

        Assert.Equal(FormMode.View, viewModel.Mode);
        Assert.False(viewModel.IsEditorEnabled);
        Assert.Equal("Mixer Reloaded", viewModel.Editor.Name);
        Assert.Equal("Suite 9", viewModel.Editor.Location);
        Assert.True(viewModel.AttachDocumentCommand.CanExecute(null));
        Assert.True(canExecuteRaised > 0);
    }

    public void Dispose() => _localization.SetLanguage(_originalLanguage);

    private static Task<bool> InvokeSaveAsync(AssetsModuleViewModel viewModel)
    {
        var method = typeof(AssetsModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(AssetsModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }

    private sealed class RecordingModuleNavigationService : IModuleNavigationService
    {
        public Func<string, object?, ModuleDocumentViewModel>? Resolver { get; set; }

        public ModuleDocumentViewModel? LastOpened { get; private set; }

        public object? LastParameter { get; private set; }

        public Task? LastInitializationTask { get; private set; }

        public ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null)
        {
            if (Resolver is null)
            {
                throw new InvalidOperationException("No resolver configured for module navigation.");
            }

            var module = Resolver(moduleKey, parameter);
            LastOpened = module;
            LastParameter = parameter;
            LastInitializationTask = module.InitializeAsync(parameter);
            return module;
        }

        public void Activate(ModuleDocumentViewModel document)
        {
        }
    }
}
