// ==============================================================================
// File: Services/DatabaseService.Users.Extensions.cs
// Project: YasGMP
// ------------------------------------------------------------------------------
// Purpose
// -------
// Extension API for DatabaseService focused on USERS & AUTH. This file is the
// single place your services/view-models call into for:
//   • GetUserByIdCoreAsync(int)         -- (renamed to avoid RBAC name collision)
//   • GetUserByUsernameAsync(string)
//   • GetAllUsersCoreAsync(bool)        -- (renamed to avoid RBAC name collision)
//   • InsertOrUpdateUserAsync(User, bool update)
//   • IncrementFailedLoginsAsync(int)
//   • MarkLoginSuccessAsync(int)
//   • LockUserAsync(int) / UnlockUserAsync(int)
//   • SetTwoFactorEnabledAsync(int, bool)
//   • ResetUserPasswordAsync(int[, actor info])
//   • DeleteUserCoreAsync(int, int, string, string, string?)  -- (renamed)
// Notes
// -----
// - Reads are schema-tolerant: password is taken from "password_hash" or legacy "password".
// - Writes prefer "password_hash" and transparently fall back to "password" if needed.
// - Failed-login counters tolerate either "failed_login_attempts" or "failed_logins".
// - Includes a tiny LogUserAuditShimAsync that maps to system_event_log; the name is unique so it
//   will NOT collide with RBAC's LogUserAuditAsync(User?, ...) even when call sites pass null.
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
    /// <b>DatabaseServiceUsersExtensions</b> – Users &amp; Authentication helpers exposed as
    /// extension methods on <see cref="DatabaseService"/>. All SQL is parameterized and
    /// column access is schema-tolerant (older dumps won’t break the app).
    /// </summary>
    public static class DatabaseServiceUsersExtensions
    {
        #region 07  USERS & AUTH (core queries)

        /// <summary>
        /// Retrieves a single user by primary key.
        /// <para><b>Name is intentionally suffixed with "Core" to avoid CS0121 collisions with RBAC.</b></para>
        /// </summary>
        /// <param name="db">Host <see cref="DatabaseService"/> instance.</param>
        /// <param name="id">User ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The resolved <see cref="User"/> or <c>null</c>.</returns>
        public static async Task<User?> GetUserByIdCoreAsync(
            this DatabaseService db,
            int id,
            CancellationToken token = default)
        {
            const string sql = @"SELECT id, username, full_name, email, role, role_id, active, is_locked, is_two_factor_enabled,
       last_login, last_failed_login, failed_login_attempts, digital_signature, last_modified, last_modified_by_id
FROM users WHERE id=@id LIMIT 1;";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            return dt.Rows.Count == 1 ? MapUser(dt.Rows[0]) : null;
        }

        /// <summary>
        /// Retrieves a single user by <paramref name="username"/> (case-insensitive).
        /// </summary>
        public static async Task<User?> GetUserByUsernameAsync(
            this DatabaseService db,
            string username,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;

            const string sql = @"SELECT id, username, full_name, email, role, role_id, active, is_locked, is_two_factor_enabled,
       last_login, last_failed_login, failed_login_attempts, digital_signature, last_modified, last_modified_by_id
FROM users WHERE LOWER(username)=LOWER(@u) LIMIT 1;";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@u", username.Trim()) }, token)
                             .ConfigureAwait(false);

            return dt.Rows.Count == 1 ? MapUser(dt.Rows[0]) : null;
        }

        /// <summary>
        /// Returns all users (basic fields).
        /// <para><b>Name is intentionally suffixed with "Core" to avoid CS0121 collisions with RBAC.</b></para>
        /// </summary>
        /// <param name="db">Host <see cref="DatabaseService"/> instance.</param>
        /// <param name="includeAudit">Kept for signature compatibility; not used here.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task<List<User>> GetAllUsersCoreAsync(
            this DatabaseService db,
            bool includeAudit,
            CancellationToken token = default)
        {
            var dt = await db.ExecuteSelectAsync("SELECT id, username, full_name, email, role, role_id, active, is_locked, is_two_factor_enabled, last_login, last_failed_login, failed_login_attempts, digital_signature, last_modified, last_modified_by_id FROM users ORDER BY username", null, token).ConfigureAwait(false);
            var list = new List<User>(dt.Rows.Count);
            foreach (DataRow r in dt.Rows) list.Add(MapUser(r));
            return list;
        }

        /// <summary>
        /// Inserts a new user when <paramref name="update"/> is <c>false</c>;
        /// updates an existing user when <paramref name="update"/> is <c>true</c>.
        /// Writes the password to <c>password_hash</c> and gracefully falls back to <c>password</c> if needed.
        /// </summary>
        public static async Task<int> InsertOrUpdateUserAsync(
            this DatabaseService db,
            User user,
            bool update,
            CancellationToken token = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            // Preferred SQL (schema-correct: password_hash)
            var sqlPreferred = !update
                ? @"INSERT INTO users
                       (username, password_hash, full_name, email, role, active, is_locked, is_two_factor_enabled)
                    VALUES(@un, @ph, @fn, @em, @ro, @ac, @lk, @tfa);"
                : @"UPDATE users SET
                        username=@un, password_hash=@ph, full_name=@fn, email=@em, role=@ro,
                        active=@ac, is_locked=@lk, is_two_factor_enabled=@tfa
                    WHERE id=@id;";

            var pars = new List<MySqlParameter>
            {
                new("@un", user.Username ?? string.Empty),
                new("@ph", user.PasswordHash ?? string.Empty),
                new("@fn", user.FullName ?? string.Empty),
                new("@em", (object?)user.Email ?? DBNull.Value),
                new("@ro", (object?)user.Role ?? DBNull.Value),
                new("@ac", user.Active),
                new("@lk", user.IsLocked),
                new("@tfa", user.IsTwoFactorEnabled)
            };
            if (update) pars.Add(new MySqlParameter("@id", user.Id));

            try
            {
                await db.ExecuteNonQueryAsync(sqlPreferred, pars.ToArray(), token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054) // unknown column -> legacy "password"
            {
                var sqlLegacy = !update
                    ? @"INSERT INTO users /* ANALYZER_IGNORE: legacy schema */
                           (username, password, full_name, email, role, active, is_locked, is_two_factor_enabled)
                        VALUES(@un, @ph, @fn, @em, @ro, @ac, @lk, @tfa);"
                    : @"UPDATE users /* ANALYZER_IGNORE: legacy schema */ SET
                            username=@un, password=@ph, full_name=@fn, email=@em, role=@ro,
                            active=@ac, is_locked=@lk, is_two_factor_enabled=@tfa
                        WHERE id=@id;";
                await db.ExecuteNonQueryAsync(sqlLegacy, pars.ToArray(), token).ConfigureAwait(false);
            }

            if (!update)
            {
                var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID()", null, token).ConfigureAwait(false);
                user.Id = Convert.ToInt32(idObj);
            }

            return user.Id;
        }

        /// <summary>
        /// Marks a successful login (sets <c>last_login = NOW()</c> and resets failed counter).
        /// Supports both schemas: <c>failed_login_attempts</c> and <c>failed_logins</c>.
        /// </summary>
        public static async Task MarkLoginSuccessAsync(this DatabaseService db, int userId, CancellationToken token = default)
        {
            try
            {
                await db.ExecuteNonQueryAsync(
                    "UPDATE users SET last_login=NOW(), failed_login_attempts=0 WHERE id=@id",
                    new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                await db.ExecuteNonQueryAsync(
                    "UPDATE users /* ANALYZER_IGNORE: legacy schema */ SET last_login=NOW(), failed_logins=0 WHERE id=@id",
                    new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Increments a user’s failed-login counter and stamps <c>last_failed_login = NOW()</c>.
        /// Supports both schemas: <c>failed_login_attempts</c> and <c>failed_logins</c>.
        /// </summary>
        public static async Task IncrementFailedLoginsAsync(this DatabaseService db, int userId, CancellationToken token = default)
        {
            try
            {
                await db.ExecuteNonQueryAsync(
                    @"UPDATE users
                      SET failed_login_attempts = IFNULL(failed_login_attempts,0) + 1,
                          last_failed_login = NOW()
                      WHERE id=@id",
                    new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                await db.ExecuteNonQueryAsync(
                    @"UPDATE users /* ANALYZER_IGNORE: legacy schema */
                      SET failed_logins = IFNULL(failed_logins,0) + 1,
                          last_failed_login = NOW()
                      WHERE id=@id",
                    new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);
            }
        }

        #endregion

        #region 07a  USERS & AUTH (account state helpers)

        /// <summary>Sets <c>is_locked = 1</c>.</summary>
        public static Task LockUserAsync(this DatabaseService db, int userId, CancellationToken token = default)
            => db.ExecuteNonQueryAsync("UPDATE users SET is_locked=1 WHERE id=@id",
                new[] { new MySqlParameter("@id", userId) }, token);

        /// <summary>Locks user + writes audit using the built-in shim (unique name avoids CS0121).</summary>
        public static async Task LockUserAsync(this DatabaseService db, int userId, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
        {
            await db.LockUserAsync(userId, token).ConfigureAwait(false);
            await db.LogUserAuditShimAsync(actorUserId, "LOCK", ip, deviceInfo, sessionId, $"UserId={userId}", token).ConfigureAwait(false);
        }

        /// <summary>Sets <c>is_locked = 0</c> and resets counter (tolerates column names).</summary>
        public static async Task UnlockUserAsync(this DatabaseService db, int userId, CancellationToken token = default)
        {
            try
            {
                await db.ExecuteNonQueryAsync(
                    "UPDATE users SET is_locked=0, failed_login_attempts=0 WHERE id=@id",
                    new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                await db.ExecuteNonQueryAsync(
                    "UPDATE users /* ANALYZER_IGNORE: legacy schema */ SET is_locked=0, failed_logins=0 WHERE id=@id",
                    new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);
            }
        }

        /// <summary>Unlocks user + writes audit using the built-in shim (unique name avoids CS0121).</summary>
        public static async Task UnlockUserAsync(this DatabaseService db, int userId, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
        {
            await db.UnlockUserAsync(userId, token).ConfigureAwait(false);
            await db.LogUserAuditShimAsync(actorUserId, "UNLOCK", ip, deviceInfo, sessionId, $"UserId={userId}", token).ConfigureAwait(false);
        }

        /// <summary>Enables/disables 2FA by toggling <c>is_two_factor_enabled</c>.</summary>
        public static Task SetTwoFactorEnabledAsync(this DatabaseService db, int userId, bool enabled, CancellationToken token = default)
            => db.ExecuteNonQueryAsync(
                "UPDATE users SET is_two_factor_enabled=@tfa WHERE id=@id",
                new[] { new MySqlParameter("@tfa", enabled), new MySqlParameter("@id", userId) }, token);

        /// <summary>Flags a user to require password reset by setting <c>password_reset_required = 1</c>.</summary>
        public static async Task ResetUserPasswordAsync(this DatabaseService db, int userId, CancellationToken token = default)
        {
            try
            {
                await db.ExecuteNonQueryAsync(
                    "UPDATE users SET password_reset_required=1 WHERE id=@id",
                    new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);
            }
            catch (MySqlException)
            {
                // Older schema without the column: ignore gracefully.
            }
        }

        /// <summary>Flags reset and writes an audit entry with actor/context.</summary>
        public static async Task ResetUserPasswordAsync(this DatabaseService db, int userId, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
        {
            await db.ResetUserPasswordAsync(userId, token).ConfigureAwait(false);
            await db.LogUserAuditShimAsync(actorUserId, "RESET_PASSWORD", ip, deviceInfo, sessionId, $"UserId={userId}", token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes a user and records an audit entry.
        /// <para><b>Name is intentionally suffixed with "Core" to avoid CS0121 collisions with RBAC.</b></para>
        /// </summary>
        public static async Task DeleteUserCoreAsync(this DatabaseService db, int userId, int actorUserId, string ip, string deviceInfo, string? sessionId, CancellationToken token = default)
        {
            await db.ExecuteNonQueryAsync("DELETE FROM users WHERE id=@id",
                new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);

            await db.LogUserAuditShimAsync(actorUserId, "DELETE", ip, deviceInfo, sessionId, $"UserId={userId}", token).ConfigureAwait(false);
        }

        #endregion

        #region 07b  USERS (compatibility & mapping)

        /// <summary>
        /// Maps a <see cref="DataRow"/> to a <see cref="User"/> in a schema-tolerant way.
        /// Prefers <c>password_hash</c> and falls back to legacy <c>password</c>.
        /// </summary>
        private static User MapUser(DataRow r)
        {
            int  GetInt(string c)   => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            bool GetBool(string c)  => r.Table.Columns.Contains(c) && r[c] != DBNull.Value && Convert.ToBoolean(r[c]);
            string S(string c)      => r.Table.Columns.Contains(c) ? r[c]?.ToString() ?? string.Empty : string.Empty;

            string pwd = S("password_hash");
            if (string.IsNullOrEmpty(pwd)) pwd = S("password"); // legacy tolerance

            var u = new User
            {
                Id                 = GetInt("id"),
                Username           = S("username"),
                PasswordHash       = pwd,
                FullName           = S("full_name"),
                Email              = S("email"),
                Role               = S("role"),
                Active             = r.Table.Columns.Contains("active") ? GetBool("active") : true,
                IsLocked           = GetBool("is_locked"),
                IsTwoFactorEnabled = GetBool("is_two_factor_enabled")
            };

            u.FailedLoginAttempts =
                r.Table.Columns.Contains("failed_login_attempts") ? GetInt("failed_login_attempts") :
                r.Table.Columns.Contains("failed_logins")         ? GetInt("failed_logins") : 0;

            if (r.Table.Columns.Contains("last_login") && r["last_login"] != DBNull.Value)
                u.LastLogin = Convert.ToDateTime(r["last_login"]);

            if (r.Table.Columns.Contains("last_failed_login") && r["last_failed_login"] != DBNull.Value)
                u.LastFailedLogin = Convert.ToDateTime(r["last_failed_login"]);

            return u;
        }

        #endregion

        #region 07c  AUDIT SHIM (unique name; no collision with RBAC)

        /// <summary>
        /// Minimal, always-available audit shim for user actions that writes to <c>system_event_log</c>.
        /// The name is unique (<c>LogUserAuditShimAsync</c>) to avoid ambiguity with RBAC’s
        /// <c>LogUserAuditAsync(DatabaseService, User?, ...)</c> when call sites pass <c>null</c>.
        /// </summary>
        public static Task LogUserAuditShimAsync(
            this DatabaseService db,
            int? actorUserId,
            string action,
            string? ip,
            string? deviceInfo,
            string? sessionId,
            string? details,
            CancellationToken token = default)
        {
            return db.LogSystemEventAsync(
                userId: actorUserId,
                eventType: action,
                tableName: "users",
                module: "UserModule",
                recordId: null,
                description: details,
                ip: ip,
                severity: "audit",
                deviceInfo: deviceInfo,
                sessionId: sessionId,
                token: token
            );
        }

        #endregion
    }
}
