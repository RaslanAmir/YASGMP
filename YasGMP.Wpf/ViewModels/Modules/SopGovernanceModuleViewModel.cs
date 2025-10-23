using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.ViewModels;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Wraps the shared <see cref="SopViewModel"/> so the WPF shell can present SOP governance workflows with SAP B1 semantics.
/// </summary>
public sealed partial class SopGovernanceModuleViewModel : ModuleDocumentViewModel, IDisposable
{
    /// <summary>Stable registry key used by the module tree and ribbon navigation.</summary>
    public const string ModuleKey = "SopGovernance";

    private readonly SopViewModel _sop;
    private readonly ILocalizationService _localization;
    private readonly AsyncRelayCommand _createCommand;
    private readonly AsyncRelayCommand _updateCommand;
    private readonly AsyncRelayCommand _deleteCommand;
    private INotifyCollectionChanged? _filteredDocumentsSubscription;

    /// <summary>
    /// Initializes a new instance of the <see cref="SopGovernanceModuleViewModel"/> class.
    /// </summary>
    public SopGovernanceModuleViewModel(
        SopViewModel sop,
        ILocalizationService localization,
        ICflDialogService cflDialogService,
        IShellInteractionService shellInteraction,
        IModuleNavigationService navigation)
        : base(ModuleKey, localization.GetString("Module.Title.SopGovernance"), localization, cflDialogService, shellInteraction, navigation)
    {
        _sop = sop ?? throw new ArgumentNullException(nameof(sop));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        _sop.PropertyChanged += OnSopPropertyChanged;
        UpdateFilteredDocumentsSubscription(_sop.FilteredDocuments);

        _createCommand = new AsyncRelayCommand(ExecuteCreateAsync, CanCreate);
        _updateCommand = new AsyncRelayCommand(ExecuteUpdateAsync, CanUpdate);
        _deleteCommand = new AsyncRelayCommand(ExecuteDeleteAsync, CanDelete);

        PropertyChanged += OnSelfPropertyChanged;

        Editor = SopDocumentEditor.FromDocument(_sop.DraftDocument, OnEditorChanged);
        IsEditorEnabled = false;
    }

    /// <summary>Shared SOP view-model surfaced for bindings.</summary>
    public SopViewModel Governance => _sop;

    /// <summary>Command that persists the draft document.</summary>
    public IAsyncRelayCommand CreateCommand => _createCommand;

    /// <summary>Command that updates the selected document.</summary>
    public IAsyncRelayCommand UpdateCommand => _updateCommand;

    /// <summary>Command that deletes the selected document.</summary>
    public IAsyncRelayCommand DeleteCommand => _deleteCommand;

    /// <summary>Available lifecycle statuses mirrored from the shared view-model.</summary>
    public IReadOnlyList<string> StatusOptions => _sop.AvailableStatuses;

    /// <summary>Available process filters mirrored from the shared view-model.</summary>
    public IReadOnlyList<string> ProcessOptions => _sop.AvailableProcesses;

    /// <summary>Forwards search term updates to the shared view-model.</summary>
    public string? SearchTerm
    {
        get => _sop.SearchTerm;
        set
        {
            if (!string.Equals(_sop.SearchTerm, value, StringComparison.Ordinal))
            {
                _sop.SearchTerm = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Status filter forwarded to the shared view-model.</summary>
    public string? StatusFilter
    {
        get => _sop.StatusFilter;
        set
        {
            if (!string.Equals(_sop.StatusFilter, value, StringComparison.Ordinal))
            {
                _sop.StatusFilter = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Process filter forwarded to the shared view-model.</summary>
    public string? ProcessFilter
    {
        get => _sop.ProcessFilter;
        set
        {
            if (!string.Equals(_sop.ProcessFilter, value, StringComparison.Ordinal))
            {
                _sop.ProcessFilter = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Minimum issue date filter.</summary>
    public DateTime? IssuedFrom
    {
        get => _sop.IssuedFrom;
        set
        {
            if (_sop.IssuedFrom != value)
            {
                _sop.IssuedFrom = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Maximum issue date filter.</summary>
    public DateTime? IssuedTo
    {
        get => _sop.IssuedTo;
        set
        {
            if (_sop.IssuedTo != value)
            {
                _sop.IssuedTo = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Indicates whether only active SOPs are shown.</summary>
    public bool IncludeOnlyActive
    {
        get => _sop.IncludeOnlyActive;
        set
        {
            if (_sop.IncludeOnlyActive != value)
            {
                _sop.IncludeOnlyActive = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Indicates whether editor fields are enabled.</summary>
    [ObservableProperty]
    private bool _isEditorEnabled;

    /// <summary>Editor projection used by the Add/Update panes.</summary>
    [ObservableProperty]
    private SopDocumentEditor _editor;

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<ModuleRecord>> LoadAsync(object? parameter)
    {
        await _sop.LoadDocumentsAsync().ConfigureAwait(false);
        return ProjectRecords();
    }

    /// <inheritdoc />
    protected override IReadOnlyList<ModuleRecord> CreateDesignTimeRecords()
    {
        var today = DateTime.Today;
        return new List<ModuleRecord>
        {
            CreateRecord(new SopDocument
            {
                Id = 501,
                Code = "SOP-QUAL-001",
                Name = "Deviation Handling",
                Status = "published",
                Process = "Quality",
                DateIssued = today.AddMonths(-3),
                DateExpiry = today.AddMonths(9)
            }),
            CreateRecord(new SopDocument
            {
                Id = 502,
                Code = "SOP-VAL-014",
                Name = "Equipment Qualification",
                Status = "under review",
                Process = "Validation",
                DateIssued = today.AddMonths(-1),
                DateExpiry = today.AddMonths(3)
            })
        };
    }

    /// <inheritdoc />
    protected override Task OnModeChangedAsync(FormMode mode)
    {
        switch (mode)
        {
            case FormMode.Add:
                _sop.ResetDraftCommand.Execute(null);
                Editor = SopDocumentEditor.FromDocument(_sop.DraftDocument, OnEditorChanged);
                IsEditorEnabled = true;
                break;
            case FormMode.Update:
                if (_sop.SelectedDocument is SopDocument document)
                {
                    Editor = SopDocumentEditor.FromDocument(document, OnEditorChanged);
                    IsEditorEnabled = true;
                }
                else
                {
                    IsEditorEnabled = false;
                }
                break;
            default:
                if (_sop.SelectedDocument is SopDocument current)
                {
                    Editor = SopDocumentEditor.FromDocument(current, OnEditorChanged);
                }
                IsEditorEnabled = false;
                break;
        }

        UpdateWorkflowCommands();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task<bool> OnSaveAsync()
    {
        if (Mode == FormMode.Add)
        {
            Editor.ApplyTo(_sop.DraftDocument);
            await _sop.CreateDocumentAsync().ConfigureAwait(false);
            StatusMessage = _sop.StatusMessage ?? string.Empty;
            return true;
        }

        if (Mode == FormMode.Update)
        {
            if (_sop.SelectedDocument is null)
            {
                return false;
            }

            Editor.ApplyTo(_sop.SelectedDocument);
            await _sop.UpdateDocumentAsync().ConfigureAwait(false);
            StatusMessage = _sop.StatusMessage ?? string.Empty;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    protected override void OnCancel()
    {
        if (Mode == FormMode.Add)
        {
            _sop.ResetDraftCommand.Execute(null);
            Editor = SopDocumentEditor.FromDocument(_sop.DraftDocument, OnEditorChanged);
        }
        else if (_sop.SelectedDocument is SopDocument document)
        {
            Editor = SopDocumentEditor.FromDocument(document, OnEditorChanged);
        }

        UpdateWorkflowCommands();
    }

    /// <inheritdoc />
    protected override Task OnRecordSelectedAsync(ModuleRecord? record)
    {
        if (record is null)
        {
            _sop.SelectedDocument = null;
            UpdateWorkflowCommands();
            return Task.CompletedTask;
        }

        if (int.TryParse(record.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            _sop.SelectedDocument = _sop.Documents.FirstOrDefault(d => d.Id == id);
            if (_sop.SelectedDocument is SopDocument doc)
            {
                Editor = SopDocumentEditor.FromDocument(doc, OnEditorChanged);
            }
        }

        UpdateWorkflowCommands();
        return Task.CompletedTask;
    }

    private async Task ExecuteCreateAsync()
    {
        if (!CanCreate())
        {
            return;
        }

        Editor.ApplyTo(_sop.DraftDocument);
        await _sop.CreateDocumentAsync().ConfigureAwait(false);
        StatusMessage = _sop.StatusMessage ?? string.Empty;
        await RefreshCommand.ExecuteAsync(null).ConfigureAwait(false);
    }

    private bool CanCreate() => !IsBusy;

    private async Task ExecuteUpdateAsync()
    {
        if (!CanUpdate())
        {
            return;
        }

        if (_sop.SelectedDocument is SopDocument document)
        {
            Editor.ApplyTo(document);
        }

        await _sop.UpdateDocumentAsync().ConfigureAwait(false);
        StatusMessage = _sop.StatusMessage ?? string.Empty;
        await RefreshCommand.ExecuteAsync(null).ConfigureAwait(false);
    }

    private bool CanUpdate()
        => !IsBusy && _sop.SelectedDocument is not null;

    private async Task ExecuteDeleteAsync()
    {
        if (!CanDelete())
        {
            return;
        }

        await _sop.DeleteDocumentAsync().ConfigureAwait(false);
        StatusMessage = _sop.StatusMessage ?? string.Empty;
        await RefreshCommand.ExecuteAsync(null).ConfigureAwait(false);
    }

    private bool CanDelete()
        => !IsBusy && _sop.SelectedDocument is not null;

    private void OnSopPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(SopViewModel.StatusMessage), StringComparison.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(_sop.StatusMessage))
            {
                StatusMessage = _sop.StatusMessage!;
            }
        }
        else if (string.Equals(e.PropertyName, nameof(SopViewModel.IsBusy), StringComparison.Ordinal))
        {
            IsBusy = _sop.IsBusy;
            RefreshCommandStates();
            UpdateWorkflowCommands();
        }
        else if (string.Equals(e.PropertyName, nameof(SopViewModel.SelectedDocument), StringComparison.Ordinal))
        {
            if (_sop.SelectedDocument is SopDocument document)
            {
                Editor = SopDocumentEditor.FromDocument(document, OnEditorChanged);
            }
            UpdateWorkflowCommands();
        }
        else if (string.Equals(e.PropertyName, nameof(SopViewModel.FilteredDocuments), StringComparison.Ordinal))
        {
            UpdateFilteredDocumentsSubscription(_sop.FilteredDocuments);
            ProjectRecordsIntoShell();
        }
    }

    private void OnFilteredDocumentsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => ProjectRecordsIntoShell();

    private void OnSelfPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(SearchText), StringComparison.Ordinal))
        {
            SearchTerm = SearchText;
        }
    }

    private void OnEditorChanged()
    {
        if (Mode is FormMode.Add or FormMode.Update)
        {
            MarkDirty();
        }
    }

    private void ProjectRecordsIntoShell()
    {
        var snapshot = ProjectRecords();
        var selectedKey = SelectedRecord?.Key;

        Records.Clear();
        foreach (var record in snapshot)
        {
            Records.Add(record);
        }

        if (selectedKey is not null)
        {
            SelectedRecord = Records.FirstOrDefault(r => string.Equals(r.Key, selectedKey, StringComparison.Ordinal));
        }
        else if (Records.Count > 0)
        {
            SelectedRecord = Records[0];
        }
        else
        {
            SelectedRecord = null;
        }
    }

    private IReadOnlyList<ModuleRecord> ProjectRecords()
        => _sop.FilteredDocuments
            .Select(CreateRecord)
            .ToList();

    private ModuleRecord CreateRecord(SopDocument document)
    {
        var owner = document.ResponsibleUser?.FullName;
        if (string.IsNullOrWhiteSpace(owner) && document.ResponsibleUserId > 0)
        {
            owner = document.ResponsibleUserId.ToString(CultureInfo.InvariantCulture);
        }

        var inspector = new List<InspectorField>
        {
            InspectorField.Create(ModuleKey, Title, document.Id.ToString(CultureInfo.InvariantCulture), document.Name, _localization.GetString("Module.SopGovernance.Field.Process"), document.Process),
            InspectorField.Create(ModuleKey, Title, document.Id.ToString(CultureInfo.InvariantCulture), document.Name, _localization.GetString("Module.SopGovernance.Field.Issued"), FormatDate(document.DateIssued)),
            InspectorField.Create(ModuleKey, Title, document.Id.ToString(CultureInfo.InvariantCulture), document.Name, _localization.GetString("Module.SopGovernance.Field.Expiry"), FormatDate(document.DateExpiry)),
            InspectorField.Create(ModuleKey, Title, document.Id.ToString(CultureInfo.InvariantCulture), document.Name, _localization.GetString("Module.SopGovernance.Field.Owner"), owner)
        };

        return new ModuleRecord(
            document.Id.ToString(CultureInfo.InvariantCulture),
            document.Name,
            document.Code,
            document.Status,
            document.Description,
            inspector);
    }

    private static string FormatDate(DateTime? value)
        => value.HasValue ? value.Value.ToString("d", CultureInfo.CurrentCulture) : string.Empty;

    private void UpdateWorkflowCommands()
    {
        _createCommand.NotifyCanExecuteChanged();
        _updateCommand.NotifyCanExecuteChanged();
        _deleteCommand.NotifyCanExecuteChanged();
    }

    private void UpdateFilteredDocumentsSubscription(INotifyCollectionChanged? next)
    {
        if (_filteredDocumentsSubscription is not null)
        {
            _filteredDocumentsSubscription.CollectionChanged -= OnFilteredDocumentsChanged;
        }

        _filteredDocumentsSubscription = next;

        if (_filteredDocumentsSubscription is not null)
        {
            _filteredDocumentsSubscription.CollectionChanged += OnFilteredDocumentsChanged;
        }
    }

    /// <summary>Editor projection surfaced to the XAML layer.</summary>
    public sealed partial class SopDocumentEditor : ObservableObject
    {
        private readonly Action _onChanged;

        private SopDocumentEditor(Action onChanged)
        {
            _onChanged = onChanged;
        }

        public static SopDocumentEditor FromDocument(SopDocument document, Action onChanged)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return new SopDocumentEditor(onChanged)
            {
                Code = document.Code ?? string.Empty,
                Name = document.Name ?? string.Empty,
                Status = document.Status ?? string.Empty,
                Process = document.Process ?? string.Empty,
                Owner = document.Owner ?? string.Empty,
                Description = document.Description ?? string.Empty,
                DateIssued = document.DateIssued,
                DateExpiry = document.DateExpiry,
                Version = document.VersionNo
            };
        }

        [ObservableProperty]
        private string _code = string.Empty;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _status = string.Empty;

        [ObservableProperty]
        private string _process = string.Empty;

        [ObservableProperty]
        private string _owner = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private DateTime? _dateIssued;

        [ObservableProperty]
        private DateTime? _dateExpiry;

        [ObservableProperty]
        private int _version;

        partial void OnCodeChanged(string value) => _onChanged();
        partial void OnNameChanged(string value) => _onChanged();
        partial void OnStatusChanged(string value) => _onChanged();
        partial void OnProcessChanged(string value) => _onChanged();
        partial void OnOwnerChanged(string value) => _onChanged();
        partial void OnDescriptionChanged(string value) => _onChanged();
        partial void OnDateIssuedChanged(DateTime? value) => _onChanged();
        partial void OnDateExpiryChanged(DateTime? value) => _onChanged();
        partial void OnVersionChanged(int value) => _onChanged();

        public void ApplyTo(SopDocument document)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            document.Code = Code;
            document.Name = Name;
            document.Status = Status;
            document.Process = Process;
            document.Owner = Owner;
            document.Description = Description;
            document.DateIssued = DateIssued;
            document.DateExpiry = DateExpiry;
            document.VersionNo = Version;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        PropertyChanged -= OnSelfPropertyChanged;
        _sop.PropertyChanged -= OnSopPropertyChanged;
        UpdateFilteredDocumentsSubscription(null);
    }
}
