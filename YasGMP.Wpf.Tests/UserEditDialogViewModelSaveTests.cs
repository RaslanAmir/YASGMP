using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Tests;

public class UserEditDialogViewModelSaveTests
{
    [Fact]
    public async Task SaveCommand_WithValidEditorPersistsRolesAndSignatureMetadata()
    {
        var userService = new FakeUserCrudService();
        userService.SeedRole(new Role { Id = 1, Name = "QA" });
        userService.SeedRole(new Role { Id = 2, Name = "Admin" });
        var workflow = new RecordingImpersonationWorkflow();
        var signature = new ElectronicSignatureDialogResult(
            "password",
            "QA",
            "Approve new user",
            "QA Approval",
            new DigitalSignature
            {
                Id = 15,
                SignatureHash = "hash-123",
                Method = "password",
                Status = "valid",
                SignedAt = DateTime.UtcNow
            });
        var signatureDialog = TestElectronicSignatureDialogService.CreateConfirmed(signature);
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 77, Username = "admin" },
            CurrentIpAddress = "127.0.0.1",
            CurrentDeviceInfo = "UnitTest",
            CurrentSessionId = "session-123"
        };
        var shell = new TestShellInteractionService();
        var localization = CreateLocalization();
        var viewModel = new UserEditDialogViewModel(
            userService,
            workflow,
            signatureDialog,
            auth,
            shell,
            localization);

        var request = new UserEditDialogRequest(
            UserEditDialogMode.Add,
            user: null,
            roles: new[]
            {
                new Role { Id = 1, Name = "QA" },
                new Role { Id = 2, Name = "Admin" }
            });

        await viewModel.InitializeAsync(request).ConfigureAwait(false);
        viewModel.Editor.Username = "new.user";
        viewModel.Editor.FullName = "New User";
        viewModel.Editor.Email = "new.user@example.com";
        viewModel.Editor.Role = "QA";
        viewModel.Editor.NewPassword = "Secret123!";
        viewModel.Editor.ConfirmPassword = "Secret123!";
        foreach (var option in viewModel.RoleOptions)
        {
            option.IsSelected = option.RoleId is 1 or 2;
        }

        await viewModel.SaveCommand.ExecuteAsync(null);

        var assignment = Assert.Single(userService.RoleAssignments);
        Assert.Equal(1, assignment.UserId);
        Assert.Equal(new[] { 1, 2 }, assignment.Roles.OrderBy(id => id));
        var snapshot = Assert.Single(userService.SavedWithContext);
        Assert.NotNull(snapshot.Entity);
        Assert.Equal("hash-123", snapshot.Entity.DigitalSignature);
        Assert.Equal("hash-123", snapshot.Entity.LastChangeSignature);
        Assert.Equal(15, snapshot.Context.SignatureId);
        Assert.Equal("hash-123", snapshot.Context.SignatureHash);
        Assert.Equal("password", snapshot.Context.SignatureMethod);
        Assert.Equal("valid", snapshot.Context.SignatureStatus);
        Assert.Null(viewModel.StatusMessage);
        Assert.Equal("User saved.", shell.StatusUpdates.Last());
    }

    [Fact]
    public async Task SaveCommand_WhenValidationFailsPopulatesMessages()
    {
        var userService = new FakeUserCrudService();
        userService.SeedRole(new Role { Id = 1, Name = "QA" });
        var workflow = new RecordingImpersonationWorkflow();
        var signatureDialog = TestElectronicSignatureDialogService.CreateConfirmed();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 77, Username = "admin" },
            CurrentIpAddress = "127.0.0.1",
            CurrentDeviceInfo = "UnitTest",
            CurrentSessionId = "session-123"
        };
        var shell = new TestShellInteractionService();
        var localization = CreateLocalization();
        var viewModel = new UserEditDialogViewModel(
            userService,
            workflow,
            signatureDialog,
            auth,
            shell,
            localization);

        var request = new UserEditDialogRequest(
            UserEditDialogMode.Add,
            user: null,
            roles: new[] { new Role { Id = 1, Name = "QA" } });

        await viewModel.InitializeAsync(request).ConfigureAwait(false);
        viewModel.Editor.Username = string.Empty;
        viewModel.Editor.FullName = string.Empty;
        viewModel.Editor.Role = string.Empty;
        viewModel.Editor.NewPassword = string.Empty;
        viewModel.Editor.ConfirmPassword = string.Empty;
        foreach (var option in viewModel.RoleOptions)
        {
            option.IsSelected = false;
        }

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains("Username is required.", viewModel.ValidationMessages);
        Assert.Contains("Full name is required.", viewModel.ValidationMessages);
        Assert.Contains("Primary role description is required.", viewModel.ValidationMessages);
        Assert.Contains("Password is required when creating a new user.", viewModel.ValidationMessages);
        Assert.Contains("At least one role must be assigned to the user.", viewModel.ValidationMessages);
        Assert.Equal("Resolve validation errors before saving.", viewModel.StatusMessage);
        Assert.Empty(userService.SavedWithContext);
        Assert.Empty(userService.RoleAssignments);
    }

    [Fact]
    public async Task SaveCommand_WhenSignatureCancelledSurfacesStatusMessage()
    {
        var userService = new FakeUserCrudService();
        userService.SeedRole(new Role { Id = 1, Name = "QA" });
        var workflow = new RecordingImpersonationWorkflow();
        var signatureDialog = TestElectronicSignatureDialogService.CreateCancelled();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 77, Username = "admin" },
            CurrentIpAddress = "127.0.0.1",
            CurrentDeviceInfo = "UnitTest",
            CurrentSessionId = "session-123"
        };
        var shell = new TestShellInteractionService();
        var localization = CreateLocalization();
        var viewModel = new UserEditDialogViewModel(
            userService,
            workflow,
            signatureDialog,
            auth,
            shell,
            localization);

        var request = new UserEditDialogRequest(
            UserEditDialogMode.Add,
            user: null,
            roles: new[] { new Role { Id = 1, Name = "QA" } });

        await viewModel.InitializeAsync(request).ConfigureAwait(false);
        viewModel.Editor.Username = "new.user";
        viewModel.Editor.FullName = "New User";
        viewModel.Editor.Role = "QA";
        viewModel.Editor.NewPassword = "Secret123!";
        viewModel.Editor.ConfirmPassword = "Secret123!";
        foreach (var option in viewModel.RoleOptions)
        {
            option.IsSelected = option.RoleId == 1;
        }

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Empty(userService.SavedWithContext);
        Assert.Empty(userService.RoleAssignments);
        Assert.Equal("Save failed: Electronic signature cancelled. Save aborted.", viewModel.StatusMessage);
        Assert.Empty(viewModel.ValidationMessages);
    }

    private static FakeLocalizationService CreateLocalization()
        => new(
            new Dictionary<string, IDictionary<string, string>>
            {
                ["neutral"] = new Dictionary<string, string>
                {
                    ["Dialog.UserEdit.Status.Saved"] = "User saved.",
                    ["Dialog.UserEdit.Status.SaveFailed"] = "Save failed: {0}",
                    ["Dialog.UserEdit.Status.ResolveValidationBeforeSaving"] = "Resolve validation errors before saving."
                }
            },
            "neutral");

    private sealed class RecordingImpersonationWorkflow : ISecurityImpersonationWorkflowService
    {
        public bool IsImpersonating { get; private set; }

        public int? ImpersonatedUserId { get; private set; }

        public ImpersonationContext? ActiveContext { get; private set; }

        public Task<IReadOnlyList<User>> GetImpersonationCandidatesAsync()
            => Task.FromResult<IReadOnlyList<User>>(Array.Empty<User>());

        public Task<ImpersonationContext> BeginImpersonationAsync(int userId, UserCrudContext context)
        {
            IsImpersonating = true;
            ImpersonatedUserId = userId;
            ActiveContext = new ImpersonationContext(
                context.UserId,
                userId,
                SessionLogId: 1,
                StartedAtUtc: DateTime.UtcNow,
                Reason: context.Reason,
                Notes: context.Notes,
                Ip: context.Ip,
                DeviceInfo: context.DeviceInfo,
                SessionId: context.SessionId,
                SignatureId: context.SignatureId,
                SignatureHash: context.SignatureHash,
                SignatureMethod: context.SignatureMethod,
                SignatureStatus: context.SignatureStatus,
                SignatureNote: context.SignatureNote);
            return Task.FromResult(ActiveContext!);
        }

        public Task EndImpersonationAsync(UserCrudContext auditContext)
        {
            IsImpersonating = false;
            ImpersonatedUserId = null;
            ActiveContext = null;
            return Task.CompletedTask;
        }
    }
}
