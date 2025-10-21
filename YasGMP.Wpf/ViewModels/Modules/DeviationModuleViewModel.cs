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
/// Represents the deviations module view model orchestrating deviation workflows.
/// </summary>
public sealed partial class DeviationModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>
    /// Module key used for registration and navigation.
    /// </summary>
    public const string ModuleKey = "Deviations";

    private const string CapaCflPrefix = "CAPA:";

    private readonly IDeviationCrudService _deviationService;
    private readonly IAuthContext _authContext;
    private readonly IFilePicker _filePicker;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private readonly ILocalizationService _localization;

    private Deviation? _loadedDeviation;
    private DeviationEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;

    /// <summary>
    /// Gets the localized status options.
    /// </summary>
    public ObservableCollection<string> StatusOptions { get; }

    /// <summary>
    /// Gets the localized severity options.
    /// </summary>
    public ObservableCollection<string> SeverityOptions { get; }

    [ObservableProperty]
    private DeviationEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>
    /// Command that uploads deviation evidence attachments.
    /// </summary>
    public IAsyncRelayCommand AttachEvidenceCommand { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviationModuleViewModel"/> class.
    /// </summary>
    public DeviationModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IDeviationCrudService deviationService,
        IAuthContext authContext,
        IFilePicker filePicker,
        IAttachmentWorkflowService attachmentWorkflow,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, localization.GetString("Module.Title.Deviations"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _deviationService = deviationService ?? throw new ArgumentNullException(nameof(deviationService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        Editor = DeviationEditor.CreateEmpty();

        StatusOptions = new ObservableCollection<string>(new[]
        {
            _localization.GetString("Module.Deviations.Status.Open"),
            _localization.GetString("Module.Deviations.Status.Investigation"),
            _localization.GetString("Module.Deviations.Status.CapaLinked"),
            _localization.GetString("Module.Deviations.Status.Closed")
        });

        SeverityOptions = new ObservableCollection<string>(new[]
        {
            _localization.GetString("Module.Deviations.Severity.Low"),
            _localization.GetString("Module.Deviations.Severity.Medium"),
            _localization.GetString("Module.Deviations.Severity.High"),
            _localization.GetString("Module.Deviations.Severity.Critical"),
            _localization.GetString("Module.Deviations.Severity.Gmp")
        });

        AttachEvidenceCommand = new AsyncRelayCommand(AttachEvidenceAsync, CanAttachEvidence);
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var deviations = await Database.GetAllDeviationsAsync().ConfigureAwait(false);
        foreach (var deviation in deviations)
        {
            deviation.Status = _deviationService.NormalizeStatus(deviation.Status);
        }

        return deviations.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new(
                "101",
                "Temperature excursion",
                "DEV-2024-101",
                StatusOptions[1],
                "Refrigerator deviated outside the qualified range",
                new[]
                {
                    new InspectorField("Severity", SeverityOptions[3]),
                    new InspectorField("Investigator", "QA Specialist"),
                    new InspectorField("Linked CAPA", "205"),
                    new InspectorField("Risk Score", "85")
                },
                CapaModuleViewModel.ModuleKey,
                205),
            new(
                "102",
                "Label mix-up",
                "DEV-2024-102",
                StatusOptions[0],
                "Incorrect lot labels discovered during final packaging",
                new[]
                {
                    new InspectorField("Severity", SeverityOptions[2]),
                    new InspectorField("Investigator", "Production Lead"),
                    new InspectorField("Risk Score", "60")
                },
                null,
                null)
        };

    /// <inheritdoc />
    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedDeviation = null;
            SetEditor(DeviationEditor.CreateEmpty());
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

        var deviation = await _deviationService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (deviation is null)
        {
            StatusMessage = string.Format(CultureInfo.CurrentCulture, _localization.GetString("Module.Deviations.Status.LoadFailed"), record.Title);
            return;
        }

        deviation.Status = _deviationService.NormalizeStatus(deviation.Status);
        _loadedDeviation = deviation;
        LoadEditor(deviation);
        UpdateAttachmentCommandState();
    }

    /// <inheritdoc />
    protected override Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                SetEditor(DeviationEditor.CreateForNew(_authContext));
                if (SelectedRecord?.RelatedParameter is int relatedId && SelectedRecord.RelatedModuleKey == CapaModuleViewModel.ModuleKey)
                {
                    Editor.LinkedCapaId = relatedId;
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

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var errors = new List<string>();
        try
        {
            var deviation = Editor.ToDeviation(_loadedDeviation);
            deviation.Status = _deviationService.NormalizeStatus(deviation.Status);
            _deviationService.Validate(deviation);
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(ex.Message);
        }
        catch (Exception ex)
        {
            errors.Add(string.Format(CultureInfo.CurrentCulture, _localization.GetString("Module.Deviations.Status.ValidationFailed"), ex.Message));
        }

        return await Task.FromResult<IReadOnlyList<string>>(errors).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        if (!IsInEditMode)
        {
            StatusMessage = _localization.GetString("Module.Deviations.Status.CflUnavailable");
            return null;
        }

        try
        {
            var capaCases = await Database.GetAllCapaCasesAsync().ConfigureAwait(false);
            var items = capaCases
                .OrderByDescending(c => c.DateOpen)
                .Take(25)
                .Select(capa =>
                {
                    var code = $"CAPA-{capa.Id:D5}";
                    var label = string.IsNullOrWhiteSpace(capa.Title) ? code : $"{code} • {capa.Title}";
                    var descriptionParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(capa.Status))
                    {
                        descriptionParts.Add(capa.Status);
                    }

                    if (!string.IsNullOrWhiteSpace(capa.Priority))
                    {
                        descriptionParts.Add(string.Format(CultureInfo.CurrentCulture, _localization.GetString("Module.Deviations.Cfl.Priority"), capa.Priority));
                    }

                    return new CflItem(
                        $"{CapaCflPrefix}{capa.Id}",
                        label,
                        descriptionParts.Count > 0 ? string.Join(" • ", descriptionParts) : string.Empty);
                })
                .ToList();

            if (items.Count == 0)
            {
                StatusMessage = _localization.GetString("Module.Deviations.Status.NoCapaOptions");
                return null;
            }

            return new CflRequest(_localization.GetString("Module.Deviations.Cfl.Title"), items);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(CultureInfo.CurrentCulture, _localization.GetString("Module.Deviations.Status.CflFailed"), ex.Message);
            return null;
        }
    }

    /// <inheritdoc />
    protected override Task OnCflSelectionAsync(CflResult result)
    {
        if (!IsInEditMode)
        {
            return Task.CompletedTask;
        }

        var key = result.Selected.Key ?? string.Empty;
        if (key.StartsWith(CapaCflPrefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(key.AsSpan(CapaCflPrefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out var capaId))
        {
            Editor.LinkedCapaId = capaId;
            StatusMessage = string.Format(CultureInfo.CurrentCulture, _localization.GetString("Module.Deviations.Status.CapaLinked"), result.Selected.Label);
            MarkDirty();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task<bool> OnSaveAsync()
    {
        var deviation = Editor.ToDeviation(_loadedDeviation);
        deviation.Status = _deviationService.NormalizeStatus(deviation.Status);

        if (Mode == FormMode.Update && _loadedDeviation is null)
        {
            StatusMessage = _localization.GetString("Module.Deviations.Status.SelectBeforeSave");
            return false;
        }

        var recordId = Mode == FormMode.Update ? _loadedDeviation!.Id : 0;

        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("deviations", recordId))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(CultureInfo.CurrentCulture, _localization.GetString("Module.Deviations.Status.SignatureFailed"), ex.Message);
            return false;
        }

        if (signatureResult is null)
        {
            StatusMessage = _localization.GetString("Module.Deviations.Status.SignatureCancelled");
            return false;
        }

        if (signatureResult.Signature is null)
        {
            StatusMessage = _localization.GetString("Module.Deviations.Status.SignatureMissing");
            return false;
        }

        deviation.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;
        deviation.LastModified = DateTime.UtcNow;
        deviation.LastModifiedById = _authContext.CurrentUser?.Id;
        deviation.SourceIp = _authContext.CurrentIpAddress ?? deviation.SourceIp ?? string.Empty;

        var context = DeviationCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress ?? string.Empty,
            _authContext.CurrentDeviceInfo ?? string.Empty,
            _authContext.CurrentSessionId,
            signatureResult);

        Deviation persisted;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _deviationService.CreateAsync(deviation, context).ConfigureAwait(false);
                if (deviation.Id == 0 && saveResult.Id > 0)
                {
                    deviation.Id = saveResult.Id;
                }

                persisted = deviation;
            }
            else if (Mode == FormMode.Update)
            {
                deviation.Id = _loadedDeviation!.Id;
                saveResult = await _deviationService.UpdateAsync(deviation, context).ConfigureAwait(false);
                persisted = deviation;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, _localization.GetString("Module.Deviations.Status.SaveFailed"), ex.Message), ex);
        }

        _loadedDeviation = deviation;
        LoadEditor(deviation);

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "deviations",
            recordId: persisted.Id,
            metadata: saveResult.SignatureMetadata,
            fallbackSignatureHash: persisted.DigitalSignature,
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
            StatusMessage = string.Format(CultureInfo.CurrentCulture, _localization.GetString("Module.Deviations.Status.SignaturePersistFailed"), ex.Message);
            Mode = FormMode.Update;
            return false;
        }

        StatusMessage = string.Format(CultureInfo.CurrentCulture, _localization.GetString("Module.Deviations.Status.SignatureCaptured"), signatureResult.ReasonDisplay);
        return true;
    }

    /// <inheritdoc />
    protected override void OnCancel()
    {
        if (Mode == FormMode.Add)
        {
            if (_loadedDeviation is not null)
            {
                LoadEditor(_loadedDeviation);
            }
            else
            {
                SetEditor(DeviationEditor.CreateEmpty());
            }
        }
        else if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }
    }

    /// <inheritdoc />
    protected override bool MatchesSearch(ModuleRecord record, string searchText)
    {
        if (base.MatchesSearch(record, searchText))
        {
            return true;
        }

        return record.InspectorFields.Any(field => field.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    private async Task AttachEvidenceAsync()
    {
        if (_loadedDeviation is null || _loadedDeviation.Id <= 0)
        {
            return;
        }

        try
        {
            IsBusy = true;
            UpdateAttachmentCommandState();

            var files = await _filePicker
                .PickFilesAsync(new FilePickerRequest(true, null, _localization.GetString("Module.Deviations.Attachments.DialogTitle")))
                .ConfigureAwait(false);

            if (files.Count == 0)
            {
                StatusMessage = _localization.GetString("Module.Deviations.Status.NoFilesSelected");
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
                    EntityType = "deviations",
                    EntityId = _loadedDeviation.Id,
                    UploadedById = uploadedBy,
                    Reason = $"deviation:{_loadedDeviation.Id}",
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
            StatusMessage = string.Format(CultureInfo.CurrentCulture, _localization.GetString("Module.Deviations.Status.AttachmentFailed"), ex.Message);
        }
        finally
        {
            IsBusy = false;
            UpdateAttachmentCommandState();
        }
    }

    private bool CanAttachEvidence()
        => !IsBusy && _loadedDeviation is { Id: > 0 } && Mode is FormMode.View or FormMode.Update;

    private void LoadEditor(Deviation deviation)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = DeviationEditor.FromDeviation(deviation, _deviationService.NormalizeStatus);
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private void SetEditor(DeviationEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private void UpdateAttachmentCommandState()
        => AttachEvidenceCommand.NotifyCanExecuteChanged();

    partial void OnEditorChanging(DeviationEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(DeviationEditor value)
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

    private static ModuleRecord ToRecord(Deviation deviation)
    {
        var fields = new List<InspectorField>
        {
            new("Severity", string.IsNullOrWhiteSpace(deviation.Severity) ? "-" : deviation.Severity),
            new("Investigator", string.IsNullOrWhiteSpace(deviation.AssignedInvestigatorName) ? "-" : deviation.AssignedInvestigatorName),
            new("Risk Score", deviation.RiskScore.ToString(CultureInfo.InvariantCulture))
        };

        if (deviation.LinkedCapaId.HasValue)
        {
            fields.Add(new InspectorField("Linked CAPA", deviation.LinkedCapaId.Value.ToString(CultureInfo.InvariantCulture)));
        }

        string? relatedModule = null;
        object? relatedParameter = null;
        if (deviation.LinkedCapaId.HasValue)
        {
            relatedModule = CapaModuleViewModel.ModuleKey;
            relatedParameter = deviation.LinkedCapaId.Value;
        }

        return new ModuleRecord(
            deviation.Id.ToString(CultureInfo.InvariantCulture),
            string.IsNullOrWhiteSpace(deviation.Title) ? "-" : deviation.Title,
            string.IsNullOrWhiteSpace(deviation.Code) ? deviation.Id.ToString(CultureInfo.InvariantCulture) : deviation.Code,
            deviation.Status,
            string.IsNullOrWhiteSpace(deviation.Description) ? string.Empty : deviation.Description,
            fields,
            relatedModule,
            relatedParameter);
    }
}

/// <summary>
/// Editor abstraction backing the deviation detail pane.
/// </summary>
public sealed partial class DeviationEditor : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string? _code;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private DateTime? _reportedAt = DateTime.UtcNow;

    [ObservableProperty]
    private int? _reportedById;

    [ObservableProperty]
    private string _severity = "LOW";

    [ObservableProperty]
    private bool _isCritical;

    [ObservableProperty]
    private string _status = "OPEN";

    [ObservableProperty]
    private int? _assignedInvestigatorId;

    [ObservableProperty]
    private string? _assignedInvestigatorName;

    [ObservableProperty]
    private DateTime? _investigationStartedAt;

    [ObservableProperty]
    private string? _rootCause;

    [ObservableProperty]
    private int? _linkedCapaId;

    [ObservableProperty]
    private string? _closureComment;

    [ObservableProperty]
    private DateTime? _closedAt;

    [ObservableProperty]
    private int _riskScore;

    [ObservableProperty]
    private double? _anomalyScore;

    [ObservableProperty]
    private string? _sourceIp;

    [ObservableProperty]
    private string? _auditNote;

    /// <summary>
    /// Creates an empty editor instance with default values.
    /// </summary>
    public static DeviationEditor CreateEmpty()
        => new()
        {
            Status = "OPEN",
            ReportedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Creates a new editor pre-populated for add mode.
    /// </summary>
    public static DeviationEditor CreateForNew(IAuthContext authContext)
    {
        if (authContext is null)
        {
            throw new ArgumentNullException(nameof(authContext));
        }

        return new DeviationEditor
        {
            Status = "OPEN",
            ReportedAt = DateTime.UtcNow,
            ReportedById = authContext.CurrentUser?.Id,
            SourceIp = authContext.CurrentIpAddress
        };
    }

    /// <summary>
    /// Creates an editor snapshot from a persisted deviation.
    /// </summary>
    public static DeviationEditor FromDeviation(Deviation deviation, Func<string?, string> normalizeStatus)
    {
        if (deviation is null)
        {
            throw new ArgumentNullException(nameof(deviation));
        }

        if (normalizeStatus is null)
        {
            throw new ArgumentNullException(nameof(normalizeStatus));
        }

        return new DeviationEditor
        {
            Id = deviation.Id,
            Code = deviation.Code,
            Title = deviation.Title,
            Description = deviation.Description,
            ReportedAt = deviation.ReportedAt,
            ReportedById = deviation.ReportedById,
            Severity = string.IsNullOrWhiteSpace(deviation.Severity) ? "LOW" : deviation.Severity,
            IsCritical = deviation.IsCritical,
            Status = normalizeStatus(deviation.Status),
            AssignedInvestigatorId = deviation.AssignedInvestigatorId,
            AssignedInvestigatorName = deviation.AssignedInvestigatorName,
            InvestigationStartedAt = deviation.InvestigationStartedAt,
            RootCause = deviation.RootCause,
            LinkedCapaId = deviation.LinkedCapaId,
            ClosureComment = deviation.ClosureComment,
            ClosedAt = deviation.ClosedAt,
            RiskScore = deviation.RiskScore,
            AnomalyScore = deviation.AnomalyScore,
            SourceIp = deviation.SourceIp,
            AuditNote = deviation.AuditNote
        };
    }

    /// <summary>
    /// Clones the editor to a new instance.
    /// </summary>
    public DeviationEditor Clone()
        => new()
        {
            Id = Id,
            Code = Code,
            Title = Title,
            Description = Description,
            ReportedAt = ReportedAt,
            ReportedById = ReportedById,
            Severity = Severity,
            IsCritical = IsCritical,
            Status = Status,
            AssignedInvestigatorId = AssignedInvestigatorId,
            AssignedInvestigatorName = AssignedInvestigatorName,
            InvestigationStartedAt = InvestigationStartedAt,
            RootCause = RootCause,
            LinkedCapaId = LinkedCapaId,
            ClosureComment = ClosureComment,
            ClosedAt = ClosedAt,
            RiskScore = RiskScore,
            AnomalyScore = AnomalyScore,
            SourceIp = SourceIp,
            AuditNote = AuditNote
        };

    /// <summary>
    /// Converts the editor state into a domain deviation instance.
    /// </summary>
    public Deviation ToDeviation(Deviation? existing)
    {
        var deviation = existing is null ? new Deviation() : new Deviation { Id = existing.Id };

        deviation.Id = Id > 0 ? Id : deviation.Id;
        deviation.Code = string.IsNullOrWhiteSpace(Code) ? null : Code.Trim();
        deviation.Title = Title?.Trim() ?? string.Empty;
        deviation.Description = Description?.Trim() ?? string.Empty;
        deviation.ReportedAt = ReportedAt;
        deviation.ReportedById = ReportedById;
        deviation.Severity = string.IsNullOrWhiteSpace(Severity) ? "LOW" : Severity.Trim();
        deviation.IsCritical = IsCritical;
        deviation.Status = string.IsNullOrWhiteSpace(Status) ? "OPEN" : Status.Trim();
        deviation.AssignedInvestigatorId = AssignedInvestigatorId;
        deviation.AssignedInvestigatorName = string.IsNullOrWhiteSpace(AssignedInvestigatorName) ? null : AssignedInvestigatorName.Trim();
        deviation.InvestigationStartedAt = InvestigationStartedAt;
        deviation.RootCause = string.IsNullOrWhiteSpace(RootCause) ? null : RootCause.Trim();
        deviation.LinkedCapaId = LinkedCapaId;
        deviation.ClosureComment = string.IsNullOrWhiteSpace(ClosureComment) ? null : ClosureComment.Trim();
        deviation.ClosedAt = ClosedAt;
        deviation.RiskScore = RiskScore;
        deviation.AnomalyScore = AnomalyScore;
        deviation.SourceIp = string.IsNullOrWhiteSpace(SourceIp) ? null : SourceIp.Trim();
        deviation.AuditNote = string.IsNullOrWhiteSpace(AuditNote) ? null : AuditNote.Trim();

        return deviation;
    }
}
