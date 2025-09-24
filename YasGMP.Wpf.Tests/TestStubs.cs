using System;
using System.Collections.Generic;
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
        public string? FullName { get; set; }
        public string? Username { get; set; }
    }

    public class Machine
    {
        public int Id { get; set; }
        public string? Name { get; set; }
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
        public List<WorkOrder> WorkOrders { get; } = new();
        public List<Calibration> Calibrations { get; } = new();

        public Task<List<Asset>> GetAllAssetsFullAsync()
            => Task.FromResult(Assets);

        public Task<List<WorkOrder>> GetAllWorkOrdersFullAsync()
            => Task.FromResult(WorkOrders);

        public Task<List<Calibration>> GetAllCalibrationsAsync()
            => Task.FromResult(Calibrations);
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
