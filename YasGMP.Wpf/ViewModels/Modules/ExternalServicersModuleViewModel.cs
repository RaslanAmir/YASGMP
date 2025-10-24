using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Models;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Dialogs;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>Maintains external servicer vendors within the WPF SAP B1 shell.</summary>
/// <remarks>
/// Form Modes: Find searches vendor roster, Add provisions <see cref="ExternalServicerEditor.CreateEmpty"/>, View locks the fields, and Update enables editing with status/service-type picklists.
/// Audit &amp; Logging: Persists through <see cref="IExternalServicerCrudService"/> while enforcing electronic signatures; vendor audit history remains in the service layer.
/// Localization: Uses inline strings such as `"External Servicers"`, `"Attachment upload failed"`, and status prompts; localisation keys have not yet been plumbed.
/// Navigation: ModuleKey `ExternalServicers` keeps shell docking aligned; CFL overrides feed Choose-From-List dialogs and Golden Arrow links from other modules back to vendor records, with status messages updating the ribbon.
/// </remarks>
public sealed partial class ExternalServicersModuleViewModel : ModuleDocumentViewModel
{
    /// <summary>Shell registration key that binds External Servicers into the docking layout.</summary>
    /// <remarks>Execution: Resolved when the shell composes modules and persists layouts. Form Mode: Identifier applies across Find/Add/View/Update. Localization: Currently paired with the inline caption "External Servicers" until `Modules_ExternalServicers_Title` is introduced.</remarks>
    public new const string ModuleKey = "ExternalServicers";

    private static readonly IReadOnlyList<string> DefaultStatusOptions = new ReadOnlyCollection<string>(new[]
    {
        "Active",
        "Pending",
        "Suspended",
        "Expired",
        "On Hold"
    });

    private static readonly IReadOnlyList<string> DefaultServiceTypeOptions = new ReadOnlyCollection<string>(new[]
    {
        "Calibration",
        "Maintenance",
        "Validation",
        "Laboratory",
        "Audit",
        "IT Services",
        "Logistics"
    });

    private readonly IExternalServicerCrudService _servicerService;
    private readonly IAuthContext _authContext;
    private readonly IElectronicSignatureDialogService _signatureDialog;

    private ExternalServicer? _loadedServicer;
    private ExternalServicerEditor? _snapshot;
    private bool _suppressDirtyNotifications;
    private int? _lastSavedServicerId;

    /// <summary>Initializes the External Servicers module view model with domain and shell services.</summary>
    /// <remarks>Execution: Invoked when the shell activates the module or Golden Arrow navigation materializes it. Form Mode: Seeds Find/View immediately while deferring Add/Update wiring to later transitions. Localization: Relies on inline strings for tab titles and prompts until module resources exist.</remarks>
    public ExternalServicersModuleViewModel(
        IExternalServicerCrudService servicerService,
        IAuthContext authContext,
        IElectronicSignatureDialogService signatureDialog,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, "External Servicers", cflDialogService, shellInteraction, navigation)
    {
        _servicerService = servicerService ?? throw new ArgumentNullException(nameof(servicerService));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));

        Editor = ExternalServicerEditor.CreateEmpty();
        StatusOptions = DefaultStatusOptions;
        ServiceTypeOptions = DefaultServiceTypeOptions;
        SummarizeWithAiCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(OpenAiSummary);
        Toolbar.Add(new ModuleToolbarCommand("Summarize (AI)", SummarizeWithAiCommand));
    }

    /// <summary>Generated property exposing the editor for the External Servicers module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_ExternalServicers_Editor` resources are available.</remarks>
    [ObservableProperty]
    private ExternalServicerEditor _editor;

    /// <summary>Generated property exposing the is editor enabled for the External Servicers module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_ExternalServicers_IsEditorEnabled` resources are available.</remarks>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Generated property exposing the status options for the External Servicers module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_ExternalServicers_StatusOptions` resources are available.</remarks>
    [ObservableProperty]
    private IReadOnlyList<string> _statusOptions;

    /// <summary>Generated property exposing the service type options for the External Servicers module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_ExternalServicers_ServiceTypeOptions` resources are available.</remarks>
    [ObservableProperty]
    private IReadOnlyList<string> _serviceTypeOptions;

    /// <summary>Opens the AI module to summarize the selected external servicer (vendor/lab).</summary>
    public CommunityToolkit.Mvvm.Input.IRelayCommand SummarizeWithAiCommand { get; }

    private void OpenAiSummary()
    {
        if (SelectedRecord is null && _loadedServicer is null)
        {
            StatusMessage = "Select a servicer to summarize.";
            return;
        }

        var s = _loadedServicer;
        string prompt;
        if (s is null)
        {
            prompt = $"Summarize external servicer: {SelectedRecord?.Title}. Provide status/risks/contracts and next steps in <= 8 bullets.";
        }
        else
        {
            prompt = $"Summarize this external servicer (<= 8 bullets). Name={s.Name}; Code={s.Code}; Type={s.Type}; Status={s.Status}; Contact={s.ContactPerson}; Email={s.Email}; Phone={s.Phone}; Cooperation={s.CooperationStart:yyyy-MM-dd}..{s.CooperationEnd:yyyy-MM-dd}; Notes={s.Comment}.";
        }

        var shell = YasGMP.Common.ServiceLocator.GetRequiredService<IShellInteractionService>();
        var doc = shell.OpenModule(AiModuleViewModel.ModuleKey, $"prompt:{prompt}");
        shell.Activate(doc);
    }

    /// <summary>Loads External Servicers records from domain services.</summary>
    /// <remarks>Execution: Triggered by Find refreshes and shell activation. Form Mode: Supplies data for Find/View while Add/Update reuse cached results. Localization: Emits inline status strings pending `Status_ExternalServicers_Loaded` resources.</remarks>
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var servicers = await _servicerService.GetAllAsync().ConfigureAwait(false);
        var ordered = servicers
            .OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(s => s.Id)
            .Select(ToRecord)
            .ToList();

        if (_lastSavedServicerId.HasValue)
        {
            var savedKey = _lastSavedServicerId.Value.ToString(CultureInfo.InvariantCulture);
            var index = ordered.FindIndex(r => r.Key == savedKey);
            if (index > 0)
            {
                var match = ordered[index];
                ordered.RemoveAt(index);
                ordered.Insert(0, match);
            }

            _lastSavedServicerId = null;
        }

        return ordered;
    }

    /// <summary>Provides design-time sample data for the External Servicers designer experience.</summary>
    /// <remarks>Execution: Invoked only by design-mode checks to support Blend/preview tooling. Form Mode: Mirrors Find mode to preview list layouts. Localization: Sample literals remain inline for clarity.</remarks>
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var sample = new[]
        {
            new ExternalServicer
            {
                Id = 1,
                Name = "Contoso Calibration",
                Code = "EXT-001",
                Type = "Calibration",
                Status = "active",
                ContactPerson = "Ivana Horvat",
                Email = "calibration@contoso.example",
                Phone = "+385 91 111 222",
                Comment = "ISO 17025 accredited laboratory"
            },
            new ExternalServicer
            {
                Id = 2,
                Name = "Globex Maintenance",
                Code = "EXT-002",
                Type = "Maintenance",
                Status = "suspended",
                ContactPerson = "Marko BariÄ‡",
                Email = "support@globex.example",
                Phone = "+385 91 555 666",
                Comment = "Pending contract renewal"
            }
        };

        return sample.Select(ToRecord).ToList();
    }
    /// <summary>Executes the on activated async routine for the External Servicers module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    protected override Task OnActivatedAsync(object? parameter)
    {
        if (parameter is null)
        {
            return Task.CompletedTask;
        }

        string key = parameter switch
        {
            int id => id.ToString(CultureInfo.InvariantCulture),
            string text => text,
            _ => parameter.ToString() ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(key))
        {
            return Task.CompletedTask;
        }

        var match = Records.FirstOrDefault(r =>
            string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase)
            || string.Equals(r.Title, key, StringComparison.OrdinalIgnoreCase)
            || string.Equals(r.Code ?? string.Empty, key, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            SelectedRecord = match;
        }

        return Task.CompletedTask;
    }

    /// <summary>Loads editor payloads for the selected External Servicers record.</summary>
    /// <remarks>Execution: Triggered when document tabs change or shell routing targets `ModuleKey` "ExternalServicers". Form Mode: Honors Add/Update safeguards to avoid overwriting dirty state. Localization: Inline status/error strings remain until `Status_ExternalServicers` resources are available.</remarks>
    protected override async Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _loadedServicer = null;
            SetEditor(ExternalServicerEditor.CreateEmpty());
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

        var entity = await _servicerService.TryGetByIdAsync(id).ConfigureAwait(false);
        if (entity is null)
        {
            StatusMessage = $"External servicer #{id} could not be located.";
            return;
        }

        _loadedServicer = entity;
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
                _loadedServicer = null;
                SetEditor(ExternalServicerEditor.CreateForNew());
                break;
            case FormMode.Update:
                if (_loadedServicer is not null)
                {
                    _snapshot = Editor.Clone();
                }
                break;
            case FormMode.View:
            case FormMode.Find:
                _snapshot = null;
                break;
        }

        return Task.CompletedTask;
    }

    /// <summary>Validates the current editor payload before persistence.</summary>
    /// <remarks>Execution: Invoked immediately prior to OK/Update actions. Form Mode: Only Add/Update trigger validation. Localization: Error messages flow from inline literals until validation resources are added.</remarks>
    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var errors = new List<string>();

        try
        {
            var draft = Editor.ToServicer(_loadedServicer);
            _servicerService.Validate(draft);
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
        }

        if (!string.IsNullOrWhiteSpace(Editor.Email)
            && !Editor.Email.Contains('@', StringComparison.Ordinal))
        {
            errors.Add("Email address must contain '@'.");
        }

        return await Task.FromResult<IReadOnlyList<string>>(errors).ConfigureAwait(false);
    }

    /// <summary>Persists the current record and coordinates signatures, attachments, and audits.</summary>
    /// <remarks>Execution: Runs after validation when OK/Update is confirmed. Form Mode: Exclusive to Add/Update operations. Localization: Success/failure messaging remains inline pending dedicated resources.</remarks>
    protected override async Task<bool> OnSaveAsync()
    {
        if (Mode == FormMode.Update && _loadedServicer is null)
        {
            StatusMessage = "Select an external servicer before saving.";
            return false;
        }

        var servicer = Editor.ToServicer(_loadedServicer);
        servicer.Status = _servicerService.NormalizeStatus(servicer.Status);

        var recordId = Mode == FormMode.Update ? _loadedServicer!.Id : 0;
        ElectronicSignatureDialogResult? signatureResult;
        try
        {
            signatureResult = await _signatureDialog
                .CaptureSignatureAsync(new ElectronicSignatureContext("external_contractors", recordId))
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

        servicer.DigitalSignature = signatureResult.Signature.SignatureHash ?? string.Empty;
        servicer.LastModified = DateTime.UtcNow;
        servicer.LastModifiedById = _authContext.CurrentUser?.Id ?? servicer.LastModifiedById;
        servicer.SourceIp = _authContext.CurrentIpAddress ?? servicer.SourceIp ?? string.Empty;

        var context = ExternalServicerCrudContext.Create(
            _authContext.CurrentUser?.Id ?? 0,
            _authContext.CurrentIpAddress,
            _authContext.CurrentDeviceInfo,
            _authContext.CurrentSessionId,
            signatureResult);

        ExternalServicer adapterResult;
        CrudSaveResult saveResult;
        try
        {
            if (Mode == FormMode.Add)
            {
                saveResult = await _servicerService.CreateAsync(servicer, context).ConfigureAwait(false);
                servicer.Id = saveResult.Id;
                adapterResult = servicer;
            }
            else if (Mode == FormMode.Update)
            {
                servicer.Id = _loadedServicer!.Id;
                saveResult = await _servicerService.UpdateAsync(servicer, context).ConfigureAwait(false);
                adapterResult = servicer;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to persist external servicer: {ex.Message}", ex);
        }

        _loadedServicer = servicer;
        _lastSavedServicerId = servicer.Id;
        LoadEditor(servicer);

        SignaturePersistenceHelper.ApplyEntityMetadata(
            signatureResult,
            tableName: "external_contractors",
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
            if (_loadedServicer is not null)
            {
                LoadEditor(_loadedServicer);
            }
            else
            {
                SetEditor(ExternalServicerEditor.CreateEmpty());
            }
        }
        else if (Mode == FormMode.Update && _snapshot is not null)
        {
            SetEditor(_snapshot.Clone());
        }
    }

    /// <summary>Builds the Choose-From-List request used for Golden Arrow navigation.</summary>
    /// <remarks>Execution: Called when the shell launches CFL dialogs, routing via `ModuleKey` "ExternalServicers". Form Mode: Provides lookup data irrespective of current mode. Localization: Dialog titles and descriptions use inline strings until `CFL_ExternalServicers` resources exist.</remarks>
    protected override async Task<CflRequest?> CreateCflRequestAsync()
    {
        var servicers = await _servicerService.GetAllAsync().ConfigureAwait(false);
        var items = servicers.Select(servicer =>
        {
            var key = servicer.Id.ToString(CultureInfo.InvariantCulture);
            var descriptionParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(servicer.Type))
            {
                descriptionParts.Add(servicer.Type!);
            }

            if (!string.IsNullOrWhiteSpace(servicer.Status))
            {
                descriptionParts.Add(servicer.Status!);
            }

            if (!string.IsNullOrWhiteSpace(servicer.ContactPerson))
            {
                descriptionParts.Add(servicer.ContactPerson!);
            }

            var description = descriptionParts.Count > 0 ? string.Join(" â€˘ ", descriptionParts) : null;
            return new CflItem(key, servicer.Name, description);
        }).ToList();

        return new CflRequest("Select External Servicer", items);
    }

    /// <summary>Applies CFL selections back into the External Servicers workspace.</summary>
    /// <remarks>Execution: Runs after CFL or Golden Arrow completion, updating `StatusMessage` for `ModuleKey` "ExternalServicers". Form Mode: Navigates records without disturbing active edits. Localization: Status feedback uses inline phrases pending `Status_ExternalServicers_Filtered`.</remarks>
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

        StatusMessage = $"Filtered external servicers by \"{SearchText}\".";
        return Task.CompletedTask;
    }

    /// <summary>Executes the matches search routine for the External Servicers module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    protected override bool MatchesSearch(ModuleRecord record, string searchText)
    {
        if (base.MatchesSearch(record, searchText))
        {
            return true;
        }

        return record.InspectorFields.Any(field =>
            field.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    partial void OnEditorChanging(ExternalServicerEditor value)
    {
        if (value is null)
        {
            return;
        }

        value.PropertyChanged -= OnEditorPropertyChanged;
    }

    partial void OnEditorChanged(ExternalServicerEditor value)
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

    private void LoadEditor(ExternalServicer servicer)
    {
        _suppressDirtyNotifications = true;
        Editor = ExternalServicerEditor.FromServicer(servicer);
        _suppressDirtyNotifications = false;
        ResetDirty();
    }

    private void SetEditor(ExternalServicerEditor editor)
    {
        _suppressDirtyNotifications = true;
        Editor = editor;
        _suppressDirtyNotifications = false;
        ResetDirty();
    }

    private static ModuleRecord ToRecord(ExternalServicer servicer)
    {
        var inspector = new List<InspectorField>
        {
            new("Type", string.IsNullOrWhiteSpace(servicer.Type) ? "-" : servicer.Type!),
            new("Contact", string.IsNullOrWhiteSpace(servicer.ContactPerson) ? "-" : servicer.ContactPerson!),
            new("Email", string.IsNullOrWhiteSpace(servicer.Email) ? "-" : servicer.Email!),
            new("Phone", string.IsNullOrWhiteSpace(servicer.Phone) ? "-" : servicer.Phone!)
        };

        var relatedParameter = !string.IsNullOrWhiteSpace(servicer.VatOrId)
            ? servicer.VatOrId
            : servicer.Name;

        return new ModuleRecord(
            servicer.Id.ToString(CultureInfo.InvariantCulture),
            servicer.Name,
            servicer.Code,
            servicer.Status,
            servicer.Comment,
            inspector,
            SuppliersModuleViewModel.ModuleKey,
            relatedParameter);
    }
}

public sealed partial class ExternalServicerEditor : ObservableObject
{
    /// <summary>Generated property exposing the id for the External Servicers module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_ExternalServicers_Id` resources are available.</remarks>
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _status = "Active";

    [ObservableProperty]
    private string _type = string.Empty;

    [ObservableProperty]
    private string _vatOrId = string.Empty;

    [ObservableProperty]
    private string _contactPerson = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    /// <summary>Generated property exposing the cooperation start for the External Servicers module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_ExternalServicers_CooperationStart` resources are available.</remarks>
    [ObservableProperty]
    private DateTime? _cooperationStart;

    /// <summary>Generated property exposing the cooperation end for the External Servicers module.</summary>
    /// <remarks>Execution: Set during data loads and user edits with notifications raised by the source generators. Form Mode: Bound in Add/Update while rendered read-only for Find/View. Localization: Field labels remain inline until `Modules_ExternalServicers_CooperationEnd` resources are available.</remarks>
    [ObservableProperty]
    private DateTime? _cooperationEnd;

    [ObservableProperty]
    private string _comment = string.Empty;

    [ObservableProperty]
    private string _extraNotes = string.Empty;

    [ObservableProperty]
    private string _digitalSignature = string.Empty;

    [ObservableProperty]
    private string _certificateFiles = string.Empty;

    /// <summary>Executes the create empty routine for the External Servicers module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public static ExternalServicerEditor CreateEmpty() => new();

    /// <summary>Executes the create for new routine for the External Servicers module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public static ExternalServicerEditor CreateForNew()
        => new()
        {
            Status = "Active",
            CooperationStart = DateTime.UtcNow.Date
        };

    /// <summary>Executes the from servicer routine for the External Servicers module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public static ExternalServicerEditor FromServicer(ExternalServicer servicer)
        => new()
        {
            Id = servicer.Id,
            Name = servicer.Name ?? string.Empty,
            Code = servicer.Code ?? string.Empty,
            Status = string.IsNullOrWhiteSpace(servicer.Status)
                ? "Active"
                : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(servicer.Status),
            Type = servicer.Type ?? string.Empty,
            VatOrId = servicer.VatOrId ?? string.Empty,
            ContactPerson = servicer.ContactPerson ?? string.Empty,
            Email = servicer.Email ?? string.Empty,
            Phone = servicer.Phone ?? string.Empty,
            Address = servicer.Address ?? string.Empty,
            CooperationStart = servicer.CooperationStart,
            CooperationEnd = servicer.CooperationEnd,
            Comment = servicer.Comment ?? string.Empty,
            ExtraNotes = servicer.ExtraNotes ?? string.Empty,
            DigitalSignature = servicer.DigitalSignature ?? string.Empty,
            CertificateFiles = servicer.CertificateFiles.Count == 0
                ? string.Empty
                : string.Join(Environment.NewLine, servicer.CertificateFiles)
        };

    /// <summary>Executes the clone routine for the External Servicers module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public ExternalServicerEditor Clone()
        => new()
        {
            Id = Id,
            Name = Name,
            Code = Code,
            Status = Status,
            Type = Type,
            VatOrId = VatOrId,
            ContactPerson = ContactPerson,
            Email = Email,
            Phone = Phone,
            Address = Address,
            CooperationStart = CooperationStart,
            CooperationEnd = CooperationEnd,
            Comment = Comment,
            ExtraNotes = ExtraNotes,
            DigitalSignature = DigitalSignature,
            CertificateFiles = CertificateFiles
        };

    /// <summary>Executes the to servicer routine for the External Servicers module.</summary>
    /// <remarks>Execution: Part of the module lifecycle. Form Mode: Applies as dictated by the calling sequence. Localization: Emits inline text pending localized resources.</remarks>
    public ExternalServicer ToServicer(ExternalServicer? existing)
    {
        var target = existing is null ? new ExternalServicer() : CloneServicer(existing);
        target.Id = Id;
        target.Name = Name?.Trim() ?? string.Empty;
        target.Code = Code?.Trim();
        target.Status = Status?.Trim() ?? string.Empty;
        target.Type = Type?.Trim();
        target.VatOrId = VatOrId?.Trim();
        target.ContactPerson = ContactPerson?.Trim();
        target.Email = Email?.Trim();
        target.Phone = Phone?.Trim();
        target.Address = Address?.Trim();
        target.CooperationStart = CooperationStart;
        target.CooperationEnd = CooperationEnd;
        target.Comment = Comment?.Trim();
        target.ExtraNotes = ExtraNotes?.Trim();
        target.DigitalSignature = DigitalSignature?.Trim();
        target.CertificateFiles = ParseCertificates(CertificateFiles);
        return target;
    }

    private static ExternalServicer CloneServicer(ExternalServicer source)
    {
        return new ExternalServicer
        {
            Id = source.Id,
            Name = source.Name,
            Code = source.Code,
            Status = source.Status,
            Type = source.Type,
            VatOrId = source.VatOrId,
            ContactPerson = source.ContactPerson,
            Email = source.Email,
            Phone = source.Phone,
            Address = source.Address,
            CooperationStart = source.CooperationStart,
            CooperationEnd = source.CooperationEnd,
            Comment = source.Comment,
            ExtraNotes = source.ExtraNotes,
            DigitalSignature = source.DigitalSignature,
            CertificateFiles = new List<string>(source.CertificateFiles)
        };
    }

    private static List<string> ParseCertificates(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        var separators = new[] { '\n', '\r', ';', ',' };
        return text.Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();
    }
}







