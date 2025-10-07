using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Shared contract used by the WPF shell and MAUI host to manage Change Control records through
/// <see cref="YasGMP.Services.ChangeControlService"/> while sharing audit plumbing.
/// </summary>
/// <remarks>
/// Module view models call these members synchronously on the dispatcher thread. Implementations relay the request to the
/// shared <see cref="YasGMP.Services.ChangeControlService"/> and <see cref="YasGMP.Services.AuditService"/> so both shells
/// persist and log identical metadata. Callers should dispatch UI updates via <see cref="WpfUiDispatcher"/> after awaiting the
/// asynchronous operations. Returned <see cref="CrudSaveResult"/> values must include identifiers, signature data, and
/// localization-ready status text for consumption with <see cref="LocalizationServiceExtensions"/> or
/// <see cref="ILocalizationService"/>.
/// </remarks>
public interface IChangeControlCrudService
{
    Task<IReadOnlyList<ChangeControl>> GetAllAsync();

    Task<ChangeControl?> TryGetByIdAsync(int id);

    /// <summary>
    /// Persists a new change control record and returns the saved identifier with signature metadata.
    /// </summary>
    Task<CrudSaveResult> CreateAsync(ChangeControl changeControl, ChangeControlCrudContext context);

    /// <summary>
    /// Updates an existing change control record and returns the signature metadata captured during persistence.
    /// </summary>
    Task<CrudSaveResult> UpdateAsync(ChangeControl changeControl, ChangeControlCrudContext context);

    void Validate(ChangeControl changeControl);

    string NormalizeStatus(string? status);
}

/// <summary>Context metadata propagated when persisting change control edits.</summary>
/// <param name="UserId">Authenticated user identifier.</param>
/// <param name="IpAddress">Source IP recorded for auditing.</param>
/// <param name="DeviceInfo">Originating device fingerprint.</param>
/// <param name="SessionId">Logical session identifier.</param>
/// <param name="SignatureId">Database identifier for the captured signature.</param>
/// <param name="SignatureHash">Hash persisted alongside the signature.</param>
/// <param name="SignatureMethod">Method used to authenticate the signature.</param>
/// <param name="SignatureStatus">Status of the captured signature (valid/revoked).</param>
/// <param name="SignatureNote">Reason or note supplied during signing.</param>
public readonly record struct ChangeControlCrudContext(
    int UserId,
    string IpAddress,
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

    public static ChangeControlCrudContext Create(int userId, string? ip, string? device, string? sessionId)
        => new(
            userId <= 0 ? 1 : userId,
            string.IsNullOrWhiteSpace(ip) ? "unknown" : ip!,
            string.IsNullOrWhiteSpace(device) ? "WPF" : device!,
            string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId,
            null,
            null,
            DefaultSignatureMethod,
            DefaultSignatureStatus,
            null);

    public static ChangeControlCrudContext Create(
        int userId,
        string? ip,
        string? device,
        string? sessionId,
        ElectronicSignatureDialogResult signatureResult)
    {
        ArgumentNullException.ThrowIfNull(signatureResult);
        ArgumentNullException.ThrowIfNull(signatureResult.Signature);

        var context = Create(userId, ip, device, sessionId);
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
