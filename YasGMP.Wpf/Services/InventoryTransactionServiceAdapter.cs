using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Services;
using YasGMP.Wpf.ViewModels.Dialogs;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Bridges the stock transaction dialog with the shared database inventory extensions
/// while capturing audit and signature metadata for the WPF shell.
/// </summary>
public sealed class InventoryTransactionServiceAdapter : IInventoryTransactionService
{
    private readonly DatabaseService _database;
    private readonly AuditService _auditService;
    private readonly IElectronicSignatureDialogService _signatureDialog;

    public InventoryTransactionServiceAdapter(
        DatabaseService database,
        AuditService auditService,
        IElectronicSignatureDialogService signatureDialog)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
    }

    public async Task<InventoryTransactionResult> ExecuteAsync(
        InventoryTransactionRequest request,
        InventoryTransactionContext context,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Quantity), "Quantity must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(context.Signature);
        ArgumentNullException.ThrowIfNull(context.Signature.Signature);

        var userId = context.EnsureUserId();
        var ip = context.EnsureIp();
        var device = context.EnsureDevice();
        var session = context.EnsureSession();
        var signatureResult = context.Signature;

        switch (request.Type)
        {
            case InventoryTransactionType.Receive:
                await _database.ReceiveStockAsync(
                    request.PartId,
                    request.WarehouseId,
                    request.Quantity,
                    userId,
                    request.Document,
                    request.Note,
                    ip,
                    device,
                    session,
                    cancellationToken).ConfigureAwait(false);
                break;
            case InventoryTransactionType.Issue:
                await _database.IssueStockAsync(
                    request.PartId,
                    request.WarehouseId,
                    request.Quantity,
                    userId,
                    request.Document,
                    request.Note,
                    ip,
                    device,
                    session,
                    cancellationToken).ConfigureAwait(false);
                break;
            case InventoryTransactionType.Adjust:
                if (!request.AdjustmentDelta.HasValue)
                {
                    throw new InvalidOperationException("Adjustment transactions require a delta value.");
                }

                var reason = string.IsNullOrWhiteSpace(request.AdjustmentReason)
                    ? request.Note
                    : request.AdjustmentReason;
                if (string.IsNullOrWhiteSpace(reason))
                {
                    reason = "Manual adjustment";
                }

                await _database.AdjustStockAsync(
                    request.PartId,
                    request.WarehouseId,
                    request.AdjustmentDelta.Value,
                    reason!,
                    userId,
                    ip,
                    device,
                    session,
                    cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.Type), request.Type, "Unsupported inventory transaction type.");
        }

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "inventory_transactions",
            recordId: request.PartId,
            metadata: null,
            fallbackSignatureHash: signatureResult.Signature!.SignatureHash,
            fallbackMethod: signatureResult.Signature.Method,
            fallbackStatus: signatureResult.Signature.Status,
            fallbackNote: signatureResult.Signature.Note,
            signedAt: signatureResult.Signature.SignedAt,
            fallbackDeviceInfo: device,
            fallbackIpAddress: ip,
            fallbackSessionId: session);

        await SignaturePersistenceHelper
            .PersistIfRequiredAsync(_signatureDialog, signatureResult, cancellationToken)
            .ConfigureAwait(false);

        await _auditService.LogSystemEventAsync(
            userId,
            "STOCK_TRANSACTION_SIGNATURE",
            "inventory_transactions",
            "Inventory",
            request.PartId,
            string.Format(
                CultureInfo.InvariantCulture,
                "type={0};warehouse={1};qty={2};doc={3}",
                request.Type,
                request.WarehouseId,
                request.Quantity,
                string.IsNullOrWhiteSpace(request.Document) ? "-" : request.Document),
            ip,
            "wpf",
            device,
            session,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new InventoryTransactionResult
        {
            Type = request.Type,
            PartId = request.PartId,
            WarehouseId = request.WarehouseId,
            Quantity = request.Quantity,
            Document = request.Document,
            Note = request.Note,
            Signature = signatureResult
        };
    }
}
