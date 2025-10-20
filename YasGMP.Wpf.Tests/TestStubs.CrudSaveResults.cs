using System;
using System.Linq;
using System.Threading.Tasks;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Wpf.Services;

namespace YasGMP.Models
{
    public sealed partial class FakeMachineCrudService
    {
        public Func<int?, int?>? SignatureMetadataIdSource { get; set; }

        public async Task<CrudSaveResult> CreateAsync(Machine machine, MachineCrudContext context)
        {
            var id = await CreateCoreAsync(machine, context).ConfigureAwait(false);
            return new CrudSaveResult(id, BuildMetadata(context, machine.DigitalSignature));
        }

        public async Task<CrudSaveResult> UpdateAsync(Machine machine, MachineCrudContext context)
        {
            await UpdateCoreAsync(machine, context).ConfigureAwait(false);
            return new CrudSaveResult(machine.Id, BuildMetadata(context, machine.DigitalSignature));
        }

        public Task DeleteAsync(int id, MachineCrudContext context)
        {
            var existing = _store.FirstOrDefault(machine => machine.Id == id);
            if (existing is not null)
            {
                _store.Remove(existing);
            }

            TrackDeletion(id, context);
            return Task.CompletedTask;
        }

        private SignatureMetadataDto BuildMetadata(MachineCrudContext context, string? signature)
            => new()
            {
                Id = ResolveMetadataId(context.SignatureId),
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.Ip
            };

        private int? ResolveMetadataId(int? contextSignatureId)
            => SignatureMetadataIdSource?.Invoke(contextSignatureId) ?? contextSignatureId;
    }

    public sealed partial class FakeComponentCrudService
    {
        public Func<int?, int?>? SignatureMetadataIdSource { get; set; }

        public async Task<CrudSaveResult> CreateAsync(Component component, ComponentCrudContext context)
        {
            var id = await CreateCoreAsync(component, context).ConfigureAwait(false);
            return new CrudSaveResult(id, BuildMetadata(context, component.DigitalSignature));
        }

        public async Task<CrudSaveResult> UpdateAsync(Component component, ComponentCrudContext context)
        {
            await UpdateCoreAsync(component, context).ConfigureAwait(false);
            return new CrudSaveResult(component.Id, BuildMetadata(context, component.DigitalSignature));
        }

        private SignatureMetadataDto BuildMetadata(ComponentCrudContext context, string? signature)
            => new()
            {
                Id = ResolveMetadataId(context.SignatureId),
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.Ip
            };

        private int? ResolveMetadataId(int? contextSignatureId)
            => SignatureMetadataIdSource?.Invoke(contextSignatureId) ?? contextSignatureId;
    }

    public sealed partial class FakeCalibrationCrudService
    {
        public Func<int?, int?>? SignatureMetadataIdSource { get; set; }

        public async Task<CrudSaveResult> CreateAsync(Calibration calibration, CalibrationCrudContext context)
        {
            var id = await CreateCoreAsync(calibration, context).ConfigureAwait(false);
            return new CrudSaveResult(id, BuildMetadata(context, calibration.DigitalSignature));
        }

        public async Task<CrudSaveResult> UpdateAsync(Calibration calibration, CalibrationCrudContext context)
        {
            await UpdateCoreAsync(calibration, context).ConfigureAwait(false);
            return new CrudSaveResult(calibration.Id, BuildMetadata(context, calibration.DigitalSignature));
        }

        private SignatureMetadataDto BuildMetadata(CalibrationCrudContext context, string? signature)
            => new()
            {
                Id = ResolveMetadataId(context.SignatureId),
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.Ip
            };

        private int? ResolveMetadataId(int? contextSignatureId)
            => SignatureMetadataIdSource?.Invoke(contextSignatureId) ?? contextSignatureId;
    }

    public sealed partial class FakeIncidentCrudService
    {
        public Func<int?, int?>? SignatureMetadataIdSource { get; set; }

        public async Task<CrudSaveResult> CreateAsync(Incident incident, IncidentCrudContext context)
        {
            var id = await CreateCoreAsync(incident, context).ConfigureAwait(false);
            return new CrudSaveResult(id, BuildMetadata(context, incident.DigitalSignature));
        }

        public async Task<CrudSaveResult> UpdateAsync(Incident incident, IncidentCrudContext context)
        {
            await UpdateCoreAsync(incident, context).ConfigureAwait(false);
            return new CrudSaveResult(incident.Id, BuildMetadata(context, incident.DigitalSignature));
        }

        private SignatureMetadataDto BuildMetadata(IncidentCrudContext context, string? signature)
            => new()
            {
                Id = ResolveMetadataId(context.SignatureId),
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.Ip
            };

        private int? ResolveMetadataId(int? contextSignatureId)
            => SignatureMetadataIdSource?.Invoke(contextSignatureId) ?? contextSignatureId;
    }

    public sealed partial class FakeChangeControlCrudService
    {
        public Func<int?, int?>? SignatureMetadataIdSource { get; set; }

        public async Task<CrudSaveResult> CreateAsync(ChangeControl changeControl, ChangeControlCrudContext context)
        {
            var id = await CreateCoreAsync(changeControl, context).ConfigureAwait(false);
            return new CrudSaveResult(id, BuildMetadata(context, changeControl.DigitalSignature));
        }

        public async Task<CrudSaveResult> UpdateAsync(ChangeControl changeControl, ChangeControlCrudContext context)
        {
            await UpdateCoreAsync(changeControl, context).ConfigureAwait(false);
            return new CrudSaveResult(changeControl.Id, BuildMetadata(context, changeControl.DigitalSignature));
        }

        private SignatureMetadataDto BuildMetadata(ChangeControlCrudContext context, string? signature)
            => new()
            {
                Id = ResolveMetadataId(context.SignatureId),
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.IpAddress
            };

        private int? ResolveMetadataId(int? contextSignatureId)
            => SignatureMetadataIdSource?.Invoke(contextSignatureId) ?? contextSignatureId;
    }

    public sealed partial class FakeCapaCrudService
    {
        public Func<int?, int?>? SignatureMetadataIdSource { get; set; }

        public async Task<CrudSaveResult> CreateAsync(CapaCase capa, CapaCrudContext context)
        {
            var id = await CreateCoreAsync(capa, context).ConfigureAwait(false);
            return new CrudSaveResult(id, BuildMetadata(context, capa.DigitalSignature));
        }

        public async Task<CrudSaveResult> UpdateAsync(CapaCase capa, CapaCrudContext context)
        {
            await UpdateCoreAsync(capa, context).ConfigureAwait(false);
            return new CrudSaveResult(capa.Id, BuildMetadata(context, capa.DigitalSignature));
        }

        private SignatureMetadataDto BuildMetadata(CapaCrudContext context, string? signature)
            => new()
            {
                Id = ResolveMetadataId(context.SignatureId),
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.Ip
            };

        private int? ResolveMetadataId(int? contextSignatureId)
            => SignatureMetadataIdSource?.Invoke(contextSignatureId) ?? contextSignatureId;
    }

    public sealed partial class FakeValidationCrudService
    {
        public Func<int?, int?>? SignatureMetadataIdSource { get; set; }

        public async Task<CrudSaveResult> CreateAsync(Validation validation, ValidationCrudContext context)
        {
            var id = await CreateCoreAsync(validation, context).ConfigureAwait(false);
            return new CrudSaveResult(id, BuildMetadata(context, validation.DigitalSignature));
        }

        public async Task<CrudSaveResult> UpdateAsync(Validation validation, ValidationCrudContext context)
        {
            await UpdateCoreAsync(validation, context).ConfigureAwait(false);
            return new CrudSaveResult(validation.Id, BuildMetadata(context, validation.DigitalSignature));
        }

        private SignatureMetadataDto BuildMetadata(ValidationCrudContext context, string? signature)
            => new()
            {
                Id = ResolveMetadataId(context.SignatureId),
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.Ip
            };

        private int? ResolveMetadataId(int? contextSignatureId)
            => SignatureMetadataIdSource?.Invoke(contextSignatureId) ?? contextSignatureId;
    }

    public sealed partial class FakePartCrudService
    {
        public Func<int?, int?>? SignatureMetadataIdSource { get; set; }

        public async Task<CrudSaveResult> CreateAsync(Part part, PartCrudContext context)
        {
            var id = await CreateCoreAsync(part, context).ConfigureAwait(false);
            return new CrudSaveResult(id, BuildMetadata(context, part.DigitalSignature));
        }

        public async Task<CrudSaveResult> UpdateAsync(Part part, PartCrudContext context)
        {
            await UpdateCoreAsync(part, context).ConfigureAwait(false);
            return new CrudSaveResult(part.Id, BuildMetadata(context, part.DigitalSignature));
        }

        private SignatureMetadataDto BuildMetadata(PartCrudContext context, string? signature)
            => new()
            {
                Id = ResolveMetadataId(context.SignatureId),
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.Ip
            };

        private int? ResolveMetadataId(int? contextSignatureId)
            => SignatureMetadataIdSource?.Invoke(contextSignatureId) ?? contextSignatureId;
    }

    public sealed partial class FakeSupplierCrudService
    {
        public Func<int?, int?>? SignatureMetadataIdSource { get; set; }

        public async Task<CrudSaveResult> CreateAsync(Supplier supplier, SupplierCrudContext context)
        {
            var id = await CreateCoreAsync(supplier, context).ConfigureAwait(false);
            return new CrudSaveResult(id, BuildMetadata(context, supplier.DigitalSignature));
        }

        public async Task<CrudSaveResult> UpdateAsync(Supplier supplier, SupplierCrudContext context)
        {
            await UpdateCoreAsync(supplier, context).ConfigureAwait(false);
            return new CrudSaveResult(supplier.Id, BuildMetadata(context, supplier.DigitalSignature));
        }

        private SignatureMetadataDto BuildMetadata(SupplierCrudContext context, string? signature)
            => new()
            {
                Id = ResolveMetadataId(context.SignatureId),
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.Ip
            };

        private int? ResolveMetadataId(int? contextSignatureId)
            => SignatureMetadataIdSource?.Invoke(contextSignatureId) ?? contextSignatureId;
    }

    public sealed partial class FakeWarehouseCrudService
    {
        public Func<int?, int?>? SignatureMetadataIdSource { get; set; }

        public async Task<CrudSaveResult> CreateAsync(Warehouse warehouse, WarehouseCrudContext context)
        {
            var id = await CreateCoreAsync(warehouse, context).ConfigureAwait(false);
            return new CrudSaveResult(id, BuildMetadata(context, warehouse.DigitalSignature));
        }

        public async Task<CrudSaveResult> UpdateAsync(Warehouse warehouse, WarehouseCrudContext context)
        {
            await UpdateCoreAsync(warehouse, context).ConfigureAwait(false);
            return new CrudSaveResult(warehouse.Id, BuildMetadata(context, warehouse.DigitalSignature));
        }

        private SignatureMetadataDto BuildMetadata(WarehouseCrudContext context, string? signature)
            => new()
            {
                Id = ResolveMetadataId(context.SignatureId),
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.Ip
            };

        private int? ResolveMetadataId(int? contextSignatureId)
            => SignatureMetadataIdSource?.Invoke(contextSignatureId) ?? contextSignatureId;
    }

    public sealed partial class FakeScheduledJobCrudService
    {
        public Func<int?, int?>? SignatureMetadataIdSource { get; set; }

        public async Task<CrudSaveResult> CreateAsync(ScheduledJob job, ScheduledJobCrudContext context)
        {
            var id = await CreateCoreAsync(job, context).ConfigureAwait(false);
            return new CrudSaveResult(id, BuildMetadata(context, job.DigitalSignature));
        }

        public async Task<CrudSaveResult> UpdateAsync(ScheduledJob job, ScheduledJobCrudContext context)
        {
            await UpdateCoreAsync(job, context).ConfigureAwait(false);
            return new CrudSaveResult(job.Id, BuildMetadata(context, job.DigitalSignature));
        }

        private SignatureMetadataDto BuildMetadata(ScheduledJobCrudContext context, string? signature)
            => new()
            {
                Id = ResolveMetadataId(context.SignatureId),
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.Ip
            };

        private int? ResolveMetadataId(int? contextSignatureId)
            => SignatureMetadataIdSource?.Invoke(contextSignatureId) ?? contextSignatureId;
    }

    public sealed partial class FakeUserCrudService
    {
        public Func<int?, int?>? SignatureMetadataIdSource { get; set; }

        public async Task<CrudSaveResult> CreateAsync(User user, string password, UserCrudContext context)
        {
            var id = await CreateCoreAsync(user, password, context).ConfigureAwait(false);
            return new CrudSaveResult(id, BuildMetadata(context, user.DigitalSignature));
        }

        public async Task<CrudSaveResult> UpdateAsync(User user, string? password, UserCrudContext context)
        {
            await UpdateCoreAsync(user, password, context).ConfigureAwait(false);
            return new CrudSaveResult(user.Id, BuildMetadata(context, user.DigitalSignature));
        }

        private SignatureMetadataDto BuildMetadata(UserCrudContext context, string? signature)
            => new()
            {
                Id = ResolveMetadataId(context.SignatureId),
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.Ip
            };

        private int? ResolveMetadataId(int? contextSignatureId)
            => SignatureMetadataIdSource?.Invoke(contextSignatureId) ?? contextSignatureId;
    }

    public sealed partial class FakeWorkOrderCrudService
    {
        public Func<int?, int?>? SignatureMetadataIdSource { get; set; }

        public async Task<CrudSaveResult> CreateAsync(WorkOrder workOrder, WorkOrderCrudContext context)
        {
            var id = await CreateCoreAsync(workOrder, context).ConfigureAwait(false);
            return new CrudSaveResult(id, BuildMetadata(context, workOrder.DigitalSignature));
        }

        public async Task<CrudSaveResult> UpdateAsync(WorkOrder workOrder, WorkOrderCrudContext context)
        {
            await UpdateCoreAsync(workOrder, context).ConfigureAwait(false);
            return new CrudSaveResult(workOrder.Id, BuildMetadata(context, workOrder.DigitalSignature));
        }

        private SignatureMetadataDto BuildMetadata(WorkOrderCrudContext context, string? signature)
            => new()
            {
                Id = ResolveMetadataId(context.SignatureId),
                Hash = string.IsNullOrWhiteSpace(signature) ? context.SignatureHash : signature,
                Method = context.SignatureMethod,
                Status = context.SignatureStatus,
                Note = context.SignatureNote,
                Session = context.SessionId,
                Device = context.DeviceInfo,
                IpAddress = context.Ip
            };

        private int? ResolveMetadataId(int? contextSignatureId)
            => SignatureMetadataIdSource?.Invoke(contextSignatureId) ?? contextSignatureId;
    }
}
