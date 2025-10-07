using System;
using System.Threading.Tasks;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Models;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Shared contract that routes incident CRUD operations through <see cref="YasGMP.Services.IncidentService"/> and the shared MAUI audit stack.
/// </summary>
/// <remarks>
/// Module view models call these members on the dispatcher thread, adapters forward the work to the shared
/// <see cref="YasGMP.Services.IncidentService"/> and <see cref="YasGMP.Services.AuditService"/>, and callers marshal UI updates with
/// <see cref="WpfUiDispatcher"/> after awaiting the asynchronous work. Implementations must return <see cref="CrudSaveResult"/> values populated with identifiers,
/// signature context, and localization-ready status text so <see cref="LocalizationServiceExtensions"/> or <see cref="ILocalizationService"/> can translate
/// them consistently with the MAUI shell.
/// </remarks>
public interface IIncidentCrudService
{
    Task<Incident?> TryGetByIdAsync(int id);

    /// <summary>
    /// Persists a new incident and returns the saved identifier with signature metadata.
    /// </summary>
    Task<CrudSaveResult> CreateAsync(Incident incident, IncidentCrudContext context);

    /// <summary>
    /// Updates an existing incident and returns the signature metadata captured during persistence.
    /// </summary>
    Task<CrudSaveResult> UpdateAsync(Incident incident, IncidentCrudContext context);

    void Validate(Incident incident);

    string NormalizeStatus(string? status);
}

/// <summary>
/// Captures the authenticated context required when persisting incident changes. Each value flows into
/// <see cref="CrudSaveResult.SignatureMetadata"/> via <see cref="SignatureMetadataDto"/> so audit and compliance
/// pipelines receive the accepted signature manifest.
/// </summary>
/// <remarks>
/// Adapters materialize <see cref="SignatureMetadataDto"/> from this context before returning
/// <see cref="CrudSaveResult"/>. Consumers in the WPF shell must persist the DTO alongside incident records and
/// surface the signature manifest in inspection panes, while MAUI screens should propagate the same payload when
/// displaying or synchronizing the record to keep the shared audit history aligned.
/// </remarks>
/// <param name="UserId">Authenticated user identifier.</param>
/// <param name="Ip">Source IP address recorded for audit trails.</param>
/// <param name="DeviceInfo">Client device identifier.</param>
/// <param name="SessionId">Logical session id.</param>
/// <param name="SignatureId">Database identifier for the captured signature.</param>
/// <param name="SignatureHash">Hash produced for the captured signature.</param>
/// <param name="SignatureMethod">Method used to authenticate the signature.</param>
/// <param name="SignatureStatus">Status describing the signature validity.</param>
/// <param name="SignatureNote">Reason or note captured during signing.</param>
public readonly record struct IncidentCrudContext(
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

    public static IncidentCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
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

    public static IncidentCrudContext Create(
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
