using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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

/// <summary>Runs work-order workflows in the WPF shell with SAP B1 style navigation.</summary>
/// <remarks>
/// Form Modes: Find filters the backlog (including CFL search), Add seeds <see cref="WorkOrderEditor.CreateEmpty"/>, View freezes the editor for read-only review, and Update enables editing, attachment uploads, and execution metadata entry.
/// Audit &amp; Logging: Persists via <see cref="IWorkOrderCrudService"/> under mandatory e-signatures and logs `CREATE`/`UPDATE` events through <see cref="AuditService"/> using <see cref="DataDrivenModuleDocumentViewModel.LogAuditAsync"/>.
/// Localization: Relies on inline strings such as `"Work Orders"`, status prompts for signature capture, and attachment feedback; resource keys are pending.
/// Navigation: ModuleKey `WorkOrders` anchors docking, while CFL overrides and related module keys link to assets/calibration records so Golden Arrow navigation and status messages stay synchronized across the shell.
/// </remarks>
public sealed partial class WorkOrdersModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Shell registration key that binds Work Orders into the docking layout.</summary>
    /// <remarks>Execution: Resolved when the shell composes modules and persists layouts. Form Mode: Identifier applies across Find/Add/View/Update. Localization: Currently paired with the inline caption "Work Orders" until `Modules_WorkOrders_Title` is introduced.</remarks>
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

    /// <summary>Initializes the Work Orders module view model with domain and shell services.</summary>
    /// <remarks>Execution: Invoked when the shell activates the module or Golden Arrow navigation materializes it. Form Mode: Seeds Find/View immediately while deferring Add/Update wiring to later transitions. Localization: Relies on inline strings for tab titles and prompts until module resources exist.</remarks>
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
        IModuleNavigationService navigation)
        : base(ModuleKey, "Work Orders", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _workOrderService = workOrderService ?? throw new ArgumentNullException(nameof(workOrderService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        Editor = WorkOrderEditor.CreateEmpty();
        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
        SummarizeWithAiCommand = new RelayCommand(OpenAiSummary);
        Toolbar.Add(new ModuleToolbarCommand("Summarize (AI)", SummarizeWithAiCommand));
    }

    /// <summary>Generated property exposing the editor for the Work Orders module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_WorkOrders_Editor` resources are available.</remarks>
    [ObservableProperty]
    private WorkOrderEditor _editor;

    /// <summary>Generated property exposing the is editor enabled for the Work Orders module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_WorkOrders_IsEditorEnabled` resources are available.</remarks>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Opens the AI module and runs a Work Orders summary.</summary>
    public IRelayCommand SummarizeWithAiCommand { get; }

    private void OpenAiSummary()
    {
        var shell = YasGMP.Common.ServiceLocator.GetRequiredService<IShellInteractionService>();
        var doc = shell.OpenModule(AiModuleViewModel.ModuleKey, "summary:workorders");
        shell.Activate(doc);
    }

    /// <summary>Command executing the attach document workflow for the Work Orders module.</summary>
    /// <remarks>Execution: Invoked when the correlated ribbon or toolbar control is activated. Form Mode: Enabled only when the current mode supports the action (generally Add/Update). Localization: Uses inline button labels/tooltips until `Ribbon_WorkOrders_AttachDocument` resources are authored.</remarks>
    public IAsyncRelayCommand AttachDocumentCommand { get; }

    /// <summary>Loads Work Orders records from domain services.</summary>
    /// <remarks>Execution: Triggered by Find refreshes and shell activation. Form Mode: Supplies data for Find/View while Add/Update reuse cached results. Localization: Emits inline status strings pending `Status_WorkOrders_Loaded` resources.</remarks>
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var result = await Database.GetAllWorkOrdersWithProvenanceAsync();
        SetProvenance($"source: work_orders, variant: {result.Variant}");
        return result.Items.Select(ToRecord).ToList();
    }

    /// <summary>Provides design-time sample data for the Work Orders designer experience.</summary>
    /// <remarks>Execution: Invoked only by design-mode checks to support Blend/preview tooling. Form Mode: Mirrors Find mode to preview list layouts. Localization: Sample literals remain inline for clarity.</remarks>
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
        => new List<ModuleRecord>
        {
            new("WO-1001", "Preventive maintenance - Autoclave", "WO-1001", "In Progress", "Monthly PM",
                new[]
                {
                    new InspectorField("Assigned To", "Technician A"),
                    new InspectorField("Priority", "High"),
                    new InspectorField("Due", DateTime.Now.AddDays(2).ToString("d", CultureInfo.CurrentCulture))
                },
                AssetsModuleViewModel.ModuleKey, 1),
            new("WO-1002", "Calibration - pH meter", "WO-1002", "Open", "Annual calibration",
                new[]
                {
                    new InspectorField("Assigned To", "Technician B"),
                    new InspectorField("Priority", "Medium"),
                    new InspectorField("Due", DateTime.Now.AddDays(5).ToString("d", CultureInfo.CurrentCulture))
                },
                CalibrationModuleViewModel.ModuleKey, 2)
        };

    /// <summary>Builds the Choose-From-List request used for Golden Arrow navigation.</summary>
    /// <remarks>Execution: Called when the shell launches CFL dialogs, routing via `ModuleKey` "WorkOrders". Form Mode: Provides lookup data irrespective of current mode. Localization: Dialog titles and descriptions use inline strings until `CFL_WorkOrders` resources exist.</remarks>
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

        return new CflRequest(YasGMP.Wpf.Helpers.Loc.S("CFL_Select_WorkOrder", "Select Work Order"), items);
    }

    /// <summary>Applies CFL selections back into the Work Orders workspace.</summary>
    /// <remarks>Execution: Runs after CFL or Golden Arrow completion, updating `StatusMessage` for `ModuleKey` "WorkOrders". Form Mode: Navigates records without disturbing active edits. Localization: Status feedback uses inline phrases pending `Status_WorkOrders_Filtered`.</remarks>
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
        StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Status_WorkOrders_FilteredBy", "Filtered {0} by \"{1}\"."), Title, search);
        return Task.CompletedTask;
    }

    /// <summary>Loads editor payloads for the selected Work Orders record.</summary>
    /// <remarks>Execution: Triggered when document tabs change or shell routing targets `ModuleKey` "WorkOrders". Form Mode: Honors Add/Update safeguards to avoid overwriting dirty state. Localization: Inline status/error strings remain until `Status_WorkOrders` resources are available.</remarks>
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
            StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Status_WorkOrders_UnableToLoad", "Unable to load {0}."), record.Title);
            return;
        }

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

    /// <summary>Validates the current editor payload before persistence.</summary>
    /// <remarks>Execution: Invoked immediately prior to OK/Update actions. Form Mode: Only Add/Update trigger validation. Localization: Error messages flow from inline literals until validation resources are added.</remarks>
    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Editor.Title))
            errors.Add(YasGMP.Wpf.Helpers.Loc.S("Validation_WorkOrders_TitleRequired", "Title is required."));

        if (string.IsNullOrWhiteSpace(Editor.Description))
            errors.Add(YasGMP.Wpf.Helpers.Loc.S("Validation_WorkOrders_DescriptionRequired", "Description is required."));

        if (string.IsNullOrWhiteSpace(Editor.Type))
            errors.Add(YasGMP.Wpf.Helpers.Loc.S("Validation_WorkOrders_TypeRequired", "Type is required."));

        if (string.IsNullOrWhiteSpace(Editor.Priority))
            errors.Add(YasGMP.Wpf.Helpers.Loc.S("Validation_WorkOrders_PriorityRequired", "Priority is required."));

        if (string.IsNullOrWhiteSpace(Editor.Status))
            errors.Add(YasGMP.Wpf.Helpers.Loc.S("Validation_WorkOrders_StatusRequired", "Status is required."));

        if (Editor.MachineId <= 0)
            errors.Add(YasGMP.Wpf.Helpers.Loc.S("Validation_WorkOrders_MachineRequired", "Machine selection is required."));

        if (Editor.RequestedById <= 0)
            errors.Add(YasGMP.Wpf.Helpers.Loc.S("Validation_WorkOrders_RequestedByRequired", "Requested by user is required."));

        if (Editor.CreatedById <= 0)
            errors.Add(YasGMP.Wpf.Helpers.Loc.S("Validation_WorkOrders_CreatedByRequired", "Created by user is required."));

        if (Editor.AssignedToId <= 0)
            errors.Add(YasGMP.Wpf.Helpers.Loc.S("Validation_WorkOrders_AssignedToRequired", "Assigned technician is required."));

        if (string.IsNullOrWhiteSpace(Editor.Result))
            errors.Add(YasGMP.Wpf.Helpers.Loc.S("Validation_WorkOrders_ResultRequired", "Result summary is required."));

        return await Task.FromResult(errors).ConfigureAwait(false);
    }

    /// <summary>Persists the current record and coordinates signatures, attachments, and audits.</summary>
    /// <remarks>Execution: Runs after validation when OK/Update is confirmed. Form Mode: Exclusive to Add/Update operations. Localization: Success/failure messaging remains inline pending dedicated resources.</remarks>
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
            StatusMessage = YasGMP.Wpf.Helpers.Loc.S("Status_WorkOrders_SelectBeforeSave", "Select a work order before saving.");
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
            StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Error_Signature_Failed", "Electronic signature failed: {0}"), ex.Message);
            return false;
        }

        if (signatureResult is null)
        {
            StatusMessage = YasGMP.Wpf.Helpers.Loc.S("Status_Signature_Cancelled", "Electronic signature cancelled. Save aborted.");
            return false;
        }

        if (signatureResult.Signature is null)
        {
            StatusMessage = YasGMP.Wpf.Helpers.Loc.S("Error_Signature_NotCaptured", "Electronic signature was not captured.");
            return false;
        }

        entity.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;

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
            throw new InvalidOperationException(string.Format(YasGMP.Wpf.Helpers.Loc.S("Error_WorkOrders_SaveFailed", "Failed to persist work order: {0}"), ex.Message), ex);
        }

        if (saveResult.SignatureMetadata?.Id is { } signatureId)
        {
            adapterResult.DigitalSignatureId = signatureId;
        }

        _loadedEntity = entity;
        LoadEditor(entity);
        UpdateAttachmentCommandState();

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

        try
        {
            await SignaturePersistenceHelper
                .PersistIfRequiredAsync(_signatureDialog, signatureResult)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Error_Signature_PersistFailed", "Failed to persist electronic signature: {0}"), ex.Message);
            Mode = FormMode.Update;
            return false;
        }

        StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Status_Signature_Captured", "Electronic signature captured ({0})."), signatureResult.ReasonDisplay);

        var auditAction = Mode == FormMode.Add ? "CREATE" : "UPDATE";
        var currentUserId = userId ?? 0;
        var currentIp = _authContext.CurrentIpAddress ?? string.Empty;
        var currentDevice = _authContext.CurrentDeviceInfo ?? string.Empty;
        var currentSession = _authContext.CurrentSessionId ?? string.Empty;
        var signature = signatureResult.Signature;
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
                YasGMP.Wpf.Helpers.Loc.S("Error_Audit_LogWorkOrderFailed", "Failed to log work order audit."))
            .ConfigureAwait(false);

        return true;
    }

    /// <summary>Reverts in-flight edits and restores the last committed snapshot.</summary>
    /// <remarks>Execution: Activated when Cancel is chosen mid-edit. Form Mode: Applies to Add/Update; inert elsewhere. Localization: Cancellation prompts use inline text until localized resources exist.</remarks>
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
            StatusMessage = YasGMP.Wpf.Helpers.Loc.S("Status_Attach_SaveBeforeAttach", "Save the work order before adding attachments.");
            return;
        }

        try
        {
            IsBusy = true;
            var files = await _filePicker
                .PickFilesAsync(new FilePickerRequest(AllowMultiple: true, Title: string.Format(YasGMP.Wpf.Helpers.Loc.S("Attachment_Picker_Title", "Attach files to {0}"), _loadedEntity.Title)))
                .ConfigureAwait(false);

            if (files is null || files.Count == 0)
            {
                StatusMessage = YasGMP.Wpf.Helpers.Loc.S("Status_Attach_Cancelled", "Attachment upload cancelled.");
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
            StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Error_Attach_UploadFailed", "Attachment upload failed: {0}"), ex.Message);
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

    private static ModuleRecord ToRecord(WorkOrder workOrder)
    {
        var fields = new List<InspectorField>
        {
            new("Assigned To", workOrder.AssignedTo?.FullName ?? workOrder.AssignedTo?.Username ?? "-"),
            new("Priority", workOrder.Priority),
            new("Status", workOrder.Status),
            new("Due Date", workOrder.DueDate?.ToString("d", CultureInfo.CurrentCulture) ?? "-"),
            new("Machine", workOrder.Machine?.Name ?? workOrder.MachineId.ToString(CultureInfo.InvariantCulture))
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

        public static WorkOrderEditor CreateEmpty() => new();

        public static WorkOrderEditor CreateForNew(IAuthContext authContext)
        {
            var userId = authContext.CurrentUser?.Id ?? 1;
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
                AssignedToId = userId
            };
        }

        public static WorkOrderEditor FromEntity(WorkOrder entity)
        {
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
                Notes = entity.Notes
            };
        }

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
                Notes = Notes
            };

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
            return entity;
        }
    }
}


