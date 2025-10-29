using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Tests;

public class SecurityImpersonationWorkflowServiceTests
{
    [Fact]
    public async Task BeginImpersonationAsync_StoresActiveContext()
    {
        var userService = new FakeUserCrudService();
        var workflow = new SecurityImpersonationWorkflowService(userService);
        var context = UserCrudContext.Create(99, "127.0.0.1", "UnitTest", "sess-begin", "Audit", "Review");

        var impersonation = await workflow.BeginImpersonationAsync(12, context).ConfigureAwait(false);

        Assert.True(workflow.IsImpersonating);
        Assert.Equal(12, workflow.ImpersonatedUserId);
        Assert.Equal(impersonation, workflow.ActiveContext);
        Assert.Equal(impersonation, userService.LastBeginImpersonationContext);
        Assert.Equal(context, userService.LastBeginImpersonationRequestContext);
    }

    [Fact]
    public async Task BeginImpersonationAsync_WhenAlreadyActiveThrows()
    {
        var userService = new FakeUserCrudService();
        var workflow = new SecurityImpersonationWorkflowService(userService);
        var context = UserCrudContext.Create(90, "10.0.0.5", "Surface", "sess-existing", "Audit", "Review");
        await workflow.BeginImpersonationAsync(15, context).ConfigureAwait(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.BeginImpersonationAsync(16, context)).ConfigureAwait(false);
        Assert.Single(userService.BeginImpersonationRequests);
    }

    [Fact]
    public async Task BeginImpersonationAsync_WhenServiceReturnsNullThrows()
    {
        var userService = new NullImpersonationUserCrudService();
        var workflow = new SecurityImpersonationWorkflowService(userService);
        var context = UserCrudContext.Create(77, "10.0.0.8", "Lab", "sess-null", "Audit", "Denied");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.BeginImpersonationAsync(20, context)).ConfigureAwait(false);
        Assert.Equal("Impersonation request was denied by the server.", exception.Message);
    }

    [Fact]
    public async Task EndImpersonationAsync_ClearsActiveContext()
    {
        var userService = new FakeUserCrudService();
        var workflow = new SecurityImpersonationWorkflowService(userService);
        var requestContext = UserCrudContext.Create(45, "192.168.1.10", "Workstation", "sess-end", "Audit", "Review");
        await workflow.BeginImpersonationAsync(30, requestContext).ConfigureAwait(false);
        var auditContext = UserCrudContext.Create(45, "192.168.1.10", "Workstation", "sess-end", "Audit", "Complete");

        await workflow.EndImpersonationAsync(auditContext).ConfigureAwait(false);

        Assert.False(workflow.IsImpersonating);
        Assert.Null(workflow.ActiveContext);
        Assert.Equal(auditContext, userService.LastEndImpersonationAuditContext);
        Assert.Equal(30, userService.LastEndImpersonationContext?.TargetUserId);
    }

    [Fact]
    public async Task EndImpersonationAsync_WithNoActiveSessionDoesNothing()
    {
        var userService = new FakeUserCrudService();
        var workflow = new SecurityImpersonationWorkflowService(userService);
        var auditContext = UserCrudContext.Create(12, "127.0.0.1", "UnitTest", "sess-none", "Audit", "Notes");

        await workflow.EndImpersonationAsync(auditContext).ConfigureAwait(false);

        Assert.Empty(userService.EndImpersonationRequests);
    }

    private sealed class NullImpersonationUserCrudService : IUserCrudService
    {
        public List<(ImpersonationContext Context, UserCrudContext AuditContext)> EndRequests { get; } = new();

        public Task<IReadOnlyList<User>> GetAllAsync() => Task.FromResult<IReadOnlyList<User>>(Array.Empty<User>());

        public Task<User?> TryGetByIdAsync(int id) => Task.FromResult<User?>(null);

        public Task<IReadOnlyList<Role>> GetAllRolesAsync() => Task.FromResult<IReadOnlyList<Role>>(Array.Empty<Role>());

        public Task<CrudSaveResult> CreateAsync(User user, string password, UserCrudContext context)
            => Task.FromResult(new CrudSaveResult(user.Id, new SignatureMetadataDto()));

        public Task<CrudSaveResult> UpdateAsync(User user, string? password, UserCrudContext context)
            => Task.FromResult(new CrudSaveResult(user.Id, new SignatureMetadataDto()));

        public Task UpdateRoleAssignmentsAsync(int userId, IReadOnlyCollection<int> roleIds, UserCrudContext context)
            => Task.CompletedTask;

        public Task DeactivateAsync(int userId, UserCrudContext context)
            => Task.CompletedTask;

        public Task<ImpersonationContext?> BeginImpersonationAsync(int targetUserId, UserCrudContext context)
            => Task.FromResult<ImpersonationContext?>(null);

        public Task EndImpersonationAsync(ImpersonationContext context, UserCrudContext auditContext)
        {
            EndRequests.Add((context, auditContext));
            return Task.CompletedTask;
        }

        public void Validate(User user)
        {
        }
    }
}
