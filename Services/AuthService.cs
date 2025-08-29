using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>AuthService</b> – GMP compliant central authentication/orchestration for YasGMP users.
    /// <para>
    /// Performs credential verification using <see cref="UserService"/>, logs every attempt via
    /// <see cref="AuditService"/>, and tracks minimal session/device forensics.
    /// </para>
    /// </summary>
    public class AuthService
    {
        private readonly UserService _userService;
        private readonly AuditService _auditService;

        /// <summary>
        /// Currently authenticated user (or <c>null</c> if none).
        /// </summary>
        public User? CurrentUser { get; private set; }

        /// <summary>
        /// Last successful logical session identifier (GUID string).
        /// </summary>
        public string CurrentSessionId { get; private set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Current device forensic info: OS, host, user.
        /// </summary>
        public string CurrentDeviceInfo =>
            $"OS={Environment.OSVersion}, Host={Environment.MachineName}, User={Environment.UserName}";

        /// <summary>
        /// Current local IP address if <c>IPlatformService</c> is available; otherwise <c>"unknown"</c>.
        /// </summary>
        public string CurrentIpAddress =>
            DependencyService.Get<IPlatformService>()?.GetLocalIpAddress() ?? "unknown";

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
        /// <para>
        /// Returns the <see cref="User"/> on success; <c>null</c> otherwise. All attempts are audited.
        /// </para>
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="password">Plain-text password.</param>
        /// <returns>The authenticated <see cref="User"/> or <c>null</c>.</returns>
        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                await _auditService.LogSystemEventAsync("LOGIN_FAILED", $"Empty credentials for user: {username}");
                return null;
            }

            try
            {
                // Fetch user from DB through UserService (keeps a single source of truth for hashing/lock policies)
                var user = await _userService.GetUserByUsernameAsync(username);
                if (user == null || !user.Active)
                {
                    await _auditService.LogSystemEventAsync("LOGIN_FAILED", $"Non-existent or deactivated user: {username}");
                    return null;
                }

                // Password check (SHA-256 Base64 as per seeding)
                string hashedInput = HashPassword(password);
                if (!string.Equals(user.PasswordHash, hashedInput, StringComparison.Ordinal))
                {
                    await _auditService.LogSystemEventAsync("LOGIN_FAILED", $"Incorrect password for user: {username}");
                    return null;
                }

                // Success: record session & device/IP footprint
                CurrentUser = user;
                CurrentSessionId = Guid.NewGuid().ToString();

                await _auditService.LogSystemEventAsync(
                    "LOGIN_SUCCESS",
                    $"User {username} logged in. Device: {CurrentDeviceInfo}, IP: {CurrentIpAddress}, Session: {CurrentSessionId}"
                );

                return user;
            }
            catch (Exception ex)
            {
                await _auditService.LogSystemEventAsync("LOGIN_ERROR", $"Login error for user {username}: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region === PASSWORD MANAGEMENT ===

        /// <summary>
        /// Computes the SHA-256 hash of <paramref name="password"/> and returns it as Base64.
        /// </summary>
        public static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Verifies a clear-text password against a stored Base64 SHA-256 hash.
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash) =>
            string.Equals(HashPassword(password), storedHash, StringComparison.Ordinal);

        #endregion

        #region === ACCOUNT SECURITY ===

        /// <summary>
        /// Soft-locks (deactivates) a user account and writes an audit record.
        /// </summary>
        /// <param name="userId">Affected user identifier.</param>
        /// <param name="reason">Reason for the lockout.</param>
        public async Task LockUserAccountAsync(int userId, string reason)
        {
            await _userService.DeactivateUserAsync(userId);
            await _auditService.LogSystemEventAsync("ACCOUNT_LOCKED", $"User ID={userId} locked. Reason: {reason}");
        }

        #endregion
    }
}
