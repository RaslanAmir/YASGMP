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
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Handles spare part inventory within the WPF shell using SAP B1 form semantics.</summary>
/// <remarks>
/// Form Modes: Find filters parts, Add seeds <see cref="PartEditor.CreateEmpty"/>, View keeps the editor read-only for stock review, and Update enables edits plus attachment uploads and supplier refreshes.
/// Audit &amp; Logging: Persists via <see cref="IPartCrudService"/> behind mandatory e-signatures; attachment retention and audit trails are delegated to the workflow and CRUD services.
/// Localization: Inline strings such as `"Parts &amp; Stock"`, `"Stock health calculated"`, and status prompts remain until resource keys become available.
/// Navigation: ModuleKey `Parts` anchors docking; CFL requests expose part pickers, and `ModuleRecord` entries carry supplier/warehouse identifiers so Golden Arrow navigation can pivot to related modules while status text updates the shell.
/// </remarks>
public sealed partial class PartsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Shell registration key that binds Parts &amp; Stock into the docking layout.</summary>
    /// <remarks>Execution: Resolved when the shell composes modules and persists layouts. Form Mode: Identifier applies across Find/Add/View/Update. Localization: Currently paired with the inline caption "Parts &amp; Stock" until `Modules_Parts_Title` is introduced.</remarks>
    public new const string ModuleKey = "Parts";

    private readonly IPartCrudService _partService;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IFilePicker _filePicker;
    private readonly IAuthContext _authContext;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private Part? _loadedPart;
    private PartEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;

    /// <summary>Initializes the Parts &amp; Stock module view model with domain and shell services.</summary>
    /// <remarks>Execution: Invoked when the shell activates the module or Golden Arrow navigation materializes it. Form Mode: Seeds Find/View immediately while deferring Add/Update wiring to later transitions. Localization: Relies on inline strings for tab titles and prompts until module resources exist.</remarks>
    public PartsModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IPartCrudService partService,
        IAttachmentWorkflowService attachmentWorkflow,
        IFilePicker filePicker,
        IAuthContext authContext,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Parts &amp; Stock", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        _partService = partService ?? throw new ArgumentNullException(nameof(partService));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));

        Editor = PartEditor.CreateEmpty();
        StatusOptions = new ReadOnlyCollection<string>(new[] { "active", "inactive", "blocked" });
        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
        SummarizeWithAiCommand = new RelayCommand(OpenAiSummary);
        Toolbar.Add(new ModuleToolbarCommand("Summarize (AI)", SummarizeWithAiCommand));
    }

    /// <summary>Generated property exposing the editor for the Parts &amp; Stock module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Parts_Editor` resources are available.</remarks>
    [ObservableProperty]
    private PartEditor _editor;

    /// <summary>Opens the AI module to summarize the selected part.</summary>
    public IRelayCommand SummarizeWithAiCommand { get; }

    /// <summary>Generated property exposing the is editor enabled for the Parts &amp; Stock module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Parts_IsEditorEnabled` resources are available.</remarks>
    [ObservableProperty]
    private bool _isEditorEnabled;

    [ObservableProperty]
    private IReadOnlyList<Supplier> _supplierOptions = Array.Empty<Supplier>();

    [ObservableProperty]
    private string _stockHealthMessage = string.Empty;

    /// <summary>Collection presenting the status options for the Parts &amp; Stock document host.</summary>
    /// <remarks>Execution: Populated as records load or staging mutates. Form Mode: Visible in all modes with editing reserved for Add/Update. Localization: Grid headers/tooltips remain inline until `Modules_Parts_Grid` resources exist.</remarks>
    public IReadOnlyList<string> StatusOptions { get; }

    /// <summary>Command executing the attach document workflow for the Parts &amp; Stock module.</summary>
    /// <remarks>Execution: Invoked when the correlated ribbon or toolbar control is activated. Form Mode: Enabled only when the current mode supports the action (generally Add/Update). Localization: Uses inline button labels/tooltips until `Ribbon_Parts_AttachDocument` resources are authored.</remarks>
    public IAsyncRelayCommand AttachDocumentCommand { get; }

    /// <summary>Loads Parts &amp; Stock records from domain services.</summary>
    /// <remarks>Execution: Triggered by Find refreshes and shell activation. Form Mode: Supplies data for Find/View while Add/Update reuse cached results. Localization: Emits inline status strings pending `Status_Parts_Loaded` resources.</remarks>
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        await EnsureSuppliersAsync().ConfigureAwait(false);
        var parts = await _partService.GetAllAsync().ConfigureAwait(false);
        return parts.Select(ToRecord).ToList();
    }

    /// <summary>Provides design-time sample data for the Parts &amp; Stock designer experience.</summary>
    /// <remarks>Execution: Invoked only by design-mode checks to support Blend/preview tooling. Form Mode: Mirrors Find mode to preview list layouts. Localization: Sample literals remain inline for clarity.</remarks>
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var sample = new[]
        {
            new Part
            {
                Id = 1,
                Code = "PRT-001",
                Name = "Sterile Tubing",
                Status = "active",
                Description = "Silicone transfer tubing",
                Location = "Main Warehouse",
                Stock = 120,
                MinStockAlert = 50,
                DefaultSupplierName = "Contoso Pharma"
            },
            new Part
            {
                Id = 2,
                Code = "FLT-010",
                Name = "HEPA Filter",
                Status = "inactive",
                Description = "Spare HEPA H14",
                Location = "Cleanroom",
                Stock = 10,
                MinStockAlert = 5,
                DefaultSupplierName = "Globex"
            }
        };

        return sample.Select(ToRecord).ToList();
    }

    /// <summary>Executes the on activated async routine for the Parts &amp; Stock module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    protected override async Task OnActivatedAsync(object? parameter)
    {
        await EnsureSuppliersAsync().ConfigureAwait(false);
    }

    /// <summary>Builds the Choose-From-List request used for Golden Arrow navigation.</summary>
    /// <remarks>Execution: Called when the shell launches CFL dialogs, routing via `ModuleKey` "Parts". Form Mode: Provides lookup data irrespective of current mode. Localization: Dialog titles and descriptions use inline strings until `CFL_Parts` resources exist.</remarks>
    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var parts = await _partService.GetAllAsync().ConfigureAwait(false);
        var items = parts.Select(part =>
        {
            var key = part.Id.ToString(CultureInfo.InvariantCulture);
            var label = string.IsNullOrWhiteSpace(part.Name) ? key : part.Name;
            var descriptionParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(part.Code))
            {
                descriptionParts.Add(part.Code!);
            }

            if (!string.IsNullOrWhiteSpace(part.Location))
            {
                descriptionParts.Add(part.Location!);
            }

            if (part.Stock.HasValue)
            {
                descriptionParts.Add($"Stock: {part.Stock.Value}");
            }

            var description = descriptionParts.Count > 0
                ? string.Join(" • ", descriptionParts)
                : null;

            return new CflItem(key, label, description);
        }).ToList();

        return new CflRequest("Select Part", items);
    }

    /// <summary>Applies CFL selections back into the Parts &amp; Stock workspace.</summary>
    /// <remarks>Execution: Runs after CFL or Golden Arrow completion, updating `StatusMessage` for `ModuleKey` "Parts". Form Mode: Navigates records without disturbing active edits. Localization: Status feedback uses inline phrases pending `Status_Parts_Filtered`.</remarks>
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

        StatusMessage = $"Filtered {Title} by \"{SearchText}\".";
        return Task.CompletedTask;
    }

    private void OpenAiSummary()
    {
        if (SelectedRecord is null && _loadedPart is null)
        {
            StatusMessage = "Select a part to summarize.";
            return;
        }

        var p = _loadedPart;
        string prompt = p is null
            ? $"Summarize part: {SelectedRecord?.Title}. Provide stock, suppliers and replenishment risks in <= 8 bullets."
            : $"Summarize this part (<= 8 bullets). Name={p.Name}; Code={p.Code}; Status={p.Status}; Stock={p.Stock}; Location={p.Location}; PreferredSupplier={p.DefaultSupplierName ?? p.Supplier}.";

        var shell = YasGMP.Common.ServiceLocator.GetRequiredService<IShellInteractionService>();
        var doc = shell.OpenModule(AiModuleViewModel.ModuleKey, $"prompt:{prompt}");
        shell.Activate(doc);
    }

    /// <summary>Loads editor payloads for the selected Parts &amp; Stock record.</summary>
    /// <remarks>Execution: Triggered when document tabs change or shell routing targets `ModuleKey` "Parts". Form Mode: Honors Add/Update safeguards to avoid overwriting dirty state. Localization: Inline status/error strings remain until `Status_Parts` resources are available.</remarks>
    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedPart = null;
            SetEditor(PartEditor.CreateEmpty());
            StockHealthMessage = string.Empty;
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

        var part = await _partService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (part is null)
        {
            StatusMessage = $"Unable to locate part #{id}.";
            return;
        }

        _loadedPart = part;
        LoadEditor(part);
        UpdateStockHealth();
        UpdateAttachmentCommandState();
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
                _loadedPart = null;
                SetEditor(PartEditor.CreateForNew(_partService.NormalizeStatus("active")));
                StockHealthMessage = "";
                break;
            case FormMode.Update:
                _snapshot = Editor.Clone();
                break;
            case FormMode.View:
                if (_loadedPart is not null)
                {
                    LoadEditor(_loadedPart);
                    UpdateStockHealth();
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
        var part = Editor.ToPart(_loadedPart);
        var errors = new List<string>();
        try
        {
            _partService.Validate(part);
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
        if (Mode == FormMode.Update && _loadedPart is null)
        {
            StatusMessage = "Select a part before saving.";
            return false;
        }

        var part = Editor.ToPart(_loadedPart);
        part.Status = _partService.NormalizeStatus(part.Status);
        if (SupplierOptions.Count > 0 && part.DefaultSupplierId.HasValue)
        {
            var supplier = SupplierOptions.FirstOrDefault(s => s.Id == part.DefaultSupplierId.Value);
            if (supplier is not null)
            {
                part.DefaultSupplierName = supplier.Name;
            }
        }

        var recordId = Mode == FormMode.Update ? _loadedPart!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("parts", recordId))
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

        part.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;

        var context = PartCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        Part adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _partService.CreateAsync(part, context).ConfigureAwait(false);
                part.Id = saveResult.Id;
                adapterResult = part;
            }
            else if (Mode == FormMode.Update)
            {
                part.Id = _loadedPart!.Id;
                saveResult = await _partService.UpdateAsync(part, context).ConfigureAwait(false);
                adapterResult = part;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist part: {ex.Message}", ex);
        }

        if (saveResult.SignatureMetadata?.Id is { } signatureId)
        {
            adapterResult.DigitalSignatureId = signatureId;
        }

        _loadedPart = part;
        LoadEditor(part);
        UpdateStockHealth();
        UpdateAttachmentCommandState();

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "parts",
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
            if (_loadedPart is not null)
            {
                LoadEditor(_loadedPart);
            }
            else
            {
                SetEditor(PartEditor.CreateEmpty());
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

    partial void OnEditorChanging(PartEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(PartEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged += OnEditorPropertyChanged;
    }

    private async Task EnsureSuppliersAsync()
    {
        if (SupplierOptions.Count > 0)
        {
            return;
        }

        var suppliers = await Database.GetAllSuppliersAsync().ConfigureAwait(false) ?? new List<Supplier>();
        SupplierOptions = new ReadOnlyCollection<Supplier>(suppliers);
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

        if (e.PropertyName is nameof(PartEditor.Stock)
            or nameof(PartEditor.MinStockAlert)
            or nameof(PartEditor.LowWarehouseCount)
            or nameof(PartEditor.IsWarehouseStockCritical))
        {
            UpdateStockHealth();
        }
    }

    private void LoadEditor(Part part)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = PartEditor.FromPart(part, _partService.NormalizeStatus);
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateStockHealth();
    }

    private void SetEditor(PartEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateStockHealth();
    }

    private bool CanAttachDocument()
        => !IsBusy
           && !IsEditorEnabled
           && _loadedPart is not null
           && _loadedPart.Id > 0;

    private async Task AttachDocumentAsync()
    {
        if (_loadedPart is null || _loadedPart.Id <= 0)
        {
            return;
        }

        var files = await _filePicker.PickFilesAsync(new FilePickerRequest
        {
            Title = "Attach file to part"
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
                EntityType = "parts",
                EntityId = _loadedPart.Id,
                FileName = file.FileName,
                ContentType = file.ContentType,
                UploadedById = _authContext.CurrentUser?.Id,
                SourceIp = _authContext.CurrentIpAddress,
                SourceHost = _authContext.CurrentDeviceInfo,
                Reason = $"Attached via WPF on {DateTime.UtcNow:O}"
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

    private void UpdateStockHealth()
    {
        if (Editor.IsWarehouseStockCritical || Editor.IsBelowMinimum)
        {
            StockHealthMessage = Editor.LowWarehouseCount > 0
                ? $"{Editor.LowWarehouseCount} warehouse location(s) below minimum threshold."
                : "Stock below configured minimum.";
        }
        else if (!string.IsNullOrWhiteSpace(Editor.WarehouseSummary))
        {
            StockHealthMessage = $"Distribution: {Editor.WarehouseSummary}.";
        }
        else
        {
            StockHealthMessage = string.Empty;
        }
    }

    private static ModuleRecord ToRecord(Part part)
    {
        var fields = new List<InspectorField>
        {
            new("SKU", part.Sku ?? part.Code),
            new("Supplier", part.DefaultSupplierName ?? "-"),
            new("Stock", part.Stock?.ToString(CultureInfo.InvariantCulture) ?? "-"),
            new("Min Stock", part.MinStockAlert?.ToString(CultureInfo.InvariantCulture) ?? "-"),
            new("Location", part.Location ?? "-"),
            new("Status", part.Status ?? "-")
        };

        return new ModuleRecord(
            part.Id.ToString(CultureInfo.InvariantCulture),
            part.Name,
            part.Code,
            part.Status,
            part.Description,
            fields,
            SuppliersModuleViewModel.ModuleKey,
            part.DefaultSupplierId);
    }

    public sealed partial class PartEditor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private string _code = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _category = string.Empty;

        [ObservableProperty]
        private string _status = "active";

        [ObservableProperty]
        private int? _stock;

        [ObservableProperty]
        private int? _minStockAlert;

        [ObservableProperty]
        private string _location = string.Empty;

        [ObservableProperty]
        private int? _defaultSupplierId;

        [ObservableProperty]
        private string _defaultSupplierName = string.Empty;

        [ObservableProperty]
        private string _sku = string.Empty;

        [ObservableProperty]
        private decimal? _price;

        [ObservableProperty]
        private int _lowWarehouseCount;

        [ObservableProperty]
        private string _warehouseSummary = string.Empty;

        [ObservableProperty]
        private bool _isWarehouseStockCritical;

        public bool IsBelowMinimum => MinStockAlert.HasValue && Stock.HasValue && Stock.Value < MinStockAlert.Value;

        public static PartEditor CreateEmpty() => new();

        public static PartEditor CreateForNew(string normalizedStatus)
            => new() { Status = normalizedStatus };

        public static PartEditor FromPart(Part part, Func<string?, string> normalizer)
        {
            return new PartEditor
            {
                Id = part.Id,
                Code = part.Code ?? string.Empty,
                Name = part.Name ?? string.Empty,
                Description = part.Description ?? string.Empty,
                Category = part.Category ?? string.Empty,
                Status = normalizer(part.Status),
                Stock = part.Stock,
                MinStockAlert = part.MinStockAlert,
                Location = part.Location ?? string.Empty,
                DefaultSupplierId = part.DefaultSupplierId,
                DefaultSupplierName = part.DefaultSupplierName ?? string.Empty,
                Sku = part.Sku ?? string.Empty,
                Price = part.Price,
                LowWarehouseCount = part.LowWarehouseCount,
                WarehouseSummary = part.WarehouseSummary ?? string.Empty,
                IsWarehouseStockCritical = part.IsWarehouseStockCritical
            };
        }

        public PartEditor Clone()
            => new()
            {
                Id = Id,
                Code = Code,
                Name = Name,
                Description = Description,
                Category = Category,
                Status = Status,
                Stock = Stock,
                MinStockAlert = MinStockAlert,
                Location = Location,
                DefaultSupplierId = DefaultSupplierId,
                DefaultSupplierName = DefaultSupplierName,
                Sku = Sku,
                Price = Price,
                LowWarehouseCount = LowWarehouseCount,
                WarehouseSummary = WarehouseSummary,
                IsWarehouseStockCritical = IsWarehouseStockCritical
            };

        public Part ToPart(Part? existing)
        {
            var part = existing is null ? new Part() : existing;
            part.Id = Id;
            part.Code = Code?.Trim() ?? string.Empty;
            part.Name = Name?.Trim() ?? string.Empty;
            part.Description = Description?.Trim();
            part.Category = Category?.Trim();
            part.Status = Status?.Trim();
            part.Stock = Stock;
            part.MinStockAlert = MinStockAlert;
            part.Location = Location?.Trim();
            part.DefaultSupplierId = DefaultSupplierId;
            part.DefaultSupplierName = DefaultSupplierName?.Trim();
            part.Sku = Sku?.Trim();
            part.Price = Price;
            part.LowWarehouseCount = LowWarehouseCount;
            part.WarehouseSummary = WarehouseSummary ?? string.Empty;
            part.IsWarehouseStockCritical = IsWarehouseStockCritical;
            return part;
        }
    }
}

