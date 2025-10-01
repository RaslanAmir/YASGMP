// ==============================================================================
// File: Services/DatabaseService.Rbac.Extensions.cs
// Purpose: Complete RBAC + User-management helpers for DatabaseService
//          (EXCLUDING core Users/Auth APIs which live in
//          DatabaseService.Users.Extensions.cs to avoid ambiguity).
// ------------------------------------------------------------------------------
// Highlights
//  • GetAllUsersAsync / GetAllUsersFullAsync (with RoleIds/PermissionIds hydration)
//  • GetUserByIdAsync
//  • AddUserAsync / UpdateUserAsync / DeleteUserAsync (call Users-extensions for I/U)
//  • RBAC helpers: assign/remove roles & permissions, user lock/unlock, 2FA flag
//  • Safe property resolver prevents AmbiguousMatchException when mapping
//  • Tolerant mapping: reads only columns that exist in the current DB
//  • Export helpers write to export_print_log when present
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using MySqlConnector;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Extension methods that hang off <see cref="DatabaseService"/> to provide
    /// RBAC + user-management functionality used by ViewModels and services.
    /// <para>
    /// <b>Note:</b> Core Users/Auth APIs such as
    /// <see cref="DatabaseServiceUsersExtensions.GetUserByUsernameAsync(DatabaseService, string, CancellationToken)"/>,
    /// <see cref="DatabaseServiceUsersExtensions.InsertOrUpdateUserAsync(DatabaseService, User, bool, CancellationToken)"/>,
    /// <see cref="DatabaseServiceUsersExtensions.MarkLoginSuccessAsync(DatabaseService, int, CancellationToken)"/>,
    /// <see cref="DatabaseServiceUsersExtensions.IncrementFailedLoginsAsync(DatabaseService, int, CancellationToken)"/>
    /// live in <c>DatabaseService.Users.Extensions.cs</c> to prevent CS0121 ambiguity.
    /// </para>
    /// </summary>
    public static class DatabaseServiceRbacExtensions
    {
        // =========================================================================
        // USERS: READ
        // =========================================================================

        /// <summary>
        /// Returns all users (basic projection).
        /// </summary>
        public static async Task<List<User>> GetAllUsersAsync(
            this DatabaseService db,
            CancellationToken token = default)
        {
            const string sql = @"SELECT id, username, full_name, email, role, role_id, active, is_locked, is_two_factor_enabled,
       last_login, last_failed_login, failed_login_attempts, digital_signature, last_change_signature, last_modified, last_modified_by_id, source_ip, device_info, session_id
FROM users ORDER BY full_name, username, id;";
            var dt = await db.ExecuteSelectAsync(sql, null, token).ConfigureAwait(false);

            var list = new List<User>();
            foreach (DataRow r in dt.Rows)
                list.Add(ParseUser(r));
            return list;
        }

        /// <summary>
        /// Overload used by callers that pass <paramref name="includeAudit"/>.
        /// The flag is accepted for signature compatibility; it does not change the projection.
        /// </summary>
        public static Task<List<User>> GetAllUsersAsync(
            this DatabaseService db,
            bool includeAudit,
            CancellationToken token = default)
            => GetAllUsersAsync(db, token);

        /// <summary>
        /// Returns all users and populates <see cref="User.RoleIds"/> /
        /// <see cref="User.PermissionIds"/> from link tables if present.
        /// </summary>
        public static async Task<List<User>> GetAllUsersFullAsync(
            this DatabaseService db,
            CancellationToken token = default)
        {
            var users = await GetAllUsersAsync(db, token).ConfigureAwait(false);
            if (users.Count == 0) return users;

            var userIds = users.Select(u => u.Id).ToList();
            var roleMap = await TryLoadUserRoleMapAsync(db, userIds, token).ConfigureAwait(false);
            var permMap = await TryLoadUserPermissionMapAsync(db, userIds, token).ConfigureAwait(false);

            foreach (var u in users)
            {
                if (roleMap.TryGetValue(u.Id, out var rid)) u.RoleIds = rid.ToArray();
                if (permMap.TryGetValue(u.Id, out var pid)) u.PermissionIds = pid.ToArray();
            }

            return users;
        }

        /// <summary>
        /// Gets a user by ID (schema-tolerant).
        /// </summary>
        public static async Task<User?> GetUserByIdAsync(
            this DatabaseService db,
            int id,
            CancellationToken token = default)
        {
            const string sql = @"SELECT id, username, full_name, email, role, role_id, active, is_locked, is_two_factor_enabled,
       last_login, last_failed_login, failed_login_attempts, digital_signature, last_change_signature, last_modified, last_modified_by_id, source_ip, device_info, session_id
FROM users WHERE id=@id LIMIT 1;";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            return dt.Rows.Count == 1 ? ParseUser(dt.Rows[0]) : null;
        }

        // =========================================================================
        // USERS: CREATE / UPDATE / DELETE
        // =========================================================================

        /// <summary>
        /// Adds a user (admin context overload). The insert is delegated to
        /// <see cref="DatabaseServiceUsersExtensions.InsertOrUpdateUserAsync(DatabaseService, User, bool, CancellationToken)"/>
        /// to keep a single source of truth for the USERS core.
        /// </summary>
        public static async Task<int> AddUserAsync(
            this DatabaseService db,
            User user,
            int adminUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            int id = await DatabaseServiceUsersExtensions
                .InsertOrUpdateUserAsync(db, user, update: false, token)
                .ConfigureAwait(false);

            await db.LogUserAuditAsync(user, "CREATE", ip, deviceInfo, sessionId, $"Created by #{adminUserId}", token).ConfigureAwait(false);

            await db.LogSystemEventAsync(
                adminUserId, "USER_CREATE", "users", "RBAC",
                id, $"User '{user.Username}' created.", ip, "audit", deviceInfo, sessionId, token: token).ConfigureAwait(false);

            return id;
        }

        /// <summary>
        /// Adds a user (signature overload). Insert is delegated to USERS core to avoid duplication.
        /// </summary>
        public static async Task<int> AddUserAsync(
            this DatabaseService db,
            User user,
            string signatureHash,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            int id = await DatabaseServiceUsersExtensions
                .InsertOrUpdateUserAsync(db, user, update: false, token)
                .ConfigureAwait(false);

            await db.LogUserAuditAsync(user, "CREATE", ip, deviceInfo, sessionId, signatureHash, token).ConfigureAwait(false);
            return id;
        }

        /// <summary>
        /// Updates a user (signature overload). Update is delegated to USERS core to avoid duplication.
        /// </summary>
        public static async Task UpdateUserAsync(
            this DatabaseService db,
            User user,
            string signatureHash,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            _ = await DatabaseServiceUsersExtensions
                .InsertOrUpdateUserAsync(db, user, update: true, token)
                .ConfigureAwait(false);

            await db.LogUserAuditAsync(user, "UPDATE", ip, deviceInfo, sessionId, signatureHash, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes a user (admin context). Attempts soft-delete first; if schema lacks the columns,
        /// falls back to a hard delete. Always writes an audit entry.
        /// </summary>
        public static async Task DeleteUserAsync(
            this DatabaseService db,
            int userId,
            int adminUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            int affected = 0;
            try
            {
                affected = await db.ExecuteNonQueryAsync(
                    "UPDATE users SET is_deleted=1, deleted_at=NOW() WHERE id=@id",
                    new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number is 1054 or 1146)
            {
                // ignore unknown column/table and fall back to hard delete
            }

            if (affected == 0)
            {
                await db.ExecuteNonQueryAsync(
                    "DELETE FROM users WHERE id=@id",
                    new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);
            }

            await db.LogUserAuditAsync(null, "DELETE", ip, deviceInfo, sessionId, $"Deleted by #{adminUserId} (UserId={userId})", token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes a user (signature overload).
        /// </summary>
        public static async Task DeleteUserAsync(
            this DatabaseService db,
            int userId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            await db.DeleteUserAsync(userId, adminUserId: 0, ip, deviceInfo, sessionId, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Records a rollback/restore intent for a user (audit only).
        /// </summary>
        public static async Task RollbackUserAsync(
            this DatabaseService db,
            int userId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            await db.LogUserAuditAsync(null, "ROLLBACK", ip, deviceInfo, sessionId, $"Rollback requested for user #{userId}", token).ConfigureAwait(false);

            await db.LogSystemEventAsync(
                null, "USER_ROLLBACK", "users", "RBAC",
                userId, "Rollback requested.", ip, "audit", deviceInfo, sessionId, token: token).ConfigureAwait(false);
        }

        // =========================================================================
        // RBAC: ROLES / PERMISSIONS
        // =========================================================================

        /// <summary>Returns all roles with tolerant mapping.</summary>
        public static async Task<List<Role>> GetAllRolesFullAsync(
            this DatabaseService db,
            CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync("SELECT * FROM roles ORDER BY name, id", null, token).ConfigureAwait(false);
            var list = new List<Role>();
            foreach (DataRow r in dt.Rows)
                list.Add(ParseRole(r));
            return list;
        }

        /// <summary>Returns all permissions with tolerant mapping.</summary>
        public static async Task<List<Permission>> GetAllPermissionsFullAsync(
            this DatabaseService db,
            CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync("SELECT * FROM permissions ORDER BY code, id", null, token).ConfigureAwait(false);
            var list = new List<Permission>();
            foreach (DataRow r in dt.Rows)
                list.Add(ParsePermission(r));
            return list;
        }

        /// <summary>Assigns a role to a user (upsert) and logs audit.</summary>
        public static async Task AssignRoleToUserAsync(
            this DatabaseService db,
            int userId,
            int roleId,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            const string sql = @"
INSERT INTO user_roles (user_id, role_id, granted_by, granted_at)
VALUES (@uid,@rid,@actor,NOW())
ON DUPLICATE KEY UPDATE granted_by=VALUES(granted_by), granted_at=VALUES(granted_at);";

            await db.ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@uid", userId),
                new MySqlParameter("@rid", roleId),
                new MySqlParameter("@actor", actorUserId)
            }, token).ConfigureAwait(false);

            await db.LogUserAuditAsync(null, "ASSIGN_ROLE", ip, deviceInfo, sessionId,
                $"Role #{roleId} → user #{userId} (by #{actorUserId})", token).ConfigureAwait(false);
        }

        /// <summary>Removes a role from a user and logs audit.</summary>
        public static async Task RemoveRoleFromUserAsync(
            this DatabaseService db,
            int userId,
            int roleId,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync(
                "DELETE FROM user_roles WHERE user_id=@u AND role_id=@r",
                new[] { new MySqlParameter("@u", userId), new MySqlParameter("@r", roleId) }, token).ConfigureAwait(false);

            await db.LogUserAuditAsync(null, "REMOVE_ROLE", ip, deviceInfo, sessionId,
                $"Role #{roleId} removed from user #{userId} (by #{actorUserId})", token).ConfigureAwait(false);
        }

        /// <summary>Assigns a permission to a role and logs audit.</summary>
        public static async Task AssignPermissionToRoleAsync(
            this DatabaseService db,
            int roleId,
            int permissionId,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            const string sql = @"
INSERT INTO role_permissions (role_id, permission_id)
VALUES (@rid,@pid)
ON DUPLICATE KEY UPDATE role_id=role_id;";

            await db.ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@rid", roleId),
                new MySqlParameter("@pid", permissionId)
            }, token).ConfigureAwait(false);

            await db.LogRoleAuditAsync(null, "ASSIGN_PERMISSION", ip, deviceInfo, sessionId,
                $"Permission #{permissionId} → role #{roleId} (by #{actorUserId})", token).ConfigureAwait(false);
        }

        /// <summary>Removes a permission from a role and logs audit.</summary>
        public static async Task RemovePermissionFromRoleAsync(
            this DatabaseService db,
            int roleId,
            int permissionId,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync(
                "DELETE FROM role_permissions WHERE role_id=@r AND permission_id=@p",
                new[]
                {
                    new MySqlParameter("@r", roleId),
                    new MySqlParameter("@p", permissionId)
                }, token).ConfigureAwait(false);

            await db.LogRoleAuditAsync(null, "REMOVE_PERMISSION", ip, deviceInfo, sessionId,
                $"Permission #{permissionId} removed from role #{roleId} (by #{actorUserId})", token).ConfigureAwait(false);
        }

        // =========================================================================
        // AUDIT WRITERS (entity-style overloads used by VMs)
        // =========================================================================

        /// <summary>Writes a user audit entry (entity overload).</summary>
        public static Task LogUserAuditAsync(
            this DatabaseService db,
            User? user,
            string action,
            string ip,
            string deviceInfo,
            string? sessionId,
            string? details,
            CancellationToken token = default)
            => db.LogUserAuditAsync(user?.Id ?? 0, action, ip, deviceInfo, sessionId, details, token);

        /// <summary>Writes a user audit entry (raw parameters).</summary>
        public static async Task LogUserAuditAsync(
            this DatabaseService db,
            int userId,
            string action,
            string ip,
            string deviceInfo,
            string? sessionId,
            string? details,
            CancellationToken token = default)
        {
            try
            {
                const string sql = @"
INSERT INTO user_audit (user_id, action, description, created_at, source_ip, device_info, session_id)
VALUES (@uid,@act,@desc,NOW(),@ip,@dev,@sid)";
                await db.ExecuteNonQueryAsync(sql, new[]
                {
                    new MySqlParameter("@uid",  (object?)userId ?? DBNull.Value),
                    new MySqlParameter("@act",  action ?? "UPDATE"),
                    new MySqlParameter("@desc", (object?)details ?? DBNull.Value),
                    new MySqlParameter("@ip",   ip ?? string.Empty),
                    new MySqlParameter("@dev",  deviceInfo ?? string.Empty),
                    new MySqlParameter("@sid",  (object?)sessionId ?? DBNull.Value)
                }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1146) // table missing
            {
                await db.LogSystemEventAsync(null, "USER_AUDIT_FALLBACK", "users", "RBAC",
                    userId, $"{action}: {details}", ip, "warn", deviceInfo, sessionId, token: token).ConfigureAwait(false);
            }
        }

        /// <summary>Writes a role audit entry (entity overload).</summary>
        public static Task LogRoleAuditAsync(
            this DatabaseService db,
            Role? role,
            string action,
            string ip,
            string deviceInfo,
            string? sessionId,
            string? details,
            CancellationToken token = default)
            => db.LogRoleAuditAsync(role?.Id ?? 0, action, ip, deviceInfo, sessionId, details, token);

        /// <summary>Writes a role audit entry (raw parameters).</summary>
        public static async Task LogRoleAuditAsync(
            this DatabaseService db,
            int roleId,
            string action,
            string ip,
            string deviceInfo,
            string? sessionId,
            string? details,
            CancellationToken token = default)
        {
            try
            {
                const string sql = @"
INSERT INTO role_audit (role_id, action, description, created_at, source_ip, device_info, session_id)
VALUES (@rid,@act,@desc,NOW(),@ip,@dev,@sid)";
                await db.ExecuteNonQueryAsync(sql, new[]
                {
                    new MySqlParameter("@rid",  (object?)roleId ?? DBNull.Value),
                    new MySqlParameter("@act",  action ?? "UPDATE"),
                    new MySqlParameter("@desc", (object?)details ?? DBNull.Value),
                    new MySqlParameter("@ip",   ip ?? string.Empty),
                    new MySqlParameter("@dev",  deviceInfo ?? string.Empty),
                    new MySqlParameter("@sid",  (object?)sessionId ?? DBNull.Value)
                }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1146) // table missing
            {
                await db.LogSystemEventAsync(null, "ROLE_AUDIT_FALLBACK", "roles", "RBAC",
                    roleId, $"{action}: {details}", ip, "warn", deviceInfo, sessionId, token: token).ConfigureAwait(false);
            }
        }

        // =========================================================================
        // EXPORTS
        // =========================================================================

        /// <summary>
        /// Exports users and returns the file path (also logs into export_print_log if available).
        /// </summary>
        public static async Task<string> ExportUsersAsync(
            this DatabaseService db,
            IEnumerable<User> users,
            string ip,
            string deviceInfo,
            string? sessionId,
            string format = "csv",
            CancellationToken token = default)
        {
            string fmt = string.IsNullOrWhiteSpace(format) ? "csv" : format.ToLowerInvariant();
            string filePath = $"/export/users_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

            try
            {
                await db.ExecuteNonQueryAsync(@"
INSERT INTO export_print_log (user_id, format, table_name, filter_used, file_path, source_ip, note)
VALUES (NULL,@fmt,'users',@filter,@path,@ip,'Users export')",
                    new[]
                    {
                        new MySqlParameter("@fmt", fmt),
                        new MySqlParameter("@filter", $"count={(users?.Count() ?? 0)}"),
                        new MySqlParameter("@path", filePath),
                        new MySqlParameter("@ip", ip ?? string.Empty)
                    }, token).ConfigureAwait(false);
            }
            catch (MySqlException) { /* table may not exist – ignore */ }

            await db.LogSystemEventAsync(null, "USER_EXPORT", "users", "RBAC",
                null, $"Exported {(users?.Count() ?? 0)} users → {filePath}", ip, "audit", deviceInfo, sessionId, token: token).ConfigureAwait(false);

            return filePath;
        }

        /// <summary>Exports roles and returns a file path.</summary>
        public static async Task<string> ExportRolesAsync(
            this DatabaseService db,
            IEnumerable<Role> roles,
            string ip,
            string deviceInfo,
            string? sessionId,
            string format = "csv",
            CancellationToken token = default)
        {
            string fmt = string.IsNullOrWhiteSpace(format) ? "csv" : format.ToLowerInvariant();
            string filePath = $"/export/roles_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

            try
            {
                await db.ExecuteNonQueryAsync(@"
INSERT INTO export_print_log (user_id, format, table_name, filter_used, file_path, source_ip, note)
VALUES (NULL,@fmt,'roles',@filter,@path,@ip,'Roles export')",
                    new[]
                    {
                        new MySqlParameter("@fmt", fmt),
                        new MySqlParameter("@filter", $"count={(roles?.Count() ?? 0)}"),
                        new MySqlParameter("@path", filePath),
                        new MySqlParameter("@ip", ip ?? string.Empty)
                    }, token).ConfigureAwait(false);
            }
            catch (MySqlException) { /* ignore if table missing */ }

            await db.LogSystemEventAsync(null, "ROLE_EXPORT", "roles", "RBAC",
                null, $"Exported {(roles?.Count() ?? 0)} roles → {filePath}", ip, "audit", deviceInfo, sessionId, token: token).ConfigureAwait(false);

            return filePath;
        }

        /// <summary>Exports permissions and returns a file path.</summary>
        public static async Task<string> ExportPermissionsAsync(
            this DatabaseService db,
            IEnumerable<Permission> perms,
            string ip,
            string deviceInfo,
            string? sessionId,
            string format = "csv",
            CancellationToken token = default)
        {
            string fmt = string.IsNullOrWhiteSpace(format) ? "csv" : format.ToLowerInvariant();
            string filePath = $"/export/permissions_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{fmt}";

            try
            {
                await db.ExecuteNonQueryAsync(@"
INSERT INTO export_print_log (user_id, format, table_name, filter_used, file_path, source_ip, note)
VALUES (NULL,@fmt,'permissions',@filter,@path,@ip,'Permissions export')",
                    new[]
                    {
                        new MySqlParameter("@fmt", fmt),
                        new MySqlParameter("@filter", $"count={(perms?.Count() ?? 0)}"),
                        new MySqlParameter("@path", filePath),
                        new MySqlParameter("@ip", ip ?? string.Empty)
                    }, token).ConfigureAwait(false);
            }
            catch (MySqlException) { /* ignore if table missing */ }

            await db.LogSystemEventAsync(null, "PERMISSION_EXPORT", "permissions", "RBAC",
                null, $"Exported {(perms?.Count() ?? 0)} permissions → {filePath}", ip, "audit", deviceInfo, sessionId, token: token).ConfigureAwait(false);

            return filePath;
        }

        // =========================================================================
        // INTERNAL HELPERS
        // =========================================================================

        /// <summary>
        /// Maps a <see cref="DataRow"/> to a <see cref="User"/> using schema-tolerant column access.
        /// </summary>
        private static User ParseUser(DataRow r)
        {
            var u = new User();
            SetIfExists(u, nameof(User.Id), GetInt(r, "id") ?? 0);
            SetIfExists(u, nameof(User.Username), GetString(r, "username") ?? GetString(r, "user_name"));
            SetIfExists(u, nameof(User.FullName), GetString(r, "full_name") ?? GetString(r, "fullname"));
            // Tolerant to either column name:
            SetIfExists(u, nameof(User.PasswordHash), GetString(r, "password") ?? GetString(r, "password_hash"));
            SetIfExists(u, nameof(User.Email), GetString(r, "email"));
            SetIfExists(u, nameof(User.Role), GetString(r, "role"));
            SetIfExists(u, nameof(User.Active), GetBool(r, "active") ?? true);
            SetIfExists(u, nameof(User.IsLocked), GetBool(r, "is_locked") ?? false);
            SetIfExists(u, nameof(User.IsTwoFactorEnabled), GetBool(r, "is_two_factor_enabled") ?? false);
            SetIfExists(u, nameof(User.LastLogin), GetDate(r, "last_login"));
            SetIfExists(u, nameof(User.DigitalSignature), GetString(r, "digital_signature"));
            SetIfExists(u, nameof(User.LastChangeSignature), GetString(r, "last_change_signature"));
            SetIfExists(u, nameof(User.LastModifiedById), GetInt(r, "last_modified_by_id"));
            SetIfExists(u, nameof(User.LastModified), GetDate(r, "last_modified") ?? u.LastModified);
            SetIfExists(u, nameof(User.SourceIp), GetString(r, "source_ip"));
            SetIfExists(u, nameof(User.DeviceInfo), GetString(r, "device_info"));
            SetIfExists(u, nameof(User.SessionId), GetString(r, "session_id"));

            var fail = GetInt(r, "failed_login_attempts") ?? GetInt(r, "failed_logins") ?? 0;
            SetIfExists(u, nameof(User.FailedLoginAttempts), fail);

            return u;
        }

        /// <summary>
        /// Maps a <see cref="DataRow"/> to a <see cref="Role"/> using tolerant access and the
        /// upgraded <c>roles</c> schema (notes, audit fields, versioning, soft delete).
        /// </summary>
        private static Role ParseRole(DataRow r)
        {
            var x = new Role();
            SetIfExists(x, "Id", GetInt(r, "id") ?? 0);
            SetIfExists(x, "Name", GetString(r, "name"));
            SetIfExists(x, "Code", GetString(r, "code")); // [NotMapped] alias for Name (kept for XAML bindings)
            SetIfExists(x, "Description", GetString(r, "description"));

            // New/extended fields aligned with YASGMP.sql (schema-tolerant)
            SetIfExists(x, "OrgUnit",          GetString(r, "org_unit"));
            SetIfExists(x, "ComplianceTags",   GetString(r, "compliance_tags"));
            SetIfExists(x, "IsDeleted",        GetBool(r, "is_deleted") ?? false);
            SetIfExists(x, "Notes",            GetString(r, "notes"));

            // Audit fields (UTC timestamps)
            SetIfExists(x, "CreatedAt",        GetDate(r, "created_at") ?? DateTime.UtcNow);
            SetIfExists(x, "UpdatedAt",        GetDate(r, "updated_at") ?? DateTime.UtcNow);

            // Actor references
            SetIfExists(x, "CreatedById",      GetInt(r, "created_by_id"));
            SetIfExists(x, "LastModifiedById", GetInt(r, "last_modified_by_id"));

            // Concurrency/version
            SetIfExists(x, "Version",          GetInt(r, "version") ?? 1);

            return x;
        }

        /// <summary>
        /// Maps a <see cref="DataRow"/> to a <see cref="Permission"/> using tolerant access.
        /// </summary>
        private static Permission ParsePermission(DataRow r)
        {
            var p = new Permission();
            SetIfExists(p, "Id", GetInt(r, "id") ?? 0);
            SetIfExists(p, "Code", GetString(r, "code"));
            SetIfExists(p, "Name", GetString(r, "name"));
            SetIfExists(p, "Description", GetString(r, "description"));
            return p;
        }

        private static async Task<Dictionary<int, List<int>>> TryLoadUserRoleMapAsync(
            DatabaseService db, List<int> userIds, CancellationToken token)
        {
            var map = new Dictionary<int, List<int>>();
            if (userIds.Count == 0) return map;

            try
            {
                var pars = userIds.Select((id, i) => new MySqlParameter($"@u{i}", id)).ToArray();
                var inClause = string.Join(",", pars.Select(p => p.ParameterName));
                var dt = await db.ExecuteSelectAsync(
                    $"SELECT user_id, role_id FROM user_roles WHERE user_id IN ({inClause})", pars, token)
                    .ConfigureAwait(false);

                foreach (DataRow r in dt.Rows)
                {
                    int uid = GetInt(r, "user_id") ?? 0;
                    int rid = GetInt(r, "role_id") ?? 0;
                    if (uid == 0 || rid == 0) continue;
                    if (!map.TryGetValue(uid, out var list))
                    {
                        list = new List<int>();
                        map[uid] = list;
                    }
                    list.Add(rid);
                }
            }
            catch (MySqlException)
            {
                // link table may not exist – ignore
            }
            return map;
        }

        private static async Task<Dictionary<int, List<int>>> TryLoadUserPermissionMapAsync(
            DatabaseService db, List<int> userIds, CancellationToken token)
        {
            var map = new Dictionary<int, List<int>>();
            if (userIds.Count == 0) return map;

            try
            {
                var pars = userIds.Select((id, i) => new MySqlParameter($"@u{i}", id)).ToArray();
                var inClause = string.Join(",", pars.Select(p => p.ParameterName));
                var dt = await db.ExecuteSelectAsync(
                    $"SELECT user_id, permission_id FROM user_permissions WHERE user_id IN ({inClause})", pars, token)
                    .ConfigureAwait(false);

                foreach (DataRow r in dt.Rows)
                {
                    int uid = GetInt(r, "user_id") ?? 0;
                    int pid = GetInt(r, "permission_id") ?? 0;
                    if (uid == 0 || pid == 0) continue;
                    if (!map.TryGetValue(uid, out var list))
                    {
                        list = new List<int>();
                        map[uid] = list;
                    }
                    list.Add(pid);
                }
            }
            catch (MySqlException)
            {
                // link table may not exist – ignore
            }
            return map;
        }

        // =========================================================================
        // TINY HELPERS
        // =========================================================================

        private static int? GetInt(DataRow r, string col) =>
            r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? Convert.ToInt32(r[col]) : (int?)null;

        private static string? GetString(DataRow r, string col) =>
            r.Table.Columns.Contains(col) ? r[col]?.ToString() : null;

        private static DateTime? GetDate(DataRow r, string col) =>
            r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? Convert.ToDateTime(r[col]) : (DateTime?)null;

        private static bool? GetBool(DataRow r, string col) =>
            r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? Convert.ToBoolean(r[col]) : (bool?)null;

        // =========================================================================
        // PROPERTY RESOLVER + SETTER (safe against AmbiguousMatchException)
        // =========================================================================

        private static PropertyInfo? ResolveProperty(Type type, string name)
        {
            const BindingFlags F = BindingFlags.Instance | BindingFlags.Public;
            var props = type.GetProperties(F);

            // 1) exact case
            var exact = props.FirstOrDefault(p => p.Name.Equals(name, StringComparison.Ordinal));
            if (exact != null) return exact;

            // 2) case-insensitive
            var ci = props.Where(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (ci.Count == 0) return null;
            if (ci.Count == 1) return ci[0];

            // 3) prefer not [NotMapped]
            var noNotMapped = ci.Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null).ToList();
            if (noNotMapped.Count == 1) return noNotMapped[0];
            if (noNotMapped.Count > 1) ci = noNotMapped;

            // 4) prefer [Column(Name=...)]
            var byColumn = ci.FirstOrDefault(p =>
            {
                var col = p.GetCustomAttribute<ColumnAttribute>();
                return col != null && !string.IsNullOrWhiteSpace(col.Name) &&
                       col.Name.Equals(name, StringComparison.OrdinalIgnoreCase);
            });
            if (byColumn != null) return byColumn;

            // 5) deterministic fallback
            return ci.OrderBy(p => p.Name, StringComparer.Ordinal).First();
        }

        private static void SetIfExists<TTarget>(TTarget target, string propertyName, object? value)
        {
            if (target is null || string.IsNullOrWhiteSpace(propertyName)) return;
            var p = ResolveProperty(typeof(TTarget), propertyName);
            if (p is null || !p.CanWrite) return;

            try
            {
                if (value == null || value is DBNull)
                {
                    if (!p.PropertyType.IsValueType || Nullable.GetUnderlyingType(p.PropertyType) != null)
                        p.SetValue(target, null);
                    return;
                }

                var dest = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

                if (dest == typeof(DateTime) && value is not DateTime &&
                    DateTime.TryParse(value.ToString(), out var dt))
                {
                    p.SetValue(target, dt);
                    return;
                }

                if (dest == typeof(bool) && value is not bool)
                {
                    var s = value.ToString()?.Trim();
                    if (string.Equals(s, "1", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(s, "yes", StringComparison.OrdinalIgnoreCase))
                    {
                        p.SetValue(target, true);
                        return;
                    }
                    if (string.Equals(s, "0", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(s, "false", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(s, "no", StringComparison.OrdinalIgnoreCase))
                    {
                        p.SetValue(target, false);
                        return;
                    }
                }

                p.SetValue(target, value is IConvertible ? Convert.ChangeType(value, dest) : value);
            }
            catch
            {
                // schema-tolerant: ignore conversion failures
            }
        }
    }
}
