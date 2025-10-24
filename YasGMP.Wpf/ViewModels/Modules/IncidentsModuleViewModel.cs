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
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Coordinates incident intake and triage through the WPF shell using SAP B1 patterns.</summary>
/// <remarks>
/// Form Modes: Find filters incidents (including CFL jumps), Add seeds <see cref="IncidentEditor.CreateEmpty"/>, View keeps the editor read-only for investigation review, and Update enables edits, attachments, and link maintenance.
/// Audit &amp; Logging: Saves through <see cref="IIncidentCrudService"/> with enforced e-signatures and attachment uploads handled by the shared workflow service; audit persistence resides in those domain services.
/// Localization: Uses inline status/priorities (`"REPORTED"`, `"CAPA_LINKED"`, etc.) and `StatusMessage` prompts (e.g. signature and attachment feedback) pending localisation keys.
/// Navigation: ModuleKey `Incidents` aligns docking; incident records embed related module keys (CAPA, Work Orders) for Golden Arrow routing, and CFL prefixes (`"WO:"`, `"CAPA:"`) support targeted navigation.
/// </remarks>
public sealed partial class IncidentsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Shell registration key that binds Incidents into the docking layout.</summary>
    /// <remarks>Execution: Resolved when the shell composes modules and persists layouts. Form Mode: Identifier applies across Find/Add/View/Update. Localization: Currently paired with the inline caption "Incidents" until `Modules_Incidents_Title` is introduced.</remarks>
    public new const string ModuleKey = "Incidents";

    private readonly IIncidentCrudService _incidentService;
    private readonly IAuthContext _authContext;
    private readonly IFilePicker _filePicker;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;

    private const string WorkOrderCflPrefix = "WO:";
    private const string CapaCflPrefix = "CAPA:";

    private Incident? _loadedIncident;
    private IncidentEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;
    public IRelayCommand SummarizeWithAiCommand { get; }

    /// <summary>Executes the new routine for the Incidents module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public ObservableCollection<string> StatusOptions { get; } = new(new[]
    {
        "REPORTED",
        "INVESTIGATION",
        "CLASSIFIED",
        "DEVIATION_LINKED",
        "CAPA_LINKED",
        "CLOSED"
    });

    /// <summary>Executes the new routine for the Incidents module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public ObservableCollection<string> PriorityOptions { get; } = new(new[]
    {
        "Low",
        "Medium",
        "High",
        "Critical"
    });

    /// <summary>Executes the new routine for the Incidents module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public ObservableCollection<string> TypeOptions { get; } = new(new[]
    {
        "Deviation",
        "Quality",
        "Safety",
        "Security",
        "IT",
        "Maintenance"
    });

    /// <summary>Generated property exposing the editor for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_Editor` resources are available.</remarks>
    [ObservableProperty]
    private IncidentEditor _editor;

    /// <summary>Generated property exposing the is editor enabled for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_IsEditorEnabled` resources are available.</remarks>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Command executing the attach evidence workflow for the Incidents module.</summary>
    /// <remarks>Execution: Invoked when the correlated ribbon or toolbar control is activated. Form Mode: Enabled only when the current mode supports the action (generally Add/Update). Localization: Uses inline button labels/tooltips until `Ribbon_Incidents_AttachEvidence` resources are authored.</remarks>
    public IAsyncRelayCommand AttachEvidenceCommand { get; }

    /// <summary>Initializes the Incidents module view model with domain and shell services.</summary>
    /// <remarks>Execution: Invoked when the shell activates the module or Golden Arrow navigation materializes it. Form Mode: Seeds Find/View immediately while deferring Add/Update wiring to later transitions. Localization: Relies on inline strings for tab titles and prompts until module resources exist.</remarks>
    public IncidentsModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IIncidentCrudService incidentService,
        IAuthContext authContext,
        IFilePicker filePicker,
        IAttachmentWorkflowService attachmentWorkflow,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Incidents", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));

        Editor = IncidentEditor.CreateEmpty();
        AttachEvidenceCommand = new AsyncRelayCommand(AttachEvidenceAsync, CanAttachEvidence);
        SummarizeWithAiCommand = new RelayCommand(OpenAiSummary);
        Toolbar.Add(new ModuleToolbarCommand("Summarize (AI)", SummarizeWithAiCommand));
    }

    /// <summary>Loads Incidents records from domain services.</summary>
    /// <remarks>Execution: Triggered by Find refreshes and shell activation. Form Mode: Supplies data for Find/View while Add/Update reuse cached results. Localization: Emits inline status strings pending `Status_Incidents_Loaded` resources.</remarks>
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var incidents = await Database.GetAllIncidentsAsync().ConfigureAwait(false);
        foreach (var incident in incidents)
        {
            incident.Status = _incidentService.NormalizeStatus(incident.Status);
        }

        return incidents.Select(ToRecord).ToList();
    }

    /// <summary>Provides design-time sample data for the Incidents designer experience.</summary>
    /// <remarks>Execution: Invoked only by design-mode checks to support Blend/preview tooling. Form Mode: Mirrors Find mode to preview list layouts. Localization: Sample literals remain inline for clarity.</remarks>
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new(
                "INC-2024-01",
                "Deviation in filling line",
                "INC-2024-01",
                "INVESTIGATION",
                "Operator reported pressure drop on filling line",
                new[]
                {
                    new InspectorField("Type", "Deviation"),
                    new InspectorField("Priority", "High"),
                    new InspectorField("Detected", DateTime.Now.AddHours(-4).ToString("g", CultureInfo.CurrentCulture)),
                    new InspectorField("Investigator", "QA Investigator"),
                    new InspectorField("Linked CAPA", "102")
                },
                CapaModuleViewModel.ModuleKey,
                102),
            new(
                "INC-2024-02",
                "Audit trail alert",
                "INC-2024-02",
                "REPORTED",
                "Unexpected login attempts captured by monitoring",
                new[]
                {
                    new InspectorField("Type", "Security"),
                    new InspectorField("Priority", "Medium"),
                    new InspectorField("Detected", DateTime.Now.AddHours(-1).ToString("g", CultureInfo.CurrentCulture)),
                    new InspectorField("Linked Work Order", "75")
                },
                WorkOrdersModuleViewModel.ModuleKey,
                75)
        };

    /// <summary>Loads editor payloads for the selected Incidents record.</summary>
    /// <remarks>Execution: Triggered when document tabs change or shell routing targets `ModuleKey` "Incidents". Form Mode: Honors Add/Update safeguards to avoid overwriting dirty state. Localization: Inline status/error strings remain until `Status_Incidents` resources are available.</remarks>
    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedIncident = null;
            SetEditor(IncidentEditor.CreateEmpty());
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

        var incident = await _incidentService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (incident is null)
        {
            StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Status_Incidents_UnableToLoad", "Unable to load {0}."), record.Title);
            return;
        }

        incident.Status = _incidentService.NormalizeStatus(incident.Status);
        _loadedIncident = incident;
        LoadEditor(incident);
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
                SetEditor(IncidentEditor.CreateForNew(_authContext));
                if (SelectedRecord?.RelatedParameter is int relatedId)
                {
                    if (SelectedRecord.RelatedModuleKey == WorkOrdersModuleViewModel.ModuleKey)
                    {
                        Editor.WorkOrderId = relatedId;
                    }
                    else if (SelectedRecord.RelatedModuleKey == CapaModuleViewModel.ModuleKey)
                    {
                        Editor.CapaCaseId = relatedId;
                    }
                }
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
        if (SelectedRecord is null && _loadedIncident is null)
        {
            StatusMessage = "Select an incident to summarize.";
            return;
        }

        var i = _loadedIncident;
        string prompt;
        if (i is null)
        {
            prompt = $"Summarize incident: {SelectedRecord?.Title}. Provide likely root cause and next steps in <= 8 bullets.";
        }
        else
        {
            prompt = $"Summarize this incident in <= 8 bullets. Title={i.Title}; Type={i.Type}; Priority={i.Priority}; Status={i.Status}; ReportedBy={i.ReportedById}; AssignedTo={i.AssignedToId}; DetectedAt={i.DetectedAt:O}; Related WO={i.WorkOrderId}; Related CAPA={i.CapaCaseId}.";
        }

        var shell = YasGMP.Common.ServiceLocator.GetRequiredService<IShellInteractionService>();
        var doc = shell.OpenModule(AiModuleViewModel.ModuleKey, $"prompt:{prompt}");
        shell.Activate(doc);
    }

    /// <summary>Validates the current editor payload before persistence.</summary>
    /// <remarks>Execution: Invoked immediately prior to OK/Update actions. Form Mode: Only Add/Update trigger validation. Localization: Error messages flow from inline literals until validation resources are added.</remarks>
    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var errors = new List<string>();
        try
        {
            var incident = Editor.ToIncident(_loadedIncident);
            incident.Status = _incidentService.NormalizeStatus(incident.Status);
            _incidentService.Validate(incident);
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

    /// <summary>Builds the Choose-From-List request used for Golden Arrow navigation.</summary>
    /// <remarks>Execution: Called when the shell launches CFL dialogs, routing via `ModuleKey` "Incidents". Form Mode: Provides lookup data irrespective of current mode. Localization: Dialog titles and descriptions use inline strings until `CFL_Incidents` resources exist.</remarks>
    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        if (!IsInEditMode)
        {
            StatusMessage = YasGMP.Wpf.Helpers.Loc.S("Status_Incidents_CflOnlyInEdit", "CFL lookups are only available while editing an incident.");
            return null;
        }

        try
        {
            var items = new List<CflItem>();

            var workOrders = await Database.GetAllWorkOrdersFullAsync().ConfigureAwait(false);
            foreach (var wo in workOrders
                .OrderByDescending(w => w.DateOpen)
                .Take(25))
            {
                var label = string.IsNullOrWhiteSpace(wo.Title)
                    ? $"WO-{wo.Id:D5}"
                    : $"WO-{wo.Id:D5} â€˘ {wo.Title}";

                var descriptionParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(wo.Type))
                {
                    descriptionParts.Add(wo.Type);
                }

                if (!string.IsNullOrWhiteSpace(wo.Status))
                {
                    descriptionParts.Add(wo.Status);
                }

                items.Add(new CflItem(
                    $"{WorkOrderCflPrefix}{wo.Id}",
                    label,
                    descriptionParts.Count > 0 ? string.Join(" â€˘ ", descriptionParts) : string.Empty));
            }

            var capaCases = await Database.GetAllCapaCasesAsync().ConfigureAwait(false);
            foreach (var capa in capaCases
                .OrderByDescending(c => c.DateOpen)
                .Take(25))
            {
                var code = $"CAPA-{capa.Id:D5}";
                var label = string.IsNullOrWhiteSpace(capa.Title)
                    ? code
                    : $"{code} â€˘ {capa.Title}";

                var descriptionParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(capa.Status))
                {
                    descriptionParts.Add(capa.Status);
                }

                if (!string.IsNullOrWhiteSpace(capa.Priority))
                {
                    descriptionParts.Add($"Priority: {capa.Priority}");
                }

                items.Add(new CflItem(
                    $"{CapaCflPrefix}{capa.Id}",
                    label,
                    descriptionParts.Count > 0 ? string.Join(" â€˘ ", descriptionParts) : string.Empty));
            }

            if (items.Count == 0)
            {
                StatusMessage = YasGMP.Wpf.Helpers.Loc.S("Status_Incidents_NoRelatedForLink", "No related CAPA cases or work orders available for linking.");
                return null;
            }

            return new CflRequest(YasGMP.Wpf.Helpers.Loc.S("CFL_Select_RelatedRecord", "Select related record"), items);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Status_Incidents_UnableToLoadRelated", "Unable to load related records: {0}"), ex.Message);
            return null;
        }
    }

    /// <summary>Applies CFL selections back into the Incidents workspace.</summary>
    /// <remarks>Execution: Runs after CFL or Golden Arrow completion, updating `StatusMessage` for `ModuleKey` "Incidents". Form Mode: Navigates records without disturbing active edits. Localization: Status feedback uses inline phrases pending `Status_Incidents_Filtered`.</remarks>
    protected override Task OnCflSelectionAsync(CflResult result)
    {
        if (!IsInEditMode)
        {
            return Task.CompletedTask;
        }

        var key = result.Selected.Key ?? string.Empty;

        if (key.StartsWith(WorkOrderCflPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(key.AsSpan(WorkOrderCflPrefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out var workOrderId))
            {
                Editor.WorkOrderId = workOrderId;
                StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Status_Incidents_LinkedWorkOrder", "Linked work order {0}."), result.Selected.Label);
                MarkDirty();
            }

            return Task.CompletedTask;
        }

        if (key.StartsWith(CapaCflPrefix, StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(key.AsSpan(CapaCflPrefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out var capaId))
            {
                Editor.CapaCaseId = capaId;
                Editor.LinkedCapaId = capaId;
                StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Status_Incidents_LinkedCapa", "Linked CAPA case {0}."), result.Selected.Label);
                MarkDirty();
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>Persists the current record and coordinates signatures, attachments, and audits.</summary>
    /// <remarks>Execution: Runs after validation when OK/Update is confirmed. Form Mode: Exclusive to Add/Update operations. Localization: Success/failure messaging remains inline pending dedicated resources.</remarks>
    protected override async Task<bool> OnSaveAsync()
    {
        var incident = Editor.ToIncident(_loadedIncident);
        incident.Status = _incidentService.NormalizeStatus(incident.Status);

        if (Mode == FormMode.Update && _loadedIncident is null)
        {
            StatusMessage = "Select an incident before saving.";
            return false;
        }

        var recordId = Mode == FormMode.Update ? _loadedIncident!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("incidents", recordId))
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

        incident.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;
        incident.LastModified = DateTime.UtcNow;
        incident.LastModifiedById = _authContext.CurrentUser?.Id;
        incident.SourceIp = _authContext.CurrentIpAddress ?? incident.SourceIp ?? string.Empty;

        var context = IncidentCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress ?? string.Empty,
            _authContext.CurrentDeviceInfo ?? string.Empty,
            _authContext.CurrentSessionId,
            signatureResult);

        Incident adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _incidentService.CreateAsync(incident, context).ConfigureAwait(false);
                if (incident.Id == 0 && saveResult.Id > 0)
                {
                    incident.Id = saveResult.Id;
                }

                adapterResult = incident;
            }
            else if (Mode == FormMode.Update)
            {
                incident.Id = _loadedIncident!.Id;
                saveResult = await _incidentService.UpdateAsync(incident, context).ConfigureAwait(false);
                adapterResult = incident;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist incident: {ex.Message}", ex);
        }

        _loadedIncident = incident;
        LoadEditor(incident);

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "incidents",
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
            if (_loadedIncident is not null)
            {
                LoadEditor(_loadedIncident);
            }
            else
            {
                SetEditor(IncidentEditor.CreateEmpty());
            }
        }
        else if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }
    }

    /// <summary>Executes the matches search routine for the Incidents module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    protected override bool MatchesSearch(ModuleRecord record, string searchText)
    {
        if (base.MatchesSearch(record, searchText))
        {
            return true;
        }

        return record.InspectorFields.Any(f => f.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    private async Task AttachEvidenceAsync()
    {
        if (_loadedIncident is null || _loadedIncident.Id <= 0)
        {
            return;
        }

        try
        {
            IsBusy = true;
            UpdateAttachmentCommandState();

            var files = await _filePicker
                .PickFilesAsync(new FilePickerRequest(true, null, "Select incident evidence"))
                .ConfigureAwait(false);

            if (files.Count == 0)
            {
                StatusMessage = "No files selected.";
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
                    EntityType = "incidents",
                    EntityId = _loadedIncident.Id,
                    UploadedById = uploadedBy,
                    Reason = $"incident:{_loadedIncident.Id}",
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
            StatusMessage = $"Attachment upload failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            UpdateAttachmentCommandState();
        }
    }

    private bool CanAttachEvidence()
        => !IsBusy && _loadedIncident is { Id: > 0 } && Mode is FormMode.View or FormMode.Update;

    private void LoadEditor(Incident incident)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = IncidentEditor.FromIncident(incident, _incidentService.NormalizeStatus);
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private void SetEditor(IncidentEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private void UpdateAttachmentCommandState()
    {
        YasGMP.Wpf.Helpers.UiCommandHelper.NotifyCanExecuteOnUi(AttachEvidenceCommand);
    }

    partial void OnEditorChanging(IncidentEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(IncidentEditor value)
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

    private static ModuleRecord ToRecord(Incident incident)
    {
        var fields = new List<InspectorField>
        {
            new("Type", string.IsNullOrWhiteSpace(incident.Type) ? "-" : incident.Type),
            new("Priority", string.IsNullOrWhiteSpace(incident.Priority) ? "-" : incident.Priority),
            new("Detected", incident.DetectedAt.ToString("g", CultureInfo.CurrentCulture)),
            new("Investigator", string.IsNullOrWhiteSpace(incident.AssignedInvestigator) ? "-" : incident.AssignedInvestigator),
            new("Classification", string.IsNullOrWhiteSpace(incident.Classification) ? "-" : incident.Classification)
        };

        if (incident.LinkedCapaId.HasValue)
        {
            fields.Add(new InspectorField("Linked CAPA", incident.LinkedCapaId.Value.ToString(CultureInfo.InvariantCulture)));
        }

        if (incident.WorkOrderId.HasValue)
        {
            fields.Add(new InspectorField("Work Order", incident.WorkOrderId.Value.ToString(CultureInfo.InvariantCulture)));
        }

        string? relatedModule = null;
        object? relatedParameter = null;

        if (incident.LinkedCapaId.HasValue)
        {
            relatedModule = CapaModuleViewModel.ModuleKey;
            relatedParameter = incident.LinkedCapaId.Value;
        }
        else if (incident.WorkOrderId.HasValue)
        {
            relatedModule = WorkOrdersModuleViewModel.ModuleKey;
            relatedParameter = incident.WorkOrderId.Value;
        }

        return new ModuleRecord(
            incident.Id.ToString(CultureInfo.InvariantCulture),
            incident.Title,
            incident.Id.ToString(CultureInfo.InvariantCulture),
            incident.Status,
            incident.Description,
            fields,
            relatedModule,
            relatedParameter);
    }
}

public sealed partial class IncidentEditor : ObservableObject
{
    /// <summary>Generated property exposing the id for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_Id` resources are available.</remarks>
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>Generated property exposing the type for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_Type` resources are available.</remarks>
    [ObservableProperty]
    private string? _type;

    /// <summary>Generated property exposing the priority for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_Priority` resources are available.</remarks>
    [ObservableProperty]
    private string? _priority;

    [ObservableProperty]
    private DateTime _detectedAt = DateTime.UtcNow;

    /// <summary>Generated property exposing the reported at for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_ReportedAt` resources are available.</remarks>
    [ObservableProperty]
    private DateTime? _reportedAt;

    /// <summary>Generated property exposing the reported by id for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_ReportedById` resources are available.</remarks>
    [ObservableProperty]
    private int? _reportedById;

    /// <summary>Generated property exposing the assigned to id for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_AssignedToId` resources are available.</remarks>
    [ObservableProperty]
    private int? _assignedToId;

    /// <summary>Generated property exposing the work order id for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_WorkOrderId` resources are available.</remarks>
    [ObservableProperty]
    private int? _workOrderId;

    /// <summary>Generated property exposing the capa case id for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_CapaCaseId` resources are available.</remarks>
    [ObservableProperty]
    private int? _capaCaseId;

    [ObservableProperty]
    private string _status = "REPORTED";

    /// <summary>Generated property exposing the root cause for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_RootCause` resources are available.</remarks>
    [ObservableProperty]
    private string? _rootCause;

    /// <summary>Generated property exposing the closed at for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_ClosedAt` resources are available.</remarks>
    [ObservableProperty]
    private DateTime? _closedAt;

    /// <summary>Generated property exposing the closed by id for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_ClosedById` resources are available.</remarks>
    [ObservableProperty]
    private int? _closedById;

    /// <summary>Generated property exposing the assigned investigator for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_AssignedInvestigator` resources are available.</remarks>
    [ObservableProperty]
    private string? _assignedInvestigator;

    /// <summary>Generated property exposing the classification for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_Classification` resources are available.</remarks>
    [ObservableProperty]
    private string? _classification;

    /// <summary>Generated property exposing the linked deviation id for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_LinkedDeviationId` resources are available.</remarks>
    [ObservableProperty]
    private int? _linkedDeviationId;

    /// <summary>Generated property exposing the linked capa id for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_LinkedCapaId` resources are available.</remarks>
    [ObservableProperty]
    private int? _linkedCapaId;

    /// <summary>Generated property exposing the closure comment for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_ClosureComment` resources are available.</remarks>
    [ObservableProperty]
    private string? _closureComment;

    /// <summary>Generated property exposing the source ip for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_SourceIp` resources are available.</remarks>
    [ObservableProperty]
    private string? _sourceIp;

    /// <summary>Generated property exposing the notes for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_Notes` resources are available.</remarks>
    [ObservableProperty]
    private string? _notes;

    /// <summary>Generated property exposing the is critical for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_IsCritical` resources are available.</remarks>
    [ObservableProperty]
    private bool _isCritical;

    /// <summary>Generated property exposing the risk level for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_RiskLevel` resources are available.</remarks>
    [ObservableProperty]
    private int _riskLevel;

    /// <summary>Generated property exposing the anomaly score for the Incidents module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Incidents_AnomalyScore` resources are available.</remarks>
    [ObservableProperty]
    private double? _anomalyScore;

    /// <summary>Executes the create empty routine for the Incidents module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public static IncidentEditor CreateEmpty()
        => new()
        {
            DetectedAt = DateTime.UtcNow,
            Status = "REPORTED"
        };

    /// <summary>Executes the create for new routine for the Incidents module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public static IncidentEditor CreateForNew(IAuthContext authContext)
    {
        return new IncidentEditor
        {
            DetectedAt = DateTime.UtcNow,
            ReportedAt = DateTime.UtcNow,
            ReportedById = authContext.CurrentUser?.Id,
            Status = "REPORTED"
        };
    }

    /// <summary>Executes the from incident routine for the Incidents module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public static IncidentEditor FromIncident(Incident incident, Func<string?, string> normalizeStatus)
    {
        return new IncidentEditor
        {
            Id = incident.Id,
            Title = incident.Title,
            Description = incident.Description,
            Type = incident.Type,
            Priority = incident.Priority,
            DetectedAt = incident.DetectedAt,
            ReportedAt = incident.ReportedAt,
            ReportedById = incident.ReportedById,
            AssignedToId = incident.AssignedToId,
            WorkOrderId = incident.WorkOrderId,
            CapaCaseId = incident.CapaCaseId,
            Status = normalizeStatus(incident.Status),
            RootCause = incident.RootCause,
            ClosedAt = incident.ClosedAt,
            ClosedById = incident.ClosedById,
            AssignedInvestigator = incident.AssignedInvestigator,
            Classification = incident.Classification,
            LinkedDeviationId = incident.LinkedDeviationId,
            LinkedCapaId = incident.LinkedCapaId,
            ClosureComment = incident.ClosureComment,
            SourceIp = incident.SourceIp,
            Notes = incident.Notes,
            IsCritical = incident.IsCritical,
            RiskLevel = incident.RiskLevel,
            AnomalyScore = incident.AnomalyScore
        };
    }

    /// <summary>Executes the clone routine for the Incidents module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public IncidentEditor Clone()
        => new()
        {
            Id = Id,
            Title = Title,
            Description = Description,
            Type = Type,
            Priority = Priority,
            DetectedAt = DetectedAt,
            ReportedAt = ReportedAt,
            ReportedById = ReportedById,
            AssignedToId = AssignedToId,
            WorkOrderId = WorkOrderId,
            CapaCaseId = CapaCaseId,
            Status = Status,
            RootCause = RootCause,
            ClosedAt = ClosedAt,
            ClosedById = ClosedById,
            AssignedInvestigator = AssignedInvestigator,
            Classification = Classification,
            LinkedDeviationId = LinkedDeviationId,
            LinkedCapaId = LinkedCapaId,
            ClosureComment = ClosureComment,
            SourceIp = SourceIp,
            Notes = Notes,
            IsCritical = IsCritical,
            RiskLevel = RiskLevel,
            AnomalyScore = AnomalyScore
        };

    /// <summary>Executes the to incident routine for the Incidents module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public Incident ToIncident(Incident? existing)
    {
        var incident = existing is null ? new Incident() : new Incident { Id = existing.Id };

        incident.Id = Id > 0 ? Id : incident.Id;
        incident.Title = Title?.Trim() ?? string.Empty;
        incident.Description = Description?.Trim() ?? string.Empty;
        incident.Type = string.IsNullOrWhiteSpace(Type) ? null : Type.Trim();
        incident.Priority = string.IsNullOrWhiteSpace(Priority) ? null : Priority.Trim();
        incident.DetectedAt = DetectedAt == default ? DateTime.UtcNow : DetectedAt;
        incident.ReportedAt = ReportedAt;
        incident.ReportedById = ReportedById;
        incident.AssignedToId = AssignedToId;
        incident.WorkOrderId = WorkOrderId;
        incident.CapaCaseId = CapaCaseId;
        incident.Status = Status ?? "REPORTED";
        incident.RootCause = string.IsNullOrWhiteSpace(RootCause) ? null : RootCause.Trim();
        incident.ClosedAt = ClosedAt;
        incident.ClosedById = ClosedById;
        incident.AssignedInvestigator = string.IsNullOrWhiteSpace(AssignedInvestigator) ? null : AssignedInvestigator.Trim();
        incident.Classification = string.IsNullOrWhiteSpace(Classification) ? null : Classification.Trim();
        incident.LinkedDeviationId = LinkedDeviationId;
        incident.LinkedCapaId = LinkedCapaId;
        incident.ClosureComment = string.IsNullOrWhiteSpace(ClosureComment) ? null : ClosureComment.Trim();
        incident.SourceIp = string.IsNullOrWhiteSpace(SourceIp) ? null : SourceIp.Trim();
        incident.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
        incident.IsCritical = IsCritical;
        incident.RiskLevel = RiskLevel;
        incident.AnomalyScore = AnomalyScore;

        return incident;
    }
}




