using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using ComponentEntity = YasGMP.Models.Component;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Runs CAPA case management within the WPF shell using SAP B1 form semantics.</summary>
/// <remarks>
/// Form Modes: Find filters cases via CFL, Add provisions a new <see cref="CapaEditor"/> with default status/priority, View locks the editor, and Update enables collaborative editing alongside attachment uploads.
/// Audit &amp; Logging: Persists CAPA records through <see cref="ICapaCrudService"/> while enforcing e-signature capture and attachment retention; detailed audit hashes are written by downstream services.
/// Localization: Emits inline captions such as `"CAPA"`, `"Attachment upload failed"`, and status prompts for workflows until resource keys (for status/priority labels) are introduced.
/// Navigation: ModuleKey `Capa` anchors docking, `CreateCflRequestAsync` surfaces Choose-From-List navigation, and status messages broadcast to the shell so Golden Arrow hops from related modules resolve back to CAPA cases.
/// </remarks>
public sealed partial class CapaModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Shell registration key that binds CAPA into the docking layout.</summary>
    /// <remarks>Execution: Resolved when the shell composes modules and persists layouts. Form Mode: Identifier applies across Find/Add/View/Update. Localization: Currently paired with the inline caption "CAPA" until `Modules_Capa_Title` is introduced.</remarks>
    public new const string ModuleKey = "Capa";

    private static readonly string[] DefaultStatuses =
    {
        "OPEN",
        "INVESTIGATION",
        "ACTION_DEFINED",
        "ACTION_APPROVED",
        "ACTION_EXECUTED",
        "EFFECTIVENESS_CHECK",
        "CLOSED"
    };

    private static readonly string[] DefaultPriorities =
    {
        "Critical",
        "High",
        "Medium",
        "Low"
    };

    private readonly ICapaCrudService _capaService;
    private readonly IComponentCrudService _componentService;
    private readonly IAuthContext _authContext;
    private readonly IFilePicker _filePicker;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;

    private IReadOnlyList<ComponentEntity> _components = Array.Empty<ComponentEntity>();
    private CapaCase? _loadedCapa;
    private CapaEditor? _snapshot;
    private bool _suppressDirtyNotifications;

    /// <summary>Initializes the CAPA module view model with domain and shell services.</summary>
    /// <remarks>Execution: Invoked when the shell activates the module or Golden Arrow navigation materializes it. Form Mode: Seeds Find/View immediately while deferring Add/Update wiring to later transitions. Localization: Relies on inline strings for tab titles and prompts until module resources exist.</remarks>
    public CapaModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ICapaCrudService capaService,
        IComponentCrudService componentService,
        IAuthContext authContext,
        IFilePicker filePicker,
        IAttachmentWorkflowService attachmentWorkflow,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "CAPA", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        _capaService = capaService ?? throw new ArgumentNullException(nameof(capaService));
        _componentService = componentService ?? throw new ArgumentNullException(nameof(componentService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));

        StatusOptions = Array.AsReadOnly(DefaultStatuses);
        PriorityOptions = Array.AsReadOnly(DefaultPriorities);
        ComponentOptions = new ObservableCollection<ComponentOption>();

        Editor = CapaEditor.CreateEmpty();
        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
        SummarizeWithAiCommand = new RelayCommand(OpenAiSummary);
        Toolbar.Add(new ModuleToolbarCommand("Summarize (AI)", SummarizeWithAiCommand));
    }

    /// <summary>Generated property exposing the editor for the CAPA module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Capa_Editor` resources are available.</remarks>
    [ObservableProperty]
    private CapaEditor _editor;

    /// <summary>Generated property exposing the is editor enabled for the CAPA module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Capa_IsEditorEnabled` resources are available.</remarks>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Collection presenting the component options for the CAPA document host.</summary>
    /// <remarks>Execution: Populated as records load or staging mutates. Form Mode: Visible in all modes with editing reserved for Add/Update. Localization: Grid headers/tooltips remain inline until `Modules_Capa_Grid` resources exist.</remarks>
    public ObservableCollection<ComponentOption> ComponentOptions { get; }

    /// <summary>Collection presenting the status options for the CAPA document host.</summary>
    /// <remarks>Execution: Populated as records load or staging mutates. Form Mode: Visible in all modes with editing reserved for Add/Update. Localization: Grid headers/tooltips remain inline until `Modules_Capa_Grid` resources exist.</remarks>
    public IReadOnlyList<string> StatusOptions { get; }

    /// <summary>Collection presenting the priority options for the CAPA document host.</summary>
    /// <remarks>Execution: Populated as records load or staging mutates. Form Mode: Visible in all modes with editing reserved for Add/Update. Localization: Grid headers/tooltips remain inline until `Modules_Capa_Grid` resources exist.</remarks>
    public IReadOnlyList<string> PriorityOptions { get; }

    /// <summary>Command executing the attach document workflow for the CAPA module.</summary>
    /// <remarks>Execution: Invoked when the correlated ribbon or toolbar control is activated. Form Mode: Enabled only when the current mode supports the action (generally Add/Update). Localization: Uses inline button labels/tooltips until `Ribbon_Capa_AttachDocument` resources are authored.</remarks>
    public IAsyncRelayCommand AttachDocumentCommand { get; }

    /// <summary>Opens the AI module to summarize the selected CAPA case.</summary>
    public IRelayCommand SummarizeWithAiCommand { get; }

    /// <summary>Loads CAPA records from domain services.</summary>
    /// <remarks>Execution: Triggered by Find refreshes and shell activation. Form Mode: Supplies data for Find/View while Add/Update reuse cached results. Localization: Emits inline status strings pending `Status_Capa_Loaded` resources.</remarks>
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        _components = await _componentService.GetAllAsync().ConfigureAwait(false);
        RefreshComponentOptions(_components);

        var capaCases = await _capaService.GetAllAsync().ConfigureAwait(false);
        foreach (var capa in capaCases)
        {
            capa.Status = _capaService.NormalizeStatus(capa.Status);
            capa.Priority = _capaService.NormalizePriority(capa.Priority);
        }

        return capaCases.Select(ToRecord).ToList();
    }

    /// <summary>Provides design-time sample data for the CAPA designer experience.</summary>
    /// <remarks>Execution: Invoked only by design-mode checks to support Blend/preview tooling. Form Mode: Mirrors Find mode to preview list layouts. Localization: Sample literals remain inline for clarity.</remarks>
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var components = new List<ComponentEntity>
        {
            new()
            {
                Id = 2001,
                Name = "Autoclave Steam Valve",
                Code = "CMP-AUTO-VAL"
            },
            new()
            {
                Id = 2002,
                Name = "Lyophilizer Shelf Probe",
                Code = "CMP-LYO-PROBE"
            }
        };

        _components = components;
        RefreshComponentOptions(components);

        var cases = new List<CapaCase>
        {
            new()
            {
                Id = 501,
                Title = "Temperature excursion investigation",
                Description = "Investigate freezer deviation and define corrective measures.",
                ComponentId = 2002,
                Priority = "High",
                Status = "INVESTIGATION",
                RootCause = "Door seal failure",
                CorrectiveAction = "Replace seal",
                PreventiveAction = "Monthly inspection",
                Reason = "Deviation GMP-2024-044",
                Actions = "Containment completed",
                Notes = "Requires QA sign-off",
                DateOpen = DateTime.UtcNow.AddDays(-7)
            },
            new()
            {
                Id = 502,
                Title = "Audit finding CAPA",
                Description = "Address supplier qualification gap discovered during audit.",
                ComponentId = 2001,
                Priority = "Medium",
                Status = "ACTION_DEFINED",
                RootCause = "Missing vendor re-evaluation",
                CorrectiveAction = "Perform vendor audit",
                PreventiveAction = "Implement annual review",
                Reason = "External audit finding",
                Actions = "Plan submitted",
                Notes = "Awaiting approval",
                DateOpen = DateTime.UtcNow.AddDays(-30)
            }
        };

        return cases.Select(ToRecord).ToList();
    }

    /// <summary>Loads editor payloads for the selected CAPA record.</summary>
    /// <remarks>Execution: Triggered when document tabs change or shell routing targets `ModuleKey` "Capa". Form Mode: Honors Add/Update safeguards to avoid overwriting dirty state. Localization: Inline status/error strings remain until `Status_Capa` resources are available.</remarks>
    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedCapa = null;
            SetEditor(CapaEditor.CreateEmpty());
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

        var capa = await _capaService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (capa is null)
        {
            StatusMessage = $"Unable to load {record.Title}.";
            return;
        }

        capa.Status = _capaService.NormalizeStatus(capa.Status);
        capa.Priority = _capaService.NormalizePriority(capa.Priority);
        _loadedCapa = capa;
        LoadEditor(capa);
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
                SetEditor(CapaEditor.CreateForNew(_authContext));
                ApplyRelatedDefaults();
                break;
            case FormMode.Update:
                _snapshot = Editor.Clone();
                break;
            case FormMode.View:
                _snapshot = null;
                break;
        }

        UpdateAttachmentCommandState();
        return Task.CompletedTask;
    }

    private void OpenAiSummary()
    {
        if (SelectedRecord is null && _loadedCapa is null)
        {
            StatusMessage = "Select a CAPA case to summarize.";
            return;
        }

        var c = _loadedCapa;
        string prompt;
        if (c is null)
        {
            prompt = $"Summarize CAPA: {SelectedRecord?.Title}. Provide root cause, actions and effectiveness checks in <= 8 bullets.";
        }
        else
        {
            prompt = $"Summarize this CAPA (<= 8 bullets). Title={c.Title}; Status={c.Status}; Priority={c.Priority}; Opened={c.OpenedAt:yyyy-MM-dd}; Closed={c.ClosedAt:yyyy-MM-dd}; RootCause={c.RootCause}; Corrective={c.CorrectiveAction}; Preventive={c.PreventiveAction}; ComponentId={c.ComponentId}.";
        }

        var shell = YasGMP.Common.ServiceLocator.GetRequiredService<IShellInteractionService>();
        var doc = shell.OpenModule(AiModuleViewModel.ModuleKey, $"prompt:{prompt}");
        shell.Activate(doc);
    }

    /// <summary>Persists the current record and coordinates signatures, attachments, and audits.</summary>
    /// <remarks>Execution: Runs after validation when OK/Update is confirmed. Form Mode: Exclusive to Add/Update operations. Localization: Success/failure messaging remains inline pending dedicated resources.</remarks>
    protected override async Task<bool> OnSaveAsync()
    {
        if (Mode == FormMode.Update && _loadedCapa is null)
        {
            StatusMessage = YasGMP.Wpf.Helpers.Loc.S("Status_CAPA_SelectBeforeSave", "Select a CAPA case before saving.");
            return false;
        }

        var capa = Editor.ToCapaCase(_loadedCapa);
        capa.Status = _capaService.NormalizeStatus(capa.Status);
        capa.Priority = _capaService.NormalizePriority(capa.Priority);

        var recordId = Mode == FormMode.Update ? _loadedCapa!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("capa_cases", recordId))
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

        capa.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;
        capa.LastModified = DateTime.UtcNow;
        capa.LastModifiedById = _authContext.CurrentUser?.Id;
        capa.SourceIp = _authContext.CurrentIpAddress ?? capa.SourceIp ?? string.Empty;

        var context = CapaCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            (_authContext.CurrentIpAddress ?? string.Empty),
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        CapaCase adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _capaService.CreateAsync(capa, context).ConfigureAwait(false);
                if (capa.Id == 0 && saveResult.Id > 0)
                {
                    capa.Id = saveResult.Id;
                }

                adapterResult = capa;
            }
            else if (Mode == FormMode.Update)
            {
                capa.Id = _loadedCapa!.Id;
                saveResult = await _capaService.UpdateAsync(capa, context).ConfigureAwait(false);
                adapterResult = capa;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(string.Format(YasGMP.Wpf.Helpers.Loc.S("Error_CAPA_SaveFailed", "Failed to persist CAPA case: {0}"), ex.Message), ex);
        }

        _loadedCapa = capa;
        LoadEditor(capa);

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "capa_cases",
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
            if (_loadedCapa is not null)
            {
                LoadEditor(_loadedCapa);
            }
            else
            {
                SetEditor(CapaEditor.CreateEmpty());
            }
        }
        else if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }
    }

    /// <summary>Builds the Choose-From-List request used for Golden Arrow navigation.</summary>
    /// <remarks>Execution: Called when the shell launches CFL dialogs, routing via `ModuleKey` "Capa". Form Mode: Provides lookup data irrespective of current mode. Localization: Dialog titles and descriptions use inline strings until `CFL_Capa` resources exist.</remarks>
    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var capaCases = await _capaService.GetAllAsync().ConfigureAwait(false);
        var items = capaCases
            .Select(capa =>
            {
                var key = capa.Id.ToString(CultureInfo.InvariantCulture);
                var label = string.IsNullOrWhiteSpace(capa.Title) ? key : capa.Title;
                var descriptionParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(capa.Priority))
                {
                    descriptionParts.Add(capa.Priority);
                }

                if (!string.IsNullOrWhiteSpace(capa.Status))
                {
                    descriptionParts.Add(capa.Status);
                }

                if (capa.DateOpen != default)
                {
                    descriptionParts.Add(capa.DateOpen.ToString("d", CultureInfo.CurrentCulture));
                }

                return new CflItem(key, label, descriptionParts.Count > 0 ? string.Join(" • ", descriptionParts) : null);
            })
            .ToList();

        return new CflRequest(YasGMP.Wpf.Helpers.Loc.S("CFL_Select_CAPA", "Select CAPA"), items);
    }

    /// <summary>Applies CFL selections back into the CAPA workspace.</summary>
    /// <remarks>Execution: Runs after CFL or Golden Arrow completion, updating `StatusMessage` for `ModuleKey` "Capa". Form Mode: Navigates records without disturbing active edits. Localization: Status feedback uses inline phrases pending `Status_Capa_Filtered`.</remarks>
    protected override Task OnCflSelectionAsync(CflResult result)
    {
        var match = Records.FirstOrDefault(r => r.Key == result.Selected.Key);
        if (match is not null)
        {
            SelectedRecord = match;
            SearchText = match.Title;
            StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Status_CAPA_LoadedTitle", "Loaded {0}."), match.Title);
            return Task.CompletedTask;
        }

        SearchText = result.Selected.Label;
        StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Status_CAPA_FilteredBy", "Filtered CAPA cases by \"{0}\"."), result.Selected.Label);
        RecordsView.Refresh();
        return Task.CompletedTask;
    }

    /// <summary>Executes the matches search routine for the CAPA module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    protected override bool MatchesSearch(ModuleRecord record, string searchText)
    {
        if (base.MatchesSearch(record, searchText))
        {
            return true;
        }

        return record.InspectorFields.Any(field =>
            field.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    private async Task AttachDocumentAsync()
    {
        if (_loadedCapa is null || _loadedCapa.Id <= 0)
        {
            return;
        }

        try
        {
            IsBusy = true;
            UpdateAttachmentCommandState();

            var files = await _filePicker
                .PickFilesAsync(new FilePickerRequest(true, null, YasGMP.Wpf.Helpers.Loc.S("Attachment_Picker_CAPA", "Select CAPA attachment")))
                .ConfigureAwait(false);

            if (files.Count == 0)
            {
                StatusMessage = YasGMP.Wpf.Helpers.Loc.S("Status_Attach_NoneSelected", "No files selected.");
                return;
            }

            var processed = 0;
            var deduplicated = 0;
            var uploadedBy = _authContext.CurrentUser?.Id;

            foreach (var file in files)
            {
                await using var stream = await file.OpenReadAsync().ConfigureAwait(false);
                var request = new AttachmentUploadRequest
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    EntityType = "capa_cases",
                    EntityId = _loadedCapa.Id,
                    UploadedById = uploadedBy,
                    Reason = $"capa:{_loadedCapa.Id}",
                    SourceIp = (_authContext.CurrentIpAddress ?? string.Empty),
                    SourceHost = _authContext.CurrentDeviceInfo,
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

    private bool CanAttachDocument()
        => !IsBusy && _loadedCapa is { Id: > 0 } && Mode is FormMode.View or FormMode.Update;

    private void LoadEditor(CapaCase capa)
    {
        _suppressDirtyNotifications = true;
        Editor = CapaEditor.FromCapa(capa, _capaService.NormalizeStatus, _capaService.NormalizePriority, ResolveComponentName);
        _suppressDirtyNotifications = false;
        ResetDirty();
    }

    private void SetEditor(CapaEditor editor)
    {
        _suppressDirtyNotifications = true;
        Editor = editor;
        _suppressDirtyNotifications = false;
        ResetDirty();
    }

    private void ApplyRelatedDefaults()
    {
        if (SelectedRecord?.RelatedParameter is int relatedId)
        {
            Editor.ComponentId = relatedId;
            Editor.ComponentName = ResolveComponentName(relatedId);
        }
    }

    private void RefreshComponentOptions(IEnumerable<ComponentEntity> components)
    {
        ComponentOptions.Clear();
        foreach (var component in components
                     .OrderBy(static c => string.IsNullOrWhiteSpace(c.Name) ? c.Code : c.Name)
                     .Select(c => new ComponentOption(c.Id, ResolveComponentName(c.Id))))
        {
            ComponentOptions.Add(component);
        }
    }

    private string ResolveComponentName(int componentId)
    {
        var component = _components.FirstOrDefault(c => c.Id == componentId);
        if (component is null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(component.Name))
        {
            return component.Name;
        }

        return component.Code ?? componentId.ToString(CultureInfo.InvariantCulture);
    }

    private static ModuleRecord ToRecord(CapaCase capa)
    {
        var fields = new List<InspectorField>
        {
            new("Priority", string.IsNullOrWhiteSpace(capa.Priority) ? "-" : capa.Priority),
            new("Status", string.IsNullOrWhiteSpace(capa.Status) ? "-" : capa.Status),
            new("Opened", capa.DateOpen == default ? "-" : capa.DateOpen.ToString("d", CultureInfo.CurrentCulture)),
            new("Assigned To", capa.AssignedTo?.FullName ?? capa.AssignedTo?.Username ?? "-"),
            new("ComponentEntity", capa.ComponentId.ToString(CultureInfo.InvariantCulture))
        };

        return new ModuleRecord(
            capa.Id.ToString(CultureInfo.InvariantCulture),
            string.IsNullOrWhiteSpace(capa.Title) ? $"CAPA-{capa.Id:D5}" : capa.Title,
            capa.CapaCode,
            capa.Status,
            capa.Description,
            fields);
    }

    private void UpdateAttachmentCommandState()
    {
        YasGMP.Wpf.Helpers.UiCommandHelper.NotifyCanExecuteOnUi(AttachDocumentCommand);
    }

    partial void OnEditorChanging(CapaEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(CapaEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged += OnEditorPropertyChanged;
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressDirtyNotifications)
        {
            return;
        }

        if (string.Equals(e.PropertyName, nameof(CapaEditor.ComponentId), StringComparison.Ordinal))
        {
            _suppressDirtyNotifications = true;
            Editor.ComponentName = ResolveComponentName(Editor.ComponentId);
            _suppressDirtyNotifications = false;
        }

        if (IsInEditMode)
        {
            MarkDirty();
        }
    }
}

public readonly record struct ComponentOption(int Id, string Name)
{
    /// <summary>Executes the to string routine for the CAPA module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public override string ToString() => Name;
}

public sealed partial class CapaEditor : ObservableObject
{
    /// <summary>Generated property exposing the id for the CAPA module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Capa_Id` resources are available.</remarks>
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>Generated property exposing the component id for the CAPA module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Capa_ComponentId` resources are available.</remarks>
    [ObservableProperty]
    private int _componentId;

    [ObservableProperty]
    private string _componentName = string.Empty;

    /// <summary>Generated property exposing the assigned to id for the CAPA module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Capa_AssignedToId` resources are available.</remarks>
    [ObservableProperty]
    private int? _assignedToId;

    [ObservableProperty]
    private string _priority = "Medium";

    [ObservableProperty]
    private string _status = "OPEN";

    [ObservableProperty]
    private DateTime _dateOpen = DateTime.UtcNow;

    /// <summary>Generated property exposing the date close for the CAPA module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Capa_DateClose` resources are available.</remarks>
    [ObservableProperty]
    private DateTime? _dateClose;

    [ObservableProperty]
    private string _reason = string.Empty;

    [ObservableProperty]
    private string _rootCause = string.Empty;

    [ObservableProperty]
    private string _correctiveAction = string.Empty;

    [ObservableProperty]
    private string _preventiveAction = string.Empty;

    [ObservableProperty]
    private string _actions = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _comments = string.Empty;

    [ObservableProperty]
    private string _digitalSignature = string.Empty;

    /// <summary>Executes the create empty routine for the CAPA module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public static CapaEditor CreateEmpty() => new();

    /// <summary>Executes the create for new routine for the CAPA module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public static CapaEditor CreateForNew(IAuthContext authContext)
    {
        return new CapaEditor
        {
            DateOpen = DateTime.UtcNow,
            AssignedToId = authContext.CurrentUser?.Id,
            Status = "OPEN",
            Priority = "Medium"
        };
    }

    /// <summary>Executes the from capa routine for the CAPA module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public static CapaEditor FromCapa(
        CapaCase capa,
        Func<string?, string> normalizeStatus,
        Func<string?, string> normalizePriority,
        Func<int, string> resolveComponentName)
    {
        return new CapaEditor
        {
            Id = capa.Id,
            Title = capa.Title,
            Description = capa.Description,
            ComponentId = capa.ComponentId,
            ComponentName = resolveComponentName(capa.ComponentId),
            AssignedToId = capa.AssignedToId,
            Priority = normalizePriority(capa.Priority),
            Status = normalizeStatus(capa.Status),
            DateOpen = capa.DateOpen == default ? DateTime.UtcNow : capa.DateOpen,
            DateClose = capa.DateClose,
            Reason = capa.Reason,
            RootCause = capa.RootCause,
            CorrectiveAction = capa.CorrectiveAction,
            PreventiveAction = capa.PreventiveAction,
            Actions = capa.Actions,
            Notes = capa.Notes,
            Comments = capa.Comments,
            DigitalSignature = capa.DigitalSignature
        };
    }

    /// <summary>Executes the clone routine for the CAPA module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public CapaEditor Clone()
        => new()
        {
            Id = Id,
            Title = Title,
            Description = Description,
            ComponentId = ComponentId,
            ComponentName = ComponentName,
            AssignedToId = AssignedToId,
            Priority = Priority,
            Status = Status,
            DateOpen = DateOpen,
            DateClose = DateClose,
            Reason = Reason,
            RootCause = RootCause,
            CorrectiveAction = CorrectiveAction,
            PreventiveAction = PreventiveAction,
            Actions = Actions,
            Notes = Notes,
            Comments = Comments,
            DigitalSignature = DigitalSignature
        };

    /// <summary>Executes the to capa case routine for the CAPA module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public CapaCase ToCapaCase(CapaCase? existing)
    {
        var capa = existing is null ? new CapaCase() : new CapaCase { Id = existing.Id };

        capa.Id = Id > 0 ? Id : capa.Id;
        capa.Title = (Title ?? string.Empty).Trim();
        capa.Description = (Description ?? string.Empty).Trim();
        capa.ComponentId = ComponentId;
        capa.AssignedToId = AssignedToId;
        capa.Priority = string.IsNullOrWhiteSpace(Priority) ? "Medium" : Priority.Trim();
        capa.Status = string.IsNullOrWhiteSpace(Status) ? "OPEN" : Status.Trim();
        capa.DateOpen = DateOpen == default ? DateTime.UtcNow : DateOpen;
        capa.DateClose = DateClose;
        capa.Reason = (Reason ?? string.Empty).Trim();
        capa.RootCause = (RootCause ?? string.Empty).Trim();
        capa.CorrectiveAction = (CorrectiveAction ?? string.Empty).Trim();
        capa.PreventiveAction = (PreventiveAction ?? string.Empty).Trim();
        capa.Actions = (Actions ?? string.Empty).Trim();
        capa.Notes = (Notes ?? string.Empty).Trim();
        capa.Comments = (Comments ?? string.Empty).Trim();
        capa.DigitalSignature = string.IsNullOrWhiteSpace(DigitalSignature)
            ? existing?.DigitalSignature ?? string.Empty
            : DigitalSignature.Trim();

        return capa;
    }
}





