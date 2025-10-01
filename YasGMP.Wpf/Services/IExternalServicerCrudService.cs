using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter-friendly abstraction exposing external servicer CRUD operations to the WPF shell.
/// </summary>
public interface IExternalServicerCrudService
{
    Task<IReadOnlyList<ExternalServicer>> GetAllAsync();

    Task<ExternalServicer?> TryGetByIdAsync(int id);

    /// <summary>
    /// Persists a new external servicer and returns the saved identifier with signature metadata.
    /// </summary>
    Task<CrudSaveResult> CreateAsync(ExternalServicer servicer, ExternalServicerCrudContext context);

    /// <summary>
    /// Updates an existing external servicer and returns the signature metadata captured during persistence.
    /// </summary>
    Task<CrudSaveResult> UpdateAsync(ExternalServicer servicer, ExternalServicerCrudContext context);

    Task DeleteAsync(int id, ExternalServicerCrudContext context);

    void Validate(ExternalServicer servicer);

    string NormalizeStatus(string? status);
}

/// <summary>
/// Metadata captured when persisting external servicer edits to feed audit/trace data.
/// </summary>
/// <param name="UserId">Authenticated operator identifier.</param>
/// <param name="Ip">Source IP captured from the current session.</param>
/// <param name="DeviceInfo">Device or workstation fingerprint.</param>
/// <param name="SessionId">Logical session identifier.</param>
/// <param name="SignatureId">Database identifier for the captured signature.</param>
/// <param name="SignatureHash">Hash recorded for the signature.</param>
/// <param name="SignatureMethod">Method used to authenticate the signature.</param>
/// <param name="SignatureStatus">Status describing the signature validity.</param>
/// <param name="SignatureNote">Reason or note captured during signing.</param>
public readonly record struct ExternalServicerCrudContext(
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

    public static ExternalServicerCrudContext Create(int userId, string? ip, string? deviceInfo, string? sessionId)
        => new(
            userId <= 0 ? 1 : userId,
            string.IsNullOrWhiteSpace(ip) ? "unknown" : ip!,
            string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo!,
            string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) : sessionId,
            null,
            null,
            DefaultSignatureMethod,
            DefaultSignatureStatus,
            null);

    public static ExternalServicerCrudContext Create(
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

/// <summary>Helper extensions for external servicer metadata transformations.</summary>
public static class ExternalServicerCrudExtensions
{
    /// <summary>Normalises external servicer status strings to lower-case tokens.</summary>
    public static string NormalizeStatusDefault(string? status)
        => string.IsNullOrWhiteSpace(status)
            ? "active"
            : status.Trim().ToLower(CultureInfo.InvariantCulture);
}
