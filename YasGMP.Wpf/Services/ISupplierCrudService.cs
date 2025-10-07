using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Shared contract that drives supplier CRUD through <see cref="YasGMP.Services.SupplierService"/> and the MAUI audit stack.
/// </summary>
/// <remarks>
/// Module view models call into this interface on the dispatcher thread; adapters forward the work to
/// <see cref="YasGMP.Services.SupplierService"/> and <see cref="YasGMP.Services.DatabaseService"/> so MAUI and WPF persist and
/// audit suppliers the same way. Await the asynchronous operations off the UI thread and dispatch UI updates via
/// <see cref="WpfUiDispatcher"/>. <see cref="CrudSaveResult"/> instances must carry identifiers, status text, and signature metadata
/// so localization can be applied through <see cref="LocalizationServiceExtensions"/> or <see cref="ILocalizationService"/> before
/// the UI presents the values, and so <see cref="YasGMP.Services.AuditService"/> exposes complete context.
/// </remarks>
public interface ISupplierCrudService
{
    Task<IReadOnlyList<Supplier>> GetAllAsync();

    Task<Supplier?> TryGetByIdAsync(int id);

    /// <summary>
    /// Persists a new supplier and returns the saved identifier with captured signature metadata.
    /// </summary>
    Task<CrudSaveResult> CreateAsync(Supplier supplier, SupplierCrudContext context);

    /// <summary>
    /// Updates an existing supplier and returns the signature metadata that was persisted.
    /// </summary>
    Task<CrudSaveResult> UpdateAsync(Supplier supplier, SupplierCrudContext context);

    void Validate(Supplier supplier);

    string NormalizeStatus(string? status);
}

/// <summary>
/// Metadata captured when persisting supplier edits to feed audit/trace data.
/// </summary>
/// <param name="UserId">Authenticated operator identifier.</param>
/// <param name="Ip">Source IP captured from the current session.</param>
/// <param name="DeviceInfo">Device or workstation fingerprint.</param>
/// <param name="SessionId">Logical session identifier.</param>
/// <param name="SignatureId">Database identifier for the captured signature, when available.</param>
/// <param name="SignatureHash">Hash generated for the digital signature.</param>
/// <param name="SignatureMethod">Method used for signature authentication.</param>
/// <param name="SignatureStatus">Status of the signature (valid/revoked, etc.).</param>
/// <param name="SignatureNote">Reason or note captured alongside the signature.</param>
public readonly record struct SupplierCrudContext(
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

    public static SupplierCrudContext Create(int userId, string? ip, string? deviceInfo, string? sessionId)
        => new(
            userId <= 0 ? 1 : userId,
            string.IsNullOrWhiteSpace(ip) ? "unknown" : ip!,
            string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo!,
            string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId,
            null,
            null,
            DefaultSignatureMethod,
            DefaultSignatureStatus,
            null);

    public static SupplierCrudContext Create(
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

/// <summary>
/// Helper extensions for supplier metadata transformations.
/// </summary>
public static class SupplierCrudExtensions
{
    /// <summary>Normalises supplier status strings to lower-case tokens.</summary>
    public static string NormalizeStatusDefault(string? status)
        => string.IsNullOrWhiteSpace(status)
            ? "active"
            : status.Trim().ToLower(CultureInfo.InvariantCulture);
}
