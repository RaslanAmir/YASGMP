using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace YasGMP.Models
{
    public class Asset
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Model { get; set; }
        public string? Manufacturer { get; set; }
        public string? Location { get; set; }
        public string? Status { get; set; }
        public DateTime? InstallDate { get; set; }
    }

    public class Component
    {
        public int Id { get; set; }
        public int MachineId { get; set; }
        public string? MachineName { get; set; }
        public string? Code { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? SopDoc { get; set; }
        public string? Status { get; set; }
        public DateTime? InstallDate { get; set; }
        public string? SerialNumber { get; set; }
        public string? Supplier { get; set; }
        public DateTime? WarrantyUntil { get; set; }
        public string? Comments { get; set; }
        public string? LifecycleState { get; set; }
    }

    public class WorkOrder
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TaskDescription { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DateOpen { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }
        public DateTime? DateClose { get; set; }
        public int RequestedById { get; set; }
        public int CreatedById { get; set; }
        public int AssignedToId { get; set; }
        public int MachineId { get; set; }
        public int? ComponentId { get; set; }
        public string Result { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string DigitalSignature { get; set; } = string.Empty;
        public User? AssignedTo { get; set; }
        public Machine? Machine { get; set; }
    }

    public class Incident
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Priority { get; set; }
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReportedAt { get; set; }
        public int? ReportedById { get; set; }
        public int? AssignedToId { get; set; }
        public int? WorkOrderId { get; set; }
        public int? CapaCaseId { get; set; }
        public string Status { get; set; } = "REPORTED";
        public string? RootCause { get; set; }
        public DateTime? ClosedAt { get; set; }
        public int? ClosedById { get; set; }
        public string? AssignedInvestigator { get; set; }
        public string? Classification { get; set; }
        public int? LinkedDeviationId { get; set; }
        public int? LinkedCapaId { get; set; }
        public string? ClosureComment { get; set; }
        public string? SourceIp { get; set; }
        public string? Notes { get; set; }
        public bool IsCritical { get; set; }
        public int RiskLevel { get; set; }
        public double? AnomalyScore { get; set; }
    }

    public class CapaCase
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime DateOpen { get; set; } = DateTime.UtcNow;
    }

    public class User
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Username { get; set; }
    }

    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Part
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public int? Stock { get; set; }
        public int? MinStockAlert { get; set; }
        public string? Location { get; set; }
        public int? DefaultSupplierId { get; set; }
        public string DefaultSupplierName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public decimal? Price { get; set; }
    }

    public class Machine
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Model { get; set; }
        public string? Manufacturer { get; set; }
        public string? Location { get; set; }
        public string? Status { get; set; }
        public string? UrsDoc { get; set; }
        public DateTime? InstallDate { get; set; }
        public DateTime? ProcurementDate { get; set; }
        public DateTime? WarrantyUntil { get; set; }
        public bool IsCritical { get; set; }
        public string? SerialNumber { get; set; }
        public string? LifecyclePhase { get; set; }
        public string? Note { get; set; }
    }

    public class Attachment
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? EntityTable { get; set; }
        public int? EntityId { get; set; }
        public string? FileType { get; set; }
        public string? Status { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AttachmentLink
    {
        public int Id { get; set; }
        public string? EntityType { get; set; }
        public int EntityId { get; set; }
    }

    public class RetentionPolicy
    {
        public string? PolicyName { get; set; }
        public DateTime? RetainUntil { get; set; }
    }

    public class Calibration
    {
        public int Id { get; set; }
        public int ComponentId { get; set; }
        public int? SupplierId { get; set; }
        public DateTime CalibrationDate { get; set; }
        public DateTime NextDue { get; set; }
        public string CertDoc { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class Warehouse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string LegacyResponsibleName { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
        public string ClimateMode { get; set; } = string.Empty;
        public bool IsQualified { get; set; } = true;
        public DateTime? LastQualified { get; set; }
        public string DigitalSignature { get; set; } = string.Empty;
    }
}

namespace YasGMP.Services
{
    using YasGMP.Models;

    public class DatabaseService
    {
        public List<Asset> Assets { get; } = new();
        public List<Component> Components { get; } = new();
        public List<WorkOrder> WorkOrders { get; } = new();
        public List<Calibration> Calibrations { get; } = new();
        public List<Supplier> Suppliers { get; } = new();
        public List<Part> Parts { get; } = new();
        public List<Warehouse> Warehouses { get; } = new();
        public List<Incident> Incidents { get; } = new();
        public List<CapaCase> CapaCases { get; } = new();

        public Task<List<Asset>> GetAllAssetsFullAsync()
            => Task.FromResult(Assets);

        public Task<List<Component>> GetAllComponentsAsync()
            => Task.FromResult(Components);

        public Task<List<WorkOrder>> GetAllWorkOrdersFullAsync()
            => Task.FromResult(WorkOrders);

        public Task<List<Calibration>> GetAllCalibrationsAsync()
            => Task.FromResult(Calibrations);

        public Task<List<Incident>> GetAllIncidentsAsync()
            => Task.FromResult(Incidents);

        public Task<List<CapaCase>> GetAllCapaCasesAsync()
            => Task.FromResult(CapaCases);

        public Task<List<Supplier>> GetAllSuppliersAsync()
            => Task.FromResult(Suppliers);

        public Task<List<Warehouse>> GetWarehousesAsync()
            => Task.FromResult(Warehouses);
    }

    public sealed class TestFilePicker : IFilePicker
    {
        public IReadOnlyList<PickedFile> Files { get; set; } = Array.Empty<PickedFile>();

        public Task<IReadOnlyList<PickedFile>> PickFilesAsync(FilePickerRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(Files);
    }
}

namespace YasGMP.Services.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using YasGMP.Models;

    public interface IAuthContext
    {
        User? CurrentUser { get; }
        string CurrentSessionId { get; }
        string CurrentDeviceInfo { get; }
        string CurrentIpAddress { get; }
    }

    public sealed class TestAuthContext : IAuthContext
    {
        public User? CurrentUser { get; set; }
        public string CurrentSessionId { get; set; } = Guid.NewGuid().ToString("N");
        public string CurrentDeviceInfo { get; set; } = "TestRig";
        public string CurrentIpAddress { get; set; } = "127.0.0.1";
    }

    public sealed class TestAttachmentService : IAttachmentService
    {
        private int _nextId = 1;

        public List<AttachmentUploadRequest> Uploads { get; } = new();

        public Task<AttachmentUploadResult> UploadAsync(Stream content, AttachmentUploadRequest request, CancellationToken token = default)
        {
            Uploads.Add(request);
            var attachment = new Attachment
            {
                Id = _nextId++,
                FileName = request.FileName,
                EntityTable = request.EntityType,
                EntityId = request.EntityId
            };
            var link = new AttachmentLink
            {
                Id = attachment.Id,
                EntityType = request.EntityType,
                EntityId = request.EntityId
            };
            var retention = new RetentionPolicy
            {
                PolicyName = request.RetentionPolicyName,
                RetainUntil = request.RetainUntil
            };
            return Task.FromResult(new AttachmentUploadResult(attachment, link, retention));
        }

        public Task<Attachment?> FindByHashAsync(string sha256, CancellationToken token = default)
            => Task.FromResult<Attachment?>(null);

        public Task<Attachment?> FindByHashAndSizeAsync(string sha256, long fileSize, CancellationToken token = default)
            => Task.FromResult<Attachment?>(null);

        public Task<AttachmentStreamResult> StreamContentAsync(int attachmentId, Stream destination, AttachmentReadRequest? request = null, CancellationToken token = default)
            => Task.FromResult(new AttachmentStreamResult(new Attachment { Id = attachmentId }, 0, 0, false, request));

        public Task<IReadOnlyList<AttachmentLinkWithAttachment>> GetLinksForEntityAsync(string entityType, int entityId, CancellationToken token = default)
            => Task.FromResult<IReadOnlyList<AttachmentLinkWithAttachment>>(Array.Empty<AttachmentLinkWithAttachment>());

        public Task RemoveLinkAsync(int linkId, CancellationToken token = default)
            => Task.CompletedTask;

        public Task RemoveLinkAsync(string entityType, int entityId, int attachmentId, CancellationToken token = default)
            => Task.CompletedTask;
    }
}

namespace YasGMP.Wpf.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using YasGMP.Wpf.ViewModels.Modules;

    public interface ICflDialogService
    {
        Task<CflResult?> ShowAsync(CflRequest request);
    }

    public interface IShellInteractionService
    {
        void UpdateStatus(string message);

        void UpdateInspector(InspectorContext context);
    }

    public interface IModuleNavigationService
    {
        ModuleDocumentViewModel OpenModule(string moduleKey, object? parameter = null);

        void Activate(ModuleDocumentViewModel document);
    }

    public sealed class CflRequest
    {
        public CflRequest(string title, IReadOnlyList<CflItem> items)
        {
            Title = title;
            Items = items;
        }

        public string Title { get; }

        public IReadOnlyList<CflItem> Items { get; }
    }

    public sealed class CflItem
    {
        public CflItem(string key, string label, string? description = null)
        {
            Key = key;
            Label = label;
            Description = description ?? string.Empty;
        }

        public string Key { get; }

        public string Label { get; }

        public string Description { get; }
    }

    public sealed class CflResult
    {
        public CflResult(CflItem selected)
        {
            Selected = selected;
        }

        public CflItem Selected { get; }
    }

    public sealed class FakeMachineCrudService : IMachineCrudService
    {
        private readonly List<Machine> _store = new();

        public List<Machine> Saved => _store;

        public Task<IReadOnlyList<Machine>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Machine>>(_store.ToList());

        public Task<Machine?> TryGetByIdAsync(int id)
            => Task.FromResult<Machine?>(_store.FirstOrDefault(m => m.Id == id));

        public Task<int> CreateAsync(Machine machine, MachineCrudContext context)
        {
            if (machine.Id == 0)
            {
                machine.Id = _store.Count == 0 ? 1 : _store.Max(m => m.Id) + 1;
            }
            _store.Add(Clone(machine));
            return Task.FromResult(machine.Id);
        }

        public Task UpdateAsync(Machine machine, MachineCrudContext context)
        {
            var existing = _store.FirstOrDefault(m => m.Id == machine.Id);
            if (existing is null)
            {
                _store.Add(Clone(machine));
            }
            else
            {
                Copy(machine, existing);
            }

            return Task.CompletedTask;
        }

        public void Validate(Machine machine)
        {
            if (string.IsNullOrWhiteSpace(machine.Name))
                throw new InvalidOperationException("Name is required.");
            if (string.IsNullOrWhiteSpace(machine.Code))
                throw new InvalidOperationException("Code is required.");
            if (string.IsNullOrWhiteSpace(machine.Manufacturer))
                throw new InvalidOperationException("Manufacturer is required.");
            if (string.IsNullOrWhiteSpace(machine.Location))
                throw new InvalidOperationException("Location is required.");
            if (string.IsNullOrWhiteSpace(machine.UrsDoc))
                throw new InvalidOperationException("URS document is required.");
        }

        public string NormalizeStatus(string? status)
            => string.IsNullOrWhiteSpace(status) ? "active" : status.Trim().ToLowerInvariant();

        private static Machine Clone(Machine source)
        {
            return new Machine
            {
                Id = source.Id,
                Code = source.Code,
                Name = source.Name,
                Description = source.Description,
                Model = source.Model,
                Manufacturer = source.Manufacturer,
                Location = source.Location,
                Status = source.Status,
                UrsDoc = source.UrsDoc,
                InstallDate = source.InstallDate,
                ProcurementDate = source.ProcurementDate,
                WarrantyUntil = source.WarrantyUntil,
                IsCritical = source.IsCritical,
                SerialNumber = source.SerialNumber,
                LifecyclePhase = source.LifecyclePhase,
                Note = source.Note
            };
        }

        private static void Copy(Machine source, Machine destination)
        {
            destination.Code = source.Code;
            destination.Name = source.Name;
            destination.Description = source.Description;
            destination.Model = source.Model;
            destination.Manufacturer = source.Manufacturer;
            destination.Location = source.Location;
            destination.Status = source.Status;
            destination.UrsDoc = source.UrsDoc;
            destination.InstallDate = source.InstallDate;
            destination.ProcurementDate = source.ProcurementDate;
            destination.WarrantyUntil = source.WarrantyUntil;
            destination.IsCritical = source.IsCritical;
            destination.SerialNumber = source.SerialNumber;
            destination.LifecyclePhase = source.LifecyclePhase;
            destination.Note = source.Note;
        }
    }

    public sealed class FakeIncidentCrudService : IIncidentCrudService
    {
        private readonly List<Incident> _store = new();

        public List<Incident> Saved => _store;

        public Task<Incident?> TryGetByIdAsync(int id)
            => Task.FromResult(_store.FirstOrDefault(i => i.Id == id));

        public Task<int> CreateAsync(Incident incident, IncidentCrudContext context)
        {
            if (incident.Id == 0)
            {
                incident.Id = _store.Count == 0 ? 1 : _store.Max(i => i.Id) + 1;
            }

            _store.Add(Clone(incident));
            return Task.FromResult(incident.Id);
        }

        public Task UpdateAsync(Incident incident, IncidentCrudContext context)
        {
            var existing = _store.FirstOrDefault(i => i.Id == incident.Id);
            if (existing is null)
            {
                _store.Add(Clone(incident));
            }
            else
            {
                Copy(incident, existing);
            }

            return Task.CompletedTask;
        }

        public void Validate(Incident incident)
        {
            if (string.IsNullOrWhiteSpace(incident.Title))
                throw new InvalidOperationException("Incident title is required.");
            if (string.IsNullOrWhiteSpace(incident.Description))
                throw new InvalidOperationException("Incident description is required.");
        }

        public string NormalizeStatus(string? status)
            => string.IsNullOrWhiteSpace(status) ? "REPORTED" : status.Trim().ToUpperInvariant();

        private static Incident Clone(Incident source)
            => new()
            {
                Id = source.Id,
                Title = source.Title,
                Description = source.Description,
                Type = source.Type,
                Priority = source.Priority,
                DetectedAt = source.DetectedAt,
                ReportedAt = source.ReportedAt,
                ReportedById = source.ReportedById,
                AssignedToId = source.AssignedToId,
                WorkOrderId = source.WorkOrderId,
                CapaCaseId = source.CapaCaseId,
                Status = source.Status,
                RootCause = source.RootCause,
                ClosedAt = source.ClosedAt,
                ClosedById = source.ClosedById,
                AssignedInvestigator = source.AssignedInvestigator,
                Classification = source.Classification,
                LinkedDeviationId = source.LinkedDeviationId,
                LinkedCapaId = source.LinkedCapaId,
                ClosureComment = source.ClosureComment,
                SourceIp = source.SourceIp,
                Notes = source.Notes,
                IsCritical = source.IsCritical,
                RiskLevel = source.RiskLevel,
                AnomalyScore = source.AnomalyScore
            };

        private static void Copy(Incident source, Incident destination)
        {
            destination.Title = source.Title;
            destination.Description = source.Description;
            destination.Type = source.Type;
            destination.Priority = source.Priority;
            destination.DetectedAt = source.DetectedAt;
            destination.ReportedAt = source.ReportedAt;
            destination.ReportedById = source.ReportedById;
            destination.AssignedToId = source.AssignedToId;
            destination.WorkOrderId = source.WorkOrderId;
            destination.CapaCaseId = source.CapaCaseId;
            destination.Status = source.Status;
            destination.RootCause = source.RootCause;
            destination.ClosedAt = source.ClosedAt;
            destination.ClosedById = source.ClosedById;
            destination.AssignedInvestigator = source.AssignedInvestigator;
            destination.Classification = source.Classification;
            destination.LinkedDeviationId = source.LinkedDeviationId;
            destination.LinkedCapaId = source.LinkedCapaId;
            destination.ClosureComment = source.ClosureComment;
            destination.SourceIp = source.SourceIp;
            destination.Notes = source.Notes;
            destination.IsCritical = source.IsCritical;
            destination.RiskLevel = source.RiskLevel;
            destination.AnomalyScore = source.AnomalyScore;
        }
    }

    public sealed class FakeCapaCrudService : ICapaCrudService
    {
        private readonly List<CapaCase> _store = new();

        public List<CapaCase> Saved => _store;

        public Task<IReadOnlyList<CapaCase>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<CapaCase>>(_store.ToList());

        public Task<CapaCase?> TryGetByIdAsync(int id)
            => Task.FromResult<CapaCase?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateAsync(CapaCase capa, CapaCrudContext context)
        {
            if (capa.Id == 0)
            {
                capa.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(capa));
            return Task.FromResult(capa.Id);
        }

        public Task UpdateAsync(CapaCase capa, CapaCrudContext context)
        {
            var existing = _store.FirstOrDefault(c => c.Id == capa.Id);
            if (existing is null)
            {
                _store.Add(Clone(capa));
            }
            else
            {
                Copy(capa, existing);
            }

            return Task.CompletedTask;
        }

        public void Validate(CapaCase capa)
        {
            if (string.IsNullOrWhiteSpace(capa.Title))
                throw new InvalidOperationException("CAPA title is required.");
            if (string.IsNullOrWhiteSpace(capa.Description))
                throw new InvalidOperationException("CAPA description is required.");
            if (capa.ComponentId <= 0)
                throw new InvalidOperationException("CAPA must reference a component.");
        }

        public string NormalizeStatus(string? status)
            => string.IsNullOrWhiteSpace(status) ? "OPEN" : status.Trim().ToUpperInvariant();

        public string NormalizePriority(string? priority)
            => string.IsNullOrWhiteSpace(priority) ? "Medium" : priority.Trim();

        public void Seed(CapaCase capa)
        {
            if (capa.Id == 0)
            {
                capa.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(capa));
        }

        private static CapaCase Clone(CapaCase source)
        {
            return new CapaCase
            {
                Id = source.Id,
                Title = source.Title,
                Description = source.Description,
                ComponentId = source.ComponentId,
                Priority = source.Priority,
                Status = source.Status,
                RootCause = source.RootCause,
                CorrectiveAction = source.CorrectiveAction,
                PreventiveAction = source.PreventiveAction,
                Reason = source.Reason,
                Actions = source.Actions,
                Notes = source.Notes,
                Comments = source.Comments,
                DateOpen = source.DateOpen,
                DateClose = source.DateClose,
                AssignedToId = source.AssignedToId,
                DigitalSignature = source.DigitalSignature
            };
        }

        private static void Copy(CapaCase source, CapaCase destination)
        {
            destination.Title = source.Title;
            destination.Description = source.Description;
            destination.ComponentId = source.ComponentId;
            destination.Priority = source.Priority;
            destination.Status = source.Status;
            destination.RootCause = source.RootCause;
            destination.CorrectiveAction = source.CorrectiveAction;
            destination.PreventiveAction = source.PreventiveAction;
            destination.Reason = source.Reason;
            destination.Actions = source.Actions;
            destination.Notes = source.Notes;
            destination.Comments = source.Comments;
            destination.DateOpen = source.DateOpen;
            destination.DateClose = source.DateClose;
            destination.AssignedToId = source.AssignedToId;
            destination.DigitalSignature = source.DigitalSignature;
        }
    }

    public sealed class FakeComponentCrudService : IComponentCrudService
    {
        private readonly List<Component> _store = new();

        public List<Component> Saved => _store;

        public Task<IReadOnlyList<Component>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Component>>(_store.ToList());

        public Task<Component?> TryGetByIdAsync(int id)
            => Task.FromResult<Component?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateAsync(Component component, ComponentCrudContext context)
        {
            if (component.Id == 0)
            {
                component.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(component));
            return Task.FromResult(component.Id);
        }

        public Task UpdateAsync(Component component, ComponentCrudContext context)
        {
            var existing = _store.FirstOrDefault(c => c.Id == component.Id);
            if (existing is null)
            {
                _store.Add(Clone(component));
            }
            else
            {
                Copy(component, existing);
            }

            return Task.CompletedTask;
        }

        public void Validate(Component component)
        {
            if (string.IsNullOrWhiteSpace(component.Name))
                throw new InvalidOperationException("Component name is required.");
            if (string.IsNullOrWhiteSpace(component.Code))
                throw new InvalidOperationException("Component code is required.");
            if (component.MachineId <= 0)
                throw new InvalidOperationException("Component must be linked to a machine.");
            if (string.IsNullOrWhiteSpace(component.SopDoc))
                throw new InvalidOperationException("SOP document is required.");
        }

        public string NormalizeStatus(string? status)
            => string.IsNullOrWhiteSpace(status) ? "active" : status.Trim().ToLowerInvariant();

        private static Component Clone(Component source)
        {
            return new Component
            {
                Id = source.Id,
                MachineId = source.MachineId,
                MachineName = source.MachineName,
                Code = source.Code,
                Name = source.Name,
                Type = source.Type,
                SopDoc = source.SopDoc,
                Status = source.Status,
                InstallDate = source.InstallDate,
                SerialNumber = source.SerialNumber,
                Supplier = source.Supplier,
                WarrantyUntil = source.WarrantyUntil,
                Comments = source.Comments,
                LifecycleState = source.LifecycleState
            };
        }

        private static void Copy(Component source, Component destination)
        {
            destination.MachineId = source.MachineId;
            destination.MachineName = source.MachineName;
            destination.Code = source.Code;
            destination.Name = source.Name;
            destination.Type = source.Type;
            destination.SopDoc = source.SopDoc;
            destination.Status = source.Status;
            destination.InstallDate = source.InstallDate;
            destination.SerialNumber = source.SerialNumber;
            destination.Supplier = source.Supplier;
            destination.WarrantyUntil = source.WarrantyUntil;
            destination.Comments = source.Comments;
            destination.LifecycleState = source.LifecycleState;
        }
    }

    public sealed class FakeCalibrationCrudService : ICalibrationCrudService
    {
        private readonly List<Calibration> _store = new();

        public List<Calibration> Saved => _store;

        public Task<IReadOnlyList<Calibration>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Calibration>>(_store.ToList());

        public Task<Calibration?> TryGetByIdAsync(int id)
            => Task.FromResult<Calibration?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateAsync(Calibration calibration, CalibrationCrudContext context)
        {
            if (calibration.Id == 0)
            {
                calibration.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(calibration));
            return Task.FromResult(calibration.Id);
        }

        public Task UpdateAsync(Calibration calibration, CalibrationCrudContext context)
        {
            var existing = _store.FirstOrDefault(c => c.Id == calibration.Id);
            if (existing is null)
            {
                _store.Add(Clone(calibration));
            }
            else
            {
                Copy(calibration, existing);
            }

            return Task.CompletedTask;
        }

        public void Validate(Calibration calibration)
        {
            if (calibration.ComponentId <= 0)
                throw new InvalidOperationException("Calibration must be linked to a component.");
            if (!calibration.SupplierId.HasValue || calibration.SupplierId.Value <= 0)
                throw new InvalidOperationException("Supplier is required.");
            if (calibration.CalibrationDate == default)
                throw new InvalidOperationException("Calibration date is required.");
            if (calibration.NextDue == default)
                throw new InvalidOperationException("Next due date is required.");
            if (calibration.NextDue < calibration.CalibrationDate)
                throw new InvalidOperationException("Next due date must be after the calibration date.");
            if (string.IsNullOrWhiteSpace(calibration.Result))
                throw new InvalidOperationException("Calibration result is required.");
        }

        private static Calibration Clone(Calibration source)
        {
            return new Calibration
            {
                Id = source.Id,
                ComponentId = source.ComponentId,
                SupplierId = source.SupplierId,
                CalibrationDate = source.CalibrationDate,
                NextDue = source.NextDue,
                CertDoc = source.CertDoc,
                Result = source.Result,
                Comment = source.Comment,
                Status = source.Status
            };
        }

        private static void Copy(Calibration source, Calibration destination)
        {
            destination.ComponentId = source.ComponentId;
            destination.SupplierId = source.SupplierId;
            destination.CalibrationDate = source.CalibrationDate;
            destination.NextDue = source.NextDue;
            destination.CertDoc = source.CertDoc;
            destination.Result = source.Result;
            destination.Comment = source.Comment;
            destination.Status = source.Status;
        }
    }
}

namespace YasGMP.Wpf.ViewModels.Modules
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using YasGMP.Services;
    using YasGMP.Wpf.Services;

    public sealed class InspectorField
    {
        public InspectorField(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }
    }

    public sealed class InspectorContext
    {
        public InspectorContext(string title, string subtitle, IReadOnlyList<InspectorField> fields)
        {
            Title = title;
            Subtitle = subtitle;
            Fields = fields;
        }

        public string Title { get; }

        public string Subtitle { get; }

        public IReadOnlyList<InspectorField> Fields { get; }
    }

    public sealed class ModuleRecord
    {
        public ModuleRecord(
            string key,
            string title,
            string? code = null,
            string? status = null,
            string? description = null,
            IReadOnlyList<InspectorField>? inspectorFields = null,
            string? relatedModuleKey = null,
            object? relatedParameter = null)
        {
            Key = key;
            Title = title;
            Code = code;
            Status = status;
            Description = description;
            InspectorFields = inspectorFields ?? new List<InspectorField>();
            RelatedModuleKey = relatedModuleKey;
            RelatedParameter = relatedParameter;
        }

        public string Key { get; }

        public string Title { get; }

        public string? Code { get; }

        public string? Status { get; }

        public string? Description { get; }

        public IReadOnlyList<InspectorField> InspectorFields { get; }

        public string? RelatedModuleKey { get; }

        public object? RelatedParameter { get; }
    }

    public enum FormMode
    {
        View,
        Find,
        Add,
        Update
    }

    public abstract class ModuleDocumentViewModel
    {
        protected ModuleDocumentViewModel(
            string moduleKey,
            string title,
            ICflDialogService _,
            IShellInteractionService __,
            IModuleNavigationService ___)
        {
            ModuleKey = moduleKey;
            Title = title;
        }

        public string ModuleKey { get; }

        public string Title { get; }

        public List<ModuleRecord> Records { get; } = new();

        public ModuleRecord? SelectedRecord { get; protected set; }

        public string? SearchText { get; protected set; }

        public string StatusMessage { get; protected set; } = "Ready";

        public List<string> ValidationMessages { get; } = new();

        public bool IsDirty { get; private set; }

        public FormMode Mode { get; set; } = FormMode.View;

        public bool IsInEditMode => Mode is FormMode.Add or FormMode.Update;

        protected static IReadOnlyList<ModuleRecord> ToReadOnlyList(IEnumerable<ModuleRecord> source)
            => source as IReadOnlyList<ModuleRecord> ?? source.ToList();

        protected virtual Task<CflRequest?> CreateCflRequestAsync()
            => Task.FromResult<CflRequest?>(null);

        protected virtual Task OnCflSelectionAsync(CflResult result)
            => Task.CompletedTask;

        protected abstract Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter);

        protected abstract IReadOnlyList<ModuleRecord> CreateDesignTimeRecords();

        public async Task InitializeAsync(object? parameter)
        {
            var records = await LoadAsync(parameter).ConfigureAwait(false);
            Records.Clear();
            foreach (var record in records)
            {
                Records.Add(record);
            }

            SelectedRecord = Records.FirstOrDefault();
        }

        public async Task<CflResult?> ExecuteShowCflAsync(ICflDialogService dialog)
        {
            var request = await CreateCflRequestAsync().ConfigureAwait(false);
            if (request is null)
            {
                return null;
            }

            var result = await dialog.ShowAsync(request).ConfigureAwait(false);
            if (result is null)
            {
                return null;
            }

            await OnCflSelectionAsync(result).ConfigureAwait(false);
            return result;
        }

        protected void MarkDirty() => IsDirty = true;

        protected void ResetDirty() => IsDirty = false;

        protected void ClearValidationMessages() => ValidationMessages.Clear();
    }

    public abstract class DataDrivenModuleDocumentViewModel : ModuleDocumentViewModel
    {
        protected DataDrivenModuleDocumentViewModel(
            string key,
            string title,
            DatabaseService database,
            ICflDialogService _,
            IShellInteractionService __,
            IModuleNavigationService ___)
            : base(key, title, _, __, ___)
        {
            Database = database;
        }

        protected DatabaseService Database { get; }
    }
}

    public sealed class FakePartCrudService : IPartCrudService
    {
        private readonly List<Part> _store = new();

        public List<Part> Saved => _store;

        public Task<IReadOnlyList<Part>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Part>>(_store.ToList());

        public Task<Part?> TryGetByIdAsync(int id)
            => Task.FromResult<Part?>(_store.FirstOrDefault(p => p.Id == id));

        public Task<int> CreateAsync(Part part, PartCrudContext context)
        {
            if (part.Id == 0)
            {
                part.Id = _store.Count == 0 ? 1 : _store.Max(p => p.Id) + 1;
            }

            _store.Add(Clone(part));
            return Task.FromResult(part.Id);
        }

        public Task UpdateAsync(Part part, PartCrudContext context)
        {
            var existing = _store.FirstOrDefault(p => p.Id == part.Id);
            if (existing is null)
            {
                _store.Add(Clone(part));
            }
            else
            {
                Copy(part, existing);
            }

            return Task.CompletedTask;
        }

        public void Validate(Part part)
        {
            if (string.IsNullOrWhiteSpace(part.Name))
                throw new InvalidOperationException("Part name is required.");
            if (string.IsNullOrWhiteSpace(part.Code))
                throw new InvalidOperationException("Part code is required.");
            if (!part.DefaultSupplierId.HasValue)
                throw new InvalidOperationException("Supplier is required.");
        }

        public string NormalizeStatus(string? status)
            => string.IsNullOrWhiteSpace(status) ? "active" : status.Trim().ToLowerInvariant();

        private static Part Clone(Part source)
        {
            return new Part
            {
                Id = source.Id,
                Code = source.Code,
                Name = source.Name,
                Description = source.Description,
                Category = source.Category,
                Status = source.Status,
                Stock = source.Stock,
                MinStockAlert = source.MinStockAlert,
                Location = source.Location,
                DefaultSupplierId = source.DefaultSupplierId,
                DefaultSupplierName = source.DefaultSupplierName,
                Sku = source.Sku,
                Price = source.Price
            };
        }

        private static void Copy(Part source, Part destination)
        {
            destination.Code = source.Code;
            destination.Name = source.Name;
            destination.Description = source.Description;
            destination.Category = source.Category;
            destination.Status = source.Status;
            destination.Stock = source.Stock;
            destination.MinStockAlert = source.MinStockAlert;
            destination.Location = source.Location;
            destination.DefaultSupplierId = source.DefaultSupplierId;
            destination.DefaultSupplierName = source.DefaultSupplierName;
            destination.Sku = source.Sku;
            destination.Price = source.Price;
        }
    }

    public sealed class FakeWarehouseCrudService : IWarehouseCrudService
    {
        private readonly List<Warehouse> _store = new();

        public List<Warehouse> Saved => _store;

        public List<WarehouseStockSnapshot> StockSnapshots { get; } = new();

        public List<InventoryMovementEntry> Movements { get; } = new();

        public Task<IReadOnlyList<Warehouse>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Warehouse>>(_store.ToList());

        public Task<Warehouse?> TryGetByIdAsync(int id)
            => Task.FromResult<Warehouse?>(_store.FirstOrDefault(w => w.Id == id));

        public Task<int> CreateAsync(Warehouse warehouse, WarehouseCrudContext context)
        {
            if (warehouse.Id == 0)
            {
                warehouse.Id = _store.Count == 0 ? 1 : _store.Max(w => w.Id) + 1;
            }

            _store.Add(Clone(warehouse));
            return Task.FromResult(warehouse.Id);
        }

        public Task UpdateAsync(Warehouse warehouse, WarehouseCrudContext context)
        {
            var existing = _store.FirstOrDefault(w => w.Id == warehouse.Id);
            if (existing is null)
            {
                _store.Add(Clone(warehouse));
            }
            else
            {
                Copy(warehouse, existing);
            }

            return Task.CompletedTask;
        }

        public void Validate(Warehouse warehouse)
        {
            if (string.IsNullOrWhiteSpace(warehouse.Name))
                throw new InvalidOperationException("Warehouse name is required.");
            if (string.IsNullOrWhiteSpace(warehouse.Location))
                throw new InvalidOperationException("Warehouse location is required.");
        }

        public string NormalizeStatus(string? status)
            => string.IsNullOrWhiteSpace(status) ? "qualified" : status.Trim().ToLowerInvariant();

        public Task<IReadOnlyList<WarehouseStockSnapshot>> GetStockSnapshotAsync(int warehouseId)
        {
            var items = StockSnapshots
                .Where(s => s.WarehouseId == warehouseId)
                .ToList();
            return Task.FromResult<IReadOnlyList<WarehouseStockSnapshot>>(items);
        }

        public Task<IReadOnlyList<InventoryMovementEntry>> GetRecentMovementsAsync(int warehouseId, int take = 10)
        {
            var items = Movements
                .Where(m => m.WarehouseId == warehouseId)
                .Take(take)
                .ToList();
            return Task.FromResult<IReadOnlyList<InventoryMovementEntry>>(items);
        }

        private static Warehouse Clone(Warehouse source)
        {
            return new Warehouse
            {
                Id = source.Id,
                Name = source.Name,
                Location = source.Location,
                Status = source.Status,
                LegacyResponsibleName = source.LegacyResponsibleName,
                Note = source.Note,
                QrCode = source.QrCode,
                ClimateMode = source.ClimateMode,
                IsQualified = source.IsQualified,
                LastQualified = source.LastQualified,
                DigitalSignature = source.DigitalSignature
            };
        }

        private static void Copy(Warehouse source, Warehouse destination)
        {
            destination.Name = source.Name;
            destination.Location = source.Location;
            destination.Status = source.Status;
            destination.LegacyResponsibleName = source.LegacyResponsibleName;
            destination.Note = source.Note;
            destination.QrCode = source.QrCode;
            destination.ClimateMode = source.ClimateMode;
            destination.IsQualified = source.IsQualified;
            destination.LastQualified = source.LastQualified;
            destination.DigitalSignature = source.DigitalSignature;
        }
    }

    public sealed class FakeWorkOrderCrudService : IWorkOrderCrudService
    {
        private readonly List<WorkOrder> _store = new();

        public List<WorkOrder> Saved => _store;

        public Task<WorkOrder?> TryGetByIdAsync(int id)
            => Task.FromResult<WorkOrder?>(_store.FirstOrDefault(w => w.Id == id));

        public Task<int> CreateAsync(WorkOrder workOrder, WorkOrderCrudContext context)
        {
            if (workOrder.Id == 0)
            {
                workOrder.Id = _store.Count == 0 ? 1 : _store.Max(w => w.Id) + 1;
            }

            _store.Add(Clone(workOrder));
            return Task.FromResult(workOrder.Id);
        }

        public Task UpdateAsync(WorkOrder workOrder, WorkOrderCrudContext context)
        {
            var existing = _store.FirstOrDefault(w => w.Id == workOrder.Id);
            if (existing is null)
            {
                _store.Add(Clone(workOrder));
            }
            else
            {
                Copy(workOrder, existing);
            }

            return Task.CompletedTask;
        }

        public void Validate(WorkOrder workOrder)
        {
            if (string.IsNullOrWhiteSpace(workOrder.Title))
                throw new InvalidOperationException("Title is required.");
            if (string.IsNullOrWhiteSpace(workOrder.Description))
                throw new InvalidOperationException("Description is required.");
            if (string.IsNullOrWhiteSpace(workOrder.Type))
                throw new InvalidOperationException("Type is required.");
            if (workOrder.MachineId <= 0)
                throw new InvalidOperationException("Machine is required.");
            if (workOrder.CreatedById <= 0)
                throw new InvalidOperationException("CreatedBy is required.");
        }

        private static WorkOrder Clone(WorkOrder source)
            => new()
            {
                Id = source.Id,
                Title = source.Title,
                Description = source.Description,
                TaskDescription = source.TaskDescription,
                Type = source.Type,
                Priority = source.Priority,
                Status = source.Status,
                DateOpen = source.DateOpen,
                DueDate = source.DueDate,
                DateClose = source.DateClose,
                RequestedById = source.RequestedById,
                CreatedById = source.CreatedById,
                AssignedToId = source.AssignedToId,
                MachineId = source.MachineId,
                ComponentId = source.ComponentId,
                Result = source.Result,
                Notes = source.Notes
            };

        private static void Copy(WorkOrder source, WorkOrder destination)
        {
            destination.Title = source.Title;
            destination.Description = source.Description;
            destination.TaskDescription = source.TaskDescription;
            destination.Type = source.Type;
            destination.Priority = source.Priority;
            destination.Status = source.Status;
            destination.DateOpen = source.DateOpen;
            destination.DueDate = source.DueDate;
            destination.DateClose = source.DateClose;
            destination.RequestedById = source.RequestedById;
            destination.CreatedById = source.CreatedById;
            destination.AssignedToId = source.AssignedToId;
            destination.MachineId = source.MachineId;
            destination.ComponentId = source.ComponentId;
            destination.Result = source.Result;
            destination.Notes = source.Notes;
        }
    }
}
