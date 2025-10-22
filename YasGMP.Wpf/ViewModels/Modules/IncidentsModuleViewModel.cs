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

namespace YasGMP.Wpf.ViewModels.Modules;
/// <summary>
/// Represents the incidents module view model value.
/// </summary>

public sealed partial class IncidentsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>
    /// Represents the module key value.
    /// </summary>
    public const string ModuleKey = "Incidents";

    private readonly IIncidentCrudService _incidentService;
    private readonly IAuthContext _authContext;
    private readonly IFilePicker _filePicker;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private readonly ILocalizationService _localization;

    private const string WorkOrderCflPrefix = "WO:";
    private const string CapaCflPrefix = "CAPA:";

    private Incident? _loadedIncident;
    private IncidentEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;
    /// <summary>
    /// Gets or sets the status options.
    /// </summary>

    public ObservableCollection<string> StatusOptions { get; }
    /// <summary>
    /// Gets or sets the priority options.
    /// </summary>

    public ObservableCollection<string> PriorityOptions { get; }
    /// <summary>
    /// Gets or sets the type options.
    /// </summary>

    public ObservableCollection<string> TypeOptions { get; }

    [ObservableProperty]
    private IncidentEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;
    /// <summary>
    /// Gets or sets the attach evidence command.
    /// </summary>

    public IAsyncRelayCommand AttachEvidenceCommand { get; }

    /// <summary>
    /// Gets the command that persists a new incident.
    /// </summary>

    public IAsyncRelayCommand AddCommand { get; }

    /// <summary>
    /// Gets the command that advances the incident into the classified stage.
    /// </summary>

    public IAsyncRelayCommand ApproveCommand { get; }

    /// <summary>
    /// Gets the command that links follow-up work to the incident.
    /// </summary>

    public IAsyncRelayCommand ExecuteCommand { get; }

    /// <summary>
    /// Gets the command that closes the incident.
    /// </summary>

    public IAsyncRelayCommand CloseCommand { get; }
    /// <summary>
    /// Initializes a new instance of the IncidentsModuleViewModel class.
    /// </summary>

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
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, localization.GetString("Module.Title.Incidents"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        Editor = IncidentEditor.CreateEmpty();
        StatusOptions = new ObservableCollection<string>(new[]
        {
            _localization.GetString("Module.Incidents.Status.Reported"),
            _localization.GetString("Module.Incidents.Status.Investigation"),
            _localization.GetString("Module.Incidents.Status.Classified"),
            _localization.GetString("Module.Incidents.Status.DeviationLinked"),
            _localization.GetString("Module.Incidents.Status.CapaLinked"),
            _localization.GetString("Module.Incidents.Status.Closed")
        });
        PriorityOptions = new ObservableCollection<string>(new[]
        {
            _localization.GetString("Module.Incidents.Priority.Low"),
            _localization.GetString("Module.Incidents.Priority.Medium"),
            _localization.GetString("Module.Incidents.Priority.High"),
            _localization.GetString("Module.Incidents.Priority.Critical")
        });
        TypeOptions = new ObservableCollection<string>(new[]
        {
            _localization.GetString("Module.Incidents.Type.Deviation"),
            _localization.GetString("Module.Incidents.Type.Quality"),
            _localization.GetString("Module.Incidents.Type.Safety"),
            _localization.GetString("Module.Incidents.Type.Security"),
            _localization.GetString("Module.Incidents.Type.It"),
            _localization.GetString("Module.Incidents.Type.Maintenance")
        });
        AttachEvidenceCommand = new AsyncRelayCommand(AttachEvidenceAsync, CanAttachEvidence);
        AddCommand = new AsyncRelayCommand(AddAsync, CanAdd);
        ApproveCommand = new AsyncRelayCommand(ApproveAsync, CanApprove);
        ExecuteCommand = new AsyncRelayCommand(ExecuteAsync, CanExecute);
        CloseCommand = new AsyncRelayCommand(CloseAsync, CanClose);
        UpdateWorkflowCommandState();
    }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var incidents = await Database.GetAllIncidentsAsync().ConfigureAwait(false);
        foreach (var incident in incidents)
        {
            incident.Status = _incidentService.NormalizeStatus(incident.Status);
        }

        return incidents.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new(
                "INC-2024-01",
                "Deviation in filling line",
                "INC-2024-01",
                StatusOptions[1],
                "Operator reported pressure drop on filling line",
                new[]
                {
                    new InspectorField("Type", TypeOptions[0]),
                    new InspectorField("Priority", PriorityOptions[2]),
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
                StatusOptions[0],
                "Unexpected login attempts captured by monitoring",
                new[]
                {
                    new InspectorField("Type", TypeOptions[3]),
                    new InspectorField("Priority", PriorityOptions[1]),
                    new InspectorField("Detected", DateTime.Now.AddHours(-1).ToString("g", CultureInfo.CurrentCulture)),
                    new InspectorField("Linked Work Order", "75")
                },
                WorkOrdersModuleViewModel.ModuleKey,
                75)
        };

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
            StatusMessage = $"Unable to load {record.Title}.";
            return;
        }

        incident.Status = _incidentService.NormalizeStatus(incident.Status);
        _loadedIncident = incident;
        LoadEditor(incident);
        UpdateAttachmentCommandState();
        UpdateWorkflowCommandState();
    }

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
        UpdateWorkflowCommandState();
        return Task.CompletedTask;
    }

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

    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        if (!IsInEditMode)
        {
            StatusMessage = "CFL lookups are only available while editing an incident.";
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
                    : $"WO-{wo.Id:D5} • {wo.Title}";

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
                    descriptionParts.Count > 0 ? string.Join(" • ", descriptionParts) : string.Empty));
            }

            var capaCases = await Database.GetAllCapaCasesAsync().ConfigureAwait(false);
            foreach (var capa in capaCases
                .OrderByDescending(c => c.DateOpen)
                .Take(25))
            {
                var code = $"CAPA-{capa.Id:D5}";
                var label = string.IsNullOrWhiteSpace(capa.Title)
                    ? code
                    : $"{code} • {capa.Title}";

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
                    descriptionParts.Count > 0 ? string.Join(" • ", descriptionParts) : string.Empty));
            }

            if (items.Count == 0)
            {
                StatusMessage = "No related CAPA cases or work orders available for linking.";
                return null;
            }

            return new CflRequest("Select related record", items);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to load related records: {ex.Message}";
            return null;
        }
    }

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
                StatusMessage = $"Linked work order {result.Selected.Label}.";
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
                StatusMessage = $"Linked CAPA case {result.Selected.Label}.";
                MarkDirty();
            }
        }

        return Task.CompletedTask;
    }

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
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
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
                    SourceIp = _authContext.CurrentIpAddress,
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
        UpdateWorkflowCommandState();
    }

    private void SetEditor(IncidentEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateWorkflowCommandState();
    }

    private void UpdateAttachmentCommandState()
        => AttachEvidenceCommand.NotifyCanExecuteChanged();

    private void UpdateWorkflowCommandState()
    {
        AddCommand.NotifyCanExecuteChanged();
        ApproveCommand.NotifyCanExecuteChanged();
        ExecuteCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();
    }

    private async Task AddAsync()
    {
        if (!CanAdd())
        {
            return;
        }

        try
        {
            IsBusy = true;
            var incident = Editor.ToIncident(null);
            incident.Status = _incidentService.NormalizeStatus("INVESTIGATION");
            incident.DetectedAt = incident.DetectedAt == default ? DateTime.UtcNow : incident.DetectedAt;
            incident.ReportedAt ??= DateTime.UtcNow;
            incident.ReportedById ??= _authContext.CurrentUser?.Id;
            incident.SourceIp ??= _authContext.CurrentIpAddress;

            _incidentService.Validate(incident);

            var context = IncidentCrudContext.Create(
                _authContext.CurrentUser?.Id ?? 0,
                _authContext.CurrentIpAddress,
                _authContext.CurrentDeviceInfo,
                _authContext.CurrentSessionId);

            var result = await _incidentService.CreateAsync(incident, context).ConfigureAwait(false);
            if (incident.Id == 0 && result.Id > 0)
            {
                incident.Id = result.Id;
            }

            _loadedIncident = incident;
            LoadEditor(incident);
            var record = UpsertRecord(incident);
            SelectedRecord = record;
            Mode = FormMode.View;
            StatusMessage = $"Incident {incident.Id} logged.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to add incident: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            UpdateAttachmentCommandState();
            UpdateWorkflowCommandState();
        }
    }

    private async Task ApproveAsync()
    {
        if (!CanApprove() || _loadedIncident is null)
        {
            return;
        }

        await TransitionAsync("CLASSIFIED", "Incident classified.").ConfigureAwait(false);
    }

    private async Task ExecuteAsync()
    {
        if (!CanExecute() || _loadedIncident is null)
        {
            return;
        }

        await TransitionAsync("CAPA_LINKED", "Incident linked to follow-up.", ensureLinks: true).ConfigureAwait(false);
    }

    private async Task CloseAsync()
    {
        if (!CanClose() || _loadedIncident is null)
        {
            return;
        }

        await TransitionAsync("CLOSED", "Incident closed.", ensureLinks: true, closeIncident: true).ConfigureAwait(false);
    }

    private async Task TransitionAsync(string targetStatus, string successMessage, bool ensureLinks = false, bool closeIncident = false)
    {
        try
        {
            IsBusy = true;
            var incident = Editor.ToIncident(_loadedIncident);
            incident.Status = _incidentService.NormalizeStatus(targetStatus);

            if (ensureLinks)
            {
                incident.LinkedCapaId = incident.CapaCaseId;
            }

            if (closeIncident)
            {
                incident.ClosedAt = DateTime.UtcNow;
                incident.ClosedById ??= _authContext.CurrentUser?.Id;
            }

            _incidentService.Validate(incident);

            var context = IncidentCrudContext.Create(
                _authContext.CurrentUser?.Id ?? 0,
                _authContext.CurrentIpAddress,
                _authContext.CurrentDeviceInfo,
                _authContext.CurrentSessionId);

            await _incidentService.UpdateAsync(incident, context).ConfigureAwait(false);

            _loadedIncident = incident;
            LoadEditor(incident);
            var record = UpsertRecord(incident);
            SelectedRecord = record;
            Mode = FormMode.View;
            StatusMessage = successMessage;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to update incident: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            UpdateAttachmentCommandState();
            UpdateWorkflowCommandState();
        }
    }

    private ModuleRecord UpsertRecord(Incident incident)
    {
        var record = ToRecord(incident);
        for (var i = 0; i < Records.Count; i++)
        {
            if (Records[i].Key == record.Key)
            {
                Records[i] = record;
                return record;
            }
        }

        Records.Add(record);
        return record;
    }

    private bool CanAdd()
        => !IsBusy && Mode == FormMode.Add;

    private bool CanApprove()
        => !IsBusy
           && Mode == FormMode.View
           && _loadedIncident is not null
           && string.Equals(Editor.Status, _incidentService.NormalizeStatus("INVESTIGATION"), StringComparison.OrdinalIgnoreCase);

    private bool CanExecute()
        => !IsBusy
           && Mode == FormMode.View
           && _loadedIncident is not null
           && string.Equals(Editor.Status, _incidentService.NormalizeStatus("CLASSIFIED"), StringComparison.OrdinalIgnoreCase);

    private bool CanClose()
        => !IsBusy
           && Mode == FormMode.View
           && _loadedIncident is not null
           && string.Equals(Editor.Status, _incidentService.NormalizeStatus("CAPA_LINKED"), StringComparison.OrdinalIgnoreCase);

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

        if (string.Equals(e.PropertyName, nameof(IncidentEditor.Status), StringComparison.Ordinal))
        {
            UpdateWorkflowCommandState();
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
/// <summary>
/// Represents the incident editor value.
/// </summary>

public sealed partial class IncidentEditor : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string? _type;

    [ObservableProperty]
    private string? _priority;

    [ObservableProperty]
    private DateTime _detectedAt = DateTime.UtcNow;

    [ObservableProperty]
    private DateTime? _reportedAt;

    [ObservableProperty]
    private int? _reportedById;

    [ObservableProperty]
    private int? _assignedToId;

    [ObservableProperty]
    private int? _workOrderId;

    [ObservableProperty]
    private int? _capaCaseId;

    [ObservableProperty]
    private string _status = "REPORTED";

    [ObservableProperty]
    private string? _rootCause;

    [ObservableProperty]
    private DateTime? _closedAt;

    [ObservableProperty]
    private int? _closedById;

    [ObservableProperty]
    private string? _assignedInvestigator;

    [ObservableProperty]
    private string? _classification;

    [ObservableProperty]
    private int? _linkedDeviationId;

    [ObservableProperty]
    private int? _linkedCapaId;

    [ObservableProperty]
    private string? _closureComment;

    [ObservableProperty]
    private string? _sourceIp;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private bool _isCritical;

    [ObservableProperty]
    private int _riskLevel;

    [ObservableProperty]
    private double? _anomalyScore;
    /// <summary>
    /// Executes the create empty operation.
    /// </summary>

    public static IncidentEditor CreateEmpty()
        => new()
        {
            DetectedAt = DateTime.UtcNow,
            Status = "REPORTED"
        };
    /// <summary>
    /// Executes the create for new operation.
    /// </summary>

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
    /// <summary>
    /// Executes the from incident operation.
    /// </summary>

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
    /// <summary>
    /// Executes the clone operation.
    /// </summary>

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
    /// <summary>
    /// Executes the to incident operation.
    /// </summary>

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
