using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;                 // MySqlParameter
using YasGMP.Models;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>RBACService</b> – GMP / 21 CFR Part 11 compliant Role-Based Access Control (RBAC) for YasGMP.
    /// Manages users, roles, permissions, delegations, approval workflow and audit-grade security events.
    /// <para>
    /// This implementation is aligned with the current schema and your <see cref="DatabaseService"/> signatures.
    /// It relies on DB helpers surfaced by <c>DatabaseService</c> and (for backward compatibility) the extension
    /// shims in <c>DatabaseServiceRbacCoreExtensions</c> to ensure builds succeed across schema versions.
    /// </para>
    /// </summary>
    public class RBACService : IRBACService
    {
        private readonly DatabaseService _db;
        private readonly AuditService _audit;

        /// <summary>
        /// Initializes a new instance of the <see cref="RBACService"/> class.
        /// </summary>
        /// <param name="databaseService">Database helper for parameterized SQL and lookups.</param>
        /// <param name="auditService">Audit writer for system/entity events.</param>
        /// <exception cref="ArgumentNullException">Thrown when any dependency is <see langword="null"/>.</exception>
        public RBACService(DatabaseService databaseService, AuditService auditService)
        {
            _db = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _audit = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        #region === Enforcement / Inspection ===

        /// <summary>
        /// Asserts that the user has a given permission; throws if not.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="permissionCode">Permission code to check.</param>
        /// <exception cref="UnauthorizedAccessException">Thrown when the permission is not held.</exception>
        public async Task AssertPermissionAsync(int userId, string permissionCode)
        {
            if (!await HasPermissionAsync(userId, permissionCode).ConfigureAwait(false))
                throw new UnauthorizedAccessException($"User {userId} does not have permission: {permissionCode}");
        }

        /// <summary>
        /// Returns <c>true</c> if the user has the permission directly, via role, or via delegation.
        /// </summary>
        public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
        {
            // 1) direct
            if (await _db.HasDirectUserPermissionAsync(userId, permissionCode).ConfigureAwait(false)) return true;

            // 2) role-based
            var roleIds = await _db.GetUserRoleIdsAsync(userId).ConfigureAwait(false);
            foreach (var rid in roleIds)
                if (await _db.HasRolePermissionAsync(rid, permissionCode).ConfigureAwait(false)) return true;

            // 3) delegated
            if (await _db.HasDelegatedPermissionAsync(userId, permissionCode).ConfigureAwait(false)) return true;

            return false;
        }

        /// <summary>
        /// Returns all effective permission codes for a user (direct + roles + delegations).
        /// </summary>
        public async Task<List<string>> GetAllUserPermissionsAsync(int userId)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var direct = await _db.GetUserPermissionCodesAsync(userId).ConfigureAwait(false);
            foreach (var p in direct) set.Add(p);

            var roleIds = await _db.GetUserRoleIdsAsync(userId).ConfigureAwait(false);
            foreach (var rid in roleIds)
                foreach (var p in await _db.GetRolePermissionCodesAsync(rid).ConfigureAwait(false))
                    set.Add(p);

            var delegated = await _db.GetDelegatedPermissionCodesAsync(userId).ConfigureAwait(false);
            foreach (var p in delegated) set.Add(p);

            return set.ToList();
        }

        #endregion

        #region === User ↔ Roles ===

        /// <summary>
        /// Grants a role to a user (optionally expiring), writes audit.
        /// </summary>
        public async Task GrantRoleAsync(int userId, int roleId, int grantedBy, DateTime? expiresAt = null, string reason = "")
        {
            await _db.AddUserRoleAsync(userId, roleId, grantedBy, expiresAt).ConfigureAwait(false);

            await _db.LogPermissionChangeAsync(
                targetUserId: userId,
                changedBy: grantedBy,
                changeType: "role",
                roleId: roleId,
                permissionId: null,
                action: "grant",
                reason: reason,
                expiresAt: expiresAt
            ).ConfigureAwait(false);

            await _audit.LogSystemEventAsync("GRANT_ROLE", $"Granted role {roleId} to user {userId}. Reason: {reason}");
        }

        /// <summary>
        /// Revokes a role from a user, writes audit.
        /// </summary>
        public async Task RevokeRoleAsync(int userId, int roleId, int revokedBy, string reason = "")
        {
            await _db.RemoveUserRoleAsync(userId, roleId).ConfigureAwait(false);

            await _db.LogPermissionChangeAsync(
                targetUserId: userId,
                changedBy: revokedBy,
                changeType: "role",
                roleId: roleId,
                permissionId: null,
                action: "revoke",
                reason: reason,
                expiresAt: null
            ).ConfigureAwait(false);

            await _audit.LogSystemEventAsync("REVOKE_ROLE", $"Revoked role {roleId} from user {userId}. Reason: {reason}");
        }

        /// <summary>Gets roles assigned to the user.</summary>
        public async Task<List<Role>> GetRolesForUserAsync(int userId)
        {
            var ids = await _db.GetUserRoleIdsAsync(userId).ConfigureAwait(false);
            var roles = await _db.GetAllRolesAsync().ConfigureAwait(false);
            return roles.Where(r => ids.Contains(r.Id)).ToList();
        }

        /// <summary>Gets roles not assigned to the user (available to add).</summary>
        public async Task<List<Role>> GetAvailableRolesForUserAsync(int userId)
        {
            var assigned = await _db.GetUserRoleIdsAsync(userId).ConfigureAwait(false);
            var roles = await _db.GetAllRolesAsync().ConfigureAwait(false);
            return roles.Where(r => !assigned.Contains(r.Id)).ToList();
        }

        #endregion

        #region === User ↔ Permissions (direct) ===

        /// <summary>
        /// Grants a direct permission to a user (optionally expiring), writes audit.
        /// </summary>
        public async Task GrantPermissionAsync(int userId, string permissionCode, int grantedBy, DateTime? expiresAt = null, string reason = "")
        {
            int permId = await _db.GetPermissionIdByCodeAsync(permissionCode).ConfigureAwait(false);

            await _db.AddUserPermissionAsync(
                userId: userId,
                permissionId: permId,
                grantedBy: grantedBy,
                expiresAt: expiresAt,
                allowed: true,
                reason: reason
            ).ConfigureAwait(false);

            await _db.LogPermissionChangeAsync(
                targetUserId: userId,
                changedBy: grantedBy,
                changeType: "permission",
                roleId: null,
                permissionId: permId,
                action: "grant",
                reason: reason,
                expiresAt: expiresAt
            ).ConfigureAwait(false);

            await _audit.LogSystemEventAsync("GRANT_PERMISSION", $"Granted permission {permissionCode} to user {userId}. Reason: {reason}");
        }

        /// <summary>
        /// Revokes a direct permission from a user, writes audit.
        /// </summary>
        public async Task RevokePermissionAsync(int userId, string permissionCode, int revokedBy, string reason = "")
        {
            int permId = await _db.GetPermissionIdByCodeAsync(permissionCode).ConfigureAwait(false);

            await _db.RemoveUserPermissionAsync(userId, permId).ConfigureAwait(false);

            await _db.LogPermissionChangeAsync(
                targetUserId: userId,
                changedBy: revokedBy,
                changeType: "permission",
                roleId: null,
                permissionId: permId,
                action: "revoke",
                reason: reason,
                expiresAt: null
            ).ConfigureAwait(false);

            await _audit.LogSystemEventAsync("REVOKE_PERMISSION", $"Revoked permission {permissionCode} from user {userId}. Reason: {reason}");
        }

        #endregion

        #region === Delegations ===

        /// <summary>
        /// Delegates a permission from one user to another (with expiry), writes audit.
        /// </summary>
        public async Task DelegatePermissionAsync(int fromUserId, int toUserId, string permissionCode, int grantedBy, DateTime expiresAt, string reason = "")
        {
            int permId = await _db.GetPermissionIdByCodeAsync(permissionCode).ConfigureAwait(false);

            await _db.AddDelegatedPermissionAsync(
                fromUserId: fromUserId,
                toUserId: toUserId,
                permissionId: permId,
                grantedBy: grantedBy,
                expiresAt: expiresAt,
                reason: reason
            ).ConfigureAwait(false);

            await _db.LogPermissionChangeAsync(
                targetUserId: toUserId,
                changedBy: grantedBy,
                changeType: "delegation",
                roleId: null,
                permissionId: permId,
                action: "grant",
                reason: reason,
                expiresAt: expiresAt
            ).ConfigureAwait(false);

            await _audit.LogSystemEventAsync("DELEGATE_PERMISSION", $"Delegated {permissionCode} from user {fromUserId} to {toUserId}. Reason: {reason}");
        }

        /// <summary>
        /// Revokes a delegated permission (by its ID), writes audit.
        /// </summary>
        public async Task RevokeDelegatedPermissionAsync(int delegatedPermissionId, int revokedBy, string reason = "")
        {
            await _db.RevokeDelegatedPermissionAsync(delegatedPermissionId).ConfigureAwait(false);

            await _db.LogPermissionChangeAsync(
                targetUserId: 0, // informational
                changedBy: revokedBy,
                changeType: "delegation",
                roleId: null,
                permissionId: null,
                action: "revoke",
                reason: reason,
                expiresAt: null
            ).ConfigureAwait(false);

            await _audit.LogSystemEventAsync("REVOKE_DELEGATED_PERMISSION", $"Revoked delegated permission ID={delegatedPermissionId}. Reason: {reason}");
        }

        #endregion

        #region === Lookups ===

        /// <summary>Returns all non-deleted roles.</summary>
        public Task<List<Role>> GetAllRolesAsync() => _db.GetAllRolesAsync();

        /// <summary>Returns all permissions.</summary>
        public Task<List<Permission>> GetAllPermissionsAsync() => _db.GetAllPermissionsAsync();

        /// <summary>Returns permissions assigned to the role.</summary>
        public async Task<List<Permission>> GetPermissionsForRoleAsync(int roleId)
        {
            var codes = await _db.GetRolePermissionCodesAsync(roleId).ConfigureAwait(false);
            var all = await _db.GetAllPermissionsAsync().ConfigureAwait(false);
            return all.Where(p => codes.Contains(p.Code, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>Returns permissions not assigned to the role (available to add).</summary>
        public async Task<List<Permission>> GetPermissionsNotInRoleAsync(int roleId)
        {
            var codes = await _db.GetRolePermissionCodesAsync(roleId).ConfigureAwait(false);
            var all = await _db.GetAllPermissionsAsync().ConfigureAwait(false);
            return all.Where(p => !codes.Contains(p.Code, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        #endregion

        #region === Role ↔ Permission writes ===

        /// <summary>
        /// Adds a permission to a role (uses mapping table), writes audit.
        /// </summary>
        public async Task AddPermissionToRoleAsync(int roleId, int permissionId, int adminUserId, string reason = "")
        {
            // match schema exactly; there is no 'reason' column in role_permissions.
            const string sql = @"
                INSERT INTO role_permissions (role_id, permission_id, allowed, assigned_by, assigned_at)
                VALUES (@r, @p, 1, @u, UTC_TIMESTAMP())
                ON DUPLICATE KEY UPDATE
                    allowed=VALUES(allowed),
                    assigned_by=VALUES(assigned_by),
                    assigned_at=VALUES(assigned_at);";

            await _db.ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@r", roleId),
                new MySqlParameter("@p", permissionId),
                new MySqlParameter("@u", adminUserId),
            }).ConfigureAwait(false);

            await _db.LogPermissionChangeAsync(
                targetUserId: 0,
                changedBy: adminUserId,
                changeType: "role_permission",
                roleId: roleId,
                permissionId: permissionId,
                action: "grant",
                reason: reason,
                expiresAt: null
            ).ConfigureAwait(false);

            await _audit.LogSystemEventAsync("ROLE_PERMISSION_GRANT", $"role={roleId}, perm={permissionId}, by={adminUserId}, {reason}");
        }

        /// <summary>
        /// Removes a permission from a role, writes audit.
        /// </summary>
        public async Task RemovePermissionFromRoleAsync(int roleId, int permissionId, int adminUserId, string reason = "")
        {
            const string sql = "DELETE FROM role_permissions WHERE role_id=@r AND permission_id=@p";
            await _db.ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@r", roleId),
                new MySqlParameter("@p", permissionId),
            }).ConfigureAwait(false);

            await _db.LogPermissionChangeAsync(
                targetUserId: 0,
                changedBy: adminUserId,
                changeType: "role_permission",
                roleId: roleId,
                permissionId: permissionId,
                action: "revoke",
                reason: reason,
                expiresAt: null
            ).ConfigureAwait(false);

            await _audit.LogSystemEventAsync("ROLE_PERMISSION_REVOKE", $"role={roleId}, perm={permissionId}, by={adminUserId}, {reason}");
        }

        #endregion

        #region === Roles CRUD (soft delete) ===

        /// <summary>
        /// Creates a new role and returns its ID; writes audit.
        /// </summary>
        public async Task<int> CreateRoleAsync(Role role, int adminUserId)
        {
            const string sql = @"
              INSERT INTO roles(name, description, org_unit, compliance_tags, is_deleted, notes, created_at, updated_at, created_by_id, last_modified_by_id, version)
              VALUES (@n,@d,@ou,@ct,0,@note,UTC_TIMESTAMP(),UTC_TIMESTAMP(),@by,@by,1);
              SELECT LAST_INSERT_ID();";

            var idObj = await _db.ExecuteScalarAsync(sql, new[]
            {
                new MySqlParameter("@n", role.Name ?? string.Empty),
                new MySqlParameter("@d", (object?)role.Description ?? DBNull.Value),
                new MySqlParameter("@ou", (object?)role.OrgUnit ?? DBNull.Value),
                new MySqlParameter("@ct", (object?)role.ComplianceTags ?? DBNull.Value),
                new MySqlParameter("@note", (object?)role.Notes ?? DBNull.Value),
                new MySqlParameter("@by", adminUserId),
            }).ConfigureAwait(false);

            var id = Convert.ToInt32(idObj);
            await _audit.LogSystemEventAsync("ROLE_CREATE", $"role={id} name={role.Name} by={adminUserId}");
            return id;
        }

        /// <summary>
        /// Updates an existing role; writes audit.
        /// </summary>
        public async Task UpdateRoleAsync(Role role, int adminUserId)
        {
            const string sql = @"
              UPDATE roles
              SET name=@n, description=@d, org_unit=@ou, compliance_tags=@ct,
                  notes=@note, updated_at=UTC_TIMESTAMP(), last_modified_by_id=@by, version=version+1
              WHERE id=@id AND is_deleted=0;";

            await _db.ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@id", role.Id),
                new MySqlParameter("@n",  role.Name ?? string.Empty),
                new MySqlParameter("@d",  (object?)role.Description ?? DBNull.Value),
                new MySqlParameter("@ou", (object?)role.OrgUnit ?? DBNull.Value),
                new MySqlParameter("@ct", (object?)role.ComplianceTags ?? DBNull.Value),
                new MySqlParameter("@note", (object?)role.Notes ?? DBNull.Value),
                new MySqlParameter("@by", adminUserId),
            }).ConfigureAwait(false);

            await _audit.LogSystemEventAsync("ROLE_UPDATE", $"role={role.Id} by={adminUserId}");
        }

        /// <summary>
        /// Soft-deletes a role (and cleans mappings); writes audit.
        /// </summary>
        public async Task DeleteRoleAsync(int roleId, int adminUserId, string reason = "")
        {
            await _db.ExecuteNonQueryAsync(
                "UPDATE roles SET is_deleted=1, updated_at=UTC_TIMESTAMP(), last_modified_by_id=@by, version=version+1 WHERE id=@id",
                new[] { new MySqlParameter("@by", adminUserId), new MySqlParameter("@id", roleId) }
            ).ConfigureAwait(false);

            await _db.ExecuteNonQueryAsync(
                "DELETE FROM role_permissions WHERE role_id=@id",
                new[] { new MySqlParameter("@id", roleId) }
            ).ConfigureAwait(false);

            await _db.ExecuteNonQueryAsync(
                "DELETE FROM user_roles WHERE role_id=@id",
                new[] { new MySqlParameter("@id", roleId) }
            ).ConfigureAwait(false);

            await _db.LogPermissionChangeAsync(
                targetUserId: 0,
                changedBy: adminUserId,
                changeType: "role",
                roleId: roleId,
                permissionId: null,
                action: "delete",
                reason: reason,
                expiresAt: null
            ).ConfigureAwait(false);

            await _audit.LogSystemEventAsync("ROLE_DELETE", $"role={roleId}, by={adminUserId}, {reason}");
        }

        #endregion

        #region === Approval workflow ===

        /// <summary>Creates a permission request and returns its ID.</summary>
        public async Task<int> RequestPermissionAsync(int userId, string permissionCode, string reason)
        {
            int permId = await _db.GetPermissionIdByCodeAsync(permissionCode).ConfigureAwait(false);
            return await _db.AddPermissionRequestAsync(userId, permId, reason).ConfigureAwait(false);
        }

        /// <summary>Approves a permission request; writes audit via <see cref="AuditService"/>.</summary>
        public async Task ApprovePermissionRequestAsync(int requestId, int approvedBy, string comment)
        {
            await _db.ApprovePermissionRequestAsync(requestId, approvedBy, comment).ConfigureAwait(false);
            await _audit.LogSystemEventAsync("PERMISSION_REQUEST_APPROVED", $"Approved request ID={requestId}. Comment: {comment}");
        }

        /// <summary>Denies a permission request; writes audit via <see cref="AuditService"/>.</summary>
        public async Task DenyPermissionRequestAsync(int requestId, int deniedBy, string comment)
        {
            await _db.DenyPermissionRequestAsync(requestId, deniedBy, comment).ConfigureAwait(false);
            await _audit.LogSystemEventAsync("PERMISSION_REQUEST_DENIED", $"Denied request ID={requestId}. Comment: {comment}");
        }

        #endregion
    }
}
