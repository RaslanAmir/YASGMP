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
using ComponentEntity = YasGMP.Models.Component;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Maintains component records linked to assets inside the WPF SAP B1 shell.</summary>
/// <remarks>
/// Form Modes: Find filters across linked machines, Add seeds a new <see cref="ComponentEditor"/>, View freezes the editor, and Update enables editing with machine selection lookups.
/// Audit &amp; Logging: Persists through <see cref="IComponentCrudService"/> with enforced electronic signatures; audit diffing lives in the shared service layer.
/// Localization: Uses inline literals such as `"Components"`, `"Unable to locate component #{id}."`, and status prompts; RESX keys are still TODO.
/// Navigation: ModuleKey `Components` ties the module into docking and status updates, while CFL helpers route Golden Arrow navigation between assets and component records using machine-related module keys.
/// </remarks>
public sealed partial class ComponentsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Shell registration key that binds Components into the docking layout.</summary>
    /// <remarks>Execution: Resolved when the shell composes modules and persists layouts. Form Mode: Identifier applies across Find/Add/View/Update. Localization: Currently paired with the inline caption "Components" until `Modules_Components_Title` is introduced.</remarks>
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

    private ComponentEntity? _loadedComponent;
    private ComponentEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;
    private IReadOnlyList<Machine> _machines = Array.Empty<Machine>();

    /// <summary>Initializes the Components module view model with domain and shell services.</summary>
    /// <remarks>Execution: Invoked when the shell activates the module or Golden Arrow navigation materializes it. Form Mode: Seeds Find/View immediately while deferring Add/Update wiring to later transitions. Localization: Relies on inline strings for tab titles and prompts until module resources exist.</remarks>
    public ComponentsModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IComponentCrudService componentService,
        IMachineCrudService machineService,
        IAuthContext authContext,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Components", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        _componentService = componentService ?? throw new ArgumentNullException(nameof(componentService));
        _machineService = machineService ?? throw new ArgumentNullException(nameof(machineService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));

        Editor = ComponentEditor.CreateEmpty();
        StatusOptions = Array.AsReadOnly(DefaultStatuses);
        TypeOptions = Array.AsReadOnly(DefaultTypes);
        MachineOptions = new ObservableCollection<MachineOption>();
        SummarizeWithAiCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(OpenAiSummary);
        Toolbar.Add(new ModuleToolbarCommand("Summarize (AI)", SummarizeWithAiCommand));
    }

    /// <summary>Generated property exposing the editor for the Components module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Components_Editor` resources are available.</remarks>
    [ObservableProperty]
    private ComponentEditor _editor;

    /// <summary>Opens the AI module to summarize the selected component.</summary>
    public CommunityToolkit.Mvvm.Input.IRelayCommand SummarizeWithAiCommand { get; }

    /// <summary>Generated property exposing the is editor enabled for the Components module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Components_IsEditorEnabled` resources are available.</remarks>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Collection presenting the machine options for the Components document host.</summary>
    /// <remarks>Execution: Populated as records load or staging mutates. Form Mode: Visible in all modes with editing reserved for Add/Update. Localization: Grid headers/tooltips remain inline until `Modules_Components_Grid` resources exist.</remarks>
    public ObservableCollection<MachineOption> MachineOptions { get; }

    /// <summary>Collection presenting the status options for the Components document host.</summary>
    /// <remarks>Execution: Populated as records load or staging mutates. Form Mode: Visible in all modes with editing reserved for Add/Update. Localization: Grid headers/tooltips remain inline until `Modules_Components_Grid` resources exist.</remarks>
    public IReadOnlyList<string> StatusOptions { get; }

    /// <summary>Collection presenting the type options for the Components document host.</summary>
    /// <remarks>Execution: Populated as records load or staging mutates. Form Mode: Visible in all modes with editing reserved for Add/Update. Localization: Grid headers/tooltips remain inline until `Modules_Components_Grid` resources exist.</remarks>
    public IReadOnlyList<string> TypeOptions { get; }

    /// <summary>Loads Components records from domain services.</summary>
    /// <remarks>Execution: Triggered by Find refreshes and shell activation. Form Mode: Supplies data for Find/View while Add/Update reuse cached results. Localization: Emits inline status strings pending `Status_Components_Loaded` resources.</remarks>
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        _machines = await _machineService.GetAllAsync().ConfigureAwait(false);
        RefreshMachineOptions(_machines);

        var components = await _componentService.GetAllAsync().ConfigureAwait(false);
        return components.Select(ToRecord).ToList();
    }

    /// <summary>Provides design-time sample data for the Components designer experience.</summary>
    /// <remarks>Execution: Invoked only by design-mode checks to support Blend/preview tooling. Form Mode: Mirrors Find mode to preview list layouts. Localization: Sample literals remain inline for clarity.</remarks>
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

        var sample = new List<ComponentEntity>
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

    /// <summary>Builds the Choose-From-List request used for Golden Arrow navigation.</summary>
    /// <remarks>Execution: Called when the shell launches CFL dialogs, routing via `ModuleKey` "Components". Form Mode: Provides lookup data irrespective of current mode. Localization: Dialog titles and descriptions use inline strings until `CFL_Components` resources exist.</remarks>
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
                    ? string.Join(" â€˘ ", descriptionParts)
                    : null;

                return new CflItem(key, label, description);
            })
            .ToList();

        return new CflRequest("Select ComponentEntity", items);
    }

    private void OpenAiSummary()
    {
        if (SelectedRecord is null && _loadedComponent is null)
        {
            StatusMessage = "Select a component to summarize.";
            return;
        }

        var c = _loadedComponent;
        string prompt = c is null
            ? $"Summarize component: {SelectedRecord?.Title}. Provide status, machine link and risks in <= 8 bullets."
            : $"Summarize this component (<= 8 bullets). Name={c.Name}; Code={c.Code}; Status={c.Status}; Type={c.Type}; Machine={c.MachineName}; Supplier={c.Supplier}; Serial={c.SerialNumber}.";

        var shell = YasGMP.Common.ServiceLocator.GetRequiredService<IShellInteractionService>();
        var doc = shell.OpenModule(AiModuleViewModel.ModuleKey, $"prompt:{prompt}");
        shell.Activate(doc);
    }

    /// <summary>Applies CFL selections back into the Components workspace.</summary>
    /// <remarks>Execution: Runs after CFL or Golden Arrow completion, updating `StatusMessage` for `ModuleKey` "Components". Form Mode: Navigates records without disturbing active edits. Localization: Status feedback uses inline phrases pending `Status_Components_Filtered`.</remarks>
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

    /// <summary>Loads editor payloads for the selected Components record.</summary>
    /// <remarks>Execution: Triggered when document tabs change or shell routing targets `ModuleKey` "Components". Form Mode: Honors Add/Update safeguards to avoid overwriting dirty state. Localization: Inline status/error strings remain until `Status_Components` resources are available.</remarks>
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

    /// <summary>Adjusts command enablement and editor state when the form mode changes.</summary>
    /// <remarks>Execution: Fired by the SAP B1 style form state machine when Find/Add/View/Update transitions occur. Form Mode: Governs which controls are writable and which commands are visible. Localization: Mode change prompts use inline strings pending localization resources.</remarks>
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

    /// <summary>Validates the current editor payload before persistence.</summary>
    /// <remarks>Execution: Invoked immediately prior to OK/Update actions. Form Mode: Only Add/Update trigger validation. Localization: Error messages flow from inline literals until validation resources are added.</remarks>
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

    /// <summary>Persists the current record and coordinates signatures, attachments, and audits.</summary>
    /// <remarks>Execution: Runs after validation when OK/Update is confirmed. Form Mode: Exclusive to Add/Update operations. Localization: Success/failure messaging remains inline pending dedicated resources.</remarks>
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
            _authContext.CurrentIpAddress ?? string.Empty,
            _authContext.CurrentDeviceInfo ?? string.Empty,
            _authContext.CurrentSessionId,
            signatureResult);

        ComponentEntity adapterResult;
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

    /// <summary>Reverts in-flight edits and restores the last committed snapshot.</summary>
    /// <remarks>Execution: Activated when Cancel is chosen mid-edit. Form Mode: Applies to Add/Update; inert elsewhere. Localization: Cancellation prompts use inline text until localized resources exist.</remarks>
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

    private void LoadEditor(ComponentEntity component)
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

    private ModuleRecord ToRecord(ComponentEntity component)
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

        public static ComponentEditor CreateEmpty() => new();

        public static ComponentEditor CreateForNew(string normalizedStatus, int machineId, string machineName)
            => new()
            {
                Status = normalizedStatus,
                MachineId = machineId,
                MachineName = machineName
            };

        public static ComponentEditor FromComponent(
            ComponentEntity component,
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

        public ComponentEntity ToComponent(ComponentEntity? existing)
        {
            var component = existing is null ? new ComponentEntity() : CloneComponent(existing);
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

        private static ComponentEntity CloneComponent(ComponentEntity source)
        {
            return new ComponentEntity
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

    /// <summary>Executes the machine option routine for the Components module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public sealed record MachineOption(int Id, string Name);
}



