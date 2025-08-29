using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Helpers;
using CommunityToolkit.Mvvm.Input;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// Super ultra-robust ViewModel for GMP Training Records management.
    /// ✅ GMP/Annex 11/21 CFR Part 11: session planning, multi-level approval, assignment, e-sign, reminders, expiry, audit.
    /// ✅ Links to users, SOPs, roles, effectiveness, attachments, analytics, dashboard, integration, future-proofed.
    /// </summary>
    public class TrainingRecordViewModel : INotifyPropertyChanged
    {
        #region Fields & Constructor

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<TrainingRecord> _trainingRecords = new();
        private ObservableCollection<TrainingRecord> _filteredTrainingRecords = new();

        // Selections/filters can legitimately be null -> mark nullable
        private TrainingRecord? _selectedTrainingRecord;
        private string? _searchTerm;
        private string? _statusFilter;
        private string? _typeFilter;

        private bool _isBusy;
        private string? _statusMessage;

        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        /// <summary>Initializes the TrainingRecordViewModel and sets up all commands and loads data.</summary>
        public TrainingRecordViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            // Null-safe coalescing to satisfy CS8601 and to keep forensics strings non-null
            _currentSessionId = _authService.CurrentSessionId ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? "ui";
            _currentIpAddress  = _authService.CurrentIpAddress  ?? "0.0.0.0";

            LoadTrainingRecordsCommand      = new AsyncRelayCommand(LoadTrainingRecordsAsync);
            InitiateTrainingRecordCommand   = new AsyncRelayCommand(InitiateTrainingRecordAsync, () => !IsBusy);
            AssignTrainingRecordCommand     = new AsyncRelayCommand(AssignTrainingRecordAsync,    () => !IsBusy && SelectedTrainingRecord is not null);
            ApproveTrainingRecordCommand    = new AsyncRelayCommand(ApproveTrainingRecordAsync,   () => !IsBusy && SelectedTrainingRecord is not null && SelectedTrainingRecord.Status == "pending_approval");
            CompleteTrainingRecordCommand   = new AsyncRelayCommand(CompleteTrainingRecordAsync,  () => !IsBusy && SelectedTrainingRecord is not null && SelectedTrainingRecord.Status == "assigned");
            CloseTrainingRecordCommand      = new AsyncRelayCommand(CloseTrainingRecordAsync,     () => !IsBusy && SelectedTrainingRecord is not null && SelectedTrainingRecord.Status == "completed");
            ExportTrainingRecordsCommand    = new AsyncRelayCommand(ExportTrainingRecordsAsync,   () => !IsBusy);
            FilterChangedCommand            = new RelayCommand(FilterTrainingRecords);

            _ = LoadTrainingRecordsAsync();
        }

        #endregion

        #region Properties

        /// <summary>All training records in the system.</summary>
        public ObservableCollection<TrainingRecord> TrainingRecords
        {
            get => _trainingRecords;
            set { _trainingRecords = value; OnPropertyChanged(); }
        }

        /// <summary>Filtered training records for UI.</summary>
        public ObservableCollection<TrainingRecord> FilteredTrainingRecords
        {
            get => _filteredTrainingRecords;
            set { _filteredTrainingRecords = value; OnPropertyChanged(); }
        }

        /// <summary>The currently selected training record (nullable by design).</summary>
        public TrainingRecord? SelectedTrainingRecord
        {
            get => _selectedTrainingRecord;
            set { _selectedTrainingRecord = value; OnPropertyChanged(); }
        }

        /// <summary>Search term for user, SOP, training type, etc.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterTrainingRecords(); }
        }

        /// <summary>Status filter (planned, assigned, pending_approval, completed, closed, expired, etc.).</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterTrainingRecords(); }
        }

        /// <summary>Training type filter (GMP, Safety, SOP, IT, etc.).</summary>
        public string? TypeFilter
        {
            get => _typeFilter;
            set { _typeFilter = value; OnPropertyChanged(); FilterTrainingRecords(); }
        }

        /// <summary>Operation busy indicator.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>UI status message.</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Available training record statuses.</summary>
        public string[] AvailableStatuses => new[] { "planned", "assigned", "pending_approval", "completed", "closed", "expired", "rejected" };

        /// <summary>Available training record types.</summary>
        public string[] AvailableTypes => new[] { "GMP", "Safety", "SOP", "IT", "Process", "Quality", "HR", "Other" };

        #endregion

        #region Commands

        public ICommand LoadTrainingRecordsCommand { get; }
        public ICommand InitiateTrainingRecordCommand { get; }
        public ICommand AssignTrainingRecordCommand { get; }
        public ICommand ApproveTrainingRecordCommand { get; }
        public ICommand CompleteTrainingRecordCommand { get; }
        public ICommand CloseTrainingRecordCommand { get; }
        public ICommand ExportTrainingRecordsCommand { get; }
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region Methods

        /// <summary>Loads all training records, including effectiveness, expiry, attachments, audit.</summary>
        public async Task LoadTrainingRecordsAsync()
        {
            IsBusy = true;
            try
            {
                var records = await _dbService.GetAllTrainingRecordsFullAsync().ConfigureAwait(false);
                TrainingRecords = new ObservableCollection<TrainingRecord>(records ?? new());
                FilterTrainingRecords();
                StatusMessage = $"Loaded {TrainingRecords.Count} training records.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading training records: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Initiates a new training record (plan).</summary>
        public async Task InitiateTrainingRecordAsync()
        {
            IsBusy = true;
            try
            {
                var newRecord = new TrainingRecord
                {
                    Title = "New Training",
                    TrainingType = TypeFilter ?? "GMP",
                    Status = "planned",
                    // FIX (CS8601): PlannedBy (non-nullable) receives a possibly-null value. Coalesce to empty.
                    PlannedBy = _authService.CurrentUser?.UserName ?? string.Empty,
                    PlannedAt = DateTime.UtcNow,
                    AssignedTo = null,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    ExpiryDate = DateTime.UtcNow.AddYears(1),
                    EffectivenessCheck = false,
                    Attachments = new(),
                    WorkflowHistory = new(),
                    DeviceInfo = _currentDeviceInfo,
                    SessionId = _currentSessionId,
                    IpAddress = _currentIpAddress
                };

                await _dbService.InitiateTrainingRecordAsync(newRecord).ConfigureAwait(false);
                await _dbService.LogTrainingRecordAuditAsync(newRecord, "INITIATE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);

                StatusMessage = "Training record initiated.";
                await LoadTrainingRecordsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Initiation failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Assigns a training record to a user/role.</summary>
        public async Task AssignTrainingRecordAsync()
        {
            if (SelectedTrainingRecord == null) { StatusMessage = "No training record selected."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.AssignTrainingRecordAsync(SelectedTrainingRecord.Id, actorId, _currentIpAddress, _currentDeviceInfo, _currentSessionId, "Assigned by workflow").ConfigureAwait(false);
                await _dbService.LogTrainingRecordAuditAsync(SelectedTrainingRecord, "ASSIGN", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);

                StatusMessage = $"Training record '{SelectedTrainingRecord.Title}' assigned.";
                await LoadTrainingRecordsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Assignment failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Approves a training record (manager approval, e-sign, audit).</summary>
        public async Task ApproveTrainingRecordAsync()
        {
            if (SelectedTrainingRecord == null) { StatusMessage = "No training record selected."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.ApproveTrainingRecordAsync(SelectedTrainingRecord.Id, actorId, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogTrainingRecordAuditAsync(SelectedTrainingRecord, "APPROVE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);

                StatusMessage = $"Training record '{SelectedTrainingRecord.Title}' approved.";
                await LoadTrainingRecordsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Approval failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Marks a training record as completed (user, e-sign).</summary>
        public async Task CompleteTrainingRecordAsync()
        {
            if (SelectedTrainingRecord == null) { StatusMessage = "No training record selected."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.CompleteTrainingRecordAsync(SelectedTrainingRecord.Id, actorId, _currentIpAddress, _currentDeviceInfo, _currentSessionId, "Training completed").ConfigureAwait(false);
                await _dbService.LogTrainingRecordAuditAsync(SelectedTrainingRecord, "COMPLETE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);

                StatusMessage = $"Training record '{SelectedTrainingRecord.Title}' completed.";
                await LoadTrainingRecordsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Completion failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Closes a training record after effectiveness check and audit.</summary>
        public async Task CloseTrainingRecordAsync()
        {
            if (SelectedTrainingRecord == null) { StatusMessage = "No training record selected."; return; }
            IsBusy = true;
            try
            {
                var actorId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.CloseTrainingRecordAsync(SelectedTrainingRecord.Id, actorId, _currentIpAddress, _currentDeviceInfo, _currentSessionId, "Closed after effectiveness check").ConfigureAwait(false);
                await _dbService.LogTrainingRecordAuditAsync(SelectedTrainingRecord, "CLOSE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);

                StatusMessage = $"Training record '{SelectedTrainingRecord.Title}' closed.";
                await LoadTrainingRecordsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Closure failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Exports training records for compliance/audit.</summary>
        public async Task ExportTrainingRecordsAsync()
        {
            IsBusy = true;
            try
            {
                await _dbService.ExportTrainingRecordsAsync(FilteredTrainingRecords.ToList(), _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogTrainingRecordAuditAsync(null, "EXPORT", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);
                StatusMessage = "Training records exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Real-time filter for training records.</summary>
        public void FilterTrainingRecords()
        {
            var filtered = TrainingRecords.Where(t =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (t.Title?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (t.TrainingType?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (t.Status?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (t.AssignedToName?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (t.LinkedSOP?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(StatusFilter) || t.Status == StatusFilter) &&
                (string.IsNullOrWhiteSpace(TypeFilter) || t.TrainingType == TypeFilter)
            );
            FilteredTrainingRecords = new ObservableCollection<TrainingRecord>(filtered);
        }

        /// <summary>Can the current user manage training records?</summary>
        public bool CanManageTrainingRecord =>
            _authService.CurrentUser?.Role == "admin" ||
            _authService.CurrentUser?.Role == "superadmin" ||
            _authService.CurrentUser?.Role == "hr" ||
            _authService.CurrentUser?.Role == "qa";

        #endregion

        #region Audit/Auxiliary

        /// <summary>Maps DTO audit entries to domain <see cref="AuditLogEntry"/> (nullable tolerant).</summary>
        private static AuditLogEntry? MapAuditEntryDtoToAuditLogEntry(YasGMP.Models.DTO.AuditEntryDto? dto)
        {
            if (dto == null) return null;
            return new AuditLogEntry
            {
                Id               = dto.Id ?? 0,
                EntityType       = dto.Entity ?? string.Empty,
                EntityId         = int.TryParse(dto.EntityId, out var eid) ? eid : 0,
                PerformedBy      = !string.IsNullOrEmpty(dto.Username) ? dto.Username : dto.UserId?.ToString(),
                Action           = dto.Action ?? string.Empty,
                OldValue         = dto.OldValue,
                NewValue         = dto.NewValue,
                ChangedAt        = dto.Timestamp,
                DeviceInfo       = dto.DeviceInfo,
                // FIXES (CS8601): coalesce possibly-null strings assigned to non-nullable properties
                IpAddress        = dto.IpAddress ?? string.Empty,
                SessionId        = dto.SessionId,
                DigitalSignature = dto.DigitalSignature,
                Note             = dto.Note ?? string.Empty
            };
        }

        /// <summary>Loads audit history for a specific training record.</summary>
        public async Task<ObservableCollection<AuditLogEntry>> LoadTrainingRecordAuditAsync(int trainingRecordId)
        {
            var dtos = await _dbService.GetAuditLogForEntityAsync("training_records", trainingRecordId).ConfigureAwait(false);
            var audits = (dtos?.Select(MapAuditEntryDtoToAuditLogEntry)
                              .Where(x => x != null)
                              .Select(x => x!) ?? Enumerable.Empty<AuditLogEntry>());
            return new ObservableCollection<AuditLogEntry>(audits);
        }

        #endregion

        #region INotifyPropertyChanged

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Safely raises <see cref="PropertyChanged"/>.</summary>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}
