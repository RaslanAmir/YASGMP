using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>AuthService</b> – GMP compliant central authentication/orchestration for YasGMP users.
    /// <para>
    /// Performs credential verification using <see cref="UserService"/>, logs every attempt via
    /// <see cref="AuditService"/>, and tracks minimal session/device forensics.
    /// </para>
    /// </summary>
    public class AuthService : IAuthContext
    {
        private readonly UserService _userService;
        private readonly AuditService _auditService;

        /// <summary>Currently authenticated user (or <c>null</c> if none).</summary>
        public User? CurrentUser { get; private set; }

        /// <summary>Last successful logical session identifier (GUID string).</summary>
        public string CurrentSessionId { get; private set; } = Guid.NewGuid().ToString();

        /// <summary>Current device forensic info: OS, host, user.</summary>
        public string CurrentDeviceInfo =>
            $"OS={Environment.OSVersion}, Host={Environment.MachineName}, User={Environment.UserName}";

        /// <summary>Current local IP address if <c>IPlatformService</c> is available; otherwise <c>unknown</c>.</summary>
        public string CurrentIpAddress =>
            ServiceLocator.GetService<IPlatformService>()?.GetLocalIpAddress() ?? "unknown";

        /// <summary>Compatibility alias for older code paths.</summary>
        public string CurrentIp => CurrentIpAddress;

        /// <summary>Compatibility alias for older code paths.</summary>
        public string CurrentUserIp => CurrentIpAddress;

        /// <summary>
        /// Initializes a new instance of <see cref="AuthService"/>.
        /// </summary>
        /// <param name="userService">User management service (credential checks, RBAC, profile).</param>
        /// <param name="auditService">Audit logging service.</param>
        /// <exception cref="ArgumentNullException">Thrown if a dependency is <c>null</c>.</exception>
        public AuthService(UserService userService, AuditService auditService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        #region === PUBLIC AUTHENTICATION ===

        /// <summary>
        /// Authenticates a user by username and password.
        /// <para>Returns the <see cref="User"/> on success; <c>null</c> otherwise. All attempts are audited.</para>
        /// </summary>
        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await _auditService.LogSystemEventAsync("LOGIN_FAILED", $"Empty credentials for user: {username}");
                return null;
            }

            try
            {
                var user = await _userService.GetUserByUsernameAsync(username);
                if (user == null)
                {
                    await _auditService.LogSystemEventAsync("LOGIN_FAILED", $"Unknown user: {username}");
                    return null;
                }

                if (!user.Active)
                {
                    await _auditService.LogSystemEventAsync("LOGIN_FAILED", $"Inactive user: {username}");
                    return null;
                }

                // Robust verification
                var result = VerifyResult(password, user.PasswordHash);
                if (!result.IsMatch)
                {
                    await _auditService.LogSystemEventAsync(
                        "LOGIN_FAILED",
                        $"Bad password for user={username}; storedShape={result.StoredShape}; storedLen={result.StoredLength}; note={result.Note}");
                    return null;
                }

                // Optional self-heal (handled by UserService after it verifies)
                if (result.NeedsRehash && result.RehashBase64 != null)
                {
                    try { await _userService.SetPasswordHashDirectAsync(user.Id, result.RehashBase64); }
                    catch { /* non-fatal */ }
                }

                CurrentUser = user;
                CurrentSessionId = Guid.NewGuid().ToString();

                await _auditService.LogSystemEventAsync(
                    "LOGIN_SUCCESS",
                    $"User {username} logged in. Device: {CurrentDeviceInfo}, IP: {CurrentIpAddress}, Session: {CurrentSessionId}");

                return user;
            }
            catch (Exception ex)
            {
                await _auditService.LogSystemEventAsync("LOGIN_ERROR", $"Login error for user {username}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Verifies a two-factor authentication code for the given username.
        /// Logs success/failure and ensures <see cref="CurrentUser"/> is hydrated for downstream consumers.
        /// </summary>
        public async Task<bool> VerifyTwoFactorCodeAsync(string username, string code)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(code))
            {
                await _auditService.LogSystemEventAsync("2FA_FAILED", $"Missing 2FA input for user: {username}");
                return false;
            }

            try
            {
                var valid = await _userService.VerifyTwoFactorCodeAsync(username, code).ConfigureAwait(false);

                if (valid)
                {
                    if (CurrentUser == null)
                    {
                        CurrentUser = await _userService.GetUserByUsernameAsync(username).ConfigureAwait(false);
                    }

                    await _auditService.LogSystemEventAsync(
                        "2FA_SUCCESS",
                        $"User {username} passed 2FA. Device: {CurrentDeviceInfo}, IP: {CurrentIpAddress}, Session: {CurrentSessionId}");
                }
                else
                {
                    await _auditService.LogSystemEventAsync(
                        "2FA_FAILED",
                        $"User {username} provided invalid 2FA code. Session: {CurrentSessionId}");
                }

                return valid;
            }
            catch (Exception ex)
            {
                await _auditService.LogSystemEventAsync("2FA_ERROR", $"2FA error for user {username}: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region === PASSWORD MANAGEMENT / HELPERS ===

        /// <summary>
        /// Computes the SHA-256 hash of <paramref name="password"/> and returns it as Base64.
        /// </summary>
        public static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password ?? string.Empty);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private static bool LooksLikeBase64Sha256(string s) =>
            s.Length is >= 43 and <= 45 && Regex.IsMatch(s, @"^[A-Za-z0-9+/]+={0,2}$");

        private static bool LooksLikeHexSha256(string s) =>
            s.Length == 64 && Regex.IsMatch(s, "^[0-9a-fA-F]{64}$");

        private static string ComputeSha256Hex(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        /// <summary>
        /// Verifies password vs stored and describes how it was interpreted; used for deep auditing.
        /// </summary>
        public static VerifyInfo VerifyResult(string password, string? storedRaw)
        {
            var info = new VerifyInfo
            {
                Stored = storedRaw ?? string.Empty,
                StoredLength = (storedRaw ?? string.Empty).Length,
                StoredShape = "unknown",
                Note = ""
            };

            var stored = (storedRaw ?? string.Empty).Trim();

            if (LooksLikeBase64Sha256(stored))
            {
                info.StoredShape = "base64-sha256";
                var base64 = HashPassword(password);
                info.IsMatch = string.Equals(base64, stored, StringComparison.Ordinal);
                return info;
            }

            if (LooksLikeHexSha256(stored))
            {
                info.StoredShape = "hex-sha256";
                var hex = ComputeSha256Hex(password);
                info.IsMatch = string.Equals(hex, stored, StringComparison.OrdinalIgnoreCase);
                info.NeedsRehash = info.IsMatch;
                if (info.IsMatch) info.RehashBase64 = HashPassword(password);
                return info;
            }

            // Last resort: legacy cleartext (dev/test)
            info.StoredShape = "cleartext";
            info.IsMatch = string.Equals(password, stored, StringComparison.Ordinal);
            info.NeedsRehash = info.IsMatch;
            if (info.IsMatch) info.RehashBase64 = HashPassword(password);
            return info;
        }

        /// <summary>Compatibility alias (Base64 SHA-256 only).</summary>
        public static bool VerifyPassword(string password, string storedHash) =>
            string.Equals(HashPassword(password), storedHash?.Trim(), StringComparison.Ordinal);

        /// <summary>Verification forensic result.</summary>
        public sealed class VerifyInfo
        {
            public bool IsMatch { get; set; }
            public string Stored { get; set; } = "";
            public int StoredLength { get; set; }
            public string StoredShape { get; set; } = "unknown";
            public bool NeedsRehash { get; set; }
            public string? RehashBase64 { get; set; }
            public string Note { get; set; } = "";
        }

        #endregion

        #region === ACCOUNT SECURITY ===

        /// <summary>
        /// Soft-locks (deactivates) a user account and writes an audit record.
        /// </summary>
        public async Task LockUserAccountAsync(int userId, string reason)
        {
            await _userService.DeactivateUserAsync(userId);
            await _auditService.LogSystemEventAsync("ACCOUNT_LOCKED", $"User ID={userId} locked. Reason: {reason}");
        }

        #endregion
    }
}
