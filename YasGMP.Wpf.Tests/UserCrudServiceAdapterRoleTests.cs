using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Tests;

public sealed class UserCrudServiceAdapterRoleTests
{
    [Fact]
    public async Task UpdateRoleAssignmentsAsync_GrantsMissingRoles()
    {
        var rbac = new RecordingRbacService
        {
            RolesForUser =
            [
                new Role { Id = 5, Name = "Existing" }
            ]
        };
        var adapter = new UserCrudServiceAdapter(new NullUserService(), rbac);
        var context = UserCrudContext.Create(actingUserId: 77, ip: "127.0.0.1", deviceInfo: "UnitTest", sessionId: "sess-add", reason: "audit", notes: "grant");

        await adapter.UpdateRoleAssignmentsAsync(10, new[] { 5, 9 }, context).ConfigureAwait(false);

        Assert.Empty(rbac.Revocations);
        var grant = Assert.Single(rbac.Grants);
        Assert.Equal(10, grant.UserId);
        Assert.Equal(9, grant.RoleId);
        Assert.Equal(77, grant.ActingUserId);
    }

    [Fact]
    public async Task UpdateRoleAssignmentsAsync_RevokesMissingAssignments()
    {
        var rbac = new RecordingRbacService
        {
            RolesForUser =
            [
                new Role { Id = 3, Name = "Remove" },
                new Role { Id = 4, Name = "Keep" }
            ]
        };
        var adapter = new UserCrudServiceAdapter(new NullUserService(), rbac);
        var context = UserCrudContext.Create(actingUserId: 91, ip: "10.0.0.2", deviceInfo: "UnitTest", sessionId: "sess-remove", reason: "audit", notes: "revoke");

        await adapter.UpdateRoleAssignmentsAsync(12, new[] { 4 }, context).ConfigureAwait(false);

        Assert.Empty(rbac.Grants);
        var revoke = Assert.Single(rbac.Revocations);
        Assert.Equal(12, revoke.UserId);
        Assert.Equal(3, revoke.RoleId);
        Assert.Equal(91, revoke.ActingUserId);
        Assert.Equal("WPF shell update", revoke.Reason);
    }

    [Fact]
    public async Task UpdateRoleAssignmentsAsync_NoChangesWhenAssignmentsMatch()
    {
        var rbac = new RecordingRbacService
        {
            RolesForUser =
            [
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "Auditor" }
            ]
        };
        var adapter = new UserCrudServiceAdapter(new NullUserService(), rbac);
        var context = UserCrudContext.Create(actingUserId: 35, ip: "192.168.0.5", deviceInfo: "UnitTest", sessionId: "sess-none", reason: "audit", notes: "noop");

        await adapter.UpdateRoleAssignmentsAsync(20, new[] { 1, 2 }, context).ConfigureAwait(false);

        Assert.Empty(rbac.Grants);
        Assert.Empty(rbac.Revocations);
        Assert.Equal(1, rbac.GetRolesForUserCalls);
    }

    private sealed class RecordingRbacService : IRBACService
    {
        public List<Role> RolesForUser { get; set; } = new();

        public List<(int UserId, int RoleId, int ActingUserId, DateTime? ExpiresAt, string Reason)> Grants { get; } = new();

        public List<(int UserId, int RoleId, int ActingUserId, string Reason)> Revocations { get; } = new();

        public int GetRolesForUserCalls { get; private set; }

        public Task<List<Role>> GetRolesForUserAsync(int userId)
        {
            GetRolesForUserCalls++;
            return Task.FromResult(RolesForUser.Select(r => new Role { Id = r.Id, Name = r.Name }).ToList());
        }

        public Task GrantRoleAsync(int userId, int roleId, int grantedBy, DateTime? expiresAt = null, string reason = "")
        {
            Grants.Add((userId, roleId, grantedBy, expiresAt, reason));
            return Task.CompletedTask;
        }

        public Task RevokeRoleAsync(int userId, int roleId, int revokedBy, string reason = "")
        {
            Revocations.Add((userId, roleId, revokedBy, reason));
            return Task.CompletedTask;
        }

        public Task AssertPermissionAsync(int userId, string permissionCode) => Task.CompletedTask;

        public Task<bool> HasPermissionAsync(int userId, string permissionCode) => Task.FromResult(true);

        public Task<List<string>> GetAllUserPermissionsAsync(int userId) => Task.FromResult(new List<string>());

        public Task<List<Role>> GetAvailableRolesForUserAsync(int userId) => Task.FromResult(new List<Role>());

        public Task GrantPermissionAsync(int userId, string permissionCode, int grantedBy, DateTime? expiresAt = null, string reason = "") => Task.CompletedTask;

        public Task RevokePermissionAsync(int userId, string permissionCode, int revokedBy, string reason = "") => Task.CompletedTask;

        public Task DelegatePermissionAsync(int fromUserId, int toUserId, string permissionCode, int grantedBy, DateTime expiresAt, string reason = "") => Task.CompletedTask;

        public Task RevokeDelegatedPermissionAsync(int delegatedPermissionId, int revokedBy, string reason = "") => Task.CompletedTask;

        public Task<List<Role>> GetAllRolesAsync() => Task.FromResult(new List<Role>());

        public Task<List<Permission>> GetAllPermissionsAsync() => Task.FromResult(new List<Permission>());

        public Task<List<Permission>> GetPermissionsForRoleAsync(int roleId) => Task.FromResult(new List<Permission>());

        public Task<List<Permission>> GetPermissionsNotInRoleAsync(int roleId) => Task.FromResult(new List<Permission>());

        public Task AddPermissionToRoleAsync(int roleId, int permissionId, int adminUserId, string reason = "") => Task.CompletedTask;

        public Task RemovePermissionFromRoleAsync(int roleId, int permissionId, int adminUserId, string reason = "") => Task.CompletedTask;

        public Task<int> CreateRoleAsync(Role role, int adminUserId) => Task.FromResult(0);

        public Task UpdateRoleAsync(Role role, int adminUserId) => Task.CompletedTask;

        public Task DeleteRoleAsync(int roleId, int adminUserId, string reason = "") => Task.CompletedTask;

        public Task<int> RequestPermissionAsync(int userId, string permissionCode, string reason) => Task.FromResult(0);

        public Task ApprovePermissionRequestAsync(int requestId, int approvedBy, string comment) => Task.CompletedTask;

        public Task DenyPermissionRequestAsync(int requestId, int deniedBy, string comment) => Task.CompletedTask;
    }

    private sealed class NullUserService : IUserService
    {
        public Task<User?> AuthenticateAsync(string username, string password) => Task.FromResult<User?>(null);

        public string HashPassword(string password) => password ?? string.Empty;

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

        public bool ValidateDigitalSignature(string signature) => !string.IsNullOrWhiteSpace(signature);

        public Task LogUserEventAsync(int userId, string eventType, string details) => Task.CompletedTask;

        public Task UnlockUserAsync(int userId, int adminId) => Task.CompletedTask;

        public Task SetTwoFactorEnabledAsync(int userId, bool enabled) => Task.CompletedTask;

        public Task UpdateUserProfileAsync(User user, int adminId = 0) => Task.CompletedTask;

        public Task<ImpersonationContext?> BeginImpersonationAsync(int targetUserId, UserCrudContext context) => Task.FromResult<ImpersonationContext?>(null);

        public Task EndImpersonationAsync(ImpersonationContext context, UserCrudContext auditContext) => Task.CompletedTask;
    }
}
