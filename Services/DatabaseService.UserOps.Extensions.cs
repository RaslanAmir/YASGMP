using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Extension helpers for <see cref="DatabaseService"/> related to User operations.
    /// <para>
    /// ⚠️ This file intentionally <b>does not</b> re-declare the canonical audit writer
    /// (<c>LogUserAuditAsync</c>) that already exists in
    /// <see cref="DatabaseServiceRbacExtensions"/>. Doing so prevents CS0121
    /// “ambiguous call” errors across view-models and services.
    /// </para>
    /// <para>
    /// What remains here are uniquely-named convenience wrappers that simply forward
    /// to the RBAC extensions. They’re safe to use from views/view-models without
    /// risking signature collisions.
    /// </para>
    /// </summary>
    public static class DatabaseServiceUserOpsExtensions
    {
        /// <summary>
        /// Convenience wrapper that returns all users via the canonical implementation
        /// in <see cref="DatabaseServiceRbacExtensions.GetAllUsersAsync(DatabaseService,System.Threading.CancellationToken)"/>.
        /// </summary>
        /// <param name="db">Database service instance.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of users.</returns>
        public static Task<List<User>> GetAllUsersBasicAsync(
            this DatabaseService db,
            CancellationToken token = default) =>
            DatabaseServiceRbacExtensions.GetAllUsersAsync(db, token);

        /// <summary>
        /// Convenience wrapper for adding a user with signature/audit context, delegating to
        /// <see cref="DatabaseServiceRbacExtensions.AddUserAsync(DatabaseService, User, string, string, string, string?, CancellationToken)"/>.
        /// </summary>
        /// <param name="db">Database service instance.</param>
        /// <param name="user">User to add.</param>
        /// <param name="signatureHash">Digital signature hash.</param>
        /// <param name="ip">Source IP.</param>
        /// <param name="deviceInfo">Device information.</param>
        /// <param name="sessionId">Optional session identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Newly created user ID.</returns>
        public static Task<int> AddUserWithAuditAsync(
            this DatabaseService db,
            User user,
            string signatureHash,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default) =>
            DatabaseServiceRbacExtensions.AddUserAsync(db, user, signatureHash, ip, deviceInfo, sessionId, token);

        /// <summary>
        /// Convenience wrapper for updating a user with signature/audit context, delegating to
        /// <see cref="DatabaseServiceRbacExtensions.UpdateUserAsync(DatabaseService, User, string, string, string, string?, CancellationToken)"/>.
        /// </summary>
        public static Task UpdateUserWithAuditAsync(
            this DatabaseService db,
            User user,
            string signatureHash,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default) =>
            DatabaseServiceRbacExtensions.UpdateUserAsync(db, user, signatureHash, ip, deviceInfo, sessionId, token);

        /// <summary>
        /// Convenience wrapper for deleting a user with signature/audit context, delegating to
        /// <see cref="DatabaseServiceRbacExtensions.DeleteUserAsync(DatabaseService, int, string, string, string?, CancellationToken)"/>.
        /// </summary>
        public static Task DeleteUserWithAuditAsync(
            this DatabaseService db,
            int userId,
            string ip,
            string deviceInfo,
            string? sessionId,
            CancellationToken token = default) =>
            DatabaseServiceRbacExtensions.DeleteUserAsync(db, userId, ip, deviceInfo, sessionId, token);
    }
}
