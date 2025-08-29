using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>UserService</b> – GMP/21 CFR Part 11–aligned user domain service.
    /// <para>
    /// Responsibilities:
    /// <list type="bullet">
    /// <item><description>Authentication helpers (username/password + optional 2FA verification hook).</description></item>
    /// <item><description>CRUD operations with audit trail mirroring (<see cref="AuditService"/>).</description></item>
    /// <item><description>RBAC guard rails via <see cref="IRBACService"/> on sensitive actions.</description></item>
    /// <item><description>Digital signature helpers (lightweight demo token + validation).</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// 🔐 <b>Audit Guarantee:</b> All login flows call
    /// <see cref="AuditService.LogSystemEventForUserAsync(int?, string, string, string, int?)"/>
    /// so the dashboard's “Korisnik (ID)” column is populated whenever a user can be resolved,
    /// even <i>before</i> <c>App.LoggedUser</c> is set by the UI layer.
    /// </para>
    /// </summary>
    public class UserService : IUserService
    {
        /// <summary>
        /// Low-level database facade for user persistence and auth counters.
        /// </summary>
        private readonly DatabaseService _db;

        /// <summary>
        /// Central audit writer; all user/security actions are mirrored here.
        /// </summary>
        private readonly AuditService _audit;

        /// <summary>
        /// Role-based access control enforcer for administrative operations.
        /// </summary>
        private readonly IRBACService _rbac;

        /// <summary>
        /// Initializes a new instance of <see cref="UserService"/> with required dependencies.
        /// </summary>
        /// <param name="databaseService">Concrete database accessor used for user reads/writes.</param>
        /// <param name="auditService">Audit logger used to persist security/audit events.</param>
        /// <param name="rbacService">RBAC service used to verify permissions on protected actions.</param>
        /// <exception cref="ArgumentNullException">Thrown if any dependency is <c>null</c>.</exception>
        public UserService(DatabaseService databaseService, AuditService auditService, IRBACService rbacService)
        {
            _db   = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _audit = auditService   ?? throw new ArgumentNullException(nameof(auditService));
            _rbac  = rbacService    ?? throw new ArgumentNullException(nameof(rbacService));
        }

        #region === AUTHENTICATION & SECURITY ===

        /// <summary>
        /// Attempts to authenticate a user with username/password.
        /// <para>
        /// Behavior:
        /// <list type="number">
        /// <item><description>Always logs <c>LOGIN_ATTEMPT</c>. If a user record is found, the attempt is logged with that user’s ID.</description></item>
        /// <item><description>On success, logs <c>LOGIN_SUCCESS</c> with the resolved user ID and resets the failed-login counter.</description></item>
        /// <item><description>On failure, logs <c>LOGIN_FAILED</c> with a precise reason: <c>empty_credentials</c>, <c>unknown_user</c>, or <c>bad_password</c>.</description></item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="username">Username supplied by the operator.</param>
        /// <param name="password">Plaintext password supplied by the operator (hashed internally).</param>
        /// <returns>
        /// A resolved <see cref="User"/> on successful authentication; otherwise <c>null</c>.
        /// </returns>
        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            // Handle empty credentials early (no DB hit), but still provide a forensic trail.
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                // Attempt (unknown user id)
                await _audit.LogSystemEventForUserAsync(
                    userId   : null,
                    action   : "LOGIN_ATTEMPT",
                    details  : $"username={username}; note=attempt; reason=empty_credentials",
                    tableName: "users",
                    recordId : null);

                // Failure (explicit reason)
                await _audit.LogSystemEventForUserAsync(
                    userId   : null,
                    action   : "LOGIN_FAILED",
                    details  : $"username={username}; reason=empty_credentials",
                    tableName: "users",
                    recordId : null);

                return null;
            }

            // Pull candidate user by username (or email, depending on your DB method semantics).
            var user = await _db.GetUserByUsernameAsync(username);

            // Always record the attempt — include userId if resolvable.
            await _audit.LogSystemEventForUserAsync(
                userId   : user?.Id,
                action   : "LOGIN_ATTEMPT",
                details  : $"username={username}; note=attempt",
                tableName: "users",
                recordId : user?.Id);

            // If user doesn't exist, log a precise failure and bail.
            if (user is null)
            {
                await _audit.LogSystemEventForUserAsync(
                    userId   : null,
                    action   : "LOGIN_FAILED",
                    details  : $"username={username}; reason=unknown_user",
                    tableName: "users",
                    recordId : null);

                return null;
            }

            // Compute the candidate hash and verify basic flags
            string hashed = HashPassword(password);

            // Locked account — do not reveal validity of the password; just record the lock state.
            if (user.IsLocked)
            {
                await _audit.LogSystemEventForUserAsync(
                    userId   : user.Id,
                    action   : "LOGIN_LOCKED",
                    details  : $"username={username}; reason=lockout",
                    tableName: "users",
                    recordId : user.Id);

                return null;
            }

            // Validate password and active flag
            if (user.PasswordHash == hashed && user.Active)
            {
                // Success path: audit + reset counters
                await _audit.LogSystemEventForUserAsync(
                    userId   : user.Id,
                    action   : "LOGIN_SUCCESS",
                    details  : $"username={username}; auth=local",
                    tableName: "users",
                    recordId : user.Id);

                await _db.MarkLoginSuccessAsync(user.Id);
                return user;
            }

            // Bad password (or inactive)
            await _audit.LogSystemEventForUserAsync(
                userId   : user.Id,
                action   : "LOGIN_FAILED",
                details  : user.Active
                           ? $"username={username}; reason=bad_password"
                           : $"username={username}; reason=inactive_account",
                tableName: "users",
                recordId : user.Id);

            // Increment server-side counter if the identity exists
            await _db.IncrementFailedLoginsAsync(user.Id);
            return null;
        }

        /// <summary>
        /// Computes a SHA-256 hash of the provided password and returns it as Base64.
        /// <para>
        /// Note: This is a simple hash helper to match your current schema. For a production-grade
        /// system use a slow, salted KDF (e.g., PBKDF2/Argon2/BCrypt) and store salt &amp; parameters separately.
        /// </para>
        /// </summary>
        /// <param name="password">Plaintext password to hash.</param>
        /// <returns>Base64-encoded SHA-256 digest of <paramref name="password"/>.</returns>
        public string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(password ?? string.Empty));
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Demo 2FA verification stub (returns <c>true</c> only for code <c>123456</c>).
        /// <para>
        /// This method intentionally does not alter session state; it can be wired into your UI
        /// (e.g., <c>LoginViewModel</c>) after a successful primary credential check.
        /// </para>
        /// </summary>
        /// <param name="username">Username used to resolve the account for logging or throttling.</param>
        /// <param name="code">Second factor code entered by the operator.</param>
        /// <returns><c>true</c> if the demo code matches; otherwise <c>false</c>.</returns>
        public async Task<bool> VerifyTwoFactorCodeAsync(string username, string code)
        {
            // Lightweight simulated latency
            await Task.Delay(50);
            return code == "123456";
        }

        /// <summary>
        /// Locks the specified user account (RBAC enforced) and writes an audit entry.
        /// </summary>
        /// <param name="userId">Target user identifier.</param>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown by <see cref="IRBACService.AssertPermissionAsync(int, string)"/> if the caller lacks permission.
        /// </exception>
        public async Task LockUserAsync(int userId)
        {
            await _rbac.AssertPermissionAsync(userId, "user.lock");
            await _db.LockUserAsync(userId);

            await _audit.LogEntityAuditAsync(
                tableName: "users",
                entityId : userId,
                action   : "LOCK",
                details  : $"Korisnik ID={userId} zaključan zbog previše neuspjelih pokušaja.");
        }

        #endregion

        #region === CRUD OPERATIONS ===

        /// <summary>
        /// Retrieves all users. RBAC enforcement is expected at the caller level (UI/workflow).
        /// </summary>
        /// <returns>List of <see cref="User"/> records.</returns>
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _db.GetAllUsersAsync(includeAudit: true);
        }

        /// <summary>
        /// Resolves a user by identifier.
        /// </summary>
        /// <param name="id">User primary key.</param>
        /// <returns>The <see cref="User"/> if present; otherwise <c>null</c>.</returns>
        public async Task<User?> GetUserByIdAsync(int id) => await _db.GetUserByIdAsync(id);

        /// <summary>
        /// Resolves a user by username (case sensitivity defined by your DB collation).
        /// </summary>
        /// <param name="username">The unique username to search for.</param>
        /// <returns>The <see cref="User"/> if present; otherwise <c>null</c>.</returns>
        public async Task<User?> GetUserByUsernameAsync(string username) => await _db.GetUserByUsernameAsync(username);

        /// <summary>
        /// Creates a user (admin permission required) and records an audit entry.
        /// </summary>
        /// <param name="user">New user entity (PasswordHash should contain a plaintext password; it will be hashed here).</param>
        /// <param name="adminId">Identifier of the admin performing the action (used by <see cref="IRBACService"/>).</param>
        public async Task CreateUserAsync(User user, int adminId = 0)
        {
            await _rbac.AssertPermissionAsync(adminId, "user.create");

            // Persist a hash derived from the supplied plaintext in PasswordHash.
            user.PasswordHash = HashPassword(user.PasswordHash);

            // FIX: parameter name must be 'update' (not 'isUpdate') to match DatabaseService signature.
            await _db.InsertOrUpdateUserAsync(user, update: false);

            await _audit.LogEntityAuditAsync(
                tableName: "users",
                entityId : user.Id,
                action   : "CREATE",
                details  : $"Korisnik {user.Username} kreiran od admin ID={adminId}");
        }

        /// <summary>
        /// Updates an existing user (admin permission required) and records an audit entry.
        /// </summary>
        /// <param name="user">Existing user entity with modified fields already set.</param>
        /// <param name="adminId">Identifier of the admin performing the action.</param>
        public async Task UpdateUserAsync(User user, int adminId = 0)
        {
            await _rbac.AssertPermissionAsync(adminId, "user.update");

            // FIX: parameter name must be 'update' (not 'isUpdate').
            await _db.InsertOrUpdateUserAsync(user, update: true);

            await _audit.LogEntityAuditAsync(
                tableName: "users",
                entityId : user.Id,
                action   : "UPDATE",
                details  : $"Korisnik {user.Username} ažuriran od admin ID={adminId}");
        }

        /// <summary>
        /// Deletes a user (admin permission required). Performs a DB-level delete and mirrors to audit.
        /// </summary>
        /// <param name="userId">Target user primary key.</param>
        /// <param name="adminId">Admin responsible for the operation.</param>
        public async Task DeleteUserAsync(int userId, int adminId = 0)
        {
            await _rbac.AssertPermissionAsync(adminId, "user.delete");

            // DatabaseService.DeleteUserAsync already accepts audit context (ip/device/session) — pass empty strings if not available here.
            await _db.DeleteUserAsync(userId, adminId, ip: string.Empty, deviceInfo: string.Empty, sessionId: null);

            await _audit.LogEntityAuditAsync(
                tableName: "users",
                entityId : userId,
                action   : "DELETE",
                details  : $"Korisnik ID={userId} obrisan od admin ID={adminId}");
        }

        /// <summary>
        /// Soft-deactivates a user account and writes an audit entry.
        /// </summary>
        /// <param name="userId">User primary key.</param>
        public async Task DeactivateUserAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user is null) return;

            user.Active = false;

            // FIX: parameter name must be 'update' (not 'isUpdate').
            await _db.InsertOrUpdateUserAsync(user, update: true);

            await _audit.LogEntityAuditAsync(
                tableName: "users",
                entityId : userId,
                action   : "DEACTIVATE",
                details  : $"Korisnik {user.Username} deaktiviran.");
        }

        #endregion

        #region === ROLE, PERMISSIONS, PROFILE ===

        /// <summary>
        /// Returns <c>true</c> when the specified user has the provided role string (case-insensitive comparison).
        /// </summary>
        /// <param name="user">Resolved user instance.</param>
        /// <param name="role">Role name to check.</param>
        /// <returns><c>true</c> if role matches; otherwise <c>false</c>.</returns>
        public bool HasRole(User? user, string role) =>
            user != null && user.Role?.Equals(role, StringComparison.OrdinalIgnoreCase) == true;

        /// <summary>
        /// Returns <c>true</c> when the specified user is active; otherwise <c>false</c>.
        /// </summary>
        /// <param name="user">Resolved user instance.</param>
        public bool IsActive(User? user) => user != null && user.Active;

        /// <summary>
        /// Changes a user's password (admin permission required) and writes an audit entry.
        /// </summary>
        /// <param name="userId">Target user identifier.</param>
        /// <param name="newPassword">New plaintext password; this method will hash and persist it.</param>
        /// <param name="adminId">Admin performing the action.</param>
public async Task ChangePasswordAsync(int userId, string newPassword, int adminId = 0)
{
    try
    {
        await _rbac.AssertPermissionAsync(adminId, "user.change_password");
    }
    catch (UnauthorizedAccessException ex)
    {
        // 1) A single RBAC_DENY row you can filter in system_event_log
        await _db.LogSystemEventAsync(
            userId: adminId,
            eventType: "RBAC_DENY",
            tableName: "users",
            module: "RBAC",
            recordId: userId,
            description: "Permission required: user.change_password",
            ip: "system",
            severity: "warn",
            deviceInfo: "server",
            sessionId: null
        );

        // 2) Full exception with stack trace for debugging
        await _db.LogExceptionAsync(ex, module: "UserService.ChangePassword", table: "users", recordId: userId);

        throw; // keep existing behavior
    }

    var user = await GetUserByIdAsync(userId);
    if (user == null) return;

    user.PasswordHash = HashPassword(newPassword);

    // NOTE: DatabaseService.InsertOrUpdateUserAsync expects parameter name 'update'
    await _db.InsertOrUpdateUserAsync(user, update: true);

    await _audit.LogEntityAuditAsync(
        tableName: "users",
        entityId: userId,
        action: "CHANGE_PASSWORD",
        details: $"Korisnik {user.Username} promijenio lozinku (admin ID={adminId})."
    );
}


        #endregion

        #region === DIGITAL SIGNATURES & GMP COMPLIANCE ===

        /// <summary>
        /// Generates a lightweight, Base64-encoded signature token suitable for demo or UI confirmation flows.
        /// <para>
        /// Format: <c>{username}|{UTC-ISO8601}|{Guid}</c>
        /// </para>
        /// </summary>
        /// <param name="user">User for whom the signature is generated.</param>
        /// <returns>Base64 token; empty string when <paramref name="user"/> is <c>null</c>.</returns>
        public string GenerateDigitalSignature(User? user)
        {
            if (user == null) return string.Empty;
            string raw = $"{user.Username}|{DateTime.UtcNow:O}|{Guid.NewGuid()}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        }

        /// <summary>
        /// Validates a previously generated digital signature string.
        /// <para>Current implementation only checks for non-emptiness.</para>
        /// </summary>
        /// <param name="signature">Signature token to validate.</param>
        /// <returns><c>true</c> if token is non-empty; otherwise <c>false</c>.</returns>
        public bool ValidateDigitalSignature(string signature) =>
            !string.IsNullOrWhiteSpace(signature);

        #endregion

        #region === GMP/CSV/21 CFR PART 11 BONUS EXTENSIONS ===

        /// <summary>
        /// Writes a custom user event into the entity audit stream (handy for UI-level actions).
        /// </summary>
        /// <param name="userId">Target user identifier.</param>
        /// <param name="eventType">Event action keyword (e.g., <c>PROFILE_VIEW</c>).</param>
        /// <param name="details">Human-readable details for the audit trail.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task LogUserEventAsync(int userId, string eventType, string details)
            => _audit.LogEntityAuditAsync("users", userId, eventType, details);

        /// <summary>
        /// Unlocks a previously locked user account (admin permission required) and records an audit entry.
        /// </summary>
        /// <param name="userId">Target user identifier.</param>
        /// <param name="adminId">Admin performing the action.</param>
        public async Task UnlockUserAsync(int userId, int adminId)
        {
            await _rbac.AssertPermissionAsync(adminId, "user.unlock");
            await _db.UnlockUserAsync(userId);

            await _audit.LogEntityAuditAsync(
                tableName: "users",
                entityId : userId,
                action   : "UNLOCK",
                details  : $"Korisnik ID={userId} otključan od admin ID={adminId}");
        }

        /// <summary>
        /// Enables or disables 2FA for the specified user (permission enforced) and records an audit entry.
        /// </summary>
        /// <param name="userId">Target user identifier.</param>
        /// <param name="enabled"><c>true</c> to enable 2FA; <c>false</c> to disable.</param>
        public async Task SetTwoFactorEnabledAsync(int userId, bool enabled)
        {
            await _rbac.AssertPermissionAsync(userId, "user.set_2fa");
            await _db.SetTwoFactorEnabledAsync(userId, enabled);

            await _audit.LogEntityAuditAsync(
                tableName: "users",
                entityId : userId,
                action   : enabled ? "ENABLE_2FA" : "DISABLE_2FA",
                details  : $"2FA postavljen na {enabled}");
        }

        /// <summary>
        /// Updates user profile fields and mirrors the change into the audit stream.
        /// </summary>
        /// <param name="user">User entity containing the updated profile.</param>
        /// <param name="adminId">Admin responsible for the change (0 if self-service).</param>
        public async Task UpdateUserProfileAsync(User user, int adminId = 0)
        {
            await UpdateUserAsync(user, adminId);

            await _audit.LogEntityAuditAsync(
                tableName: "users",
                entityId : user.Id,
                action   : "PROFILE_UPDATE",
                details  : $"Korisnik {user.Username} ažurirao profil (admin ID={adminId}).");
        }

        #endregion
    }
}
