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

/// <summary>Manages supplier qualification data inside the WPF shell with SAP B1 conventions.</summary>
/// <remarks>
/// Form Modes: Find filters suppliers, Add seeds <see cref="SupplierEditor.CreateEmpty"/>, View keeps the record immutable for review, and Update enables edits, risk/status updates, and attachment capture.
/// Audit &amp; Logging: Persists through <see cref="ISupplierCrudService"/> with enforced e-signature capture and attachment retention; audit history is produced by the backend service.
/// Localization: Inline strings such as `"Suppliers"`, `"Active"`, `"Blacklisted"`, and workflow status prompts will be replaced by localisation keys in a later pass.
/// Navigation: ModuleKey `Suppliers` anchors docking and Golden Arrow routing (e.g. from parts or incidents), while `StatusMessage` updates share supplier save/search outcomes with the shell status bar.
/// </remarks>
public sealed partial class SuppliersModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Shell registration key that binds Suppliers into the docking layout.</summary>
    /// <remarks>Execution: Resolved when the shell composes modules and persists layouts. Form Mode: Identifier applies across Find/Add/View/Update. Localization: Currently paired with the inline caption "Suppliers" until `Modules_Suppliers_Title` is introduced.</remarks>
    public new const string ModuleKey = "Suppliers";

    private static readonly IReadOnlyList<string> DefaultStatusOptions = new ReadOnlyCollection<string>(new[]
    {
        "Active",
        "Validated",
        "Qualified",
        "Suspended",
        "Under Review",
        "Pending Approval",
        "CAPA",
        "Expired",
        "Delisted",
        "Blacklisted",
        "Probation"
    });

    private static readonly IReadOnlyList<string> DefaultRiskOptions = new ReadOnlyCollection<string>(new[]
    {
        "Low",
        "Moderate",
        "Elevated",
        "High",
        "Critical"
    });

    private readonly ISupplierCrudService _supplierService;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IFilePicker _filePicker;
    private readonly IAuthContext _authContext;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private Supplier? _loadedSupplier;
    private SupplierEditor? _snapshot;
    private bool _suppressDirtyNotifications;
    private int? _lastSavedSupplierId;

    /// <summary>Initializes the Suppliers module view model with domain and shell services.</summary>
    /// <remarks>Execution: Invoked when the shell activates the module or Golden Arrow navigation materializes it. Form Mode: Seeds Find/View immediately while deferring Add/Update wiring to later transitions. Localization: Relies on inline strings for tab titles and prompts until module resources exist.</remarks>
    public SuppliersModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ISupplierCrudService supplierService,
        IAttachmentWorkflowService attachmentWorkflow,
        IFilePicker filePicker,
        IAuthContext authContext,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Suppliers", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));

        Editor = SupplierEditor.CreateEmpty();
        StatusOptions = DefaultStatusOptions;
        RiskOptions = DefaultRiskOptions;
        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
        SummarizeWithAiCommand = new RelayCommand(OpenAiSummary);
        Toolbar.Add(new ModuleToolbarCommand("Summarize (AI)", SummarizeWithAiCommand));
    }

    /// <summary>Generated property exposing the editor for the Suppliers module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Suppliers_Editor` resources are available.</remarks>
    [ObservableProperty]
    private SupplierEditor _editor;

    /// <summary>Opens the AI module to summarize the currently selected supplier.</summary>
    public IRelayCommand SummarizeWithAiCommand { get; }

    private void OpenAiSummary()
    {
        if (SelectedRecord is null && _loadedSupplier is null)
        {
            StatusMessage = "Select a supplier to summarize.";
            return;
        }

        var s = _loadedSupplier;
        string prompt;
        if (s is null)
        {
            // Fallback to record title only
            prompt = $"Summarize supplier: {SelectedRecord?.Title}. Provide risks, status and recommended next steps in <= 8 bullets.";
        }
        else
        {
            prompt = $"Summarize this supplier for QA/Procurement in <= 8 bullets. Name={s.Name}; Type={s.SupplierType}; Status={s.Status}; Risk={s.RiskLevel}; Country={s.Country}; Contacts={s.Email}/{s.Phone}; ActiveFrom={s.CooperationStart:yyyy-MM-dd}; ActiveTo={s.CooperationEnd:yyyy-MM-dd}.";
        }

        var shell = YasGMP.Common.ServiceLocator.GetRequiredService<IShellInteractionService>();
        var doc = shell.OpenModule(AiModuleViewModel.ModuleKey, $"prompt:{prompt}");
        shell.Activate(doc);
    }

    /// <summary>Generated property exposing the is editor enabled for the Suppliers module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Suppliers_IsEditorEnabled` resources are available.</remarks>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Generated property exposing the status options for the Suppliers module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Suppliers_StatusOptions` resources are available.</remarks>
    [ObservableProperty]
    private IReadOnlyList<string> _statusOptions;

    /// <summary>Generated property exposing the risk options for the Suppliers module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Suppliers_RiskOptions` resources are available.</remarks>
    [ObservableProperty]
    private IReadOnlyList<string> _riskOptions;

    /// <summary>Command executing the attach document workflow for the Suppliers module.</summary>
    /// <remarks>Execution: Invoked when the correlated ribbon or toolbar control is activated. Form Mode: Enabled only when the current mode supports the action (generally Add/Update). Localization: Uses inline button labels/tooltips until `Ribbon_Suppliers_AttachDocument` resources are authored.</remarks>
    public IAsyncRelayCommand AttachDocumentCommand { get; }

    /// <summary>Loads Suppliers records from domain services.</summary>
    /// <remarks>Execution: Triggered by Find refreshes and shell activation. Form Mode: Supplies data for Find/View while Add/Update reuse cached results. Localization: Emits inline status strings pending `Status_Suppliers_Loaded` resources.</remarks>
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var suppliers = await _supplierService.GetAllAsync().ConfigureAwait(false);
        var ordered = suppliers
            .OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(s => s.Id)
            .Select(ToRecord)
            .ToList();

        if (_lastSavedSupplierId.HasValue)
        {
            var savedKey = _lastSavedSupplierId.Value.ToString(CultureInfo.InvariantCulture);
            var index = ordered.FindIndex(r => r.Key == savedKey);
            if (index > 0)
            {
                var match = ordered[index];
                ordered.RemoveAt(index);
                ordered.Insert(0, match);
            }

            _lastSavedSupplierId = null;
        }

        return ordered;
    }

    /// <summary>Provides design-time sample data for the Suppliers designer experience.</summary>
    /// <remarks>Execution: Invoked only by design-mode checks to support Blend/preview tooling. Form Mode: Mirrors Find mode to preview list layouts. Localization: Sample literals remain inline for clarity.</remarks>
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var sample = new[]
        {
            new Supplier
            {
                Id = 1,
                Name = "Contoso Calibration",
                SupplierType = "Calibration",
                Status = "active",
                Email = "support@contoso.example",
                Phone = "+385 91 111 222",
                RiskLevel = "Moderate",
                Country = "Croatia",
                CooperationStart = DateTime.UtcNow.AddYears(-2),
                CooperationEnd = DateTime.UtcNow.AddYears(1)
            },
            new Supplier
            {
                Id = 2,
                Name = "Globex Servicers",
                SupplierType = "Maintenance",
                Status = "suspended",
                Email = "hq@globex.example",
                Phone = "+385 91 333 444",
                RiskLevel = "High",
                Country = "Austria"
            }
        };

        return sample.Select(ToRecord).ToList();
    }

    /// <summary>Loads editor payloads for the selected Suppliers record.</summary>
    /// <remarks>Execution: Triggered when document tabs change or shell routing targets `ModuleKey` "Suppliers". Form Mode: Honors Add/Update safeguards to avoid overwriting dirty state. Localization: Inline status/error strings remain until `Status_Suppliers` resources are available.</remarks>
    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedSupplier = null;
            SetEditor(SupplierEditor.CreateEmpty());
            UpdateAttachmentCommandState();
            return;
        }

        if (!int.TryParse(record.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return;
        }

        if (IsInEditMode)
        {
            return;
        }

        var supplier = await _supplierService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (supplier is null)
        {
            StatusMessage = $"Supplier #{id} could not be located.";
            return;
        }


        _loadedSupplier = supplier;
        LoadEditor(supplier);
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
                _loadedSupplier = null;
                SetEditor(SupplierEditor.CreateForNew());
                break;
            case FormMode.Update:
                if (_loadedSupplier is not null)
                {
                    _snapshot = Editor.Clone();
                }
                break;
            case FormMode.Find:
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
        var supplier = Editor.ToSupplier(_loadedSupplier);
        var errors = new List<string>();

        try
        {
            _supplierService.Validate(supplier);
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(ex.Message);
        }
        catch (Exception ex)
        {
            errors.Add($"Unexpected validation error: {ex.Message}");
        }

        return await Task.FromResult<IReadOnlyList<string>>(errors).ConfigureAwait(false);
    }

    /// <summary>Persists the current record and coordinates signatures, attachments, and audits.</summary>
    /// <remarks>Execution: Runs after validation when OK/Update is confirmed. Form Mode: Exclusive to Add/Update operations. Localization: Success/failure messaging remains inline pending dedicated resources.</remarks>
    protected override async Task<bool> OnSaveAsync()
    {
        if (Mode == FormMode.Update && _loadedSupplier is null)
        {
            StatusMessage = "Select a supplier before saving.";
            return false;
        }

        var supplier = Editor.ToSupplier(_loadedSupplier);
        supplier.Status = _supplierService.NormalizeStatus(supplier.Status);

        var recordId = Mode == FormMode.Update ? _loadedSupplier!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("suppliers", recordId))
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

        supplier.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;

        var context = SupplierCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        Supplier adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _supplierService.CreateAsync(supplier, context).ConfigureAwait(false);
                supplier.Id = saveResult.Id;
                adapterResult = supplier;
            }
            else if (Mode == FormMode.Update)
            {
                supplier.Id = _loadedSupplier!.Id;
                saveResult = await _supplierService.UpdateAsync(supplier, context).ConfigureAwait(false);
                adapterResult = supplier;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist supplier: {ex.Message}", ex);
        }

        _loadedSupplier = supplier;
        _lastSavedSupplierId = supplier.Id;

        LoadEditor(supplier);
        UpdateAttachmentCommandState();

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "suppliers",
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
            if (_loadedSupplier is not null)
            {
                LoadEditor(_loadedSupplier);
            }
            else
            {
                SetEditor(SupplierEditor.CreateEmpty());
            }
        }
        else if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }

        UpdateAttachmentCommandState();
    }

    /// <summary>Builds the Choose-From-List request used for Golden Arrow navigation.</summary>
    /// <remarks>Execution: Called when the shell launches CFL dialogs, routing via `ModuleKey` "Suppliers". Form Mode: Provides lookup data irrespective of current mode. Localization: Dialog titles and descriptions use inline strings until `CFL_Suppliers` resources exist.</remarks>
    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var suppliers = await _supplierService.GetAllAsync().ConfigureAwait(false);
        var items = suppliers.Select(supplier =>
        {
            var key = supplier.Id.ToString(CultureInfo.InvariantCulture);
            var descriptionParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(supplier.SupplierType))
            {
                descriptionParts.Add(supplier.SupplierType);
            }

            if (!string.IsNullOrWhiteSpace(supplier.Country))
            {
                descriptionParts.Add(supplier.Country);
            }

            if (!string.IsNullOrWhiteSpace(supplier.Email))
            {
                descriptionParts.Add(supplier.Email);
            }

            var description = descriptionParts.Count > 0 ? string.Join(" • ", descriptionParts) : null;
            return new CflItem(key, supplier.Name, description);
        }).ToList();

        return new CflRequest("Select Supplier", items);
    }

    /// <summary>Applies CFL selections back into the Suppliers workspace.</summary>
    /// <remarks>Execution: Runs after CFL or Golden Arrow completion, updating `StatusMessage` for `ModuleKey` "Suppliers". Form Mode: Navigates records without disturbing active edits. Localization: Status feedback uses inline phrases pending `Status_Suppliers_Filtered`.</remarks>
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

        StatusMessage = $"Filtered suppliers by \"{SearchText}\".";
        return Task.CompletedTask;
    }

    /// <summary>Executes the matches search routine for the Suppliers module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    protected override bool MatchesSearch(ModuleRecord record, string searchText)
    {
        if (base.MatchesSearch(record, searchText))
        {
            return true;
        }

        return record.InspectorFields.Any(field => field.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase));
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

    partial void OnEditorChanging(SupplierEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(SupplierEditor value)
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

        if (IsEditorEnabled)
        {
            MarkDirty();
        }
    }

    private void LoadEditor(Supplier supplier)
    {
        _suppressDirtyNotifications = true;
        Editor = SupplierEditor.FromSupplier(supplier);
        _suppressDirtyNotifications = false;
        ResetDirty();
    }

    private void SetEditor(SupplierEditor editor)
    {
        _suppressDirtyNotifications = true;
        Editor = editor;
        _suppressDirtyNotifications = false;
        ResetDirty();
    }

    private bool CanAttachDocument()
        => !IsBusy && !IsEditorEnabled && _loadedSupplier is not null && _loadedSupplier.Id > 0;

    private void UpdateAttachmentCommandState()
    {
        var app = System.Windows.Application.Current;
        void Invoke() { if (AttachDocumentCommand is AsyncRelayCommand c) c.NotifyCanExecuteChanged(); }
        if (app?.Dispatcher?.CheckAccess() == true) Invoke();
        else app?.Dispatcher?.BeginInvoke(new Action(Invoke));
    }

    private async Task AttachDocumentAsync()
    {
        if (_loadedSupplier is null || _loadedSupplier.Id <= 0)
        {
            return;
        }

        var files = await _filePicker.PickFilesAsync(new FilePickerRequest
        {
            Title = "Attach supplier document"
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
                EntityType = "suppliers",
                EntityId = _loadedSupplier.Id,
                FileName = file.FileName,
                ContentType = file.ContentType,
                UploadedById = _authContext.CurrentUser?.Id,
                SourceIp = _authContext.CurrentIpAddress,
                SourceHost = _authContext.CurrentDeviceInfo,
                Reason = "Supplier document upload"
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

    private static ModuleRecord ToRecord(Supplier supplier)
    {
        var inspector = new List<InspectorField>
        {
            new("Type", string.IsNullOrWhiteSpace(supplier.SupplierType) ? "-" : supplier.SupplierType!),
            new("Email", string.IsNullOrWhiteSpace(supplier.Email) ? "-" : supplier.Email!),
            new("Phone", string.IsNullOrWhiteSpace(supplier.Phone) ? "-" : supplier.Phone!),
            new("Risk", string.IsNullOrWhiteSpace(supplier.RiskLevel) ? "-" : supplier.RiskLevel!),
            new("Country", string.IsNullOrWhiteSpace(supplier.Country) ? "-" : supplier.Country!)
        };

        var relatedKey = supplier.PartsSupplied.Count > 0 ? PartsModuleViewModel.ModuleKey : null;
        return new ModuleRecord(
            supplier.Id.ToString(CultureInfo.InvariantCulture),
            supplier.Name,
            supplier.VatNumber,
            supplier.Status,
            supplier.Notes,
            inspector,
            relatedKey,
            supplier.PartsSupplied.Count > 0 ? supplier.PartsSupplied.First().Id : null);
    }
}

public sealed partial class SupplierEditor : ObservableObject
{
    /// <summary>Generated property exposing the id for the Suppliers module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Suppliers_Id` resources are available.</remarks>
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _supplierType = string.Empty;

    [ObservableProperty]
    private string _status = "Active";

    [ObservableProperty]
    private string _vatNumber = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _website = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private string _city = string.Empty;

    [ObservableProperty]
    private string _country = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _contractFile = string.Empty;

    [ObservableProperty]
    private string _riskLevel = "Low";

    /// <summary>Generated property exposing the is qualified for the Suppliers module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Suppliers_IsQualified` resources are available.</remarks>
    [ObservableProperty]
    private bool _isQualified;

    [ObservableProperty]
    private DateTime? _cooperationStart = DateTime.UtcNow.Date;

    /// <summary>Generated property exposing the cooperation end for the Suppliers module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Suppliers_CooperationEnd` resources are available.</remarks>
    [ObservableProperty]
    private DateTime? _cooperationEnd;

    [ObservableProperty]
    private string _registeredAuthorities = string.Empty;

    [ObservableProperty]
    private string _digitalSignature = string.Empty;

    /// <summary>Executes the create empty routine for the Suppliers module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public static SupplierEditor CreateEmpty() => new();

    /// <summary>Executes the create for new routine for the Suppliers module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public static SupplierEditor CreateForNew()
        => new()
        {
            Status = "Active",
            RiskLevel = "Low",
            CooperationStart = DateTime.UtcNow.Date
        };

    /// <summary>Executes the from supplier routine for the Suppliers module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public static SupplierEditor FromSupplier(Supplier supplier)
    {
        return new SupplierEditor
        {
            Id = supplier.Id,
            Name = supplier.Name ?? string.Empty,
            SupplierType = supplier.SupplierType ?? string.Empty,
            Status = string.IsNullOrWhiteSpace(supplier.Status)
                ? "Active"
                : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(supplier.Status),
            VatNumber = supplier.VatNumber ?? string.Empty,
            Email = supplier.Email ?? string.Empty,
            Phone = supplier.Phone ?? string.Empty,
            Website = supplier.Website ?? string.Empty,
            Address = supplier.Address ?? string.Empty,
            City = supplier.City ?? string.Empty,
            Country = supplier.Country ?? string.Empty,
            Notes = supplier.Notes ?? string.Empty,
            ContractFile = supplier.ContractFile ?? string.Empty,
            RiskLevel = string.IsNullOrWhiteSpace(supplier.RiskLevel) ? "Low" : supplier.RiskLevel!,
            IsQualified = supplier.IsQualified,
            CooperationStart = supplier.CooperationStart,
            CooperationEnd = supplier.CooperationEnd,
            RegisteredAuthorities = supplier.RegisteredAuthorities ?? string.Empty,
            DigitalSignature = supplier.DigitalSignature ?? string.Empty
        };
    }

    /// <summary>Executes the clone routine for the Suppliers module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public SupplierEditor Clone()
    {
        return new SupplierEditor
        {
            Id = Id,
            Name = Name,
            SupplierType = SupplierType,
            Status = Status,
            VatNumber = VatNumber,
            Email = Email,
            Phone = Phone,
            Website = Website,
            Address = Address,
            City = City,
            Country = Country,
            Notes = Notes,
            ContractFile = ContractFile,
            RiskLevel = RiskLevel,
            IsQualified = IsQualified,
            CooperationStart = CooperationStart,
            CooperationEnd = CooperationEnd,
            RegisteredAuthorities = RegisteredAuthorities,
            DigitalSignature = DigitalSignature
        };
    }

    /// <summary>Executes the to supplier routine for the Suppliers module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public Supplier ToSupplier(Supplier? existing)
    {
        var supplier = existing ?? new Supplier();
        supplier.Id = Id;
        supplier.Name = Name?.Trim() ?? string.Empty;
        supplier.SupplierType = SupplierType?.Trim() ?? string.Empty;
        supplier.Status = Status?.Trim() ?? string.Empty;
        supplier.VatNumber = VatNumber?.Trim() ?? string.Empty;
        supplier.Email = Email?.Trim() ?? string.Empty;
        supplier.Phone = Phone?.Trim() ?? string.Empty;
        supplier.Website = Website?.Trim() ?? string.Empty;
        supplier.Address = Address?.Trim() ?? string.Empty;
        supplier.City = City?.Trim() ?? string.Empty;
        supplier.Country = Country?.Trim() ?? string.Empty;
        supplier.Notes = Notes?.Trim() ?? string.Empty;
        supplier.ContractFile = ContractFile?.Trim() ?? string.Empty;
        supplier.RiskLevel = RiskLevel?.Trim() ?? string.Empty;
        supplier.IsQualified = IsQualified;
        supplier.CooperationStart = CooperationStart;
        supplier.CooperationEnd = CooperationEnd;
        supplier.RegisteredAuthorities = RegisteredAuthorities?.Trim() ?? string.Empty;
        supplier.DigitalSignature = DigitalSignature ?? existing?.DigitalSignature ?? string.Empty;
        return supplier;
    }
}



