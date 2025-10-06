using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services.Interfaces
{
    /// <summary>
    /// <b>IRBACService</b> – Interface for GMP/21CFR11-compliant Role-Based Access Control in YasGMP.
    /// Provides user/role/permission/assignment/delegation/approval workflow and enforcement checks.
    /// </summary>
    public interface IRBACService
    {
        // ---- Enforcement / Inspection ----
        Task AssertPermissionAsync(int userId, string permissionCode);
        Task<bool> HasPermissionAsync(int userId, string permissionCode);
        Task<List<string>> GetAllUserPermissionsAsync(int userId);

        // ---- User ↔ Roles ----
        Task GrantRoleAsync(int userId, int roleId, int grantedBy, DateTime? expiresAt = null, string reason = "");
        Task RevokeRoleAsync(int userId, int roleId, int revokedBy, string reason = "");
        Task<List<Role>> GetRolesForUserAsync(int userId);
        Task<List<Role>> GetAvailableRolesForUserAsync(int userId);

        // ---- User ↔ Permissions (direct) ----
        Task GrantPermissionAsync(int userId, string permissionCode, int grantedBy, DateTime? expiresAt = null, string reason = "");
        Task RevokePermissionAsync(int userId, string permissionCode, int revokedBy, string reason = "");

        // ---- Delegations ----
        Task DelegatePermissionAsync(int fromUserId, int toUserId, string permissionCode, int grantedBy, DateTime expiresAt, string reason = "");
        Task RevokeDelegatedPermissionAsync(int delegatedPermissionId, int revokedBy, string reason = "");

        // ---- Reads / Lookups ----
        Task<List<Role>> GetAllRolesAsync();
        Task<List<Permission>> GetAllPermissionsAsync();
        Task<List<Permission>> GetPermissionsForRoleAsync(int roleId);
        Task<List<Permission>> GetPermissionsNotInRoleAsync(int roleId);

        // ---- Role ↔ Permission writes ----
        Task AddPermissionToRoleAsync(int roleId, int permissionId, int adminUserId, string reason = "");
        Task RemovePermissionFromRoleAsync(int roleId, int permissionId, int adminUserId, string reason = "");

        // ---- Roles CRUD (for Admin UI) ----
        Task<int> CreateRoleAsync(Role role, int adminUserId);
        Task UpdateRoleAsync(Role role, int adminUserId);
        Task DeleteRoleAsync(int roleId, int adminUserId, string reason = "");

        // ---- Approval workflow ----
        Task<int> RequestPermissionAsync(int userId, string permissionCode, string reason);
        Task ApprovePermissionRequestAsync(int requestId, int approvedBy, string comment);
        Task DenyPermissionRequestAsync(int requestId, int deniedBy, string comment);
    }
}

