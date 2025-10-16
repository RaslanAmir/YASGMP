using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.ViewModels.Modules;
/// <summary>
/// Represents the work orders module view model value.
/// </summary>

public sealed partial class WorkOrdersModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>
    /// Represents the module key value.
    /// </summary>
    public new const string ModuleKey = "WorkOrders";

    private readonly AuditService _auditService;
    private readonly IAuthContext _authContext;
    private readonly IWorkOrderCrudService _workOrderService;
    private readonly IFilePicker _filePicker;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;

    private WorkOrder? _loadedEntity;
    private WorkOrderEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;
    /// <summary>
    /// Initializes a new instance of the WorkOrdersModuleViewModel class.
    /// </summary>

    public WorkOrdersModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IWorkOrderCrudService workOrderService,
        IAuthContext authContext,
        IFilePicker filePicker,
        IAttachmentWorkflowService attachmentWorkflow,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, localization.GetString("Module.Title.WorkOrders"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _workOrderService = workOrderService ?? throw new ArgumentNullException(nameof(workOrderService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        Editor = WorkOrderEditor.CreateEmpty();
        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
    }

    [ObservableProperty]
    private WorkOrderEditor _editor;

    [ObservableProperty]
    private bool _isEditorEnabled;
    /// <summary>
    /// Gets or sets the attach document command.
    /// </summary>

    public IAsyncRelayCommand AttachDocumentCommand { get; }

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var workOrders = await Database.GetAllWorkOrdersFullAsync().ConfigureAwait(false);
        return workOrders.Select(ToRecord).ToList();
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new("WO-1001", "Preventive maintenance - Autoclave", "WO-1001", "In Progress", "Monthly PM",
                new[]
                {
                    CreateInspectorField("WO-1001", "Preventive maintenance - Autoclave", "Assigned To", "Technician A"),
                    CreateInspectorField("WO-1001", "Preventive maintenance - Autoclave", "Priority", "High"),
                    CreateInspectorField(
                        "WO-1001",
                        "Preventive maintenance - Autoclave",
                        "Due",
                        DateTime.Now.AddDays(2).ToString("d", CultureInfo.CurrentCulture))
                },
                AssetsModuleViewModel.ModuleKey, 1),
            new("WO-1002", "Calibration - pH meter", "WO-1002", "Open", "Annual calibration",
                new[]
                {
                    CreateInspectorField("WO-1002", "Calibration - pH meter", "Assigned To", "Technician B"),
                    CreateInspectorField("WO-1002", "Calibration - pH meter", "Priority", "Medium"),
                    CreateInspectorField(
                        "WO-1002",
                        "Calibration - pH meter",
                        "Due",
                        DateTime.Now.AddDays(5).ToString("d", CultureInfo.CurrentCulture))
                },
                CalibrationModuleViewModel.ModuleKey, 2)
        };

    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var workOrders = await Database.GetAllWorkOrdersFullAsync().ConfigureAwait(false);
        var items = workOrders
            .Select(order =>
            {
                var key = order.Id.ToString(CultureInfo.InvariantCulture);
                var label = string.IsNullOrWhiteSpace(order.Title) ? key : order.Title;
                var descriptionParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(order.Status))
                {
                    descriptionParts.Add(order.Status!);
                }

                if (order.DueDate is not null)
                {
                    descriptionParts.Add(order.DueDate.Value.ToString("d", CultureInfo.CurrentCulture));
                }

                if (!string.IsNullOrWhiteSpace(order.Machine?.Name))
                {
                    descriptionParts.Add(order.Machine!.Name!);
                }

                var description = descriptionParts.Count > 0
                    ? string.Join(" • ", descriptionParts)
                    : null;

                return new CflItem(key, label, description);
            })
            .ToList();

        return new CflRequest("Select Work Order", items);
    }

    protected override Task OnCflSelectionAsync(CflResult result)
    {
        var search = result.Selected.Label;
        var match = Records.FirstOrDefault(r => r.Key == result.Selected.Key);
        if (match is not null)
        {
            SelectedRecord = match;
            search = match.Title;
        }

        SearchText = search;
        StatusMessage = $"Filtered {Title} by \"{search}\".";
        return Task.CompletedTask;
    }

    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedEntity = null;
            SetEditor(WorkOrderEditor.CreateEmpty());
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

        var entity = await _workOrderService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (entity is null)
        {
            StatusMessage = $"Unable to load {record.Title}.";
            return;
        }

        _loadedEntity = entity;
        LoadEditor(entity);
    }

    protected override Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                SetEditor(WorkOrderEditor.CreateForNew(_authContext));
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

        if (string.IsNullOrWhiteSpace(Editor.Title))
        {
            errors.Add("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.Description))
        {
            errors.Add("Description is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.Type))
        {
            errors.Add("Type is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.Priority))
        {
            errors.Add("Priority is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.Status))
        {
            errors.Add("Status is required.");
        }

        if (Editor.MachineId <= 0)
        {
            errors.Add("Machine selection is required.");
        }

        if (Editor.RequestedById <= 0)
        {
            errors.Add("Requested by user is required.");
        }

        if (Editor.CreatedById <= 0)
        {
            errors.Add("Created by user is required.");
        }

        if (Editor.AssignedToId <= 0)
        {
            errors.Add("Assigned technician is required.");
        }

        if (string.IsNullOrWhiteSpace(Editor.Result))
        {
            errors.Add("Result summary is required.");
        }

        return await Task.FromResult(errors).ConfigureAwait(false);
    }

    protected override async Task<bool> OnSaveAsync()
    {
        if (Editor is null)
        {
            return false;
        }

        var userId = _authContext.CurrentUser?.Id;
        if (userId is null or <= 0)
        {
            userId = 1;
        }

        var entity = Editor.ToEntity(_loadedEntity);
        _workOrderService.Validate(entity);
        entity.LastModified = DateTime.UtcNow;
        entity.LastModifiedById = userId;
        entity.DeviceInfo = _authContext.CurrentDeviceInfo;
        entity.SourceIp = _authContext.CurrentIpAddress;
        entity.SessionId = _authContext.CurrentSessionId;

        if (Mode == FormMode.Update && _loadedEntity is null)
        {
            StatusMessage = "Select a work order before saving.";
            return false;
        }

        var recordId = Mode == FormMode.Update ? _loadedEntity!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("work_orders", recordId))
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

        var signature = signatureResult.Signature;
        entity.DigitalSignature = signature.SignatureHash ?? string.Empty;

        var context = WorkOrderCrudContext.Create(
            userId.Value,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        WorkOrder adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                entity.CreatedById = Editor.CreatedById > 0 ? Editor.CreatedById : userId.Value;
                entity.RequestedById = Editor.RequestedById > 0 ? Editor.RequestedById : userId.Value;
                entity.AssignedToId = Editor.AssignedToId > 0 ? Editor.AssignedToId : userId.Value;
                entity.DateOpen = Editor.DateOpen == default ? DateTime.UtcNow : Editor.DateOpen;

                saveResult = await _workOrderService.CreateAsync(entity, context).ConfigureAwait(false);
                entity.Id = saveResult.Id;
                adapterResult = entity;
            }
            else if (Mode == FormMode.Update)
            {
                saveResult = await _workOrderService.UpdateAsync(entity, context).ConfigureAwait(false);
                adapterResult = entity;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist work order: {ex.Message}", ex);
        }

        if (saveResult.SignatureMetadata?.Id is { } signatureId)
        {
            adapterResult.DigitalSignatureId = signatureId;
        }

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "work_orders",
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

        ApplySignatureMetadataToEntity(entity, signatureResult, context, saveResult.SignatureMetadata);

        _loadedEntity = entity;
        LoadEditor(entity);
        UpdateAttachmentCommandState();

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

        var auditAction = Mode == FormMode.Add ? "CREATE" : "UPDATE";
        var currentUserId = userId ?? 0;
        var currentIp = _authContext.CurrentIpAddress ?? string.Empty;
        var currentDevice = _authContext.CurrentDeviceInfo ?? string.Empty;
        var currentSession = _authContext.CurrentSessionId ?? string.Empty;
        var details = string.Join(", ", new[]
        {
            $"user={currentUserId}",
            $"reason={signatureResult.ReasonDisplay ?? string.Empty}",
            $"status={entity.Status ?? string.Empty}",
            $"signature={signature?.SignatureHash ?? string.Empty}",
            $"method={signature?.Method ?? string.Empty}",
            $"outcome={signature?.Status ?? string.Empty}",
            $"ip={currentIp}",
            $"device={currentDevice}",
            $"session={currentSession}"
        });

        await LogAuditAsync(
                _ => _auditService.LogEntityAuditAsync("work_orders", entity.Id, auditAction, details),
                "Failed to log work order audit.")
            .ConfigureAwait(false);

        return true;
    }

    protected override void OnCancel()
    {
        if (Mode == FormMode.Add)
        {
            if (_loadedEntity is not null)
            {
                LoadEditor(_loadedEntity);
            }
            else
            {
                SetEditor(WorkOrderEditor.CreateEmpty());
            }
        }
        else if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }

        UpdateAttachmentCommandState();
    }

    partial void OnEditorChanging(WorkOrderEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(WorkOrderEditor value)
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

    private void LoadEditor(WorkOrder entity)
    {
        _suppressEditorDirtyNotifications = true;
        SetEditor(WorkOrderEditor.FromEntity(entity));
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateAttachmentCommandState();
    }

    private void SetEditor(WorkOrderEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateAttachmentCommandState();
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
            StatusMessage = "Save the work order before adding attachments.";
            return;
        }

        try
        {
            IsBusy = true;
            var files = await _filePicker
                .PickFilesAsync(new FilePickerRequest(AllowMultiple: true, Title: $"Attach files to {_loadedEntity.Title}"))
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
                    EntityType = "work_orders",
                    EntityId = _loadedEntity.Id,
                    UploadedById = uploadedBy,
                    Reason = $"workorder:{_loadedEntity.Id}",
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
        => AttachDocumentCommand.NotifyCanExecuteChanged();

    private void ApplySignatureMetadataToEntity(
        WorkOrder entity,
        ElectronicSignatureDialogResult signatureResult,
        WorkOrderCrudContext context,
        SignatureMetadataDto? metadata)
    {
        if (entity is null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        if (signatureResult is null)
        {
            throw new ArgumentNullException(nameof(signatureResult));
        }

        if (signatureResult.Signature is null)
        {
            throw new ArgumentException("Signature result is missing the captured signature payload.", nameof(signatureResult));
        }

        var signature = signatureResult.Signature;
        var signerId = signature.UserId != 0 ? signature.UserId : context.UserId;
        var fallbackName = _authContext.CurrentUser?.FullName
            ?? _authContext.CurrentUser?.Username
            ?? string.Empty;
        var signerName = !string.IsNullOrWhiteSpace(signature.UserName)
            ? signature.UserName!
            : !string.IsNullOrWhiteSpace(fallbackName)
                ? fallbackName
                : entity.LastModifiedBy?.FullName
                    ?? entity.LastModifiedBy?.Username
                    ?? string.Empty;

        var signatureHash = !string.IsNullOrWhiteSpace(signature.SignatureHash)
            ? signature.SignatureHash!
            : metadata?.Hash ?? entity.DigitalSignature ?? string.Empty;

        entity.DigitalSignature = signatureHash;
        var metadataId = metadata?.Id ?? (signature.Id > 0 ? signature.Id : (int?)null);
        if (metadataId.HasValue)
        {
            entity.DigitalSignatureId = metadataId;
        }

        entity.LastModified = signature.SignedAt ?? DateTime.UtcNow;

        if (signerId > 0)
        {
            entity.LastModifiedById = signerId;
        }

        var deviceInfo = !string.IsNullOrWhiteSpace(signature.DeviceInfo)
            ? signature.DeviceInfo!
            : metadata?.Device ?? context.DeviceInfo ?? entity.DeviceInfo ?? string.Empty;
        var sourceIp = !string.IsNullOrWhiteSpace(signature.IpAddress)
            ? signature.IpAddress!
            : metadata?.IpAddress ?? context.Ip ?? entity.SourceIp ?? string.Empty;
        var sessionId = !string.IsNullOrWhiteSpace(signature.SessionId)
            ? signature.SessionId!
            : metadata?.Session ?? context.SessionId ?? entity.SessionId ?? string.Empty;

        entity.DeviceInfo = deviceInfo ?? string.Empty;
        entity.SourceIp = sourceIp ?? string.Empty;
        entity.SessionId = sessionId ?? string.Empty;

        if (entity.LastModifiedBy is null && (!string.IsNullOrWhiteSpace(signerName) || signerId > 0))
        {
            entity.LastModifiedBy = new User
            {
                Id = signerId,
                FullName = signerName,
                Username = signerName
            };
        }
        else if (entity.LastModifiedBy is not null)
        {
            if (signerId > 0)
            {
                entity.LastModifiedBy.Id = signerId;
            }

            if (!string.IsNullOrWhiteSpace(signerName))
            {
                if (string.IsNullOrWhiteSpace(entity.LastModifiedBy.FullName))
                {
                    entity.LastModifiedBy.FullName = signerName;
                }

                if (string.IsNullOrWhiteSpace(entity.LastModifiedBy.Username))
                {
                    entity.LastModifiedBy.Username = signerName;
                }
            }
        }

        entity.Signatures ??= new List<WorkOrderSignature>();

        var expectedId = metadataId;
        var latestSignature = entity.Signatures
            .OrderByDescending(s => s.SignedAt ?? DateTime.MinValue)
            .FirstOrDefault();

        if (latestSignature is null || (expectedId.HasValue && expectedId.Value > 0 && latestSignature.Id != expectedId.Value))
        {
            latestSignature = new WorkOrderSignature();
            entity.Signatures.Add(latestSignature);
        }

        latestSignature.WorkOrderId = entity.Id;
        if (expectedId.HasValue && expectedId.Value > 0)
        {
            latestSignature.Id = expectedId.Value;
        }

        latestSignature.UserId = signerId;
        latestSignature.SignatureHash = signatureHash;
        latestSignature.SignedAt = signature.SignedAt ?? entity.LastModified;
        latestSignature.Note = !string.IsNullOrWhiteSpace(signature.Note)
            ? signature.Note
            : metadata?.Note ?? context.SignatureNote ?? latestSignature.Note;

        if (!string.IsNullOrWhiteSpace(signatureResult.ReasonDisplay))
        {
            latestSignature.ReasonDescription = signatureResult.ReasonDisplay;
        }

        if (!string.IsNullOrWhiteSpace(signatureResult.ReasonCode))
        {
            latestSignature.ReasonCode = signatureResult.ReasonCode;
        }

        latestSignature.DeviceInfo = !string.IsNullOrWhiteSpace(signature.DeviceInfo)
            ? signature.DeviceInfo
            : metadata?.Device ?? context.DeviceInfo ?? latestSignature.DeviceInfo;
        latestSignature.IpAddress = !string.IsNullOrWhiteSpace(signature.IpAddress)
            ? signature.IpAddress
            : metadata?.IpAddress ?? context.Ip ?? latestSignature.IpAddress;
        latestSignature.SessionId = !string.IsNullOrWhiteSpace(signature.SessionId)
            ? signature.SessionId
            : metadata?.Session ?? context.SessionId ?? latestSignature.SessionId;

        if (latestSignature.User is null && (!string.IsNullOrWhiteSpace(signerName) || signerId > 0))
        {
            latestSignature.User = new User
            {
                Id = signerId,
                FullName = signerName,
                Username = signerName
            };
        }
        else if (latestSignature.User is not null)
        {
            if (signerId > 0)
            {
                latestSignature.User.Id = signerId;
            }

            if (!string.IsNullOrWhiteSpace(signerName))
            {
                if (string.IsNullOrWhiteSpace(latestSignature.User.FullName))
                {
                    latestSignature.User.FullName = signerName;
                }

                if (string.IsNullOrWhiteSpace(latestSignature.User.Username))
                {
                    latestSignature.User.Username = signerName;
                }
            }
        }
    }

    private ModuleRecord ToRecord(WorkOrder workOrder)
    {
        var fields = new List<InspectorField>
        {
            CreateInspectorField(
                workOrder.Id.ToString(CultureInfo.InvariantCulture),
                workOrder.Title,
                "Assigned To",
                workOrder.AssignedTo?.FullName ?? workOrder.AssignedTo?.Username ?? "-"),
            CreateInspectorField(workOrder.Id.ToString(CultureInfo.InvariantCulture), workOrder.Title, "Priority", workOrder.Priority),
            CreateInspectorField(workOrder.Id.ToString(CultureInfo.InvariantCulture), workOrder.Title, "Status", workOrder.Status),
            CreateInspectorField(
                workOrder.Id.ToString(CultureInfo.InvariantCulture),
                workOrder.Title,
                "Due Date",
                workOrder.DueDate?.ToString("d", CultureInfo.CurrentCulture) ?? "-"),
            CreateInspectorField(
                workOrder.Id.ToString(CultureInfo.InvariantCulture),
                workOrder.Title,
                "Machine",
                workOrder.Machine?.Name ?? workOrder.MachineId.ToString(CultureInfo.InvariantCulture))
        };

        return new ModuleRecord(
            workOrder.Id.ToString(CultureInfo.InvariantCulture),
            workOrder.Title,
            workOrder.Title,
            workOrder.Status,
            workOrder.Description,
            fields,
            AssetsModuleViewModel.ModuleKey,
            workOrder.MachineId);
    }
    /// <summary>
    /// Represents the work order editor value.
    /// </summary>

    public sealed partial class WorkOrderEditor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _taskDescription = string.Empty;

        [ObservableProperty]
        private string _type = string.Empty;

        [ObservableProperty]
        private string _priority = string.Empty;

        [ObservableProperty]
        private string _status = string.Empty;

        [ObservableProperty]
        private DateTime _dateOpen = DateTime.UtcNow;

        [ObservableProperty]
        private DateTime? _dueDate;

        [ObservableProperty]
        private DateTime? _dateClose;

        [ObservableProperty]
        private int _requestedById;

        [ObservableProperty]
        private int _createdById;

        [ObservableProperty]
        private int _assignedToId;

        [ObservableProperty]
        private int _machineId;

        [ObservableProperty]
        private int? _componentId;

        [ObservableProperty]
        private string _result = string.Empty;

        [ObservableProperty]
        private string _notes = string.Empty;

        [ObservableProperty]
        private int? _digitalSignatureId;

        [ObservableProperty]
        private string _signatureHash = string.Empty;

        [ObservableProperty]
        private string _signatureReason = string.Empty;

        [ObservableProperty]
        private string _signatureNote = string.Empty;

        [ObservableProperty]
        private DateTime? _signatureTimestampUtc;

        [ObservableProperty]
        private int? _signerUserId;

        [ObservableProperty]
        private string _signerUserName = string.Empty;

        [ObservableProperty]
        private DateTime? _lastModifiedUtc;

        [ObservableProperty]
        private int? _lastModifiedById;

        [ObservableProperty]
        private string _lastModifiedByName = string.Empty;

        [ObservableProperty]
        private string _sourceIp = string.Empty;

        [ObservableProperty]
        private string _sessionId = string.Empty;

        [ObservableProperty]
        private string _deviceInfo = string.Empty;
        /// <summary>
        /// Executes the create empty operation.
        /// </summary>

        public static WorkOrderEditor CreateEmpty()
            => new()
            {
                SignatureHash = string.Empty,
                SignatureReason = string.Empty,
                SignatureNote = string.Empty,
                SignatureTimestampUtc = null,
                SignerUserId = null,
                SignerUserName = string.Empty,
                LastModifiedUtc = null,
                LastModifiedById = null,
                LastModifiedByName = string.Empty,
                SourceIp = string.Empty,
                SessionId = string.Empty,
                DeviceInfo = string.Empty
            };
        /// <summary>
        /// Executes the create for new operation.
        /// </summary>

        public static WorkOrderEditor CreateForNew(IAuthContext authContext)
        {
            var userId = authContext.CurrentUser?.Id ?? 1;
            var userName = authContext.CurrentUser?.FullName
                ?? authContext.CurrentUser?.Username
                ?? string.Empty;
            return new WorkOrderEditor
            {
                Status = "OPEN",
                Priority = "Medium",
                Type = "MAINTENANCE",
                Result = string.Empty,
                Notes = string.Empty,
                DateOpen = DateTime.UtcNow,
                RequestedById = userId,
                CreatedById = userId,
                AssignedToId = userId,
                LastModifiedUtc = DateTime.UtcNow,
                LastModifiedById = userId,
                LastModifiedByName = userName,
                SignatureTimestampUtc = null,
                SignerUserId = userId,
                SignerUserName = userName,
                SignatureHash = string.Empty,
                SignatureReason = string.Empty,
                SignatureNote = string.Empty,
                SourceIp = authContext.CurrentIpAddress ?? string.Empty,
                SessionId = authContext.CurrentSessionId ?? string.Empty,
                DeviceInfo = authContext.CurrentDeviceInfo ?? string.Empty
            };
        }
        /// <summary>
        /// Executes the from entity operation.
        /// </summary>

        public static WorkOrderEditor FromEntity(WorkOrder entity)
        {
            var latestSignature = entity.Signatures?
                .OrderByDescending(s => s.SignedAt ?? DateTime.MinValue)
                .FirstOrDefault();

            var signerName = latestSignature?.User?.FullName
                              ?? latestSignature?.User?.Username
                              ?? entity.LastModifiedBy?.FullName
                              ?? entity.LastModifiedBy?.Username
                              ?? string.Empty;

            return new WorkOrderEditor
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                TaskDescription = entity.TaskDescription,
                Type = entity.Type,
                Priority = entity.Priority,
                Status = entity.Status,
                DateOpen = entity.DateOpen,
                DueDate = entity.DueDate,
                DateClose = entity.DateClose,
                RequestedById = entity.RequestedById,
                CreatedById = entity.CreatedById,
                AssignedToId = entity.AssignedToId,
                MachineId = entity.MachineId,
                ComponentId = entity.ComponentId,
                Result = entity.Result,
                Notes = entity.Notes,
                DigitalSignatureId = entity.DigitalSignatureId,
                SignatureHash = entity.DigitalSignature ?? string.Empty,
                SignatureReason = latestSignature?.ReasonDescription ?? string.Empty,
                SignatureNote = latestSignature?.Note ?? string.Empty,
                SignatureTimestampUtc = latestSignature?.SignedAt ?? entity.LastModified,
                SignerUserId = latestSignature?.UserId ?? entity.LastModifiedById,
                SignerUserName = signerName,
                LastModifiedUtc = entity.LastModified,
                LastModifiedById = entity.LastModifiedById,
                LastModifiedByName = signerName,
                SourceIp = entity.SourceIp ?? string.Empty,
                SessionId = entity.SessionId ?? string.Empty,
                DeviceInfo = entity.DeviceInfo ?? string.Empty
            };
        }
        /// <summary>
        /// Executes the clone operation.
        /// </summary>

        public WorkOrderEditor Clone()
            => new()
            {
                Id = Id,
                Title = Title,
                Description = Description,
                TaskDescription = TaskDescription,
                Type = Type,
                Priority = Priority,
                Status = Status,
                DateOpen = DateOpen,
                DueDate = DueDate,
                DateClose = DateClose,
                RequestedById = RequestedById,
                CreatedById = CreatedById,
                AssignedToId = AssignedToId,
                MachineId = MachineId,
                ComponentId = ComponentId,
                Result = Result,
                Notes = Notes,
                DigitalSignatureId = DigitalSignatureId,
                SignatureHash = SignatureHash,
                SignatureReason = SignatureReason,
                SignatureNote = SignatureNote,
                SignatureTimestampUtc = SignatureTimestampUtc,
                SignerUserId = SignerUserId,
                SignerUserName = SignerUserName,
                LastModifiedUtc = LastModifiedUtc,
                LastModifiedById = LastModifiedById,
                LastModifiedByName = LastModifiedByName,
                SourceIp = SourceIp,
                SessionId = SessionId,
                DeviceInfo = DeviceInfo
            };
        /// <summary>
        /// Executes the to entity operation.
        /// </summary>

        public WorkOrder ToEntity(WorkOrder? existing)
        {
            var entity = existing ?? new WorkOrder();
            entity.Id = Id;
            entity.Title = Title;
            entity.Description = Description;
            entity.TaskDescription = TaskDescription;
            entity.Type = Type;
            entity.Priority = Priority;
            entity.Status = Status;
            entity.DateOpen = DateOpen;
            entity.DueDate = DueDate;
            entity.DateClose = DateClose;
            entity.RequestedById = RequestedById;
            entity.CreatedById = CreatedById;
            entity.AssignedToId = AssignedToId;
            entity.MachineId = MachineId;
            entity.ComponentId = ComponentId;
            entity.Result = Result;
            entity.Notes = Notes;
            entity.DigitalSignatureId = DigitalSignatureId;
            entity.DigitalSignature = SignatureHash ?? string.Empty;
            entity.LastModified = LastModifiedUtc ?? entity.LastModified;
            entity.LastModifiedById = LastModifiedById ?? entity.LastModifiedById;
            entity.DeviceInfo = string.IsNullOrWhiteSpace(DeviceInfo) ? entity.DeviceInfo : DeviceInfo;
            entity.SourceIp = string.IsNullOrWhiteSpace(SourceIp) ? entity.SourceIp : SourceIp;
            entity.SessionId = string.IsNullOrWhiteSpace(SessionId) ? entity.SessionId : SessionId;

            if (!string.IsNullOrWhiteSpace(SignatureReason)
                || !string.IsNullOrWhiteSpace(SignatureNote)
                || SignatureTimestampUtc is not null
                || SignerUserId is not null
                || !string.IsNullOrWhiteSpace(SignerUserName))
            {
                entity.Signatures ??= new List<WorkOrderSignature>();
                var latestSignature = entity.Signatures
                    .OrderByDescending(s => s.SignedAt ?? DateTime.MinValue)
                    .FirstOrDefault();

                if (latestSignature is null)
                {
                    latestSignature = new WorkOrderSignature { WorkOrderId = entity.Id };
                    entity.Signatures.Add(latestSignature);
                }

                latestSignature.SignatureHash = string.IsNullOrWhiteSpace(SignatureHash)
                    ? latestSignature.SignatureHash
                    : SignatureHash;
                latestSignature.ReasonDescription = SignatureReason ?? latestSignature.ReasonDescription;
                latestSignature.Note = SignatureNote ?? latestSignature.Note;
                latestSignature.SignedAt = SignatureTimestampUtc ?? latestSignature.SignedAt;

                if (SignerUserId is > 0)
                {
                    latestSignature.UserId = SignerUserId.Value;
                }

                if (!string.IsNullOrWhiteSpace(SignerUserName))
                {
                    if (latestSignature.User is null)
                    {
                        latestSignature.User = new User
                        {
                            Id = SignerUserId ?? latestSignature.UserId,
                            FullName = SignerUserName,
                            Username = SignerUserName
                        };
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(latestSignature.User.FullName))
                        {
                            latestSignature.User.FullName = SignerUserName;
                        }

                        if (string.IsNullOrWhiteSpace(latestSignature.User.Username))
                        {
                            latestSignature.User.Username = SignerUserName;
                        }
                    }
                }
            }

            return entity;
        }
    }
}
