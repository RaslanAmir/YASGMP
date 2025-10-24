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
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Runs warehouse oversight in the WPF shell using SAP B1 workflows.</summary>
/// <remarks>
/// Form Modes: Find lists warehouses, Add seeds <see cref="WarehouseEditor.CreateEmpty"/>, View exposes ledger/stock snapshots in read-only mode, and Update unlocks editing with attachment capture and movement acknowledgements.
/// Audit &amp; Logging: Persists via <see cref="IWarehouseCrudService"/> with enforced e-signatures and delegates ledger/audit retention to the CRUD and attachment workflow services.
/// Localization: Inline literals such as `"Warehouse"`, status names (`"qualified"`, `"maintenance"`), and stock alert messages remain pending RESX wiring.
/// Navigation: ModuleKey `Warehouse` keeps docking and Golden Arrow routing aligned; CFL pickers drive cross-navigation to specific warehouses and status updates broadcast ledger refresh states through the shell status bar.
/// </remarks>
public sealed partial class WarehouseModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Shell registration key that binds Warehouse into the docking layout.</summary>
    /// <remarks>Execution: Resolved when the shell composes modules and persists layouts. Form Mode: Identifier applies across Find/Add/View/Update. Localization: Currently paired with the inline caption "Warehouse" until `Modules_Warehouse_Title` is introduced.</remarks>
    public new const string ModuleKey = "Warehouse";

    private readonly IWarehouseCrudService _warehouseService;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IFilePicker _filePicker;
    private readonly IAuthContext _authContext;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private Warehouse? _loadedWarehouse;
    private WarehouseEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;

    /// <summary>Initializes the Warehouse module view model with domain and shell services.</summary>
    /// <remarks>Execution: Invoked when the shell activates the module or Golden Arrow navigation materializes it. Form Mode: Seeds Find/View immediately while deferring Add/Update wiring to later transitions. Localization: Relies on inline strings for tab titles and prompts until module resources exist.</remarks>
    public WarehouseModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IWarehouseCrudService warehouseService,
        IAttachmentWorkflowService attachmentWorkflow,
        IFilePicker filePicker,
        IAuthContext authContext,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Warehouse", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        _warehouseService = warehouseService ?? throw new ArgumentNullException(nameof(warehouseService));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));

        Editor = WarehouseEditor.CreateEmpty();
        StatusOptions = new ReadOnlyCollection<string>(new[] { "qualified", "in-qualification", "maintenance", "inactive" });
        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
        SummarizeWithAiCommand = new RelayCommand(OpenAiSummary);
        Toolbar.Add(new ModuleToolbarCommand("Summarize (AI)", SummarizeWithAiCommand));
    }

    /// <summary>Generated property exposing the editor for the Warehouse module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Warehouse_Editor` resources are available.</remarks>
    [ObservableProperty]
    private WarehouseEditor _editor;

    /// <summary>Generated property exposing the is editor enabled for the Warehouse module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Warehouse_IsEditorEnabled` resources are available.</remarks>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Opens the AI module to summarize the selected warehouse.</summary>
    public IRelayCommand SummarizeWithAiCommand { get; }

    [ObservableProperty]
    private ObservableCollection<WarehouseStockSnapshot> _stockSnapshot = new();

    [ObservableProperty]
    private ObservableCollection<InventoryMovementEntry> _recentMovements = new();

    /// <summary>Generated property exposing the has stock alerts for the Warehouse module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Warehouse_HasStockAlerts` resources are available.</remarks>
    [ObservableProperty]
    private bool _hasStockAlerts;

    /// <summary>Collection presenting the status options for the Warehouse document host.</summary>
    /// <remarks>Execution: Populated as records load or staging mutates. Form Mode: Visible in all modes with editing reserved for Add/Update. Localization: Grid headers/tooltips remain inline until `Modules_Warehouse_Grid` resources exist.</remarks>
    public IReadOnlyList<string> StatusOptions { get; }

    /// <summary>Command executing the attach document workflow for the Warehouse module.</summary>
    /// <remarks>Execution: Invoked when the correlated ribbon or toolbar control is activated. Form Mode: Enabled only when the current mode supports the action (generally Add/Update). Localization: Uses inline button labels/tooltips until `Ribbon_Warehouse_AttachDocument` resources are authored.</remarks>
    public IAsyncRelayCommand AttachDocumentCommand { get; }

    /// <summary>Loads Warehouse records from domain services.</summary>
    /// <remarks>Execution: Triggered by Find refreshes and shell activation. Form Mode: Supplies data for Find/View while Add/Update reuse cached results. Localization: Emits inline status strings pending `Status_Warehouse_Loaded` resources.</remarks>
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var warehouses = await _warehouseService.GetAllAsync().ConfigureAwait(false);
        return warehouses.Select(ToRecord).ToList();
    }

    /// <summary>Provides design-time sample data for the Warehouse designer experience.</summary>
    /// <remarks>Execution: Invoked only by design-mode checks to support Blend/preview tooling. Form Mode: Mirrors Find mode to preview list layouts. Localization: Sample literals remain inline for clarity.</remarks>
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var sample = new[]
        {
            new Warehouse
            {
                Id = 1,
                Name = "Main Warehouse",
                Location = "Building B",
                Status = "qualified",
                LegacyResponsibleName = "John Doe",
                Note = "Primary GMP warehouse"
            },
            new Warehouse
            {
                Id = 2,
                Name = "Cold Storage",
                Location = "Building C",
                Status = "qualified",
                LegacyResponsibleName = "Jane Smith",
                ClimateMode = "2-8°C"
            }
        };

        return sample.Select(ToRecord).ToList();
    }

    /// <summary>Builds the Choose-From-List request used for Golden Arrow navigation.</summary>
    /// <remarks>Execution: Called when the shell launches CFL dialogs, routing via `ModuleKey` "Warehouse". Form Mode: Provides lookup data irrespective of current mode. Localization: Dialog titles and descriptions use inline strings until `CFL_Warehouse` resources exist.</remarks>
    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var warehouses = await _warehouseService.GetAllAsync().ConfigureAwait(false);
        var items = warehouses.Select(warehouse =>
        {
            var key = warehouse.Id.ToString(CultureInfo.InvariantCulture);
            var label = string.IsNullOrWhiteSpace(warehouse.Name) ? key : warehouse.Name;
            var descriptionParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(warehouse.Location))
            {
                descriptionParts.Add(warehouse.Location!);
            }

            if (!string.IsNullOrWhiteSpace(warehouse.Status))
            {
                descriptionParts.Add(warehouse.Status!);
            }

            return new CflItem(key, label, descriptionParts.Count > 0 ? string.Join(" • ", descriptionParts) : null);
        }).ToList();

        return new CflRequest(YasGMP.Wpf.Helpers.Loc.S("CFL_Select_Warehouse", "Select Warehouse"), items);
    }

    /// <summary>Applies CFL selections back into the Warehouse workspace.</summary>
    /// <remarks>Execution: Runs after CFL or Golden Arrow completion, updating `StatusMessage` for `ModuleKey` "Warehouse". Form Mode: Navigates records without disturbing active edits. Localization: Status feedback uses inline phrases pending `Status_Warehouse_Filtered`.</remarks>
    protected override Task OnCflSelectionAsync(CflResult result)
    {
        var match = Records.FirstOrDefault(r => r.Key == result.Selected.Key);
        if (match is not null)
        {
            SelectedRecord = match;
            SearchText = match.Title;
        }
        else
        {
            SearchText = result.Selected.Label;
        }

        StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Status_Warehouse_FilteredBy", "Filtered {0} by \"{1}\"."), Title, SearchText);
        return Task.CompletedTask;
    }

    /// <summary>Loads editor payloads for the selected Warehouse record.</summary>
    /// <remarks>Execution: Triggered when document tabs change or shell routing targets `ModuleKey` "Warehouse". Form Mode: Honors Add/Update safeguards to avoid overwriting dirty state. Localization: Inline status/error strings remain until `Status_Warehouse` resources are available.</remarks>
    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedWarehouse = null;
            SetEditor(WarehouseEditor.CreateEmpty());
            ClearInsights();
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

        var warehouse = await _warehouseService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (warehouse is null)
        {
            StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Status_Warehouse_UnableToLocateById", "Unable to locate warehouse #{0}."), id);
            return;
        }

        _loadedWarehouse = warehouse;
        LoadEditor(warehouse);
        await LoadInsightsAsync(id).ConfigureAwait(false);
        UpdateAttachmentCommandState();
    }

    private void OpenAiSummary()
    {
        if (SelectedRecord is null && _loadedWarehouse is null)
        {
            StatusMessage = "Select a warehouse to summarize.";
            return;
        }

        var w = _loadedWarehouse;
        string prompt = w is null
            ? $"Summarize warehouse: {SelectedRecord?.Title}. Provide stock/alerts/status and next steps in <= 8 bullets."
            : $"Summarize this warehouse (<= 8 bullets). Name={w.Name}; Location={w.Location}; Status={w.Status}; Qualified={w.IsQualified}; Climate={w.ClimateMode}; IoT={w.IoTDeviceId}.";

        var shell = YasGMP.Common.ServiceLocator.GetRequiredService<IShellInteractionService>();
        var doc = shell.OpenModule(AiModuleViewModel.ModuleKey, $"prompt:{prompt}");
        shell.Activate(doc);
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
                _loadedWarehouse = null;
                SetEditor(WarehouseEditor.CreateForNew(_warehouseService.NormalizeStatus("qualified")));
                ClearInsights();
                break;
            case FormMode.Update:
                _snapshot = Editor.Clone();
                break;
            case FormMode.View:
                if (_loadedWarehouse is not null)
                {
                    LoadEditor(_loadedWarehouse);
                    if (_loadedWarehouse.Id > 0)
                    {
                        _ = LoadInsightsAsync(_loadedWarehouse.Id);
                    }
                }
                break;
        }

        UpdateAttachmentCommandState();
        return Task.CompletedTask;
    }

    /// <summary>Validates the current editor payload before persistence.</summary>
    /// <remarks>Execution: Invoked immediately prior to OK/Update actions. Form Mode: Only Add/Update trigger validation. Localization: Error messages flow from inline literals until validation resources are added.</remarks>
    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var warehouse = Editor.ToWarehouse(_loadedWarehouse);
        var errors = new List<string>();
        try
        {
            _warehouseService.Validate(warehouse);
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(ex.Message);
        }
        catch (Exception ex)
        {
            errors.Add($"Unexpected validation error: {ex.Message}");
        }

        return await Task.FromResult(errors).ConfigureAwait(false);
    }

    /// <summary>Persists the current record and coordinates signatures, attachments, and audits.</summary>
    /// <remarks>Execution: Runs after validation when OK/Update is confirmed. Form Mode: Exclusive to Add/Update operations. Localization: Success/failure messaging remains inline pending dedicated resources.</remarks>
    protected override async Task<bool> OnSaveAsync()
    {
        var warehouse = Editor.ToWarehouse(_loadedWarehouse);
        warehouse.Status = _warehouseService.NormalizeStatus(warehouse.Status);

        if (Mode == FormMode.Update && _loadedWarehouse is null)
        {
            StatusMessage = "Select a warehouse before saving.";
            return false;
        }

        var recordId = Mode == FormMode.Update ? _loadedWarehouse!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("warehouses", recordId))
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

        warehouse.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;
        warehouse.LastModified = DateTime.UtcNow;
        warehouse.LastModifiedById = _authContext.CurrentUser?.Id;
        warehouse.LastModifiedByName = _authContext.CurrentUser?.FullName ?? warehouse.LastModifiedByName;
        warehouse.SourceIp = _authContext.CurrentIpAddress ?? warehouse.SourceIp ?? string.Empty;
        warehouse.SessionId = _authContext.CurrentSessionId ?? warehouse.SessionId ?? string.Empty;

        var context = WarehouseCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            (_authContext.CurrentIpAddress ?? string.Empty),
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        Warehouse adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _warehouseService.CreateAsync(warehouse, context).ConfigureAwait(false);
                adapterResult = warehouse;
            }
            else if (Mode == FormMode.Update)
            {
                warehouse.Id = _loadedWarehouse!.Id;
                saveResult = await _warehouseService.UpdateAsync(warehouse, context).ConfigureAwait(false);
                adapterResult = warehouse;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist warehouse: {ex.Message}", ex);
        }

        _loadedWarehouse = warehouse;
        LoadEditor(warehouse);

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "warehouses",
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
            if (_loadedWarehouse is not null)
            {
                LoadEditor(_loadedWarehouse);
            }
            else
            {
                SetEditor(WarehouseEditor.CreateEmpty());
            }
        }
        else if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }

        UpdateAttachmentCommandState();
    }

    /// <summary>Observes property changes to maintain dirty state and command availability.</summary>
    /// <remarks>Execution: Raised whenever generated observable setters fire. Form Mode: Primarily impacts Add/Update as Find/View remain read-only. Localization: Downstream notifications still rely on inline strings.</remarks>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is nameof(IsBusy) or nameof(Mode) or nameof(SelectedRecord) or nameof(IsDirty))
        {
            UpdateAttachmentCommandState();
        }
    }

    partial void OnEditorChanging(WarehouseEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(WarehouseEditor value)
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

    private void LoadEditor(Warehouse warehouse)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = WarehouseEditor.FromWarehouse(warehouse, _warehouseService.NormalizeStatus);
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private void SetEditor(WarehouseEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private async Task LoadInsightsAsync(int warehouseId)
    {
        try
        {
            var stock = await _warehouseService.GetStockSnapshotAsync(warehouseId).ConfigureAwait(false);
            StockSnapshot = new ObservableCollection<WarehouseStockSnapshot>(stock);
            HasStockAlerts = StockSnapshot.Any(s => s.IsBelowMinimum || s.IsAboveMaximum);

            var movements = await _warehouseService.GetRecentMovementsAsync(warehouseId, 15).ConfigureAwait(false);
            RecentMovements = new ObservableCollection<InventoryMovementEntry>(movements);

            StatusMessage = HasStockAlerts
                ? $"{Title}: stock alerts detected for {StockSnapshot.Count(s => s.IsBelowMinimum)} item(s)."
                : $"{Title}: stock overview refreshed ({StockSnapshot.Count} tracked parts).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to load warehouse insight: {ex.Message}";
        }
    }

    private void ClearInsights()
    {
        StockSnapshot.Clear();
        RecentMovements.Clear();
        HasStockAlerts = false;
    }

    private bool CanAttachDocument()
        => !IsBusy
           && !IsEditorEnabled
           && _loadedWarehouse is not null
           && _loadedWarehouse.Id > 0;

    private async Task AttachDocumentAsync()
    {
        if (_loadedWarehouse is null || _loadedWarehouse.Id <= 0)
        {
            return;
        }

        var files = await _filePicker.PickFilesAsync(new FilePickerRequest
        {
            Title = "Attach file to warehouse"
        }).ConfigureAwait(false);

        if (files.Count == 0)
        {
            return;
        }

        var processed = 0;
        var deduplicated = 0;

        foreach (var file in files)
        {
            await using var stream = await file.OpenReadAsync().ConfigureAwait(false);
            var request = new AttachmentUploadRequest
            {
                EntityType = "warehouses",
                EntityId = _loadedWarehouse.Id,
                FileName = file.FileName,
                ContentType = file.ContentType,
                UploadedById = _authContext.CurrentUser?.Id,
                SourceIp = (_authContext.CurrentIpAddress ?? string.Empty),
                SourceHost = _authContext.CurrentDeviceInfo,
                Reason = $"Warehouse attachment via WPF on {DateTime.UtcNow:O}"
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

    private void UpdateAttachmentCommandState()
    {
        YasGMP.Wpf.Helpers.UiCommandHelper.NotifyCanExecuteOnUi(AttachDocumentCommand);
    }

    private static ModuleRecord ToRecord(Warehouse warehouse)
    {
        var fields = new List<InspectorField>
        {
            new("Location", warehouse.Location ?? "-"),
            new("Responsible", warehouse.LegacyResponsibleName ?? "-"),
            new("Status", warehouse.Status ?? "-"),
            new("Qualified", warehouse.IsQualified ? "Yes" : "No")
        };

        return new ModuleRecord(
            warehouse.Id.ToString(CultureInfo.InvariantCulture),
            warehouse.Name,
            warehouse.Name,
            warehouse.Status,
            warehouse.Note,
            fields,
            null,
            warehouse.Id);
    }

    public sealed partial class WarehouseEditor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _location = string.Empty;

        [ObservableProperty]
        private string _status = "qualified";

        [ObservableProperty]
        private string _responsible = string.Empty;

        [ObservableProperty]
        private string _note = string.Empty;

        [ObservableProperty]
        private string _qrCode = string.Empty;

        [ObservableProperty]
        private string _climateMode = string.Empty;

        [ObservableProperty]
        private bool _isQualified = true;

        [ObservableProperty]
        private DateTime? _lastQualified = DateTime.UtcNow.Date;

        public static WarehouseEditor CreateEmpty() => new();

        public static WarehouseEditor CreateForNew(string normalizedStatus)
            => new() { Status = normalizedStatus, IsQualified = normalizedStatus == "qualified" };

        public static WarehouseEditor FromWarehouse(Warehouse warehouse, Func<string?, string> normalizer)
        {
            return new WarehouseEditor
            {
                Id = warehouse.Id,
                Name = warehouse.Name ?? string.Empty,
                Location = warehouse.Location ?? string.Empty,
                Status = normalizer(warehouse.Status),
                Responsible = warehouse.LegacyResponsibleName ?? string.Empty,
                Note = warehouse.Note ?? string.Empty,
                QrCode = warehouse.QrCode ?? string.Empty,
                ClimateMode = warehouse.ClimateMode ?? string.Empty,
                IsQualified = warehouse.IsQualified,
                LastQualified = warehouse.LastQualified
            };
        }

        public WarehouseEditor Clone()
            => new()
            {
                Id = Id,
                Name = Name,
                Location = Location,
                Status = Status,
                Responsible = Responsible,
                Note = Note,
                QrCode = QrCode,
                ClimateMode = ClimateMode,
                IsQualified = IsQualified,
                LastQualified = LastQualified
            };

        public Warehouse ToWarehouse(Warehouse? existing)
        {
            var warehouse = existing is null ? new Warehouse() : existing;
            warehouse.Id = Id;
            warehouse.Name = Name?.Trim() ?? string.Empty;
            warehouse.Location = Location?.Trim() ?? string.Empty;
            warehouse.Status = Status?.Trim() ?? string.Empty;
            warehouse.LegacyResponsibleName = Responsible?.Trim() ?? string.Empty;
            warehouse.Note = Note?.Trim() ?? string.Empty;
            warehouse.QrCode = QrCode?.Trim() ?? string.Empty;
            warehouse.ClimateMode = ClimateMode?.Trim() ?? string.Empty;
            warehouse.IsQualified = IsQualified;
            warehouse.LastQualified = LastQualified;
            return warehouse;
        }
    }
}



