using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
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
        var coreUserService = new NullUserService();
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
                    ["Dialog.UserEdit.Status.ImpersonationEnded"] = "Impersonation session ended."
                }
            },
            "neutral");

        var impersonationWorkflow = new SecurityImpersonationWorkflowService(userService, coreUserService, auth);
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
        var coreUserService = new NullUserService();
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
                    ["Dialog.UserEdit.Status.ImpersonationEnded"] = "Impersonation session ended."
                }
            },
            "neutral");

        var impersonationWorkflow = new SecurityImpersonationWorkflowService(userService, coreUserService, auth);
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

    private sealed class NullUserService : IUserService
    {
        public List<(int TargetId, UserCrudContext Context)> BeginCalls { get; } = new();
        public List<(ImpersonationContext Context, UserCrudContext Audit)> EndCalls { get; } = new();

        public Task<User?> AuthenticateAsync(string username, string password) => Task.FromResult<User?>(null);

        public string HashPassword(string password) => password;

        public Task<bool> VerifyTwoFactorCodeAsync(string username, string code) => Task.FromResult(false);

        public Task LockUserAsync(int userId) => Task.CompletedTask;

        public Task<List<User>> GetAllUsersAsync() => Task.FromResult(new List<User>());

        public Task<User?> GetUserByIdAsync(int id) => Task.FromResult<User?>(null);

        public Task<User?> GetUserByUsernameAsync(string username) => Task.FromResult<User?>(null);

        public Task CreateUserAsync(User user, int adminId = 0) => Task.CompletedTask;

        public Task UpdateUserAsync(User user, int adminId = 0) => Task.CompletedTask;

        public Task DeleteUserAsync(int userId, int adminId = 0) => Task.CompletedTask;

        public Task DeactivateUserAsync(int userId) => Task.CompletedTask;

        public bool HasRole(User user, string role) => false;

        public bool IsActive(User user) => user?.Active ?? false;

        public Task ChangePasswordAsync(int userId, string newPassword, int adminId = 0) => Task.CompletedTask;

        public string GenerateDigitalSignature(User user) => string.Empty;

        public bool ValidateDigitalSignature(string signature) => false;

        public Task LogUserEventAsync(int userId, string eventType, string details) => Task.CompletedTask;

        public Task UnlockUserAsync(int userId, int adminId) => Task.CompletedTask;

        public Task SetTwoFactorEnabledAsync(int userId, bool enabled) => Task.CompletedTask;

        public Task UpdateUserProfileAsync(User user, int adminId = 0) => Task.CompletedTask;

        public Task<ImpersonationContext?> BeginImpersonationAsync(int targetUserId, UserCrudContext context)
        {
            BeginCalls.Add((targetUserId, context));
            var impersonation = new ImpersonationContext(
                context.UserId,
                targetUserId,
                SessionLogId: 1,
                StartedAtUtc: DateTime.UtcNow,
                Reason: context.Reason ?? string.Empty,
                Notes: context.Notes,
                Ip: context.Ip,
                DeviceInfo: context.DeviceInfo,
                SessionId: context.SessionId,
                SignatureId: context.SignatureId,
                SignatureHash: context.SignatureHash,
                SignatureMethod: context.SignatureMethod,
                SignatureStatus: context.SignatureStatus,
                SignatureNote: context.SignatureNote);
            return Task.FromResult<ImpersonationContext?>(impersonation);
        }

        public Task EndImpersonationAsync(ImpersonationContext context, UserCrudContext auditContext)
        {
            EndCalls.Add((context, auditContext));
            return Task.CompletedTask;
        }
    }
}
