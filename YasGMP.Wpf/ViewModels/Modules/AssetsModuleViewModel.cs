using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed partial class AssetsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public new const string ModuleKey = "Assets";

    private readonly IMachineCrudService _machineService;
    private readonly IAuthContext _authContext;
    private Machine? _loadedMachine;
    private AssetEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;

    public AssetsModuleViewModel(
        DatabaseService databaseService,
        IMachineCrudService machineService,
        IAuthContext authContext,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Assets", databaseService, cflDialogService, shellInteraction, navigation)
    {
        _machineService = machineService ?? throw new ArgumentNullException(nameof(machineService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        Editor = AssetEditor.CreateEmpty();
        StatusOptions = new ReadOnlyCollection<string>(new[]
        {
            "active",
            "maintenance",
            "reserved",
            "decommissioned",
            "scrapped"
        });
    }

    /// <summary>Editor payload bound to the form fields.</summary>
    [ObservableProperty]
    private AssetEditor _editor;

    /// <summary>Indicates whether form controls are writable (Add/Update modes).</summary>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Canonical status options rendered in the combo-box.</summary>
    public IReadOnlyList<string> StatusOptions { get; }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var machines = await _machineService.GetAllAsync().ConfigureAwait(false);
        return machines.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var sample = new[]
        {
            new Machine
            {
                Id = 1001,
                Name = "Autoclave",
                Code = "AUTO-001",
                Status = "active",
                Description = "Steam sterilizer",
                Manufacturer = "Steris",
                Location = "Building A",
                InstallDate = DateTime.UtcNow.AddYears(-3)
            },
            new Machine
            {
                Id = 1002,
                Name = "pH Meter",
                Code = "LAB-PH-12",
                Status = "maintenance",
                Description = "Metrohm pH meter",
                Manufacturer = "Metrohm",
                Location = "QC Lab",
                InstallDate = DateTime.UtcNow.AddYears(-2)
            }
        };

        return sample.Select(ToRecord).ToList();
    }

    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var machines = await _machineService.GetAllAsync().ConfigureAwait(false);
        var items = machines
            .Select(machine =>
            {
                var key = machine.Id.ToString(CultureInfo.InvariantCulture);
                var label = string.IsNullOrWhiteSpace(machine.Name) ? key : machine.Name;
                var descriptionParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(machine.Code))
                {
                    descriptionParts.Add(machine.Code);
                }

                if (!string.IsNullOrWhiteSpace(machine.Location))
                {
                    descriptionParts.Add(machine.Location!);
                }

                if (!string.IsNullOrWhiteSpace(machine.Status))
                {
                    descriptionParts.Add(machine.Status!);
                }

                var description = descriptionParts.Count > 0
                    ? string.Join(" • ", descriptionParts)
                    : null;

                return new CflItem(key, label, description);
            })
            .ToList();

        return new CflRequest("Select Asset", items);
    }

    protected override Task OnCflSelectionAsync(CflResult result)
    {
        var search = result.Selected.Label;
        var match = Records.FirstOrDefault(r => r.Key == result.Selected.Key);
        if (match is not null)
        {
            SelectedRecord = match;
            search = match.Title;
        }

        SearchText = search;
        StatusMessage = $"Filtered {Title} by \"{search}\".";
        return Task.CompletedTask;
    }

    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedMachine = null;
            SetEditor(AssetEditor.CreateEmpty());
            return;
        }

        if (IsInEditMode)
        {
            return;
        }

        if (!int.TryParse(record.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return;
        }

        var machine = await _machineService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (machine is null)
        {
            StatusMessage = $"Unable to locate asset #{id}.";
            return;
        }

        _loadedMachine = machine;
        LoadEditor(machine);
    }

    protected override Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                _loadedMachine = null;
                SetEditor(AssetEditor.CreateForNew(_machineService.NormalizeStatus("active")));
                break;
            case FormMode.Update:
                _snapshot = Editor.Clone();
                break;
            case FormMode.View:
                _snapshot = null;
                break;
        }

        return Task.CompletedTask;
    }

    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var errors = new List<string>();
        try
        {
            var machine = Editor.ToMachine(_loadedMachine);
            machine.Status = _machineService.NormalizeStatus(machine.Status);
            _machineService.Validate(machine);
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(ex.Message);
        }
        catch (Exception ex)
        {
            errors.Add($"Unexpected validation failure: {ex.Message}");
        }

        return await Task.FromResult<IReadOnlyList<string>>(errors).ConfigureAwait(false);
    }

    protected override async Task<bool> OnSaveAsync()
    {
        var context = MachineCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId);

        var machine = Editor.ToMachine(_loadedMachine);
        machine.Status = _machineService.NormalizeStatus(machine.Status);

        if (Mode == FormMode.Add)
        {
            await _machineService.CreateAsync(machine, context).ConfigureAwait(false);
            _loadedMachine = machine;
            LoadEditor(machine);
            return true;
        }

        if (Mode == FormMode.Update)
        {
            if (_loadedMachine is null)
            {
                return false;
            }

            machine.Id = _loadedMachine.Id;
            await _machineService.UpdateAsync(machine, context).ConfigureAwait(false);
            _loadedMachine = machine;
            LoadEditor(machine);
            return true;
        }

        return false;
    }

    protected override void OnCancel()
    {
        if (Mode == FormMode.Add)
        {
            if (_loadedMachine is not null)
            {
                LoadEditor(_loadedMachine);
            }
            else
            {
                SetEditor(AssetEditor.CreateEmpty());
            }
        }
        else if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }
    }

    partial void OnEditorChanging(AssetEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(AssetEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged += OnEditorPropertyChanged;
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressEditorDirtyNotifications)
        {
            return;
        }

        if (IsInEditMode)
        {
            MarkDirty();
        }
    }

    private void LoadEditor(Machine machine)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = AssetEditor.FromMachine(machine, _machineService.NormalizeStatus);
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private void SetEditor(AssetEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private static ModuleRecord ToRecord(Machine machine)
    {
        var fields = new List<InspectorField>
        {
            new("Location", machine.Location ?? "-"),
            new("Model", machine.Model ?? "-"),
            new("Manufacturer", machine.Manufacturer ?? "-"),
            new("Status", machine.Status ?? "-"),
            new("Installed", machine.InstallDate?.ToString("d", CultureInfo.CurrentCulture) ?? "-")
        };

        return new ModuleRecord(
            machine.Id.ToString(CultureInfo.InvariantCulture),
            machine.Name,
            machine.Code,
            machine.Status,
            machine.Description,
            fields,
            WorkOrdersModuleViewModel.ModuleKey,
            machine.Id);
    }

    public sealed partial class AssetEditor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _code = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _model = string.Empty;

        [ObservableProperty]
        private string _manufacturer = string.Empty;

        [ObservableProperty]
        private string _location = string.Empty;

        [ObservableProperty]
        private string _status = "active";

        [ObservableProperty]
        private string _ursDoc = string.Empty;

        [ObservableProperty]
        private DateTime? _installDate = DateTime.UtcNow.Date;

        [ObservableProperty]
        private DateTime? _procurementDate;

        [ObservableProperty]
        private DateTime? _warrantyUntil;

        [ObservableProperty]
        private bool _isCritical;

        [ObservableProperty]
        private string _serialNumber = string.Empty;

        [ObservableProperty]
        private string _lifecyclePhase = string.Empty;

        [ObservableProperty]
        private string _notes = string.Empty;

        public static AssetEditor CreateEmpty() => new();

        public static AssetEditor CreateForNew(string normalizedStatus)
            => new() { Status = normalizedStatus };

        public static AssetEditor FromMachine(Machine machine, Func<string?, string> normalizer)
        {
            return new AssetEditor
            {
                Id = machine.Id,
                Code = machine.Code ?? string.Empty,
                Name = machine.Name ?? string.Empty,
                Description = machine.Description ?? string.Empty,
                Model = machine.Model ?? string.Empty,
                Manufacturer = machine.Manufacturer ?? string.Empty,
                Location = machine.Location ?? string.Empty,
                Status = normalizer(machine.Status),
                UrsDoc = machine.UrsDoc ?? string.Empty,
                InstallDate = machine.InstallDate,
                ProcurementDate = machine.ProcurementDate,
                WarrantyUntil = machine.WarrantyUntil,
                IsCritical = machine.IsCritical,
                SerialNumber = machine.SerialNumber ?? string.Empty,
                LifecyclePhase = machine.LifecyclePhase ?? string.Empty,
                Notes = machine.Note ?? string.Empty
            };
        }

        public Machine ToMachine(Machine? existing)
        {
            var machine = existing is null ? new Machine() : CloneMachine(existing);
            machine.Id = Id;
            machine.Code = Code;
            machine.Name = Name;
            machine.Description = Description;
            machine.Model = Model;
            machine.Manufacturer = Manufacturer;
            machine.Location = Location;
            machine.Status = Status;
            machine.UrsDoc = UrsDoc;
            machine.InstallDate = InstallDate;
            machine.ProcurementDate = ProcurementDate;
            machine.WarrantyUntil = WarrantyUntil;
            machine.IsCritical = IsCritical;
            machine.SerialNumber = string.IsNullOrWhiteSpace(SerialNumber) ? machine.SerialNumber : SerialNumber;
            machine.LifecyclePhase = LifecyclePhase;
            machine.Note = Notes;
            return machine;
        }

        public AssetEditor Clone()
            => new()
            {
                Id = Id,
                Code = Code,
                Name = Name,
                Description = Description,
                Model = Model,
                Manufacturer = Manufacturer,
                Location = Location,
                Status = Status,
                UrsDoc = UrsDoc,
                InstallDate = InstallDate,
                ProcurementDate = ProcurementDate,
                WarrantyUntil = WarrantyUntil,
                IsCritical = IsCritical,
                SerialNumber = SerialNumber,
                LifecyclePhase = LifecyclePhase,
                Notes = Notes
            };

        private static Machine CloneMachine(Machine source)
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
    }
}
