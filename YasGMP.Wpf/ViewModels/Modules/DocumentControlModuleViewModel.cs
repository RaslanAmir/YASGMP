using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using YasGMP.AppCore.Models.Signatures;
using YasGMP.Models;
using YasGMP.Models.DTO;
using YasGMP.Services.Interfaces;
using YasGMP.ViewModels;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Bridges the shared MAUI document control workflow into the WPF shell while preserving SAP B1 semantics.
/// </summary>
public sealed partial class DocumentControlModuleViewModel : ModuleDocumentViewModel
{
    /// <summary>Stable module key consumed by the shell.</summary>
    public const string ModuleKey = "DocumentControl";

    private readonly DocumentControlViewModel _documentControl;
    private readonly ILocalizationService _localization;
    private readonly IAttachmentWorkflowService _attachmentWorkflow;
    private readonly IElectronicSignatureDialogService _signatureDialog;

    private ObservableCollection<SopDocument>? _filteredDocuments;
    private bool _suppressSelectionSync;
    private bool _suppressSearchSync;
    private bool _suppressEditorDirtyNotifications;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentControlModuleViewModel"/> class.
    /// </summary>
    public DocumentControlModuleViewModel(
        DocumentControlViewModel documentControl,
        ILocalizationService localization,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation,
        IAttachmentWorkflowService attachmentWorkflow,
        IElectronicSignatureDialogService signatureDialog)
        : base(ModuleKey, localization.GetString("Module.Title.DocumentControl"), localization, cflDialogService, shellInteraction, navigation)
    {
        _documentControl = documentControl ?? throw new ArgumentNullException(nameof(documentControl));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _attachmentWorkflow = attachmentWorkflow ?? throw new ArgumentNullException(nameof(attachmentWorkflow));
        _signatureDialog = signatureDialog ?? throw new ArgumentNullException(nameof(signatureDialog));

        DocumentControl = _documentControl;

        AttachDocumentCommand = new AsyncRelayCommand(ExecuteAttachDocumentAsync, CanAttachDocument);

        Editor = DocumentControlEditor.CreateEmpty();
        IsEditorEnabled = false;

        HookDocumentControl();
        ProjectRecordsIntoShell();
    }

    /// <summary>Gets the shared MAUI document control view-model.</summary>
    public DocumentControlViewModel DocumentControl { get; }

    /// <summary>Editor projection surfaced to the WPF view.</summary>
    [ObservableProperty]
    private DocumentControlEditor _editor;

    /// <summary>Indicates whether editor fields are enabled for edit operations.</summary>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Toolbar command surfaced for attachment uploads.</summary>
    public IAsyncRelayCommand AttachDocumentCommand { get; }

    /// <summary>Available status values propagated from the shared view-model.</summary>
    public string[] StatusOptions => DocumentControl.AvailableStatuses;

    /// <summary>Available document types propagated from the shared view-model.</summary>
    public string[] TypeOptions => DocumentControl.AvailableTypes;

    /// <summary>Status filter forwarded to the shared MAUI view-model.</summary>
    public string? StatusFilter
    {
        get => DocumentControl.StatusFilter;
        set
        {
            if (!string.Equals(DocumentControl.StatusFilter, value, StringComparison.Ordinal))
            {
                DocumentControl.StatusFilter = value;
                OnPropertyChanged();
                ProjectRecordsIntoShell();
            }
        }
    }

    /// <summary>Type filter forwarded to the shared MAUI view-model.</summary>
    public string? TypeFilter
    {
        get => DocumentControl.TypeFilter;
        set
        {
            if (!string.Equals(DocumentControl.TypeFilter, value, StringComparison.Ordinal))
            {
                DocumentControl.TypeFilter = value;
                OnPropertyChanged();
                ProjectRecordsIntoShell();
            }
        }
    }

    /// <summary>Change control options surfaced for linking dialogs.</summary>
    public ObservableCollection<ChangeControlSummaryDto> AvailableChangeControls => DocumentControl.AvailableChangeControls;

    /// <summary>Selected change control used when linking to a document.</summary>
    public ChangeControlSummaryDto? SelectedChangeControlForLink
    {
        get => DocumentControl.SelectedChangeControlForLink;
        set
        {
            if (!EqualityComparer<ChangeControlSummaryDto?>.Default.Equals(DocumentControl.SelectedChangeControlForLink, value))
            {
                DocumentControl.SelectedChangeControlForLink = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets a value indicating whether the change control picker is visible.</summary>
    public bool IsChangeControlPickerOpen => DocumentControl.IsChangeControlPickerOpen;

    /// <summary>Command that opens the change control picker overlay.</summary>
    public ICommand OpenChangeControlPickerCommand => DocumentControl.OpenChangeControlPickerCommand;

    /// <summary>Command that links the selected change control.</summary>
    public ICommand LinkChangeControlCommand => DocumentControl.LinkChangeControlCommand;

    /// <summary>Command that cancels the change control picker.</summary>
    public ICommand CancelChangeControlPickerCommand => DocumentControl.CancelChangeControlPickerCommand;

    /// <summary>Command that exports the current filtered list.</summary>
    public ICommand ExportDocumentsCommand => DocumentControl.ExportDocumentsCommand;

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        await _documentControl.LoadDocumentsAsync().ConfigureAwait(false);
        HookFilteredDocuments(DocumentControl.FilteredDocuments);
        return ProjectRecords();
    }

    /// <inheritdoc />
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var today = DateTime.Now;
        var demo = new List<SopDocument>
        {
            new()
            {
                Id = 101,
                Code = "SOP-001",
                Name = "Gowning Procedure",
                Status = "approved",
                VersionNo = 3,
                Description = "Controlled gowning steps for clean room entry.",
                RelatedType = "change_controls",
                RelatedId = 401,
                DateIssued = today.AddMonths(-6),
                DateExpiry = today.AddMonths(6),
                ReviewNotes = "Reviewed annually"
            },
            new()
            {
                Id = 102,
                Code = "POL-014",
                Name = "Quality Policy",
                Status = "published",
                VersionNo = 2,
                Description = "Corporate quality commitment statement.",
                RelatedType = "",
                DateIssued = today.AddYears(-1),
                DateExpiry = today.AddYears(1),
                ReviewNotes = "Next review Q4"
            }
        };

        return demo.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    protected override Task OnModeChangedAsync(FormMode mode)
    {
        IsEditorEnabled = mode is FormMode.Add or FormMode.Update;

        switch (mode)
        {
            case FormMode.Add:
                SetEditor(DocumentControlEditor.CreateForNew());
                break;
            case FormMode.Update:
                if (DocumentControl.SelectedDocument is not null)
                {
                    SetEditor(DocumentControlEditor.FromDocument(DocumentControl.SelectedDocument));
                }
                break;
            case FormMode.View:
                if (DocumentControl.SelectedDocument is not null)
                {
                    SetEditor(DocumentControlEditor.FromDocument(DocumentControl.SelectedDocument));
                }
                else
                {
                    SetEditor(DocumentControlEditor.CreateEmpty());
                }
                break;
        }

        UpdateAttachmentCommandState();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task<bool> OnSaveAsync()
    {
        if (Mode == FormMode.Add)
        {
            await _documentControl.InitiateDocumentAsync().ConfigureAwait(false);
            ProjectRecordsIntoShell();
            return true;
        }

        if (Mode == FormMode.Update)
        {
            var document = DocumentControl.SelectedDocument;
            if (document is null)
            {
                StatusMessage = "Select a document before saving.";
                return false;
            }

            ElectronicSignatureDialogResult? signatureResult;
            try
            {
                signatureResult = await _signatureDialog
                    .CaptureSignatureAsync(new ElectronicSignatureContext("sop_documents", document.Id))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Electronic signature failed: {ex.Message}";
                return false;
            }

            if (signatureResult is null || signatureResult.Signature is null)
            {
                StatusMessage = "Electronic signature cancelled.";
                return false;
            }

            await _documentControl.ReviseDocumentAsync().ConfigureAwait(false);
            await SignaturePersistenceHelper
                .PersistIfRequiredAsync(_signatureDialog, signatureResult)
                .ConfigureAwait(false);

            ProjectRecordsIntoShell();
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    protected override void OnCancel()
    {
        if (DocumentControl.SelectedDocument is not null)
        {
            SetEditor(DocumentControlEditor.FromDocument(DocumentControl.SelectedDocument));
        }
        else
        {
            SetEditor(DocumentControlEditor.CreateEmpty());
        }
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<string>> ValidateAsync()
    {
        var errors = new List<string>();
        if (Mode is FormMode.Add or FormMode.Update)
        {
            if (string.IsNullOrWhiteSpace(Editor.Title))
            {
                errors.Add("Title is required.");
            }

            if (string.IsNullOrWhiteSpace(Editor.Code))
            {
                errors.Add("Code is required.");
            }
        }

        return await Task.FromResult(errors);
    }

    /// <inheritdoc />
    protected override Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (_suppressSelectionSync)
        {
            return Task.CompletedTask;
        }

        _suppressSelectionSync = true;
        try
        {
            if (record is null)
            {
                if (DocumentControl.SelectedDocument is not null)
                {
                    DocumentControl.SelectedDocument = null;
                }

                SetEditor(DocumentControlEditor.CreateEmpty());
            }
            else
            {
                var document = FindDocument(record.Key);
                if (!ReferenceEquals(document, DocumentControl.SelectedDocument))
                {
                    DocumentControl.SelectedDocument = document;
                }

                if (document is not null)
                {
                    SetEditor(DocumentControlEditor.FromDocument(document));
                }
                else
                {
                    SetEditor(DocumentControlEditor.CreateEmpty());
                }
            }
        }
        finally
        {
            _suppressSelectionSync = false;
            UpdateAttachmentCommandState();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override string FormatLoadedStatus(int count)
    {
        var status = DocumentControl.StatusMessage;
        if (!string.IsNullOrWhiteSpace(status))
        {
            return status!;
        }

        if (count == 0)
        {
            return "No documents found for the current filters.";
        }

        return count == 1
            ? "Loaded 1 document."
            : $"Loaded {count} documents.";
    }

    /// <inheritdoc />
    protected override bool MatchesSearch(ModuleRecord record, string searchText)
        => true; // Delegated to the shared DocumentControlViewModel filtering pipeline.

    /// <inheritdoc />
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SearchText) && !_suppressSearchSync)
        {
            if (!string.Equals(DocumentControl.SearchTerm, SearchText, StringComparison.Ordinal))
            {
                DocumentControl.SearchTerm = SearchText;
            }
        }
        else if (e.PropertyName == nameof(IsBusy))
        {
            UpdateAttachmentCommandState();
        }
    }

    private void HookDocumentControl()
    {
        _documentControl.PropertyChanged += OnDocumentControlPropertyChanged;
        HookFilteredDocuments(_documentControl.FilteredDocuments);
    }

    private void HookFilteredDocuments(ObservableCollection<SopDocument>? collection)
    {
        if (_filteredDocuments is not null)
        {
            _filteredDocuments.CollectionChanged -= OnFilteredDocumentsChanged;
        }

        _filteredDocuments = collection;
        if (_filteredDocuments is not null)
        {
            _filteredDocuments.CollectionChanged += OnFilteredDocumentsChanged;
        }
    }

    private void OnFilteredDocumentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => ProjectRecordsIntoShell();

    private void OnDocumentControlPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DocumentControlViewModel.IsBusy):
                if (IsBusy != DocumentControl.IsBusy)
                {
                    IsBusy = DocumentControl.IsBusy;
                }

                break;
            case nameof(DocumentControlViewModel.StatusMessage):
                if (!string.Equals(StatusMessage, DocumentControl.StatusMessage, StringComparison.Ordinal))
                {
                    StatusMessage = DocumentControl.StatusMessage ?? string.Empty;
                }

                break;
            case nameof(DocumentControlViewModel.FilteredDocuments):
                HookFilteredDocuments(DocumentControl.FilteredDocuments);
                ProjectRecordsIntoShell();
                break;
            case nameof(DocumentControlViewModel.SelectedDocument):
                SyncSelectionFromDocumentControl();
                break;
            case nameof(DocumentControlViewModel.SearchTerm):
                if (!_suppressSearchSync && !string.Equals(SearchText, DocumentControl.SearchTerm, StringComparison.Ordinal))
                {
                    _suppressSearchSync = true;
                    try
                    {
                        SearchText = DocumentControl.SearchTerm;
                    }
                    finally
                    {
                        _suppressSearchSync = false;
                    }
                }

                break;
            case nameof(DocumentControlViewModel.StatusFilter):
                OnPropertyChanged(nameof(StatusFilter));
                ProjectRecordsIntoShell();
                break;
            case nameof(DocumentControlViewModel.TypeFilter):
                OnPropertyChanged(nameof(TypeFilter));
                ProjectRecordsIntoShell();
                break;
            case nameof(DocumentControlViewModel.IsChangeControlPickerOpen):
                OnPropertyChanged(nameof(IsChangeControlPickerOpen));
                break;
            case nameof(DocumentControlViewModel.AvailableChangeControls):
                OnPropertyChanged(nameof(AvailableChangeControls));
                break;
            case nameof(DocumentControlViewModel.SelectedChangeControlForLink):
                OnPropertyChanged(nameof(SelectedChangeControlForLink));
                break;
        }
    }

    private void SyncSelectionFromDocumentControl()
    {
        if (_suppressSelectionSync)
        {
            return;
        }

        _suppressSelectionSync = true;
        try
        {
            if (DocumentControl.SelectedDocument is null)
            {
                SelectedRecord = null;
                SetEditor(DocumentControlEditor.CreateEmpty());
            }
            else
            {
                var key = DocumentControl.SelectedDocument.Id.ToString(CultureInfo.InvariantCulture);
                var match = Records.FirstOrDefault(r => r.Key == key);
                if (match is null)
                {
                    match = ToRecord(DocumentControl.SelectedDocument);
                    Records.Add(match);
                }

                SelectedRecord = match;
                SetEditor(DocumentControlEditor.FromDocument(DocumentControl.SelectedDocument));
            }
        }
        finally
        {
            _suppressSelectionSync = false;
            UpdateAttachmentCommandState();
        }
    }

    private void ProjectRecordsIntoShell()
    {
        var snapshot = ProjectRecords();
        Records.Clear();
        foreach (var record in snapshot)
        {
            Records.Add(record);
        }

        RecordsView.Refresh();
        if (Records.Count > 0)
        {
            var desiredKey = DocumentControl.SelectedDocument?.Id.ToString(CultureInfo.InvariantCulture);
            var match = desiredKey is not null
                ? Records.FirstOrDefault(r => r.Key == desiredKey)
                : null;

            _suppressSelectionSync = true;
            try
            {
                SelectedRecord = match ?? Records[0];
            }
            finally
            {
                _suppressSelectionSync = false;
            }
        }
        else
        {
            _suppressSelectionSync = true;
            try
            {
                SelectedRecord = null;
            }
            finally
            {
                _suppressSelectionSync = false;
            }
        }

        UpdateAttachmentCommandState();
    }

    private IReadOnlyList<ModuleRecord> ProjectRecords()
    {
        if (DocumentControl.FilteredDocuments is null || DocumentControl.FilteredDocuments.Count == 0)
        {
            return Array.Empty<ModuleRecord>();
        }

        return DocumentControl.FilteredDocuments.Select(ToRecord).ToList();
    }

    private static ModuleRecord ToRecord(SopDocument document)
    {
        var key = document.Id.ToString(CultureInfo.InvariantCulture);
        var title = string.IsNullOrWhiteSpace(document.Name) ? document.Code : document.Name;
        var inspector = new List<InspectorField>
        {
            InspectorField.Create(ModuleKey, "Document Control", key, title, "Status", document.Status),
            InspectorField.Create(ModuleKey, "Document Control", key, title, "Type", document.RelatedType),
            InspectorField.Create(ModuleKey, "Document Control", key, title, "Version", document.VersionNo.ToString(CultureInfo.InvariantCulture)),
            InspectorField.Create(ModuleKey, "Document Control", key, title, "Effective", document.DateIssued.ToString("d", CultureInfo.CurrentCulture)),
            InspectorField.Create(ModuleKey, "Document Control", key, title, "Expires", document.DateExpiry?.ToString("d", CultureInfo.CurrentCulture) ?? "-"),
            InspectorField.Create(ModuleKey, "Document Control", key, title, "Notes", document.ReviewNotes)
        };

        string? relatedModuleKey = null;
        object? relatedParameter = null;
        if (string.Equals(document.RelatedType, "change_controls", StringComparison.OrdinalIgnoreCase) && document.RelatedId.HasValue)
        {
            relatedModuleKey = ChangeControlModuleViewModel.ModuleKey;
            relatedParameter = document.RelatedId.Value;
        }

        return new ModuleRecord(
            key,
            title ?? key,
            document.Code,
            document.Status,
            document.Description,
            inspector,
            relatedModuleKey,
            relatedParameter);
    }

    private SopDocument? FindDocument(string key)
    {
        if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            return null;
        }

        var filtered = DocumentControl.FilteredDocuments?.FirstOrDefault(d => d.Id == id);
        if (filtered is not null)
        {
            return filtered;
        }

        return DocumentControl.Documents?.FirstOrDefault(d => d.Id == id);
    }

    private void SetEditor(DocumentControlEditor editor)
    {
        _suppressEditorDirtyNotifications = true;
        Editor = editor ?? DocumentControlEditor.CreateEmpty();
        _suppressEditorDirtyNotifications = false;
        ResetDirty();
    }

    private bool CanAttachDocument()
        => !IsBusy && DocumentControl.SelectedDocument is not null;

    private async Task ExecuteAttachDocumentAsync()
    {
        if (DocumentControl.SelectedDocument is null)
        {
            StatusMessage = "Select a document before uploading attachments.";
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = _localization.GetString("Module.Toolbar.Button.Attach.ToolTip") ?? "Attach Files",
            Multiselect = true
        };

        if (dialog.ShowDialog() != true || dialog.FileNames.Length == 0)
        {
            StatusMessage = "Attachment upload cancelled.";
            return;
        }

        var processed = 0;
        var deduplicated = 0;

        try
        {
            IsBusy = true;
            foreach (var file in dialog.FileNames)
            {
                await using var stream = File.OpenRead(file);
                var request = new AttachmentUploadRequest
                {
                    FileName = Path.GetFileName(file),
                    ContentType = "application/octet-stream",
                    EntityType = "sop_documents",
                    EntityId = DocumentControl.SelectedDocument.Id,
                    Reason = $"documentcontrol:{DocumentControl.SelectedDocument.Id}",
                    SourceHost = Environment.MachineName,
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

    partial void OnEditorChanging(DocumentControlEditor value)
    {
        if (Editor is not null)
        {
            Editor.PropertyChanged -= OnEditorPropertyChanged;
        }
    }

    partial void OnEditorChanged(DocumentControlEditor value)
    {
        if (value is not null)
        {
            value.PropertyChanged += OnEditorPropertyChanged;
        }
    }

    /// <summary>
    /// Editor projection used by the WPF shell to surface document metadata while reusing shared services.
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
        private string? _status = "draft";

        [ObservableProperty]
        private string? _documentType = "SOP";

        [ObservableProperty]
        private string? _versionNumber = "1";

        [ObservableProperty]
        private DateTime? _effectiveDate = DateTime.UtcNow.Date;

        [ObservableProperty]
        private DateTime? _expirationDate;

        [ObservableProperty]
        private string? _owner;

        [ObservableProperty]
        private string? _summary;

        [ObservableProperty]
        private string? _reviewNotes;

        /// <summary>Creates an empty editor instance.</summary>
        public static DocumentControlEditor CreateEmpty() => new();

        /// <summary>Creates a template used when entering Add mode.</summary>
        public static DocumentControlEditor CreateForNew()
            => new()
            {
                Code = $"DOC-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Title = string.Empty,
                Status = "draft",
                DocumentType = "SOP",
                VersionNumber = "1",
                EffectiveDate = DateTime.UtcNow.Date
            };

        /// <summary>Creates an editor projection from an existing document.</summary>
        public static DocumentControlEditor FromDocument(SopDocument document)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var ownerDisplay = document.ResponsibleUser is not null
                ? document.ResponsibleUser.FullName
                : (document.ResponsibleUserId != 0 ? document.ResponsibleUserId.ToString(CultureInfo.InvariantCulture) : null);

            return new DocumentControlEditor
            {
                Id = document.Id,
                Code = document.Code ?? string.Empty,
                Title = document.Name ?? string.Empty,
                Status = document.Status,
                DocumentType = document.RelatedType,
                VersionNumber = document.VersionNo <= 0
                    ? "1"
                    : document.VersionNo.ToString(CultureInfo.InvariantCulture),
                EffectiveDate = document.DateIssued,
                ExpirationDate = document.DateExpiry,
                Owner = ownerDisplay,
                Summary = string.IsNullOrWhiteSpace(document.Description) ? document.Comment : document.Description,
                ReviewNotes = document.ReviewNotes
            };
        }

        /// <summary>Applies editor fields onto the supplied entity.</summary>
        public SopDocument ApplyTo(SopDocument target)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            target.Id = Id;
            target.Code = Code ?? target.Code;
            target.Name = Title ?? target.Name;
            target.Status = Status ?? target.Status;
            target.RelatedType = DocumentType ?? target.RelatedType;
            if (int.TryParse(VersionNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out var version))
            {
                target.VersionNo = version;
            }

            if (EffectiveDate.HasValue)
            {
                target.DateIssued = EffectiveDate.Value;
            }

            target.DateExpiry = ExpirationDate;
            target.Description = Summary ?? target.Description;
            target.ReviewNotes = ReviewNotes ?? target.ReviewNotes;
            return target;
        }

        /// <summary>Creates a detached entity using the current editor state.</summary>
        public SopDocument ToEntity()
        {
            var entity = new SopDocument();
            ApplyTo(entity);
            return entity;
        }
    }
}
