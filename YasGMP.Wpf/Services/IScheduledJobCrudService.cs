using System;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter-friendly abstraction for scheduled job persistence so the WPF shell can
/// operate without binding directly to database-specific infrastructure.
/// </summary>
public interface IScheduledJobCrudService
{
    Task<ScheduledJob?> TryGetByIdAsync(int id);

    /// <summary>
    /// Persists a new scheduled job and returns the saved identifier with signature metadata.
    /// </summary>
    Task<CrudSaveResult> CreateAsync(ScheduledJob job, ScheduledJobCrudContext context);

    /// <summary>
    /// Updates an existing scheduled job and returns the signature metadata captured during persistence.
    /// </summary>
    Task<CrudSaveResult> UpdateAsync(ScheduledJob job, ScheduledJobCrudContext context);

    Task ExecuteAsync(int jobId, ScheduledJobCrudContext context);

    Task AcknowledgeAsync(int jobId, ScheduledJobCrudContext context);

    void Validate(ScheduledJob job);

    string NormalizeStatus(string? status);
}

/// <summary>
/// Ambient metadata required for auditing scheduled job operations.
/// </summary>
/// <param name="UserId">Authenticated user identifier.</param>
/// <param name="UserName">Display/user name captured for audit manifest.</param>
/// <param name="Ip">Source IP address.</param>
/// <param name="DeviceInfo">Originating device information.</param>
/// <param name="SessionId">Logical session identifier.</param>
/// <param name="SignatureId">Database identifier for the captured signature.</param>
/// <param name="SignatureHash">Hash generated for the signature payload.</param>
/// <param name="SignatureMethod">Method used to authenticate the signature.</param>
/// <param name="SignatureStatus">Status describing the signature validity.</param>
/// <param name="SignatureNote">Reason or note captured during signing.</param>
public readonly record struct ScheduledJobCrudContext(
    int UserId,
    string UserName,
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

    public static ScheduledJobCrudContext Create(int userId, string? userName, string ip, string deviceInfo, string? sessionId)
        => new(
            userId <= 0 ? 1 : userId,
            string.IsNullOrWhiteSpace(userName) ? $"user:{(userId <= 0 ? 1 : userId)}" : userName.Trim(),
            string.IsNullOrWhiteSpace(ip) ? "unknown" : ip,
            string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo,
            string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId,
            null,
            null,
            DefaultSignatureMethod,
            DefaultSignatureStatus,
            null);

    public static ScheduledJobCrudContext Create(
        int userId,
        string? userName,
        string ip,
        string deviceInfo,
        string? sessionId,
        ElectronicSignatureDialogResult signatureResult)
    {
        ArgumentNullException.ThrowIfNull(signatureResult);
        ArgumentNullException.ThrowIfNull(signatureResult.Signature);

        var context = Create(userId, userName, ip, deviceInfo, sessionId);
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

