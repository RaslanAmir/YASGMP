using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Adapter abstraction that surfaces Parts CRUD operations to the WPF shell
    /// without binding the UI to the concrete infrastructure implementation.
    /// </summary>
    public interface IPartCrudService
    {
        Task<IReadOnlyList<Part>> GetAllAsync();

        Task<Part?> TryGetByIdAsync(int id);

        Task<int> CreateAsync(Part part, PartCrudContext context);

        Task UpdateAsync(Part part, PartCrudContext context);

        void Validate(Part part);

        string NormalizeStatus(string? status);
    }

    /// <summary>
    /// Metadata required when persisting parts for audit purposes.
    /// </summary>
    /// <param name="UserId">Authenticated operator identifier.</param>
    /// <param name="Ip">Source IP captured by the auth context.</param>
    /// <param name="DeviceInfo">Device fingerprint or hostname.</param>
/// <param name="SessionId">Logical session identifier.</param>
/// <param name="SignatureId">Database identifier for the captured digital signature.</param>
/// <param name="SignatureHash">Hash generated for the signature payload.</param>
/// <param name="SignatureMethod">Method used to authenticate the signature.</param>
/// <param name="SignatureStatus">Status describing the signature validity.</param>
/// <param name="SignatureNote">Reason or justification captured during signing.</param>
public readonly record struct PartCrudContext(
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

    public static PartCrudContext Create(int userId, string ip, string deviceInfo, string? sessionId)
        => new(userId <= 0 ? 1 : userId,
               string.IsNullOrWhiteSpace(ip) ? "unknown" : ip,
               string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo,
               string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId,
               null,
               null,
               DefaultSignatureMethod,
               DefaultSignatureStatus,
               null);

    public static PartCrudContext Create(
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
}
