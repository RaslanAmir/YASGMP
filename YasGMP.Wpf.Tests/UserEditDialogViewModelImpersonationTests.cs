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

public class UserEditDialogViewModelImpersonationTests
{
    [Fact]
    public async Task BeginImpersonationAsync_WithValidTargetUpdatesState()
    {
        var userService = new FakeUserCrudService();
        var workflow = new RecordingImpersonationWorkflow
        {
            Candidates =
            {
                new User { Id = 5, Username = "delegate", FullName = "Delegate User", Role = "QA", Active = true }
            }
        };
        var shell = new TestShellInteractionService();
        var localization = CreateLocalization();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 99, Username = "admin" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.1"
        };
        var viewModel = new UserEditDialogViewModel(
            userService,
            workflow,
            new TestElectronicSignatureDialogService(),
            auth,
            shell,
            localization);

        workflow.BeginResult = new ImpersonationContext(
            auth.CurrentUser!.Id,
            5,
            SessionLogId: 42,
            StartedAtUtc: DateTime.UtcNow,
            Reason: "Audit reason",
            Notes: "Notes",
            Ip: auth.CurrentIpAddress,
            DeviceInfo: auth.CurrentDeviceInfo,
            SessionId: auth.CurrentSessionId,
            SignatureId: null,
            SignatureHash: null,
            SignatureMethod: "password",
            SignatureStatus: "valid",
            SignatureNote: null);

        await viewModel.InitializeAsync(null).ConfigureAwait(false);
        viewModel.SelectedImpersonationTarget = viewModel.ImpersonationTargets.First();
        viewModel.ImpersonationReason = "Audit reason";
        viewModel.ImpersonationNotes = "Notes";

        await viewModel.BeginImpersonationCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsImpersonating);
        Assert.NotNull(viewModel.ActiveImpersonationContext);
        Assert.Equal(5, viewModel.ActiveImpersonationContext?.TargetUserId);
        Assert.True(viewModel.Result?.ImpersonationRequested);
        Assert.Contains("Impersonation requested for #5.", shell.StatusUpdates);
        var beginContext = Assert.Single(workflow.BeginContexts);
        Assert.Equal("Audit reason", beginContext.Reason);
        Assert.Equal("Notes", beginContext.Notes);
    }

    [Fact]
    public async Task BeginImpersonationAsync_WhenReasonMissingAddsValidationMessage()
    {
        var userService = new FakeUserCrudService();
        var workflow = new RecordingImpersonationWorkflow
        {
            Candidates =
            {
                new User { Id = 6, Username = "delegate", FullName = "Delegate" }
            }
        };
        var (viewModel, _) = CreateViewModel(userService, workflow);

        await viewModel.InitializeAsync(null).ConfigureAwait(false);
        viewModel.SelectedImpersonationTarget = viewModel.ImpersonationTargets.First();
        viewModel.ImpersonationReason = string.Empty;

        await viewModel.BeginImpersonationCommand.ExecuteAsync(null);

        Assert.Contains("Impersonation reason is required.", viewModel.ValidationMessages);
        Assert.False(viewModel.IsImpersonating);
        Assert.Empty(workflow.BeginContexts);
    }

    [Fact]
    public async Task BeginImpersonationAsync_WhenWorkflowThrowsSetsStatusMessage()
    {
        var userService = new FakeUserCrudService();
        var workflow = new RecordingImpersonationWorkflow
        {
            Candidates =
            {
                new User { Id = 7, Username = "delegate", FullName = "Delegate" }
            },
            BeginException = new InvalidOperationException("Denied")
        };
        var (viewModel, _) = CreateViewModel(userService, workflow);

        await viewModel.InitializeAsync(null).ConfigureAwait(false);
        viewModel.SelectedImpersonationTarget = viewModel.ImpersonationTargets.First();
        viewModel.ImpersonationReason = "Audit";

        await viewModel.BeginImpersonationCommand.ExecuteAsync(null);

        Assert.Equal("Denied", viewModel.StatusMessage);
        Assert.Empty(workflow.BeginContexts);
        Assert.False(viewModel.IsImpersonating);
    }

    [Fact]
    public async Task EndImpersonationAsync_WithActiveContextClearsState()
    {
        var userService = new FakeUserCrudService();
        var workflow = new RecordingImpersonationWorkflow
        {
            Candidates =
            {
                new User { Id = 8, Username = "delegate", FullName = "Delegate" }
            }
        };
        var (viewModel, shell) = CreateViewModel(userService, workflow);
        workflow.BeginResult = new ImpersonationContext(
            42,
            8,
            SessionLogId: 10,
            StartedAtUtc: DateTime.UtcNow,
            Reason: "Audit",
            Notes: "Notes",
            Ip: "127.0.0.1",
            DeviceInfo: "UnitTest",
            SessionId: "sess",
            SignatureId: null,
            SignatureHash: null,
            SignatureMethod: "password",
            SignatureStatus: "valid",
            SignatureNote: null);

        await viewModel.InitializeAsync(null).ConfigureAwait(false);
        viewModel.SelectedImpersonationTarget = viewModel.ImpersonationTargets.First();
        viewModel.ImpersonationReason = "Audit";
        await viewModel.BeginImpersonationCommand.ExecuteAsync(null);

        await viewModel.EndImpersonationCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsImpersonating);
        Assert.Null(viewModel.ActiveImpersonationContext);
        Assert.True(viewModel.Result?.ImpersonationEnded);
        Assert.Contains("Impersonation session ended.", shell.StatusUpdates);
        var auditContext = Assert.Single(workflow.EndContexts);
        Assert.Equal("Audit", auditContext.Reason);
    }

    [Fact]
    public async Task EndImpersonationAsync_WhenWorkflowThrowsSetsStatusMessage()
    {
        var userService = new FakeUserCrudService();
        var workflow = new RecordingImpersonationWorkflow
        {
            Candidates =
            {
                new User { Id = 9, Username = "delegate", FullName = "Delegate" }
            },
            EndException = new InvalidOperationException("End failed")
        };
        var (viewModel, _) = CreateViewModel(userService, workflow);
        workflow.BeginResult = new ImpersonationContext(
            55,
            9,
            SessionLogId: 11,
            StartedAtUtc: DateTime.UtcNow,
            Reason: "Audit",
            Notes: "Notes",
            Ip: "127.0.0.1",
            DeviceInfo: "UnitTest",
            SessionId: "sess",
            SignatureId: null,
            SignatureHash: null,
            SignatureMethod: "password",
            SignatureStatus: "valid",
            SignatureNote: null);

        await viewModel.InitializeAsync(null).ConfigureAwait(false);
        viewModel.SelectedImpersonationTarget = viewModel.ImpersonationTargets.First();
        viewModel.ImpersonationReason = "Audit";
        await viewModel.BeginImpersonationCommand.ExecuteAsync(null);

        await viewModel.EndImpersonationCommand.ExecuteAsync(null);

        Assert.Equal("End failed", viewModel.StatusMessage);
        Assert.True(viewModel.IsImpersonating);
        Assert.Single(workflow.EndContexts);
    }

    private static (UserEditDialogViewModel ViewModel, TestShellInteractionService Shell) CreateViewModel(
        FakeUserCrudService userService,
        RecordingImpersonationWorkflow workflow)
    {
        var shell = new TestShellInteractionService();
        var localization = CreateLocalization();
        var auth = new TestAuthContext
        {
            CurrentUser = new User { Id = 101, Username = "admin" },
            CurrentDeviceInfo = "UnitTest",
            CurrentIpAddress = "127.0.0.1"
        };

        var viewModel = new UserEditDialogViewModel(
            userService,
            workflow,
            new TestElectronicSignatureDialogService(),
            auth,
            shell,
            localization);
        return (viewModel, shell);
    }

    private static FakeLocalizationService CreateLocalization()
        => new(
            new Dictionary<string, IDictionary<string, string>>
            {
                ["neutral"] = new Dictionary<string, string>
                {
                    ["Dialog.UserEdit.Status.ImpersonationRequestedWithTarget"] = "Impersonation requested for #{0}.",
                    ["Dialog.UserEdit.Status.ImpersonationEnded"] = "Impersonation session ended.",
                    ["Dialog.UserEdit.Status.ImpersonationFailed"] = "Impersonation failed: {0}",
                    ["Dialog.UserEdit.Status.EndImpersonationFailed"] = "Unable to end impersonation: {0}",
                    ["Dialog.UserEdit.Validation.ImpersonationTargetRequired"] = "Impersonation target required.",
                    ["Dialog.UserEdit.Validation.ImpersonationReasonRequired"] = "Impersonation reason is required."
                }
            },
            "neutral");

    private sealed class RecordingImpersonationWorkflow : ISecurityImpersonationWorkflowService
    {
        public List<User> Candidates { get; } = new();

        public List<int> BeginTargets { get; } = new();

        public List<UserCrudContext> BeginContexts { get; } = new();

        public List<UserCrudContext> EndContexts { get; } = new();

        public ImpersonationContext BeginResult { get; set; } = new ImpersonationContext(0, 0, 0, DateTime.UtcNow, string.Empty, null, null, null, null, null, null, null, null, null);

        public Exception? BeginException { get; set; }

        public Exception? EndException { get; set; }

        public bool IsImpersonating { get; private set; }

        public int? ImpersonatedUserId { get; private set; }

        public ImpersonationContext? ActiveContext { get; private set; }

        public Task<IReadOnlyList<User>> GetImpersonationCandidatesAsync()
            => Task.FromResult<IReadOnlyList<User>>(Candidates);

        public Task<ImpersonationContext> BeginImpersonationAsync(int userId, UserCrudContext context)
        {
            BeginTargets.Add(userId);
            BeginContexts.Add(context);
            if (BeginException is not null)
            {
                throw BeginException;
            }

            var result = BeginResult with
            {
                TargetUserId = userId,
                ActorUserId = context.UserId,
                Reason = context.Reason ?? BeginResult.Reason,
                Notes = context.Notes,
                Ip = context.Ip,
                DeviceInfo = context.DeviceInfo,
                SessionId = context.SessionId
            };
            BeginResult = result;
            ActiveContext = result;
            IsImpersonating = true;
            ImpersonatedUserId = userId;
            return Task.FromResult(result);
        }

        public Task EndImpersonationAsync(UserCrudContext auditContext)
        {
            EndContexts.Add(auditContext);
            if (EndException is not null)
            {
                throw EndException;
            }

            IsImpersonating = false;
            ImpersonatedUserId = null;
            ActiveContext = null;
            return Task.CompletedTask;
        }
    }
}
