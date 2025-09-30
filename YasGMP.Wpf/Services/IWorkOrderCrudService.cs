using System;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Abstraction over the <see cref="YasGMP.Services.WorkOrderService"/> so the WPF shell
/// can execute CRUD operations in a testable manner without connecting to the full
/// database runtime.
/// </summary>
public interface IWorkOrderCrudService
{
    Task<WorkOrder?> TryGetByIdAsync(int id);

    Task<int> CreateAsync(WorkOrder workOrder, WorkOrderCrudContext context);

    Task UpdateAsync(WorkOrder workOrder, WorkOrderCrudContext context);

    void Validate(WorkOrder workOrder);
}

/// <summary>
/// Context metadata captured when persisting work-order edits so audit trails receive
/// consistent identifiers.
/// </summary>
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
