using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Helpers;
using YasGMP.Models;
using YasGMP.Models.DTO; // <-- USE DTOs returned by DatabaseService
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// Robust ViewModel for GMP Document/SOP control.
    /// <para>
    /// • Lifecycle: create (initiate), revise, assign, approve, publish, expire/obsolete.<br/>
    /// • Fully aligned with <see cref="SopDocument"/> model (uses <c>VersionNo</c>, <c>ReviewNotes</c>, etc.).<br/>
    /// • All service calls match your <see cref="DatabaseService"/> region “14 · SOP / DOCUMENT CONTROL”.<br/>
    /// • Real “type” filter uses <see cref="SopDocument.RelatedType"/> (e.g., <c>"SOP"</c>, <c>"Policy"</c>, etc.).
    /// </para>
    /// </summary>
    public sealed class DocumentControlViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<SopDocument> _documents = new();
        private ObservableCollection<SopDocument> _filteredDocuments = new();
        private ObservableCollection<ChangeControlSummaryDto> _availableChangeControls = new();

        // nullable to avoid CS8618; UI guards all usages
        private SopDocument? _selectedDocument;
        private ChangeControlSummaryDto? _selectedChangeControlForLink;

        private string? _searchTerm;
        private string? _statusFilter;
        private string? _typeFilter;

        private bool _isBusy;
        private string? _statusMessage;
        private bool _isChangeControlPickerOpen;

        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the ViewModel, commands and loads initial data.
        /// </summary>
        /// <param name="dbService">Database service.</param>
        /// <param name="authService">Authentication service.</param>
        /// <exception cref="ArgumentNullException">Thrown if a dependency is null.</exception>
        public DocumentControlViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService   = dbService  ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService?? throw new ArgumentNullException(nameof(authService));

            // coalesce session info safely
            _currentSessionId  = _authService.CurrentSessionId  ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? string.Empty;
            _currentIpAddress  = _authService.CurrentIpAddress  ?? string.Empty;

            // Commands
            LoadDocumentsCommand      = new AsyncRelayCommand(LoadDocumentsAsync);
            InitiateDocumentCommand   = new AsyncRelayCommand(InitiateDocumentAsync,   () => !IsBusy);
            ReviseDocumentCommand     = new AsyncRelayCommand(ReviseDocumentAsync,     () => !IsBusy && SelectedDocument != null);
            AssignDocumentCommand     = new AsyncRelayCommand(AssignDocumentAsync,     () => !IsBusy && SelectedDocument != null);
            ApproveDocumentCommand    = new AsyncRelayCommand(ApproveDocumentAsync,    () => !IsBusy && SelectedDocument != null && SelectedDocument.Status?.Equals("pending_approval", StringComparison.OrdinalIgnoreCase) == true);
            PublishDocumentCommand    = new AsyncRelayCommand(PublishDocumentAsync,    () => !IsBusy && SelectedDocument != null && SelectedDocument.Status?.Equals("approved", StringComparison.OrdinalIgnoreCase) == true);
            ExpireDocumentCommand     = new AsyncRelayCommand(ExpireDocumentAsync,     () => !IsBusy && SelectedDocument != null && SelectedDocument.Status?.Equals("published", StringComparison.OrdinalIgnoreCase) == true);
            OpenChangeControlPickerCommand = new AsyncRelayCommand(OpenChangeControlPickerAsync, () => !IsBusy && SelectedDocument != null);
            LinkChangeControlCommand       = new AsyncRelayCommand(LinkChangeControlAsync,  () => !IsBusy && SelectedDocument != null);
            CancelChangeControlPickerCommand = new RelayCommand(CancelChangeControlPicker);
            ExportDocumentsCommand    = new AsyncRelayCommand(ExportDocumentsAsync,    () => !IsBusy);
            FilterChangedCommand      = new RelayCommand(FilterDocuments);

            // Load initial data silently
            _ = LoadDocumentsAsync();
        }

        #endregion

        #region Properties : Collections & Selection

        /// <summary>All SOP/documents.</summary>
        public ObservableCollection<SopDocument> Documents
        {
            get => _documents;
            private set { _documents = value ?? new(); OnPropertyChanged(); }
        }

        /// <summary>Filtered documents for UI binding.</summary>
        public ObservableCollection<SopDocument> FilteredDocuments
        {
            get => _filteredDocuments;
            private set { _filteredDocuments = value ?? new(); OnPropertyChanged(); }
        }

        /// <summary>Currently selected document (nullable).</summary>
        public SopDocument? SelectedDocument
        {
            get => _selectedDocument;
            set
            {
                _selectedDocument = value;
                OnPropertyChanged();
                CancelChangeControlPicker();
            }
        }

        #endregion

        #region Properties : Change Control Linking

        /// <summary>Change controls available for linking in the picker dialog.</summary>
        public ObservableCollection<ChangeControlSummaryDto> AvailableChangeControls
        {
            get => _availableChangeControls;
            private set
            {
                _availableChangeControls = value ?? new ObservableCollection<ChangeControlSummaryDto>();
                OnPropertyChanged();
            }
        }

        /// <summary>Currently selected change control in the picker dialog.</summary>
        public ChangeControlSummaryDto? SelectedChangeControlForLink
        {
            get => _selectedChangeControlForLink;
            set
            {
                _selectedChangeControlForLink = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Whether the change control picker dialog should be visible.</summary>
        public bool IsChangeControlPickerOpen
        {
            get => _isChangeControlPickerOpen;
            private set
            {
                _isChangeControlPickerOpen = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Properties : Filters & UI

        /// <summary>Free-text search against name, code, status, notes, and version number.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterDocuments(); }
        }

        /// <summary>Status filter (<c>draft</c>, <c>pending_approval</c>, <c>approved</c>, <c>published</c>, <c>expired</c>, ...).</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterDocuments(); }
        }

        /// <summary>“Type” filter mapped to <see cref="SopDocument.RelatedType"/> (e.g., SOP, Policy, Work Instruction).</summary>
        public string? TypeFilter
        {
            get => _typeFilter;
            set { _typeFilter = value; OnPropertyChanged(); FilterDocuments(); }
        }

        /// <summary>Indicates whether a long-running operation is active.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>UI status message to present non-blocking feedback.</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            private set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Common document statuses (informational; not enforced).</summary>
        public string[] AvailableStatuses => new[]
        {
            "draft", "under review", "pending approval", "approved", "published", "expired", "obsolete", "archived"
        };

        /// <summary>Real “type” values used by the filter, tied to <see cref="SopDocument.RelatedType"/>.</summary>
        public string[] AvailableTypes => new[]
        {
            "SOP", "Policy", "Work Instruction", "Form", "Template", "Checklist", "Protocol", "Report", "Other"
        };

        #endregion

        #region Commands
        /// <summary>
        /// Gets or sets the load documents command.
        /// </summary>

        public ICommand LoadDocumentsCommand { get; }
        /// <summary>
        /// Gets or sets the initiate document command.
        /// </summary>
        public ICommand InitiateDocumentCommand { get; }
        /// <summary>
        /// Gets or sets the revise document command.
        /// </summary>
        public ICommand ReviseDocumentCommand { get; }
        /// <summary>
        /// Gets or sets the assign document command.
        /// </summary>
        public ICommand AssignDocumentCommand { get; }
        /// <summary>
        /// Gets or sets the approve document command.
        /// </summary>
        public ICommand ApproveDocumentCommand { get; }
        /// <summary>
        /// Gets or sets the publish document command.
        /// </summary>
        public ICommand PublishDocumentCommand { get; }
        /// <summary>
        /// Gets or sets the expire document command.
        /// </summary>
        public ICommand ExpireDocumentCommand { get; }
        /// <summary>
        /// Gets or sets the open change control picker command.
        /// </summary>
        public ICommand OpenChangeControlPickerCommand { get; }
        /// <summary>
        /// Gets or sets the link change control command.
        /// </summary>
        public ICommand LinkChangeControlCommand { get; }
        /// <summary>
        /// Gets or sets the cancel change control picker command.
        /// </summary>
        public ICommand CancelChangeControlPickerCommand { get; }
        /// <summary>
        /// Gets or sets the export documents command.
        /// </summary>
        public ICommand ExportDocumentsCommand { get; }
        /// <summary>
        /// Gets or sets the filter changed command.
        /// </summary>
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region Methods : Data Loading

        /// <summary>Loads all documents from the database and applies the active filters.</summary>
        public async Task LoadDocumentsAsync()
        {
            IsBusy = true;
            try
            {
                var docs = await _dbService.GetAllDocumentsFullAsync().ConfigureAwait(false);

                // CS0019 fix: if the service returns List<SopDocument>, coalesce to the same type.
                Documents = new ObservableCollection<SopDocument>(docs ?? new System.Collections.Generic.List<SopDocument>());

                FilterDocuments();
                StatusMessage = $"Loaded {Documents.Count} document(s).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading documents: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Methods : Actions

        /// <summary>Creates/initiates a new document as a draft using the canonical service signature.</summary>
        public async Task InitiateDocumentAsync()
        {
            IsBusy = true;
            try
            {
                var code          = $"DOC-{DateTime.UtcNow:yyyyMMddHHmmss}";
                var name          = "New Document";
                var versionString = "1";
                var actorId       = _authService.CurrentUser?.Id ?? 0;
                var notes         = "Initiated from UI";

                var newId = await _dbService.InitiateDocumentAsync(
                    code: code,
                    name: name,
                    version: versionString,
                    filePath: null,
                    actorUserId: actorId,
                    notes: notes
                ).ConfigureAwait(false);

                await _dbService.LogDocumentAuditAsync(
                    documentId: newId,
                    action: "INITIATE",
                    actorUserId: actorId,
                    description: notes,
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId
                ).ConfigureAwait(false);

                StatusMessage = $"Document initiated (ID={newId}).";
                await LoadDocumentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Initiation failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Creates a new revision for the <see cref="SelectedDocument"/>.</summary>
        public async Task ReviseDocumentAsync()
        {
            if (SelectedDocument == null)
            {
                StatusMessage = "No document selected.";
                return;
            }

            IsBusy = true;
            try
            {
                var newVersion = (SelectedDocument.VersionNo <= 0 ? 1 : SelectedDocument.VersionNo + 1).ToString();

                await _dbService.ReviseDocumentAsync(
                    documentId: SelectedDocument.Id,
                    newVersion: newVersion,
                    newFilePath: SelectedDocument.FilePath,
                    actorUserId: _authService.CurrentUser?.Id ?? 0,
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo
                ).ConfigureAwait(false);

                await _dbService.LogDocumentAuditAsync(
                    documentId: SelectedDocument.Id,
                    action: "REVISE",
                    actorUserId: _authService.CurrentUser?.Id ?? 0,
                    description: $"Revised to v{newVersion}",
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId
                ).ConfigureAwait(false);

                StatusMessage = $"Document '{SelectedDocument.Name}' revised to v{newVersion}.";
                await LoadDocumentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Revision failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Assigns the <see cref="SelectedDocument"/> to the current user (demo).</summary>
        public async Task AssignDocumentAsync()
        {
            if (SelectedDocument == null)
            {
                StatusMessage = "No document selected.";
                return;
            }

            IsBusy = true;
            try
            {
                var currentUserId = _authService.CurrentUser?.Id ?? 0;

                await _dbService.AssignDocumentAsync(
                    documentId: SelectedDocument.Id,
                    userId: currentUserId,
                    note: "Assigned by workflow",
                    actorUserId: currentUserId,
                    ip: _currentIpAddress,
                    device: _currentDeviceInfo
                ).ConfigureAwait(false);

                await _dbService.LogDocumentAuditAsync(
                    documentId: SelectedDocument.Id,
                    action: "ASSIGN",
                    actorUserId: currentUserId,
                    description: "Assigned by workflow",
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId
                ).ConfigureAwait(false);

                StatusMessage = $"Document '{SelectedDocument.Name}' assigned.";
                await LoadDocumentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Assignment failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Approves the <see cref="SelectedDocument"/>.</summary>
        public async Task ApproveDocumentAsync()
        {
            if (SelectedDocument == null)
            {
                StatusMessage = "No document selected.";
                return;
            }

            IsBusy = true;
            try
            {
                await _dbService.ApproveDocumentAsync(
                    documentId: SelectedDocument.Id,
                    approverUserId: _authService.CurrentUser?.Id ?? 0,
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    signatureHash: null
                ).ConfigureAwait(false);

                await _dbService.LogDocumentAuditAsync(
                    documentId: SelectedDocument.Id,
                    action: "APPROVE",
                    actorUserId: _authService.CurrentUser?.Id ?? 0,
                    description: "Approved",
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId
                ).ConfigureAwait(false);

                StatusMessage = $"Document '{SelectedDocument.Name}' approved.";
                await LoadDocumentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Approval failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Publishes the <see cref="SelectedDocument"/>.</summary>
        public async Task PublishDocumentAsync()
        {
            if (SelectedDocument == null)
            {
                StatusMessage = "No document selected.";
                return;
            }

            IsBusy = true;
            try
            {
                await _dbService.PublishDocumentAsync(
                    documentId: SelectedDocument.Id,
                    actorUserId: _authService.CurrentUser?.Id ?? 0,
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo
                ).ConfigureAwait(false);

                await _dbService.LogDocumentAuditAsync(
                    documentId: SelectedDocument.Id,
                    action: "PUBLISH",
                    actorUserId: _authService.CurrentUser?.Id ?? 0,
                    description: "Published",
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId
                ).ConfigureAwait(false);

                StatusMessage = $"Document '{SelectedDocument.Name}' published.";
                await LoadDocumentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Publishing failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Expires (obsoletes) the <see cref="SelectedDocument"/>.</summary>
        public async Task ExpireDocumentAsync()
        {
            if (SelectedDocument == null)
            {
                StatusMessage = "No document selected.";
                return;
            }

            IsBusy = true;
            try
            {
                await _dbService.ExpireDocumentAsync(
                    documentId: SelectedDocument.Id,
                    actorUserId: _authService.CurrentUser?.Id ?? 0,
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo
                ).ConfigureAwait(false);

                await _dbService.LogDocumentAuditAsync(
                    documentId: SelectedDocument.Id,
                    action: "EXPIRE",
                    actorUserId: _authService.CurrentUser?.Id ?? 0,
                    description: "Expired",
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId
                ).ConfigureAwait(false);

                StatusMessage = $"Document '{SelectedDocument.Name}' expired.";
                await LoadDocumentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Expiration failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Loads available change controls and opens the picker dialog.</summary>
        public async Task OpenChangeControlPickerAsync()
        {
            if (SelectedDocument == null)
            {
                StatusMessage = "No document selected.";
                return;
            }

            IsBusy = true;
            try
            {
                var changeControls = await _dbService.GetChangeControlsAsync().ConfigureAwait(false)
                                   ?? new List<ChangeControlSummaryDto>();
                AvailableChangeControls = new ObservableCollection<ChangeControlSummaryDto>(changeControls);
                SelectedChangeControlForLink = null;
                IsChangeControlPickerOpen = true;
                StatusMessage = $"Loaded {AvailableChangeControls.Count} change control(s).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading change controls: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void CancelChangeControlPicker()
        {
            IsChangeControlPickerOpen = false;
            SelectedChangeControlForLink = null;
        }

        /// <summary>Links the selected document to a change control.</summary>
        public async Task LinkChangeControlAsync()
        {
            if (SelectedDocument == null)
            {
                StatusMessage = "No document selected.";
                return;
            }

            if (SelectedChangeControlForLink == null)
            {
                StatusMessage = "Select a change control to link.";
                return;
            }

            IsBusy = true;
            try
            {
                await _dbService.LinkChangeControlToDocumentAsync(
                    documentId: SelectedDocument.Id,
                    changeControlId: SelectedChangeControlForLink.Id,
                    actorUserId: _authService.CurrentUser?.Id ?? 0,
                    ip: _currentIpAddress,
                    device: _currentDeviceInfo
                ).ConfigureAwait(false);

                await _dbService.LogDocumentAuditAsync(
                    documentId: SelectedDocument.Id,
                    action: "LINK_CHANGE",
                    actorUserId: _authService.CurrentUser?.Id ?? 0,
                    description: $"Linked CC #{SelectedChangeControlForLink.Id} ({SelectedChangeControlForLink.Code})",
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId
                ).ConfigureAwait(false);

                StatusMessage = $"Change control '{SelectedChangeControlForLink.Code}' linked to '{SelectedDocument.Name}'.";
                CancelChangeControlPicker();
                await LoadDocumentsAsync().ConfigureAwait(false);
            }
            catch (DocumentControlLinkException ex)
            {
                StatusMessage = ex.Message;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Linking failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Exports the filtered list of documents (server-side logging only).</summary>
        public async Task ExportDocumentsAsync()
        {
            IsBusy = true;
            try
            {
                var path = await _dbService.ExportDocumentsAsync(
                    rows: FilteredDocuments.ToList(),
                    format: "zip",
                    actorUserId: _authService.CurrentUser?.Id ?? 0,
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId
                ).ConfigureAwait(false);

                await _dbService.LogDocumentAuditAsync(
                    documentId: 0,
                    action: "EXPORT",
                    actorUserId: _authService.CurrentUser?.Id ?? 0,
                    description: $"Exported {FilteredDocuments.Count} document(s) -> {path}",
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId
                ).ConfigureAwait(false);

                StatusMessage = "Documents exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Methods : Filtering & Audit

        /// <summary>Applies <see cref="SearchTerm"/>, <see cref="StatusFilter"/>, and <see cref="TypeFilter"/> to <see cref="Documents"/>.</summary>
        public void FilterDocuments()
        {
            var filtered = Documents.Where(d =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (d.Name?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.Code?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.Status?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.ReviewNotes?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    d.VersionNo.ToString().Contains(SearchTerm ?? string.Empty)) &&
                (string.IsNullOrWhiteSpace(StatusFilter) ||
                    (d.Status?.Equals(StatusFilter, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(TypeFilter) ||
                    (d.RelatedType?.Equals(TypeFilter, StringComparison.OrdinalIgnoreCase) ?? false))
            );

            FilteredDocuments = new ObservableCollection<SopDocument>(filtered);
        }

        /// <summary>Loads canonical audit entries (DTO) for a specific document.</summary>
        public async Task<ObservableCollection<AuditEntryDto>> LoadDocumentAuditAsync(int documentId)
        {
            var audits = await _dbService.GetAuditLogForEntityAsync("sop_documents", documentId).ConfigureAwait(false)
                         ?? new System.Collections.Generic.List<AuditEntryDto>();
            return new ObservableCollection<AuditEntryDto>(audits);
        }

        #endregion

        #region INotifyPropertyChanged

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raises <see cref="PropertyChanged"/> for UI binding updates.</summary>
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));

        #endregion
    }
}
