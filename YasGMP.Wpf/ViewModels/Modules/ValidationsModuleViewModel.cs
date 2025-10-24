using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Models.Enums;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Manages equipment/software validation packages in the WPF SAP B1 shell.</summary>
/// <remarks>
/// Form Modes: Find filters validations (with machine/component CFL pickers), Add seeds <see cref="ValidationEditor.CreateEmpty"/>, View keeps history read-only, and Update enables editing with attachment capture and status tracking.
/// Audit &amp; Logging: Persists via <see cref="IValidationCrudService"/> under enforced e-signatures, delegating audit hashing and attachment retention to shared services.
/// Localization: Inline literals—for example `"Validations"`, `"Pending"`, and status messages for due dates—remain until localisation keys are supplied.
/// Navigation: ModuleKey `Validations` anchors docking; overrides build CFL payloads and `ModuleRecord` entries embed machine/component identifiers for Golden Arrow navigation while status messages feed the shell.
/// </remarks>
public sealed partial class ValidationsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Shell registration key that binds Validations into the docking layout.</summary>
    /// <remarks>Execution: Resolved when the shell composes modules and persists layouts. Form Mode: Identifier applies across Find/Add/View/Update. Localization: Currently paired with the inline caption "Validations" until `Modules_Validations_Title` is introduced.</remarks>
    public new const string ModuleKey = "Validations";

    private readonly IValidationCrudService _validationService;
    private readonly IAuthContext _authContext;
    private readonly IFilePicker _filePicker;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;

    private Validation? _loadedValidation;
    private ValidationEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;
    private IReadOnlyList<Machine> _machines = Array.Empty<Machine>();
    private IReadOnlyList<MachineComponent> _components = Array.Empty<MachineComponent>();

    /// <summary>Initializes the Validations module view model with domain and shell services.</summary>
    /// <remarks>Execution: Invoked when the shell activates the module or Golden Arrow navigation materializes it. Form Mode: Seeds Find/View immediately while deferring Add/Update wiring to later transitions. Localization: Relies on inline strings for tab titles and prompts until module resources exist.</remarks>
    public ValidationsModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IValidationCrudService validationService,
        IAuthContext authContext,
        IFilePicker filePicker,
        IAttachmentWorkflowService attachmentWorkflow,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Validations", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));

        Editor = ValidationEditor.CreateEmpty();
        MachineOptions = new ObservableCollection<MachineOption>();
        ComponentOptions = new ObservableCollection<ComponentOption>();
        StatusOptions = new ObservableCollection<string>(Enum.GetNames(typeof(ValidationStatus)));
        TypeOptions = new ObservableCollection<string>(Enum.GetNames(typeof(ValidationType)));

        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
        SummarizeWithAiCommand = new RelayCommand(OpenAiSummary);
        Toolbar.Add(new ModuleToolbarCommand("Summarize (AI)", SummarizeWithAiCommand));
    }

    /// <summary>Generated property exposing the editor for the Validations module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Validations_Editor` resources are available.</remarks>
    [ObservableProperty]
    private ValidationEditor _editor;

    /// <summary>Opens the AI module to summarize the selected validation.</summary>
    public IRelayCommand SummarizeWithAiCommand { get; }

    /// <summary>Generated property exposing the is editor enabled for the Validations module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Validations_IsEditorEnabled` resources are available.</remarks>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Collection presenting the machine options for the Validations document host.</summary>
    /// <remarks>Execution: Populated as records load or staging mutates. Form Mode: Visible in all modes with editing reserved for Add/Update. Localization: Grid headers/tooltips remain inline until `Modules_Validations_Grid` resources exist.</remarks>
    public ObservableCollection<MachineOption> MachineOptions { get; }

    /// <summary>Collection presenting the component options for the Validations document host.</summary>
    /// <remarks>Execution: Populated as records load or staging mutates. Form Mode: Visible in all modes with editing reserved for Add/Update. Localization: Grid headers/tooltips remain inline until `Modules_Validations_Grid` resources exist.</remarks>
    public ObservableCollection<ComponentOption> ComponentOptions { get; }

    /// <summary>Collection presenting the status options for the Validations document host.</summary>
    /// <remarks>Execution: Populated as records load or staging mutates. Form Mode: Visible in all modes with editing reserved for Add/Update. Localization: Grid headers/tooltips remain inline until `Modules_Validations_Grid` resources exist.</remarks>
    public ObservableCollection<string> StatusOptions { get; }

    /// <summary>Collection presenting the type options for the Validations document host.</summary>
    /// <remarks>Execution: Populated as records load or staging mutates. Form Mode: Visible in all modes with editing reserved for Add/Update. Localization: Grid headers/tooltips remain inline until `Modules_Validations_Grid` resources exist.</remarks>
    public ObservableCollection<string> TypeOptions { get; }

    /// <summary>Command executing the attach document workflow for the Validations module.</summary>
    /// <remarks>Execution: Invoked when the correlated ribbon or toolbar control is activated. Form Mode: Enabled only when the current mode supports the action (generally Add/Update). Localization: Uses inline button labels/tooltips until `Ribbon_Validations_AttachDocument` resources are authored.</remarks>
    public IAsyncRelayCommand AttachDocumentCommand { get; }

    /// <summary>Loads Validations records from domain services.</summary>
    /// <remarks>Execution: Triggered by Find refreshes and shell activation. Form Mode: Supplies data for Find/View while Add/Update reuse cached results. Localization: Emits inline status strings pending `Status_Validations_Loaded` resources.</remarks>
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        _machines = await Database.GetAllMachinesAsync().ConfigureAwait(false);
        _components = await Database.GetAllComponentsAsync().ConfigureAwait(false);

        RefreshMachineOptions(_machines);
        RefreshComponentOptions(_components);

        var validations = await _validationService.GetAllAsync().ConfigureAwait(false);
        return validations.Select(ToRecord).ToList();
    }

    /// <summary>Provides design-time sample data for the Validations designer experience.</summary>
    /// <remarks>Execution: Invoked only by design-mode checks to support Blend/preview tooling. Form Mode: Mirrors Find mode to preview list layouts. Localization: Sample literals remain inline for clarity.</remarks>
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        _machines = new List<Machine>
        {
            new() { Id = 100, Name = "Autoclave" },
            new() { Id = 101, Name = "Filling Line" }
        };

        _components = new List<MachineComponent>
        {
            new() { Id = 200, Name = "Temperature Sensor" },
            new() { Id = 201, Name = "Pressure Gauge" }
        };

        RefreshMachineOptions(_machines);
        RefreshComponentOptions(_components);

        var sample = new List<Validation>
        {
            new()
            {
                Id = 1,
                Code = "VAL-0001",
                Type = ValidationType.IQ.ToString(),
                MachineId = 100,
                DateStart = DateTime.UtcNow.AddDays(-10),
                DateEnd = DateTime.UtcNow.AddDays(-7),
                Status = ValidationStatus.Successful.ToString(),
                NextDue = DateTime.UtcNow.AddMonths(12),
                Comment = "Initial IQ for autoclave"
            },
            new()
            {
                Id = 2,
                Code = "VAL-0002",
                Type = ValidationType.OQ.ToString(),
                ComponentId = 200,
                DateStart = DateTime.UtcNow.AddDays(-5),
                Status = ValidationStatus.InProgress.ToString(),
                NextDue = DateTime.UtcNow.AddMonths(6),
                Comment = "OQ for sensor calibration"
            }
        };

        return sample.Select(ToRecord).ToList();
    }

    /// <summary>Builds the Choose-From-List request used for Golden Arrow navigation.</summary>
    /// <remarks>Execution: Called when the shell launches CFL dialogs, routing via `ModuleKey` "Validations". Form Mode: Provides lookup data irrespective of current mode. Localization: Dialog titles and descriptions use inline strings until `CFL_Validations` resources exist.</remarks>
    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var validations = await _validationService.GetAllAsync().ConfigureAwait(false);
        var items = validations
            .Select(validation =>
            {
                var key = validation.Id.ToString(CultureInfo.InvariantCulture);
                var description = new List<string>
                {
                    validation.Type,
                    FindMachineName(validation.MachineId) ?? FindComponentName(validation.ComponentId) ?? "Unassigned",
                    validation.Status
                };

                if (validation.NextDue.HasValue)
                {
                    description.Add($"Next due {validation.NextDue.Value:d}");
                }

                return new CflItem(
                    key,
                    $"{validation.Code} ({validation.Type})",
                    string.Join(" â€˘ ", description.Where(static part => !string.IsNullOrWhiteSpace(part))));
            })
            .ToList();

        return new CflRequest("Select Validation", items);
    }

    /// <summary>Applies CFL selections back into the Validations workspace.</summary>
    /// <remarks>Execution: Runs after CFL or Golden Arrow completion, updating `StatusMessage` for `ModuleKey` "Validations". Form Mode: Navigates records without disturbing active edits. Localization: Status feedback uses inline phrases pending `Status_Validations_Filtered`.</remarks>
    protected override Task OnCflSelectionAsync(CflResult result)
    {
        var match = Records.FirstOrDefault(r => r.Key == result.Selected.Key);
        if (match is not null)
        {
            SelectedRecord = match;
            SearchText = match.Title;
        }
        else
        {
            SearchText = result.Selected.Label;
        }

        StatusMessage = $"Filtered {Title} by \"{SearchText}\".";
        return Task.CompletedTask;
    }

    private void OpenAiSummary()
    {
        if (SelectedRecord is null && _loadedValidation is null)
        {
            StatusMessage = "Select a validation to summarize.";
            return;
        }

        var v = _loadedValidation;
        string prompt;
        if (v is null)
        {
            prompt = $"Summarize validation: {SelectedRecord?.Title}. Provide scope, status, due dates and next steps in <= 8 bullets.";
        }
        else
        {
            prompt = $"Summarize this validation (<= 8 bullets). Code={v.Code}; Type={v.Type}; Status={v.Status}; MachineId={v.MachineId}; ComponentId={v.ComponentId}; Start={v.DateStart:yyyy-MM-dd}; End={v.DateEnd:yyyy-MM-dd}; NextDue={v.NextDue:yyyy-MM-dd}; Comment={v.Comment}.";
        }

        var shell = YasGMP.Common.ServiceLocator.GetRequiredService<IShellInteractionService>();
        var doc = shell.OpenModule(AiModuleViewModel.ModuleKey, $"prompt:{prompt}");
        shell.Activate(doc);
    }

    /// <summary>Loads editor payloads for the selected Validations record.</summary>
    /// <remarks>Execution: Triggered when document tabs change or shell routing targets `ModuleKey` "Validations". Form Mode: Honors Add/Update safeguards to avoid overwriting dirty state. Localization: Inline status/error strings remain until `Status_Validations` resources are available.</remarks>
    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedValidation = null;
            SetEditor(ValidationEditor.CreateEmpty());
            UpdateAttachmentCommandState();
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

        var validation = await _validationService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (validation is null)
        {
            StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Status_Validations_UnableToLocateById", "Unable to load validation #{0}."), id);
            return;
        }

        _loadedValidation = validation;
        LoadEditor(validation);
        UpdateAttachmentCommandState();
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
                _loadedValidation = null;
                SetEditor(ValidationEditor.CreateForNew());
                ApplyDefaultLookups();
                break;
            case FormMode.Update:
                _snapshot = Editor.Clone();
                break;
            case FormMode.View:
                _snapshot = null;
                if (_loadedValidation is not null)
                {
                    LoadEditor(_loadedValidation);
                }
                break;
        }

        UpdateAttachmentCommandState();
        return Task.CompletedTask;
    }

    /// <summary>Validates the current editor payload before persistence.</summary>
    /// <remarks>Execution: Invoked immediately prior to OK/Update actions. Form Mode: Only Add/Update trigger validation. Localization: Error messages flow from inline literals until validation resources are added.</remarks>
    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Editor.Code))
        {
            errors.Add("Protocol number is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.Type))
        {
            errors.Add("Validation type is required.");
        }

        if (Editor.MachineId is null && Editor.ComponentId is null)
        {
            errors.Add("Select a machine or component.");
        }

        if (Editor.DateStart is null)
        {
            errors.Add("Start date is required.");
        }

        if (Editor.DateEnd is not null && Editor.DateStart is not null && Editor.DateEnd < Editor.DateStart)
        {
            errors.Add("End date must not precede the start date.");
        }

        try
        {
            var entity = Editor.ToValidation(_loadedValidation);
            _validationService.Validate(entity);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
        }

        return await Task.FromResult(errors);
    }

    /// <summary>Persists the current record and coordinates signatures, attachments, and audits.</summary>
    /// <remarks>Execution: Runs after validation when OK/Update is confirmed. Form Mode: Exclusive to Add/Update operations. Localization: Success/failure messaging remains inline pending dedicated resources.</remarks>
    protected override async Task<bool> OnSaveAsync()
    {
        var entity = Editor.ToValidation(_loadedValidation);

        if (Mode == FormMode.Update && _loadedValidation is null)
        {
            StatusMessage = YasGMP.Wpf.Helpers.Loc.S("Status_Validations_SelectBeforeSave", "Select a validation before saving.");
            return false;
        }

        var recordId = Mode == FormMode.Update ? _loadedValidation!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("validations", recordId))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Error_Signature_Failed", "Electronic signature failed: {0}"), ex.Message);
            return false;
        }

        if (signatureResult is null)
        {
            StatusMessage = YasGMP.Wpf.Helpers.Loc.S("Status_Signature_Cancelled", "Electronic signature cancelled. Save aborted.");
            return false;
        }

        if (signatureResult.Signature is null)
        {
            StatusMessage = YasGMP.Wpf.Helpers.Loc.S("Error_Signature_NotCaptured", "Electronic signature was not captured.");
            return false;
        }

        entity.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;
        entity.LastModified = DateTime.UtcNow;
        entity.LastModifiedById = _authContext.CurrentUser?.Id;
        entity.SourceIp = _authContext.CurrentIpAddress ?? entity.SourceIp ?? string.Empty;
        entity.SessionId = _authContext.CurrentSessionId ?? entity.SessionId ?? string.Empty;
        entity.SignatureTimestamp = DateTime.UtcNow;

        var context = ValidationCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress ?? string.Empty,
            _authContext.CurrentDeviceInfo ?? string.Empty,
            _authContext.CurrentSessionId,
            signatureResult);

        Validation adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                if (string.IsNullOrWhiteSpace(entity.Code))
                {
                    entity.Code = $"VAL-{DateTime.UtcNow:yyyyMMddHHmmss}";
                }

                saveResult = await _validationService.CreateAsync(entity, context).ConfigureAwait(false);
                entity.Id = saveResult.Id;
                Records.Add(ToRecord(entity));
                adapterResult = entity;
            }
            else if (Mode == FormMode.Update)
            {
                entity.Id = _loadedValidation!.Id;
                saveResult = await _validationService.UpdateAsync(entity, context).ConfigureAwait(false);
                ReplaceRecord(entity);
                adapterResult = entity;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(string.Format(YasGMP.Wpf.Helpers.Loc.S("Error_Validations_SaveFailed", "Failed to persist validation: {0}"), ex.Message), ex);
        }

        _loadedValidation = entity;
        LoadEditor(entity);
        UpdateAttachmentCommandState();

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "validations",
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
            StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Error_Signature_PersistFailed", "Failed to persist electronic signature: {0}"), ex.Message);
            Mode = FormMode.Update;
            return false;
        }

        StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Status_Signature_Captured", "Electronic signature captured ({0})."), signatureResult.ReasonDisplay);
        return true;
    }

    /// <summary>Reverts in-flight edits and restores the last committed snapshot.</summary>
    /// <remarks>Execution: Activated when Cancel is chosen mid-edit. Form Mode: Applies to Add/Update; inert elsewhere. Localization: Cancellation prompts use inline text until localized resources exist.</remarks>
    protected override void OnCancel()
    {
        if (Mode == FormMode.Add)
        {
            _loadedValidation = null;
            SetEditor(ValidationEditor.CreateEmpty());
            ApplyDefaultLookups();
        }
        else if (Mode == FormMode.Update)
        {
            if (_snapshot is not null)
            {
                SetEditor(_snapshot.Clone());
            }
            else if (_loadedValidation is not null)
            {
                LoadEditor(_loadedValidation);
            }
        }

        UpdateAttachmentCommandState();
    }

    private void LoadEditor(Validation validation)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = ValidationEditor.FromValidation(validation, FindMachineName, FindComponentName);
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private void SetEditor(ValidationEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    partial void OnEditorChanging(ValidationEditor value)
    {
        if (_editor is not null)
        {
            _editor.PropertyChanged -= OnEditorPropertyChanged;
        }
    }

    partial void OnEditorChanged(ValidationEditor value)
    {
        value.PropertyChanged += OnEditorPropertyChanged;
        UpdateAttachmentCommandState();
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressEditorDirtyNotifications)
        {
            return;
        }

        if (e.PropertyName == nameof(ValidationEditor.MachineId))
        {
            Editor.MachineName = FindMachineName(Editor.MachineId) ?? string.Empty;
            if (Editor.MachineId.HasValue)
            {
                Editor.ComponentId = null;
            }
        }
        else if (e.PropertyName == nameof(ValidationEditor.ComponentId))
        {
            Editor.ComponentName = FindComponentName(Editor.ComponentId) ?? string.Empty;
            if (Editor.ComponentId.HasValue)
            {
                Editor.MachineId = null;
            }
        }

        if (IsInEditMode)
        {
            MarkDirty();
        }
    }

    private void ApplyDefaultLookups()
    {
        if (MachineOptions.Count > 0)
        {
            Editor.MachineId = MachineOptions[0].Id;
        }
        else if (ComponentOptions.Count > 0)
        {
            Editor.ComponentId = ComponentOptions[0].Id;
        }

        if (string.IsNullOrWhiteSpace(Editor.Type) && TypeOptions.Count > 0)
        {
            Editor.Type = TypeOptions[0];
        }

        if (string.IsNullOrWhiteSpace(Editor.Status) && StatusOptions.Count > 0)
        {
            Editor.Status = StatusOptions[0];
        }
    }

    private void RefreshMachineOptions(IEnumerable<Machine> machines)
    {
        MachineOptions.Clear();
        foreach (var machine in machines)
        {
            var name = string.IsNullOrWhiteSpace(machine.Name)
                ? machine.Id.ToString(CultureInfo.InvariantCulture)
                : machine.Name!;
            MachineOptions.Add(new MachineOption(machine.Id, name));
        }

        Editor.MachineName = FindMachineName(Editor.MachineId) ?? string.Empty;
    }

    private void RefreshComponentOptions(IEnumerable<MachineComponent> components)
    {
        ComponentOptions.Clear();
        foreach (var component in components)
        {
            var name = string.IsNullOrWhiteSpace(component.Name)
                ? component.Id.ToString(CultureInfo.InvariantCulture)
                : component.Name!;
            ComponentOptions.Add(new ComponentOption(component.Id, name));
        }

        Editor.ComponentName = FindComponentName(Editor.ComponentId) ?? string.Empty;
    }

    private ModuleRecord ToRecord(Validation validation)
    {
        var targetName = FindMachineName(validation.MachineId) ?? FindComponentName(validation.ComponentId) ?? "Unassigned";
        var fields = new List<InspectorField>
        {
            new("Type", validation.Type),
            new("Status", string.IsNullOrWhiteSpace(validation.Status) ? "-" : validation.Status),
            new("Target", targetName),
            new("Start", validation.DateStart?.ToString("d", CultureInfo.CurrentCulture) ?? "-"),
            new("Next Due", validation.NextDue?.ToString("d", CultureInfo.CurrentCulture) ?? "-")
        };

        var relatedKey = validation.ComponentId.HasValue
            ? ComponentsModuleViewModel.ModuleKey
            : validation.MachineId.HasValue
                ? AssetsModuleViewModel.ModuleKey
                : null;
        var relatedParameter = validation.ComponentId ?? validation.MachineId as object;

        return new ModuleRecord(
            validation.Id.ToString(CultureInfo.InvariantCulture),
            $"{validation.Code} ({validation.Type})",
            validation.Code,
            validation.Status,
            validation.Comment,
            fields,
            relatedKey,
            relatedParameter);
    }

    private string? FindMachineName(int? machineId)
    {
        if (!machineId.HasValue)
        {
            return null;
        }

        return _machines.FirstOrDefault(m => m.Id == machineId.Value)?.Name;
    }

    private string? FindComponentName(int? componentId)
    {
        if (!componentId.HasValue)
        {
            return null;
        }

        return _components.FirstOrDefault(c => c.Id == componentId.Value)?.Name;
    }

    private void ReplaceRecord(Validation validation)
    {
        var key = validation.Id.ToString(CultureInfo.InvariantCulture);
        var index = Records.ToList().FindIndex(r => r.Key == key);
        if (index >= 0)
        {
            Records[index] = ToRecord(validation);
            SelectedRecord = Records[index];
        }
        else
        {
            Records.Add(ToRecord(validation));
            SelectedRecord = Records.Last();
        }
    }

    private bool CanAttachDocument()
        => !IsBusy
           && !IsEditorEnabled
           && _loadedValidation is { Id: > 0 };

    private async Task AttachDocumentAsync()
    {
        if (_loadedValidation is null || _loadedValidation.Id <= 0)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var files = await _filePicker
                .PickFilesAsync(new FilePickerRequest(AllowMultiple: true, Title: string.Format(YasGMP.Wpf.Helpers.Loc.S("Attachment_Picker_Title", "Attach files to {0}"), _loadedValidation.Code)))
                .ConfigureAwait(false);

            if (files is null || files.Count == 0)
            {
                StatusMessage = YasGMP.Wpf.Helpers.Loc.S("Status_Attach_Cancelled", "Attachment upload cancelled.");
                return;
            }

            var processed = 0;
            var deduplicated = 0;
            foreach (var file in files)
            {
                await using var stream = await file.OpenReadAsync().ConfigureAwait(false);
                var request = new AttachmentUploadRequest
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    EntityType = "validations",
                    EntityId = _loadedValidation.Id,
                    UploadedById = _authContext.CurrentUser?.Id,
                    Reason = $"validation:{_loadedValidation.Id}",
                    SourceIp = _authContext.CurrentIpAddress ?? string.Empty,
                    SourceHost = _authContext.CurrentDeviceInfo ?? string.Empty,
                    Notes = $"WPF:{ModuleKey}:{DateTime.UtcNow:O}"
                };

                var result = await _attachmentWorkflow.UploadAsync(stream, request).ConfigureAwait(false);
                processed++;
                if (result.Deduplicated)
                {
                    deduplicated++;
                }
            }

            StatusMessage = AttachmentStatusFormatter.Format(processed, deduplicated);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Error_Attach_UploadFailed", "Attachment upload failed: {0}"), ex.Message);
        }
        finally
        {
            IsBusy = false;
            UpdateAttachmentCommandState();
        }
    }

    private void UpdateAttachmentCommandState()
    {
        YasGMP.Wpf.Helpers.UiCommandHelper.NotifyCanExecuteOnUi(AttachDocumentCommand);
    }

    public sealed partial class ValidationEditor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _code = string.Empty;

        [ObservableProperty]
        private string _type = string.Empty;

        [ObservableProperty]
        private int? _machineId;

        [ObservableProperty]
        private string _machineName = string.Empty;

        [ObservableProperty]
        private int? _componentId;

        [ObservableProperty]
        private string _componentName = string.Empty;

        [ObservableProperty]
        private DateTime? _dateStart = DateTime.UtcNow.Date;

        [ObservableProperty]
        private DateTime? _dateEnd;

        [ObservableProperty]
        private DateTime? _nextDue = DateTime.UtcNow.Date.AddMonths(12);

        [ObservableProperty]
        private string _status = string.Empty;

        [ObservableProperty]
        private string _documentation = string.Empty;

        [ObservableProperty]
        private string _comment = string.Empty;

        public static ValidationEditor CreateEmpty() => new();

        public static ValidationEditor CreateForNew() => new();

        public static ValidationEditor FromValidation(
            Validation validation,
            Func<int?, string?> machineLookup,
            Func<int?, string?> componentLookup)
        {
            return new ValidationEditor
            {
                Id = validation.Id,
                Code = validation.Code ?? string.Empty,
                Type = validation.Type ?? string.Empty,
                MachineId = validation.MachineId,
                MachineName = machineLookup(validation.MachineId) ?? string.Empty,
                ComponentId = validation.ComponentId,
                ComponentName = componentLookup(validation.ComponentId) ?? string.Empty,
                DateStart = validation.DateStart,
                DateEnd = validation.DateEnd,
                NextDue = validation.NextDue,
                Status = validation.Status ?? string.Empty,
                Documentation = validation.Documentation ?? string.Empty,
                Comment = validation.Comment ?? string.Empty
            };
        }

        public Validation ToValidation(Validation? existing)
        {
            var validation = existing is null ? new Validation() : CloneValidation(existing);
            validation.Id = Id;
            validation.Code = Code ?? string.Empty;
            validation.Type = Type ?? string.Empty;
            validation.MachineId = MachineId;
            validation.ComponentId = ComponentId;
            validation.DateStart = DateStart;
            validation.DateEnd = DateEnd;
            validation.NextDue = NextDue;
            validation.Status = Status ?? string.Empty;
            validation.Documentation = Documentation ?? string.Empty;
            validation.Comment = Comment ?? string.Empty;
            return validation;
        }

        public ValidationEditor Clone()
            => new()
            {
                Id = Id,
                Code = Code,
                Type = Type,
                MachineId = MachineId,
                MachineName = MachineName,
                ComponentId = ComponentId,
                ComponentName = ComponentName,
                DateStart = DateStart,
                DateEnd = DateEnd,
                NextDue = NextDue,
                Status = Status,
                Documentation = Documentation,
                Comment = Comment
            };

        private static Validation CloneValidation(Validation source)
        {
            return new Validation
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
                Comment = source.Comment,
                NextDue = source.NextDue
            };
        }
    }

    /// <summary>Executes the machine option routine for the Validations module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public readonly record struct MachineOption(int Id, string Name)
    {
        public override string ToString() => Name;
    }

    /// <summary>Executes the component option routine for the Validations module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public readonly record struct ComponentOption(int Id, string Name)
    {
        public override string ToString() => Name;
    }
}



