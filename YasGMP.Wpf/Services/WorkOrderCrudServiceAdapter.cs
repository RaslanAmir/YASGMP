using System;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Bridges Work Order module view models in the WPF shell to the shared MAUI
/// <see cref="YasGMP.Services.WorkOrderService"/> and related infrastructure.
/// </summary>
/// <remarks>
/// The WPF module view models call this adapter, which then invokes
/// <see cref="YasGMP.Services.WorkOrderService"/> and the shared <see cref="YasGMP.Services.AuditService"/>
/// so the persisted data and audit trail match the MAUI implementation. Operations should be awaited off the
/// dispatcher thread, with UI updates marshalled through <see cref="WpfUiDispatcher"/>. The <see cref="CrudSaveResult"/>
/// returned by save operations must propagate identifiers, status text, and signature metadata which callers localize through
/// <see cref="LocalizationServiceExtensions"/> or <see cref="ILocalizationService"/> before surfacing to operators.
/// </remarks>
public sealed class WorkOrderCrudServiceAdapter : IWorkOrderCrudService
{
    private readonly WorkOrderService _service;
    /// <summary>
    /// Initializes a new instance of the WorkOrderCrudServiceAdapter class.
    /// </summary>

    public WorkOrderCrudServiceAdapter(WorkOrderService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }
    /// <summary>
    /// Executes the try get by id async operation.
    /// </summary>

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
        /// <summary>
        /// Executes the create async operation.
        /// </summary>

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
        /// <summary>
        /// Executes the update async operation.
        /// </summary>

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
    /// <summary>
    /// Executes the validate operation.
    /// </summary>

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
