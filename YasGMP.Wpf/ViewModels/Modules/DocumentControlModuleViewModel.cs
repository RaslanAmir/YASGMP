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
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.Services.Interfaces;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Document Control module surfaced inside the WPF shell. Provides SAP B1 style form
/// modes, filtering, change-control linking and attachment workflow integration.
/// </summary>
public sealed partial class DocumentControlModuleViewModel : DataDrivenModuleDocumentViewModel
{
    /// <summary>Stable module key registered inside the module registry.</summary>
    public const string ModuleKey = "DocumentControl";

    private static readonly string[] DefaultStatuses =
    {
        "draft", "under review", "pending approval", "approved", "published", "expired", "obsolete", "archived"
    };

    private static readonly string[] DefaultTypes =
    {
        "SOP", "Policy", "Work Instruction", "Form", "Template", "Checklist", "Protocol", "Report", "Other"
    };

    private readonly DatabaseService _databaseService;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IFilePicker _filePicker;
    private readonly IAuthContext _authContext;

    private readonly Dictionary<string, SopDocument> _documentsByKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _statusLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _typeLookup = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<string> _statusOptions;
    private IReadOnlyList<string> _typeOptions;
    private SopDocument? _loadedDocument;
    private bool _suppressDirtyNotifications;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentControlModuleViewModel"/> class.
    /// </summary>
    public DocumentControlModuleViewModel(
        DatabaseService databaseService,
        AuditService auditService,
        IAttachmentWorkflowService attachmentWorkflow,
        IFilePicker filePicker,
        IAuthContext authContext,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        ILocalizationService localization)
        : base(ModuleKey, localization.GetString("Module.Title.DocumentControl", "Document Control"), databaseService, localization, cflDialogService, shellInteraction, navigation, auditService)
    {
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));

        _statusLookup.UnionWith(DefaultStatuses);
        _typeLookup.UnionWith(DefaultTypes);
        _statusOptions = DefaultStatuses;
        _typeOptions = DefaultTypes;

        AvailableChangeControls = new ObservableCollection<ChangeControlSummaryDto>();

        AttachDocumentCommand = new AsyncRelayCommand(AttachDocumentAsync, CanAttachDocument);
        OpenChangeControlPickerCommand = new AsyncRelayCommand(OpenChangeControlPickerAsync, CanOpenChangeControlPicker);
        LinkChangeControlCommand = new AsyncRelayCommand(LinkChangeControlAsync, CanLinkChangeControl);
        CancelChangeControlPickerCommand = new RelayCommand(CancelChangeControlPicker);

        SetEditor(DocumentControlEditor.CreateEmpty());

        PropertyChanged += OnPropertyChanged;
    }

    [ObservableProperty]
    private DocumentControlEditor _editor = null!;

    [ObservableProperty]
    private bool _isEditorEnabled;

    [ObservableProperty]
    private string? _statusFilter;

    [ObservableProperty]
    private string? _typeFilter;

    [ObservableProperty]
    private bool _isChangeControlPickerOpen;

    [ObservableProperty]
    private ChangeControlSummaryDto? _selectedChangeControlForLink;

    /// <summary>Toolbar command exposed for attachment uploads.</summary>
    public IAsyncRelayCommand AttachDocumentCommand { get; }

    /// <summary>Command that loads change controls and opens the picker overlay.</summary>
    public IAsyncRelayCommand OpenChangeControlPickerCommand { get; }

    /// <summary>Command that persists the link between the current document and a change control.</summary>
    public IAsyncRelayCommand LinkChangeControlCommand { get; }

    /// <summary>Command that closes the change control picker without linking.</summary>
    public IRelayCommand CancelChangeControlPickerCommand { get; }

    /// <summary>Change controls available for selection when linking.</summary>
    public ObservableCollection<ChangeControlSummaryDto> AvailableChangeControls { get; }

    /// <summary>Options surfaced in the status filter and editor combo box.</summary>
    public IReadOnlyList<string> AvailableStatuses => _statusOptions;

    /// <summary>Options surfaced in the document type filter and editor combo box.</summary>
    public IReadOnlyList<string> AvailableTypes => _typeOptions;

    /// <summary>Status options used by the editor form.</summary>
    public IReadOnlyList<string> StatusOptions => _statusOptions;

    /// <summary>Document type options used by the editor form.</summary>
    public IReadOnlyList<string> TypeOptions => _typeOptions;

    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        var documents = await _databaseService
            .GetAllDocumentsFullAsync()
            .ConfigureAwait(false);

        _documentsByKey.Clear();

        foreach (var doc in documents)
        {
            var status = NormalizeStatus(doc.Status);
            if (!string.IsNullOrWhiteSpace(status) && _statusLookup.Add(status))
            {
                _statusOptions = _statusLookup.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray();
            }

            var type = NormalizeType(doc.RelatedType);
            if (!string.IsNullOrWhiteSpace(type) && _typeLookup.Add(type))
            {
                _typeOptions = _typeLookup.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToArray();
            }
        }

        var records = documents
            .Select(ToRecord)
            .ToList();

        return ToReadOnlyList(records);
    }

    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var now = DateTime.Now;
        var demo = new List<SopDocument>
        {
            new()
            {
                Id = 1001,
                Code = "DOC-001",
                Name = "SOP: Clean Room Startup",
                Status = "published",
                VersionNo = 3,
                Description = "Startup and shutdown procedure for ISO7 clean rooms.",
                RelatedType = "SOP",
                ReviewNotes = "Linked to CC-2024-010",
                DateIssued = now.AddMonths(-6),
                DateExpiry = now.AddYears(1),
                LastModified = now.AddMonths(-1)
            },
            new()
            {
                Id = 1002,
                Code = "POL-014",
                Name = "Quality Policy",
                Status = "approved",
                VersionNo = 2,
                Description = "Corporate quality management policy statement.",
                RelatedType = "Policy",
                ReviewNotes = "Pending publication after management review.",
                DateIssued = now.AddMonths(-2),
                LastModified = now.AddDays(-10)
            }
        };

        return ToReadOnlyList(demo.Select(ToRecord));
    }

    protected override Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;
        UpdateCommandStates();
        return Task.CompletedTask;
    }

    protected override Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null || !_documentsByKey.TryGetValue(record.Key, out var document))
        {
            _loadedDocument = null;
            SetEditor(DocumentControlEditor.CreateEmpty());
            return Task.CompletedTask;
        }

        _loadedDocument = document;
        LoadEditor(document);
        return Task.CompletedTask;
    }

    protected override bool MatchesSearch(ModuleRecord record, string searchText)
    {
        if (!string.IsNullOrWhiteSpace(StatusFilter)
            && !string.Equals(record.Status, StatusFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(TypeFilter))
        {
            var typeField = record.InspectorFields.FirstOrDefault()?.Value;
            if (!string.Equals(typeField, TypeFilter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return base.MatchesSearch(record, searchText);
    }

    private ModuleRecord ToRecord(SopDocument document)
    {
        var key = document.Id > 0
            ? document.Id.ToString(CultureInfo.InvariantCulture)
            : (string.IsNullOrWhiteSpace(document.Code) ? Guid.NewGuid().ToString("N") : document.Code);

        _documentsByKey[key] = document;

        var inspector = new List<InspectorField>
        {
            InspectorField.Create(ModuleKey, Title, key, document.Name ?? document.Code, "Type", NormalizeType(document.RelatedType)),
            InspectorField.Create(ModuleKey, Title, key, document.Name ?? document.Code, "Version", document.VersionNo > 0 ? $"v{document.VersionNo}" : "v1"),
            InspectorField.Create(ModuleKey, Title, key, document.Name ?? document.Code, "Owner", document.ResponsibleUser?.FullName ?? document.Comment ?? string.Empty)
        };

        return new ModuleRecord(
            key,
            string.IsNullOrWhiteSpace(document.Name) ? document.Code : document.Name,
            document.Code,
            NormalizeStatus(document.Status),
            document.Description,
            inspector,
            ChangeControlModuleViewModel.ModuleKey,
            document.Comment);
    }

    private void LoadEditor(SopDocument document)
    {
        var editor = DocumentControlEditor.FromDocument(document, NormalizeStatus, NormalizeType);
        SetEditor(editor);
        Mode = FormMode.View;
    }

    private void SetEditor(DocumentControlEditor editor)
    {
        if (Editor is not null)
        {
            Editor.PropertyChanged -= OnEditorPropertyChanged;
        }

        _suppressDirtyNotifications = true;
        Editor = editor;
        Editor.PropertyChanged += OnEditorPropertyChanged;
        _suppressDirtyNotifications = false;
        ResetDirty();
        UpdateCommandStates();
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressDirtyNotifications)
        {
            return;
        }

        if (Mode is FormMode.Add or FormMode.Update)
        {
            MarkDirty();
        }
    }

    private bool CanAttachDocument()
        => !IsBusy
           && !IsEditorEnabled
           && _loadedDocument is { Id: > 0 };

    private async Task AttachDocumentAsync()
    {
        if (_loadedDocument is null || _loadedDocument.Id <= 0)
        {
            StatusMessage = "Select a saved document before attaching files.";
            return;
        }

        var files = await _filePicker
            .PickFilesAsync(new FilePickerRequest(AllowMultiple: true, Title: $"Attach files to {_loadedDocument.Code}"))
            .ConfigureAwait(false);

        if (files is null || files.Count == 0)
        {
            StatusMessage = "Attachment upload cancelled.";
            return;
        }

        var processed = 0;
        var deduplicated = 0;

        foreach (var file in files)
        {
            await using var stream = await file.OpenReadAsync().ConfigureAwait(false);
            var request = new AttachmentUploadRequest
            {
                EntityType = "documentcontrol",
                EntityId = _loadedDocument.Id,
                FileName = file.FileName,
                ContentType = file.ContentType,
                UploadedById = _authContext.CurrentUser?.Id,
                SourceIp = _authContext.CurrentIpAddress,
                SourceHost = _authContext.CurrentDeviceInfo,
                Reason = "Document control attachment"
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

    private bool CanOpenChangeControlPicker()
        => !IsBusy
           && !IsEditorEnabled
           && _loadedDocument is { Id: > 0 };

    private async Task OpenChangeControlPickerAsync()
    {
        if (_loadedDocument is null || _loadedDocument.Id <= 0)
        {
            StatusMessage = "Save the document before linking change controls.";
            return;
        }

        try
        {
            IsBusy = true;
            var rows = await _databaseService.GetChangeControlsAsync().ConfigureAwait(false);
            AvailableChangeControls.Clear();
            foreach (var row in rows)
            {
                AvailableChangeControls.Add(row);
            }

            SelectedChangeControlForLink = null;
            IsChangeControlPickerOpen = true;
            StatusMessage = "Select a change control to link.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load change controls: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            UpdateCommandStates();
        }
    }

    private bool CanLinkChangeControl()
        => !IsBusy
           && IsChangeControlPickerOpen
           && _loadedDocument is { Id: > 0 }
           && SelectedChangeControlForLink is not null;

    private async Task LinkChangeControlAsync()
    {
        if (!CanLinkChangeControl())
        {
            return;
        }

        try
        {
            IsBusy = true;
            var actor = _authContext.CurrentUser?.Id ?? 0;
            var ip = _authContext.CurrentIpAddress ?? string.Empty;
            var device = _authContext.CurrentDeviceInfo ?? string.Empty;
            await _databaseService
                .LinkChangeControlToDocumentAsync(_loadedDocument!.Id, SelectedChangeControlForLink!.Id, actor, ip, device)
                .ConfigureAwait(false);

            StatusMessage = $"Linked change control {SelectedChangeControlForLink.Code} to {_loadedDocument.Code}.";
            IsChangeControlPickerOpen = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to link change control: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            UpdateCommandStates();
        }
    }

    private void CancelChangeControlPicker()
    {
        IsChangeControlPickerOpen = false;
        SelectedChangeControlForLink = null;
        UpdateCommandStates();
    }

    private void UpdateCommandStates()
    {
        if (AttachDocumentCommand is AsyncRelayCommand attach)
        {
            attach.NotifyCanExecuteChanged();
        }

        if (OpenChangeControlPickerCommand is AsyncRelayCommand open)
        {
            open.NotifyCanExecuteChanged();
        }

        if (LinkChangeControlCommand is AsyncRelayCommand link)
        {
            link.NotifyCanExecuteChanged();
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(IsBusy), StringComparison.Ordinal)
            || string.Equals(e.PropertyName, nameof(IsEditorEnabled), StringComparison.Ordinal)
            || string.Equals(e.PropertyName, nameof(IsChangeControlPickerOpen), StringComparison.Ordinal)
            || string.Equals(e.PropertyName, nameof(SelectedChangeControlForLink), StringComparison.Ordinal))
        {
            UpdateCommandStates();
        }
    }

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "draft";
        }

        return status.Trim();
    }

    private static string NormalizeType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return DefaultTypes[0];
        }

        return type.Trim();
    }

    partial void OnStatusFilterChanged(string? value)
    {
        RecordsView.Refresh();
    }

    partial void OnTypeFilterChanged(string? value)
    {
        RecordsView.Refresh();
    }
}

/// <summary>
/// Editor surface backing the Document Control detail pane.
/// </summary>
public sealed partial class DocumentControlEditor : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _status = "draft";

    [ObservableProperty]
    private string _documentType = "SOP";

    [ObservableProperty]
    private string _versionNumber = "1";

    [ObservableProperty]
    private DateTime? _effectiveDate;

    [ObservableProperty]
    private DateTime? _expirationDate;

    [ObservableProperty]
    private string _owner = string.Empty;

    [ObservableProperty]
    private string _reviewNotes = string.Empty;

    [ObservableProperty]
    private string _summary = string.Empty;

    [ObservableProperty]
    private string _signatureHash = string.Empty;

    [ObservableProperty]
    private string _signatureReason = string.Empty;

    [ObservableProperty]
    private string _signatureNote = string.Empty;

    [ObservableProperty]
    private string _signerUserName = string.Empty;

    [ObservableProperty]
    private string _signerUserId = string.Empty;

    [ObservableProperty]
    private DateTime? _signatureTimestampUtc;

    [ObservableProperty]
    private string _sourceIp = string.Empty;

    [ObservableProperty]
    private string _sessionId = string.Empty;

    [ObservableProperty]
    private string _deviceInfo = string.Empty;

    [ObservableProperty]
    private DateTime? _lastModifiedUtc;

    [ObservableProperty]
    private string _lastModifiedByName = string.Empty;

    /// <summary>Creates an editor with default values.</summary>
    public static DocumentControlEditor CreateEmpty() => new();

    /// <summary>Creates an editor populated from a persisted document.</summary>
    public static DocumentControlEditor FromDocument(
        SopDocument document,
        Func<string?, string> normalizeStatus,
        Func<string?, string> normalizeType)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return new DocumentControlEditor
        {
            Id = document.Id,
            Code = document.Code ?? string.Empty,
            Title = document.Name ?? string.Empty,
            Status = normalizeStatus(document.Status),
            DocumentType = normalizeType(document.RelatedType),
            VersionNumber = document.VersionNo > 0 ? document.VersionNo.ToString(CultureInfo.InvariantCulture) : "1",
            EffectiveDate = document.DateIssued == default ? (DateTime?)null : document.DateIssued,
            ExpirationDate = document.DateExpiry,
            Owner = document.ResponsibleUser?.FullName ?? string.Empty,
            ReviewNotes = document.ReviewNotes ?? string.Empty,
            Summary = document.Description ?? string.Empty,
            SignatureHash = document.DigitalSignature ?? string.Empty,
            SignatureReason = string.Empty,
            SignatureNote = document.Comment ?? string.Empty,
            SignerUserName = document.Approvers.FirstOrDefault()?.FullName ?? string.Empty,
            SignerUserId = document.Approvers.FirstOrDefault()?.Id.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            SignatureTimestampUtc = document.ApprovalTimestamps.FirstOrDefault(),
            SourceIp = document.SourceIp ?? string.Empty,
            SessionId = string.Empty,
            DeviceInfo = document.ChainHash ?? string.Empty,
            LastModifiedUtc = document.LastModified == default ? (DateTime?)null : document.LastModified,
            LastModifiedByName = document.LastModifiedBy?.FullName ?? string.Empty
        };
    }
}
