using System;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Contract that wraps inventory stock movements with audit and signature plumbing
/// for the WPF shell.
/// </summary>
public interface IInventoryTransactionService
{
    Task<InventoryTransactionResult> ExecuteAsync(
        InventoryTransactionRequest request,
        InventoryTransactionContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Inventory operation supported by the transaction dialog.
/// </summary>
public enum InventoryTransactionType
{
    Receive,
    Issue,
    Adjust
}

/// <summary>
/// Request payload gathered from the stock transaction dialog.
/// </summary>
public readonly record struct InventoryTransactionRequest(
    InventoryTransactionType Type,
    int PartId,
    int WarehouseId,
    int Quantity,
    string? Document,
    string? Note,
    int? AdjustmentDelta,
    string? AdjustmentReason)
{
    public static InventoryTransactionRequest CreateReceive(
        int partId,
        int warehouseId,
        int quantity,
        string? document,
        string? note)
        => new(InventoryTransactionType.Receive, partId, warehouseId, quantity, document, note, null, null);

    public static InventoryTransactionRequest CreateIssue(
        int partId,
        int warehouseId,
        int quantity,
        string? document,
        string? note)
        => new(InventoryTransactionType.Issue, partId, warehouseId, quantity, document, note, null, null);

    public static InventoryTransactionRequest CreateAdjustment(
        int partId,
        int warehouseId,
        int delta,
        string? document,
        string? note,
        string? reason)
        => new(InventoryTransactionType.Adjust, partId, warehouseId, Math.Abs(delta), document, note, delta, reason);
}

/// <summary>
/// Context captured from the authentication/audit pipeline that accompanies
/// inventory transactions.
/// </summary>
public readonly record struct InventoryTransactionContext(
    int UserId,
    string Ip,
    string Device,
    string? SessionId,
    ElectronicSignatureDialogResult Signature)
{
    public int EnsureUserId()
        => UserId <= 0 ? 1 : UserId;

    public string EnsureIp()
        => string.IsNullOrWhiteSpace(Ip) ? "unknown" : Ip;

    public string EnsureDevice()
        => string.IsNullOrWhiteSpace(Device) ? "WPF" : Device;

    public string EnsureSession()
        => string.IsNullOrWhiteSpace(SessionId) ? Guid.NewGuid().ToString("N") : SessionId!;
}

/// <summary>
/// Result surfaced back to the module so it can refresh alerts and reporting grids.
/// </summary>
public sealed class InventoryTransactionResult
{
    public InventoryTransactionType Type { get; init; }

    public int PartId { get; init; }

    public int WarehouseId { get; init; }

    public int Quantity { get; init; }

    public string? Document { get; init; }

    public string? Note { get; init; }

    public ElectronicSignatureDialogResult Signature { get; init; } = default!;
}
