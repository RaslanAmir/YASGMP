using System;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Adapter that bridges the WPF shell to the domain <see cref="WorkOrderService"/>.
/// </summary>
public sealed class WorkOrderCrudServiceAdapter : IWorkOrderCrudService
{
    private readonly WorkOrderService _service;

    public WorkOrderCrudServiceAdapter(WorkOrderService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public async Task<WorkOrder?> TryGetByIdAsync(int id)
    {
        try
        {
            return await _service.GetByIdAsync(id).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async Task<int> CreateAsync(WorkOrder workOrder, WorkOrderCrudContext context)
    {
        if (workOrder is null)
        {
            throw new ArgumentNullException(nameof(workOrder));
        }

        Validate(workOrder);
        await _service.CreateAsync(workOrder, context.UserId).ConfigureAwait(false);
        return workOrder.Id;
    }

    public async Task UpdateAsync(WorkOrder workOrder, WorkOrderCrudContext context)
    {
        if (workOrder is null)
        {
            throw new ArgumentNullException(nameof(workOrder));
        }

        Validate(workOrder);
        await _service.UpdateAsync(workOrder, context.UserId).ConfigureAwait(false);
    }

    public void Validate(WorkOrder workOrder)
    {
        if (workOrder is null)
        {
            throw new ArgumentNullException(nameof(workOrder));
        }

        if (string.IsNullOrWhiteSpace(workOrder.Title))
        {
            throw new InvalidOperationException("Work order title is required.");
        }

        if (string.IsNullOrWhiteSpace(workOrder.Description))
        {
            throw new InvalidOperationException("Work order description is required.");
        }

        if (string.IsNullOrWhiteSpace(workOrder.Type))
        {
            throw new InvalidOperationException("Work order type is required.");
        }

        if (workOrder.MachineId <= 0)
        {
            throw new InvalidOperationException("Work order must reference a machine.");
        }

        if (workOrder.CreatedById <= 0)
        {
            throw new InvalidOperationException("Work order must record the creator.");
        }
    }
}
