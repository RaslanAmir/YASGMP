using System;
using YasGMP.AppCore.Models.Signatures;

namespace YasGMP.Services;

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
/// <param name="Reason">Business justification for the change or impersonation session.</param>
/// <param name="Notes">Supplemental notes captured for the audit trail.</param>
public readonly record struct UserCrudContext(
    int UserId,
    string Ip,
    string DeviceInfo,
    string? SessionId,
    int? SignatureId,
    string? SignatureHash,
    string? SignatureMethod,
    string? SignatureStatus,
    string? SignatureNote,
    string? Reason,
    string? Notes)
{
    private const string DefaultSignatureMethod = "password";
    private const string DefaultSignatureStatus = "valid";

    /// <summary>
    /// Creates a context using basic session metadata when no electronic signature is required.
    /// </summary>
    public static UserCrudContext Create(
        int userId,
        string? ip,
        string? deviceInfo,
        string? sessionId,
        string? reason = null,
        string? notes = null)
        => new(
            userId <= 0 ? 1 : userId,
            string.IsNullOrWhiteSpace(ip) ? "unknown" : ip!,
            string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo!,
            string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId,
            null,
            null,
            DefaultSignatureMethod,
            DefaultSignatureStatus,
            null,
            string.IsNullOrWhiteSpace(reason) ? null : reason,
            string.IsNullOrWhiteSpace(notes) ? null : notes);

    /// <summary>
    /// Creates a context using the supplied electronic signature result.
    /// </summary>
    public static UserCrudContext Create(
        int userId,
        string? ip,
        string? deviceInfo,
        string? sessionId,
        ElectronicSignatureDialogResult signatureResult,
        string? reason = null,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(signatureResult);
        ArgumentNullException.ThrowIfNull(signatureResult.Signature);

        var context = Create(userId, ip, deviceInfo, sessionId, reason, notes);
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
