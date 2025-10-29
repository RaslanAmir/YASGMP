using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Dialogs;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public class SecurityModuleViewModelTests
{
    [Fact]
    public async Task CreateUserCommand_SavedResultRefreshesSelection()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var userService = new FakeUserCrudService();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 42, Username = "admin" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.1"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var localization = new FakeLocalizationService(
            new Dictionary<string, IDictionary<string, string>>
            {
                ["neutral"] = new Dictionary<string, string>
                {
                    ["Module.Title.Security"] = "Security",
                    ["Dialog.UserEdit.Status.Saved"] = "User saved successfully.",
                    ["Dialog.UserEdit.Status.ImpersonationRequested"] = "Impersonation requested.",
                    ["Dialog.UserEdit.Status.ImpersonationRequestedWithTarget"] = "Impersonation requested for #{0}.",
                    ["Dialog.UserEdit.Status.ImpersonationEnded"] = "Impersonation session ended.",
                    ["Dialog.UserEdit.Status.ImpersonationFailed"] = "Impersonation failed: {0}",
                    ["Dialog.UserEdit.Status.EndImpersonationFailed"] = "Unable to end impersonation: {0}",
                    ["Dialog.UserEdit.Validation.ImpersonationTargetRequired"] = "Impersonation target required.",
                    ["Dialog.UserEdit.Validation.ImpersonationReasonRequired"] = "Impersonation reason is required."
                }
            },
            "neutral");

        var impersonationWorkflow = new SecurityImpersonationWorkflowService(userService);
        var dialogService = new RecordingDialogService();
        dialogService.OnShowUserEdit = request =>
        {
            var createdUser = new User
            {
                Id = 5,
                Username = "new.user",
                FullName = "New User",
                Email = "new.user@example.com",
                Role = "Administrator",
                Active = true
            };
            userService.SeedUser(createdUser);

            return Task.FromResult<UserEditDialogViewModel.UserEditDialogResult?>(
                new UserEditDialogViewModel.UserEditDialogResult(
                    true,
                    false,
                    false,
                    UserEditDialogViewModel.UserEditor.FromUser(createdUser),
                    null,
                    null,
                    null,
                    null,
                    null,
                    null));
        };

        Func<UserEditDialogViewModel> userDialogFactory = () =>
            new UserEditDialogViewModel(userService, impersonationWorkflow, signatureDialog, auth, shell, localization);

        var viewModel = new SecurityModuleViewModel(
            database,
            audit,
            userService,
            dialog,
            shell,
            navigation,
            localization,
            dialogService,
            userDialogFactory);

        await viewModel.InitializeAsync(null);

        await viewModel.CreateUserCommand.ExecuteAsync(null);

        var request = Assert.Single(dialogService.UserEditRequests);
        Assert.Equal(UserEditDialogMode.Add, request.Mode);
        Assert.Null(request.User);
        Assert.Equal(FormMode.View, viewModel.Mode);
        Assert.Equal("User saved successfully.", viewModel.StatusMessage);
        Assert.Contains(viewModel.Records, record => record.Key == "5");
        Assert.Equal("5", viewModel.SelectedRecord?.Key);
        Assert.Equal("new.user", viewModel.Dialog.Editor.Username);
    }

    [Fact]
    public async Task EditUserCommand_SavedResultKeepsSelectionAndUpdatesInspector()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var userService = new FakeUserCrudService();
        userService.SeedUser(new User
        {
            Id = 7,
            Username = "existing",
            FullName = "Existing User",
            Email = "existing@example.com",
            Role = "Administrator",
            Active = true
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 42, Username = "admin" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.1"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var localization = new FakeLocalizationService(
            new Dictionary<string, IDictionary<string, string>>
            {
                ["neutral"] = new Dictionary<string, string>
                {
                    ["Module.Title.Security"] = "Security",
                    ["Dialog.UserEdit.Status.Saved"] = "User saved successfully.",
                    ["Dialog.UserEdit.Status.ImpersonationRequested"] = "Impersonation requested.",
                    ["Dialog.UserEdit.Status.ImpersonationRequestedWithTarget"] = "Impersonation requested for #{0}.",
                    ["Dialog.UserEdit.Status.ImpersonationEnded"] = "Impersonation session ended.",
                    ["Dialog.UserEdit.Status.ImpersonationFailed"] = "Impersonation failed: {0}",
                    ["Dialog.UserEdit.Status.EndImpersonationFailed"] = "Unable to end impersonation: {0}",
                    ["Dialog.UserEdit.Validation.ImpersonationTargetRequired"] = "Impersonation target required.",
                    ["Dialog.UserEdit.Validation.ImpersonationReasonRequired"] = "Impersonation reason is required."
                }
            },
            "neutral");

        var impersonationWorkflow = new SecurityImpersonationWorkflowService(userService);
        var dialogService = new RecordingDialogService();
        dialogService.OnShowUserEdit = request =>
        {
            Assert.Equal(UserEditDialogMode.Update, request.Mode);
            Assert.Equal(7, request.User?.Id);

            var updatedUser = new User
            {
                Id = 7,
                Username = "existing",
                FullName = "Existing User",
                Email = "updated@example.com",
                Role = "Administrator",
                Active = true
            };
            userService.SeedUser(updatedUser);

            return Task.FromResult<UserEditDialogViewModel.UserEditDialogResult?>(
                new UserEditDialogViewModel.UserEditDialogResult(
                    true,
                    false,
                    false,
                    UserEditDialogViewModel.UserEditor.FromUser(updatedUser),
                    null,
                    null,
                    null,
                    null,
                    null,
                    null));
        };

        Func<UserEditDialogViewModel> userDialogFactory = () =>
            new UserEditDialogViewModel(userService, impersonationWorkflow, signatureDialog, auth, shell, localization);

        var viewModel = new SecurityModuleViewModel(
            database,
            audit,
            userService,
            dialog,
            shell,
            navigation,
            localization,
            dialogService,
            userDialogFactory);

        await viewModel.InitializeAsync(null);
        viewModel.SelectedRecord = viewModel.Records.First();

        await viewModel.EditUserCommand.ExecuteAsync(null);

        var request = Assert.Single(dialogService.UserEditRequests);
        Assert.Equal(UserEditDialogMode.Update, request.Mode);
        Assert.Equal(7, request.User?.Id);
        Assert.Equal(FormMode.View, viewModel.Mode);
        Assert.Equal("User saved successfully.", viewModel.StatusMessage);
        Assert.Equal("7", viewModel.SelectedRecord?.Key);
        Assert.Equal("updated@example.com", viewModel.Dialog.Editor.Email);
    }

    [Fact]
    public async Task BeginImpersonationCommand_WithValidSelectionUpdatesStatusAndContext()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var userService = new FakeUserCrudService();
        userService.SeedUser(new User
        {
            Id = 10,
            Username = "primary",
            FullName = "Primary Admin",
            Email = "primary@example.com",
            Role = "Administrator",
            Active = true
        });
        userService.SeedUser(new User
        {
            Id = 11,
            Username = "delegate",
            FullName = "Delegate User",
            Email = "delegate@example.com",
            Role = "Quality",
            Active = true
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 99, Username = "admin" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.1"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var localization = new FakeLocalizationService(
            new Dictionary<string, IDictionary<string, string>>
            {
                ["neutral"] = new Dictionary<string, string>
                {
                    ["Module.Title.Security"] = "Security",
                    ["Dialog.UserEdit.Status.Saved"] = "User saved successfully.",
                    ["Dialog.UserEdit.Status.ImpersonationRequested"] = "Impersonation requested.",
                    ["Dialog.UserEdit.Status.ImpersonationRequestedWithTarget"] = "Impersonation requested for #{0}.",
                    ["Dialog.UserEdit.Status.ImpersonationEnded"] = "Impersonation session ended.",
                    ["Dialog.UserEdit.Status.ImpersonationFailed"] = "Impersonation failed: {0}",
                    ["Dialog.UserEdit.Status.EndImpersonationFailed"] = "Unable to end impersonation: {0}",
                    ["Dialog.UserEdit.Validation.ImpersonationTargetRequired"] = "Impersonation target required.",
                    ["Dialog.UserEdit.Validation.ImpersonationReasonRequired"] = "Impersonation reason is required."
                }
            },
            "neutral");

        var impersonationWorkflow = new SecurityImpersonationWorkflowService(userService);
        var dialogService = new RecordingDialogService();

        Func<UserEditDialogViewModel> userDialogFactory = () =>
        {
            var vm = new UserEditDialogViewModel(userService, impersonationWorkflow, signatureDialog, auth, shell, localization);
            return vm;
        };

        var viewModel = new SecurityModuleViewModel(
            database,
            audit,
            userService,
            dialog,
            shell,
            navigation,
            localization,
            dialogService,
            userDialogFactory);

        await viewModel.InitializeAsync(null).ConfigureAwait(false);
        await viewModel.Dialog.InitializeAsync(null).ConfigureAwait(false);

        var target = viewModel.Dialog.ImpersonationTargets.First(t => t.Id == 11);
        viewModel.Dialog.SelectedImpersonationTarget = target;
        viewModel.Dialog.ImpersonationReason = "Audit review";
        viewModel.Dialog.ImpersonationNotes = "Verify permissions";

        viewModel.BeginImpersonationCommand.Execute(null);
        await WaitForAsync(() => viewModel.StatusMessage is not null).ConfigureAwait(false);

        Assert.Equal("Impersonation requested for #11.", viewModel.StatusMessage);
        Assert.Contains("Impersonation requested for #11.", shell.StatusUpdates);
        Assert.True(viewModel.Dialog.IsImpersonating);
        Assert.NotNull(viewModel.Dialog.ActiveImpersonationContext);
        Assert.Equal("Audit review", viewModel.Dialog.ActiveImpersonationContext?.Reason);
        Assert.Equal("Audit review", userService.LastBeginImpersonationRequestContext?.Reason);
        Assert.Equal(11, userService.LastBeginImpersonationContext?.TargetUserId);
    }

    [Fact]
    public async Task BeginImpersonationCommand_WithMissingReasonExposesValidation()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var userService = new FakeUserCrudService();
        userService.SeedUser(new User
        {
            Id = 50,
            Username = "primary",
            FullName = "Primary Admin",
            Role = "Administrator",
            Active = true
        });
        userService.SeedUser(new User
        {
            Id = 51,
            Username = "delegate",
            FullName = "Delegate",
            Role = "Quality",
            Active = true
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 77, Username = "admin" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.1"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var localization = new FakeLocalizationService(
            new Dictionary<string, IDictionary<string, string>>
            {
                ["neutral"] = new Dictionary<string, string>
                {
                    ["Module.Title.Security"] = "Security",
                    ["Dialog.UserEdit.Status.Saved"] = "User saved successfully.",
                    ["Dialog.UserEdit.Status.ImpersonationRequested"] = "Impersonation requested.",
                    ["Dialog.UserEdit.Status.ImpersonationRequestedWithTarget"] = "Impersonation requested for #{0}.",
                    ["Dialog.UserEdit.Status.ImpersonationEnded"] = "Impersonation session ended.",
                    ["Dialog.UserEdit.Status.ImpersonationFailed"] = "Impersonation failed: {0}",
                    ["Dialog.UserEdit.Status.EndImpersonationFailed"] = "Unable to end impersonation: {0}",
                    ["Dialog.UserEdit.Validation.ImpersonationTargetRequired"] = "Impersonation target required.",
                    ["Dialog.UserEdit.Validation.ImpersonationReasonRequired"] = "Impersonation reason is required."
                }
            },
            "neutral");

        var impersonationWorkflow = new SecurityImpersonationWorkflowService(userService);
        var dialogService = new RecordingDialogService();

        Func<UserEditDialogViewModel> userDialogFactory = () =>
            new UserEditDialogViewModel(userService, impersonationWorkflow, signatureDialog, auth, shell, localization);

        var viewModel = new SecurityModuleViewModel(
            database,
            audit,
            userService,
            dialog,
            shell,
            navigation,
            localization,
            dialogService,
            userDialogFactory);

        await viewModel.InitializeAsync(null).ConfigureAwait(false);
        await viewModel.Dialog.InitializeAsync(null).ConfigureAwait(false);

        var target = viewModel.Dialog.ImpersonationTargets.First(t => t.Id == 51);
        viewModel.Dialog.SelectedImpersonationTarget = target;
        viewModel.Dialog.ImpersonationReason = string.Empty;

        viewModel.BeginImpersonationCommand.Execute(null);

        Assert.Contains(
            "Impersonation reason is required.",
            viewModel.Dialog.ValidationMessages);
        Assert.Null(viewModel.StatusMessage);
        Assert.Empty(userService.BeginImpersonationRequests);
    }

    [Fact]
    public async Task BeginImpersonationCommand_WhenCallbackThrowsSurfacesStatusMessage()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var userService = new FakeUserCrudService();
        userService.SeedUser(new User
        {
            Id = 60,
            Username = "primary",
            FullName = "Primary Admin",
            Role = "Administrator",
            Active = true
        });
        userService.SeedUser(new User
        {
            Id = 61,
            Username = "delegate",
            FullName = "Delegate",
            Role = "Quality",
            Active = true
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 82, Username = "admin" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.1"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var localization = new FakeLocalizationService(
            new Dictionary<string, IDictionary<string, string>>
            {
                ["neutral"] = new Dictionary<string, string>
                {
                    ["Module.Title.Security"] = "Security",
                    ["Dialog.UserEdit.Status.Saved"] = "User saved successfully.",
                    ["Dialog.UserEdit.Status.ImpersonationRequested"] = "Impersonation requested.",
                    ["Dialog.UserEdit.Status.ImpersonationRequestedWithTarget"] = "Impersonation requested for #{0}.",
                    ["Dialog.UserEdit.Status.ImpersonationEnded"] = "Impersonation session ended.",
                    ["Dialog.UserEdit.Status.ImpersonationFailed"] = "Impersonation failed: {0}",
                    ["Dialog.UserEdit.Status.EndImpersonationFailed"] = "Unable to end impersonation: {0}",
                    ["Dialog.UserEdit.Validation.ImpersonationTargetRequired"] = "Impersonation target required.",
                    ["Dialog.UserEdit.Validation.ImpersonationReasonRequired"] = "Impersonation reason is required."
                }
            },
            "neutral");

        var impersonationWorkflow = new SecurityImpersonationWorkflowService(userService);
        var dialogService = new RecordingDialogService();

        Func<UserEditDialogViewModel> userDialogFactory = () =>
            new UserEditDialogViewModel(userService, impersonationWorkflow, signatureDialog, auth, shell, localization);

        var viewModel = new SecurityModuleViewModel(
            database,
            audit,
            userService,
            dialog,
            shell,
            navigation,
            localization,
            dialogService,
            userDialogFactory);

        await viewModel.InitializeAsync(null).ConfigureAwait(false);
        await viewModel.Dialog.InitializeAsync(null).ConfigureAwait(false);

        var target = viewModel.Dialog.ImpersonationTargets.First(t => t.Id == 61);
        viewModel.Dialog.SelectedImpersonationTarget = target;
        viewModel.Dialog.ImpersonationReason = "Audit";
        viewModel.Dialog.BeginImpersonationCallback = _ => throw new InvalidOperationException("Denied");

        viewModel.BeginImpersonationCommand.Execute(null);
        await WaitForAsync(() => string.Equals(viewModel.StatusMessage, "Denied", StringComparison.Ordinal)).ConfigureAwait(false);

        Assert.Equal("Denied", viewModel.StatusMessage);
        Assert.Equal("Denied", viewModel.Dialog.StatusMessage);
        Assert.Empty(userService.BeginImpersonationRequests);
    }

    [Fact]
    public async Task EndImpersonationCommand_WithActiveSessionUpdatesStatus()
    {
        var database = new DatabaseService();
        var audit = new AuditService(database);
        var userService = new FakeUserCrudService();
        userService.SeedUser(new User
        {
            Id = 70,
            Username = "primary",
            FullName = "Primary Admin",
            Role = "Administrator",
            Active = true
        });
        userService.SeedUser(new User
        {
            Id = 71,
            Username = "delegate",
            FullName = "Delegate",
            Role = "Quality",
            Active = true
        });

        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 90, Username = "admin" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.1"
        };
        var signatureDialog = new TestElectronicSignatureDialogService();
        var dialog = new TestCflDialogService();
        var shell = new TestShellInteractionService();
        var navigation = new TestModuleNavigationService();
        var localization = new FakeLocalizationService(
            new Dictionary<string, IDictionary<string, string>>
            {
                ["neutral"] = new Dictionary<string, string>
                {
                    ["Module.Title.Security"] = "Security",
                    ["Dialog.UserEdit.Status.Saved"] = "User saved successfully.",
                    ["Dialog.UserEdit.Status.ImpersonationRequested"] = "Impersonation requested.",
                    ["Dialog.UserEdit.Status.ImpersonationRequestedWithTarget"] = "Impersonation requested for #{0}.",
                    ["Dialog.UserEdit.Status.ImpersonationEnded"] = "Impersonation session ended.",
                    ["Dialog.UserEdit.Status.ImpersonationFailed"] = "Impersonation failed: {0}",
                    ["Dialog.UserEdit.Status.EndImpersonationFailed"] = "Unable to end impersonation: {0}",
                    ["Dialog.UserEdit.Validation.ImpersonationTargetRequired"] = "Impersonation target required.",
                    ["Dialog.UserEdit.Validation.ImpersonationReasonRequired"] = "Impersonation reason is required."
                }
            },
            "neutral");

        var impersonationWorkflow = new SecurityImpersonationWorkflowService(userService);
        var dialogService = new RecordingDialogService();

        Func<UserEditDialogViewModel> userDialogFactory = () =>
            new UserEditDialogViewModel(userService, impersonationWorkflow, signatureDialog, auth, shell, localization);

        var viewModel = new SecurityModuleViewModel(
            database,
            audit,
            userService,
            dialog,
            shell,
            navigation,
            localization,
            dialogService,
            userDialogFactory);

        await viewModel.InitializeAsync(null).ConfigureAwait(false);
        await viewModel.Dialog.InitializeAsync(null).ConfigureAwait(false);

        var target = viewModel.Dialog.ImpersonationTargets.First(t => t.Id == 71);
        viewModel.Dialog.SelectedImpersonationTarget = target;
        viewModel.Dialog.ImpersonationReason = "Audit";

        viewModel.BeginImpersonationCommand.Execute(null);
        await WaitForAsync(() => userService.LastBeginImpersonationContext is not null).ConfigureAwait(false);

        viewModel.EndImpersonationCommand.Execute(null);
        await WaitForAsync(() => viewModel.StatusMessage == "Impersonation session ended.").ConfigureAwait(false);

        Assert.Equal("Impersonation session ended.", viewModel.StatusMessage);
        Assert.Contains("Impersonation session ended.", shell.StatusUpdates);
        Assert.False(viewModel.Dialog.IsImpersonating);
        Assert.Null(viewModel.Dialog.ActiveImpersonationContext);
        Assert.NotNull(userService.LastEndImpersonationContext);
        Assert.NotNull(userService.LastEndImpersonationAuditContext);
        Assert.Equal("Audit", userService.LastEndImpersonationAuditContext?.Reason);
    }

    private static async Task WaitForAsync(Func<bool> condition, int timeoutMilliseconds = 1000, int pollMilliseconds = 10)
    {
        var timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
        var delay = TimeSpan.FromMilliseconds(pollMilliseconds);
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > timeout)
            {
                throw new TimeoutException("Condition was not satisfied within the allotted time.");
            }

            await Task.Delay(delay).ConfigureAwait(false);
        }
    }

    private sealed class RecordingDialogService : IDialogService
    {
        public List<UserEditDialogRequest> UserEditRequests { get; } = new();

        public Func<UserEditDialogRequest, Task<UserEditDialogViewModel.UserEditDialogResult?>>? OnShowUserEdit { get; set; }

        public Task ShowAlertAsync(string title, string message, string cancel)
            => Task.CompletedTask;

        public Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
            => Task.FromResult(false);

        public Task<string?> ShowActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
            => Task.FromResult<string?>(null);

        public async Task<T?> ShowDialogAsync<T>(string dialogId, object? parameter = null, CancellationToken cancellationToken = default)
        {
            if (dialogId == DialogIds.UserEdit)
            {
                var request = Assert.IsType<UserEditDialogRequest>(parameter);
                UserEditRequests.Add(request);
                if (OnShowUserEdit is null)
                {
                    return default;
                }

                var result = await OnShowUserEdit(request).ConfigureAwait(false);
                return (T?)(object?)result;
            }

            return default;
        }
    }

}
