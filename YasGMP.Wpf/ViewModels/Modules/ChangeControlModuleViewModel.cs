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

/// <summary>Hosts change-control workflows inside the WPF shell with SAP B1 tooling.</summary>
/// <remarks>
/// Form Modes: Find filters change requests, Add seeds <see cref="ChangeControlEditor.CreateEmpty"/>, View keeps records read-only, and Update unlocks editing plus attachment/e-signature flows.
/// Audit &amp; Logging: Defers persistence (and corresponding audit hashing) to <see cref="IChangeControlCrudService"/> with mandatory electronic signatures before save and file retention via the attachment workflow.
/// Localization: Currently relies on inline labels (`"Change Control"`, status prompts, error/status messages) pending resource keys.
/// Navigation: ModuleKey `ChangeControl` registers the document, `CreateCflRequestAsync` powers CFL pickers, and status strings keep the shell status bar and Golden Arrow navigation aligned with change request transitions.
/// </remarks>
public sealed partial class ChangeControlModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Shell registration key that binds Change Control into the docking layout.</summary>
    /// <remarks>Execution: Resolved when the shell composes modules and persists layouts. Form Mode: Identifier applies across Find/Add/View/Update. Localization: Currently paired with the inline caption "Change Control" until `Modules_ChangeControl_Title` is introduced.</remarks>
    public new const string ModuleKey = "ChangeControl";

    private readonly IChangeControlCrudService _changeControlService;
    private readonly IAuthContext _authContext;
    private readonly IFilePicker _filePicker;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;

    private ChangeControl? _loadedEntity;
    private ChangeControlEditor? _snapshot;
    private bool _suppressDirtyNotifications;

    /// <summary>Initializes the Change Control module view model with domain and shell services.</summary>
    /// <remarks>Execution: Invoked when the shell activates the module or Golden Arrow navigation materializes it. Form Mode: Seeds Find/View immediately while deferring Add/Update wiring to later transitions. Localization: Relies on inline strings for tab titles and prompts until module resources exist.</remarks>
    public ChangeControlModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IChangeControlCrudService changeControlService,
        IAuthContext authContext,
        IFilePicker filePicker,
        IAttachmentWorkflowService attachmentWorkflow,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Change Control", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        _changeControlService = changeControlService ?? throw new ArgumentNullException(nameof(changeControlService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));

        StatusOptions = Enum.GetNames(typeof(ChangeControlStatus));
        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
        SummarizeWithAiCommand = new RelayCommand(OpenAiSummary);
        Toolbar.Add(new ModuleToolbarCommand("Summarize (AI)", SummarizeWithAiCommand));
        SetEditor(ChangeControlEditor.CreateEmpty());
    }

    [ObservableProperty]
    private ChangeControlEditor _editor = null!;

    /// <summary>Generated property exposing the is editor enabled for the Change Control module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_ChangeControl_IsEditorEnabled` resources are available.</remarks>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Collection presenting the status options for the Change Control document host.</summary>
    /// <remarks>Execution: Populated as records load or staging mutates. Form Mode: Visible in all modes with editing reserved for Add/Update. Localization: Grid headers/tooltips remain inline until `Modules_ChangeControl_Grid` resources exist.</remarks>
    public IReadOnlyList<string> StatusOptions { get; }

    /// <summary>Opens the AI module to summarize the selected change control.</summary>
    public IRelayCommand SummarizeWithAiCommand { get; }

    /// <summary>Command executing the attach document workflow for the Change Control module.</summary>
    /// <remarks>Execution: Invoked when the correlated ribbon or toolbar control is activated. Form Mode: Enabled only when the current mode supports the action (generally Add/Update). Localization: Uses inline button labels/tooltips until `Ribbon_ChangeControl_AttachDocument` resources are authored.</remarks>
    public IAsyncRelayCommand AttachDocumentCommand { get; }

    /// <summary>Loads Change Control records from domain services.</summary>
    /// <remarks>Execution: Triggered by Find refreshes and shell activation. Form Mode: Supplies data for Find/View while Add/Update reuse cached results. Localization: Emits inline status strings pending `Status_ChangeControl_Loaded` resources.</remarks>
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var changeControls = await _changeControlService.GetAllAsync().ConfigureAwait(false);
        foreach (var cc in changeControls)
        {
            cc.StatusRaw = _changeControlService.NormalizeStatus(cc.StatusRaw);
        }

        return changeControls.Select(ToRecord).ToList();
    }

    /// <summary>Provides design-time sample data for the Change Control designer experience.</summary>
    /// <remarks>Execution: Invoked only by design-mode checks to support Blend/preview tooling. Form Mode: Mirrors Find mode to preview list layouts. Localization: Sample literals remain inline for clarity.</remarks>
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var now = DateTime.Now;
        var demo = new List<ChangeControl>
        {
            new()
            {
                Id = 401,
                Code = "CC-2024-001",
                Title = "Formulation update",
                StatusRaw = ChangeControlStatus.UnderReview.ToString(),
                Description = "Change request to introduce new excipient supplier.",
                DateRequested = now.AddDays(-7)
            },
            new()
            {
                Id = 402,
                Code = "CC-2024-002",
                Title = "Clean room HVAC tweak",
                StatusRaw = ChangeControlStatus.Draft.ToString(),
                Description = "Adjust HVAC schedule for energy savings.",
                DateRequested = now.AddDays(-2)
            }
        };

        return demo.Select(ToRecord).ToList();
    }

    /// <summary>Loads editor payloads for the selected Change Control record.</summary>
    /// <remarks>Execution: Triggered when document tabs change or shell routing targets `ModuleKey` "ChangeControl". Form Mode: Honors Add/Update safeguards to avoid overwriting dirty state. Localization: Inline status/error strings remain until `Status_ChangeControl` resources are available.</remarks>
    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedEntity = null;
            SetEditor(ChangeControlEditor.CreateEmpty());
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

        var entity = await _changeControlService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (entity is null)
        {
            StatusMessage = $"Unable to load {record.Title}.";
            return;
        }

        entity.StatusRaw = _changeControlService.NormalizeStatus(entity.StatusRaw);
        _loadedEntity = entity;
        LoadEditor(entity);
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
                _loadedEntity = null;
                SetEditor(ChangeControlEditor.CreateForNew(_authContext));
                break;
            case FormMode.Update:
                _snapshot = Editor.Clone();
                break;
            case FormMode.View:
                _snapshot = null;
                if (_loadedEntity is not null)
                {
                    LoadEditor(_loadedEntity);
                }
                break;
        }

        UpdateAttachmentCommandState();
        return Task.CompletedTask;
    }

    private void OpenAiSummary()
    {
        if (SelectedRecord is null && _loadedEntity is null)
        {
            StatusMessage = "Select a change control record to summarize.";
            return;
        }

        var c = _loadedEntity;
        string prompt;
        if (c is null)
        {
            prompt = $"Summarize change control: {SelectedRecord?.Title}. Provide scope, risk, approvals and next steps in <= 8 bullets.";
        }
        else
        {
            prompt = $"Summarize this change control (<= 8 bullets). Code={c.Code}; Status={c.StatusRaw}; Title={c.Title}; Opened={c.CreatedAt:yyyy-MM-dd}; Closed={c.UpdatedAt:yyyy-MM-dd}; Risk={c.Status}; Owner={(c.AssignedToId ?? c.RequestedById)}.";
        }

        var shell = YasGMP.Common.ServiceLocator.GetRequiredService<IShellInteractionService>();
        var doc = shell.OpenModule(AiModuleViewModel.ModuleKey, $"prompt:{prompt}");
        shell.Activate(doc);
    }

    /// <summary>Validates the current editor payload before persistence.</summary>
    /// <remarks>Execution: Invoked immediately prior to OK/Update actions. Form Mode: Only Add/Update trigger validation. Localization: Error messages flow from inline literals until validation resources are added.</remarks>
    protected override Task<IReadOnlyList<string>> ValidateAsync()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Editor.Title))
        {
            errors.Add("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.Code))
        {
            errors.Add("Code is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.Status))
        {
            errors.Add("Status is required.");
        }

        if (Editor.Status.Equals(ChangeControlStatus.Cancelled.ToString(), StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(Editor.Description))
        {
            errors.Add("Provide a reason when cancelling a change control.");
        }

        try
        {
            var candidate = Editor.ToEntity(_loadedEntity ?? new ChangeControl());
            candidate.StatusRaw = _changeControlService.NormalizeStatus(Editor.Status);
            _changeControlService.Validate(candidate);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
        }

        return Task.FromResult<IReadOnlyList<string>>(errors);
    }

    /// <summary>Persists the current record and coordinates signatures, attachments, and audits.</summary>
    /// <remarks>Execution: Runs after validation when OK/Update is confirmed. Form Mode: Exclusive to Add/Update operations. Localization: Success/failure messaging remains inline pending dedicated resources.</remarks>
    protected override async Task<bool> OnSaveAsync()
    {
        var entity = Editor.ToEntity(_loadedEntity ?? new ChangeControl());
        entity.StatusRaw = _changeControlService.NormalizeStatus(Editor.Status);

        if (Mode == FormMode.Update && _loadedEntity is null)
        {
            StatusMessage = "Select a change control before saving.";
            return false;
        }

        if (Mode == FormMode.Add && string.IsNullOrWhiteSpace(entity.Code))
        {
            entity.Code = $"CC-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        var recordId = Mode == FormMode.Update ? _loadedEntity!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("change_controls", recordId))
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

        entity.LastModified = DateTime.UtcNow;
        entity.LastModifiedById = _authContext.CurrentUser?.Id;
        entity.UpdatedAt = DateTime.UtcNow;

        var context = ChangeControlCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        ChangeControl adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _changeControlService.CreateAsync(entity, context).ConfigureAwait(false);
                entity.Id = saveResult.Id;
                Records.Add(ToRecord(entity));
                adapterResult = entity;
            }
            else if (Mode == FormMode.Update && _loadedEntity is not null)
            {
                entity.Id = _loadedEntity.Id;
                saveResult = await _changeControlService.UpdateAsync(entity, context).ConfigureAwait(false);
                adapterResult = entity;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist change control: {ex.Message}", ex);
        }

        _loadedEntity = entity;
        LoadEditor(entity);
        UpdateAttachmentCommandState();

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "change_controls",
            recordId: adapterResult.Id,
            metadata: saveResult.SignatureMetadata,
            fallbackSignatureHash: context.SignatureHash ?? signatureResult.Signature.SignatureHash,
            fallbackMethod: context.SignatureMethod,
            fallbackStatus: context.SignatureStatus,
            fallbackNote: context.SignatureNote,
            signedAt: signatureResult.Signature.SignedAt,
            fallbackDeviceInfo: context.DeviceInfo,
            fallbackIpAddress: context.IpAddress,
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
            _loadedEntity = null;
            SetEditor(ChangeControlEditor.CreateEmpty());
        }
        else if (Mode == FormMode.Update)
        {
            if (_snapshot is not null)
            {
                SetEditor(_snapshot.Clone());
            }
            else if (_loadedEntity is not null)
            {
                LoadEditor(_loadedEntity);
            }
        }

        UpdateAttachmentCommandState();
    }

    /// <summary>Builds the Choose-From-List request used for Golden Arrow navigation.</summary>
    /// <remarks>Execution: Called when the shell launches CFL dialogs, routing via `ModuleKey` "ChangeControl". Form Mode: Provides lookup data irrespective of current mode. Localization: Dialog titles and descriptions use inline strings until `CFL_ChangeControl` resources exist.</remarks>
    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var changeControls = await _changeControlService.GetAllAsync().ConfigureAwait(false);
        var items = changeControls
            .Select(cc =>
            {
                var label = string.IsNullOrWhiteSpace(cc.Title) ? cc.Code ?? cc.Id.ToString(CultureInfo.InvariantCulture) : cc.Title!;
                var descriptionParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(cc.Code))
                {
                    descriptionParts.Add(cc.Code!);
                }

                if (!string.IsNullOrWhiteSpace(cc.StatusRaw))
                {
                    descriptionParts.Add(cc.StatusRaw!);
                }

                if (cc.DateRequested.HasValue)
                {
                    descriptionParts.Add(cc.DateRequested.Value.ToString("d", CultureInfo.CurrentCulture));
                }

                var description = descriptionParts.Count > 0 ? string.Join(" â€˘ ", descriptionParts) : null;
                return new CflItem(cc.Id.ToString(CultureInfo.InvariantCulture), label, description);
            })
            .ToList();

        return new CflRequest("Select Change Control", items);
    }

    /// <summary>Applies CFL selections back into the Change Control workspace.</summary>
    /// <remarks>Execution: Runs after CFL or Golden Arrow completion, updating `StatusMessage` for `ModuleKey` "ChangeControl". Form Mode: Navigates records without disturbing active edits. Localization: Status feedback uses inline phrases pending `Status_ChangeControl_Filtered`.</remarks>
    protected override Task OnCflSelectionAsync(CflResult result)
    {
        var match = Records.FirstOrDefault(r => r.Key == result.Selected.Key);
        if (match is not null)
        {
            SelectedRecord = match;
            SearchText = match.Title;
            StatusMessage = $"Loaded change control '{match.Title}'.";
        }
        else
        {
            SearchText = result.Selected.Label;
            StatusMessage = $"Filtered change controls by '{result.Selected.Label}'.";
        }

        return Task.CompletedTask;
    }

    /// <summary>Executes the matches search routine for the Change Control module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    protected override bool MatchesSearch(ModuleRecord record, string searchText)
    {
        if (base.MatchesSearch(record, searchText))
        {
            return true;
        }

        return (record.Status?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false)
               || record.InspectorFields.Any(f => f.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    private ModuleRecord ToRecord(ChangeControl changeControl)
    {
        var fields = new List<InspectorField>
        {
            new("Status", changeControl.StatusRaw ?? ChangeControlStatus.Draft.ToString()),
            new("Requested", changeControl.DateRequested?.ToString("d", CultureInfo.CurrentCulture) ?? "-"),
            new("Assigned To", changeControl.AssignedToId?.ToString(CultureInfo.InvariantCulture) ?? "-")
        };

        return new ModuleRecord(
            changeControl.Id.ToString(CultureInfo.InvariantCulture),
            string.IsNullOrWhiteSpace(changeControl.Title) ? changeControl.Code ?? changeControl.Id.ToString(CultureInfo.InvariantCulture) : changeControl.Title!,
            changeControl.Code,
            changeControl.StatusRaw,
            changeControl.Description,
            fields,
            CapaModuleViewModel.ModuleKey,
            changeControl.Id);
    }

    private void LoadEditor(ChangeControl entity)
        => SetEditor(ChangeControlEditor.FromEntity(entity));

    private void SetEditor(ChangeControlEditor editor)
    {
        if (Editor is not null)
        {
            Editor.PropertyChanged -= OnEditorPropertyChanged;
        }

        _suppressDirtyNotifications = true;
        Editor = editor;
        Editor.PropertyChanged += OnEditorPropertyChanged;
        _suppressDirtyNotifications = false;
        ResetDirty();
        UpdateAttachmentCommandState();
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressDirtyNotifications)
        {
            return;
        }

        if (Mode is FormMode.Add or FormMode.Update)
        {
            MarkDirty();
        }
    }

    private bool CanAttachDocument()
        => !IsBusy
           && !IsEditorEnabled
           && _loadedEntity is not null
           && _loadedEntity.Id > 0;

    private async Task AttachDocumentAsync()
    {
        if (_loadedEntity is null || _loadedEntity.Id <= 0)
        {
            StatusMessage = "Save the change control before adding attachments.";
            return;
        }

        try
        {
            IsBusy = true;
            var files = await _filePicker
                .PickFilesAsync(new FilePickerRequest(AllowMultiple: true, Title: $"Attach files to {_loadedEntity.Code ?? _loadedEntity.Id.ToString(CultureInfo.InvariantCulture)}"))
                .ConfigureAwait(false);

            if (files is null || files.Count == 0)
            {
                StatusMessage = "Attachment upload cancelled.";
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
                    EntityType = "change_controls",
                    EntityId = _loadedEntity.Id,
                    UploadedById = _authContext.CurrentUser?.Id,
                    Reason = $"changecontrol:{_loadedEntity.Id}",
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

    private void UpdateAttachmentCommandState()
    {
        YasGMP.Wpf.Helpers.UiCommandHelper.NotifyCanExecuteOnUi(AttachDocumentCommand);
    }

    public sealed partial class ChangeControlEditor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _code = string.Empty;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string? _description;

        [ObservableProperty]
        private string _status = ChangeControlStatus.Draft.ToString();

        [ObservableProperty]
        private int? _requestedById;

        [ObservableProperty]
        private DateTime? _dateRequested;

        [ObservableProperty]
        private int? _assignedToId;

        [ObservableProperty]
        private DateTime? _dateAssigned;

        [ObservableProperty]
        private int? _lastModifiedById;

        [ObservableProperty]
        private DateTime? _lastModified;

        [ObservableProperty]
        private DateTime? _createdAt;

        [ObservableProperty]
        private DateTime? _updatedAt;

        public static ChangeControlEditor CreateEmpty() => new();

        public static ChangeControlEditor CreateForNew(IAuthContext auth)
            => new()
            {
                Code = $"CC-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Title = string.Empty,
                Status = ChangeControlStatus.Draft.ToString(),
                RequestedById = auth.CurrentUser?.Id,
                DateRequested = DateTime.UtcNow
            };

        public static ChangeControlEditor FromEntity(ChangeControl entity)
            => new()
            {
                Id = entity.Id,
                Code = entity.Code ?? string.Empty,
                Title = entity.Title ?? string.Empty,
                Description = entity.Description,
                Status = entity.StatusRaw ?? ChangeControlStatus.Draft.ToString(),
                RequestedById = entity.RequestedById,
                DateRequested = entity.DateRequested,
                AssignedToId = entity.AssignedToId,
                DateAssigned = entity.DateAssigned,
                LastModifiedById = entity.LastModifiedById,
                LastModified = entity.LastModified,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };

        public ChangeControlEditor Clone()
            => new()
            {
                Id = Id,
                Code = Code,
                Title = Title,
                Description = Description,
                Status = Status,
                RequestedById = RequestedById,
                DateRequested = DateRequested,
                AssignedToId = AssignedToId,
                DateAssigned = DateAssigned,
                LastModifiedById = LastModifiedById,
                LastModified = LastModified,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };

        public ChangeControl ToEntity(ChangeControl? destination = null)
        {
            var entity = destination ?? new ChangeControl();
            entity.Id = Id;
            entity.Code = Code;
            entity.Title = Title;
            entity.Description = Description;
            entity.StatusRaw = Status;
            entity.RequestedById = RequestedById;
            entity.DateRequested = DateRequested;
            entity.AssignedToId = AssignedToId;
            entity.DateAssigned = DateAssigned;
            entity.LastModifiedById = LastModifiedById;
            entity.LastModified = LastModified;
            entity.CreatedAt = CreatedAt;
            entity.UpdatedAt = UpdatedAt;
            return entity;
        }
    }
}


