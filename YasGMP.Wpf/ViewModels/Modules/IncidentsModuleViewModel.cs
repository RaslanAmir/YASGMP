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

namespace YasGMP.Wpf.ViewModels.Modules;

public sealed partial class IncidentsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "Incidents";

    private readonly IIncidentCrudService _incidentService;
    private readonly IAuthContext _authContext;
    private readonly IFilePicker _filePicker;
    private readonly IAttachmentService _attachmentService;

    private Incident? _loadedIncident;
    private IncidentEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;

    public ObservableCollection<string> StatusOptions { get; } = new(new[]
    {
        "REPORTED",
        "INVESTIGATION",
        "CLASSIFIED",
        "DEVIATION_LINKED",
        "CAPA_LINKED",
        "CLOSED"
    });

    public ObservableCollection<string> PriorityOptions { get; } = new(new[]
    {
        "Low",
        "Medium",
        "High",
        "Critical"
    });

    public ObservableCollection<string> TypeOptions { get; } = new(new[]
    {
        "Deviation",
        "Quality",
        "Safety",
        "Security",
        "IT",
        "Maintenance"
    });

    [ObservableProperty]
    private IncidentEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;

    public IAsyncRelayCommand AttachEvidenceCommand { get; }

    public IncidentsModuleViewModel(
        DatabaseService databaseService,
        IIncidentCrudService incidentService,
        IAuthContext authContext,
        IFilePicker filePicker,
        IAttachmentService attachmentService,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Incidents", databaseService, cflDialogService, shellInteraction, navigation)
    {
        _incidentService = incidentService ?? throw new ArgumentNullException(nameof(incidentService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentService = attachmentService ?? throw new ArgumentNullException(nameof(attachmentService));

        Editor = IncidentEditor.CreateEmpty();
        AttachEvidenceCommand = new AsyncRelayCommand(AttachEvidenceAsync, CanAttachEvidence);
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

    protected override async Task<bool> OnSaveAsync()
    {
        var context = IncidentCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId);

        var incident = Editor.ToIncident(_loadedIncident);
        incident.Status = _incidentService.NormalizeStatus(incident.Status);

        if (Mode == FormMode.Add)
        {
            var id = await _incidentService.CreateAsync(incident, context).ConfigureAwait(false);
            if (incident.Id == 0 && id > 0)
            {
                incident.Id = id;
            }

            _loadedIncident = incident;
            LoadEditor(incident);
            return true;
        }

        if (Mode == FormMode.Update)
        {
            if (_loadedIncident is null)
            {
                return false;
            }

            incident.Id = _loadedIncident.Id;
            await _incidentService.UpdateAsync(incident, context).ConfigureAwait(false);
            _loadedIncident = incident;
            LoadEditor(incident);
            return true;
        }

        return false;
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

            var uploads = 0;
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

                await _attachmentService.UploadAsync(stream, request).ConfigureAwait(false);
                uploads++;
            }

            StatusMessage = uploads == 1
                ? "Attachment uploaded successfully."
                : $"Uploaded {uploads} attachments successfully.";
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
        => AttachEvidenceCommand.NotifyCanExecuteChanged();

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

    public static IncidentEditor CreateEmpty()
        => new()
        {
            DetectedAt = DateTime.UtcNow,
            Status = "REPORTED"
        };

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
