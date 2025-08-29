using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;

/// <summary>
/// <b>IUserService</b> – Interface for ultra-robust, GMP-compliant user management.
/// <para>Supports authentication, secure password hashing, CRUD, role/permission checks, digital signatures, 2FA, and audit logging.</para>
/// </summary>
namespace YasGMP.Services.Interfaces
{
    public interface IUserService
    {
        // === AUTHENTICATION & SECURITY ===

        /// <summary>
        /// Authenticates a user using secure GMP/CSV-compliant logic.
        /// Returns <c>null</c> when authentication fails.
        /// </summary>
        /// <param name="username">Username (unique).</param>
        /// <param name="password">Plaintext password to verify.</param>
        /// <returns>The authenticated <see cref="User"/> or <c>null</c>.</returns>
        Task<User?> AuthenticateAsync(string username, string password);

        /// <summary>
        /// GMP-compliant SHA256 password hashing.
        /// </summary>
        /// <param name="password">Plaintext password.</param>
        /// <returns>Base64-encoded SHA256 hash.</returns>
        string HashPassword(string password);

        /// <summary>
        /// Two-factor code verification hook.
        /// </summary>
        /// <param name="username">User to verify.</param>
        /// <param name="code">One-time code.</param>
        /// <returns><c>true</c> if code is valid; otherwise <c>false</c>.</returns>
        Task<bool> VerifyTwoFactorCodeAsync(string username, string code);

        /// <summary>
        /// Locks out the user after repeated failed attempts.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        Task LockUserAsync(int userId);

        // === CRUD OPERATIONS ===

        /// <summary>Returns all users (authorization is implementation-specific).</summary>
        Task<List<User>> GetAllUsersAsync();

        /// <summary>
        /// Retrieves a user by identifier.
        /// Returns <c>null</c> when not found.
        /// </summary>
        Task<User?> GetUserByIdAsync(int id);

        /// <summary>
        /// Retrieves a user by username.
        /// Returns <c>null</c> when not found.
        /// </summary>
        Task<User?> GetUserByUsernameAsync(string username);

        /// <summary>Creates a new user.</summary>
        Task CreateUserAsync(User user, int adminId = 0);

        /// <summary>Updates an existing user.</summary>
        Task UpdateUserAsync(User user, int adminId = 0);

        /// <summary>Deletes a user by identifier.</summary>
        Task DeleteUserAsync(int userId, int adminId = 0);

        /// <summary>Deactivates (soft-disables) a user.</summary>
        Task DeactivateUserAsync(int userId);

        // === ROLE, PERMISSIONS, PROFILE ===

        /// <summary>Checks if the user holds a role (case-insensitive).</summary>
        bool HasRole(User user, string role);

        /// <summary>Determines whether the user account is active.</summary>
        bool IsActive(User user);

        /// <summary>Changes the user's password with proper authorization.</summary>
        Task ChangePasswordAsync(int userId, string newPassword, int adminId = 0);

        // === DIGITAL SIGNATURES & GMP COMPLIANCE ===

        /// <summary>Generates a digital signature for the user’s profile snapshot.</summary>
        string GenerateDigitalSignature(User user);

        /// <summary>Validates a digital signature string.</summary>
        bool ValidateDigitalSignature(string signature);

        // === GMP/CSV/21 CFR PART 11 BONUS EXTENSIONS ===

        /// <summary>Writes a free-form user-related audit record.</summary>
        Task LogUserEventAsync(int userId, string eventType, string details);

        /// <summary>Unlocks a user (admin).</summary>
        Task UnlockUserAsync(int userId, int adminId);

        /// <summary>Enables or disables two-factor authentication for the user.</summary>
        Task SetTwoFactorEnabledAsync(int userId, bool enabled);

        /// <summary>Updates a user profile with auditing.</summary>
        Task UpdateUserProfileAsync(User user, int adminId = 0);
    }
}
