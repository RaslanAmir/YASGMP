using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Controls scheduled job definitions within the WPF shell using SAP B1 semantics.</summary>
/// <remarks>
/// Form Modes: Find surfaces job listings, Add seeds <see cref="ScheduledJobEditor.CreateEmpty"/>, View allows read-only runtime operations, and Update unlocks editing, attachment capture, and acknowledgment flows.
/// Audit &amp; Logging: Persists jobs through <see cref="IScheduledJobCrudService"/> with signature enforcement, logs execution/acknowledgement outcomes through domain services, and defers retention to the attachment workflow.
/// Localization: Inline strings such as `"Scheduled Jobs"`, `"Execute job failed"`, and status prompts remain until module-specific resource keys land.
/// Navigation: ModuleKey `Scheduling` registers the module; `ModuleRecord` entries include related module keys (e.g. Work Orders, Calibration) so Golden Arrow jumps locate targets, while status messages keep the shell informed about execution and attachment results.
/// </remarks>
public sealed partial class SchedulingModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Shell registration key that binds Scheduled Jobs into the docking layout.</summary>
    /// <remarks>Execution: Resolved when the shell composes modules and persists layouts. Form Mode: Identifier applies across Find/Add/View/Update. Localization: Currently paired with the inline caption "Scheduled Jobs" until `Modules_Scheduling_Title` is introduced.</remarks>
    public new const string ModuleKey = "Scheduling";

    private readonly IScheduledJobCrudService _scheduledJobService;
    private readonly IAuthContext _authContext;
    private readonly IFilePicker _filePicker;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;

    private ScheduledJob? _loadedJob;
    private ScheduledJobEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;

    /// <summary>Initializes the Scheduled Jobs module view model with domain and shell services.</summary>
    /// <remarks>Execution: Invoked when the shell activates the module or Golden Arrow navigation materializes it. Form Mode: Seeds Find/View immediately while deferring Add/Update wiring to later transitions. Localization: Relies on inline strings for tab titles and prompts until module resources exist.</remarks>
    public SchedulingModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IScheduledJobCrudService scheduledJobService,
        IAuthContext authContext,
        IFilePicker filePicker,
        IAttachmentWorkflowService attachmentWorkflow,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Scheduled Jobs", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        _scheduledJobService = scheduledJobService ?? throw new ArgumentNullException(nameof(scheduledJobService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));

        Editor = ScheduledJobEditor.CreateEmpty();

        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
        ExecuteJobCommand = new AsyncRelayCommand(ExecuteJobAsync, CanExecuteJob);
        AcknowledgeJobCommand = new AsyncRelayCommand(AcknowledgeJobAsync, CanAcknowledgeJob);
        SummarizeWithAiCommand = new RelayCommand(OpenAiSummary);
        Toolbar.Add(new ModuleToolbarCommand("Summarize (AI)", SummarizeWithAiCommand));
    }

    /// <summary>Generated property exposing the editor for the Scheduled Jobs module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Scheduling_Editor` resources are available.</remarks>
    [ObservableProperty]
    private ScheduledJobEditor _editor;

    /// <summary>Opens the AI module to summarize the selected scheduled job.</summary>
    public IRelayCommand SummarizeWithAiCommand { get; }

    /// <summary>Generated property exposing the is editor enabled for the Scheduled Jobs module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Scheduling_IsEditorEnabled` resources are available.</remarks>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Command executing the attach document workflow for the Scheduled Jobs module.</summary>
    /// <remarks>Execution: Invoked when the correlated ribbon or toolbar control is activated. Form Mode: Enabled only when the current mode supports the action (generally Add/Update). Localization: Uses inline button labels/tooltips until `Ribbon_Scheduling_AttachDocument` resources are authored.</remarks>
    public IAsyncRelayCommand AttachDocumentCommand { get; }

    /// <summary>Command executing the execute job workflow for the Scheduled Jobs module.</summary>
    /// <remarks>Execution: Invoked when the correlated ribbon or toolbar control is activated. Form Mode: Enabled only when the current mode supports the action (generally Add/Update). Localization: Uses inline button labels/tooltips until `Ribbon_Scheduling_ExecuteJob` resources are authored.</remarks>
    public IAsyncRelayCommand ExecuteJobCommand { get; }

    /// <summary>Command executing the acknowledge job workflow for the Scheduled Jobs module.</summary>
    /// <remarks>Execution: Invoked when the correlated ribbon or toolbar control is activated. Form Mode: Enabled only when the current mode supports the action (generally Add/Update). Localization: Uses inline button labels/tooltips until `Ribbon_Scheduling_AcknowledgeJob` resources are authored.</remarks>
    public IAsyncRelayCommand AcknowledgeJobCommand { get; }

    /// <summary>Loads Scheduled Jobs records from domain services.</summary>
    /// <remarks>Execution: Triggered by Find refreshes and shell activation. Form Mode: Supplies data for Find/View while Add/Update reuse cached results. Localization: Emits inline status strings pending `Status_Scheduling_Loaded` resources.</remarks>
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var jobs = await Database.GetAllScheduledJobsFullAsync().ConfigureAwait(false);
        return jobs.Select(ToRecord).ToList();
    }

    /// <summary>Provides design-time sample data for the Scheduled Jobs designer experience.</summary>
    /// <remarks>Execution: Invoked only by design-mode checks to support Blend/preview tooling. Form Mode: Mirrors Find mode to preview list layouts. Localization: Sample literals remain inline for clarity.</remarks>
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new(
                "1001",
                "Weekly Maintenance Digest",
                "JOB-1001",
                "scheduled",
                "Distributes upcoming work orders",
                new[]
                {
                    new InspectorField("Next Due", DateTime.Now.AddDays(1).ToString("g", CultureInfo.CurrentCulture)),
                    new InspectorField("Recurrence", "0 6 * * MON"),
                    new InspectorField("Type", "report"),
                    new InspectorField("Needs Ack", "No")
                },
                WorkOrdersModuleViewModel.ModuleKey,
                1),
            new(
                "1002",
                "Calibration Reminder Sweep",
                "JOB-1002",
                "pending_ack",
                "Emails calibration owners 14 days before due",
                new[]
                {
                    new InspectorField("Next Due", DateTime.Now.AddHours(6).ToString("g", CultureInfo.CurrentCulture)),
                    new InspectorField("Recurrence", "0 7 * * *"),
                    new InspectorField("Type", "notification"),
                    new InspectorField("Needs Ack", "Yes")
                },
                CalibrationModuleViewModel.ModuleKey,
                4)
        };

    /// <summary>Loads editor payloads for the selected Scheduled Jobs record.</summary>
    /// <remarks>Execution: Triggered when document tabs change or shell routing targets `ModuleKey` "Scheduling". Form Mode: Honors Add/Update safeguards to avoid overwriting dirty state. Localization: Inline status/error strings remain until `Status_Scheduling` resources are available.</remarks>
    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (IsInEditMode)
        {
            return;
        }

        if (record is null)
        {
            _loadedJob = null;
            SetEditor(ScheduledJobEditor.CreateEmpty());
            UpdateActionStates();
            return;
        }

        if (!int.TryParse(record.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return;
        }

        var job = await _scheduledJobService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (job is null)
        {
            StatusMessage = $"Unable to load {record.Title}.";
            return;
        }

        _loadedJob = job;
        LoadEditor(job);
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
                _loadedJob = null;
                SetEditor(ScheduledJobEditor.CreateForNew(_authContext));
                break;
            case FormMode.Update:
                _snapshot = Editor.Clone();
                break;
            case FormMode.View:
                _snapshot = null;
                if (_loadedJob is not null)
                {
                    LoadEditor(_loadedJob);
                }
                break;
        }

        UpdateActionStates();
        return Task.CompletedTask;
    }

    private void OpenAiSummary()
    {
        if (SelectedRecord is null && _loadedJob is null)
        {
            StatusMessage = "Select a job to summarize.";
            return;
        }

        var j = _loadedJob;
        string prompt = j is null
            ? $"Summarize scheduled job: {SelectedRecord?.Title}. Provide type, schedule, dependencies and next actions in <= 8 bullets."
            : $"Summarize this scheduled job (<= 8 bullets). Name={j.Name}; Type={j.JobType}; Status={j.Status}; Recurrence={j.RecurrencePattern}; NextDue={j.NextDue:yyyy-MM-dd}; RequiresAck={j.NeedsAcknowledgment}.";

        var shell = YasGMP.Common.ServiceLocator.GetRequiredService<IShellInteractionService>();
        var doc = shell.OpenModule(AiModuleViewModel.ModuleKey, $"prompt:{prompt}");
        shell.Activate(doc);
    }

    /// <summary>Validates the current editor payload before persistence.</summary>
    /// <remarks>Execution: Invoked immediately prior to OK/Update actions. Form Mode: Only Add/Update trigger validation. Localization: Error messages flow from inline literals until validation resources are added.</remarks>
    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Editor.Name))
        {
            errors.Add("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.JobType))
        {
            errors.Add("Job type is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.Status))
        {
            errors.Add("Status is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.RecurrencePattern))
        {
            errors.Add("Recurrence pattern is required.");
        }

        if (Editor.NextDue == default)
        {
            errors.Add("Next due date must be specified.");
        }

        return await Task.FromResult(errors).ConfigureAwait(false);
    }

    /// <summary>Persists the current record and coordinates signatures, attachments, and audits.</summary>
    /// <remarks>Execution: Runs after validation when OK/Update is confirmed. Form Mode: Exclusive to Add/Update operations. Localization: Success/failure messaging remains inline pending dedicated resources.</remarks>
    protected override async Task<bool> OnSaveAsync()
    {
        if (Mode == FormMode.Update && _loadedJob is null)
        {
            StatusMessage = "Select a scheduled job before saving.";
            return false;
        }

        var baseContext = CreateContext();
        var entity = Editor.ToEntity(_loadedJob);
        entity.LastModified = DateTime.UtcNow;
        entity.LastModifiedById = baseContext.UserId;
        entity.DeviceInfo = baseContext.DeviceInfo;
        entity.SessionId = baseContext.SessionId ?? string.Empty;
        entity.IpAddress = baseContext.Ip;
        entity.CreatedById ??= baseContext.UserId;
        if (string.IsNullOrWhiteSpace(entity.CreatedBy))
        {
            entity.CreatedBy = baseContext.UserName;
        }

        _scheduledJobService.Validate(entity);

        var recordId = Mode == FormMode.Update ? _loadedJob!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("scheduled_jobs", recordId))
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

        entity.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;

        var context = ScheduledJobCrudContext.Create(
            baseContext.UserId,
            baseContext.UserName,
            baseContext.Ip,
            baseContext.DeviceInfo,
            baseContext.SessionId,
            signatureResult);

        ScheduledJob adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _scheduledJobService.CreateAsync(entity, context).ConfigureAwait(false);
                entity.Id = saveResult.Id;
                adapterResult = entity;
            }
            else if (Mode == FormMode.Update)
            {
                saveResult = await _scheduledJobService.UpdateAsync(entity, context).ConfigureAwait(false);
                adapterResult = entity;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist scheduled job: {ex.Message}", ex);
        }

        _loadedJob = entity;
        LoadEditor(entity);
        UpdateActionStates();

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "scheduled_jobs",
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
            if (_loadedJob is not null)
            {
                LoadEditor(_loadedJob);
            }
            else
            {
                SetEditor(ScheduledJobEditor.CreateEmpty());
            }
        }
        else if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }

        UpdateActionStates();
    }

    partial void OnEditorChanging(ScheduledJobEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(ScheduledJobEditor value)
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

        if (Mode is FormMode.Add or FormMode.Update)
        {
            MarkDirty();
        }
    }

    private ScheduledJobCrudContext CreateContext()
    {
        var user = _authContext.CurrentUser;
        var userId = user?.Id ?? 1;
        var displayName = !string.IsNullOrWhiteSpace(user?.FullName)
            ? user!.FullName!
            : !string.IsNullOrWhiteSpace(user?.Username)
                ? user!.Username!
                : $"user:{userId}";

        return ScheduledJobCrudContext.Create(
            userId,
            displayName,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId);
    }

    private bool CanAttachDocument()
        => !IsBusy
           && !IsInEditMode
           && _loadedJob is { Id: > 0 };

    private async Task AttachDocumentAsync()
    {
        if (_loadedJob is not { Id: > 0 })
        {
            StatusMessage = "Save the scheduled job before adding attachments.";
            return;
        }

        try
        {
            IsBusy = true;
            var files = await _filePicker
                .PickFilesAsync(new FilePickerRequest(AllowMultiple: true, Title: $"Attach files to {_loadedJob.Name}"), CancellationToken.None)
                .ConfigureAwait(false);

            if (files is null || files.Count == 0)
            {
                StatusMessage = "Attachment upload cancelled.";
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
                    EntityType = "scheduled_jobs",
                    EntityId = _loadedJob.Id,
                    UploadedById = uploadedBy,
                    Reason = $"scheduledjob:{_loadedJob.Id}",
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
            UpdateActionStates();
        }
    }

    private bool CanExecuteJob()
        => !IsBusy
           && !IsInEditMode
           && _loadedJob is { Id: > 0 } job
           && string.Equals(job.Status, "scheduled", StringComparison.OrdinalIgnoreCase);

    private async Task ExecuteJobAsync()
    {
        if (_loadedJob is not { Id: > 0 } job || IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var context = CreateContext();
            await _scheduledJobService.ExecuteAsync(job.Id, context).ConfigureAwait(false);
            StatusMessage = $"Execution triggered for {job.Name}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to execute {job.Name}: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            await RefreshAsync().ConfigureAwait(false);
            UpdateActionStates();
        }
    }

    private bool CanAcknowledgeJob()
        => !IsBusy
           && !IsInEditMode
           && _loadedJob is { Id: > 0 } job
           && job.NeedsAcknowledgment
           && string.Equals(job.Status, "pending_ack", StringComparison.OrdinalIgnoreCase);

    private async Task AcknowledgeJobAsync()
    {
        if (_loadedJob is not { Id: > 0 } job || IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var context = CreateContext();
            await _scheduledJobService.AcknowledgeAsync(job.Id, context).ConfigureAwait(false);
            StatusMessage = $"Acknowledged {job.Name}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to acknowledge {job.Name}: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            await RefreshAsync().ConfigureAwait(false);
            UpdateActionStates();
        }
    }

    private void LoadEditor(ScheduledJob entity)
    {
        _suppressEditorDirtyNotifications = true;
        SetEditor(ScheduledJobEditor.FromEntity(entity));
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateActionStates();
    }

    private void SetEditor(ScheduledJobEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateActionStates();
    }

    private void UpdateActionStates()
    {
        YasGMP.Wpf.Helpers.UiCommandHelper.NotifyManyOnUi(
            AttachDocumentCommand,
            ExecuteJobCommand,
            AcknowledgeJobCommand);
    }

    private static ModuleRecord ToRecord(ScheduledJob job)
    {
        var fields = new List<InspectorField>
        {
            new("Next Due", job.NextDue.ToString("g", CultureInfo.CurrentCulture)),
            new("Recurrence", string.IsNullOrWhiteSpace(job.RecurrencePattern) ? "-" : job.RecurrencePattern),
            new("Type", job.JobType),
            new("Needs Ack", job.NeedsAcknowledgment ? "Yes" : "No")
        };

        var relatedModule = job.EntityType?.ToLowerInvariant() switch
        {
            "workorders" or "work_orders" => WorkOrdersModuleViewModel.ModuleKey,
            "assets" or "machines" => AssetsModuleViewModel.ModuleKey,
            "calibration" or "calibrations" => CalibrationModuleViewModel.ModuleKey,
            _ => null
        };

        return new ModuleRecord(
            job.Id.ToString(CultureInfo.InvariantCulture),
            string.IsNullOrWhiteSpace(job.Name) ? $"Job {job.Id}" : job.Name,
            job.Name,
            job.Status,
            job.Comment,
            fields,
            relatedModule,
            job.EntityId);
    }

    public sealed partial class ScheduledJobEditor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _jobType = string.Empty;

        [ObservableProperty]
        private string _status = "scheduled";

        [ObservableProperty]
        private DateTime _nextDue = DateTime.UtcNow;

        [ObservableProperty]
        private string _recurrencePattern = string.Empty;

        [ObservableProperty]
        private string? _cronExpression;

        [ObservableProperty]
        private string _entityType = string.Empty;

        [ObservableProperty]
        private int? _entityId;

        [ObservableProperty]
        private string _comment = string.Empty;

        [ObservableProperty]
        private bool _isCritical;

        [ObservableProperty]
        private bool _needsAcknowledgment;

        [ObservableProperty]
        private bool _alertOnFailure = true;

        [ObservableProperty]
        private string _escalationNote = string.Empty;

        [ObservableProperty]
        private int _retries;

        [ObservableProperty]
        private int _maxRetries = 3;

        [ObservableProperty]
        private string _lastResult = string.Empty;

        [ObservableProperty]
        private string _lastError = string.Empty;

        [ObservableProperty]
        private DateTime? _lastExecuted;

        [ObservableProperty]
        private string _extraParams = string.Empty;

        [ObservableProperty]
        private int? _createdById;

        [ObservableProperty]
        private string _createdBy = string.Empty;

        [ObservableProperty]
        private int? _lastModifiedById;

        [ObservableProperty]
        private string _deviceInfo = string.Empty;

        [ObservableProperty]
        private string _sessionId = string.Empty;

        [ObservableProperty]
        private string _ipAddress = string.Empty;

        public static ScheduledJobEditor CreateEmpty() => new();

        public static ScheduledJobEditor CreateForNew(IAuthContext authContext)
        {
            var userId = authContext.CurrentUser?.Id ?? 1;
            var userName = !string.IsNullOrWhiteSpace(authContext.CurrentUser?.FullName)
                ? authContext.CurrentUser!.FullName!
                : authContext.CurrentUser?.Username ?? $"user:{userId}";

            return new ScheduledJobEditor
            {
                JobType = "maintenance",
                Status = "scheduled",
                NextDue = DateTime.UtcNow.AddDays(7),
                RecurrencePattern = "0 6 * * MON",
                CreatedById = userId,
                CreatedBy = userName,
                DeviceInfo = authContext.CurrentDeviceInfo,
                SessionId = authContext.CurrentSessionId,
                IpAddress = authContext.CurrentIpAddress
            };
        }

        public static ScheduledJobEditor FromEntity(ScheduledJob entity)
        {
            return new ScheduledJobEditor
            {
                Id = entity.Id,
                Name = entity.Name,
                JobType = entity.JobType,
                Status = entity.Status,
                NextDue = entity.NextDue == default ? DateTime.UtcNow : entity.NextDue,
                RecurrencePattern = entity.RecurrencePattern,
                CronExpression = entity.CronExpression,
                EntityType = entity.EntityType ?? string.Empty,
                EntityId = entity.EntityId,
                Comment = entity.Comment ?? string.Empty,
                IsCritical = entity.IsCritical,
                NeedsAcknowledgment = entity.NeedsAcknowledgment,
                AlertOnFailure = entity.AlertOnFailure,
                EscalationNote = entity.EscalationNote ?? string.Empty,
                Retries = entity.Retries,
                MaxRetries = entity.MaxRetries,
                LastResult = entity.LastResult ?? string.Empty,
                LastError = entity.LastError ?? string.Empty,
                LastExecuted = entity.LastExecuted,
                ExtraParams = entity.ExtraParams ?? string.Empty,
                CreatedById = entity.CreatedById,
                CreatedBy = entity.CreatedBy ?? string.Empty,
                LastModifiedById = entity.LastModifiedById,
                DeviceInfo = entity.DeviceInfo ?? string.Empty,
                SessionId = entity.SessionId ?? string.Empty,
                IpAddress = entity.IpAddress ?? string.Empty
            };
        }

        public ScheduledJobEditor Clone()
            => new()
            {
                Id = Id,
                Name = Name,
                JobType = JobType,
                Status = Status,
                NextDue = NextDue,
                RecurrencePattern = RecurrencePattern,
                CronExpression = CronExpression,
                EntityType = EntityType,
                EntityId = EntityId,
                Comment = Comment,
                IsCritical = IsCritical,
                NeedsAcknowledgment = NeedsAcknowledgment,
                AlertOnFailure = AlertOnFailure,
                EscalationNote = EscalationNote,
                Retries = Retries,
                MaxRetries = MaxRetries,
                LastResult = LastResult,
                LastError = LastError,
                LastExecuted = LastExecuted,
                ExtraParams = ExtraParams,
                CreatedById = CreatedById,
                CreatedBy = CreatedBy,
                LastModifiedById = LastModifiedById,
                DeviceInfo = DeviceInfo,
                SessionId = SessionId,
                IpAddress = IpAddress
            };

        public ScheduledJob ToEntity(ScheduledJob? existing)
        {
            var entity = existing ?? new ScheduledJob();
            entity.Id = Id;
            entity.Name = Name;
            entity.JobType = JobType;
            entity.Status = Status;
            entity.NextDue = NextDue == default ? DateTime.UtcNow : NextDue;
            entity.RecurrencePattern = RecurrencePattern;
            entity.CronExpression = CronExpression;
            entity.EntityType = EntityType;
            entity.EntityId = EntityId;
            entity.Comment = Comment;
            entity.IsCritical = IsCritical;
            entity.NeedsAcknowledgment = NeedsAcknowledgment;
            entity.AlertOnFailure = AlertOnFailure;
            entity.EscalationNote = EscalationNote;
            entity.Retries = Retries;
            entity.MaxRetries = MaxRetries;
            entity.LastResult = LastResult;
            entity.LastError = LastError;
            entity.LastExecuted = LastExecuted;
            entity.ExtraParams = ExtraParams;
            entity.CreatedById = CreatedById;
            entity.CreatedBy = CreatedBy;
            entity.LastModifiedById = LastModifiedById;
            entity.DeviceInfo = DeviceInfo;
            entity.SessionId = SessionId;
            entity.IpAddress = IpAddress;
            return entity;
        }
    }
}


