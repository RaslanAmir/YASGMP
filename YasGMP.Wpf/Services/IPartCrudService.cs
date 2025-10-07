using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Models;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Shared contract that lets both the WPF shell and the MAUI host drive Parts
    /// persistence through the <see cref="YasGMP.Services.PartService"/> pipeline.
    /// </summary>
    /// <remarks>
    /// Module view models invoke this interface on the UI thread, the adapter forwards the call to the
    /// shared MAUI services (<see cref="YasGMP.Services.PartService"/> and <see cref="YasGMP.Services.AuditService"/>)
    /// and callers must dispatch UI updates via <see cref="WpfUiDispatcher"/> once the awaited operation completes.
    /// The resulting <see cref="CrudSaveResult"/> must carry the identifier, session, and signature metadata so
    /// audit logs stay aligned across shells and any status or note text can be localized with
    /// <see cref="LocalizationServiceExtensions"/> or <see cref="ILocalizationService"/> before presentation.
    /// </remarks>
    public interface IPartCrudService
    {
        Task<IReadOnlyList<Part>> GetAllAsync();

        Task<Part?> TryGetByIdAsync(int id);

        /// <summary>
        /// Persists a new part and returns the saved identifier alongside captured signature metadata.
        /// </summary>
        Task<CrudSaveResult> CreateAsync(Part part, PartCrudContext context);

        /// <summary>
        /// Updates an existing part and returns the signature metadata that was persisted.
        /// </summary>
        Task<CrudSaveResult> UpdateAsync(Part part, PartCrudContext context);

        void Validate(Part part);

        string NormalizeStatus(string? status);
    }

    /// <summary>
    /// Metadata required when persisting parts for audit purposes. Each value flows into
    /// <see cref="CrudSaveResult.SignatureMetadata"/> via <see cref="SignatureMetadataDto"/> so compliance pipelines retain the
    /// accepted signature manifest.
    /// </summary>
    /// <remarks>
    /// Adapters hydrate <see cref="SignatureMetadataDto"/> from this record before returning <see cref="CrudSaveResult"/>.
    /// WPF shell consumers must persist and surface the DTO beside part records, and MAUI experiences should propagate the
    /// same payload when presenting or synchronizing parts so shared audit history remains aligned.
    /// </remarks>
    /// <param name="UserId">Authenticated operator identifier.</param>
    /// <param name="Ip">Source IP captured by the auth context.</param>
    /// <param name="DeviceInfo">Device fingerprint or hostname.</param>
/// <param name="SessionId">Logical session identifier.</param>
/// <param name="SignatureId">Database identifier for the captured digital signature.</param>
/// <param name="SignatureHash">Hash generated for the signature payload.</param>
/// <param name="SignatureMethod">Method used to authenticate the signature.</param>
/// <param name="SignatureStatus">Status describing the signature validity.</param>
/// <param name="SignatureNote">Reason or justification captured during signing.</param>
public readonly record struct PartCrudContext(
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
    /// <summary>
    /// Executes the create operation.
    /// </summary>

    public static PartCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
        => new(userId <= 0 ? 1 : userId,
               string.IsNullOrWhiteSpace(ip) ? "unknown" : ip,
               string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo,
               string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId,
               null,
               null,
               DefaultSignatureMethod,
               DefaultSignatureStatus,
               null);
    /// <summary>
    /// Executes the create operation.
    /// </summary>

    public static PartCrudContext Create(
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
