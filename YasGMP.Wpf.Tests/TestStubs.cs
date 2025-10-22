using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Wpf.ViewModels;

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
        public static InspectorField Create(
            string moduleKey,
            string moduleTitle,
            string? recordKey,
            string? recordTitle,
            string label,
            string? value)
        {
            var moduleToken = AutomationIdSanitizer.Normalize(moduleKey, "module");
            var recordToken = AutomationIdSanitizer.Normalize(recordKey, "record");
            var labelToken = AutomationIdSanitizer.Normalize(label, "field");
            var displayModule = string.IsNullOrWhiteSpace(moduleTitle) ? moduleKey : moduleTitle;
            var displayRecord = string.IsNullOrWhiteSpace(recordTitle)
                ? (string.IsNullOrWhiteSpace(recordKey) ? "Record" : recordKey)
                : recordTitle;

            var automationName = string.Format(
                CultureInfo.CurrentCulture,
                "{0} — {1} ({2})",
                displayModule,
                label,
                displayRecord);

            var automationId = string.Format(
                CultureInfo.InvariantCulture,
                "Dock.Inspector.{0}.{1}.{2}",
                moduleToken,
                recordToken,
                labelToken);

            var automationTooltip = string.Format(
                CultureInfo.CurrentCulture,
                "{0} for {1} in {2}.",
                label,
                displayRecord,
                displayModule);

            return new InspectorField(label, value, automationName, automationId, automationTooltip);
        }
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
        public string? QrCode { get; set; }
        public string? QrPayload { get; set; }
        public string? CodeOverride { get; set; }
        public bool IsCodeOverrideEnabled { get; set; }
        public List<string> LinkedDocuments { get; set; } = new();
    }

    public class WorkOrder
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

    public class ChangeControl
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? StatusRaw { get; set; }
        public int? RequestedById { get; set; }
        public DateTime? DateRequested { get; set; }
        public int? AssignedToId { get; set; }
        public DateTime? DateAssigned { get; set; }
        public int? LastModifiedById { get; set; }
        public DateTime? LastModified { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CapaCase
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime DateOpen { get; set; } = DateTime.UtcNow;
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

    public class ChangeControl
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? StatusRaw { get; set; }
        public int? RequestedById { get; set; }
        public DateTime? DateRequested { get; set; }
        public int? AssignedToId { get; set; }
        public DateTime? DateAssigned { get; set; }
        public int? LastModifiedById { get; set; }
        public DateTime? LastModified { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
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

public sealed partial class FakeMachineCrudService : IMachineCrudService
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

    public class ScheduledJob
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string JobType { get; set; } = string.Empty;
        public string Status { get; set; } = "scheduled";
        public DateTime NextDue { get; set; } = DateTime.UtcNow;
        public string RecurrencePattern { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string? CronExpression { get; set; }
        public string Comment { get; set; } = string.Empty;
        public bool IsCritical { get; set; }
        public bool NeedsAcknowledgment { get; set; }
        public bool AlertOnFailure { get; set; } = true;
        public int Retries { get; set; }
        public int MaxRetries { get; set; } = 3;
        public string EscalationNote { get; set; } = string.Empty;
        public DateTime? LastExecuted { get; set; }
        public string LastResult { get; set; } = string.Empty;
        public string LastError { get; set; } = string.Empty;
        public string ExtraParams { get; set; } = string.Empty;
        public int? CreatedById { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public int? LastModifiedById { get; set; }
        public string DeviceInfo { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
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

        private readonly List<Machine> _store = new();


        public List<Machine> Saved => _store;


    public partial class DatabaseService
    {
        public List<Asset> Assets { get; } = new();
        public List<Component> Components { get; } = new();
        public List<WorkOrder> WorkOrders { get; } = new();
        public List<Calibration> Calibrations { get; } = new();
        public List<ScheduledJob> ScheduledJobs { get; } = new();
        public List<Supplier> Suppliers { get; } = new();
        public List<Part> Parts { get; } = new();
        public List<Warehouse> Warehouses { get; } = new();
        public List<Incident> Incidents { get; } = new();
        public List<CapaCase> CapaCases { get; } = new();

        public Task<IReadOnlyList<Machine>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Machine>>(_store.ToList());




        public Task<Machine?> TryGetByIdAsync(int id)
            => Task.FromResult<Machine?>(_store.FirstOrDefault(m => m.Id == id));


        public Task<List<Component>> GetAllComponentsAsync()
            => Task.FromResult(Components);

        public Task<List<Component>> GetAllComponentsAsync()
            => Task.FromResult(Components);

        public Task<List<Component>> GetAllComponentsAsync()
            => Task.FromResult(Components);

        public Task<List<Component>> GetAllComponentsAsync()
            => Task.FromResult(Components);

        public Task<List<WorkOrder>> GetAllWorkOrdersFullAsync()
            => Task.FromResult(WorkOrders);

        public Task<List<Calibration>> GetAllCalibrationsAsync()
            => Task.FromResult(Calibrations);

        public Task<List<ScheduledJob>> GetAllScheduledJobsFullAsync()
            => Task.FromResult(ScheduledJobs);

        public Task<List<Incident>> GetAllIncidentsAsync()
            => Task.FromResult(Incidents);

        public Task<List<CapaCase>> GetAllCapaCasesAsync()
            => Task.FromResult(CapaCases);

        public Task<List<Supplier>> GetAllSuppliersAsync()
            => Task.FromResult(Suppliers);

        public Task<List<Warehouse>> GetWarehousesAsync()
        {
            if (WarehousesExceptionFactory?.Invoke() is Exception dynamicException)
            {
                throw dynamicException;
            }

            if (WarehousesException is not null)
            {
                throw WarehousesException;
            }

            return Task.FromResult(Warehouses.Select(CloneWarehouse).ToList());
        }

        public Func<List<Part>>? PartsFactory { get; set; }

        public Func<Exception?>? PartsExceptionFactory { get; set; }

        public Exception? PartsException { get; set; }

        public Task<List<Part>> GetAllPartsAsync()
        {
            if (PartsExceptionFactory?.Invoke() is Exception dynamicException)
            {
                throw dynamicException;
            }

            if (PartsException is not null)
            {
                throw PartsException;
            }

            if (PartsFactory is not null)
            {
                return Task.FromResult(PartsFactory());
            }

            return Task.FromResult(Parts.Select(ClonePart).ToList());
        }

        private static Part ClonePart(Part source)
            => new()
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

    public sealed class TestFilePicker : IFilePicker
    {
        public IReadOnlyList<PickedFile> Files { get; set; } = Array.Empty<PickedFile>();

        public Task<IReadOnlyList<PickedFile>> PickFilesAsync(FilePickerRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(Files);
    }
}

namespace YasGMP.Services
{
    using System.Linq;
    using YasGMP.Models;

    public partial class DatabaseService
    {
        public List<DashboardEvent> DashboardEvents { get; } = new();

        public Exception? DashboardEventsException { get; set; }

        public Task<List<DashboardEvent>> GetRecentDashboardEventsAsync(int take, CancellationToken cancellationToken = default)
        {
            if (DashboardEventsException is not null)
            {
                throw DashboardEventsException;
            }

            if (take <= 0)
            {
                take = 1;
            }

            var ordered = DashboardEvents
                .OrderByDescending(evt => evt.Timestamp)
                .ThenByDescending(evt => evt.Id)
                .Take(take)
                .Select(CloneDashboardEvent)
                .ToList();

            return Task.FromResult(ordered);
        }

        private static DashboardEvent CloneDashboardEvent(DashboardEvent source)
            => new()
            {
                Id = source.Id,
                EventType = source.EventType,
                Description = source.Description,
                Timestamp = source.Timestamp,
                Severity = source.Severity,
                UserId = source.UserId,
                RelatedModule = source.RelatedModule,
                RelatedRecordId = source.RelatedRecordId,
                Icon = source.Icon,
                IsUnread = source.IsUnread,
                DrilldownKey = source.DrilldownKey,
                Note = source.Note
            };
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

        public Task<int> CreateCoreAsync(Machine machine, MachineCrudContext context)
        {
            if (machine.Id == 0)
            {
                machine.Id = _store.Count == 0 ? 1 : _store.Max(m => m.Id) + 1;
            }
            _store.Add(Clone(machine));
            TrackSnapshot(machine, context);
            return Task.FromResult(machine.Id);
        }

        public Task UpdateCoreAsync(Machine machine, MachineCrudContext context)
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

        private void TrackSnapshot(Machine machine, MachineCrudContext context)
            => _savedSnapshots.Add((Clone(machine), context));

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
                Note = source.Note,
                QrCode = source.QrCode,
                QrPayload = source.QrPayload,
                DigitalSignature = source.DigitalSignature
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
            destination.QrCode = source.QrCode;
            destination.QrPayload = source.QrPayload;
            destination.DigitalSignature = source.DigitalSignature;
        }
    }


    public sealed partial class FakeComponentCrudService : IComponentCrudService
    {
        private readonly List<Component> _store = new();
        private readonly List<(Component Entity, ComponentCrudContext Context)> _savedSnapshots = new();

        public List<Component> Saved => _store;
        public IReadOnlyList<(Component Entity, ComponentCrudContext Context)> SavedWithContext => _savedSnapshots;
        public ComponentCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Component? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<ComponentCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Component> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Component>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Component>>(_store.ToList());

        public Task<Component?> TryGetByIdAsync(int id)
            => Task.FromResult<Component?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Component component, ComponentCrudContext context)
        {
            if (component.Id == 0)
            {
                component.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(component));
            TrackSnapshot(component, context);
            return Task.FromResult(component.Id);
        }

        public Task UpdateCoreAsync(Component component, ComponentCrudContext context)
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

            TrackSnapshot(component, context);
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

        private void TrackSnapshot(Component component, ComponentCrudContext context)
            => _savedSnapshots.Add((Clone(component), context));

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

    public sealed partial class FakeCalibrationCrudService : ICalibrationCrudService
    {
        private readonly List<Calibration> _store = new();
        private readonly List<(Calibration Entity, CalibrationCrudContext Context)> _savedSnapshots = new();

        public List<Calibration> Saved => _store;
        public IReadOnlyList<(Calibration Entity, CalibrationCrudContext Context)> SavedWithContext => _savedSnapshots;
        public CalibrationCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Calibration? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<CalibrationCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Calibration> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Calibration>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Calibration>>(_store.ToList());

        public Task<Calibration?> TryGetByIdAsync(int id)
            => Task.FromResult<Calibration?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Calibration calibration, CalibrationCrudContext context)
        {
            if (calibration.Id == 0)
            {
                calibration.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(calibration));
            TrackSnapshot(calibration, context);
            return Task.FromResult(calibration.Id);
        }

        public Task UpdateCoreAsync(Calibration calibration, CalibrationCrudContext context)
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

            TrackSnapshot(calibration, context);
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

        private void TrackSnapshot(Calibration calibration, CalibrationCrudContext context)
            => _savedSnapshots.Add((Clone(calibration), context));

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


    public sealed partial class FakeMachineCrudService : IMachineCrudService
    {
        private readonly List<Machine> _store = new();
        private readonly List<(Machine Entity, MachineCrudContext Context)> _savedSnapshots = new();
        private readonly List<(int Id, MachineCrudContext Context)> _deletedSnapshots = new();

        public List<Machine> Saved => _store;
        public IReadOnlyList<(Machine Entity, MachineCrudContext Context)> SavedWithContext => _savedSnapshots;
        public MachineCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Machine? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<MachineCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Machine> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));
        public IReadOnlyList<(int Id, MachineCrudContext Context)> DeletedWithContext => _deletedSnapshots;
        public IEnumerable<MachineCrudContext> DeletedContexts => _deletedSnapshots.Select(tuple => tuple.Context);

        public Task<IReadOnlyList<Machine>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Machine>>(_store.ToList());

        public Task<Machine?> TryGetByIdAsync(int id)
            => Task.FromResult<Machine?>(_store.FirstOrDefault(m => m.Id == id));

        public Task<int> CreateCoreAsync(Machine machine, MachineCrudContext context)
        {
            if (machine.Id == 0)
            {
                machine.Id = _store.Count == 0 ? 1 : _store.Max(m => m.Id) + 1;
            }
            _store.Add(Clone(machine));
            TrackSnapshot(machine, context);
            return Task.FromResult(machine.Id);
        }

        public Task UpdateCoreAsync(Machine machine, MachineCrudContext context)
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

            TrackSnapshot(machine, context);
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

        private void TrackSnapshot(Machine machine, MachineCrudContext context)
            => _savedSnapshots.Add((Clone(machine), context));

        private void TrackDeletion(int id, MachineCrudContext context)
            => _deletedSnapshots.Add((id, context));

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
                Note = source.Note,
                QrCode = source.QrCode,
                QrPayload = source.QrPayload,
                DigitalSignature = source.DigitalSignature
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
            destination.QrCode = source.QrCode;
            destination.QrPayload = source.QrPayload;
            destination.DigitalSignature = source.DigitalSignature;
        }
    }

    public sealed partial class FakeComponentCrudService : IComponentCrudService
    {
        private readonly List<Component> _store = new();
        private readonly List<(Component Entity, ComponentCrudContext Context)> _savedSnapshots = new();

        public List<Component> Saved => _store;
        public IReadOnlyList<(Component Entity, ComponentCrudContext Context)> SavedWithContext => _savedSnapshots;
        public ComponentCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Component? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<ComponentCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Component> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Component>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Component>>(_store.ToList());

        public Task<Component?> TryGetByIdAsync(int id)
            => Task.FromResult<Component?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Component component, ComponentCrudContext context)
        {
            if (component.Id == 0)
            {
                component.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(component));
            TrackSnapshot(component, context);
            return Task.FromResult(component.Id);
        }

        public Task UpdateCoreAsync(Component component, ComponentCrudContext context)
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

            TrackSnapshot(component, context);
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

        private void TrackSnapshot(Component component, ComponentCrudContext context)
            => _savedSnapshots.Add((Clone(component), context));

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


    public sealed partial class FakeMachineCrudService : IMachineCrudService
    {
        private readonly List<Machine> _store = new();
        private readonly List<(Machine Entity, MachineCrudContext Context)> _savedSnapshots = new();

        public List<Machine> Saved => _store;
        public IReadOnlyList<(Machine Entity, MachineCrudContext Context)> SavedWithContext => _savedSnapshots;
        public MachineCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Machine? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<MachineCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Machine> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Machine>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Machine>>(_store.ToList());

        public Task<Machine?> TryGetByIdAsync(int id)
            => Task.FromResult<Machine?>(_store.FirstOrDefault(m => m.Id == id));

        public Task<int> CreateCoreAsync(Machine machine, MachineCrudContext context)
        {
            if (machine.Id == 0)
            {
                machine.Id = _store.Count == 0 ? 1 : _store.Max(m => m.Id) + 1;
            }
            _store.Add(Clone(machine));
            TrackSnapshot(machine, context);
            return Task.FromResult(machine.Id);
        }

        public Task UpdateCoreAsync(Machine machine, MachineCrudContext context)
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

            TrackSnapshot(machine, context);
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

        private void TrackSnapshot(Machine machine, MachineCrudContext context)
            => _savedSnapshots.Add((Clone(machine), context));

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
                Note = source.Note,
                QrCode = source.QrCode,
                QrPayload = source.QrPayload,
                DigitalSignature = source.DigitalSignature
            };
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
            destination.QrCode = source.QrCode;
            destination.QrPayload = source.QrPayload;
            destination.DigitalSignature = source.DigitalSignature;
        }
    }

    public sealed partial class FakeComponentCrudService : IComponentCrudService
    {
        private readonly List<Component> _store = new();

        public List<Component> Saved => _store;


        public Task<IReadOnlyList<Component>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Component>>(_store.ToList());


    public partial class DatabaseService
    {
        public List<Asset> Assets { get; } = new();
        public List<Component> Components { get; } = new();
        public List<WorkOrder> WorkOrders { get; } = new();
        public List<Calibration> Calibrations { get; } = new();
        public List<Supplier> Suppliers { get; } = new();
        public List<Part> Parts { get; } = new();
        public List<Warehouse> Warehouses { get; } = new();

        public Task<Component?> TryGetByIdAsync(int id)
            => Task.FromResult<Component?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Component component, ComponentCrudContext context)
        {
            if (component.Id == 0)
            {
                component.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }


            _store.Add(Clone(component));
            TrackSnapshot(component, context);
            return Task.FromResult(component.Id);
        }


        public Task<List<Component>> GetAllComponentsAsync()
            => Task.FromResult(Components);

        public Task<List<WorkOrder>> GetAllWorkOrdersFullAsync()
            => Task.FromResult(WorkOrders);

        public Task<List<Calibration>> GetAllCalibrationsAsync()
            => Task.FromResult(Calibrations);

        public Task<List<Supplier>> GetAllSuppliersAsync()
            => Task.FromResult(Suppliers);

        public Task<List<Warehouse>> GetWarehousesAsync()
        {
            if (WarehousesExceptionFactory?.Invoke() is Exception dynamicException)
            {
                throw dynamicException;
            }

            if (WarehousesException is not null)
            {
                throw WarehousesException;
            }

            return Task.FromResult(Warehouses.Select(CloneWarehouse).ToList());
        }
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
        public Task UpdateCoreAsync(Component component, ComponentCrudContext context)
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

            TrackSnapshot(component, context);
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

        private void TrackSnapshot(Component component, ComponentCrudContext context)
            => _savedSnapshots.Add((Clone(component), context));

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


    public sealed partial class FakeCalibrationCrudService : ICalibrationCrudService
    {
        private readonly List<Calibration> _store = new();
        private readonly List<(Calibration Entity, CalibrationCrudContext Context)> _savedSnapshots = new();

        public List<Calibration> Saved => _store;
        public IReadOnlyList<(Calibration Entity, CalibrationCrudContext Context)> SavedWithContext => _savedSnapshots;
        public CalibrationCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Calibration? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<CalibrationCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Calibration> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Calibration>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Calibration>>(_store.ToList());

        public Task<Calibration?> TryGetByIdAsync(int id)
            => Task.FromResult<Calibration?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Calibration calibration, CalibrationCrudContext context)
        {
            if (calibration.Id == 0)
            {
                calibration.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(calibration));
            TrackSnapshot(calibration, context);
            return Task.FromResult(calibration.Id);
        }

        public Task UpdateCoreAsync(Calibration calibration, CalibrationCrudContext context)
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

            TrackSnapshot(calibration, context);
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

        private void TrackSnapshot(Calibration calibration, CalibrationCrudContext context)
            => _savedSnapshots.Add((Clone(calibration), context));

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

    public sealed partial class FakeMachineCrudService : IMachineCrudService
    {
        private readonly List<Machine> _store = new();
        private readonly List<(Machine Entity, MachineCrudContext Context)> _savedSnapshots = new();

        public List<Machine> Saved => _store;
        public IReadOnlyList<(Machine Entity, MachineCrudContext Context)> SavedWithContext => _savedSnapshots;
        public MachineCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Machine? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<MachineCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Machine> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Machine>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Machine>>(_store.ToList());

        public Task<Machine?> TryGetByIdAsync(int id)
            => Task.FromResult<Machine?>(_store.FirstOrDefault(m => m.Id == id));

        public Task<int> CreateCoreAsync(Machine machine, MachineCrudContext context)
        {
            if (machine.Id == 0)
            {
                machine.Id = _store.Count == 0 ? 1 : _store.Max(m => m.Id) + 1;
            }
            _store.Add(Clone(machine));
            TrackSnapshot(machine, context);
            return Task.FromResult(machine.Id);
        }

        public Task UpdateCoreAsync(Machine machine, MachineCrudContext context)
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

            TrackSnapshot(machine, context);
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

        private void TrackSnapshot(Machine machine, MachineCrudContext context)
            => _savedSnapshots.Add((Clone(machine), context));

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
                Note = source.Note,
                QrCode = source.QrCode,
                QrPayload = source.QrPayload,
                DigitalSignature = source.DigitalSignature
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
            destination.QrCode = source.QrCode;
            destination.QrPayload = source.QrPayload;
            destination.DigitalSignature = source.DigitalSignature;
        }
    }

    public sealed partial class FakeComponentCrudService : IComponentCrudService
    {
        private readonly List<Component> _store = new();
        private readonly List<(Component Entity, ComponentCrudContext Context)> _savedSnapshots = new();

        public List<Component> Saved => _store;
        public IReadOnlyList<(Component Entity, ComponentCrudContext Context)> SavedWithContext => _savedSnapshots;
        public ComponentCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Component? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<ComponentCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Component> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Component>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Component>>(_store.ToList());

        public Task<Component?> TryGetByIdAsync(int id)
            => Task.FromResult<Component?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Component component, ComponentCrudContext context)
        {
            if (component.Id == 0)
            {
                component.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(component));
            TrackSnapshot(component, context);
            return Task.FromResult(component.Id);
        }

        public Task UpdateCoreAsync(Component component, ComponentCrudContext context)
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

            TrackSnapshot(component, context);
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

        private void TrackSnapshot(Component component, ComponentCrudContext context)
            => _savedSnapshots.Add((Clone(component), context));

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

    public sealed partial class FakeCalibrationCrudService : ICalibrationCrudService
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

        public Task<Calibration?> TryGetByIdAsync(int id)
            => Task.FromResult<Calibration?>(_store.FirstOrDefault(c => c.Id == id));

    public partial class DatabaseService
    {
        public List<Asset> Assets { get; } = new();
        public List<Component> Components { get; } = new();
        public List<WorkOrder> WorkOrders { get; } = new();
        public List<Calibration> Calibrations { get; } = new();
        public List<Supplier> Suppliers { get; } = new();
        public List<Part> Parts { get; } = new();
        public List<Warehouse> Warehouses { get; } = new();

            _store.Add(Clone(calibration));
            TrackSnapshot(calibration, context);
            return Task.FromResult(calibration.Id);
        }

        public Task<List<Component>> GetAllComponentsAsync()
            => Task.FromResult(Components);

        public Task<List<WorkOrder>> GetAllWorkOrdersFullAsync()
            => Task.FromResult(WorkOrders);

        public Task<List<Calibration>> GetAllCalibrationsAsync()
            => Task.FromResult(Calibrations);

        public Task<List<Supplier>> GetAllSuppliersAsync()
            => Task.FromResult(Suppliers);

        public Task<List<Warehouse>> GetWarehousesAsync()
        {
            if (WarehousesExceptionFactory?.Invoke() is Exception dynamicException)
            {
                throw dynamicException;
            }

            if (WarehousesException is not null)
            {
                throw WarehousesException;
            }

            return Task.FromResult(Warehouses.Select(CloneWarehouse).ToList());
        }
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

        private void TrackSnapshot(Calibration calibration, CalibrationCrudContext context)
            => _savedSnapshots.Add((Clone(calibration), context));

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

    public sealed partial class FakeMachineCrudService : IMachineCrudService
    {
        private readonly List<Machine> _store = new();
        private readonly List<(Machine Entity, MachineCrudContext Context)> _savedSnapshots = new();

        public List<Machine> Saved => _store;
        public IReadOnlyList<(Machine Entity, MachineCrudContext Context)> SavedWithContext => _savedSnapshots;
        public MachineCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Machine? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<MachineCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Machine> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Machine>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Machine>>(_store.ToList());

        public Task<Machine?> TryGetByIdAsync(int id)
            => Task.FromResult<Machine?>(_store.FirstOrDefault(m => m.Id == id));

        public Task<int> CreateCoreAsync(Machine machine, MachineCrudContext context)
        {
            if (machine.Id == 0)
            {
                machine.Id = _store.Count == 0 ? 1 : _store.Max(m => m.Id) + 1;
            }
            _store.Add(Clone(machine));
            TrackSnapshot(machine, context);
            return Task.FromResult(machine.Id);
        }

        public Task UpdateCoreAsync(Machine machine, MachineCrudContext context)
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

            TrackSnapshot(machine, context);
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

        private void TrackSnapshot(Machine machine, MachineCrudContext context)
            => _savedSnapshots.Add((Clone(machine), context));

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
                Note = source.Note,
                QrCode = source.QrCode,
                QrPayload = source.QrPayload,
                DigitalSignature = source.DigitalSignature
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
            destination.QrCode = source.QrCode;
            destination.QrPayload = source.QrPayload;
            destination.DigitalSignature = source.DigitalSignature;
        }
    }

    public sealed partial class FakeComponentCrudService : IComponentCrudService
    {
        private readonly List<Component> _store = new();
        private readonly List<(Component Entity, ComponentCrudContext Context)> _savedSnapshots = new();

        public List<Component> Saved => _store;
        public IReadOnlyList<(Component Entity, ComponentCrudContext Context)> SavedWithContext => _savedSnapshots;
        public ComponentCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Component? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<ComponentCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Component> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Component>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Component>>(_store.ToList());

        public Task<Component?> TryGetByIdAsync(int id)
            => Task.FromResult<Component?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Component component, ComponentCrudContext context)
        {
            if (component.Id == 0)
            {
                component.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(component));
            TrackSnapshot(component, context);
            return Task.FromResult(component.Id);
        }

        public Task UpdateCoreAsync(Component component, ComponentCrudContext context)
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

            TrackSnapshot(component, context);
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

        private void TrackSnapshot(Component component, ComponentCrudContext context)
            => _savedSnapshots.Add((Clone(component), context));

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

        public Task<IReadOnlyList<Calibration>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Calibration>>(_store.ToList());

    public partial class DatabaseService
    {
        public List<Asset> Assets { get; } = new();
        public List<Component> Components { get; } = new();
        public List<WorkOrder> WorkOrders { get; } = new();
        public List<Calibration> Calibrations { get; } = new();
        public List<Supplier> Suppliers { get; } = new();
        public List<Part> Parts { get; } = new();
        public List<Warehouse> Warehouses { get; } = new();
        public List<Incident> Incidents { get; } = new();

        public Task<int> CreateCoreAsync(Calibration calibration, CalibrationCrudContext context)
        {
            if (calibration.Id == 0)
            {
                calibration.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

        public Task<List<Component>> GetAllComponentsAsync()
            => Task.FromResult(Components);

        public Task<List<WorkOrder>> GetAllWorkOrdersFullAsync()
            => Task.FromResult(WorkOrders);

        public Task<List<Calibration>> GetAllCalibrationsAsync()
            => Task.FromResult(Calibrations);

        public Task<List<Incident>> GetAllIncidentsAsync()
            => Task.FromResult(Incidents);

        public Task<List<Supplier>> GetAllSuppliersAsync()
            => Task.FromResult(Suppliers);

        public Task<List<Warehouse>> GetWarehousesAsync()
        {
            if (WarehousesExceptionFactory?.Invoke() is Exception dynamicException)
            {
                throw dynamicException;
            }

            if (WarehousesException is not null)
            {
                throw WarehousesException;
            }

            return Task.FromResult(Warehouses.Select(CloneWarehouse).ToList());
        }
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

        private void TrackSnapshot(Calibration calibration, CalibrationCrudContext context)
            => _savedSnapshots.Add((Clone(calibration), context));

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

    public sealed partial class FakeMachineCrudService : IMachineCrudService
    {
        private readonly List<Machine> _store = new();
        private readonly List<(Machine Entity, MachineCrudContext Context)> _savedSnapshots = new();

        public List<Machine> Saved => _store;
        public IReadOnlyList<(Machine Entity, MachineCrudContext Context)> SavedWithContext => _savedSnapshots;
        public MachineCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Machine? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<MachineCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Machine> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Machine>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Machine>>(_store.ToList());

        public Task<Machine?> TryGetByIdAsync(int id)
            => Task.FromResult<Machine?>(_store.FirstOrDefault(m => m.Id == id));

        public Task<int> CreateCoreAsync(Machine machine, MachineCrudContext context)
        {
            if (machine.Id == 0)
            {
                machine.Id = _store.Count == 0 ? 1 : _store.Max(m => m.Id) + 1;
            }
            _store.Add(Clone(machine));
            TrackSnapshot(machine, context);
            return Task.FromResult(machine.Id);
        }

        public Task UpdateCoreAsync(Machine machine, MachineCrudContext context)
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

            TrackSnapshot(machine, context);
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

        private void TrackSnapshot(Machine machine, MachineCrudContext context)
            => _savedSnapshots.Add((Clone(machine), context));

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
                Note = source.Note,
                QrCode = source.QrCode,
                QrPayload = source.QrPayload,
                DigitalSignature = source.DigitalSignature
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
            destination.QrCode = source.QrCode;
            destination.QrPayload = source.QrPayload;
            destination.DigitalSignature = source.DigitalSignature;
        }
    }

    public sealed partial class FakeComponentCrudService : IComponentCrudService
    {
        private readonly List<Component> _store = new();
        private readonly List<(Component Entity, ComponentCrudContext Context)> _savedSnapshots = new();

        public List<Component> Saved => _store;
        public IReadOnlyList<(Component Entity, ComponentCrudContext Context)> SavedWithContext => _savedSnapshots;
        public ComponentCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Component? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<ComponentCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Component> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Component>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Component>>(_store.ToList());

        public Task<Component?> TryGetByIdAsync(int id)
            => Task.FromResult<Component?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Component component, ComponentCrudContext context)
        {
            if (component.Id == 0)
            {
                component.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(component));
            TrackSnapshot(component, context);
            return Task.FromResult(component.Id);
        }

        public Task UpdateCoreAsync(Component component, ComponentCrudContext context)
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

            TrackSnapshot(component, context);
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

        private void TrackSnapshot(Component component, ComponentCrudContext context)
            => _savedSnapshots.Add((Clone(component), context));

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

    public sealed partial class FakeCalibrationCrudService : ICalibrationCrudService
    {
        private readonly List<Calibration> _store = new();
        private readonly List<(Calibration Entity, CalibrationCrudContext Context)> _savedSnapshots = new();

        public List<Calibration> Saved => _store;
        public IReadOnlyList<(Calibration Entity, CalibrationCrudContext Context)> SavedWithContext => _savedSnapshots;
        public CalibrationCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Calibration? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<CalibrationCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Calibration> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Calibration>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Calibration>>(_store.ToList());

        public Task<Calibration?> TryGetByIdAsync(int id)
            => Task.FromResult<Calibration?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Calibration calibration, CalibrationCrudContext context)
        {
            if (calibration.Id == 0)
            {
                calibration.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(calibration));
            TrackSnapshot(calibration, context);
            return Task.FromResult(calibration.Id);
        }

        public Task UpdateCoreAsync(Calibration calibration, CalibrationCrudContext context)
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

            TrackSnapshot(calibration, context);
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

    public sealed partial class FakeMachineCrudService : IMachineCrudService
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

        public List<Machine> Saved => _store;

    public partial class DatabaseService
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

        public Task<Machine?> TryGetByIdAsync(int id)
            => Task.FromResult<Machine?>(_store.FirstOrDefault(m => m.Id == id));

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
        {
            if (WarehousesExceptionFactory?.Invoke() is Exception dynamicException)
            {
                throw dynamicException;
            }

            if (WarehousesException is not null)
            {
                throw WarehousesException;
            }

            return Task.FromResult(Warehouses.Select(CloneWarehouse).ToList());
        }
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

        private void TrackSnapshot(Machine machine, MachineCrudContext context)
            => _savedSnapshots.Add((Clone(machine), context));

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
                Note = source.Note,
                QrCode = source.QrCode,
                QrPayload = source.QrPayload,
                DigitalSignature = source.DigitalSignature
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
            destination.QrCode = source.QrCode;
            destination.QrPayload = source.QrPayload;
            destination.DigitalSignature = source.DigitalSignature;
        }
    }

    public sealed partial class FakeIncidentCrudService : IIncidentCrudService
    {
        private readonly List<Incident> _store = new();
        private readonly List<(Incident Entity, IncidentCrudContext Context)> _savedSnapshots = new();

        public List<Incident> Saved => _store;
        public IReadOnlyList<(Incident Entity, IncidentCrudContext Context)> SavedWithContext => _savedSnapshots;
        public IncidentCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Incident? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<IncidentCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Incident> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<Incident?> TryGetByIdAsync(int id)
            => Task.FromResult(_store.FirstOrDefault(i => i.Id == id));

        public Task<int> CreateCoreAsync(Incident incident, IncidentCrudContext context)
        {
            if (incident.Id == 0)
            {
                incident.Id = _store.Count == 0 ? 1 : _store.Max(i => i.Id) + 1;
            }

            _store.Add(Clone(incident));
            TrackSnapshot(incident, context);
            RecordTransition("Create", incident, context);
            return Task.FromResult(incident.Id);
        }

        public Task UpdateCoreAsync(Incident incident, IncidentCrudContext context)
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

            TrackSnapshot(incident, context);
            RecordTransition("Update", incident, context);
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

        private void TrackSnapshot(Incident incident, IncidentCrudContext context)
            => _savedSnapshots.Add((Clone(incident), context));

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

    public sealed partial class FakeComponentCrudService : IComponentCrudService
    {
        private readonly List<Component> _store = new();
        private readonly List<(Component Entity, ComponentCrudContext Context)> _savedSnapshots = new();

        public List<Component> Saved => _store;
        public IReadOnlyList<(Component Entity, ComponentCrudContext Context)> SavedWithContext => _savedSnapshots;
        public ComponentCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Component? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<ComponentCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Component> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Component>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Component>>(_store.ToList());

        public Task<Component?> TryGetByIdAsync(int id)
            => Task.FromResult<Component?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Component component, ComponentCrudContext context)
        {
            if (component.Id == 0)
            {
                component.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(component));
            TrackSnapshot(component, context);
            return Task.FromResult(component.Id);
        }

        public Task UpdateCoreAsync(Component component, ComponentCrudContext context)
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

            TrackSnapshot(component, context);
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

        private void TrackSnapshot(Component component, ComponentCrudContext context)
            => _savedSnapshots.Add((Clone(component), context));

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

    public sealed partial class FakeCalibrationCrudService : ICalibrationCrudService
    {
        private readonly List<Calibration> _store = new();
        private readonly List<(Calibration Entity, CalibrationCrudContext Context)> _savedSnapshots = new();

        public List<Calibration> Saved => _store;
        public IReadOnlyList<(Calibration Entity, CalibrationCrudContext Context)> SavedWithContext => _savedSnapshots;
        public CalibrationCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Calibration? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<CalibrationCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Calibration> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Calibration>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Calibration>>(_store.ToList());

        public Task<Calibration?> TryGetByIdAsync(int id)
            => Task.FromResult<Calibration?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Calibration calibration, CalibrationCrudContext context)
        {
            if (calibration.Id == 0)
            {
                calibration.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(calibration));
            TrackSnapshot(calibration, context);
            return Task.FromResult(calibration.Id);
        }

        public Task UpdateCoreAsync(Calibration calibration, CalibrationCrudContext context)
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

            TrackSnapshot(calibration, context);
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

    public class ChangeControl
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? StatusRaw { get; set; }
        public int? RequestedById { get; set; }
        public DateTime? DateRequested { get; set; }
        public int? AssignedToId { get; set; }
        public DateTime? DateAssigned { get; set; }
        public int? LastModifiedById { get; set; }
        public DateTime? LastModified { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
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

        public Task<Incident?> TryGetByIdAsync(int id)
            => Task.FromResult(_store.FirstOrDefault(i => i.Id == id));

    public partial class DatabaseService
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

            _store.Add(Clone(incident));
            TrackSnapshot(incident, context);
            return Task.FromResult(incident.Id);
        }

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
        {
            if (WarehousesExceptionFactory?.Invoke() is Exception dynamicException)
            {
                throw dynamicException;
            }

            if (WarehousesException is not null)
            {
                throw WarehousesException;
            }

            return Task.FromResult(Warehouses.Select(CloneWarehouse).ToList());
        }
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

        public void Validate(Incident incident)
        {
            if (string.IsNullOrWhiteSpace(incident.Title))
                throw new InvalidOperationException("Incident title is required.");
            if (string.IsNullOrWhiteSpace(incident.Description))
                throw new InvalidOperationException("Incident description is required.");
        }

        public string NormalizeStatus(string? status)
            => string.IsNullOrWhiteSpace(status) ? "REPORTED" : status.Trim().ToUpperInvariant();

        private void TrackSnapshot(Incident incident, IncidentCrudContext context)
            => _savedSnapshots.Add((Clone(incident), context));

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

    public sealed partial class FakeCapaCrudService : ICapaCrudService
    {
        private readonly List<CapaCase> _store = new();
        private readonly List<(CapaCase Entity, CapaCrudContext Context)> _savedSnapshots = new();

        public List<CapaCase> Saved => _store;
        public IReadOnlyList<(CapaCase Entity, CapaCrudContext Context)> SavedWithContext => _savedSnapshots;
        public CapaCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public CapaCase? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<CapaCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<CapaCase> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<CapaCase>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<CapaCase>>(_store.ToList());

        public Task<CapaCase?> TryGetByIdAsync(int id)
            => Task.FromResult<CapaCase?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(CapaCase capa, CapaCrudContext context)
        {
            if (capa.Id == 0)
            {
                capa.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(capa));
            TrackSnapshot(capa, context);
            RecordTransition("Create", capa, context);
            return Task.FromResult(capa.Id);
        }

        public Task UpdateCoreAsync(CapaCase capa, CapaCrudContext context)
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

            TrackSnapshot(capa, context);
            RecordTransition("Update", capa, context);
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

        private void TrackSnapshot(CapaCase capa, CapaCrudContext context)
            => _savedSnapshots.Add((Clone(capa), context));

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

    public sealed partial class FakeComponentCrudService : IComponentCrudService
    {
        private readonly List<Component> _store = new();
        private readonly List<(Component Entity, ComponentCrudContext Context)> _savedSnapshots = new();

        public List<Component> Saved => _store;
        public IReadOnlyList<(Component Entity, ComponentCrudContext Context)> SavedWithContext => _savedSnapshots;
        public ComponentCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Component? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<ComponentCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Component> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Component>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Component>>(_store.ToList());

        public Task<Component?> TryGetByIdAsync(int id)
            => Task.FromResult<Component?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Component component, ComponentCrudContext context)
        {
            if (component.Id == 0)
            {
                component.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(component));
            TrackSnapshot(component, context);
            return Task.FromResult(component.Id);
        }

        public Task UpdateCoreAsync(Component component, ComponentCrudContext context)
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

            TrackSnapshot(component, context);
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

        private void TrackSnapshot(Component component, ComponentCrudContext context)
            => _savedSnapshots.Add((Clone(component), context));

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

    public sealed partial class FakeCalibrationCrudService : ICalibrationCrudService
    {
        private readonly List<Calibration> _store = new();
        private readonly List<(Calibration Entity, CalibrationCrudContext Context)> _savedSnapshots = new();

        public List<Calibration> Saved => _store;
        public IReadOnlyList<(Calibration Entity, CalibrationCrudContext Context)> SavedWithContext => _savedSnapshots;
        public CalibrationCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Calibration? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<CalibrationCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Calibration> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Calibration>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Calibration>>(_store.ToList());

        public Task<Calibration?> TryGetByIdAsync(int id)
            => Task.FromResult<Calibration?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Calibration calibration, CalibrationCrudContext context)
        {
            if (calibration.Id == 0)
            {
                calibration.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(calibration));
            TrackSnapshot(calibration, context);
            return Task.FromResult(calibration.Id);
        }

        public Task UpdateCoreAsync(Calibration calibration, CalibrationCrudContext context)
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

            TrackSnapshot(calibration, context);
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

        private void TrackSnapshot(Calibration calibration, CalibrationCrudContext context)
            => _savedSnapshots.Add((Clone(calibration), context));

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

    public sealed partial class FakeMachineCrudService : IMachineCrudService
    {
        private readonly List<Machine> _store = new();
        private readonly List<(Machine Entity, MachineCrudContext Context)> _savedSnapshots = new();

        public List<Machine> Saved => _store;
        public IReadOnlyList<(Machine Entity, MachineCrudContext Context)> SavedWithContext => _savedSnapshots;
        public MachineCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Machine? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<MachineCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Machine> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Machine>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Machine>>(_store.ToList());

        public Task<Machine?> TryGetByIdAsync(int id)
            => Task.FromResult<Machine?>(_store.FirstOrDefault(m => m.Id == id));

        public Task<int> CreateCoreAsync(Machine machine, MachineCrudContext context)
        {
            if (machine.Id == 0)
            {
                machine.Id = _store.Count == 0 ? 1 : _store.Max(m => m.Id) + 1;
            }
            _store.Add(Clone(machine));
            TrackSnapshot(machine, context);
            return Task.FromResult(machine.Id);
        }

        public Task UpdateCoreAsync(Machine machine, MachineCrudContext context)
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

            TrackSnapshot(machine, context);
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

        private void TrackSnapshot(Machine machine, MachineCrudContext context)
            => _savedSnapshots.Add((Clone(machine), context));

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
                Note = source.Note,
                QrCode = source.QrCode,
                QrPayload = source.QrPayload,
                DigitalSignature = source.DigitalSignature
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
            destination.QrCode = source.QrCode;
            destination.QrPayload = source.QrPayload;
            destination.DigitalSignature = source.DigitalSignature;
        }
    }

    public sealed partial class FakeIncidentCrudService : IIncidentCrudService
    {
        private readonly List<Incident> _store = new();
        private readonly List<(Incident Entity, IncidentCrudContext Context)> _savedSnapshots = new();

        public List<Incident> Saved => _store;
        public IReadOnlyList<(Incident Entity, IncidentCrudContext Context)> SavedWithContext => _savedSnapshots;
        public IncidentCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Incident? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<IncidentCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Incident> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<Incident?> TryGetByIdAsync(int id)
            => Task.FromResult(_store.FirstOrDefault(i => i.Id == id));

        public Task<int> CreateCoreAsync(Incident incident, IncidentCrudContext context)
        {
            if (incident.Id == 0)
            {
                incident.Id = _store.Count == 0 ? 1 : _store.Max(i => i.Id) + 1;
            }

            _store.Add(Clone(incident));
            TrackSnapshot(incident, context);
            return Task.FromResult(incident.Id);
        }

        public Task UpdateCoreAsync(Incident incident, IncidentCrudContext context)
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

            TrackSnapshot(incident, context);
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

        private void TrackSnapshot(Incident incident, IncidentCrudContext context)
            => _savedSnapshots.Add((Clone(incident), context));

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

    public sealed partial class FakeChangeControlCrudService : IChangeControlCrudService
    {
        private readonly List<ChangeControl> _store = new();
        private readonly List<(ChangeControl Entity, ChangeControlCrudContext Context)> _savedSnapshots = new();

        public List<ChangeControl> Saved => _store;
        public IReadOnlyList<(ChangeControl Entity, ChangeControlCrudContext Context)> SavedWithContext => _savedSnapshots;
        public ChangeControlCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public ChangeControl? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<ChangeControlCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<ChangeControl> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<ChangeControl>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<ChangeControl>>(_store.Select(Clone).ToList());

        public Task<ChangeControl?> TryGetByIdAsync(int id)
        {
            var match = _store.FirstOrDefault(c => c.Id == id);
            return Task.FromResult(match is null ? null : Clone(match));
        }

        public Task<int> CreateCoreAsync(ChangeControl changeControl, ChangeControlCrudContext context)
        {
            if (changeControl.Id == 0)
            {
                changeControl.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(changeControl));
            TrackSnapshot(changeControl, context);
            RecordTransition("Create", changeControl, context);
            return Task.FromResult(changeControl.Id);
        }

        public Task UpdateCoreAsync(ChangeControl changeControl, ChangeControlCrudContext context)
        {
            var existing = _store.FirstOrDefault(c => c.Id == changeControl.Id);
            if (existing is null)
            {
                _store.Add(Clone(changeControl));
            }
            else
            {
                Copy(changeControl, existing);
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

    public class ChangeControl
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? StatusRaw { get; set; }
        public int? RequestedById { get; set; }
        public DateTime? DateRequested { get; set; }
        public int? AssignedToId { get; set; }
        public DateTime? DateAssigned { get; set; }
        public int? LastModifiedById { get; set; }
        public DateTime? LastModified { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
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

    public sealed partial class FakeCapaCrudService : ICapaCrudService
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

        public List<ScheduledJob> ScheduledJobs { get; } = new();
        public List<Supplier> Suppliers { get; } = new();
        public List<Part> Parts { get; } = new();
        public List<Warehouse> Warehouses { get; } = new();
        public List<Incident> Incidents { get; } = new();
        public List<CapaCase> CapaCases { get; } = new();

        public string NormalizeStatus(string? status)
            => string.IsNullOrWhiteSpace(status) ? "OPEN" : status.Trim().ToUpperInvariant();

        public Task<List<Component>> GetAllComponentsAsync()
            => Task.FromResult(Components);

        public Task<List<WorkOrder>> GetAllWorkOrdersFullAsync()
            => Task.FromResult(WorkOrders);

        public Task<List<Calibration>> GetAllCalibrationsAsync()
            => Task.FromResult(Calibrations);

        public Task<List<ScheduledJob>> GetAllScheduledJobsFullAsync()
            => Task.FromResult(ScheduledJobs);

        public Task<List<Incident>> GetAllIncidentsAsync()
            => Task.FromResult(Incidents);

        public Task<List<CapaCase>> GetAllCapaCasesAsync()
            => Task.FromResult(CapaCases);

        public Task<List<Supplier>> GetAllSuppliersAsync()
            => Task.FromResult(Suppliers);

        public Task<List<Warehouse>> GetWarehousesAsync()
        {
            if (WarehousesExceptionFactory?.Invoke() is Exception dynamicException)
            {
                throw dynamicException;
            }

            if (WarehousesException is not null)
            {
                throw WarehousesException;
            }

            return Task.FromResult(Warehouses.Select(CloneWarehouse).ToList());
        }
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

            _store.Add(Clone(capa));
        }

        private void TrackSnapshot(CapaCase capa, CapaCrudContext context)
            => _savedSnapshots.Add((Clone(capa), context));

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

    public sealed partial class FakeComponentCrudService : IComponentCrudService
    {
        private readonly List<Component> _store = new();

        public List<Component> Saved => _store;


        public Task<IReadOnlyList<Component>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Component>>(_store.ToList());

        public Task<Component?> TryGetByIdAsync(int id)
            => Task.FromResult<Component?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Component component, ComponentCrudContext context)
        {
            if (component.Id == 0)
            {
                component.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(component));
            TrackSnapshot(component, context);
            return Task.FromResult(component.Id);
        }

        public Task UpdateCoreAsync(Component component, ComponentCrudContext context)
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

            TrackSnapshot(component, context);
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

        private void TrackSnapshot(Component component, ComponentCrudContext context)
            => _savedSnapshots.Add((Clone(component), context));

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

    public sealed partial class FakeCalibrationCrudService : ICalibrationCrudService
    {
        private readonly List<Calibration> _store = new();
        private readonly List<(Calibration Entity, CalibrationCrudContext Context)> _savedSnapshots = new();

        public List<Calibration> Saved => _store;
        public IReadOnlyList<(Calibration Entity, CalibrationCrudContext Context)> SavedWithContext => _savedSnapshots;
        public CalibrationCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Calibration? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<CalibrationCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Calibration> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Calibration>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Calibration>>(_store.ToList());

        public Task<Calibration?> TryGetByIdAsync(int id)
            => Task.FromResult<Calibration?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Calibration calibration, CalibrationCrudContext context)
        {
            if (calibration.Id == 0)
            {
                calibration.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(calibration));
            TrackSnapshot(calibration, context);
            return Task.FromResult(calibration.Id);
        }

        public Task UpdateCoreAsync(Calibration calibration, CalibrationCrudContext context)
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

            TrackSnapshot(calibration, context);
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

        private void TrackSnapshot(Calibration calibration, CalibrationCrudContext context)
            => _savedSnapshots.Add((Clone(calibration), context));

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

    public sealed partial class FakeMachineCrudService : IMachineCrudService
    {
        private readonly List<Machine> _store = new();
        private readonly List<(Machine Entity, MachineCrudContext Context)> _savedSnapshots = new();

        public List<Machine> Saved => _store;
        public IReadOnlyList<(Machine Entity, MachineCrudContext Context)> SavedWithContext => _savedSnapshots;
        public MachineCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Machine? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<MachineCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Machine> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Machine>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Machine>>(_store.ToList());

        public Task<Machine?> TryGetByIdAsync(int id)
            => Task.FromResult<Machine?>(_store.FirstOrDefault(m => m.Id == id));

        public Task<int> CreateCoreAsync(Machine machine, MachineCrudContext context)
        {
            if (machine.Id == 0)
            {
                machine.Id = _store.Count == 0 ? 1 : _store.Max(m => m.Id) + 1;
            }
            _store.Add(Clone(machine));
            TrackSnapshot(machine, context);
            return Task.FromResult(machine.Id);
        }

        public Task UpdateCoreAsync(Machine machine, MachineCrudContext context)
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

            TrackSnapshot(machine, context);
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

        private void TrackSnapshot(Machine machine, MachineCrudContext context)
            => _savedSnapshots.Add((Clone(machine), context));

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
                Note = source.Note,
                QrCode = source.QrCode,
                QrPayload = source.QrPayload,
                DigitalSignature = source.DigitalSignature
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
            destination.QrCode = source.QrCode;
            destination.QrPayload = source.QrPayload;
            destination.DigitalSignature = source.DigitalSignature;
        }
    }

    public sealed partial class FakeIncidentCrudService : IIncidentCrudService
    {
        private readonly List<Incident> _store = new();
        private readonly List<(Incident Entity, IncidentCrudContext Context)> _savedSnapshots = new();

        public List<Incident> Saved => _store;
        public IReadOnlyList<(Incident Entity, IncidentCrudContext Context)> SavedWithContext => _savedSnapshots;
        public IncidentCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Incident? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<IncidentCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Incident> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<Incident?> TryGetByIdAsync(int id)
            => Task.FromResult(_store.FirstOrDefault(i => i.Id == id));

        public Task<int> CreateCoreAsync(Incident incident, IncidentCrudContext context)
        {
            if (incident.Id == 0)
            {
                incident.Id = _store.Count == 0 ? 1 : _store.Max(i => i.Id) + 1;
            }

            _store.Add(Clone(incident));
            TrackSnapshot(incident, context);
            return Task.FromResult(incident.Id);
        }

        public Task UpdateCoreAsync(Incident incident, IncidentCrudContext context)

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

            TrackSnapshot(incident, context);
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

        private void TrackSnapshot(Incident incident, IncidentCrudContext context)
            => _savedSnapshots.Add((Clone(incident), context));

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

    public sealed partial class FakeChangeControlCrudService : IChangeControlCrudService
    {
        private readonly List<ChangeControl> _store = new();
        private readonly List<(ChangeControl Entity, ChangeControlCrudContext Context)> _savedSnapshots = new();

        public List<ChangeControl> Saved => _store;
        public IReadOnlyList<(ChangeControl Entity, ChangeControlCrudContext Context)> SavedWithContext => _savedSnapshots;
        public ChangeControlCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public ChangeControl? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<ChangeControlCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<ChangeControl> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<ChangeControl>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<ChangeControl>>(_store.Select(Clone).ToList());

        public Task<ChangeControl?> TryGetByIdAsync(int id)
        {
            var match = _store.FirstOrDefault(c => c.Id == id);
            return Task.FromResult(match is null ? null : Clone(match));
        }

        public Task<int> CreateCoreAsync(ChangeControl changeControl, ChangeControlCrudContext context)
        {
            if (changeControl.Id == 0)
            {
                changeControl.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(changeControl));
            TrackSnapshot(changeControl, context);
            RecordTransition("Create", changeControl, context);
            return Task.FromResult(changeControl.Id);
        }

        public Task UpdateCoreAsync(ChangeControl changeControl, ChangeControlCrudContext context)
        {
            var existing = _store.FirstOrDefault(c => c.Id == changeControl.Id);
            if (existing is null)
            {
                _store.Add(Clone(changeControl));
            }
            else
            {
                Copy(changeControl, existing);
            }

            TrackSnapshot(changeControl, context);
            RecordTransition("Update", changeControl, context);
            return Task.CompletedTask;
        }

        public void Validate(ChangeControl changeControl)
        {
            if (string.IsNullOrWhiteSpace(changeControl.Title))
            {
                throw new InvalidOperationException("Title is required.");
            }

            if (string.IsNullOrWhiteSpace(changeControl.Code))
            {
                throw new InvalidOperationException("Code is required.");
            }
        }

        public string NormalizeStatus(string? status)
            => string.IsNullOrWhiteSpace(status) ? "Draft" : status.Trim();

        public void Seed(ChangeControl changeControl)
        {
            if (changeControl.Id == 0)
            {
                changeControl.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(changeControl));
        }

        private void TrackSnapshot(ChangeControl changeControl, ChangeControlCrudContext context)
            => _savedSnapshots.Add((Clone(changeControl), context));

        private static ChangeControl Clone(ChangeControl source)
            => new ChangeControl
            {
                Id = source.Id,
                Code = source.Code,
                Title = source.Title,
                Description = source.Description,
                StatusRaw = source.StatusRaw,
                RequestedById = source.RequestedById,
                DateRequested = source.DateRequested,
                AssignedToId = source.AssignedToId,
                DateAssigned = source.DateAssigned,
                LastModifiedById = source.LastModifiedById,
                LastModified = source.LastModified,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };

        private static void Copy(ChangeControl source, ChangeControl destination)
        {
            destination.Code = source.Code;
            destination.Title = source.Title;
            destination.Description = source.Description;
            destination.StatusRaw = source.StatusRaw;
            destination.RequestedById = source.RequestedById;
            destination.DateRequested = source.DateRequested;
            destination.AssignedToId = source.AssignedToId;
            destination.DateAssigned = source.DateAssigned;
            destination.LastModifiedById = source.LastModifiedById;
            destination.LastModified = source.LastModified;
            destination.CreatedAt = source.CreatedAt;
            destination.UpdatedAt = source.UpdatedAt;
        }
    }

    public sealed partial class FakeValidationCrudService : IValidationCrudService
    {
        private readonly List<Validation> _store = new();
        private readonly List<(Validation Entity, ValidationCrudContext Context)> _savedSnapshots = new();

        public List<Validation> Saved => _store;
        public IReadOnlyList<(Validation Entity, ValidationCrudContext Context)> SavedWithContext => _savedSnapshots;
        public ValidationCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Validation? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<ValidationCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Validation> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Validation>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Validation>>(_store.Select(Clone).ToList());

        public Task<Validation?> TryGetByIdAsync(int id)
        {
            var match = _store.FirstOrDefault(v => v.Id == id);
            return Task.FromResult(match is null ? null : Clone(match));
        }

        public Task<int> CreateCoreAsync(Validation validation, ValidationCrudContext context)
        {
            if (validation.Id == 0)
            {
                validation.Id = _store.Count == 0 ? 1 : _store.Max(v => v.Id) + 1;
            }

            _store.Add(Clone(validation));
            TrackSnapshot(validation, context);
            return Task.FromResult(validation.Id);
        }

        public Task UpdateCoreAsync(Validation validation, ValidationCrudContext context)
        {
            var existing = _store.FirstOrDefault(v => v.Id == validation.Id);
            if (existing is null)
            {
                _store.Add(Clone(validation));
            }
            else
            {
                Copy(validation, existing);
            }

            TrackSnapshot(validation, context);
            return Task.CompletedTask;
        }

        public void Validate(Validation validation)
        {
            if (string.IsNullOrWhiteSpace(validation.Type))
            {
                throw new InvalidOperationException("Validation type is required.");
            }

            if (string.IsNullOrWhiteSpace(validation.Code))
            {
                throw new InvalidOperationException("Protocol number is required.");
            }

            if (validation.MachineId is null && validation.ComponentId is null)
            {
                throw new InvalidOperationException("Select a machine or component.");
            }
        }

        public void Seed(Validation validation)
        {
            if (validation.Id == 0)
            {
                validation.Id = _store.Count == 0 ? 1 : _store.Max(v => v.Id) + 1;
            }

            _store.Add(Clone(validation));
        }

        private void TrackSnapshot(Validation validation, ValidationCrudContext context)
            => _savedSnapshots.Add((Clone(validation), context));

        private static Validation Clone(Validation source)
            => new Validation
            {
                Id = source.Id,
                Code = source.Code,
                Type = source.Type,
                MachineId = source.MachineId,
                ComponentId = source.ComponentId,
                DateStart = source.DateStart,
                DateEnd = source.DateEnd,
                Status = source.Status,
                Documentation = source.Documentation,
                DigitalSignature = source.DigitalSignature,
                SourceIp = source.SourceIp,
                SessionId = source.SessionId,
                Comment = source.Comment,
                NextDue = source.NextDue
            };

        private static void Copy(Validation source, Validation destination)
        {
            destination.Code = source.Code;
            destination.Type = source.Type;
            destination.MachineId = source.MachineId;
            destination.ComponentId = source.ComponentId;
            destination.DateStart = source.DateStart;
            destination.DateEnd = source.DateEnd;
            destination.Status = source.Status;
            destination.Documentation = source.Documentation;
            destination.DigitalSignature = source.DigitalSignature;
            destination.SourceIp = source.SourceIp;
            destination.SessionId = source.SessionId;
            destination.Comment = source.Comment;
            destination.NextDue = source.NextDue;
        }
    }

    public sealed partial class FakeCapaCrudService : ICapaCrudService
    {
        private readonly List<CapaCase> _store = new();
        private readonly List<(CapaCase Entity, CapaCrudContext Context)> _savedSnapshots = new();

        public List<CapaCase> Saved => _store;
        public IReadOnlyList<(CapaCase Entity, CapaCrudContext Context)> SavedWithContext => _savedSnapshots;
        public CapaCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public CapaCase? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<CapaCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<CapaCase> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<CapaCase>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<CapaCase>>(_store.ToList());

        public Task<CapaCase?> TryGetByIdAsync(int id)
            => Task.FromResult<CapaCase?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(CapaCase capa, CapaCrudContext context)
        {
            if (capa.Id == 0)
            {
                capa.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(capa));
            TrackSnapshot(capa, context);
            return Task.FromResult(capa.Id);
        }

        public Task UpdateCoreAsync(CapaCase capa, CapaCrudContext context)
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

            TrackSnapshot(capa, context);
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

        private void TrackSnapshot(CapaCase capa, CapaCrudContext context)
            => _savedSnapshots.Add((Clone(capa), context));

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

    public sealed partial class FakeComponentCrudService : IComponentCrudService
    {
        private readonly List<Component> _store = new();
        private readonly List<(Component Entity, ComponentCrudContext Context)> _savedSnapshots = new();

        public List<Component> Saved => _store;
        public IReadOnlyList<(Component Entity, ComponentCrudContext Context)> SavedWithContext => _savedSnapshots;
        public ComponentCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Component? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<ComponentCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Component> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Component>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Component>>(_store.ToList());

        public Task<Component?> TryGetByIdAsync(int id)
            => Task.FromResult<Component?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Component component, ComponentCrudContext context)
        {
            if (component.Id == 0)
            {
                component.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(component));
            TrackSnapshot(component, context);
            return Task.FromResult(component.Id);
        }

        public Task UpdateCoreAsync(Component component, ComponentCrudContext context)
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

            TrackSnapshot(component, context);
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

        private void TrackSnapshot(Component component, ComponentCrudContext context)
            => _savedSnapshots.Add((Clone(component), context));

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

    public sealed partial class FakeCalibrationCrudService : ICalibrationCrudService
    {
        private readonly List<Calibration> _store = new();
        private readonly List<(Calibration Entity, CalibrationCrudContext Context)> _savedSnapshots = new();

        public List<Calibration> Saved => _store;
        public IReadOnlyList<(Calibration Entity, CalibrationCrudContext Context)> SavedWithContext => _savedSnapshots;
        public CalibrationCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Calibration? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<CalibrationCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Calibration> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Calibration>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Calibration>>(_store.ToList());

        public Task<Calibration?> TryGetByIdAsync(int id)
            => Task.FromResult<Calibration?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Calibration calibration, CalibrationCrudContext context)
        {
            if (calibration.Id == 0)
            {
                calibration.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(calibration));
            TrackSnapshot(calibration, context);
            return Task.FromResult(calibration.Id);
        }

        public Task UpdateCoreAsync(Calibration calibration, CalibrationCrudContext context)
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

            TrackSnapshot(calibration, context);
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

        private void TrackSnapshot(Calibration calibration, CalibrationCrudContext context)
            => _savedSnapshots.Add((Clone(calibration), context));

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

    public sealed partial class FakeMachineCrudService : IMachineCrudService
    {
        private readonly List<Machine> _store = new();
        private readonly List<(Machine Entity, MachineCrudContext Context)> _savedSnapshots = new();

        public List<Machine> Saved => _store;
        public IReadOnlyList<(Machine Entity, MachineCrudContext Context)> SavedWithContext => _savedSnapshots;
        public MachineCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Machine? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<MachineCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Machine> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Machine>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Machine>>(_store.ToList());

        public Task<Machine?> TryGetByIdAsync(int id)
            => Task.FromResult<Machine?>(_store.FirstOrDefault(m => m.Id == id));

        public Task<int> CreateCoreAsync(Machine machine, MachineCrudContext context)
        {
            if (machine.Id == 0)
            {
                machine.Id = _store.Count == 0 ? 1 : _store.Max(m => m.Id) + 1;
            }
            _store.Add(Clone(machine));
            TrackSnapshot(machine, context);
            return Task.FromResult(machine.Id);
        }

        public Task UpdateCoreAsync(Machine machine, MachineCrudContext context)
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

            TrackSnapshot(machine, context);
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

        private void TrackSnapshot(Machine machine, MachineCrudContext context)
            => _savedSnapshots.Add((Clone(machine), context));

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
                Note = source.Note,
                QrCode = source.QrCode,
                QrPayload = source.QrPayload,
                DigitalSignature = source.DigitalSignature
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
            destination.QrCode = source.QrCode;
            destination.QrPayload = source.QrPayload;
            destination.DigitalSignature = source.DigitalSignature;
        }
    }

    public sealed partial class FakeIncidentCrudService : IIncidentCrudService
    {
        private readonly List<Incident> _store = new();
        private readonly List<(Incident Entity, IncidentCrudContext Context)> _savedSnapshots = new();

        public List<Incident> Saved => _store;
        public IReadOnlyList<(Incident Entity, IncidentCrudContext Context)> SavedWithContext => _savedSnapshots;
        public IncidentCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Incident? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<IncidentCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Incident> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<Incident?> TryGetByIdAsync(int id)
            => Task.FromResult(_store.FirstOrDefault(i => i.Id == id));

        public Task<int> CreateCoreAsync(Incident incident, IncidentCrudContext context)
        {
            if (incident.Id == 0)
            {
                incident.Id = _store.Count == 0 ? 1 : _store.Max(i => i.Id) + 1;
            }

            _store.Add(Clone(incident));
            TrackSnapshot(incident, context);
            return Task.FromResult(incident.Id);
        }

        public Task UpdateCoreAsync(Incident incident, IncidentCrudContext context)

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

            TrackSnapshot(incident, context);
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

        private void TrackSnapshot(Incident incident, IncidentCrudContext context)
            => _savedSnapshots.Add((Clone(incident), context));

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

    public sealed partial class FakeChangeControlCrudService : IChangeControlCrudService
    {
        private readonly List<ChangeControl> _store = new();
        private readonly List<(ChangeControl Entity, ChangeControlCrudContext Context)> _savedSnapshots = new();

        public List<ChangeControl> Saved => _store;
        public IReadOnlyList<(ChangeControl Entity, ChangeControlCrudContext Context)> SavedWithContext => _savedSnapshots;
        public ChangeControlCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public ChangeControl? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<ChangeControlCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<ChangeControl> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<ChangeControl>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<ChangeControl>>(_store.Select(Clone).ToList());

        public Task<ChangeControl?> TryGetByIdAsync(int id)
        {
            var match = _store.FirstOrDefault(c => c.Id == id);
            return Task.FromResult(match is null ? null : Clone(match));
        }

        public Task<int> CreateCoreAsync(ChangeControl changeControl, ChangeControlCrudContext context)
        {
            if (changeControl.Id == 0)
            {
                changeControl.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(changeControl));
            TrackSnapshot(changeControl, context);
            return Task.FromResult(changeControl.Id);
        }

        public Task UpdateCoreAsync(ChangeControl changeControl, ChangeControlCrudContext context)
        {
            var existing = _store.FirstOrDefault(c => c.Id == changeControl.Id);
            if (existing is null)
            {
                _store.Add(Clone(changeControl));
            }
            else
            {
                Copy(changeControl, existing);
            }

            TrackSnapshot(changeControl, context);
            return Task.CompletedTask;
        }

        public void Validate(ChangeControl changeControl)
        {
            if (string.IsNullOrWhiteSpace(changeControl.Title))
            {
                throw new InvalidOperationException("Title is required.");
            }

            if (string.IsNullOrWhiteSpace(changeControl.Code))
            {
                throw new InvalidOperationException("Code is required.");
            }
        }

        public string NormalizeStatus(string? status)
            => string.IsNullOrWhiteSpace(status) ? "Draft" : status.Trim();

        public void Seed(ChangeControl changeControl)
        {
            if (changeControl.Id == 0)
            {
                changeControl.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(changeControl));
        }

        private void TrackSnapshot(ChangeControl changeControl, ChangeControlCrudContext context)
            => _savedSnapshots.Add((Clone(changeControl), context));

        private static ChangeControl Clone(ChangeControl source)
            => new ChangeControl
            {
                Id = source.Id,
                Code = source.Code,
                Title = source.Title,
                Description = source.Description,
                StatusRaw = source.StatusRaw,
                RequestedById = source.RequestedById,
                DateRequested = source.DateRequested,
                AssignedToId = source.AssignedToId,
                DateAssigned = source.DateAssigned,
                LastModifiedById = source.LastModifiedById,
                LastModified = source.LastModified,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };

        private static void Copy(ChangeControl source, ChangeControl destination)
        {
            destination.Code = source.Code;
            destination.Title = source.Title;
            destination.Description = source.Description;
            destination.StatusRaw = source.StatusRaw;
            destination.RequestedById = source.RequestedById;
            destination.DateRequested = source.DateRequested;
            destination.AssignedToId = source.AssignedToId;
            destination.DateAssigned = source.DateAssigned;
            destination.LastModifiedById = source.LastModifiedById;
            destination.LastModified = source.LastModified;
            destination.CreatedAt = source.CreatedAt;
            destination.UpdatedAt = source.UpdatedAt;
        }
    }

    public sealed partial class FakeValidationCrudService : IValidationCrudService
    {
        private readonly List<Validation> _store = new();
        private readonly List<(Validation Entity, ValidationCrudContext Context)> _savedSnapshots = new();

        public List<Validation> Saved => _store;
        public IReadOnlyList<(Validation Entity, ValidationCrudContext Context)> SavedWithContext => _savedSnapshots;
        public ValidationCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Validation? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<ValidationCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Validation> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Validation>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Validation>>(_store.Select(Clone).ToList());

        public Task<Validation?> TryGetByIdAsync(int id)
        {
            var match = _store.FirstOrDefault(v => v.Id == id);
            return Task.FromResult(match is null ? null : Clone(match));
        }

        public Task<int> CreateCoreAsync(Validation validation, ValidationCrudContext context)
        {
            if (validation.Id == 0)
            {
                validation.Id = _store.Count == 0 ? 1 : _store.Max(v => v.Id) + 1;
            }

            _store.Add(Clone(validation));
            TrackSnapshot(validation, context);
            return Task.FromResult(validation.Id);
        }

        public Task UpdateCoreAsync(Validation validation, ValidationCrudContext context)
        {
            var existing = _store.FirstOrDefault(v => v.Id == validation.Id);
            if (existing is null)
            {
                _store.Add(Clone(validation));
            }
            else
            {
                Copy(validation, existing);
            }

            TrackSnapshot(validation, context);
            return Task.CompletedTask;
        }

        public void Validate(Validation validation)
        {
            if (string.IsNullOrWhiteSpace(validation.Type))
            {
                throw new InvalidOperationException("Validation type is required.");
            }

            if (string.IsNullOrWhiteSpace(validation.Code))
            {
                throw new InvalidOperationException("Protocol number is required.");
            }

            if (validation.MachineId is null && validation.ComponentId is null)
            {
                throw new InvalidOperationException("Select a machine or component.");
            }
        }

        public void Seed(Validation validation)
        {
            if (validation.Id == 0)
            {
                validation.Id = _store.Count == 0 ? 1 : _store.Max(v => v.Id) + 1;
            }

            _store.Add(Clone(validation));
        }

        private void TrackSnapshot(Validation validation, ValidationCrudContext context)
            => _savedSnapshots.Add((Clone(validation), context));

        private static Validation Clone(Validation source)
            => new Validation
            {
                Id = source.Id,
                Code = source.Code,
                Type = source.Type,
                MachineId = source.MachineId,
                ComponentId = source.ComponentId,
                DateStart = source.DateStart,
                DateEnd = source.DateEnd,
                Status = source.Status,
                Documentation = source.Documentation,
                DigitalSignature = source.DigitalSignature,
                SourceIp = source.SourceIp,
                SessionId = source.SessionId,
                Comment = source.Comment,
                NextDue = source.NextDue
            };

        private static void Copy(Validation source, Validation destination)
        {
            destination.Code = source.Code;
            destination.Type = source.Type;
            destination.MachineId = source.MachineId;
            destination.ComponentId = source.ComponentId;
            destination.DateStart = source.DateStart;
            destination.DateEnd = source.DateEnd;
            destination.Status = source.Status;
            destination.Documentation = source.Documentation;
            destination.DigitalSignature = source.DigitalSignature;
            destination.SourceIp = source.SourceIp;
            destination.SessionId = source.SessionId;
            destination.Comment = source.Comment;
            destination.NextDue = source.NextDue;
        }
    }

    public sealed partial class FakeCapaCrudService : ICapaCrudService
    {
        private readonly List<CapaCase> _store = new();
        private readonly List<(CapaCase Entity, CapaCrudContext Context)> _savedSnapshots = new();

        public List<CapaCase> Saved => _store;
        public IReadOnlyList<(CapaCase Entity, CapaCrudContext Context)> SavedWithContext => _savedSnapshots;
        public CapaCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public CapaCase? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<CapaCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<CapaCase> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<CapaCase>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<CapaCase>>(_store.ToList());

        public Task<CapaCase?> TryGetByIdAsync(int id)
            => Task.FromResult<CapaCase?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(CapaCase capa, CapaCrudContext context)
        {
            if (capa.Id == 0)
            {
                capa.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(capa));
            TrackSnapshot(capa, context);
            return Task.FromResult(capa.Id);
        }

        public Task UpdateCoreAsync(CapaCase capa, CapaCrudContext context)
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

            TrackSnapshot(capa, context);
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

        private void TrackSnapshot(CapaCase capa, CapaCrudContext context)
            => _savedSnapshots.Add((Clone(capa), context));

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

    public sealed partial class FakeComponentCrudService : IComponentCrudService
    {
        private readonly List<Component> _store = new();
        private readonly List<(Component Entity, ComponentCrudContext Context)> _savedSnapshots = new();

        public List<Component> Saved => _store;
        public IReadOnlyList<(Component Entity, ComponentCrudContext Context)> SavedWithContext => _savedSnapshots;
        public ComponentCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Component? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<ComponentCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Component> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Component>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Component>>(_store.ToList());

        public Task<Component?> TryGetByIdAsync(int id)
            => Task.FromResult<Component?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Component component, ComponentCrudContext context)
        {
            if (component.Id == 0)
            {
                component.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(component));
            TrackSnapshot(component, context);
            return Task.FromResult(component.Id);
        }

        public Task UpdateCoreAsync(Component component, ComponentCrudContext context)
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

            TrackSnapshot(component, context);
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

        private void TrackSnapshot(Component component, ComponentCrudContext context)
            => _savedSnapshots.Add((Clone(component), context));

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

    public sealed partial class FakeCalibrationCrudService : ICalibrationCrudService
    {
        private readonly List<Calibration> _store = new();
        private readonly List<(Calibration Entity, CalibrationCrudContext Context)> _savedSnapshots = new();

        public List<Calibration> Saved => _store;
        public IReadOnlyList<(Calibration Entity, CalibrationCrudContext Context)> SavedWithContext => _savedSnapshots;
        public CalibrationCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Calibration? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<CalibrationCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Calibration> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Calibration>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Calibration>>(_store.ToList());

        public Task<Calibration?> TryGetByIdAsync(int id)
            => Task.FromResult<Calibration?>(_store.FirstOrDefault(c => c.Id == id));

        public Task<int> CreateCoreAsync(Calibration calibration, CalibrationCrudContext context)
        {
            if (calibration.Id == 0)
            {
                calibration.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            }

            _store.Add(Clone(calibration));
            TrackSnapshot(calibration, context);
            return Task.FromResult(calibration.Id);
        }

        public Task UpdateCoreAsync(Calibration calibration, CalibrationCrudContext context)
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

            TrackSnapshot(calibration, context);
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

        private void TrackSnapshot(Calibration calibration, CalibrationCrudContext context)
            => _savedSnapshots.Add((Clone(calibration), context));

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
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using YasGMP.Services;
    using YasGMP.Wpf.Services;

    public sealed class InspectorField
    {
        public InspectorField(string label, string? value)
            : this(label, value, string.Empty, string.Empty, string.Empty)
        {
        }

        public InspectorField(string label, string? value, string automationName, string automationId, string automationTooltip)
        {
            Label = label;
            Value = value ?? string.Empty;
            AutomationName = automationName;
            AutomationId = automationId;
            AutomationTooltip = automationTooltip;
        }

        public string Label { get; }

        public string Value { get; }

        public string AutomationName { get; }

        public string AutomationId { get; }

        public string AutomationTooltip { get; }
    }

    public sealed class InspectorContext
    {
        public InspectorContext(string moduleKey, string title, string? recordKey, string subtitle, IReadOnlyList<InspectorField> fields)
        {
            ModuleKey = moduleKey;
            Title = title;
            RecordKey = recordKey ?? string.Empty;
            Subtitle = subtitle;
            Fields = fields ?? new List<InspectorField>();
        }

        public string ModuleKey { get; }

        public string Title { get; }

        public string RecordKey { get; }

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
            IModuleNavigationService ___,
            AuditService? audit = null)
            : base(key, title, _, __, ___, audit)
        {
            Database = database;
        }

        protected DatabaseService Database { get; }
    }
}

    public sealed partial class FakePartCrudService : IPartCrudService
    {
        private readonly List<Part> _store = new();
        private readonly List<(Part Entity, PartCrudContext Context)> _savedSnapshots = new();

        public List<Part> Saved => _store;
        public IReadOnlyList<(Part Entity, PartCrudContext Context)> SavedWithContext => _savedSnapshots;
        public PartCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Part? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<PartCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Part> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Part>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Part>>(_store.ToList());

        public Task<Part?> TryGetByIdAsync(int id)
            => Task.FromResult<Part?>(_store.FirstOrDefault(p => p.Id == id));

        public Task<int> CreateCoreAsync(Part part, PartCrudContext context)
        {
            if (part.Id == 0)
            {
                part.Id = _store.Count == 0 ? 1 : _store.Max(p => p.Id) + 1;
            }

            _store.Add(Clone(part));
            TrackSnapshot(part, context);
            return Task.FromResult(part.Id);
        }

        public Task UpdateCoreAsync(Part part, PartCrudContext context)
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

            TrackSnapshot(part, context);
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

        private void TrackSnapshot(Part part, PartCrudContext context)
            => _savedSnapshots.Add((Clone(part), context));

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

    public sealed partial class FakeSupplierCrudService : ISupplierCrudService
    {
        private readonly List<Supplier> _store = new();
        private readonly List<(Supplier Entity, SupplierCrudContext Context)> _savedSnapshots = new();

        public List<Supplier> Saved => _store;
        public IReadOnlyList<(Supplier Entity, SupplierCrudContext Context)> SavedWithContext => _savedSnapshots;
        public SupplierCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Supplier? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<SupplierCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Supplier> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<IReadOnlyList<Supplier>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Supplier>>(_store.ToList());

        public Task<Supplier?> TryGetByIdAsync(int id)
            => Task.FromResult<Supplier?>(_store.FirstOrDefault(s => s.Id == id));

        public Task<int> CreateCoreAsync(Supplier supplier, SupplierCrudContext context)
        {
            if (supplier.Id == 0)
            {
                supplier.Id = _store.Count == 0 ? 1 : _store.Max(s => s.Id) + 1;
            }

            _store.Add(Clone(supplier));
            TrackSnapshot(supplier, context);
            return Task.FromResult(supplier.Id);
        }

        public Task UpdateCoreAsync(Supplier supplier, SupplierCrudContext context)
        {
            var existing = _store.FirstOrDefault(s => s.Id == supplier.Id);
            if (existing is null)
            {
                _store.Add(Clone(supplier));
            }
            else
            {
                Copy(supplier, existing);
            }

            TrackSnapshot(supplier, context);
            return Task.CompletedTask;
        }

        public void Validate(Supplier supplier)
        {
            if (string.IsNullOrWhiteSpace(supplier.Name))
                throw new InvalidOperationException("Supplier name is required.");
            if (string.IsNullOrWhiteSpace(supplier.Email))
                throw new InvalidOperationException("Supplier email address is required.");
            if (string.IsNullOrWhiteSpace(supplier.VatNumber))
                throw new InvalidOperationException("VAT number is required.");
            if (supplier.CooperationEnd is not null && supplier.CooperationStart is not null
                && supplier.CooperationEnd < supplier.CooperationStart)
                throw new InvalidOperationException("Cooperation end cannot precede the start date.");
        }

        public string NormalizeStatus(string? status)
            => SupplierCrudExtensions.NormalizeStatusDefault(status);

        private void TrackSnapshot(Supplier supplier, SupplierCrudContext context)
            => _savedSnapshots.Add((Clone(supplier), context));

        private static Supplier Clone(Supplier source)
        {
            return new Supplier
            {
                Id = source.Id,
                Name = source.Name,
                SupplierType = source.SupplierType,
                Status = source.Status,
                Email = source.Email,
                Phone = source.Phone,
                Website = source.Website,
                VatNumber = source.VatNumber,
                Address = source.Address,
                City = source.City,
                Country = source.Country,
                Notes = source.Notes,
                ContractFile = source.ContractFile,
                RiskLevel = source.RiskLevel,
                IsQualified = source.IsQualified,
                CooperationStart = source.CooperationStart,
                CooperationEnd = source.CooperationEnd,
                RegisteredAuthorities = source.RegisteredAuthorities,
                DigitalSignature = source.DigitalSignature
            };
        }

        private static void Copy(Supplier source, Supplier destination)
        {
            destination.Name = source.Name;
            destination.SupplierType = source.SupplierType;
            destination.Status = source.Status;
            destination.Email = source.Email;
            destination.Phone = source.Phone;
            destination.Website = source.Website;
            destination.VatNumber = source.VatNumber;
            destination.Address = source.Address;
            destination.City = source.City;
            destination.Country = source.Country;
            destination.Notes = source.Notes;
            destination.ContractFile = source.ContractFile;
            destination.RiskLevel = source.RiskLevel;
            destination.IsQualified = source.IsQualified;
            destination.CooperationStart = source.CooperationStart;
            destination.CooperationEnd = source.CooperationEnd;
            destination.RegisteredAuthorities = source.RegisteredAuthorities;
            destination.DigitalSignature = source.DigitalSignature;
        }
    }

    public sealed partial class FakeWarehouseCrudService : IWarehouseCrudService
    {
        private readonly List<Warehouse> _store = new();
        private readonly List<(Warehouse Entity, WarehouseCrudContext Context)> _savedSnapshots = new();

        public List<Warehouse> Saved => _store;
        public IReadOnlyList<(Warehouse Entity, WarehouseCrudContext Context)> SavedWithContext => _savedSnapshots;
        public WarehouseCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public Warehouse? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<WarehouseCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<Warehouse> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public List<WarehouseStockSnapshot> StockSnapshots { get; } = new();

        public List<InventoryMovementEntry> Movements { get; } = new();

        public List<InventoryTransactionRequest> ExecutedTransactions { get; } = new();

        public List<ElectronicSignatureDialogResult> ExecutedSignatures { get; } = new();

        public Task<IReadOnlyList<Warehouse>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<Warehouse>>(_store.ToList());

        public Task<Warehouse?> TryGetByIdAsync(int id)
            => Task.FromResult<Warehouse?>(_store.FirstOrDefault(w => w.Id == id));

        public Task<int> CreateCoreAsync(Warehouse warehouse, WarehouseCrudContext context)
        {
            if (warehouse.Id == 0)
            {
                warehouse.Id = _store.Count == 0 ? 1 : _store.Max(w => w.Id) + 1;
            }

            _store.Add(Clone(warehouse));
            TrackSnapshot(warehouse, context);
            return Task.FromResult(warehouse.Id);
        }

        public Task UpdateCoreAsync(Warehouse warehouse, WarehouseCrudContext context)
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

            TrackSnapshot(warehouse, context);
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

        public Task<InventoryTransactionResult> ExecuteInventoryTransactionAsync(
            InventoryTransactionRequest request,
            WarehouseCrudContext context,
            ElectronicSignatureDialogResult signatureResult,
            CancellationToken cancellationToken = default)
        {
            ExecutedTransactions.Add(request);
            ExecutedSignatures.Add(signatureResult);
            return Task.FromResult(new InventoryTransactionResult
            {
                Type = request.Type,
                PartId = request.PartId,
                WarehouseId = request.WarehouseId,
                Quantity = request.Quantity,
                Document = request.Document,
                Note = request.Note,
                Signature = signatureResult
            });
        }

        private void TrackSnapshot(Warehouse warehouse, WarehouseCrudContext context)
            => _savedSnapshots.Add((Clone(warehouse), context));

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

    public sealed partial class FakeScheduledJobCrudService : IScheduledJobCrudService
    {
        private readonly List<ScheduledJob> _store = new();
        private readonly List<(ScheduledJob Entity, ScheduledJobCrudContext Context)> _savedSnapshots = new();
        private readonly List<(ScheduledJob Entity, ScheduledJobCrudContext Context)> _createdSnapshots = new();
        private readonly List<(ScheduledJob Entity, ScheduledJobCrudContext Context)> _updatedSnapshots = new();

        public List<ScheduledJob> Saved => _store;
        public IReadOnlyList<(ScheduledJob Entity, ScheduledJobCrudContext Context)> SavedWithContext => _savedSnapshots;
        public ScheduledJobCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public ScheduledJob? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<ScheduledJobCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<ScheduledJob> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public IReadOnlyList<(ScheduledJob Entity, ScheduledJobCrudContext Context)> CreatedWithContext => _createdSnapshots;
        public IReadOnlyList<(ScheduledJob Entity, ScheduledJobCrudContext Context)> UpdatedWithContext => _updatedSnapshots;

        public ScheduledJobCrudContext? LastCreatedContext => _createdSnapshots.Count == 0 ? null : _createdSnapshots[^1].Context;
        public ScheduledJobCrudContext? LastUpdatedContext => _updatedSnapshots.Count == 0 ? null : _updatedSnapshots[^1].Context;

        public List<int> Executed { get; } = new();

        public List<int> Acknowledged { get; } = new();

        public List<(int JobId, ScheduledJobCrudContext Context)> ExecutionLog { get; } = new();

        public List<(int JobId, ScheduledJobCrudContext Context)> AcknowledgementLog { get; } = new();

        public void Seed(ScheduledJob job)
        {
            var existing = _store.FirstOrDefault(j => j.Id == job.Id);
            if (existing is null)
            {
                _store.Add(Clone(job));
            }
            else
            {
                Copy(job, existing);
            }
        }

        public Task<ScheduledJob?> TryGetByIdAsync(int id)
            => Task.FromResult<ScheduledJob?>(_store.FirstOrDefault(j => j.Id == id));

        public Task<int> CreateCoreAsync(ScheduledJob job, ScheduledJobCrudContext context)
        {
            if (job.Id == 0)
            {
                job.Id = _store.Count == 0 ? 1 : _store.Max(j => j.Id) + 1;
            }

            _store.Add(Clone(job));
            TrackSnapshot(job, context, isUpdate: false);
            return Task.FromResult(job.Id);
        }

        public Task UpdateCoreAsync(ScheduledJob job, ScheduledJobCrudContext context)
        {
            var existing = _store.FirstOrDefault(j => j.Id == job.Id);
            if (existing is null)
            {
                _store.Add(Clone(job));
            }
            else
            {
                Copy(job, existing);
            }

            TrackSnapshot(job, context, isUpdate: true);
            return Task.CompletedTask;
        }

        public Task ExecuteAsync(int jobId, ScheduledJobCrudContext context)
        {
            Executed.Add(jobId);
            ExecutionLog.Add((jobId, context));
            var job = _store.FirstOrDefault(j => j.Id == jobId);
            if (job is not null)
            {
                job.Status = "in_progress";
                job.LastExecuted = DateTime.UtcNow;
            }

            return Task.CompletedTask;
        }

        public Task AcknowledgeAsync(int jobId, ScheduledJobCrudContext context)
        {
            Acknowledged.Add(jobId);
            AcknowledgementLog.Add((jobId, context));
            var job = _store.FirstOrDefault(j => j.Id == jobId);
            if (job is not null)
            {
                job.Status = "acknowledged";
            }

            return Task.CompletedTask;
        }

        public void Validate(ScheduledJob job)
        {
            if (string.IsNullOrWhiteSpace(job.Name))
                throw new InvalidOperationException("Job name is required.");
            if (string.IsNullOrWhiteSpace(job.JobType))
                throw new InvalidOperationException("Job type is required.");
            if (string.IsNullOrWhiteSpace(job.Status))
                throw new InvalidOperationException("Status is required.");
            if (string.IsNullOrWhiteSpace(job.RecurrencePattern))
                throw new InvalidOperationException("Recurrence is required.");
        }

        public string NormalizeStatus(string? status)
            => string.IsNullOrWhiteSpace(status) ? "scheduled" : status.Trim().ToLowerInvariant();

        private void TrackSnapshot(ScheduledJob job, ScheduledJobCrudContext context, bool isUpdate)
        {
            var snapshot = (Clone(job), context);
            _savedSnapshots.Add(snapshot);
            if (isUpdate)
            {
                _updatedSnapshots.Add(snapshot);
            }
            else
            {
                _createdSnapshots.Add(snapshot);
            }
        }

        private static ScheduledJob Clone(ScheduledJob source)
        {
            return new ScheduledJob
            {
                Id = source.Id,
                Name = source.Name,
                JobType = source.JobType,
                Status = source.Status,
                NextDue = source.NextDue,
                RecurrencePattern = source.RecurrencePattern,
                EntityType = source.EntityType,
                EntityId = source.EntityId,
                CronExpression = source.CronExpression,
                Comment = source.Comment,
                IsCritical = source.IsCritical,
                NeedsAcknowledgment = source.NeedsAcknowledgment,
                AlertOnFailure = source.AlertOnFailure,
                Retries = source.Retries,
                MaxRetries = source.MaxRetries,
                EscalationNote = source.EscalationNote,
                LastExecuted = source.LastExecuted,
                LastResult = source.LastResult,
                LastError = source.LastError,
                ExtraParams = source.ExtraParams,
                CreatedById = source.CreatedById,
                CreatedBy = source.CreatedBy,
                CreatedAt = source.CreatedAt,
                LastModifiedById = source.LastModifiedById,
                DeviceInfo = source.DeviceInfo,
                SessionId = source.SessionId,
                IpAddress = source.IpAddress
            };
        }

        private static void Copy(ScheduledJob source, ScheduledJob destination)
        {
            destination.Name = source.Name;
            destination.JobType = source.JobType;
            destination.Status = source.Status;
            destination.NextDue = source.NextDue;
            destination.RecurrencePattern = source.RecurrencePattern;
            destination.EntityType = source.EntityType;
            destination.EntityId = source.EntityId;
            destination.CronExpression = source.CronExpression;
            destination.Comment = source.Comment;
            destination.IsCritical = source.IsCritical;
            destination.NeedsAcknowledgment = source.NeedsAcknowledgment;
            destination.AlertOnFailure = source.AlertOnFailure;
            destination.Retries = source.Retries;
            destination.MaxRetries = source.MaxRetries;
            destination.EscalationNote = source.EscalationNote;
            destination.LastExecuted = source.LastExecuted;
            destination.LastResult = source.LastResult;
            destination.LastError = source.LastError;
            destination.ExtraParams = source.ExtraParams;
            destination.CreatedById = source.CreatedById;
            destination.CreatedBy = source.CreatedBy;
            destination.CreatedAt = source.CreatedAt;
            destination.LastModifiedById = source.LastModifiedById;
            destination.DeviceInfo = source.DeviceInfo;
            destination.SessionId = source.SessionId;
            destination.IpAddress = source.IpAddress;
        }
    }

    public sealed partial class FakeUserCrudService : IUserCrudService
    {
        private readonly List<User> _users = new();
        private readonly List<Role> _roles = new();
        private readonly List<(User Entity, UserCrudContext Context)> _savedSnapshots = new();

        public List<User> CreatedUsers { get; } = new();

        public List<User> UpdatedUsers { get; } = new();

        public List<(int UserId, IReadOnlyCollection<int> Roles)> RoleAssignments { get; } = new();

        public IReadOnlyList<(User Entity, UserCrudContext Context)> SavedWithContext => _savedSnapshots;
        public UserCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public User? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<UserCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<User> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public void SeedUser(User user)
        {
            var existing = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existing is null)
            {
                _users.Add(Clone(user));
            }
            else
            {
                Copy(user, existing);
            }
        }

        public void SeedRole(Role role)
        {
            var existing = _roles.FirstOrDefault(r => r.Id == role.Id);
            if (existing is null)
            {
                _roles.Add(Clone(role));
            }
            else
            {
                existing.Name = role.Name;
                existing.Description = role.Description;
            }
        }

        public Task<IReadOnlyList<User>> GetAllAsync()
            => Task.FromResult<IReadOnlyList<User>>(_users.Select(Clone).ToList());

        public Task<User?> TryGetByIdAsync(int id)
        {
            var match = _users.FirstOrDefault(u => u.Id == id);
            return Task.FromResult<User?>(match is null ? null : Clone(match));
        }

        public Task<IReadOnlyList<Role>> GetAllRolesAsync()
            => Task.FromResult<IReadOnlyList<Role>>(_roles.Select(Clone).ToList());

        public Task<int> CreateCoreAsync(User user, string password, UserCrudContext context)
        {
            if (user.Id == 0)
            {
                user.Id = _users.Count == 0 ? 1 : _users.Max(u => u.Id) + 1;
            }

            user.PasswordHash = password;
            var clone = Clone(user);
            _users.Add(clone);
            CreatedUsers.Add(Clone(user));
            TrackSnapshot(user, context);
            return Task.FromResult(user.Id);
        }

        public Task UpdateCoreAsync(User user, string? password, UserCrudContext context)
        {
            var existing = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existing is null)
            {
                _users.Add(Clone(user));
            }
            else
            {
                Copy(user, existing);
                if (!string.IsNullOrWhiteSpace(password))
                {
                    existing.PasswordHash = password!;
                }
            }

            UpdatedUsers.Add(Clone(user));
            TrackSnapshot(user, context);
            return Task.CompletedTask;
        }

        public Task UpdateRoleAssignmentsAsync(int userId, IReadOnlyCollection<int> roleIds, UserCrudContext context)
        {
            RoleAssignments.Add((userId, roleIds.ToArray()));
            return Task.CompletedTask;
        }

        public Task DeactivateAsync(int userId, UserCrudContext context)
        {
            var match = _users.FirstOrDefault(u => u.Id == userId);
            if (match is not null)
            {
                match.Active = false;
            }

            return Task.CompletedTask;
        }

        public void Validate(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Username))
                throw new InvalidOperationException("Username required");
            if (string.IsNullOrWhiteSpace(user.FullName))
                throw new InvalidOperationException("Full name required");
            if (string.IsNullOrWhiteSpace(user.Role))
                throw new InvalidOperationException("Role required");
        }

        private void TrackSnapshot(User user, UserCrudContext context)
            => _savedSnapshots.Add((Clone(user), context));

        private static User Clone(User source)
        {
            return new User
            {
                Id = source.Id,
                Username = source.Username,
                PasswordHash = source.PasswordHash,
                FullName = source.FullName,
                Email = source.Email,
                Phone = source.Phone,
                Role = source.Role,
                DepartmentName = source.DepartmentName,
                Active = source.Active,
                IsLocked = source.IsLocked,
                IsTwoFactorEnabled = source.IsTwoFactorEnabled,
                RoleIds = source.RoleIds?.ToArray() ?? Array.Empty<int>()
            };
        }

        private static void Copy(User source, User destination)
        {
            destination.Username = source.Username;
            destination.PasswordHash = source.PasswordHash;
            destination.FullName = source.FullName;
            destination.Email = source.Email;
            destination.Phone = source.Phone;
            destination.Role = source.Role;
            destination.DepartmentName = source.DepartmentName;
            destination.Active = source.Active;
            destination.IsLocked = source.IsLocked;
            destination.IsTwoFactorEnabled = source.IsTwoFactorEnabled;
            destination.RoleIds = source.RoleIds?.ToArray() ?? Array.Empty<int>();
        }

        private static Role Clone(Role source)
            => new() { Id = source.Id, Name = source.Name, Description = source.Description };
    }

    public sealed partial class FakeWorkOrderCrudService : IWorkOrderCrudService
    {
        private readonly List<WorkOrder> _store = new();
        private readonly List<(WorkOrder Entity, WorkOrderCrudContext Context)> _savedSnapshots = new();

        public List<WorkOrder> Saved => _store;
        public IReadOnlyList<(WorkOrder Entity, WorkOrderCrudContext Context)> SavedWithContext => _savedSnapshots;
        public WorkOrderCrudContext? LastSavedContext => _savedSnapshots.Count == 0 ? null : _savedSnapshots[^1].Context;
        public WorkOrder? LastSavedEntity => _savedSnapshots.Count == 0 ? null : Clone(_savedSnapshots[^1].Entity);
        public IEnumerable<WorkOrderCrudContext> SavedContexts => _savedSnapshots.Select(tuple => tuple.Context);
        public IEnumerable<WorkOrder> SavedEntities => _savedSnapshots.Select(tuple => Clone(tuple.Entity));

        public Task<WorkOrder?> TryGetByIdAsync(int id)
            => Task.FromResult<WorkOrder?>(_store.FirstOrDefault(w => w.Id == id));

        public Task<int> CreateCoreAsync(WorkOrder workOrder, WorkOrderCrudContext context)
        {
            if (workOrder.Id == 0)
            {
                workOrder.Id = _store.Count == 0 ? 1 : _store.Max(w => w.Id) + 1;
            }

            _store.Add(Clone(workOrder));
            TrackSnapshot(workOrder, context);
            return Task.FromResult(workOrder.Id);
        }

        public Task UpdateCoreAsync(WorkOrder workOrder, WorkOrderCrudContext context)
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

            TrackSnapshot(workOrder, context);
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

        private void TrackSnapshot(WorkOrder workOrder, WorkOrderCrudContext context)
            => _savedSnapshots.Add((Clone(workOrder), context));

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
