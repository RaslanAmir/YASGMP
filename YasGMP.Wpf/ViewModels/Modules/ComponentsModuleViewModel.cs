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
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.ViewModels.Modules;
/// <summary>
/// Represents the components module view model value.
/// </summary>

public sealed partial class ComponentsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>
    /// Represents the module key value.
    /// </summary>
    public new const string ModuleKey = "Components";

    private static readonly string[] DefaultStatuses =
    {
        "active",
        "maintenance",
        "out-of-service",
        "decommissioned"
    };

    private static readonly string[] DefaultTypes =
    {
        "sensor",
        "assembly",
        "consumable",
        "control",
        "other"
    };

    private readonly IComponentCrudService _componentService;
    private readonly IMachineCrudService _machineService;
    private readonly IAuthContext _authContext;
    private readonly IElectronicSignatureDialogService _signatureDialog;

    private Component? _loadedComponent;
    private ComponentEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;
    private IReadOnlyList<Machine> _machines = Array.Empty<Machine>();
    /// <summary>
    /// Initializes a new instance of the ComponentsModuleViewModel class.
    /// </summary>

    public ComponentsModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IComponentCrudService componentService,
        IMachineCrudService machineService,
        IAuthContext authContext,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, localization.GetString("Module.Title.Components"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _componentService = componentService ?? throw new ArgumentNullException(nameof(componentService));
        _machineService = machineService ?? throw new ArgumentNullException(nameof(machineService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));

        Editor = ComponentEditor.CreateEmpty();
        StatusOptions = Array.AsReadOnly(DefaultStatuses);
        TypeOptions = Array.AsReadOnly(DefaultTypes);
        MachineOptions = new ObservableCollection<MachineOption>();
    }

    [ObservableProperty]
    private ComponentEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;
    /// <summary>
    /// Gets or sets the machine options.
    /// </summary>

    public ObservableCollection<MachineOption> MachineOptions { get; }
    /// <summary>
    /// Gets or sets the status options.
    /// </summary>

    public IReadOnlyList<string> StatusOptions { get; }
    /// <summary>
    /// Gets or sets the type options.
    /// </summary>

    public IReadOnlyList<string> TypeOptions { get; }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        _machines = await _machineService.GetAllAsync().ConfigureAwait(false);
        RefreshMachineOptions(_machines);

        var components = await _componentService.GetAllAsync().ConfigureAwait(false);
        return components.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var machines = new List<Machine>
        {
            new()
            {
                Id = 101,
                Name = "Lyophilizer",
                Code = "AST-500"
            },
            new()
            {
                Id = 102,
                Name = "Autoclave",
                Code = "AUTO-01"
            }
        };

        _machines = machines;
        RefreshMachineOptions(machines);

        var sample = new List<Component>
        {
            new()
            {
                Id = 2001,
                MachineId = 101,
                MachineName = "Lyophilizer",
                Code = "CMP-LYO-01",
                Name = "Shelf Temperature Probe",
                Type = "sensor",
                SopDoc = "SOP-LYO-01",
                Status = "active",
                InstallDate = DateTime.UtcNow.AddMonths(-8),
                Supplier = "Contoso Sensors",
                SerialNumber = "LYO-PRB-2024-09",
                Comments = "Calibrated quarterly.",
                LifecycleState = "in-use"
            },
            new()
            {
                Id = 2002,
                MachineId = 102,
                MachineName = "Autoclave",
                Code = "CMP-AUTO-VAL",
                Name = "Steam Valve Assembly",
                Type = "assembly",
                SopDoc = "SOP-AUTO-VAL",
                Status = "maintenance",
                InstallDate = DateTime.UtcNow.AddYears(-2),
                Supplier = "Steris",
                SerialNumber = "AUTO-VAL-11",
                Comments = "Awaiting spare kit.",
                LifecycleState = "maintenance"
            }
        };

        return sample.Select(ToRecord).ToList();
    }

    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var components = await _componentService.GetAllAsync().ConfigureAwait(false);
        var items = components
            .Select(component =>
            {
                var key = component.Id.ToString(CultureInfo.InvariantCulture);
                var label = string.IsNullOrWhiteSpace(component.Name) ? key : component.Name;
                var descriptionParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(component.Code))
                {
                    descriptionParts.Add(component.Code);
                }

                if (!string.IsNullOrWhiteSpace(component.MachineName))
                {
                    descriptionParts.Add(component.MachineName);
                }

                if (!string.IsNullOrWhiteSpace(component.Status))
                {
                    descriptionParts.Add(component.Status);
                }

                var description = descriptionParts.Count > 0
                    ? string.Join(" • ", descriptionParts)
                    : null;

                return new CflItem(key, label, description);
            })
            .ToList();

        return new CflRequest("Select Component", items);
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
            _loadedComponent = null;
            SetEditor(ComponentEditor.CreateEmpty());
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

        var component = await _componentService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (component is null)
        {
            StatusMessage = $"Unable to locate component #{id}.";
            return;
        }

        _loadedComponent = component;
        LoadEditor(component);
    }

    protected override Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                _loadedComponent = null;
                var defaultMachineId = GetDefaultMachineId();
                var defaultMachineName = ResolveMachineName(defaultMachineId);
                SetEditor(ComponentEditor.CreateForNew(
                    _componentService.NormalizeStatus("active"),
                    defaultMachineId,
                    defaultMachineName));
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
            var component = Editor.ToComponent(_loadedComponent);
            component.Status = _componentService.NormalizeStatus(component.Status);
            component.MachineName = ResolveMachineName(component.MachineId);
            _componentService.Validate(component);
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
        var component = Editor.ToComponent(_loadedComponent);
        component.Status = _componentService.NormalizeStatus(component.Status);
        component.MachineName = ResolveMachineName(component.MachineId);

        if (Mode == FormMode.Update && _loadedComponent is null)
        {
            StatusMessage = "Select a component before saving.";
            return false;
        }

        var recordId = Mode == FormMode.Update ? _loadedComponent!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("components", recordId))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Electronic signature failed: {ex.Message}";
            return false;
        }

        if (signatureResult is null)
        {
            StatusMessage = "Electronic signature cancelled. Save aborted.";
            return false;
        }

        if (signatureResult.Signature is null)
        {
            StatusMessage = "Electronic signature was not captured.";
            return false;
        }

        component.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;
        component.LastModified = DateTime.UtcNow;
        component.LastModifiedById = _authContext.CurrentUser?.Id ?? component.LastModifiedById;
        component.SourceIp = _authContext.CurrentIpAddress ?? component.SourceIp ?? string.Empty;

        var context = ComponentCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        Component adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _componentService.CreateAsync(component, context).ConfigureAwait(false);
                if (component.Id == 0 && saveResult.Id > 0)
                {
                    component.Id = saveResult.Id;
                }

                adapterResult = component;
            }
            else if (Mode == FormMode.Update)
            {
                component.Id = _loadedComponent!.Id;
                saveResult = await _componentService.UpdateAsync(component, context).ConfigureAwait(false);
                adapterResult = component;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist component: {ex.Message}", ex);
        }

        _loadedComponent = component;
        LoadEditor(component);

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "components",
            recordId: adapterResult.Id,
            metadata: saveResult.SignatureMetadata,
            fallbackSignatureHash: adapterResult.DigitalSignature,
            fallbackMethod: context.SignatureMethod,
            fallbackStatus: context.SignatureStatus,
            fallbackNote: context.SignatureNote,
            signedAt: signatureResult.Signature.SignedAt,
            fallbackDeviceInfo: context.DeviceInfo,
            fallbackIpAddress: context.Ip,
            fallbackSessionId: context.SessionId);

        try
        {
            await SignaturePersistenceHelper
                .PersistIfRequiredAsync(_signatureDialog, signatureResult)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to persist electronic signature: {ex.Message}";
            Mode = FormMode.Update;
            return false;
        }

        StatusMessage = $"Electronic signature captured ({signatureResult.ReasonDisplay}).";
        return true;
    }

    protected override void OnCancel()
    {
        if (Mode == FormMode.Add)
        {
            if (_loadedComponent is not null)
            {
                LoadEditor(_loadedComponent);
            }
            else
            {
                SetEditor(ComponentEditor.CreateEmpty());
            }
        }
        else if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }
    }

    partial void OnEditorChanging(ComponentEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(ComponentEditor value)
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

        if (string.Equals(e.PropertyName, nameof(ComponentEditor.MachineId), StringComparison.Ordinal))
        {
            _suppressEditorDirtyNotifications = true;
            Editor.MachineName = ResolveMachineName(Editor.MachineId);
            _suppressEditorDirtyNotifications = false;
        }

        if (IsInEditMode)
        {
            MarkDirty();
        }
    }

    private void LoadEditor(Component component)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = ComponentEditor.FromComponent(component, _componentService.NormalizeStatus, ResolveMachineName);
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private void SetEditor(ComponentEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private void RefreshMachineOptions(IEnumerable<Machine> machines)
    {
        MachineOptions.Clear();
        foreach (var option in machines
                     .OrderBy(static m => string.IsNullOrWhiteSpace(m.Name) ? m.Code : m.Name)
                     .Select(m => new MachineOption(m.Id, ResolveMachineDisplayName(m))))
        {
            MachineOptions.Add(option);
        }
    }

    private int GetDefaultMachineId()
    {
        if (_loadedComponent is not null)
        {
            return _loadedComponent.MachineId;
        }

        if (SelectedRecord?.RelatedParameter is int relatedId)
        {
            return relatedId;
        }

        if (SelectedRecord?.RelatedParameter is string relatedKey
            && int.TryParse(relatedKey, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return _machines.FirstOrDefault()?.Id ?? 0;
    }

    private string ResolveMachineName(int machineId)
    {
        if (machineId <= 0)
        {
            return string.Empty;
        }

        var machine = _machines.FirstOrDefault(m => m.Id == machineId);
        return machine is null ? string.Empty : ResolveMachineDisplayName(machine);
    }

    private static string ResolveMachineDisplayName(Machine machine)
    {
        if (!string.IsNullOrWhiteSpace(machine.Name))
        {
            return machine.Name;
        }

        if (!string.IsNullOrWhiteSpace(machine.Code))
        {
            return machine.Code;
        }

        return $"Machine #{machine.Id}";
    }

    private ModuleRecord ToRecord(Component component)
    {
        var inspector = new List<InspectorField>
        {
            new("Machine", string.IsNullOrWhiteSpace(component.MachineName)
                ? ResolveMachineName(component.MachineId)
                : component.MachineName),
            new("Type", string.IsNullOrWhiteSpace(component.Type) ? "-" : component.Type!),
            new("Status", string.IsNullOrWhiteSpace(component.Status) ? "-" : component.Status!),
            new("SOP", string.IsNullOrWhiteSpace(component.SopDoc) ? "-" : component.SopDoc!),
            new("Lifecycle", string.IsNullOrWhiteSpace(component.LifecycleState) ? "-" : component.LifecycleState!)
        };

        return new ModuleRecord(
            component.Id.ToString(CultureInfo.InvariantCulture),
            component.Name,
            component.Code,
            component.Status,
            component.Comments,
            inspector,
            AssetsModuleViewModel.ModuleKey,
            component.MachineId);
    }
    /// <summary>
    /// Represents the component editor value.
    /// </summary>

    public sealed partial class ComponentEditor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private int _machineId;

        [ObservableProperty]
        private string _machineName = string.Empty;

        [ObservableProperty]
        private string _code = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _type = string.Empty;

        [ObservableProperty]
        private string _sopDoc = string.Empty;

        [ObservableProperty]
        private string _status = "active";

        [ObservableProperty]
        private DateTime? _installDate;

        [ObservableProperty]
        private string _serialNumber = string.Empty;

        [ObservableProperty]
        private string _supplier = string.Empty;

        [ObservableProperty]
        private DateTime? _warrantyUntil;

        [ObservableProperty]
        private string _comments = string.Empty;

        [ObservableProperty]
        private string _lifecycleState = string.Empty;
        /// <summary>
        /// Executes the create empty operation.
        /// </summary>

        public static ComponentEditor CreateEmpty() => new();
        /// <summary>
        /// Executes the create for new operation.
        /// </summary>

        public static ComponentEditor CreateForNew(string normalizedStatus, int machineId, string machineName)
            => new()
            {
                Status = normalizedStatus,
                MachineId = machineId,
                MachineName = machineName
            };
        /// <summary>
        /// Executes the from component operation.
        /// </summary>

        public static ComponentEditor FromComponent(
            Component component,
            Func<string?, string> statusNormalizer,
            Func<int, string> machineNameResolver)
        {
            return new ComponentEditor
            {
                Id = component.Id,
                MachineId = component.MachineId,
                MachineName = string.IsNullOrWhiteSpace(component.MachineName)
                    ? machineNameResolver(component.MachineId)
                    : component.MachineName,
                Code = component.Code ?? string.Empty,
                Name = component.Name ?? string.Empty,
                Type = component.Type ?? string.Empty,
                SopDoc = component.SopDoc ?? string.Empty,
                Status = statusNormalizer(component.Status),
                InstallDate = component.InstallDate,
                SerialNumber = component.SerialNumber ?? string.Empty,
                Supplier = component.Supplier ?? string.Empty,
                WarrantyUntil = component.WarrantyUntil,
                Comments = component.Comments ?? string.Empty,
                LifecycleState = component.LifecycleState ?? string.Empty
            };
        }
        /// <summary>
        /// Executes the to component operation.
        /// </summary>

        public Component ToComponent(Component? existing)
        {
            var component = existing is null ? new Component() : CloneComponent(existing);
            component.Id = Id;
            component.MachineId = MachineId;
            component.MachineName = MachineName;
            component.Code = Code;
            component.Name = Name;
            component.Type = Type;
            component.SopDoc = SopDoc;
            component.Status = Status;
            component.InstallDate = InstallDate;
            component.SerialNumber = SerialNumber;
            component.Supplier = Supplier;
            component.WarrantyUntil = WarrantyUntil;
            component.Comments = Comments;
            component.LifecycleState = LifecycleState;
            return component;
        }
        /// <summary>
        /// Executes the clone operation.
        /// </summary>

        public ComponentEditor Clone()
            => new()
            {
                Id = Id,
                MachineId = MachineId,
                MachineName = MachineName,
                Code = Code,
                Name = Name,
                Type = Type,
                SopDoc = SopDoc,
                Status = Status,
                InstallDate = InstallDate,
                SerialNumber = SerialNumber,
                Supplier = Supplier,
                WarrantyUntil = WarrantyUntil,
                Comments = Comments,
                LifecycleState = LifecycleState
            };

        private static Component CloneComponent(Component source)
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
    }
    /// <summary>
    /// Represents the Machine Option record.
    /// </summary>

    public sealed record MachineOption(int Id, string Name);
}
