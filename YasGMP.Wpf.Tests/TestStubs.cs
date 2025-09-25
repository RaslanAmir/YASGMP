using System;
using System.Collections.Generic;
using System.Linq;
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

        public Task<List<Asset>> GetAllAssetsFullAsync()
            => Task.FromResult(Assets);

        public Task<List<Component>> GetAllComponentsAsync()
            => Task.FromResult(Components);

        public Task<List<WorkOrder>> GetAllWorkOrdersFullAsync()
            => Task.FromResult(WorkOrders);

        public Task<List<Calibration>> GetAllCalibrationsAsync()
            => Task.FromResult(Calibrations);

        public Task<List<Supplier>> GetAllSuppliersAsync()
            => Task.FromResult(Suppliers);
    }
}

namespace YasGMP.Services.Interfaces
{
    using System;
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
