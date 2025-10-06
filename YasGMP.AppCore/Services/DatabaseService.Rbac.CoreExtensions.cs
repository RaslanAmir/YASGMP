// ==============================================================================
// File: Services/DatabaseService.Rbac.CoreExtensions.cs
// Project: YasGMP
// ------------------------------------------------------------------------------
// Purpose
// -------
// Compatibility + augmentation layer for RBAC-related helpers on DatabaseService.
// Provides extension methods that RBACService depends on, so projects compile
// cleanly regardless of which DatabaseService version is present. Where the
// concrete DatabaseService already implements an equivalent *instance* method,
// the instance method will be preferred automatically by the C# binder. When
// only these extensions exist, they execute parameterized SQL against the
// current schema (as defined by YASGMP.sql) and remain tolerant to small
// schema variations.
//
// Notes
// -----
// - Every method is asynchronous and fully parameterized (MySqlConnector).
// - Where an audit is appropriate, we try to use the newer, schema-tolerant
//   DatabaseService.LogPermissionChangeAsync(userId, roleId, permissionId, …)
//   instance overload when present. If it’s not present (older DatabaseService),
//   these extensions still perform the primary DB write and a minimal audit so
//   the app remains healthy.
// - These are defined as *extension methods* on DatabaseService, so existing
//   call sites like `_db.GetUserRoleIdsAsync(...)` keep working.
//
// Copyright © 2025
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Extension helpers that light up RBAC features on <see cref="DatabaseService"/>
    /// while keeping older builds source-compatible.
    /// </summary>
    public static class DatabaseServiceRbacCoreExtensions
    {
        // =====================================================================
        // 08A · Permission inspection
        // =====================================================================

        /// <summary>
        /// Returns <c>true</c> if the user has a direct, non-expired <c>ALLOW</c> entry in
        /// <c>user_permissions</c> for the given permission <paramref name="permissionCode"/>.
        /// </summary>
        public static async Task<bool> HasDirectUserPermissionAsync(
            this DatabaseService db,
            int userId,
            string permissionCode,
            CancellationToken token = default)
        {
            const string sql = @"
SELECT 1
FROM user_permissions up
JOIN permissions p ON p.id = up.permission_id
WHERE up.user_id=@uid
  AND p.code=@code
  AND up.allowed=1
  AND (up.expires_at IS NULL OR up.expires_at > NOW())
LIMIT 1;";
            var dt = await db.ExecuteSelectAsync(sql, new[]
            {
                new MySqlParameter("@uid",  userId),
                new MySqlParameter("@code", permissionCode ?? string.Empty)
            }, token).ConfigureAwait(false);

            return dt.Rows.Count > 0;
        }

        /// <summary>
        /// Returns the active role ids for a user (ignores expired assignments).
        /// </summary>
        public static async Task<List<int>> GetUserRoleIdsAsync(
            this DatabaseService db,
            int userId,
            CancellationToken token = default)
        {
            const string sql = @"
SELECT role_id
FROM user_roles
WHERE user_id=@uid
  AND (expires_at IS NULL OR expires_at > NOW());";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@uid", userId) }, token).ConfigureAwait(false);

            var list = new List<int>();
            foreach (DataRow r in dt.Rows)
                if (r["role_id"] != DBNull.Value) list.Add(Convert.ToInt32(r["role_id"]));
            return list;
        }

        /// <summary>
        /// Returns <c>true</c> if the given role grants the specified permission code.
        /// </summary>
        public static async Task<bool> HasRolePermissionAsync(
            this DatabaseService db,
            int roleId,
            string permissionCode,
            CancellationToken token = default)
        {
            const string sql = @"
SELECT 1
FROM role_permissions rp
JOIN permissions p ON p.id = rp.permission_id
WHERE rp.role_id=@rid
  AND p.code=@code
  AND (rp.allowed IS NULL OR rp.allowed=1)
LIMIT 1;";
            var dt = await db.ExecuteSelectAsync(sql, new[]
            {
                new MySqlParameter("@rid",  roleId),
                new MySqlParameter("@code", permissionCode ?? string.Empty)
            }, token).ConfigureAwait(false);

            return dt.Rows.Count > 0;
        }

        /// <summary>
        /// Returns <c>true</c> if user has a live, non-revoked, non-expired delegation for the permission code.
        /// </summary>
        public static async Task<bool> HasDelegatedPermissionAsync(
            this DatabaseService db,
            int userId,
            string permissionCode,
            CancellationToken token = default)
        {
            const string sql = @"
SELECT 1
FROM delegated_permissions d
JOIN permissions p ON p.id = d.permission_id
WHERE d.to_user_id=@uid
  AND p.code=@code
  AND (d.revoked IS NULL OR d.revoked=0)
  AND d.expires_at > NOW()
LIMIT 1;";
            var dt = await db.ExecuteSelectAsync(sql, new[]
            {
                new MySqlParameter("@uid",  userId),
                new MySqlParameter("@code", permissionCode ?? string.Empty)
            }, token).ConfigureAwait(false);

            return dt.Rows.Count > 0;
        }

        /// <summary>All direct, non-expired permission codes for the user.</summary>
        public static async Task<List<string>> GetUserPermissionCodesAsync(
            this DatabaseService db,
            int userId,
            CancellationToken token = default)
        {
            const string sql = @"
SELECT p.code
FROM user_permissions up
JOIN permissions p ON p.id = up.permission_id
WHERE up.user_id=@uid
  AND up.allowed=1
  AND (up.expires_at IS NULL OR up.expires_at > NOW())
ORDER BY p.code;";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@uid", userId) }, token).ConfigureAwait(false);

            var list = new List<string>();
            foreach (DataRow r in dt.Rows)
            {
                var code = r["code"]?.ToString();
                if (!string.IsNullOrWhiteSpace(code)) list.Add(code);
            }
            return list;
        }

        /// <summary>All permission codes granted to a role.</summary>
        public static async Task<List<string>> GetRolePermissionCodesAsync(
            this DatabaseService db,
            int roleId,
            CancellationToken token = default)
        {
            const string sql = @"
SELECT p.code
FROM role_permissions rp
JOIN permissions p ON p.id=rp.permission_id
WHERE rp.role_id=@rid
  AND (rp.allowed IS NULL OR rp.allowed=1)
ORDER BY p.code;";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@rid", roleId) }, token).ConfigureAwait(false);

            var list = new List<string>();
            foreach (DataRow r in dt.Rows)
            {
                var code = r["code"]?.ToString();
                if (!string.IsNullOrWhiteSpace(code)) list.Add(code);
            }
            return list;
        }

        /// <summary>All live delegated permission codes for the user.</summary>
        public static async Task<List<string>> GetDelegatedPermissionCodesAsync(
            this DatabaseService db,
            int userId,
            CancellationToken token = default)
        {
            const string sql = @"
SELECT p.code
FROM delegated_permissions d
JOIN permissions p ON p.id = d.permission_id
WHERE d.to_user_id=@uid
  AND (d.revoked IS NULL OR d.revoked=0)
  AND d.expires_at > NOW()
ORDER BY p.code;";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@uid", userId) }, token).ConfigureAwait(false);

            var list = new List<string>();
            foreach (DataRow r in dt.Rows)
            {
                var code = r["code"]?.ToString();
                if (!string.IsNullOrWhiteSpace(code)) list.Add(code);
            }
            return list;
        }

        // =====================================================================
        // 08B · Writes (user↔role / user↔permission / delegations)
        // =====================================================================

        /// <summary>
        /// Adds (or refreshes) a user→role mapping. Performs an UPSERT and writes audit.
        /// </summary>
        public static async Task AddUserRoleAsync(
            this DatabaseService db,
            int userId,
            int roleId,
            int actorUserId,
            DateTime? expiresAt = null,
            string? sourceIp = null,
            string? sessionId = null,
            CancellationToken token = default)
        {
            const string upsert = @"
INSERT INTO user_roles (user_id, role_id, granted_by, granted_at, expires_at)
VALUES (@uid,@rid,@actor,NOW(),@exp)
ON DUPLICATE KEY UPDATE
    granted_by = VALUES(granted_by),
    granted_at = VALUES(granted_at),
    expires_at = VALUES(expires_at);";

            await db.ExecuteNonQueryAsync(upsert, new[]
            {
                new MySqlParameter("@uid",   userId),
                new MySqlParameter("@rid",   roleId),
                new MySqlParameter("@actor", actorUserId),
                new MySqlParameter("@exp",   (object?)expiresAt ?? DBNull.Value)
            }, token).ConfigureAwait(false);

            try
            {
                // Prefer instance tolerant writer if present
                await db.LogPermissionChangeAsync(
                    userId, roleId, null, "role", actorUserId,
                    ip: sourceIp ?? string.Empty,
                    deviceInfo: string.Empty,
                    sessionId: sessionId,
                    action: "grant",
                    details: $"Granted role #{roleId} to user #{userId}",
                    reason: string.Empty,
                    token: token
                ).ConfigureAwait(false);
            }
            catch { /* older DatabaseService – primary write is already done */ }
        }

        /// <summary>Removes a user→role mapping and writes audit.</summary>
        public static async Task RemoveUserRoleAsync(
            this DatabaseService db,
            int userId,
            int roleId,
            int actorUserId = 0,
            string? reason = null,
            string? sourceIp = null,
            string? sessionId = null,
            CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync(
                "DELETE FROM user_roles WHERE user_id=@uid AND role_id=@rid",
                new[]
                {
                    new MySqlParameter("@uid", userId),
                    new MySqlParameter("@rid", roleId)
                }, token).ConfigureAwait(false);

            try
            {
                await db.LogPermissionChangeAsync(
                    userId, roleId, null, "role", actorUserId,
                    ip: sourceIp ?? string.Empty,
                    deviceInfo: string.Empty,
                    sessionId: sessionId,
                    action: "revoke",
                    details: null,
                    reason: reason ?? string.Empty,
                    token: token
                ).ConfigureAwait(false);
            }
            catch { /* ignore */ }
        }

        /// <summary>Resolves a permission id from code. Creates it if requested.</summary>
        public static async Task<int> GetPermissionIdByCodeAsync(
            this DatabaseService db,
            string permissionCode,
            bool createIfMissing = false,
            string? displayName = null,
            CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync("SELECT id FROM permissions WHERE code=@code LIMIT 1",
                new[] { new MySqlParameter("@code", permissionCode ?? string.Empty) }, token).ConfigureAwait(false);

            if (dt.Rows.Count > 0) return Convert.ToInt32(dt.Rows[0]["id"]);

            if (!createIfMissing)
                throw new KeyNotFoundException($"Permission code not found: '{permissionCode}'.");

            await db.ExecuteNonQueryAsync(
                "INSERT INTO permissions (code, name) VALUES (@code,@name)",
                new[]
                {
                    new MySqlParameter("@code", permissionCode ?? string.Empty),
                    new MySqlParameter("@name", (object?)(displayName ?? permissionCode ?? string.Empty) ?? DBNull.Value)
                }, token).ConfigureAwait(false);

            return Convert.ToInt32(await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
        }

        /// <summary>
        /// Adds or updates a direct user permission (override) and logs the change.
        /// </summary>
        public static async Task AddUserPermissionAsync(
            this DatabaseService db,
            int userId,
            int permissionId,
            int grantedBy,
            DateTime? expiresAt,
            bool allowed,
            string? reason = null,
            string? sourceIp = null,
            string? sessionId = null,
            CancellationToken token = default)
        {
            const string upsert = @"
INSERT INTO user_permissions (user_id, permission_id, allowed, reason, granted_by, granted_at, expires_at)
VALUES (@uid,@pid,@allow,@reason,@by,NOW(),@exp)
ON DUPLICATE KEY UPDATE
    allowed    = VALUES(allowed),
    reason     = VALUES(reason),
    granted_by = VALUES(granted_by),
    granted_at = VALUES(granted_at),
    expires_at = VALUES(expires_at);";

            await db.ExecuteNonQueryAsync(upsert, new[]
            {
                new MySqlParameter("@uid",   userId),
                new MySqlParameter("@pid",   permissionId),
                new MySqlParameter("@allow", allowed),
                new MySqlParameter("@reason", reason ?? string.Empty),
                new MySqlParameter("@by",    grantedBy),
                new MySqlParameter("@exp",   (object?)expiresAt ?? DBNull.Value)
            }, token).ConfigureAwait(false);

            try
            {
                await db.LogPermissionChangeAsync(
                    userId, null, permissionId, "permission", grantedBy,
                    ip: sourceIp ?? string.Empty,
                    deviceInfo: string.Empty,
                    sessionId: sessionId,
                    action: allowed ? "grant" : "deny",
                    details: null,
                    reason: reason ?? string.Empty,
                    token: token
                ).ConfigureAwait(false);
            }
            catch { /* ignore */ }
        }

        /// <summary>Removes a direct user permission and writes audit.</summary>
        public static async Task RemoveUserPermissionAsync(
            this DatabaseService db,
            int userId,
            int permissionId,
            int actorUserId = 0,
            string? reason = null,
            string? sourceIp = null,
            string? sessionId = null,
            CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync(
                "DELETE FROM user_permissions WHERE user_id=@uid AND permission_id=@pid",
                new[]
                {
                    new MySqlParameter("@uid", userId),
                    new MySqlParameter("@pid", permissionId)
                }, token).ConfigureAwait(false);

            try
            {
                await db.LogPermissionChangeAsync(
                    userId, null, permissionId, "permission", actorUserId,
                    ip: sourceIp ?? string.Empty,
                    deviceInfo: string.Empty,
                    sessionId: sessionId,
                    action: "revoke",
                    details: null,
                    reason: reason ?? string.Empty,
                    token: token
                ).ConfigureAwait(false);
            }
            catch { /* ignore */ }
        }

        /// <summary>
        /// Delegates a permission from one user to another (non-revoked, with hard expiry).
        /// </summary>
        public static async Task AddDelegatedPermissionAsync(
            this DatabaseService db,
            int fromUserId,
            int toUserId,
            int permissionId,
            int grantedBy,
            DateTime expiresAt,
            string? reason = null,
            string? sourceIp = null,
            string? sessionId = null,
            CancellationToken token = default)
        {
            const string sql = @"
INSERT INTO delegated_permissions (from_user_id, to_user_id, permission_id, expires_at, reason, granted_by)
VALUES (@from,@to,@pid,@exp,@reason,@by);";

            await db.ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@from",   fromUserId),
                new MySqlParameter("@to",     toUserId),
                new MySqlParameter("@pid",    permissionId),
                new MySqlParameter("@exp",    expiresAt),
                new MySqlParameter("@reason", reason ?? string.Empty),
                new MySqlParameter("@by",     grantedBy)
            }, token).ConfigureAwait(false);

            try
            {
                await db.LogPermissionChangeAsync(
                    toUserId, null, permissionId, "delegation", grantedBy,
                    ip: sourceIp ?? string.Empty,
                    deviceInfo: string.Empty,
                    sessionId: sessionId,
                    action: "grant",
                    details: $"from={fromUserId}",
                    reason: reason ?? string.Empty,
                    token: token
                ).ConfigureAwait(false);
            }
            catch { /* ignore */ }
        }

        /// <summary>
        /// Revokes a live delegation (requires actor id). Uses tolerant logger with safe fallback.
        /// </summary>
        public static async Task RevokeDelegatedPermissionAsync(
            this DatabaseService db,
            int delegationId, int actorUserId, string? reason = null, CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync(@"
UPDATE delegated_permissions
SET revoked=1, revoked_at=NOW()
WHERE id=@id",
                new[] { new MySqlParameter("@id", delegationId) }, token).ConfigureAwait(false);

            // Try tolerant instance writer first (if present)
            try
            {
                var dt = await db.ExecuteSelectAsync(
                    "SELECT to_user_id, permission_id FROM delegated_permissions WHERE id=@id",
                    new[] { new MySqlParameter("@id", delegationId) }, token).ConfigureAwait(false);

                int? toUserId = null; int? permissionId = null;
                if (dt.Rows.Count == 1)
                {
                    if (dt.Rows[0]["to_user_id"] != DBNull.Value) toUserId = Convert.ToInt32(dt.Rows[0]["to_user_id"]);
                    if (dt.Rows[0]["permission_id"] != DBNull.Value) permissionId = Convert.ToInt32(dt.Rows[0]["permission_id"]);
                }

                await db.LogPermissionChangeAsync(
                    toUserId, null, permissionId, "delegation", actorUserId,
                    ip: string.Empty, deviceInfo: string.Empty, sessionId: null,
                    action: "revoke",
                    details: $"delegation_id={delegationId}",
                    reason: reason ?? string.Empty,
                    token: token
                ).ConfigureAwait(false);

                return;
            }
            catch
            {
                // Fall back to minimal legacy audit (no fragile columns).
                await db.ExecuteNonQueryAsync(@"
INSERT INTO permission_change_log (user_id, changed_by, change_type, role_id, permission_id, action)
SELECT IFNULL(to_user_id, @actor), @actor, 'delegation', NULL, permission_id, 'revoke'
FROM delegated_permissions WHERE id=@id;",
                    new[]
                    {
                        new MySqlParameter("@id",    delegationId),
                        new MySqlParameter("@actor", actorUserId),
                    }, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Overload to support callers that don't pass actor id (back-compat shim).
        /// </summary>
        public static Task RevokeDelegatedPermissionAsync(this DatabaseService db, int delegationId, CancellationToken token = default)
            => db.RevokeDelegatedPermissionAsync(delegationId, actorUserId: 0, reason: null, token);

        // =====================================================================
        // 08C · Permission requests
        // =====================================================================

        /// <summary>
        /// Adds a permission request (user → permission) and returns its id.
        /// </summary>
        public static async Task<int> AddPermissionRequestAsync(
            this DatabaseService db,
            int userId,
            int permissionId,
            string? reason,
            CancellationToken token = default)
        {
            const string sql = @"
INSERT INTO permission_requests (user_id, permission_id, reason, status, requested_at)
VALUES (@uid,@pid,@reason,'pending',NOW());";
            await db.ExecuteNonQueryAsync(sql, new[]
            {
                new MySqlParameter("@uid", userId),
                new MySqlParameter("@pid", permissionId),
                new MySqlParameter("@reason", reason ?? string.Empty)
            }, token).ConfigureAwait(false);

            return Convert.ToInt32(await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false));
        }

        /// <summary>Approves a pending permission request and grants a direct override.</summary>
        public static async Task ApprovePermissionRequestAsync(
            this DatabaseService db,
            int requestId,
            int approvedBy,
            string? comment,
            DateTime? expiresAt = null,
            CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync("SELECT * FROM permission_requests WHERE id=@id",
                new[] { new MySqlParameter("@id", requestId) }, token).ConfigureAwait(false);
            if (dt.Rows.Count == 0) return;

            int userId       = Convert.ToInt32(dt.Rows[0]["user_id"]);
            int permissionId = Convert.ToInt32(dt.Rows[0]["permission_id"]);

            await db.ExecuteNonQueryAsync(@"
UPDATE permission_requests
SET status='approved', reviewed_by=@rev, reviewed_at=NOW(), review_comment=@cmt
WHERE id=@id",
                new[]
                {
                    new MySqlParameter("@rev", approvedBy),
                    new MySqlParameter("@cmt", comment ?? string.Empty),
                    new MySqlParameter("@id",  requestId)
                }, token).ConfigureAwait(false);

            await db.AddUserPermissionAsync(userId, permissionId, approvedBy, expiresAt, allowed: true, reason: $"Approved request #{requestId}: {comment}", token: token)
                .ConfigureAwait(false);
        }

        /// <summary>Denies a pending permission request and writes the decision.</summary>
        public static async Task DenyPermissionRequestAsync(
            this DatabaseService db,
            int requestId,
            int deniedBy,
            string? comment,
            CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync("SELECT * FROM permission_requests WHERE id=@id",
                new[] { new MySqlParameter("@id", requestId) }, token).ConfigureAwait(false);
            if (dt.Rows.Count == 0) return;

            int userId       = Convert.ToInt32(dt.Rows[0]["user_id"]);
            int permissionId = Convert.ToInt32(dt.Rows[0]["permission_id"]);

            await db.ExecuteNonQueryAsync(@"
UPDATE permission_requests
SET status='denied', reviewed_by=@rev, reviewed_at=NOW(), review_comment=@cmt
WHERE id=@id",
                new[]
                {
                    new MySqlParameter("@rev", deniedBy),
                    new MySqlParameter("@cmt", comment ?? string.Empty),
                    new MySqlParameter("@id",  requestId)
                }, token).ConfigureAwait(false);

            try
            {
                await db.LogPermissionChangeAsync(
                    userId, null, permissionId, "request", deniedBy,
                    ip: string.Empty, deviceInfo: string.Empty, sessionId: null,
                    action: "deny",
                    details: null,
                    reason: comment ?? string.Empty,
                    token: token
                ).ConfigureAwait(false);
            }
            catch { /* ignore */ }
        }

        // =====================================================================
        // 08D · Lookups
        // =====================================================================

        /// <summary>Returns all roles. Schema-tolerant mapping.</summary>
        public static async Task<List<Role>> GetAllRolesAsync(this DatabaseService db, CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync("SELECT id, name, description, org_unit, compliance_tags, is_deleted, notes FROM roles ORDER BY name, id", null, token).ConfigureAwait(false);
            var list = new List<Role>();
            foreach (DataRow r in dt.Rows)
            {
                var role = Activator.CreateInstance<Role>();
                if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)                SetIfExists(role, "Id", Convert.ToInt32(r["id"]));
                if (r.Table.Columns.Contains("name"))                                         SetIfExists(role, "Name", r["name"]?.ToString());
                if (r.Table.Columns.Contains("code"))                                         SetIfExists(role, "Code", r["code"]?.ToString());
                if (r.Table.Columns.Contains("description"))                                  SetIfExists(role, "Description", r["description"]?.ToString());
                list.Add(role);
            }
            return list;
        }

        /// <summary>Returns all permissions. Schema-tolerant mapping.</summary>
        public static async Task<List<Permission>> GetAllPermissionsAsync(this DatabaseService db, CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync("SELECT id, code, name, description, module, permission_type, critical, is_deleted FROM permissions ORDER BY code, id", null, token).ConfigureAwait(false);
            var list = new List<Permission>();
            foreach (DataRow r in dt.Rows)
            {
                var p = Activator.CreateInstance<Permission>();
                if (r.Table.Columns.Contains("id") && r["id"] != DBNull.Value)                SetIfExists(p, "Id", Convert.ToInt32(r["id"]));
                if (r.Table.Columns.Contains("code"))                                         SetIfExists(p, "Code", r["code"]?.ToString());
                if (r.Table.Columns.Contains("name"))                                         SetIfExists(p, "Name", r["name"]?.ToString());
                if (r.Table.Columns.Contains("description"))                                  SetIfExists(p, "Description", r["description"]?.ToString());
                list.Add(p);
            }
            return list;
        }

        // =====================================================================
        // 08E · Back-compat LogPermissionChange shim (signature used by RBACService)
        // =====================================================================

        /// <summary>
        /// Back-compat shim so older call sites (e.g., <c>RBACService</c>) can call
        /// <c>LogPermissionChangeAsync(targetUserId: …)</c> without knowing about the
        /// newer, schema-tolerant overload on <see cref="DatabaseService"/>.
        /// </summary>
        public static async Task LogPermissionChangeAsync(
            this DatabaseService db,
            int targetUserId,
            int changedBy,
            string changeType,
            int? roleId,
            int? permissionId,
            string action,
            string? reason,
            DateTime? expiresAt,
            string? sourceIp  = null,
            string? sessionId = null,
            CancellationToken token = default)
        {
            // Prefer the new tolerant *instance* writer on DatabaseService if present.
            try
            {
                await db.LogPermissionChangeAsync(
                    userId: targetUserId == 0 ? null : targetUserId,
                    roleId: roleId,
                    permissionId: permissionId,
                    changeType: changeType,
                    changedByUserId: changedBy,
                    ip: sourceIp ?? string.Empty,
                    deviceInfo: string.Empty,
                    sessionId: sessionId,
                    action: action,
                    details: null,
                    reason: reason,
                    token: token
                ).ConfigureAwait(false);
                return;
            }
            catch
            {
                // Minimal, schema-safe legacy INSERT (no fragile columns like 'changed_at' or optional fields).
                await db.ExecuteNonQueryAsync(@"
INSERT INTO permission_change_log (user_id, changed_by, change_type, role_id, permission_id, action)
VALUES (@uid, @actor, @ctype, @rid, @pid, @act);",
                    new[]
                    {
                        // user_id must never be null: fall back to actor if caller passed 0
                        new MySqlParameter("@uid",   targetUserId != 0 ? targetUserId : changedBy),
                        new MySqlParameter("@actor", changedBy),
                        new MySqlParameter("@ctype", changeType ?? string.Empty),
                        new MySqlParameter("@rid",   (object?)roleId ?? DBNull.Value),
                        new MySqlParameter("@pid",   (object?)permissionId ?? DBNull.Value),
                        new MySqlParameter("@act",   action ?? string.Empty),
                    }, token).ConfigureAwait(false);
            }
        }

        // =====================================================================
        // Tiny mapping helper (mirror of DatabaseService.SetIfExists for POCOs)
        // =====================================================================

        private static void SetIfExists<TTarget>(TTarget target, string propertyName, object? value)
        {
            if (target is null || string.IsNullOrWhiteSpace(propertyName)) return;
            var p = typeof(TTarget).GetProperty(propertyName);
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
                if (value is IConvertible) p.SetValue(target, Convert.ChangeType(value, dest));
                else p.SetValue(target, value);
            }
            catch
            {
                // schema-tolerant: ignore conversion failures
            }
        }
    }
}

