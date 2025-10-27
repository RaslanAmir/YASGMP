using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
                    ["Module.Title.Security"] = "Security"
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
                    null));
        };

        Func<UserEditDialogViewModel> userDialogFactory = () =>
            new UserEditDialogViewModel(userService, impersonationWorkflow, signatureDialog, auth, shell);

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
        Assert.Equal(FormMode.Add, request.Mode);
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
                    ["Module.Title.Security"] = "Security"
                }
            },
            "neutral");

        var impersonationWorkflow = new SecurityImpersonationWorkflowService(userService);
        var dialogService = new RecordingDialogService();
        dialogService.OnShowUserEdit = request =>
        {
            Assert.Equal(FormMode.Update, request.Mode);
            Assert.Equal(7, request.ViewModel.Editor.Id);

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
                    null));
        };

        Func<UserEditDialogViewModel> userDialogFactory = () =>
            new UserEditDialogViewModel(userService, impersonationWorkflow, signatureDialog, auth, shell);

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
        Assert.Equal(FormMode.Update, request.Mode);
        Assert.Equal(FormMode.View, viewModel.Mode);
        Assert.Equal("User saved successfully.", viewModel.StatusMessage);
        Assert.Equal("7", viewModel.SelectedRecord?.Key);
        Assert.Equal("updated@example.com", viewModel.Dialog.Editor.Email);
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
