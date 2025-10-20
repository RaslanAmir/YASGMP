using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.Tests.TestDoubles;

public sealed class RecordingInventoryTransactionService : IInventoryTransactionService
{
    public sealed record Execution(
        InventoryTransactionRequest Request,
        InventoryTransactionContext Context,
        CancellationToken CancellationToken);

    private readonly List<Execution> _executions = new();

    public IReadOnlyList<Execution> Executions => _executions;

    public bool ShouldThrow { get; set; }

    public Exception ExceptionToThrow { get; set; } = new InvalidOperationException("Simulated inventory failure.");

    public Func<InventoryTransactionRequest, InventoryTransactionContext, InventoryTransactionResult>? ResultFactory { get; set; }

    public Task<InventoryTransactionResult> ExecuteAsync(
        InventoryTransactionRequest request,
        InventoryTransactionContext context,
        CancellationToken cancellationToken = default)
    {
        _executions.Add(new Execution(request, context, cancellationToken));

        if (ShouldThrow)
        {
            throw ExceptionToThrow;
        }

        var result = ResultFactory?.Invoke(request, context) ?? new InventoryTransactionResult
        {
            Type = request.Type,
            PartId = request.PartId,
            WarehouseId = request.WarehouseId,
            Quantity = request.AdjustmentDelta ?? request.Quantity,
            Document = request.Document,
            Note = request.Note,
            Signature = context.Signature
        };

        return Task.FromResult(result);
    }
}
