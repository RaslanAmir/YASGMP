using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Models;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Shared contract that allows the WPF shell and MAUI client to orchestrate Component
/// persistence through <see cref="YasGMP.Services.ComponentService"/>.
/// </summary>
/// <remarks>
/// Module view models call into this interface, the adapter forwards requests to the shared
/// MAUI services (<see cref="YasGMP.Services.ComponentService"/> and <see cref="YasGMP.Services.AuditService"/>),
/// and UI updates should be dispatched via <see cref="WpfUiDispatcher"/> after awaiting the returned tasks.
/// Implementations must return a <see cref="CrudSaveResult"/> populated with identifiers, status text, and
/// signature metadata so audit records remain in sync and localization can be applied via
/// <see cref="LocalizationServiceExtensions"/> or <see cref="ILocalizationService"/> prior to presentation.
/// </remarks>
public interface IComponentCrudService
{
    Task<IReadOnlyList<Component>> GetAllAsync();

    Task<Component?> TryGetByIdAsync(int id);

    /// <summary>
    /// Persists a new <paramref name="component"/> and returns the saved identifier together with
    /// the captured signature metadata so callers can refresh editor state.
    /// </summary>
    Task<CrudSaveResult> CreateAsync(Component component, ComponentCrudContext context);

    /// <summary>
    /// Updates an existing <paramref name="component"/> and returns the signature metadata that was
    /// persisted so the caller can surface the latest signing details.
    /// </summary>
    Task<CrudSaveResult> UpdateAsync(Component component, ComponentCrudContext context);

    void Validate(Component component);

    string NormalizeStatus(string? status);
}

/// <summary>
/// Context metadata captured when persisting component edits so audit trails and downstream services receive consistent
/// identifiers. Each value is forwarded into <see cref="CrudSaveResult.SignatureMetadata"/> via <see cref="SignatureMetadataDto"/>
/// to preserve the accepted signature manifest for compliance pipelines.
/// </summary>
/// <remarks>
/// Adapters hydrate <see cref="SignatureMetadataDto"/> from this record before returning <see cref="CrudSaveResult"/>.
/// WPF shell consumers must persist and surface the DTO beside component records, and MAUI experiences should propagate the
/// same payload when presenting or synchronizing components so shared audit history stays aligned.
/// </remarks>
/// <param name="UserId">Authenticated user identifier.</param>
/// <param name="Ip">Source IP captured from the current session.</param>
/// <param name="DeviceInfo">Machine fingerprint or hostname.</param>
/// <param name="SessionId">Logical application session identifier.</param>
/// <param name="SignatureId">Database identifier for the captured digital signature.</param>
/// <param name="SignatureHash">Hash associated with the digital signature.</param>
/// <param name="SignatureMethod">Method used to capture the signature.</param>
/// <param name="SignatureStatus">Status of the signature at capture time.</param>
/// <param name="SignatureNote">Operator-provided reason captured with the signature.</param>
public readonly record struct ComponentCrudContext(
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

    public static ComponentCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
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

    public static ComponentCrudContext Create(
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
