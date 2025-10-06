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

public sealed partial class SchedulingModuleViewModel : DataDrivenModuleDocumentViewModel
{
    public const string ModuleKey = "Scheduling";

    private readonly IScheduledJobCrudService _scheduledJobService;
    private readonly IAuthContext _authContext;
    private readonly IFilePicker _filePicker;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;

    private ScheduledJob? _loadedJob;
    private ScheduledJobEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;

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
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, "Scheduled Jobs", databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
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
    }

    [ObservableProperty]
    private ScheduledJobEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;

    public IAsyncRelayCommand AttachDocumentCommand { get; }

    public IAsyncRelayCommand ExecuteJobCommand { get; }

    public IAsyncRelayCommand AcknowledgeJobCommand { get; }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var jobs = await Database.GetAllScheduledJobsFullAsync().ConfigureAwait(false);
        return jobs.Select(ToRecord).ToList();
    }

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
        AttachDocumentCommand.NotifyCanExecuteChanged();
        ExecuteJobCommand.NotifyCanExecuteChanged();
        AcknowledgeJobCommand.NotifyCanExecuteChanged();
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
