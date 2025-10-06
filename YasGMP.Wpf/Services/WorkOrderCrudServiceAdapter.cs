using System;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.DTO;
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

        public async Task<CrudSaveResult> CreateAsync(WorkOrder workOrder, WorkOrderCrudContext context)
        {
            if (workOrder is null)
            {
                throw new ArgumentNullException(nameof(workOrder));
            }

            Validate(workOrder);

            var signature = ApplyContext(workOrder, context);
            var metadata = CreateMetadata(context, signature);

            await _service.CreateAsync(workOrder, context.UserId, metadata).ConfigureAwait(false);

            workOrder.DigitalSignature = signature;
            return new CrudSaveResult(workOrder.Id, metadata);
        }

        public async Task<CrudSaveResult> UpdateAsync(WorkOrder workOrder, WorkOrderCrudContext context)
        {
            if (workOrder is null)
            {
                throw new ArgumentNullException(nameof(workOrder));
            }

            Validate(workOrder);

            var signature = ApplyContext(workOrder, context);
            var metadata = CreateMetadata(context, signature);

            await _service.UpdateAsync(workOrder, context.UserId, metadata).ConfigureAwait(false);

            workOrder.DigitalSignature = signature;
            return new CrudSaveResult(workOrder.Id, metadata);
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

    private static string ApplyContext(WorkOrder workOrder, WorkOrderCrudContext context)
    {
        var signature = context.SignatureHash ?? workOrder.DigitalSignature ?? string.Empty;
        workOrder.DigitalSignature = signature;

        if (context.UserId > 0)
        {
            workOrder.LastModifiedById = context.UserId;
        }

        if (!string.IsNullOrWhiteSpace(context.DeviceInfo))
        {
            workOrder.DeviceInfo = context.DeviceInfo;
        }

        if (!string.IsNullOrWhiteSpace(context.Ip))
        {
            workOrder.SourceIp = context.Ip;
        }

        if (!string.IsNullOrWhiteSpace(context.SessionId))
        {
            workOrder.SessionId = context.SessionId!;
        }

        return signature;
    }

    private static SignatureMetadataDto CreateMetadata(WorkOrderCrudContext context, string signature)
        => new()
        {
            Id = context.SignatureId,
            Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
            Method = context.SignatureMethod,
            Status = context.SignatureStatus,
            Note = context.SignatureNote,
            Session = context.SessionId,
            Device = context.DeviceInfo,
            IpAddress = context.Ip
        };
}

