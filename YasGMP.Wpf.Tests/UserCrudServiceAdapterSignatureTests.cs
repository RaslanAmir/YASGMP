using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Tests;

public class UserCrudServiceAdapterSignatureTests
{
    [Fact]
    public async Task CreateAsync_PersistsSignatureAndContext()
    {
        var db = new DatabaseService("Server=localhost;Database=test;Uid=test;Pwd=test;");
        var commands = new List<(string Sql, List<MySqlParameter> Parameters)>();
        db.ExecuteNonQueryOverride = (sql, parameters, _) =>
        {
            commands.Add((sql, parameters?.Select(p => (MySqlParameter)p).ToList() ?? new List<MySqlParameter>()));
            return Task.FromResult(1);
        };
        db.ExecuteScalarOverride = (_, _, _) => Task.FromResult<object?>(42);

        var userService = new TestUserService(db);
        var adapter = new UserCrudServiceAdapter(userService, new NullRbacService());

        var user = new User
        {
            Username = "qa",
            FullName = "QA Lead",
            Role = "admin",
            Email = "qa@example.com"
        };

        var context = new UserCrudContext(
            UserId: 99,
            Ip: "10.0.0.5",
            DeviceInfo: "SurfacePro",
            SessionId: "sess-123",
            SignatureId: 777,
            SignatureHash: "HASH-001",
            SignatureMethod: "password",
            SignatureStatus: "valid",
            SignatureNote: "approval",
            Reason: null,
            Notes: null);

        var result = await adapter.CreateAsync(user, "Password!", context).ConfigureAwait(false);

        Assert.Equal(42, result.Id);
        var command = Assert.Single(commands);
        Assert.Contains("INSERT INTO users", command.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("digital_signature", command.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("last_change_signature", command.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("source_ip", command.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("device_info", command.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("session_id", command.Sql, StringComparison.OrdinalIgnoreCase);

        var parameterValues = command.Parameters.ToDictionary(
            p => p.ParameterName,
            p => p.Value is DBNull ? null : p.Value);

        Assert.Equal("HASH-001", Assert.IsType<string>(parameterValues["@sig"]!));
        Assert.Equal("HASH-001", Assert.IsType<string>(parameterValues["@lsig"]!));
        Assert.Equal("10.0.0.5", Assert.IsType<string>(parameterValues["@ip"]!));
        Assert.Equal("SurfacePro", Assert.IsType<string>(parameterValues["@dev"]!));
        Assert.Equal("sess-123", Assert.IsType<string>(parameterValues["@sid"]!));
        Assert.Equal(99, Assert.IsType<int>(parameterValues["@lmb"]!));
        Assert.Equal("HASH-001", user.DigitalSignature);
        Assert.Equal("HASH-001", user.LastChangeSignature);
        Assert.Equal("10.0.0.5", user.SourceIp);
        Assert.Equal("SurfacePro", user.DeviceInfo);
        Assert.Equal("sess-123", user.SessionId);
    }

    [Fact]
    public async Task UpdateAsync_PersistsSignatureAndContext()
    {
        var db = new DatabaseService("Server=localhost;Database=test;Uid=test;Pwd=test;");
        var commands = new List<(string Sql, List<MySqlParameter> Parameters)>();
        db.ExecuteNonQueryOverride = (sql, parameters, _) =>
        {
            commands.Add((sql, parameters?.Select(p => (MySqlParameter)p).ToList() ?? new List<MySqlParameter>()));
            return Task.FromResult(1);
        };

        var userService = new TestUserService(db);
        var adapter = new UserCrudServiceAdapter(userService, new NullRbacService());

        var user = new User
        {
            Id = 7,
            Username = "qa",
            FullName = "QA Lead",
            Role = "admin",
            Email = "qa@example.com",
            DigitalSignature = "OLD",
            LastChangeSignature = "OLD"
        };

        var context = new UserCrudContext(
            UserId: 21,
            Ip: "192.168.1.10",
            DeviceInfo: "ThinkPad",
            SessionId: "sess-999",
            SignatureId: 888,
            SignatureHash: "HASH-002",
            SignatureMethod: "password",
            SignatureStatus: "valid",
            SignatureNote: "edit",
            Reason: null,
            Notes: null);

        var result = await adapter.UpdateAsync(user, password: null, context).ConfigureAwait(false);

        Assert.Equal(7, result.Id);
        var command = Assert.Single(commands);
        Assert.Contains("UPDATE users", command.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE id=@id", command.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("digital_signature=@sig", command.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("last_change_signature=@lsig", command.Sql, StringComparison.OrdinalIgnoreCase);

        var parameterValues = command.Parameters.ToDictionary(
            p => p.ParameterName,
            p => p.Value is DBNull ? null : p.Value);

        Assert.Equal("HASH-002", Assert.IsType<string>(parameterValues["@sig"]!));
        Assert.Equal("HASH-002", Assert.IsType<string>(parameterValues["@lsig"]!));
        Assert.Equal("192.168.1.10", Assert.IsType<string>(parameterValues["@ip"]!));
        Assert.Equal("ThinkPad", Assert.IsType<string>(parameterValues["@dev"]!));
        Assert.Equal("sess-999", Assert.IsType<string>(parameterValues["@sid"]!));
        Assert.Equal(7, Assert.IsType<int>(parameterValues["@id"]!));
        Assert.Equal(21, Assert.IsType<int>(parameterValues["@lmb"]!));
    }

    private sealed class TestUserService : IUserService
    {
        private readonly DatabaseService _db;

        public TestUserService(DatabaseService db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public Task<User?> AuthenticateAsync(string username, string password) => Task.FromResult<User?>(null);

        public string HashPassword(string password) => password ?? string.Empty;

        public Task<bool> VerifyTwoFactorCodeAsync(string username, string code) => Task.FromResult(false);

        public Task LockUserAsync(int userId) => Task.CompletedTask;

        public Task<List<User>> GetAllUsersAsync() => Task.FromResult(new List<User>());

        public Task<User?> GetUserByIdAsync(int id) => Task.FromResult<User?>(null);

        public Task<User?> GetUserByUsernameAsync(string username) => Task.FromResult<User?>(null);

        public Task CreateUserAsync(User user, int adminId = 0) => _db.InsertOrUpdateUserAsync(user, update: false);

        public Task UpdateUserAsync(User user, int adminId = 0) => _db.InsertOrUpdateUserAsync(user, update: true);

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
    }

    private sealed class NullRbacService : IRBACService
    {
        public Task AssertPermissionAsync(int userId, string permissionCode) => Task.CompletedTask;

        public Task<bool> HasPermissionAsync(int userId, string permissionCode) => Task.FromResult(true);

        public Task<List<string>> GetAllUserPermissionsAsync(int userId) => Task.FromResult(new List<string>());

        public Task GrantRoleAsync(int userId, int roleId, int grantedBy, DateTime? expiresAt = null, string reason = "") => Task.CompletedTask;

        public Task RevokeRoleAsync(int userId, int roleId, int revokedBy, string reason = "") => Task.CompletedTask;

        public Task<List<Role>> GetRolesForUserAsync(int userId) => Task.FromResult(new List<Role>());

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
}
