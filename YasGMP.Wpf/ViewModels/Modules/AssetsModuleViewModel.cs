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

/// <summary>Coordinates asset master data management in the WPF shell with SAP B1 style tooling.</summary>
/// <remarks>
/// Form Modes: Find filters asset records via CFL search, Add seeds a fresh <see cref="AssetEditor"/> with normalized status, View keeps the editor read-only, and Update enables editing plus attachment workflows.
/// Audit &amp; Logging: Persists assets through <see cref="IMachineCrudService"/> with enforced e-signature capture and signature metadata persisted via <see cref="SignaturePersistenceHelper"/>; retention and audit hashing are delegated to the CRUD and attachment workflow services.
/// Localization: Currently emits inline strings such as `"Assets"`, `"Select Asset"`, `"Filtered {Title} by"` and attachment status prompts pending RESX resource keys.
/// Navigation: ModuleKey `Assets` anchors shell docking, `CreateCflRequestAsync` and `OnCflSelectionAsync` power Choose-From-List / Golden Arrow routing, and `StatusMessage` updates inform the ribbon status bar during navigation and saves.
/// </remarks>
public sealed partial class AssetsModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Shell registration key for binding assets into the docked workspace.</summary>
    /// <remarks>Execution: Consumed during module catalog initialization so the shell can route to this view. Form Mode: Neutral identifier applied across Find/Add/View/Update. Localization: Currently coupled to the inline caption "Assets" awaiting `Modules_Assets_Title`.</remarks>
    public new const string ModuleKey = "Assets";

    private readonly IMachineCrudService _machineService;
    private readonly IAuthContext _authContext;
    private readonly IFilePicker _filePicker;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;
    private Machine? _loadedMachine;
    private AssetEditor? _snapshot;
    private bool _suppressEditorDirtyNotifications;

    /// <summary>Constructs the assets surface with CRUD, audit, attachment, and navigation services.</summary>
    /// <remarks>Execution: Invoked when the host resolves the module on shell start or when Golden Arrow requests activation. Form Mode: Seeds Find/View data immediately; Add/Update wiring occurs as mode changes flow through. Localization: Inline strings such as "Assets" and "Select Asset" remain until RESX resources are wired.</remarks>
    public AssetsModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IMachineCrudService machineService,
        IAuthContext authContext,
        IFilePicker filePicker,
        IAttachmentWorkflowService attachmentWorkflow,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "Assets", databaseService, cflDialogService, shellInteraction, navigation, auditService)
    {
        _machineService = machineService ?? throw new ArgumentNullException(nameof(machineService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));
        Editor = AssetEditor.CreateEmpty();
        StatusOptions = new ReadOnlyCollection<string>(new[]
        {
            "active",
            "maintenance",
            "reserved",
            "decommissioned",
            "scrapped"
        });

        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
        SummarizeWithAiCommand = new RelayCommand(OpenAiSummary);
        Toolbar.Add(new ModuleToolbarCommand("Summarize (AI)", SummarizeWithAiCommand));
    }

    /// <summary>Editor payload bound to the form controls for the current asset.</summary>
    /// <remarks>Execution: Updated during record selection and commit cycles. Form Mode: Editable during Add/Update, read-only snapshot in Find/View. Localization: Field headers currently use inline labels pending `AssetsEditor_*` resources.</remarks>
    [ObservableProperty]
    private AssetEditor _editor;

    /// <summary>Opens the AI module to summarize the selected asset.</summary>
    public IRelayCommand SummarizeWithAiCommand { get; }

    /// <summary>Flag indicating whether form controls accept user edits.</summary>
    /// <remarks>Execution: Toggled by <see cref="OnModeChangedAsync(FormMode)"/> as modes shift. Form Mode: True for Add/Update, false for Find/View. Localization: Bound to inline `IsEnabled` captions and tooltips until resources arrive.</remarks>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Canonical status options rendered in the status combo box.</summary>
    /// <remarks>Execution: Initialized during construction and reused across mode transitions. Form Mode: Options are selectable when Add/Update unlock fields; read-only otherwise. Localization: Uses inline lowercase status text pending resource mappings.</remarks>
    public IReadOnlyList<string> StatusOptions { get; }

    /// <summary>Command exposed on the ribbon to stage attachment uploads.</summary>
    /// <remarks>Execution: Fired when Upload is tapped; delegates to <see cref="AttachDocumentAsync"/>. Form Mode: Enabled only when Add/Update and the attachment workflow services are available. Localization: Button label/tooltips use inline strings until `Ribbon_Assets_Attach` resources exist.</remarks>
    public IAsyncRelayCommand AttachDocumentCommand { get; }

    /// <summary>Retrieves assets from the domain service for presentation.</summary>
    /// <remarks>Execution: Triggered by Find mode refreshes and shell reloads. Form Mode: Feeds Find/View lists; Add/Update reuse cached records. Localization: Status messaging uses inline phrases like "Filtered Assets" pending resource keys.</remarks>
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var machines = await _machineService.GetAllAsync().ConfigureAwait(false);
        return machines.Select(ToRecord).ToList();
    }

    /// <summary>Supplies design-time asset data for Blend previews.</summary>
    /// <remarks>Execution: Runs only in design contexts when `IsInDesignMode` is true. Form Mode: Emulates Find mode for tooling. Localization: Samples retain inline strings for preview clarity.</remarks>
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var sample = new[]
        {
            new Machine
            {
                Id = 1001,
                Name = "Autoclave",
                Code = "AUTO-001",
                Status = "active",
                Description = "Steam sterilizer",
                Manufacturer = "Steris",
                Location = "Building A",
                InstallDate = DateTime.UtcNow.AddYears(-3)
            },
            new Machine
            {
                Id = 1002,
                Name = "pH Meter",
                Code = "LAB-PH-12",
                Status = "maintenance",
                Description = "Metrohm pH meter",
                Manufacturer = "Metrohm",
                Location = "QC Lab",
                InstallDate = DateTime.UtcNow.AddYears(-2)
            }
        };

        return sample.Select(ToRecord).ToList();
    }

    /// <summary>Builds the Choose-From-List payload for asset navigation.</summary>
    /// <remarks>Execution: Invoked when the shell launches a CFL dialog or Golden Arrow, routing back through <see cref="ModuleKey"/> `"Assets"`. Form Mode: Aligns with Find mode search; available in all modes for navigation. Localization: Dialog title `"Select Asset"` and description tokens remain inline until `CFL_Assets_Select` resources are defined.</remarks>
    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var machines = await _machineService.GetAllAsync().ConfigureAwait(false);
        var items = machines
            .Select(machine =>
            {
                var key = machine.Id.ToString(CultureInfo.InvariantCulture);
                var label = string.IsNullOrWhiteSpace(machine.Name) ? key : machine.Name;
                var descriptionParts = new List<string>();
                if (!string.IsNullOrWhiteSpace(machine.Code))
                {
                    descriptionParts.Add(machine.Code);
                }

                if (!string.IsNullOrWhiteSpace(machine.Location))
                {
                    descriptionParts.Add(machine.Location!);
                }

                if (!string.IsNullOrWhiteSpace(machine.Status))
                {
                    descriptionParts.Add(machine.Status!);
                }

                var description = descriptionParts.Count > 0
                    ? string.Join(" • ", descriptionParts)
                    : null;

                return new CflItem(key, label, description);
            })
            .ToList();

        return new CflRequest(YasGMP.Wpf.Helpers.Loc.S("CFL_Select_Asset", "Select Asset"), items);
    }

    /// <summary>Applies the selected CFL result back to the asset list and inspector.</summary>
    /// <remarks>Execution: Runs immediately after a user confirms a CFL choice or Golden Arrow jump, updating shell routing for `ModuleKey` `"Assets"` via `StatusMessage`. Form Mode: Navigates records without altering edit mode; ensures Update is not interrupted. Localization: Writes status text like `"Filtered Assets"` pending ribbon resource strings.</remarks>
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
        StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Status_Assets_FilteredBy", "Filtered {0} by \"{1}\"."), Title, search);
        return Task.CompletedTask;
    }

    private void OpenAiSummary()
    {
        if (SelectedRecord is null && _loadedMachine is null)
        {
            StatusMessage = "Select an asset to summarize.";
            return;
        }

        var m = _loadedMachine;
        string prompt = m is null
            ? $"Summarize asset: {SelectedRecord?.Title}. Provide status, location, and maintenance risks in <= 8 bullets."
            : $"Summarize this asset (<= 8 bullets). Name={m.Name}; Code={m.Code}; Status={m.Status}; Location={m.Location}; Serial={m.SerialNumber}; Vendor={m.Manufacturer}; LastService={m.LastModified:yyyy-MM-dd}.";

        var shell = YasGMP.Common.ServiceLocator.GetRequiredService<IShellInteractionService>();
        var doc = shell.OpenModule(AiModuleViewModel.ModuleKey, $"prompt:{prompt}");
        shell.Activate(doc);
    }

    /// <summary>Loads or clears the editor based on selected module record.</summary>
    /// <remarks>Execution: Fired when the document host changes selection or the shell routes via Golden Arrow for `ModuleKey` `"Assets"`. Form Mode: Respects Add/Update guardrails to avoid clobbering unsaved edits. Localization: Uses inline error/status strings until `Status_Assets_*` resources exist.</remarks>
    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedMachine = null;
            SetEditor(AssetEditor.CreateEmpty());
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

        var machine = await _machineService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (machine is null)
        {
            StatusMessage = string.Format(YasGMP.Wpf.Helpers.Loc.S("Status_Assets_UnableToLocateById", "Unable to locate asset #{0}."), id);
            return;
        }

        _loadedMachine = machine;
        LoadEditor(machine);
        UpdateAttachmentCommandState();
    }

    /// <summary>Adjusts editor state and attachment tooling when the form mode changes.</summary>
    /// <remarks>Execution: Raised by the base form state machine whenever commands such as Add, Find, or Update fire. Form Mode: Enables editing only for Add/Update while snapshotting View states. Localization: Emits inline status messages (e.g., "Assets ready for update") until corresponding resources exist.</remarks>
    protected override Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                _snapshot = null;
                _loadedMachine = null;
                SetEditor(AssetEditor.CreateForNew(_machineService.NormalizeStatus("active")));
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

    /// <summary>Checks the current editor content for business rule compliance.</summary>
    /// <remarks>Execution: Invoked just before save/OK execution. Form Mode: Evaluated during Add/Update flows; bypassed during Find/View. Localization: Validation errors bubble up as inline strings pending resource coverage.</remarks>
    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var errors = new List<string>();
        try
        {
            var machine = Editor.ToMachine(_loadedMachine);
            machine.Status = _machineService.NormalizeStatus(machine.Status);
            _machineService.Validate(machine);
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

    /// <summary>Writes the current asset to the database and triggers e-signature capture.</summary>
    /// <remarks>Execution: Called after validation passes when OK/Update is committed. Form Mode: Only Add/Update reach this method. Localization: Success/failure strings use inline values such as `"Saved asset"` pending resources.</remarks>
    protected override async Task<bool> OnSaveAsync()
    {
        var machine = Editor.ToMachine(_loadedMachine);
        machine.Status = _machineService.NormalizeStatus(machine.Status);

        if (Mode == FormMode.Update && _loadedMachine is null)
        {
            StatusMessage = YasGMP.Wpf.Helpers.Loc.S("Status_Assets_SelectBeforeSave", "Select an asset before saving.");
            return false;
        }

        var recordId = Mode == FormMode.Update ? _loadedMachine!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("machines", recordId))
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

        var signature = signatureResult.Signature;
        var signerDisplayName = _authContext.CurrentUser?.FullName;
        if (string.IsNullOrWhiteSpace(signerDisplayName))
        {
            signerDisplayName = _authContext.CurrentUser?.Username ?? string.Empty;
        }

        machine.DigitalSignature = signature.SignatureHash ?? string.Empty;
        machine.LastModified = signature.SignedAt ?? DateTime.UtcNow;
        machine.LastModifiedById = signature.UserId != 0
            ? signature.UserId
            : _authContext.CurrentUser?.Id ?? machine.LastModifiedById;

        var context = MachineCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        Editor.SignatureHash = machine.DigitalSignature;
        Editor.SignatureReason = signatureResult.ReasonDisplay;
        Editor.SignatureNote = signature.Note ?? string.Empty;
        Editor.SignatureTimestampUtc = signature.SignedAt;
        Editor.SignerUserId = signature.UserId == 0 ? _authContext.CurrentUser?.Id : signature.UserId;
        Editor.SignerUserName = string.IsNullOrWhiteSpace(signature.UserName)
            ? signerDisplayName ?? string.Empty
            : signature.UserName;
        Editor.LastModifiedUtc = machine.LastModified;
        Editor.LastModifiedById = machine.LastModifiedById;
        Editor.LastModifiedByName = Editor.SignerUserName;
        Editor.SourceIp = signature.IpAddress ?? _authContext.CurrentIpAddress ?? string.Empty;
        Editor.SessionId = signature.SessionId ?? _authContext.CurrentSessionId ?? string.Empty;
        Editor.DeviceInfo = signature.DeviceInfo ?? _authContext.CurrentDeviceInfo ?? string.Empty;

        Machine adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _machineService.CreateAsync(machine, context).ConfigureAwait(false);
                machine.Id = saveResult.Id;
                adapterResult = machine;
            }
            else if (Mode == FormMode.Update)
            {
                machine.Id = _loadedMachine!.Id;
                saveResult = await _machineService.UpdateAsync(machine, context).ConfigureAwait(false);
                adapterResult = machine;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(string.Format(YasGMP.Wpf.Helpers.Loc.S("Error_Assets_SaveFailed", "Failed to persist asset: {0}"), ex.Message), ex);
        }

        if (saveResult.SignatureMetadata?.Id is { } signatureId)
        {
            adapterResult.DigitalSignatureId = signatureId;
        }

        _loadedMachine = machine;
        LoadEditor(machine);
        UpdateAttachmentCommandState();

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "machines",
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
        return true;
    }

    /// <summary>Restores the editor from the snapshot when the user cancels an edit.</summary>
    /// <remarks>Execution: Runs when Cancel is invoked mid Add/Update cycle. Form Mode: Specific to Add/Update, with no effect in Find/View. Localization: Uses inline status text like `"Changes discarded"` until an assets resource entry is added.</remarks>
    protected override void OnCancel()
    {
        if (Mode == FormMode.Add)
        {
            if (_loadedMachine is not null)
            {
                LoadEditor(_loadedMachine);
            }
            else
            {
                SetEditor(AssetEditor.CreateEmpty());
            }
        }
        else if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }

        UpdateAttachmentCommandState();
    }

    partial void OnEditorChanging(AssetEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(AssetEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged += OnEditorPropertyChanged;
    }

    /// <summary>Catches editor property updates to maintain dirty tracking and command state.</summary>
    /// <remarks>Execution: Triggered whenever a generated observable property setter raises change notifications. Form Mode: Relevant during Add/Update; suppressed otherwise via `_suppressEditorDirtyNotifications`. Localization: Downstream status updates remain inline until resources map them.</remarks>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is nameof(IsBusy) or nameof(Mode) or nameof(SelectedRecord) or nameof(IsDirty))
        {
            UpdateAttachmentCommandState();
        }
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

    private void LoadEditor(Machine machine)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = AssetEditor.FromMachine(machine, _machineService.NormalizeStatus);
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
        UpdateAttachmentCommandState();
    }

    private void SetEditor(AssetEditor editor)
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
           && _loadedMachine is not null
           && _loadedMachine.Id > 0;

    private async Task AttachDocumentAsync()
    {
        if (_loadedMachine is null || _loadedMachine.Id <= 0)
        {
            StatusMessage = "Save the asset before adding attachments.";
            return;
        }

        try
        {
            IsBusy = true;
            var files = await _filePicker.PickFilesAsync(
                    new FilePickerRequest(AllowMultiple: true, Title: $"Attach files to {_loadedMachine.Name}"))
                .ConfigureAwait(false);

            if (files is null || files.Count == 0)
            {
                StatusMessage = YasGMP.Wpf.Helpers.Loc.S("Status_Attach_Cancelled", "Attachment upload cancelled.");
                return;
            }

            var uploadedBy = _authContext.CurrentUser?.Id;
            var processed = 0;
            var deduplicated = 0;

            foreach (var file in files)
            {
                await using var stream = await file.OpenReadAsync().ConfigureAwait(false);
                var request = new AttachmentUploadRequest
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    EntityType = "machines",
                    EntityId = _loadedMachine.Id,
                    UploadedById = uploadedBy,
                    Reason = $"asset:{_loadedMachine.Id}",
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
        var app = System.Windows.Application.Current;
        YasGMP.Wpf.Helpers.UiCommandHelper.NotifyCanExecuteOnUi(AttachDocumentCommand);
    }

    private static ModuleRecord ToRecord(Machine machine)
    {
        var fields = new List<InspectorField>
        {
            new("Location", machine.Location ?? "-"),
            new("Model", machine.Model ?? "-"),
            new("Manufacturer", machine.Manufacturer ?? "-"),
            new("Status", machine.Status ?? "-"),
            new("Installed", machine.InstallDate?.ToString("d", CultureInfo.CurrentCulture) ?? "-")
        };

        return new ModuleRecord(
            machine.Id.ToString(CultureInfo.InvariantCulture),
            machine.Name,
            machine.Code,
            machine.Status,
            machine.Description,
            fields,
            WorkOrdersModuleViewModel.ModuleKey,
            machine.Id);
    }

    public sealed partial class AssetEditor : SignatureAwareEditor
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
        private string _model = string.Empty;

        [ObservableProperty]
        private string _manufacturer = string.Empty;

        [ObservableProperty]
        private string _location = string.Empty;

        [ObservableProperty]
        private string _status = "active";

        [ObservableProperty]
        private string _ursDoc = string.Empty;

        [ObservableProperty]
        private DateTime? _installDate = DateTime.UtcNow.Date;

        [ObservableProperty]
        private DateTime? _procurementDate;

        [ObservableProperty]
        private DateTime? _warrantyUntil;

        [ObservableProperty]
        private bool _isCritical;

        [ObservableProperty]
        private string _serialNumber = string.Empty;

        [ObservableProperty]
        private string _lifecyclePhase = string.Empty;

        [ObservableProperty]
        private string _notes = string.Empty;

        public static AssetEditor CreateEmpty() => new()
        {
            LastModifiedUtc = null,
            LastModifiedById = null,
            LastModifiedByName = string.Empty,
            SignatureHash = string.Empty,
            SignatureReason = string.Empty,
            SignatureNote = string.Empty,
            SignatureTimestampUtc = null,
            SignerUserId = null,
            SignerUserName = string.Empty,
            SourceIp = string.Empty,
            SessionId = string.Empty,
            DeviceInfo = string.Empty
        };

        public static AssetEditor CreateForNew(string normalizedStatus)
            => new()
            {
                Status = normalizedStatus,
                LastModifiedUtc = DateTime.UtcNow,
                LastModifiedById = null,
                LastModifiedByName = string.Empty,
                SignatureHash = string.Empty,
                SignatureReason = string.Empty,
                SignatureNote = string.Empty,
                SignatureTimestampUtc = null,
                SignerUserId = null,
                SignerUserName = string.Empty,
                SourceIp = string.Empty,
                SessionId = string.Empty,
                DeviceInfo = string.Empty
            };

        public static AssetEditor FromMachine(Machine machine, Func<string?, string> normalizer)
        {
            return new AssetEditor
            {
                Id = machine.Id,
                Code = machine.Code ?? string.Empty,
                Name = machine.Name ?? string.Empty,
                Description = machine.Description ?? string.Empty,
                Model = machine.Model ?? string.Empty,
                Manufacturer = machine.Manufacturer ?? string.Empty,
                Location = machine.Location ?? string.Empty,
                Status = normalizer(machine.Status),
                UrsDoc = machine.UrsDoc ?? string.Empty,
                InstallDate = machine.InstallDate,
                ProcurementDate = machine.ProcurementDate,
                WarrantyUntil = machine.WarrantyUntil,
                IsCritical = machine.IsCritical,
                SerialNumber = machine.SerialNumber ?? string.Empty,
                LifecyclePhase = machine.LifecyclePhase ?? string.Empty,
                Notes = machine.Note ?? string.Empty,
                SignatureHash = machine.DigitalSignature ?? string.Empty,
                LastModifiedUtc = machine.LastModified,
                LastModifiedById = machine.LastModifiedById,
                LastModifiedByName = machine.LastModifiedBy?.FullName ?? string.Empty,
                SignatureTimestampUtc = machine.LastModified,
                SignerUserId = machine.LastModifiedById,
                SignerUserName = machine.LastModifiedBy?.FullName ?? string.Empty
            };
        }

        public Machine ToMachine(Machine? existing)
        {
            var machine = existing is null ? new Machine() : CloneMachine(existing);
            machine.Id = Id;
            machine.Code = Code;
            machine.Name = Name;
            machine.Description = Description;
            machine.Model = Model;
            machine.Manufacturer = Manufacturer;
            machine.Location = Location;
            machine.Status = Status;
            machine.UrsDoc = UrsDoc;
            machine.InstallDate = InstallDate;
            machine.ProcurementDate = ProcurementDate;
            machine.WarrantyUntil = WarrantyUntil;
            machine.IsCritical = IsCritical;
            machine.SerialNumber = string.IsNullOrWhiteSpace(SerialNumber) ? machine.SerialNumber : SerialNumber;
            machine.LifecyclePhase = LifecyclePhase;
            machine.Note = Notes;
            machine.DigitalSignature = SignatureHash;
            machine.LastModified = LastModifiedUtc ?? DateTime.UtcNow;
            machine.LastModifiedById = LastModifiedById ?? machine.LastModifiedById;
            return machine;
        }

        public AssetEditor Clone()
            => new()
            {
                Id = Id,
                Code = Code,
                Name = Name,
                Description = Description,
                Model = Model,
                Manufacturer = Manufacturer,
                Location = Location,
                Status = Status,
                UrsDoc = UrsDoc,
                InstallDate = InstallDate,
                ProcurementDate = ProcurementDate,
                WarrantyUntil = WarrantyUntil,
                IsCritical = IsCritical,
                SerialNumber = SerialNumber,
                LifecyclePhase = LifecyclePhase,
                Notes = Notes,
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

        private static Machine CloneMachine(Machine source)
        {
            return new Machine
            {
                Id = source.Id,
                Code = source.Code,
                Name = source.Name,
                Description = source.Description,
                Model = source.Model,
                Manufacturer = source.Manufacturer,
                Location = source.Location,
                Status = source.Status,
                UrsDoc = source.UrsDoc,
                InstallDate = source.InstallDate,
                ProcurementDate = source.ProcurementDate,
                WarrantyUntil = source.WarrantyUntil,
                IsCritical = source.IsCritical,
                SerialNumber = source.SerialNumber,
                LifecyclePhase = source.LifecyclePhase,
                Note = source.Note
            };
        }
    }
}
