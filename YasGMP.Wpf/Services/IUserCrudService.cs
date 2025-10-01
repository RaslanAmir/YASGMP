using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Adapter-friendly abstraction that exposes user and role CRUD functionality to the WPF shell
    /// without binding directly to <see cref="YasGMP.Services.UserService"/> or <see cref="YasGMP.Services.RBACService"/>.
    /// </summary>
    public interface IUserCrudService
    {
        Task<IReadOnlyList<User>> GetAllAsync();

        Task<User?> TryGetByIdAsync(int id);

        Task<IReadOnlyList<Role>> GetAllRolesAsync();

        /// <summary>
        /// Persists a new user and returns the saved identifier with signature metadata.
        /// </summary>
        Task<CrudSaveResult> CreateAsync(User user, string password, UserCrudContext context);

        /// <summary>
        /// Updates an existing user and returns the signature metadata captured during persistence.
        /// </summary>
        Task<CrudSaveResult> UpdateAsync(User user, string? password, UserCrudContext context);

        Task UpdateRoleAssignmentsAsync(int userId, IReadOnlyCollection<int> roleIds, UserCrudContext context);

        Task DeactivateAsync(int userId, UserCrudContext context);

        void Validate(User user);
    }

    /// <summary>
    /// Ambient metadata captured when persisting security changes so audit trails include the actor and origin details.
    /// </summary>
    /// <param name="UserId">Authenticated operator identifier.</param>
    /// <param name="Ip">Source IP address captured from the session.</param>
    /// <param name="DeviceInfo">Device or workstation fingerprint.</param>
    /// <param name="SessionId">Logical session identifier for traceability.</param>
    /// <param name="SignatureId">Database identifier for the captured signature.</param>
    /// <param name="SignatureHash">Hash generated for the signature payload.</param>
    /// <param name="SignatureMethod">Method used to authenticate the signature.</param>
    /// <param name="SignatureStatus">Status describing the signature validity.</param>
    /// <param name="SignatureNote">Reason or note captured during signing.</param>
    public readonly record struct UserCrudContext(
        int UserId,
        string Ip,
        string DeviceInfo,
        string? SessionId,
        int? SignatureId,
        string? SignatureHash,
        string? SignatureMethod,
        string? SignatureStatus,
        string? SignatureNote)
    {
        private const string DefaultSignatureMethod = "password";
        private const string DefaultSignatureStatus = "valid";

        public static UserCrudContext Create(int userId, string? ip, string? deviceInfo, string? sessionId)
            => new(
                userId <= 0 ? 1 : userId,
                string.IsNullOrWhiteSpace(ip) ? "unknown" : ip!,
                string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo!,
                string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId,
                null,
                null,
                DefaultSignatureMethod,
                DefaultSignatureStatus,
                null);

        public static UserCrudContext Create(
            int userId,
            string? ip,
            string? deviceInfo,
            string? sessionId,
            ElectronicSignatureDialogResult signatureResult)
        {
            ArgumentNullException.ThrowIfNull(signatureResult);
            ArgumentNullException.ThrowIfNull(signatureResult.Signature);

            var context = Create(userId, ip, deviceInfo, sessionId);
            var signature = signatureResult.Signature;

            return context with
            {
                SignatureId = signature.Id > 0 ? signature.Id : null,
                SignatureHash = string.IsNullOrWhiteSpace(signature.SignatureHash) ? null : signature.SignatureHash,
                SignatureMethod = string.IsNullOrWhiteSpace(signature.Method) ? DefaultSignatureMethod : signature.Method,
                SignatureStatus = string.IsNullOrWhiteSpace(signature.Status) ? DefaultSignatureStatus : signature.Status,
                SignatureNote = !string.IsNullOrWhiteSpace(signature.Note)
                    ? signature.Note
                    : !string.IsNullOrWhiteSpace(signatureResult.ReasonDetail)
                        ? signatureResult.ReasonDetail
                        : signatureResult.ReasonCode
            };
        }
    }
}
