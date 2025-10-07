using System;
using System.Threading.Tasks;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Models;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Shared contract consumed by the WPF shell and MAUI host to orchestrate Work Order persistence
/// through <see cref="YasGMP.Services.WorkOrderService"/> and the shared audit infrastructure.
/// </summary>
/// <remarks>
/// Module view models call these members on the dispatcher thread, adapters forward the work to
/// <see cref="YasGMP.Services.WorkOrderService"/> and <see cref="YasGMP.Services.AuditService"/>, and callers must
/// marshal UI updates back through <see cref="WpfUiDispatcher"/> after awaiting the result. Implementations are responsible
/// for returning a <see cref="CrudSaveResult"/> containing identifiers, signature context, and localization-ready status text
/// so MAUI and WPF shells emit identical audit logs and can localize notes through
/// <see cref="LocalizationServiceExtensions"/> or <see cref="ILocalizationService"/>.
/// </remarks>
public interface IWorkOrderCrudService
{
    Task<WorkOrder?> TryGetByIdAsync(int id);

    /// <summary>
    /// Persists a new work order and returns the saved identifier with signature metadata.
    /// </summary>
    Task<CrudSaveResult> CreateAsync(WorkOrder workOrder, WorkOrderCrudContext context);

    /// <summary>
    /// Updates an existing work order and returns the signature metadata that was persisted.
    /// </summary>
    Task<CrudSaveResult> UpdateAsync(WorkOrder workOrder, WorkOrderCrudContext context);

    void Validate(WorkOrder workOrder);
}

/// <summary>
/// Context metadata captured when persisting work-order edits so audit trails receive consistent identifiers. Each value feeds
/// <see cref="CrudSaveResult.SignatureMetadata"/> via <see cref="SignatureMetadataDto"/> to preserve the accepted signature
/// manifest for compliance pipelines.
/// </summary>
/// <remarks>
/// Adapters hydrate <see cref="SignatureMetadataDto"/> from this context before returning <see cref="CrudSaveResult"/>.
/// WPF shell consumers must persist and surface the DTO beside work orders, and MAUI experiences should propagate the same
/// payload when presenting or synchronizing records to keep shared audit history aligned.
/// </remarks>
/// <param name="UserId">Authenticated user identifier.</param>
/// <param name="Ip">Source IP captured from the current session.</param>
/// <param name="DeviceInfo">Machine fingerprint or hostname.</param>
/// <param name="SessionId">Logical application session identifier.</param>
/// <param name="SignatureId">Database identifier of the captured signature.</param>
/// <param name="SignatureHash">Hash persisted with the captured signature.</param>
/// <param name="SignatureMethod">Method used to authenticate the signature.</param>
/// <param name="SignatureStatus">Status describing the signature validity.</param>
/// <param name="SignatureNote">Operator note/reason captured during signing.</param>
public readonly record struct WorkOrderCrudContext(
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

    public static WorkOrderCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
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
    /// <summary>
    /// Executes the create operation.
    /// </summary>

    public static WorkOrderCrudContext Create(
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
