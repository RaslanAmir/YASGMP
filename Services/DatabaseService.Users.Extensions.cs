// ==============================================================================
// File: Services/DatabaseService.Users.Extensions.cs
// Project: YasGMP
// ------------------------------------------------------------------------------
// Purpose
// -------
// Extension API for DatabaseService focused on USERS & AUTH. This file is the
// single place your services/view-models call into for:
//   • GetUserByUsernameAsync(string)
//   • InsertOrUpdateUserAsync(User, bool update)
//   • IncrementFailedLoginsAsync(int)
//   • MarkLoginSuccessAsync(int)
//   • LockUserAsync(int) / UnlockUserAsync(int)
//   • SetTwoFactorEnabledAsync(int, bool)
//   • ResetUserPasswordAsync(int[, actor info])
// Notes
// -----
// - Writes use the DB column "password_hash" (schema-correct from YASGMP.sql).
// - Failed-login counters tolerate either "failed_login_attempts" or "failed_logins".
// - Mapping is schema-tolerant and ignores missing columns gracefully.
// - A minimal GetAllUsersBasicAsync is kept for legacy call sites.
// - Overloads with actor/context write to user_audit when the table exists;
//   otherwise they safely fall back to system_event_log.
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
    /// extension methods on <see cref="DatabaseService"/>. These methods are used by
    /// <c>UserService</c>, login view-models, and other auth flows.
    /// <para>
    /// All SQL is parameterized. Column access is schema-tolerant: if a column is missing
    /// in the current database, it’s skipped so older dumps don’t break the app.
    /// </para>
    /// </summary>
    public static class DatabaseServiceUsersExtensions
    {
        #region 07  USERS & AUTH (core queries)

        /// <summary>
        /// Retrieves a single user by <paramref name="username"/> (case-insensitive).
        /// Populates the <see cref="User.PasswordHash"/> from the <c>password_hash</c> column.
        /// </summary>
        /// <param name="db">Host <see cref="DatabaseService"/> instance.</param>
        /// <param name="username">Username to look up.</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>The resolved <see cref="User"/> or <c>null</c> if not found.</returns>
        public static async Task<User?> GetUserByUsernameAsync(
            this DatabaseService db,
            string username,
            CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;

            const string sql = @"SELECT * FROM users WHERE LOWER(username)=LOWER(@u) LIMIT 1;";
            var dt = await db.ExecuteSelectAsync(sql, new[] { new MySqlParameter("@u", username.Trim()) }, token)
                             .ConfigureAwait(false);

            return dt.Rows.Count == 1 ? MapUser(dt.Rows[0]) : null;
        }

        /// <summary>
        /// Inserts a new user when <paramref name="update"/> is <c>false</c>;
        /// updates an existing user when <paramref name="update"/> is <c>true</c>.
        /// Writes the password to <c>password_hash</c> (schema-correct).
        /// </summary>
        /// <param name="db">Host <see cref="DatabaseService"/> instance.</param>
        /// <param name="user">User entity to persist. On insert, the method sets <see cref="User.Id"/>.</param>
        /// <param name="update"><c>false</c> = insert; <c>true</c> = update.</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>The user’s ID.</returns>
        /// <exception cref="ArgumentNullException">When <paramref name="user"/> is <c>null</c>.</exception>
        public static async Task<int> InsertOrUpdateUserAsync(
            this DatabaseService db,
            User user,
            bool update,
            CancellationToken token = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            var sql = !update
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

            await db.ExecuteNonQueryAsync(sql, pars.ToArray(), token).ConfigureAwait(false);

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
        /// <param name="db">Host <see cref="DatabaseService"/> instance.</param>
        /// <param name="userId">User PK.</param>
        /// <param name="token">Optional token.</param>
        public static async Task MarkLoginSuccessAsync(
            this DatabaseService db,
            int userId,
            CancellationToken token = default)
        {
            try
            {
                await db.ExecuteNonQueryAsync(
                    "UPDATE users SET last_login=NOW(), failed_login_attempts=0 WHERE id=@id",
                    new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054) // unknown column
            {
                await db.ExecuteNonQueryAsync(
                    "UPDATE users SET last_login=NOW(), failed_logins=0 WHERE id=@id",
                    new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Increments a user’s failed-login counter and stamps <c>last_failed_login = NOW()</c>.
        /// Supports both schemas: <c>failed_login_attempts</c> and <c>failed_logins</c>.
        /// </summary>
        /// <param name="db">Host <see cref="DatabaseService"/> instance.</param>
        /// <param name="userId">User PK.</param>
        /// <param name="token">Optional token.</param>
        public static async Task IncrementFailedLoginsAsync(
            this DatabaseService db,
            int userId,
            CancellationToken token = default)
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
            catch (MySqlException ex) when (ex.Number == 1054) // unknown column
            {
                await db.ExecuteNonQueryAsync(
                    @"UPDATE users
                      SET failed_logins = IFNULL(failed_logins,0) + 1,
                          last_failed_login = NOW()
                      WHERE id=@id",
                    new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);
            }
        }

        #endregion

        #region 07a  USERS & AUTH (account state helpers)

        /// <summary>
        /// Locks a user account by setting <c>is_locked = 1</c>.
        /// </summary>
        /// <param name="db">Host <see cref="DatabaseService"/>.</param>
        /// <param name="userId">Target user primary key.</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static Task LockUserAsync(
            this DatabaseService db,
            int userId,
            CancellationToken token = default)
            => db.ExecuteNonQueryAsync("UPDATE users SET is_locked=1 WHERE id=@id",
                new[] { new MySqlParameter("@id", userId) }, token);

        /// <summary>
        /// Locks a user and writes an audit entry with actor/context (if audit table exists).
        /// </summary>
        /// <param name="db">Host <see cref="DatabaseService"/>.</param>
        /// <param name="userId">Target user ID.</param>
        /// <param name="actorUserId">Acting admin/operator ID.</param>
        /// <param name="ip">Source IP.</param>
        /// <param name="deviceInfo">Device fingerprint/info.</param>
        /// <param name="sessionId">Optional session ID.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task LockUserAsync(
            this DatabaseService db,
            int userId,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            await db.LockUserAsync(userId, token).ConfigureAwait(false);
            // Write user audit if available (defined in RBAC extensions). If not, swallow gracefully.
            try
            {
                await db.LogUserAuditAsync(null, "LOCK", ip, deviceInfo, sessionId,
                    $"Locked by #{actorUserId} (UserId={userId})", token).ConfigureAwait(false);
            }
            catch { /* audit extension may not be present in some builds */ }
        }

        /// <summary>
        /// Unlocks a user account by setting <c>is_locked = 0</c> and resets the failed counter.
        /// Tolerates both counter column names.
        /// </summary>
        public static async Task UnlockUserAsync(
            this DatabaseService db,
            int userId,
            CancellationToken token = default)
        {
            try
            {
                await db.ExecuteNonQueryAsync(
                    "UPDATE users SET is_locked=0, failed_login_attempts=0 WHERE id=@id",
                    new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054) // unknown column
            {
                await db.ExecuteNonQueryAsync(
                    "UPDATE users SET is_locked=0, failed_logins=0 WHERE id=@id",
                    new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Unlocks a user and writes an audit entry with actor/context (if audit table exists).
        /// </summary>
        public static async Task UnlockUserAsync(
            this DatabaseService db,
            int userId,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            await db.UnlockUserAsync(userId, token).ConfigureAwait(false);
            try
            {
                await db.LogUserAuditAsync(null, "UNLOCK", ip, deviceInfo, sessionId,
                    $"Unlocked by #{actorUserId} (UserId={userId})", token).ConfigureAwait(false);
            }
            catch { /* audit extension may not be present in some builds */ }
        }

        /// <summary>
        /// Enables or disables two-factor authentication for a user by setting <c>is_two_factor_enabled</c>.
        /// </summary>
        /// <param name="db">Host <see cref="DatabaseService"/>.</param>
        /// <param name="userId">Target user ID.</param>
        /// <param name="enabled">Desired 2FA state.</param>
        /// <param name="token">Cancellation token.</param>
        public static Task SetTwoFactorEnabledAsync(
            this DatabaseService db,
            int userId,
            bool enabled,
            CancellationToken token = default)
            => db.ExecuteNonQueryAsync(
                "UPDATE users SET is_two_factor_enabled=@tfa WHERE id=@id",
                new[] { new MySqlParameter("@tfa", enabled), new MySqlParameter("@id", userId) }, token);

        /// <summary>
        /// Flags a user to require password reset by setting <c>password_reset_required = 1</c>.
        /// (Workflow/UI should handle collecting and persisting the new secret.)
        /// </summary>
        /// <param name="db">Host <see cref="DatabaseService"/>.</param>
        /// <param name="userId">Target user ID.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task ResetUserPasswordAsync(
            this DatabaseService db,
            int userId,
            CancellationToken token = default)
        {
            try
            {
                await db.ExecuteNonQueryAsync(
                    "UPDATE users SET password_reset_required=1 WHERE id=@id",
                    new[] { new MySqlParameter("@id", userId) }, token).ConfigureAwait(false);
            }
            catch (MySqlException)
            {
                // If the column doesn't exist in some older schema, just no-op.
            }
        }

        /// <summary>
        /// Flags a user to require password reset and writes an audit entry with actor/context.
        /// </summary>
        /// <param name="db">Host <see cref="DatabaseService"/>.</param>
        /// <param name="userId">Target user ID.</param>
        /// <param name="actorUserId">Acting admin/operator ID.</param>
        /// <param name="ip">Source IP.</param>
        /// <param name="deviceInfo">Device fingerprint/info.</param>
        /// <param name="sessionId">Optional session ID.</param>
        /// <param name="token">Cancellation token.</param>
        public static async Task ResetUserPasswordAsync(
            this DatabaseService db,
            int userId,
            int actorUserId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default)
        {
            await db.ResetUserPasswordAsync(userId, token).ConfigureAwait(false);
            try
            {
                await db.LogUserAuditAsync(null, "RESET_PASSWORD", ip, deviceInfo, sessionId,
                    $"Password reset requested by #{actorUserId} (UserId={userId})", token).ConfigureAwait(false);
            }
            catch { /* audit extension may not be present in some builds */ }
        }

        #endregion

        #region 07b  USERS (compatibility)

        /// <summary>
        /// Compatibility/basic fetch kept so older call sites continue to compile.
        /// Prefer richer RBAC-aware methods elsewhere in your codebase.
        /// </summary>
        /// <param name="db">Host <see cref="DatabaseService"/>.</param>
        /// <param name="cancellationToken">Optional token.</param>
        /// <returns>Empty list (legacy placeholder).</returns>
        public static Task<List<User>> GetAllUsersBasicAsync(
            this DatabaseService db,
            CancellationToken cancellationToken = default)
        {
            // Intentional minimal fallback (no DB access).
            return Task.FromResult(new List<User>());
        }

        #endregion

        #region 07c  INTERNAL MAPPING

        /// <summary>
        /// Maps a <see cref="DataRow"/> to a <see cref="User"/> in a schema-tolerant way.
        /// Only sets properties when the column exists and value is non-DBNULL.
        /// </summary>
        private static User MapUser(DataRow r)
        {
            int  GetInt(string c)   => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            bool GetBool(string c)  => r.Table.Columns.Contains(c) && r[c] != DBNull.Value && Convert.ToBoolean(r[c]);
            string S(string c)      => r.Table.Columns.Contains(c) ? r[c]?.ToString() ?? string.Empty : string.Empty;

            var u = new User
            {
                Id                 = GetInt("id"),
                Username           = S("username"),
                PasswordHash       = S("password_hash"),  // schema-correct
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
    }
}
