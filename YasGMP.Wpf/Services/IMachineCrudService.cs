using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Adapter-friendly abstraction around <see cref="YasGMP.Services.MachineService"/>
    /// so the WPF shell can be unit-tested without the full database infrastructure.
    /// </summary>
    public interface IMachineCrudService
    {
        Task<IReadOnlyList<Machine>> GetAllAsync();

        Task<Machine?> TryGetByIdAsync(int id);

        /// <summary>
        /// Persists a new machine and returns the saved identifier together with captured signature metadata.
        /// </summary>
        Task<CrudSaveResult> CreateAsync(Machine machine, MachineCrudContext context);

        /// <summary>
        /// Updates an existing machine and returns the updated signature metadata so callers can refresh UI state.
        /// </summary>
        Task<CrudSaveResult> UpdateAsync(Machine machine, MachineCrudContext context);

        void Validate(Machine machine);

        string NormalizeStatus(string? status);
    }

    /// <summary>
    /// Ambient metadata required for audit logging when persisting machines.
    /// </summary>
    /// <param name="UserId">Authenticated user identifier.</param>
    /// <param name="Ip">Source IP captured by the auth context.</param>
    /// <param name="DeviceInfo">Device fingerprint (Workstation name, etc.).</param>
    /// <param name="SessionId">Logical session identifier.</param>
    /// <param name="SignatureId">Database identifier of the captured digital signature, when persisted.</param>
    /// <param name="SignatureHash">Hash associated with the captured signature.</param>
    /// <param name="SignatureMethod">Method used to capture the signature (password, cert, etc.).</param>
    /// <param name="SignatureStatus">Status of the captured signature (valid/revoked).</param>
    /// <param name="SignatureNote">Operator-supplied reason/note captured during signing.</param>
    public readonly record struct MachineCrudContext(
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

        public static MachineCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
            => new(userId <= 0 ? 1 : userId,
                   string.IsNullOrWhiteSpace(ip) ? "unknown" : ip,
                   string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo,
                   string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId,
                   null,
                   null,
                   DefaultSignatureMethod,
                   DefaultSignatureStatus,
                   null);

        public static MachineCrudContext Create(
            int userId,
            string ip,
            string deviceInfo,
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
