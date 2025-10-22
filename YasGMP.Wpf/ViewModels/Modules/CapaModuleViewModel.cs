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
/// Represents the capa module view model value.
/// </summary>

public sealed partial class CapaModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>
    /// Represents the module key value.
    /// </summary>
    public const string ModuleKey = "Capa";

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

    private IReadOnlyList<Component> _components = Array.Empty<Component>();
    private CapaCase? _loadedCapa;
    private CapaEditor? _snapshot;
    private bool _suppressDirtyNotifications;
    /// <summary>
    /// Initializes a new instance of the CapaModuleViewModel class.
    /// </summary>

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
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, localization.GetString("Module.Title.Capa"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
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
        AddCommand = new AsyncRelayCommand(AddAsync, CanAdd);
        ApproveCommand = new AsyncRelayCommand(ApproveAsync, CanApprove);
        ExecuteCommand = new AsyncRelayCommand(ExecuteAsync, CanExecute);
        CloseCommand = new AsyncRelayCommand(CloseAsync, CanClose);

        UpdateWorkflowCommandState();
    }

    [ObservableProperty]
    private CapaEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;
    /// <summary>
    /// Gets or sets the component options.
    /// </summary>

    public ObservableCollection<ComponentOption> ComponentOptions { get; }
    /// <summary>
    /// Gets or sets the status options.
    /// </summary>

    public IReadOnlyList<string> StatusOptions { get; }
    /// <summary>
    /// Gets or sets the priority options.
    /// </summary>

    public IReadOnlyList<string> PriorityOptions { get; }
    /// <summary>
    /// Gets or sets the attach document command.
    /// </summary>

    public IAsyncRelayCommand AttachDocumentCommand { get; }

    /// <summary>
    /// Gets the command that persists a new CAPA case.
    /// </summary>

    public IAsyncRelayCommand AddCommand { get; }

    /// <summary>
    /// Gets the command that advances the CAPA case into the approved stage.
    /// </summary>

    public IAsyncRelayCommand ApproveCommand { get; }

    /// <summary>
    /// Gets the command that advances the CAPA case into the executed stage.
    /// </summary>

    public IAsyncRelayCommand ExecuteCommand { get; }

    /// <summary>
    /// Gets the command that closes the CAPA case.
    /// </summary>

    public IAsyncRelayCommand CloseCommand { get; }

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

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var components = new List<Component>
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
        UpdateWorkflowCommandState();
    }

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
        UpdateWorkflowCommandState();
        return Task.CompletedTask;
    }

    protected override async Task<bool> OnSaveAsync()
    {
        if (Mode == FormMode.Update && _loadedCapa is null)
        {
            StatusMessage = "Select a CAPA case before saving.";
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
            _authContext.CurrentIpAddress,
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
            throw new InvalidOperationException($"Failed to persist CAPA case: {ex.Message}", ex);
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

        return new CflRequest("Select CAPA", items);
    }

    protected override Task OnCflSelectionAsync(CflResult result)
    {
        var match = Records.FirstOrDefault(r => r.Key == result.Selected.Key);
        if (match is not null)
        {
            SelectedRecord = match;
            SearchText = match.Title;
            StatusMessage = $"Loaded {match.Title}.";
            return Task.CompletedTask;
        }

        SearchText = result.Selected.Label;
        StatusMessage = $"Filtered CAPA cases by \"{result.Selected.Label}\".";
        RecordsView.Refresh();
        return Task.CompletedTask;
    }

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
                .PickFilesAsync(new FilePickerRequest(true, null, "Select CAPA attachment"))
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
                    EntityType = "capa_cases",
                    EntityId = _loadedCapa.Id,
                    UploadedById = uploadedBy,
                    Reason = $"capa:{_loadedCapa.Id}",
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

    private void RefreshComponentOptions(IEnumerable<Component> components)
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
            new("Component", capa.ComponentId.ToString(CultureInfo.InvariantCulture))
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
        => AttachDocumentCommand.NotifyCanExecuteChanged();

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
            var capa = Editor.ToCapaCase(null);
            capa.Status = _capaService.NormalizeStatus("ACTION_DEFINED");
            capa.Priority = _capaService.NormalizePriority(capa.Priority);
            capa.DateOpen = DateTime.UtcNow;

            _capaService.Validate(capa);

            var context = CapaCrudContext.Create(
                _authContext.CurrentUser?.Id ?? 0,
                _authContext.CurrentIpAddress,
                _authContext.CurrentDeviceInfo,
                _authContext.CurrentSessionId);

            var result = await _capaService.CreateAsync(capa, context).ConfigureAwait(false);
            if (capa.Id == 0 && result.Id > 0)
            {
                capa.Id = result.Id;
            }

            _loadedCapa = capa;
            LoadEditor(capa);
            var record = UpsertRecord(capa);
            SelectedRecord = record;
            Mode = FormMode.View;
            StatusMessage = $"CAPA {capa.Id} added.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to add CAPA: {ex.Message}";
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
        if (!CanApprove() || _loadedCapa is null)
        {
            return;
        }

        await TransitionAsync("ACTION_APPROVED", "CAPA approved.", updateCloseDate: false).ConfigureAwait(false);
    }

    private async Task ExecuteAsync()
    {
        if (!CanExecute() || _loadedCapa is null)
        {
            return;
        }

        await TransitionAsync("ACTION_EXECUTED", "CAPA executed.", updateCloseDate: false).ConfigureAwait(false);
    }

    private async Task CloseAsync()
    {
        if (!CanClose() || _loadedCapa is null)
        {
            return;
        }

        await TransitionAsync("CLOSED", "CAPA closed.", updateCloseDate: true).ConfigureAwait(false);
    }

    private async Task TransitionAsync(string targetStatus, string successMessage, bool updateCloseDate)
    {
        try
        {
            IsBusy = true;
            var capa = Editor.ToCapaCase(_loadedCapa);
            capa.Status = _capaService.NormalizeStatus(targetStatus);
            capa.Priority = _capaService.NormalizePriority(capa.Priority);

            if (updateCloseDate)
            {
                capa.DateClose = DateTime.UtcNow;
            }

            var context = CapaCrudContext.Create(
                _authContext.CurrentUser?.Id ?? 0,
                _authContext.CurrentIpAddress,
                _authContext.CurrentDeviceInfo,
                _authContext.CurrentSessionId);

            await _capaService.UpdateAsync(capa, context).ConfigureAwait(false);

            _loadedCapa = capa;
            LoadEditor(capa);
            var record = UpsertRecord(capa);
            SelectedRecord = record;
            Mode = FormMode.View;
            StatusMessage = successMessage;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to update CAPA: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            UpdateAttachmentCommandState();
            UpdateWorkflowCommandState();
        }
    }

    private ModuleRecord UpsertRecord(CapaCase capa)
    {
        var record = ToRecord(capa);
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
           && _loadedCapa is not null
           && string.Equals(Editor.Status, "ACTION_DEFINED", StringComparison.OrdinalIgnoreCase);

    private bool CanExecute()
        => !IsBusy
           && Mode == FormMode.View
           && _loadedCapa is not null
           && string.Equals(Editor.Status, "ACTION_APPROVED", StringComparison.OrdinalIgnoreCase);

    private bool CanClose()
        => !IsBusy
           && Mode == FormMode.View
           && _loadedCapa is not null
           && string.Equals(Editor.Status, "ACTION_EXECUTED", StringComparison.OrdinalIgnoreCase);

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

        if (string.Equals(e.PropertyName, nameof(CapaEditor.Status), StringComparison.Ordinal))
        {
            UpdateWorkflowCommandState();
        }

        if (IsInEditMode)
        {
            MarkDirty();
        }
    }
}
/// <summary>
/// Executes the struct operation.
/// </summary>

public readonly record struct ComponentOption(int Id, string Name)
{
    /// <summary>
    /// Executes the to string operation.
    /// </summary>
    public override string ToString() => Name;
}
/// <summary>
/// Represents the capa editor value.
/// </summary>

public sealed partial class CapaEditor : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private int _componentId;

    [ObservableProperty]
    private string _componentName = string.Empty;

    [ObservableProperty]
    private int? _assignedToId;

    [ObservableProperty]
    private string _priority = "Medium";

    [ObservableProperty]
    private string _status = "OPEN";

    [ObservableProperty]
    private DateTime _dateOpen = DateTime.UtcNow;

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
    /// <summary>
    /// Executes the create empty operation.
    /// </summary>

    public static CapaEditor CreateEmpty() => new();
    /// <summary>
    /// Executes the create for new operation.
    /// </summary>

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
    /// <summary>
    /// Executes the from capa operation.
    /// </summary>

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
    /// <summary>
    /// Executes the clone operation.
    /// </summary>

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
    /// <summary>
    /// Executes the to capa case operation.
    /// </summary>

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
