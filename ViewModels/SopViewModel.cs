using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// View model that loads, filters, and manages SOP documents via <see cref="DatabaseService"/>.
    /// </summary>
    public class SopViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly AuthService _authService;

        private ObservableCollection<SopDocument> _documents = new();
        private ObservableCollection<SopDocument> _filteredDocuments = new();
        private SopDocument? _selectedDocument;
        private SopDocument _draftDocument = new();
        private string? _searchTerm;
        private string? _statusFilter;
        private string? _processFilter;
        private DateTime? _issuedFrom;
        private DateTime? _issuedTo;
        private bool _includeOnlyActive;
        private bool _isBusy;
        private string? _statusMessage;

        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        /// <summary>
        /// Initializes the SOP view model and triggers initial data load.
        /// </summary>
        /// <param name="databaseService">Database service that exposes SOP helpers.</param>
        /// <param name="authService">Authentication/session context.</param>
        /// <exception cref="ArgumentNullException">Thrown when dependencies are null.</exception>
        public SopViewModel(DatabaseService databaseService, AuthService authService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId = _authService.CurrentSessionId ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? string.Empty;
            _currentIpAddress = _authService.CurrentIpAddress ?? "0.0.0.0";

            LoadDocumentsCommand = new AsyncRelayCommand(LoadDocumentsAsync);
            CreateDocumentCommand = new AsyncRelayCommand(CreateDocumentAsync, () => !IsBusy);
            UpdateDocumentCommand = new AsyncRelayCommand(UpdateDocumentAsync, () => !IsBusy && SelectedDocument != null);
            DeleteDocumentCommand = new AsyncRelayCommand(DeleteDocumentAsync, () => !IsBusy && SelectedDocument != null);
            FilterChangedCommand = new RelayCommand(FilterDocuments);
            ResetDraftCommand = new RelayCommand(ResetDraftDocument);

            ResetDraftDocument();
            _ = LoadDocumentsAsync();
        }

        /// <summary>All SOP documents loaded from the database.</summary>
        public ObservableCollection<SopDocument> Documents
        {
            get => _documents;
            private set
            {
                _documents = value ?? new ObservableCollection<SopDocument>();
                OnPropertyChanged();
            }
        }

        /// <summary>Filtered SOP documents based on active filters.</summary>
        public ObservableCollection<SopDocument> FilteredDocuments
        {
            get => _filteredDocuments;
            private set
            {
                _filteredDocuments = value ?? new ObservableCollection<SopDocument>();
                OnPropertyChanged();
            }
        }

        /// <summary>The currently selected SOP document for edit/delete operations.</summary>
        public SopDocument? SelectedDocument
        {
            get => _selectedDocument;
            set
            {
                if (!ReferenceEquals(_selectedDocument, value))
                {
                    _selectedDocument = value;
                    OnPropertyChanged();
                    NotifyCommandsCanExecuteChanged();
                }
            }
        }

        /// <summary>Draft SOP used for creation scenarios.</summary>
        public SopDocument DraftDocument
        {
            get => _draftDocument;
            private set
            {
                _draftDocument = value ?? new SopDocument();
                OnPropertyChanged();
            }
        }

        /// <summary>Free-text search across SOP code, name, description, status and process.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (!string.Equals(_searchTerm, value, StringComparison.Ordinal))
                {
                    _searchTerm = value;
                    OnPropertyChanged();
                    FilterDocuments();
                }
            }
        }

        /// <summary>Status filter (draft, active, approved, published, expired, archived, etc.).</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set
            {
                if (!string.Equals(_statusFilter, value, StringComparison.Ordinal))
                {
                    _statusFilter = value;
                    OnPropertyChanged();
                    FilterDocuments();
                }
            }
        }

        /// <summary>Process filter (Quality, Production, Maintenance, etc.).</summary>
        public string? ProcessFilter
        {
            get => _processFilter;
            set
            {
                if (!string.Equals(_processFilter, value, StringComparison.Ordinal))
                {
                    _processFilter = value;
                    OnPropertyChanged();
                    FilterDocuments();
                }
            }
        }

        /// <summary>Filters SOPs issued on or after the specified date.</summary>
        public DateTime? IssuedFrom
        {
            get => _issuedFrom;
            set
            {
                if (_issuedFrom != value)
                {
                    _issuedFrom = value;
                    OnPropertyChanged();
                    FilterDocuments();
                }
            }
        }

        /// <summary>Filters SOPs issued on or before the specified date.</summary>
        public DateTime? IssuedTo
        {
            get => _issuedTo;
            set
            {
                if (_issuedTo != value)
                {
                    _issuedTo = value;
                    OnPropertyChanged();
                    FilterDocuments();
                }
            }
        }

        /// <summary>When true shows only SOPs with active/published status.</summary>
        public bool IncludeOnlyActive
        {
            get => _includeOnlyActive;
            set
            {
                if (_includeOnlyActive != value)
                {
                    _includeOnlyActive = value;
                    OnPropertyChanged();
                    FilterDocuments();
                }
            }
        }

        /// <summary>Indicates an active long-running operation.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                    NotifyCommandsCanExecuteChanged();
                }
            }
        }

        /// <summary>User-facing status or error message.</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (!string.Equals(_statusMessage, value, StringComparison.Ordinal))
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>Common SOP lifecycle statuses for UI filter binding.</summary>
        public string[] AvailableStatuses => new[]
        {
            "draft", "under review", "pending approval", "approved", "published", "expired", "archived"
        };

        /// <summary>Common processes/categories for SOP classification.</summary>
        public string[] AvailableProcesses => new[]
        {
            "Quality", "Production", "Maintenance", "Validation", "Cleaning", "Safety", "Logistics", "IT", "HR", "Other"
        };

        /// <summary>Command that loads SOP documents from the database.</summary>
        public ICommand LoadDocumentsCommand { get; }

        /// <summary>Command that persists <see cref="DraftDocument"/> as a new SOP.</summary>
        public ICommand CreateDocumentCommand { get; }

        /// <summary>Command that updates the <see cref="SelectedDocument"/>.</summary>
        public ICommand UpdateDocumentCommand { get; }

        /// <summary>Command that deletes the <see cref="SelectedDocument"/>.</summary>
        public ICommand DeleteDocumentCommand { get; }

        /// <summary>Command invoked when filter UI changes (re-applies filters).</summary>
        public ICommand FilterChangedCommand { get; }

        /// <summary>Command that resets <see cref="DraftDocument"/> to defaults.</summary>
        public ICommand ResetDraftCommand { get; }

        /// <summary>
        /// Applies a database snapshot to the view-model, replacing the current
        /// document collections and reapplying the active filters.
        /// </summary>
        /// <param name="documents">Snapshot retrieved from the persistence layer.</param>
        public void ApplyDocuments(IReadOnlyList<SopDocument> documents)
        {
            var snapshot = documents ?? Array.Empty<SopDocument>();
            var previousId = SelectedDocument?.Id;

            Documents = new ObservableCollection<SopDocument>(snapshot);
            FilterDocuments();

            if (previousId.HasValue)
            {
                SelectedDocument = Documents.FirstOrDefault(d => d.Id == previousId.Value);
            }
            else if (Documents.Count == 0)
            {
                SelectedDocument = null;
            }
        }

        /// <summary>
        /// Prepares a SOP document for persistence, mirroring the sanitization
        /// and validation logic used by the legacy command handlers.
        /// </summary>
        /// <param name="document">Document to normalize.</param>
        /// <param name="isNew">Indicates whether the document is being created.</param>
        /// <returns>A deep copy ready for persistence.</returns>
        public SopDocument PrepareForSave(SopDocument document, bool isNew)
            => PrepareDocumentForPersistence(document, isNew);

        /// <summary>Loads SOP documents and applies the active filters.</summary>
        public async Task LoadDocumentsAsync()
        {
            IsBusy = true;
            try
            {
                var documents = await _databaseService.GetSopDocumentsAsync().ConfigureAwait(false);
                Documents = new ObservableCollection<SopDocument>(documents ?? new List<SopDocument>());
                FilterDocuments();
                StatusMessage = $"Loaded {Documents.Count} SOP document(s).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load SOP documents: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Creates a new SOP using <see cref="DraftDocument"/>.</summary>
        public async Task CreateDocumentAsync()
        {
            SopDocument prepared;
            try
            {
                prepared = PrepareForSave(DraftDocument, isNew: true);
            }
            catch (InvalidOperationException validationEx)
            {
                StatusMessage = validationEx.Message;
                return;
            }

            var actorId = _authService.CurrentUser?.Id ?? 0;

            IsBusy = true;
            try
            {
                var newId = await _databaseService.CreateSopDocumentAsync(
                    prepared,
                    actorId,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId).ConfigureAwait(false);

                StatusMessage = $"Created SOP '{prepared.Name}' (ID={newId}).";
                await LoadDocumentsAsync().ConfigureAwait(false);
                ResetDraftDocument();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to create SOP: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Updates the <see cref="SelectedDocument"/> in the database.</summary>
        public async Task UpdateDocumentAsync()
        {
            if (SelectedDocument == null)
            {
                StatusMessage = "Select a SOP document to update.";
                return;
            }

            SopDocument prepared;
            try
            {
                prepared = PrepareForSave(SelectedDocument, isNew: false);
                prepared.Id = SelectedDocument.Id;
            }
            catch (InvalidOperationException validationEx)
            {
                StatusMessage = validationEx.Message;
                return;
            }

            var actorId = _authService.CurrentUser?.Id ?? 0;

            IsBusy = true;
            try
            {
                await _databaseService.UpdateSopDocumentAsync(
                    prepared,
                    actorId,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId).ConfigureAwait(false);

                StatusMessage = $"Updated SOP '{prepared.Name}'.";
                await LoadDocumentsAsync().ConfigureAwait(false);
                SelectedDocument = Documents.FirstOrDefault(d => d.Id == prepared.Id);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to update SOP: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Deletes the <see cref="SelectedDocument"/>.</summary>
        public async Task DeleteDocumentAsync()
        {
            if (SelectedDocument == null)
            {
                StatusMessage = "Select a SOP document to delete.";
                return;
            }

            var document = SelectedDocument;
            var actorId = _authService.CurrentUser?.Id ?? 0;

            IsBusy = true;
            try
            {
                await _databaseService.DeleteSopDocumentAsync(
                    document.Id,
                    actorId,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId).ConfigureAwait(false);

                StatusMessage = $"Deleted SOP '{document.Name}' (ID={document.Id}).";
                await LoadDocumentsAsync().ConfigureAwait(false);
                SelectedDocument = null;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to delete SOP: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Applies the active filters to <see cref="Documents"/>.</summary>
        public void FilterDocuments()
        {
            if (Documents == null || Documents.Count == 0)
            {
                FilteredDocuments = new ObservableCollection<SopDocument>();
                return;
            }

            IEnumerable<SopDocument> query = Documents;

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();
                query = query.Where(doc =>
                    (!string.IsNullOrWhiteSpace(doc.Name) && doc.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(doc.Code) && doc.Code.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(doc.Description) && doc.Description.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(doc.Process) && doc.Process.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(doc.Status) && doc.Status.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    doc.VersionNo.ToString().Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter))
            {
                query = query.Where(doc =>
                    doc.Status?.Equals(StatusFilter, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (!string.IsNullOrWhiteSpace(ProcessFilter))
            {
                query = query.Where(doc =>
                    doc.Process?.Equals(ProcessFilter, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (IssuedFrom.HasValue)
            {
                query = query.Where(doc => doc.DateIssued >= IssuedFrom.Value);
            }

            if (IssuedTo.HasValue)
            {
                query = query.Where(doc => doc.DateIssued <= IssuedTo.Value);
            }

            if (IncludeOnlyActive)
            {
                query = query.Where(doc =>
                    doc.Status?.Equals("active", StringComparison.OrdinalIgnoreCase) == true ||
                    doc.Status?.Equals("published", StringComparison.OrdinalIgnoreCase) == true);
            }

            FilteredDocuments = new ObservableCollection<SopDocument>(query.ToList());
        }

        /// <summary>Resets <see cref="DraftDocument"/> to sane defaults for new SOP creation.</summary>
        private void ResetDraftDocument()
        {
            DraftDocument = new SopDocument
            {
                Code = GenerateSopCode(),
                Name = string.Empty,
                Description = string.Empty,
                Process = string.Empty,
                Language = "hr",
                DateIssued = DateTime.UtcNow,
                Status = "draft",
                FilePath = string.Empty,
                VersionNo = 1,
                ResponsibleUserId = _authService.CurrentUser?.Id ?? 0,
                CreatedById = _authService.CurrentUser?.Id,
                LastModified = DateTime.UtcNow,
                LastModifiedById = _authService.CurrentUser?.Id ?? 0,
                SourceIp = _currentIpAddress,
                AiTags = string.Empty
            };
        }

        /// <summary>Validates and prepares a SOP document before persistence.</summary>
        private SopDocument PrepareDocumentForPersistence(SopDocument document, bool isNew)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var copy = document.DeepCopy();

            copy.Code = string.IsNullOrWhiteSpace(copy.Code)
                ? GenerateSopCode()
                : copy.Code.Trim();

            if (string.IsNullOrWhiteSpace(copy.Name))
            {
                throw new InvalidOperationException("SOP name is required.");
            }

            copy.Name = copy.Name.Trim();

            if (string.IsNullOrWhiteSpace(copy.FilePath))
            {
                throw new InvalidOperationException("SOP file path is required.");
            }

            copy.FilePath = copy.FilePath.Trim();

            if (copy.VersionNo <= 0)
            {
                copy.VersionNo = 1;
            }

            if (copy.DateIssued == default)
            {
                copy.DateIssued = DateTime.UtcNow;
            }

            if (copy.LastModified == default)
            {
                copy.LastModified = DateTime.UtcNow;
            }

            if (string.IsNullOrWhiteSpace(copy.Status))
            {
                copy.Status = isNew ? "draft" : "active";
            }

            copy.Status = copy.Status.Trim();

            copy.Description = copy.Description ?? string.Empty;
            copy.Process = copy.Process ?? string.Empty;
            copy.Language = string.IsNullOrWhiteSpace(copy.Language) ? "hr" : copy.Language.Trim();
            copy.ReviewNotes = copy.ReviewNotes ?? string.Empty;
            copy.PdfMetadata = copy.PdfMetadata ?? string.Empty;
            copy.Comment = copy.Comment ?? string.Empty;
            copy.AiTags = copy.AiTags ?? string.Empty;
            copy.SourceIp = string.IsNullOrWhiteSpace(copy.SourceIp) ? _currentIpAddress : copy.SourceIp.Trim();

            copy.Attachments ??= new List<string>();
            copy.ApproverIds ??= new List<int>();
            copy.Approvers ??= new List<User>();
            copy.ApprovalTimestamps ??= new List<DateTime>();

            var currentUserId = _authService.CurrentUser?.Id ?? 0;

            if (isNew)
            {
                if (copy.ResponsibleUserId == 0 && currentUserId > 0)
                {
                    copy.ResponsibleUserId = currentUserId;
                }

                if (!copy.CreatedById.HasValue && currentUserId > 0)
                {
                    copy.CreatedById = currentUserId;
                }
            }

            if (currentUserId > 0)
            {
                copy.LastModifiedById = currentUserId;
            }

            copy.LastModified = EnsureUtc(copy.LastModified);
            copy.DateIssued = EnsureUtc(copy.DateIssued);
            if (copy.DateExpiry.HasValue)
            {
                copy.DateExpiry = EnsureUtc(copy.DateExpiry.Value);
            }

            if (copy.NextReviewDate.HasValue)
            {
                copy.NextReviewDate = EnsureUtc(copy.NextReviewDate.Value);
            }

            return copy;
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                _ => value.ToUniversalTime()
            };
        }

        private static string GenerateSopCode() =>
            $"SOP-{DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}";

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));

        private void NotifyCommandsCanExecuteChanged()
        {
            (LoadDocumentsCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (CreateDocumentCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (UpdateDocumentCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (DeleteDocumentCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }
    }
}