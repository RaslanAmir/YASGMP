using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter abstraction over <see cref="YasGMP.Services.CAPAService"/> so the WPF shell
/// can execute CAPA CRUD operations without binding directly to infrastructure types.
/// </summary>
public interface ICapaCrudService
{
    Task<IReadOnlyList<CapaCase>> GetAllAsync();

    Task<CapaCase?> TryGetByIdAsync(int id);

    /// <summary>
    /// Persists a new CAPA case and returns the saved identifier with signature metadata.
    /// </summary>
    Task<CrudSaveResult> CreateAsync(CapaCase capa, CapaCrudContext context);

    /// <summary>
    /// Updates an existing CAPA case and returns the signature metadata captured during persistence.
    /// </summary>
    Task<CrudSaveResult> UpdateAsync(CapaCase capa, CapaCrudContext context);

    void Validate(CapaCase capa);

    string NormalizeStatus(string? status);

    string NormalizePriority(string? priority);
}

/// <summary>
/// Captures authenticated metadata that must flow alongside CAPA saves.
/// </summary>
/// <param name="UserId">Authenticated operator identifier.</param>
/// <param name="Ip">Source IP recorded for audit.</param>
/// <param name="DeviceInfo">Client device fingerprint.</param>
/// <param name="SessionId">Logical session identifier.</param>
/// <param name="SignatureId">Database identifier of the captured signature.</param>
/// <param name="SignatureHash">Hash produced for the signature.</param>
/// <param name="SignatureMethod">Method used to authenticate the signature.</param>
/// <param name="SignatureStatus">Status describing the signature validity.</param>
/// <param name="SignatureNote">Reason or note entered during signing.</param>
public readonly record struct CapaCrudContext(
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

    public static CapaCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
        => new(
            userId <= 0 ? 1 : userId,
            string.IsNullOrWhiteSpace(ip) ? "unknown" : ip,
            string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo,
            string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId,
            null,
            null,
            DefaultSignatureMethod,
            DefaultSignatureStatus,
            null);

    public static CapaCrudContext Create(
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
