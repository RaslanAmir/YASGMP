using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using ComponentEntity = YasGMP.Models.Component;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Handles equipment calibration records with SAP B1 workflows inside the WPF shell.</summary>
/// <remarks>
/// Form Modes: Find filters calibrations via CFL, Add seeds <see cref="CalibrationEditor.CreateEmpty"/>, View locks the editor, and Update enables editing with attachment and supplier/component selectors.
/// Audit &amp; Logging: Delegates persistence to <see cref="ICalibrationCrudService"/> with required e-signatures and attachment handling; actual audit hashes are written by the underlying services rather than this view-model.
/// Localization: Emits inline strings such as `"Calibration"`, `"Select Calibration"`, status prompts for due dates, and attachment notifications; resource keys are pending.
/// Navigation: ModuleKey `Calibration` aligns docking, while overrides of `CreateCflRequestAsync` and related status strings drive Golden Arrow/CFL routing between components, suppliers, and calibration records.
/// </remarks>
public sealed partial class CalibrationModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Shell registration key that binds Calibration into the docking layout.</summary>
    /// <remarks>Execution: Resolved when the shell composes modules and persists layouts. Form Mode: Identifier applies across Find/Add/View/Update. Localization: Currently paired with the inline caption "Calibration" until `Modules_Calibration_Title` is introduced.</remarks>
    public new const string ModuleKey = "Calibration";

    private readonly ICalibrationCrudService _calibrationService;
    private readonly IComponentCrudService _componentService;
    private readonly IAuthContext _authContext;
    private readonly IFilePicker _filePicker;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;

    private Calibration? _loadedCalibration;
    private CalibrationEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;
    private IReadOnlyList<ComponentEntity> _components = Array.Empty<ComponentEntity>();
    private IReadOnlyList<Supplier> _suppliers = Array.Empty<Supplier>();

    /// <summary>Initializes the Calibration module view model with domain and shell services.</summary>
    /// <remarks>Execution: Invoked when the shell activates the module or Golden Arrow navigation materializes it. Form Mode: Seeds Find/View immediately while deferring Add/Update wiring to later transitions. Localization: Relies on inline strings for tab titles and prompts until module resources exist.</remarks>
    public CalibrationModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        ICalibrationCrudService calibrationService,
        IComponentCrudService componentService,
        IAuthContext authContext,
        IFilePicker filePicker,
        IAttachmentWorkflowService attachmentWorkflow,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Calibration", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        _calibrationService = calibrationService ?? throw new ArgumentNullException(nameof(calibrationService));
        _componentService = componentService ?? throw new ArgumentNullException(nameof(componentService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));

        Editor = CalibrationEditor.CreateEmpty();
        ComponentOptions = new ObservableCollection<ComponentOption>();
        SupplierOptions = new ObservableCollection<SupplierOption>();

        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
    }

    /// <summary>Generated property exposing the editor for the Calibration module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Calibration_Editor` resources are available.</remarks>
    [ObservableProperty]
    private CalibrationEditor _editor;

    /// <summary>Generated property exposing the is editor enabled for the Calibration module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_Calibration_IsEditorEnabled` resources are available.</remarks>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Collection presenting the component options for the Calibration document host.</summary>
    /// <remarks>Execution: Populated as records load or staging mutates. Form Mode: Visible in all modes with editing reserved for Add/Update. Localization: Grid headers/tooltips remain inline until `Modules_Calibration_Grid` resources exist.</remarks>
    public ObservableCollection<ComponentOption> ComponentOptions { get; }

    /// <summary>Collection presenting the supplier options for the Calibration document host.</summary>
    /// <remarks>Execution: Populated as records load or staging mutates. Form Mode: Visible in all modes with editing reserved for Add/Update. Localization: Grid headers/tooltips remain inline until `Modules_Calibration_Grid` resources exist.</remarks>
    public ObservableCollection<SupplierOption> SupplierOptions { get; }

    /// <summary>Command executing the attach document workflow for the Calibration module.</summary>
    /// <remarks>Execution: Invoked when the correlated ribbon or toolbar control is activated. Form Mode: Enabled only when the current mode supports the action (generally Add/Update). Localization: Uses inline button labels/tooltips until `Ribbon_Calibration_AttachDocument` resources are authored.</remarks>
    public IAsyncRelayCommand AttachDocumentCommand { get; }

    /// <summary>Loads Calibration records from domain services.</summary>
    /// <remarks>Execution: Triggered by Find refreshes and shell activation. Form Mode: Supplies data for Find/View while Add/Update reuse cached results. Localization: Emits inline status strings pending `Status_Calibration_Loaded` resources.</remarks>
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        _components = await _componentService.GetAllAsync().ConfigureAwait(false);
        _suppliers = await Database.GetAllSuppliersAsync().ConfigureAwait(false);

        RefreshComponentOptions(_components);
        RefreshSupplierOptions(_suppliers);

        var calibrations = await _calibrationService.GetAllAsync().ConfigureAwait(false);
        return calibrations.Select(ToRecord).ToList();
    }

    /// <summary>Provides design-time sample data for the Calibration designer experience.</summary>
    /// <remarks>Execution: Invoked only by design-mode checks to support Blend/preview tooling. Form Mode: Mirrors Find mode to preview list layouts. Localization: Sample literals remain inline for clarity.</remarks>
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        _components = new List<ComponentEntity>
        {
            new() { Id = 201, Name = "Temperature Probe" },
            new() { Id = 202, Name = "Pressure Transducer" }
        };

        _suppliers = new List<Supplier>
        {
            new() { Id = 51, Name = "Metrologix Labs" }
        };

        RefreshComponentOptions(_components);
        RefreshSupplierOptions(_suppliers);

        var sample = new List<Calibration>
        {
            new()
            {
                Id = 1001,
                ComponentId = 201,
                SupplierId = 51,
                CalibrationDate = DateTime.UtcNow.AddDays(-14),
                NextDue = DateTime.UtcNow.AddMonths(6),
                Result = "PASS",
                CertDoc = "CAL-1001.pdf",
                Comment = "Initial qualification"
            },
            new()
            {
                Id = 1002,
                ComponentId = 202,
                SupplierId = 51,
                CalibrationDate = DateTime.UtcNow.AddDays(-45),
                NextDue = DateTime.UtcNow.AddMonths(3),
                Result = "PASS",
                CertDoc = "CAL-1002.pdf",
                Comment = "Semi-annual"
            }
        };

        return sample.Select(ToRecord).ToList();
    }

    /// <summary>Builds the Choose-From-List request used for Golden Arrow navigation.</summary>
    /// <remarks>Execution: Called when the shell launches CFL dialogs, routing via `ModuleKey` "Calibration". Form Mode: Provides lookup data irrespective of current mode. Localization: Dialog titles and descriptions use inline strings until `CFL_Calibration` resources exist.</remarks>
    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var calibrations = await _calibrationService.GetAllAsync().ConfigureAwait(false);
        var items = calibrations
            .Select(calibration =>
            {
                var key = calibration.Id.ToString(CultureInfo.InvariantCulture);
                var label = $"Calibration #{calibration.Id}";
                var descriptionParts = new List<string>
                {
                    FindComponentName(calibration.ComponentId) ?? $"ComponentEntity #{calibration.ComponentId}",
                    calibration.CalibrationDate.ToString("d", CultureInfo.CurrentCulture),
                    calibration.NextDue.ToString("d", CultureInfo.CurrentCulture)
                };

                if (!string.IsNullOrWhiteSpace(calibration.Result))
                {
                    descriptionParts.Add(calibration.Result!);
                }

                return new CflItem(key, label, string.Join(" • ", descriptionParts));
            })
            .ToList();

        return new CflRequest("Select Calibration", items);
    }

    /// <summary>Applies CFL selections back into the Calibration workspace.</summary>
    /// <remarks>Execution: Runs after CFL or Golden Arrow completion, updating `StatusMessage` for `ModuleKey` "Calibration". Form Mode: Navigates records without disturbing active edits. Localization: Status feedback uses inline phrases pending `Status_Calibration_Filtered`.</remarks>
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

    /// <summary>Loads editor payloads for the selected Calibration record.</summary>
    /// <remarks>Execution: Triggered when document tabs change or shell routing targets `ModuleKey` "Calibration". Form Mode: Honors Add/Update safeguards to avoid overwriting dirty state. Localization: Inline status/error strings remain until `Status_Calibration` resources are available.</remarks>
    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedCalibration = null;
            SetEditor(CalibrationEditor.CreateEmpty());
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

        var calibration = await _calibrationService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (calibration is null)
        {
            StatusMessage = $"Unable to locate calibration #{id}.";
            return;
        }

        _loadedCalibration = calibration;
        LoadEditor(calibration);
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
                _loadedCalibration = null;
                SetEditor(CalibrationEditor.CreateForNew());
                ApplyDefaultLookupSelections();
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
        try
        {
            var calibration = Editor.ToCalibration(_loadedCalibration);
            _calibrationService.Validate(calibration);
        }
        catch (InvalidOperationException ex)
        {
            errors.Add(ex.Message);
        }
        catch (Exception ex)
        {
            errors.Add($"Unexpected validation failure: {ex.Message}");
        }

        return await Task.FromResult<IReadOnlyList<string>>(errors).ConfigureAwait(false);
    }

    /// <summary>Persists the current record and coordinates signatures, attachments, and audits.</summary>
    /// <remarks>Execution: Runs after validation when OK/Update is confirmed. Form Mode: Exclusive to Add/Update operations. Localization: Success/failure messaging remains inline pending dedicated resources.</remarks>
    protected override async Task<bool> OnSaveAsync()
    {
        if (Mode == FormMode.Update && _loadedCalibration is null)
        {
            StatusMessage = "Select a calibration before saving.";
            return false;
        }

        var calibration = Editor.ToCalibration(_loadedCalibration);
        var recordId = Mode == FormMode.Update ? _loadedCalibration!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("calibrations", recordId))
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

        calibration.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;

        var context = CalibrationCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        Calibration adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _calibrationService.CreateAsync(calibration, context).ConfigureAwait(false);
                calibration.Id = saveResult.Id;
                adapterResult = calibration;
            }
            else if (Mode == FormMode.Update)
            {
                calibration.Id = _loadedCalibration!.Id;
                saveResult = await _calibrationService.UpdateAsync(calibration, context).ConfigureAwait(false);
                adapterResult = calibration;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist calibration: {ex.Message}", ex);
        }

        if (saveResult.SignatureMetadata?.Id is { } signatureId)
        {
            adapterResult.DigitalSignatureId = signatureId;
        }

        _loadedCalibration = calibration;
        LoadEditor(calibration);
        UpdateAttachmentCommandState();

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "calibrations",
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
            SetEditor(_loadedCalibration is null
                ? CalibrationEditor.CreateEmpty()
                : CalibrationEditor.FromCalibration(_loadedCalibration, FindComponentName, FindSupplierName));
            UpdateAttachmentCommandState();
            return;
        }

        if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
            UpdateAttachmentCommandState();
            return;
        }

        if (_loadedCalibration is not null)
        {
            LoadEditor(_loadedCalibration);
        }

        UpdateAttachmentCommandState();
    }

    partial void OnEditorChanging(CalibrationEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(CalibrationEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged += OnEditorPropertyChanged;
    }

    private void OnEditorPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_suppressEditorDirtyNotifications)
        {
            return;
        }

        if (e.PropertyName == nameof(CalibrationEditor.ComponentId))
        {
            Editor.ComponentName = FindComponentName(Editor.ComponentId) ?? string.Empty;
        }
        else if (e.PropertyName == nameof(CalibrationEditor.SupplierId))
        {
            Editor.SupplierName = FindSupplierName(Editor.SupplierId) ?? string.Empty;
        }

        if (IsInEditMode)
        {
            MarkDirty();
        }
    }

    private void ApplyDefaultLookupSelections()
    {
        if (ComponentOptions.Count > 0)
        {
            Editor.ComponentId = ComponentOptions[0].Id;
        }

        if (SupplierOptions.Count > 0)
        {
            Editor.SupplierId = SupplierOptions[0].Id;
        }
    }

    private void LoadEditor(Calibration calibration)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = CalibrationEditor.FromCalibration(calibration, FindComponentName, FindSupplierName);
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateAttachmentCommandState();
    }

    private void SetEditor(CalibrationEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor;
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateAttachmentCommandState();
    }

    private void RefreshComponentOptions(IEnumerable<ComponentEntity> components)
    {
        ComponentOptions.Clear();
        foreach (var component in components)
        {
            var label = string.IsNullOrWhiteSpace(component.Name)
                ? component.Id.ToString(CultureInfo.InvariantCulture)
                : component.Name!;
            ComponentOptions.Add(new ComponentOption(component.Id, label));
        }

        Editor.ComponentName = FindComponentName(Editor.ComponentId) ?? string.Empty;
    }

    private void RefreshSupplierOptions(IEnumerable<Supplier> suppliers)
    {
        SupplierOptions.Clear();
        foreach (var supplier in suppliers)
        {
            SupplierOptions.Add(new SupplierOption(supplier.Id, supplier.Name));
        }

        Editor.SupplierName = FindSupplierName(Editor.SupplierId) ?? string.Empty;
    }

    private ModuleRecord ToRecord(Calibration calibration)
    {
        var fields = new List<InspectorField>
        {
            new("ComponentEntity", FindComponentName(calibration.ComponentId) ?? $"ComponentEntity #{calibration.ComponentId}"),
            new("Supplier", FindSupplierName(calibration.SupplierId) ?? "-"),
            new("Calibrated", calibration.CalibrationDate.ToString("d", CultureInfo.CurrentCulture)),
            new("Next Due", calibration.NextDue.ToString("d", CultureInfo.CurrentCulture)),
            new("Result", string.IsNullOrWhiteSpace(calibration.Result) ? "-" : calibration.Result)
        };

        return new ModuleRecord(
            calibration.Id.ToString(CultureInfo.InvariantCulture),
            $"Calibration #{calibration.Id}",
            calibration.CertDoc,
            null,
            calibration.Comment,
            fields,
            ComponentsModuleViewModel.ModuleKey,
            calibration.ComponentId);
    }

    private string? FindComponentName(int componentId)
        => _components.FirstOrDefault(c => c.Id == componentId)?.Name;

    private string? FindSupplierName(int? supplierId)
    {
        if (!supplierId.HasValue)
        {
            return null;
        }

        return _suppliers.FirstOrDefault(s => s.Id == supplierId.Value)?.Name;
    }

    private bool CanAttachDocument()
        => !IsBusy
           && !IsEditorEnabled
           && _loadedCalibration is { Id: > 0 };

    private async Task AttachDocumentAsync()
    {
        if (_loadedCalibration is null || _loadedCalibration.Id <= 0)
        {
            StatusMessage = "Save the calibration before adding attachments.";
            return;
        }

        try
        {
            IsBusy = true;
            var files = await _filePicker
                .PickFilesAsync(new FilePickerRequest(AllowMultiple: true, Title: $"Attach files to calibration #{_loadedCalibration.Id}"))
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
                    EntityType = "calibrations",
                    EntityId = _loadedCalibration.Id,
                    UploadedById = uploadedBy,
                    Reason = $"calibration:{_loadedCalibration.Id}",
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

    public sealed partial class CalibrationEditor : ObservableObject
    {
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        private int _componentId;

        [ObservableProperty]
        private string _componentName = string.Empty;

        [ObservableProperty]
        private int? _supplierId;

        [ObservableProperty]
        private string _supplierName = string.Empty;

        [ObservableProperty]
        private DateTime _calibrationDate = DateTime.UtcNow.Date;

        [ObservableProperty]
        private DateTime _nextDue = DateTime.UtcNow.Date.AddMonths(6);

        [ObservableProperty]
        private string _certDoc = string.Empty;

        [ObservableProperty]
        private string _result = string.Empty;

        [ObservableProperty]
        private string _comment = string.Empty;

        public static CalibrationEditor CreateEmpty() => new();

        public static CalibrationEditor CreateForNew() => new();

        public static CalibrationEditor FromCalibration(
            Calibration calibration,
            Func<int, string?> componentLookup,
            Func<int?, string?> supplierLookup)
        {
            return new CalibrationEditor
            {
                Id = calibration.Id,
                ComponentId = calibration.ComponentId,
                ComponentName = componentLookup(calibration.ComponentId) ?? string.Empty,
                SupplierId = calibration.SupplierId,
                SupplierName = supplierLookup(calibration.SupplierId) ?? string.Empty,
                CalibrationDate = calibration.CalibrationDate,
                NextDue = calibration.NextDue,
                CertDoc = calibration.CertDoc ?? string.Empty,
                Result = calibration.Result ?? string.Empty,
                Comment = calibration.Comment ?? string.Empty
            };
        }

        public Calibration ToCalibration(Calibration? existing)
        {
            var calibration = existing is null ? new Calibration() : CloneCalibration(existing);
            calibration.Id = Id;
            calibration.ComponentId = ComponentId;
            calibration.SupplierId = SupplierId;
            calibration.CalibrationDate = CalibrationDate;
            calibration.NextDue = NextDue;
            calibration.CertDoc = CertDoc;
            calibration.Result = Result;
            calibration.Comment = Comment;
            return calibration;
        }

        public CalibrationEditor Clone()
            => new()
            {
                Id = Id,
                ComponentId = ComponentId,
                ComponentName = ComponentName,
                SupplierId = SupplierId,
                SupplierName = SupplierName,
                CalibrationDate = CalibrationDate,
                NextDue = NextDue,
                CertDoc = CertDoc,
                Result = Result,
                Comment = Comment
            };

        private static Calibration CloneCalibration(Calibration source)
        {
            return new Calibration
            {
                Id = source.Id,
                ComponentId = source.ComponentId,
                SupplierId = source.SupplierId,
                CalibrationDate = source.CalibrationDate,
                NextDue = source.NextDue,
                CertDoc = source.CertDoc,
                Result = source.Result,
                Comment = source.Comment
            };
        }
    }

    /// <summary>Executes the component option routine for the Calibration module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public readonly record struct ComponentOption(int Id, string Name)
    {
        public override string ToString() => Name;
    }

    /// <summary>Executes the supplier option routine for the Calibration module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public readonly record struct SupplierOption(int Id, string Name)
    {
        public override string ToString() => Name;
    }
}





