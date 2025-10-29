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

#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
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
            const string sqlPreferred = @"SELECT id, username, password_hash, password, full_name, email, role, role_id, active, is_locked, is_two_factor_enabled,
       last_login, last_failed_login, failed_login_attempts, digital_signature, last_change_signature, last_modified, last_modified_by_id, source_ip, device_info, session_id
FROM users WHERE id=@id LIMIT 1;";
            const string sqlLegacy = @"SELECT id, username, password, full_name, email, role, role_id, active, is_locked, is_two_factor_enabled,
       last_login, last_failed_login, failed_login_attempts, digital_signature, last_modified, last_modified_by_id
FROM users WHERE id=@id LIMIT 1;";

            DataTable dt;
            try
            {
                dt = await db.ExecuteSelectAsync(sqlPreferred, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054) // unknown column -> legacy
            {
                dt = await db.ExecuteSelectAsync(sqlLegacy, new[] { new MySqlParameter("@id", id) }, token).ConfigureAwait(false);
            }

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

            const string sqlPreferred = @"SELECT id, username, password_hash, password, full_name, email, role, role_id, active, is_locked, is_two_factor_enabled,
       last_login, last_failed_login, failed_login_attempts, digital_signature, last_change_signature, last_modified, last_modified_by_id, source_ip, device_info, session_id
FROM users WHERE LOWER(username)=LOWER(@u) LIMIT 1;";
            const string sqlLegacy = @"SELECT id, username, password, full_name, email, role, role_id, active, is_locked, is_two_factor_enabled,
       last_login, last_failed_login, failed_login_attempts, digital_signature, last_modified, last_modified_by_id
FROM users WHERE LOWER(username)=LOWER(@u) LIMIT 1;";

            DataTable dt;
            try
            {
                dt = await db.ExecuteSelectAsync(sqlPreferred, new[] { new MySqlParameter("@u", username.Trim()) }, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054) // unknown column -> legacy
            {
                dt = await db.ExecuteSelectAsync(sqlLegacy, new[] { new MySqlParameter("@u", username.Trim()) }, token).ConfigureAwait(false);
            }

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
            DataTable dt;
            try
            {
                dt = await db.ExecuteSelectAsync(
                    "SELECT id, username, full_name, email, role, role_id, active, is_locked, is_two_factor_enabled, last_login, last_failed_login, failed_login_attempts, digital_signature, last_change_signature, last_modified, last_modified_by_id, source_ip, device_info, session_id FROM users ORDER BY username",
                    null, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                dt = await db.ExecuteSelectAsync(
                    "SELECT id, username, full_name, email, role, role_id, active, is_locked, is_two_factor_enabled, last_login, last_failed_login, failed_login_attempts, digital_signature, last_modified, last_modified_by_id FROM users ORDER BY username",
                    null, token).ConfigureAwait(false);
            }

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

            var parameterValues = new Dictionary<string, object?>
            {
                ["@un"] = user.Username ?? string.Empty,
                ["@ph"] = user.PasswordHash ?? string.Empty,
                ["@fn"] = user.FullName ?? string.Empty,
                ["@em"] = string.IsNullOrWhiteSpace(user.Email) ? DBNull.Value : user.Email!,
                ["@ro"] = string.IsNullOrWhiteSpace(user.Role) ? DBNull.Value : user.Role!,
                ["@ac"] = user.Active,
                ["@lk"] = user.IsLocked,
                ["@tfa"] = user.IsTwoFactorEnabled
            };

            var normalizedSignature = string.IsNullOrWhiteSpace(user.DigitalSignature) ? null : user.DigitalSignature;
            var normalizedLastChange = string.IsNullOrWhiteSpace(user.LastChangeSignature)
                ? normalizedSignature
                : user.LastChangeSignature;

            parameterValues["@sig"] = normalizedSignature ?? (object)DBNull.Value;
            parameterValues["@lsig"] = normalizedLastChange ?? (object)DBNull.Value;
            parameterValues["@lmb"] = user.LastModifiedById.HasValue ? user.LastModifiedById.Value : (object)DBNull.Value;
            parameterValues["@lm"] = user.LastModified == default ? DateTime.UtcNow : user.LastModified;
            parameterValues["@ip"] = string.IsNullOrWhiteSpace(user.SourceIp) ? (object)DBNull.Value : user.SourceIp!;
            parameterValues["@dev"] = string.IsNullOrWhiteSpace(user.DeviceInfo) ? (object)DBNull.Value : user.DeviceInfo!;
            parameterValues["@sid"] = string.IsNullOrWhiteSpace(user.SessionId) ? (object)DBNull.Value : user.SessionId!;

            if (update)
            {
                parameterValues["@id"] = user.Id;
            }

            const string insertFull = @"INSERT INTO users
                       (username, password_hash, full_name, email, role, active, is_locked, is_two_factor_enabled, digital_signature, last_change_signature, last_modified_by_id, last_modified, source_ip, device_info, session_id)
                    VALUES(@un, @ph, @fn, @em, @ro, @ac, @lk, @tfa, @sig, @lsig, @lmb, @lm, @ip, @dev, @sid);";

            const string updateFull = @"UPDATE users SET
                        username=@un, password_hash=@ph, full_name=@fn, email=@em, role=@ro,
                        active=@ac, is_locked=@lk, is_two_factor_enabled=@tfa,
                        digital_signature=@sig, last_change_signature=@lsig,
                        last_modified_by_id=@lmb, last_modified=@lm,
                        source_ip=@ip, device_info=@dev, session_id=@sid
                    WHERE id=@id;";

            const string insertNoContext = @"INSERT INTO users
                       (username, password_hash, full_name, email, role, active, is_locked, is_two_factor_enabled, digital_signature, last_change_signature, last_modified_by_id, last_modified)
                    VALUES(@un, @ph, @fn, @em, @ro, @ac, @lk, @tfa, @sig, @lsig, @lmb, @lm);";

            const string updateNoContext = @"UPDATE users SET
                        username=@un, password_hash=@ph, full_name=@fn, email=@em, role=@ro,
                        active=@ac, is_locked=@lk, is_two_factor_enabled=@tfa,
                        digital_signature=@sig, last_change_signature=@lsig,
                        last_modified_by_id=@lmb, last_modified=@lm
                    WHERE id=@id;";

            const string insertLegacyModern = @"INSERT INTO users
                       (username, password_hash, full_name, email, role, active, is_locked, is_two_factor_enabled)
                    VALUES(@un, @ph, @fn, @em, @ro, @ac, @lk, @tfa);";

            const string updateLegacyModern = @"UPDATE users SET
                        username=@un, password_hash=@ph, full_name=@fn, email=@em, role=@ro,
                        active=@ac, is_locked=@lk, is_two_factor_enabled=@tfa
                    WHERE id=@id;";

            const string insertPasswordFallback = @"INSERT INTO users /* ANALYZER_IGNORE: legacy schema */
                       (username, password, full_name, email, role, active, is_locked, is_two_factor_enabled)
                    VALUES(@un, @ph, @fn, @em, @ro, @ac, @lk, @tfa);";

            const string updatePasswordFallback = @"UPDATE users /* ANALYZER_IGNORE: legacy schema */ SET
                        username=@un, password=@ph, full_name=@fn, email=@em, role=@ro,
                        active=@ac, is_locked=@lk, is_two_factor_enabled=@tfa
                    WHERE id=@id;";

            static MySqlParameter[] BuildParameters(Dictionary<string, object?> values, IReadOnlyCollection<string> names)
            {
                var list = new List<MySqlParameter>(names.Count);
                foreach (var name in names)
                {
                    if (!values.TryGetValue(name, out var val))
                    {
                        continue;
                    }

                    list.Add(new MySqlParameter(name, val ?? DBNull.Value));
                }

                return list.ToArray();
            }

            var attempts = update
                ? new[]
                {
                    (Sql: updateFull, Params: new[] { "@un", "@ph", "@fn", "@em", "@ro", "@ac", "@lk", "@tfa", "@sig", "@lsig", "@lmb", "@lm", "@ip", "@dev", "@sid", "@id" }),
                    (Sql: updateNoContext, Params: new[] { "@un", "@ph", "@fn", "@em", "@ro", "@ac", "@lk", "@tfa", "@sig", "@lsig", "@lmb", "@lm", "@id" }),
                    (Sql: updateLegacyModern, Params: new[] { "@un", "@ph", "@fn", "@em", "@ro", "@ac", "@lk", "@tfa", "@id" }),
                    (Sql: updatePasswordFallback, Params: new[] { "@un", "@ph", "@fn", "@em", "@ro", "@ac", "@lk", "@tfa", "@id" })
                }
                : new[]
                {
                    (Sql: insertFull, Params: new[] { "@un", "@ph", "@fn", "@em", "@ro", "@ac", "@lk", "@tfa", "@sig", "@lsig", "@lmb", "@lm", "@ip", "@dev", "@sid" }),
                    (Sql: insertNoContext, Params: new[] { "@un", "@ph", "@fn", "@em", "@ro", "@ac", "@lk", "@tfa", "@sig", "@lsig", "@lmb", "@lm" }),
                    (Sql: insertLegacyModern, Params: new[] { "@un", "@ph", "@fn", "@em", "@ro", "@ac", "@lk", "@tfa" }),
                    (Sql: insertPasswordFallback, Params: new[] { "@un", "@ph", "@fn", "@em", "@ro", "@ac", "@lk", "@tfa" })
                };

            var executed = false;
            MySqlException? schemaException = null;

            foreach (var attempt in attempts)
            {
                var parameters = BuildParameters(parameterValues, attempt.Params);

                try
                {
                    await db.ExecuteNonQueryAsync(attempt.Sql, parameters, token).ConfigureAwait(false);
                    executed = true;
                    break;
                }
                catch (MySqlException ex) when (ex.Number is 1054 or 1146)
                {
                    schemaException = ex;
                }
            }

            if (!executed)
            {
                if (schemaException != null)
                {
                    throw schemaException;
                }

                throw new InvalidOperationException("InsertOrUpdateUserAsync failed due to an unexpected schema mismatch.");
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

        #region 07a  USERS (impersonation helpers)

        /// <summary>
        /// Inserts an impersonation session row into <c>session_log</c> capturing the actor, target, and context metadata.
        /// </summary>
        public static async Task<int> BeginImpersonationSessionAsync(
            this DatabaseService db,
            int actorUserId,
            int targetUserId,
            DateTime startedAtUtc,
            string? sessionId,
            string? ip,
            string? deviceInfo,
            string reason,
            string? notes,
            int? signatureId,
            string? signatureHash,
            string? signatureMethod,
            string? signatureStatus,
            string? signatureNote,
            CancellationToken token = default)
        {
            if (db is null) throw new ArgumentNullException(nameof(db));
            if (targetUserId <= 0) throw new ArgumentOutOfRangeException(nameof(targetUserId));

            var normalizedReason = string.IsNullOrWhiteSpace(reason) ? "Impersonation" : reason.Trim();
            var normalizedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            var normalizedIp = string.IsNullOrWhiteSpace(ip) ? null : ip.Trim();
            var normalizedDevice = string.IsNullOrWhiteSpace(deviceInfo) ? null : deviceInfo.Trim();
            var normalizedSession = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId.Trim();
            var normalizedSignatureHash = string.IsNullOrWhiteSpace(signatureHash) ? null : signatureHash.Trim();
            var normalizedSignatureMethod = string.IsNullOrWhiteSpace(signatureMethod) ? null : signatureMethod.Trim();
            var normalizedSignatureStatus = string.IsNullOrWhiteSpace(signatureStatus) ? null : signatureStatus.Trim();
            var normalizedSignatureNote = string.IsNullOrWhiteSpace(signatureNote) ? normalizedNotes : signatureNote.Trim();
            var timestamp = startedAtUtc == default ? DateTime.UtcNow : startedAtUtc;

            static MySqlParameter P(string name, object? value)
                => new(name, value ?? DBNull.Value);

            var baseParameters = new List<MySqlParameter>
            {
                P("@target", targetUserId),
                P("@actor", actorUserId <= 0 ? DBNull.Value : actorUserId),
                P("@login", timestamp),
                P("@sessionId", normalizedSession),
                P("@sessionToken", normalizedSession),
                P("@ip", normalizedIp ?? (object)DBNull.Value),
                P("@device", normalizedDevice ?? (object)DBNull.Value),
                P("@reason", normalizedReason),
                P("@note", normalizedNotes ?? (object)DBNull.Value),
                P("@sigId", signatureId.HasValue && signatureId.Value > 0 ? signatureId : DBNull.Value),
                P("@sigHash", normalizedSignatureHash ?? (object)DBNull.Value),
                P("@sigMethod", normalizedSignatureMethod ?? (object)DBNull.Value),
                P("@sigStatus", normalizedSignatureStatus ?? (object)DBNull.Value),
                P("@sigNote", normalizedSignatureNote ?? (object)DBNull.Value),
                P("@created", timestamp),
                P("@updated", timestamp)
            };

            const string sqlFull = @"
INSERT INTO session_log
    (user_id, impersonated_by_id, is_impersonated, login_time, session_id, session_token,
     ip_address, device_info, reason, note, digital_signature_id, digital_signature,
     signature_method, signature_status, signature_note, created_at, updated_at)
VALUES
    (@target, @actor, 1, @login, @sessionId, @sessionToken,
     @ip, @device, @reason, @note, @sigId, @sigHash,
     @sigMethod, @sigStatus, @sigNote, @created, @updated);";

            const string sqlNoSignatureMeta = @"
INSERT INTO session_log
    (user_id, impersonated_by_id, is_impersonated, login_time, session_id, session_token,
     ip_address, device_info, reason, note, digital_signature_id, digital_signature, created_at, updated_at)
VALUES
    (@target, @actor, 1, @login, @sessionId, @sessionToken,
     @ip, @device, @reason, @note, @sigId, @sigHash, @created, @updated);";

            const string sqlLegacy = @"
INSERT INTO session_log
    (user_id, impersonated_by_id, is_impersonated, login_time, session_token,
     ip_address, device_info, reason, note)
VALUES
    (@target, @actor, 1, @login, @sessionToken,
     @ip, @device, @reason, @note);";

            async Task<int> ExecuteAsync(string sql, IEnumerable<MySqlParameter> parameters)
            {
                _ = await db.ExecuteNonQueryAsync(sql, parameters, token).ConfigureAwait(false);
                var idObj = await db.ExecuteScalarAsync("SELECT LAST_INSERT_ID();", null, token).ConfigureAwait(false);
                return Convert.ToInt32(idObj, CultureInfo.InvariantCulture);
            }

            try
            {
                return await ExecuteAsync(sqlFull, baseParameters).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054 || ex.Number == 1146)
            {
                // fall back to schema without signature metadata columns
            }

            var withoutMeta = baseParameters
                .Where(p => p.ParameterName is not ("@sigMethod" or "@sigStatus" or "@sigNote"))
                .ToList();

            try
            {
                return await ExecuteAsync(sqlNoSignatureMeta, withoutMeta).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054 || ex.Number == 1146)
            {
                // fall back to legacy schema lacking session_id and signature columns
            }

            var legacyParameters = baseParameters
                .Where(p => p.ParameterName is "@target" or "@actor" or "@login" or "@sessionToken" or "@ip" or "@device" or "@reason" or "@note")
                .ToList();

            return await ExecuteAsync(sqlLegacy, legacyParameters).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the impersonation session to mark it terminated, capturing logout metadata.
        /// </summary>
        public static async Task<int> EndImpersonationSessionAsync(
            this DatabaseService db,
            int sessionLogId,
            int actorUserId,
            DateTime endedAtUtc,
            string? notes,
            int? signatureId,
            string? signatureHash,
            string? signatureMethod,
            string? signatureStatus,
            string? signatureNote,
            CancellationToken token = default)
        {
            if (db is null) throw new ArgumentNullException(nameof(db));
            if (sessionLogId <= 0) throw new ArgumentOutOfRangeException(nameof(sessionLogId));

            var normalizedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            var normalizedSignatureHash = string.IsNullOrWhiteSpace(signatureHash) ? null : signatureHash.Trim();
            var normalizedSignatureMethod = string.IsNullOrWhiteSpace(signatureMethod) ? null : signatureMethod.Trim();
            var normalizedSignatureStatus = string.IsNullOrWhiteSpace(signatureStatus) ? null : signatureStatus.Trim();
            var normalizedSignatureNote = string.IsNullOrWhiteSpace(signatureNote) ? normalizedNotes : signatureNote.Trim();
            var timestamp = endedAtUtc == default ? DateTime.UtcNow : endedAtUtc;

            static MySqlParameter P(string name, object? value)
                => new(name, value ?? DBNull.Value);

            var baseParameters = new List<MySqlParameter>
            {
                P("@id", sessionLogId),
                P("@actor", actorUserId <= 0 ? DBNull.Value : actorUserId),
                P("@logout", timestamp),
                P("@note", normalizedNotes ?? (object)DBNull.Value),
                P("@sigId", signatureId.HasValue && signatureId.Value > 0 ? signatureId : DBNull.Value),
                P("@sigHash", normalizedSignatureHash ?? (object)DBNull.Value),
                P("@sigMethod", normalizedSignatureMethod ?? (object)DBNull.Value),
                P("@sigStatus", normalizedSignatureStatus ?? (object)DBNull.Value),
                P("@sigNote", normalizedSignatureNote ?? (object)DBNull.Value),
                P("@updated", timestamp)
            };

            const string sqlFull = @"
UPDATE session_log
SET logout_time=@logout,
    logout_at=@logout,
    updated_at=@updated,
    is_terminated=1,
    terminated_by=@actor,
    note=COALESCE(@note, note),
    digital_signature_id=@sigId,
    digital_signature=@sigHash,
    signature_method=@sigMethod,
    signature_status=@sigStatus,
    signature_note=@sigNote
WHERE id=@id;";

            const string sqlNoSignatureMeta = @"
UPDATE session_log
SET logout_time=@logout,
    logout_at=@logout,
    updated_at=@updated,
    is_terminated=1,
    terminated_by=@actor,
    note=COALESCE(@note, note),
    digital_signature_id=@sigId,
    digital_signature=@sigHash
WHERE id=@id;";

            const string sqlLegacy = @"
UPDATE session_log
SET logout_time=@logout,
    is_terminated=1,
    terminated_by=@actor,
    note=COALESCE(@note, note)
WHERE id=@id;";

            try
            {
                _ = await db.ExecuteNonQueryAsync(sqlFull, baseParameters, token).ConfigureAwait(false);
            }
            catch (MySqlException ex) when (ex.Number == 1054 || ex.Number == 1146)
            {
                var withoutMeta = baseParameters
                    .Where(p => p.ParameterName is not ("@sigMethod" or "@sigStatus" or "@sigNote"))
                    .ToList();

                try
                {
                    _ = await db.ExecuteNonQueryAsync(sqlNoSignatureMeta, withoutMeta, token).ConfigureAwait(false);
                }
                catch (MySqlException inner) when (inner.Number == 1054 || inner.Number == 1146)
                {
                    var legacyParameters = baseParameters
                        .Where(p => p.ParameterName is "@id" or "@actor" or "@logout" or "@note")
                        .ToList();
                    _ = await db.ExecuteNonQueryAsync(sqlLegacy, legacyParameters, token).ConfigureAwait(false);
                }
            }

            return sessionLogId;
        }

        #endregion

        #region 07b  USERS (compatibility & mapping)

        /// <summary>
        /// Maps a <see cref="DataRow"/> to a <see cref="User"/> in a schema-tolerant way.
        /// Prefers <c>password_hash</c> and falls back to legacy <c>password</c>.
        /// </summary>
        private static User MapUser(DataRow r)
        {
            int GetInt(string c)   => r.Table.Columns.Contains(c) && r[c] != DBNull.Value ? Convert.ToInt32(r[c]) : 0;
            bool GetBool(string c) => r.Table.Columns.Contains(c) && r[c] != DBNull.Value && Convert.ToBoolean(r[c]);
            string S(string c)     => r.Table.Columns.Contains(c) ? r[c]?.ToString() ?? string.Empty : string.Empty;

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
                IsTwoFactorEnabled = GetBool("is_two_factor_enabled"),
                DigitalSignature   = S("digital_signature"),
                LastChangeSignature = S("last_change_signature"),
                DeviceInfo         = S("device_info"),
                SourceIp           = S("source_ip"),
                SessionId          = S("session_id")
            };

            u.FailedLoginAttempts =
                r.Table.Columns.Contains("failed_login_attempts") ? GetInt("failed_login_attempts") :
                r.Table.Columns.Contains("failed_logins")         ? GetInt("failed_logins") : 0;

            if (r.Table.Columns.Contains("last_login") && r["last_login"] != DBNull.Value)
                u.LastLogin = Convert.ToDateTime(r["last_login"]);

            if (r.Table.Columns.Contains("last_failed_login") && r["last_failed_login"] != DBNull.Value)
                u.LastFailedLogin = Convert.ToDateTime(r["last_failed_login"]);

            if (r.Table.Columns.Contains("last_modified") && r["last_modified"] != DBNull.Value)
                u.LastModified = Convert.ToDateTime(r["last_modified"]);

            if (r.Table.Columns.Contains("last_modified_by_id") && r["last_modified_by_id"] != DBNull.Value)
                u.LastModifiedById = Convert.ToInt32(r["last_modified_by_id"]);

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
