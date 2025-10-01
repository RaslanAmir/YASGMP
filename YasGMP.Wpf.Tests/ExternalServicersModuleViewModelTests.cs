using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class ExternalServicersModuleViewModelTests
{
    [Fact]
    public async Task OnSaveAsync_AddMode_PersistsServicer()
    {
        const int adapterSignatureId = 3344;
        var service = new FakeExternalServicerCrudService
        {
            SignatureMetadataIdSource = _ => adapterSignatureId
        };
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 7, FullName = "QA" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.1",
            CurrentSessionId = "session"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ExternalServicersModuleViewModel(service, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.Mode = FormMode.Add;
        viewModel.Editor.Name = "Contoso Labs";
        viewModel.Editor.Email = "labs@contoso.example";
        viewModel.Editor.Type = "Calibration";
        viewModel.Editor.Status = "Active";
        viewModel.Editor.ContactPerson = "Ivana";

        var saved = await InvokeSaveAsync(viewModel);

        Assert.True(saved);
        Assert.Equal("Electronic signature captured (QA Reason).", viewModel.StatusMessage);
        Assert.Single(service.Saved);
        var servicer = service.Saved[0];
        Assert.Equal("Contoso Labs", servicer.Name);
        Assert.Equal("calibration", servicer.Type?.ToLowerInvariant());
        Assert.Equal("labs@contoso.example", servicer.Email);
        Assert.Equal("test-signature", servicer.DigitalSignature);
        var context = Assert.Single(service.SavedContexts);
        Assert.Equal("test-signature", context.SignatureHash);
        Assert.Equal("password", context.SignatureMethod);
        Assert.Equal("valid", context.SignatureStatus);
        Assert.Equal("Automated test", context.SignatureNote);
        Assert.Collection(signatureDialog.Requests, ctx =>
        {
            Assert.Equal("external_contractors", ctx.TableName);
            Assert.Equal(0, ctx.RecordId);
        });
        var capturedResult = Assert.Single(signatureDialog.CapturedResults);
        var signatureResult = Assert.NotNull(capturedResult);
        Assert.Equal(service.Saved[0].Id, signatureResult.Signature.RecordId);
        Assert.Equal(adapterSignatureId, signatureResult.Signature.Id);
        Assert.Empty(signatureDialog.PersistedResults);
        Assert.Equal(0, signatureDialog.PersistInvocationCount);
    }

    [Fact]
    public async Task Cancel_UpdateMode_RestoresSnapshot()
    {
        var service = new FakeExternalServicerCrudService();
        service.Saved.Add(new ExternalServicer
        {
            Id = 5,
            Name = "Globex",
            Email = "service@globex.example",
            Type = "Maintenance",
            Status = "active"
        });

        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ExternalServicersModuleViewModel(service, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        viewModel.SelectedRecord = viewModel.Records.First();
        await viewModel.EnterUpdateModeCommand.ExecuteAsync(null);
        var original = viewModel.Editor.Name;
        viewModel.Editor.Name = "Updated";

        viewModel.CancelCommand.Execute(null);

        Assert.Equal(original, viewModel.Editor.Name);
    }

    [Fact]
    public async Task ModeTransitions_ToggleEditorEnablement()
    {
        var service = new FakeExternalServicerCrudService();
        var auth = new TestAuthContext();
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();

        var viewModel = new ExternalServicersModuleViewModel(service, auth, signatureDialog, dialog, shell, navigation);
        await viewModel.InitializeAsync(null);

        Assert.False(viewModel.IsEditorEnabled);

        await viewModel.EnterAddModeCommand.ExecuteAsync(null);
        Assert.True(viewModel.IsEditorEnabled);

        viewModel.CancelCommand.Execute(null);
        Assert.False(viewModel.IsEditorEnabled);
    }

    private static Task<bool> InvokeSaveAsync(ExternalServicersModuleViewModel viewModel)
    {
        var method = typeof(ExternalServicersModuleViewModel)
            .GetMethod("OnSaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(ExternalServicersModuleViewModel), "OnSaveAsync");
        return (Task<bool>)method.Invoke(viewModel, null)!;
    }

    private sealed class TestCflDialogService : ICflDialogService
    {
        public Task<CflResult?> ShowAsync(CflRequest request) => Task.FromResult<CflResult?>(null);
    }

    private sealed class TestShellInteractionService : IShellInteractionService
    {
        public void UpdateStatus(string message)
        {
        }

        public void UpdateInspector(InspectorContext context)
        {
        }
    }

    private sealed class TestModuleNavigationService : IModuleNavigationService
    {
        public ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null)
            => throw new NotSupportedException();

        public void Activate(ModuleDocumentViewModel document)
        {
        }
    }

    private sealed class TestAuthContext : IAuthContext
    {
        public User? CurrentUser { get; set; }

        public string CurrentSessionId { get; set; } = Guid.NewGuid().ToString("N");

        public string CurrentDeviceInfo { get; set; } = "UnitTest";

        public string CurrentIpAddress { get; set; } = "127.0.0.1";
    }
}
