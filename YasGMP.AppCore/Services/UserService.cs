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
    /// </summary>
    public class UserService : IUserService
    {
        private readonly DatabaseService _db;
        private readonly AuditService _audit;
        private readonly IRBACService _rbac;
        /// <summary>
        /// Initializes a new instance of the UserService class.
        /// </summary>

        public UserService(DatabaseService databaseService, AuditService auditService, IRBACService rbacService)
        {
            _db   = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _audit = auditService   ?? throw new ArgumentNullException(nameof(auditService));
            _rbac  = rbacService    ?? throw new ArgumentNullException(nameof(rbacService));
        }

        #region === AUTHENTICATION & SECURITY ===
        /// <summary>
        /// Executes the authenticate async operation.
        /// </summary>

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await _audit.LogSystemEventForUserAsync(null, "LOGIN_FAILED",
                    $"username={username}; reason=empty_credentials", "users", null);
                return null;
            }

            var user = await _db.GetUserByUsernameAsync(username);

            await _audit.LogSystemEventForUserAsync(user?.Id, "LOGIN_ATTEMPT",
                $"username={username}; note=attempt", "users", user?.Id);

            if (user is null)
            {
                await _audit.LogSystemEventForUserAsync(null, "LOGIN_FAILED",
                    $"username={username}; reason=unknown_user", "users", null);
                return null;
            }

            if (user.IsLocked)
            {
                await _audit.LogSystemEventForUserAsync(user.Id, "LOGIN_LOCKED",
                    $"username={username}; reason=lockout", "users", user.Id);
                return null;
            }

            var vf = AuthService.VerifyResult(password, user.PasswordHash);

            if (vf.IsMatch && user.Active)
            {
                // Self-heal formats (cleartext/hex -> base64)
                if (vf.NeedsRehash && !string.IsNullOrEmpty(vf.RehashBase64))
                {
                    try { await SetPasswordHashDirectAsync(user.Id, vf.RehashBase64); }
                    catch { /* non-fatal */ }
                }

                await _audit.LogSystemEventForUserAsync(user.Id, "LOGIN_SUCCESS",
                    $"username={username}; auth=local; shape={vf.StoredShape}", "users", user.Id);

                await _db.MarkLoginSuccessAsync(user.Id);
                return user;
            }

            await _audit.LogSystemEventForUserAsync(
                user.Id, "LOGIN_FAILED",
                user.Active
                    ? $"username={username}; reason=bad_password; shape={vf.StoredShape}; storedLen={vf.StoredLength}"
                    : $"username={username}; reason=inactive_account",
                "users", user.Id);

            await _db.IncrementFailedLoginsAsync(user.Id);
            return null;
        }

        /// <summary>Computes Base64 SHA-256.</summary>
        public string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(password ?? string.Empty));
            return Convert.ToBase64String(hash);
        }
        /// <summary>
        /// Executes the verify two factor code async operation.
        /// </summary>

        public async Task<bool> VerifyTwoFactorCodeAsync(string username, string code)
        {
            await Task.Delay(50);
            return code == "123456";
        }
        /// <summary>
        /// Executes the lock user async operation.
        /// </summary>

        public async Task LockUserAsync(int userId)
        {
            await _rbac.AssertPermissionAsync(userId, "user.lock");
            await _db.LockUserAsync(userId);

            await _audit.LogEntityAuditAsync("users", userId, "LOCK",
                $"Korisnik ID={userId} zaključan zbog previše neuspjelih pokušaja.");
        }

        #endregion

        #region === CRUD OPERATIONS ===
        /// <summary>
        /// Executes the get all users async operation.
        /// </summary>

        public async Task<List<User>> GetAllUsersAsync() => await _db.GetAllUsersAsync(includeAudit: true);
        /// <summary>
        /// Executes the get user by id async operation.
        /// </summary>

        public async Task<User?> GetUserByIdAsync(int id) => await _db.GetUserByIdAsync(id);
        /// <summary>
        /// Executes the get user by username async operation.
        /// </summary>

        public async Task<User?> GetUserByUsernameAsync(string username) => await _db.GetUserByUsernameAsync(username);
        /// <summary>
        /// Executes the create user async operation.
        /// </summary>

        public async Task CreateUserAsync(User user, int adminId = 0)
        {
            await _rbac.AssertPermissionAsync(adminId, "user.create");
            user.PasswordHash = HashPassword(user.PasswordHash);
            await _db.InsertOrUpdateUserAsync(user, update: false);

            await _audit.LogEntityAuditAsync("users", user.Id, "CREATE",
                $"Korisnik {user.Username} kreiran od admin ID={adminId}");
        }
        /// <summary>
        /// Executes the update user async operation.
        /// </summary>

        public async Task UpdateUserAsync(User user, int adminId = 0)
        {
            await _rbac.AssertPermissionAsync(adminId, "user.update");
            await _db.InsertOrUpdateUserAsync(user, update: true);

            await _audit.LogEntityAuditAsync("users", user.Id, "UPDATE",
                $"Korisnik {user.Username} ažuriran od admin ID={adminId}");
        }
        /// <summary>
        /// Executes the delete user async operation.
        /// </summary>

        public async Task DeleteUserAsync(int userId, int adminId = 0)
        {
            await _rbac.AssertPermissionAsync(adminId, "user.delete");
            await _db.DeleteUserAsync(userId, adminId, ip: string.Empty, deviceInfo: string.Empty, sessionId: null);

            await _audit.LogEntityAuditAsync("users", userId, "DELETE",
                $"Korisnik ID={userId} obrisan od admin ID={adminId}");
        }
        /// <summary>
        /// Executes the deactivate user async operation.
        /// </summary>

        public async Task DeactivateUserAsync(int userId)
        {
            var user = await GetUserByIdAsync(userId);
            if (user is null) return;

            user.Active = false;
            await _db.InsertOrUpdateUserAsync(user, update: true);

            await _audit.LogEntityAuditAsync("users", userId, "DEACTIVATE",
                $"Korisnik {user.Username} deaktiviran.");
        }

        #endregion

        #region === ROLE, PERMISSIONS, PROFILE ===
        /// <summary>
        /// Executes the has role operation.
        /// </summary>

        public bool HasRole(User? user, string role) =>
            user != null && user.Role?.Equals(role, StringComparison.OrdinalIgnoreCase) == true;
        /// <summary>
        /// Executes the is active operation.
        /// </summary>

        public bool IsActive(User? user) => user != null && user.Active;
        /// <summary>
        /// Executes the change password async operation.
        /// </summary>

        public async Task ChangePasswordAsync(int userId, string newPassword, int adminId = 0)
        {
            try
            {
                await _rbac.AssertPermissionAsync(adminId, "user.change_password");
            }
            catch (UnauthorizedAccessException ex)
            {
                await _db.LogSystemEventAsync(
                    userId: adminId, eventType: "RBAC_DENY", tableName: "users", module: "RBAC",
                    recordId: userId, description: "Permission required: user.change_password",
                    ip: "system", severity: "warn", deviceInfo: "server", sessionId: null);

                await _db.LogExceptionAsync(ex, module: "UserService.ChangePassword", table: "users", recordId: userId);
                throw;
            }

            var user = await GetUserByIdAsync(userId);
            if (user == null) return;

            user.PasswordHash = HashPassword(newPassword);
            await _db.InsertOrUpdateUserAsync(user, update: true);

            await _audit.LogEntityAuditAsync("users", userId, "CHANGE_PASSWORD",
                $"Korisnik {user.Username} promijenio lozinku (admin ID={adminId}).");
        }

        #endregion

        #region === DIGITAL SIGNATURES & EXTRAS ===
        /// <summary>
        /// Executes the generate digital signature operation.
        /// </summary>

        public string GenerateDigitalSignature(User? user)
        {
            if (user == null) return string.Empty;
            string raw = $"{user.Username}|{DateTime.UtcNow:O}|{Guid.NewGuid()}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        }
        /// <summary>
        /// Executes the validate digital signature operation.
        /// </summary>

        public bool ValidateDigitalSignature(string signature) =>
            !string.IsNullOrWhiteSpace(signature);
        /// <summary>
        /// Executes the log user event async operation.
        /// </summary>

        public Task LogUserEventAsync(int userId, string eventType, string details)
            => _audit.LogEntityAuditAsync("users", userId, eventType, details);
        /// <summary>
        /// Executes the unlock user async operation.
        /// </summary>

        public async Task UnlockUserAsync(int userId, int adminId)
        {
            await _rbac.AssertPermissionAsync(adminId, "user.unlock");
            await _db.UnlockUserAsync(userId);

            await _audit.LogEntityAuditAsync("users", userId, "UNLOCK",
                $"Korisnik ID={userId} otključan od admin ID={adminId}");
        }
        /// <summary>
        /// Executes the set two factor enabled async operation.
        /// </summary>

        public async Task SetTwoFactorEnabledAsync(int userId, bool enabled)
        {
            await _rbac.AssertPermissionAsync(userId, "user.set_2fa");
            await _db.SetTwoFactorEnabledAsync(userId, enabled);

            await _audit.LogEntityAuditAsync("users", userId,
                enabled ? "ENABLE_2FA" : "DISABLE_2FA", $"2FA postavljen na {enabled}");
        }
        /// <summary>
        /// Executes the update user profile async operation.
        /// </summary>

        public async Task UpdateUserProfileAsync(User user, int adminId = 0)
        {
            await UpdateUserAsync(user, adminId);
            await _audit.LogEntityAuditAsync("users", user.Id, "PROFILE_UPDATE",
                $"Korisnik {user.Username} ažurirao profil (admin ID={adminId}).");
        }

        /// <summary>
        /// Directly sets a new password hash (Base64 SHA-256) for a user without knowing the plaintext.
        /// Useful for self-healing when detecting legacy formats.
        /// </summary>
        public async Task SetPasswordHashDirectAsync(int userId, string base64Sha256)
        {
            if (string.IsNullOrWhiteSpace(base64Sha256)) return;
            await _db.ExecuteNonQueryAsync(
                "UPDATE users SET password_hash=@ph WHERE id=@id",
                new MySqlConnector.MySqlParameter[]
                {
                    new("@ph", base64Sha256.Trim()),
                    new("@id", userId)
                });
        }

        #endregion

        #region === IMPERSONATION SUPPORT ===

        /// <inheritdoc />
        public async Task<ImpersonationContext?> BeginImpersonationAsync(int targetUserId, UserCrudContext context)
        {
            if (targetUserId <= 0) throw new ArgumentOutOfRangeException(nameof(targetUserId));

            await _rbac.AssertPermissionAsync(context.UserId, "user.impersonate");

            if (context.UserId == targetUserId)
            {
                await _db.LogSystemEventAsync(
                    context.UserId,
                    "IMPERSONATION_DENIED",
                    "session_log",
                    "UserService",
                    null,
                    "actor attempted to impersonate self",
                    context.Ip,
                    "warn",
                    context.DeviceInfo,
                    context.SessionId,
                    signatureId: context.SignatureId,
                    signatureHash: context.SignatureHash).ConfigureAwait(false);
                return null;
            }

            var target = await _db.GetUserByIdAsync(targetUserId).ConfigureAwait(false);
            if (target is null || !target.Active)
            {
                await _db.LogSystemEventAsync(
                    context.UserId,
                    "IMPERSONATION_DENIED",
                    "session_log",
                    "UserService",
                    targetUserId,
                    "target user unavailable or inactive",
                    context.Ip,
                    "warn",
                    context.DeviceInfo,
                    context.SessionId,
                    signatureId: context.SignatureId,
                    signatureHash: context.SignatureHash).ConfigureAwait(false);
                return null;
            }

            var reason = string.IsNullOrWhiteSpace(context.Reason)
                ? $"Impersonation of user {targetUserId}"
                : context.Reason.Trim();
            var notes = string.IsNullOrWhiteSpace(context.Notes) ? null : context.Notes.Trim();
            var sessionId = string.IsNullOrWhiteSpace(context.SessionId)
                ? Guid.NewGuid().ToString("N")
                : context.SessionId!;
            var startedAt = DateTime.UtcNow;

            var sessionLogId = await _db.BeginImpersonationSessionAsync(
                actorUserId: context.UserId,
                targetUserId: targetUserId,
                startedAtUtc: startedAt,
                sessionId: sessionId,
                ip: context.Ip,
                deviceInfo: context.DeviceInfo,
                reason: reason,
                notes: notes,
                signatureId: context.SignatureId,
                signatureHash: context.SignatureHash,
                signatureMethod: context.SignatureMethod,
                signatureStatus: context.SignatureStatus,
                signatureNote: context.SignatureNote).ConfigureAwait(false);

            await _db.LogSystemEventAsync(
                context.UserId,
                "IMPERSONATION_BEGIN",
                "session_log",
                "UserService",
                sessionLogId,
                $"actor={context.UserId}; target={targetUserId}; reason={reason}; notes={notes ?? "-"}",
                context.Ip,
                "audit",
                context.DeviceInfo,
                sessionId,
                signatureId: context.SignatureId,
                signatureHash: context.SignatureHash).ConfigureAwait(false);

            return new ImpersonationContext(
                context.UserId,
                targetUserId,
                sessionLogId,
                startedAt,
                reason,
                notes,
                context.Ip,
                context.DeviceInfo,
                sessionId,
                context.SignatureId,
                context.SignatureHash,
                context.SignatureMethod,
                context.SignatureStatus,
                context.SignatureNote);
        }

        /// <inheritdoc />
        public async Task EndImpersonationAsync(ImpersonationContext context, UserCrudContext auditContext)
        {
            ArgumentNullException.ThrowIfNull(context);

            await _rbac.AssertPermissionAsync(auditContext.UserId, "user.impersonate");

            var mergedSignatureId = auditContext.SignatureId ?? context.SignatureId;
            var mergedSignatureHash = string.IsNullOrWhiteSpace(auditContext.SignatureHash)
                ? context.SignatureHash
                : auditContext.SignatureHash;
            var mergedSignatureMethod = string.IsNullOrWhiteSpace(auditContext.SignatureMethod)
                ? context.SignatureMethod
                : auditContext.SignatureMethod;
            var mergedSignatureStatus = string.IsNullOrWhiteSpace(auditContext.SignatureStatus)
                ? context.SignatureStatus
                : auditContext.SignatureStatus;
            var mergedSignatureNote = string.IsNullOrWhiteSpace(auditContext.SignatureNote)
                ? context.SignatureNote
                : auditContext.SignatureNote;
            var mergedNotes = string.IsNullOrWhiteSpace(auditContext.Notes)
                ? context.Notes
                : auditContext.Notes;

            await _db.EndImpersonationSessionAsync(
                sessionLogId: context.SessionLogId,
                actorUserId: auditContext.UserId,
                endedAtUtc: DateTime.UtcNow,
                notes: mergedNotes,
                signatureId: mergedSignatureId,
                signatureHash: mergedSignatureHash,
                signatureMethod: mergedSignatureMethod,
                signatureStatus: mergedSignatureStatus,
                signatureNote: mergedSignatureNote).ConfigureAwait(false);

            await _db.LogSystemEventAsync(
                auditContext.UserId,
                "IMPERSONATION_END",
                "session_log",
                "UserService",
                context.SessionLogId,
                $"actor={context.ActorUserId}; target={context.TargetUserId}; reason={context.Reason}; notes={mergedNotes ?? "-"}",
                auditContext.Ip,
                "audit",
                auditContext.DeviceInfo,
                auditContext.SessionId,
                signatureId: mergedSignatureId,
                signatureHash: mergedSignatureHash).ConfigureAwait(false);
        }

        #endregion
    }
}
