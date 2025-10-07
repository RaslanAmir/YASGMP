using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Shared CRUD contract used by both shells to persist calibrations through
/// <see cref="YasGMP.Services.CalibrationService"/> and the MAUI audit infrastructure.
/// </summary>
/// <remarks>
/// Module view models invoke this interface on the dispatcher thread, implementations relay work to the shared
/// <see cref="YasGMP.Services.CalibrationService"/> and <see cref="YasGMP.Services.AuditService"/>, and UI consumers must marshal
/// updates with <see cref="WpfUiDispatcher"/> once operations complete. The returned <see cref="CrudSaveResult"/> is expected to
/// include identifiers, signature data, and localization-ready status/notes so MAUI and WPF present consistent audit history and
/// can translate the text using <see cref="LocalizationServiceExtensions"/> or <see cref="ILocalizationService"/>.
/// </remarks>
public interface ICalibrationCrudService
{
    Task<IReadOnlyList<Calibration>> GetAllAsync();

    Task<Calibration?> TryGetByIdAsync(int id);

    /// <summary>
    /// Persists a new calibration entry and returns the saved identifier with signature metadata.
    /// </summary>
    Task<CrudSaveResult> CreateAsync(Calibration calibration, CalibrationCrudContext context);

    /// <summary>
    /// Updates an existing calibration entry and returns the captured signature metadata.
    /// </summary>
    Task<CrudSaveResult> UpdateAsync(Calibration calibration, CalibrationCrudContext context);

    void Validate(Calibration calibration);
}

/// <summary>
/// Context metadata captured when persisting calibrations to support downstream audit logging.
/// </summary>
/// <param name="UserId">Authenticated user identifier.</param>
/// <param name="Ip">Source IP captured from the current auth context.</param>
/// <param name="DeviceInfo">Machine fingerprint/hostname.</param>
/// <param name="SessionId">Logical application session identifier.</param>
/// <param name="SignatureId">Database identifier for the captured digital signature.</param>
/// <param name="SignatureHash">Hash generated for the captured signature.</param>
/// <param name="SignatureMethod">Method used to capture/verify the signature.</param>
/// <param name="SignatureStatus">Status describing the signature validity.</param>
/// <param name="SignatureNote">Reason or justification supplied during signing.</param>
public readonly record struct CalibrationCrudContext(
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

    public static CalibrationCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
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

    public static CalibrationCrudContext Create(
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
