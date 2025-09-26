using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter-friendly abstraction exposing supplier CRUD to the WPF shell without
/// binding directly to <see cref="YasGMP.Services.SupplierService"/>.
/// </summary>
public interface ISupplierCrudService
{
    Task<IReadOnlyList<Supplier>> GetAllAsync();

    Task<Supplier?> TryGetByIdAsync(int id);

    Task<int> CreateAsync(Supplier supplier, SupplierCrudContext context);

    Task UpdateAsync(Supplier supplier, SupplierCrudContext context);

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
public readonly record struct SupplierCrudContext(int UserId, string Ip, string DeviceInfo, string? SessionId)
{
    public static SupplierCrudContext Create(int userId, string? ip, string? deviceInfo, string? sessionId)
        => new(
            userId <= 0 ? 1 : userId,
            string.IsNullOrWhiteSpace(ip) ? "unknown" : ip!,
            string.IsNullOrWhiteSpace(deviceInfo) ? "WPF" : deviceInfo!,
            string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId);
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
