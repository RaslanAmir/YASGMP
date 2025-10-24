using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Models.DTO; // audits
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// ViewModel for periodic/recurring automated jobs (GMP/Annex 11/21 CFR Part 11 compliant).
    /// Provides: load, add, update, delete, acknowledge, execute, filter, export, and audit retrieval.
    /// </summary>
    public class SchedulerViewModel : INotifyPropertyChanged
    {
        #region === Fields & Ctor ===================================================

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<ScheduledJob> _scheduledJobs = new();
        private ObservableCollection<ScheduledJob> _filteredJobs = new();

        // Selections/filters are legitimately nullable in UI lifecycle
        private ScheduledJob? _selectedJob;
        private string? _searchTerm;
        private string? _jobTypeFilter;
        private string? _statusFilter;

        private bool _isBusy;
        private string? _statusMessage;

        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        /// <summary>
        /// Initializes a new instance with required services.
        /// </summary>
        /// <param name="dbService">Database gateway.</param>
        /// <param name="authService">Authentication/session context.</param>
        /// <exception cref="ArgumentNullException">Thrown if a dependency is null.</exception>
        public SchedulerViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            // Null-safe copies (eliminate CS8601): enforce never-null strings for audit trails
            _currentSessionId = _authService.CurrentSessionId ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? "ui";
            _currentIpAddress  = _authService.CurrentIpAddress  ?? "0.0.0.0";

            // Commands (safe predicates – avoid CS8602 on SelectedJob.Status)
            LoadJobsCommand        = new AsyncRelayCommand(LoadJobsAsync);
            AddJobCommand          = new AsyncRelayCommand(AddJobAsync,         () => !IsBusy);
            UpdateJobCommand       = new AsyncRelayCommand(UpdateJobAsync,      () => !IsBusy && SelectedJob is not null);
            DeleteJobCommand       = new AsyncRelayCommand(DeleteJobAsync,      () => !IsBusy && SelectedJob is not null);
            AcknowledgeJobCommand  = new AsyncRelayCommand(AcknowledgeJobAsync, () => !IsBusy && (SelectedJob?.Status == "pending_ack"));
            ExecuteJobCommand      = new AsyncRelayCommand(ExecuteJobAsync,     () => !IsBusy && (SelectedJob?.Status == "scheduled"));
            ExportJobsCommand      = new AsyncRelayCommand(ExportJobsAsync,     () => !IsBusy);
            FilterChangedCommand   = new RelayCommand(FilterJobs);

            _ = LoadJobsAsync();
        }

        #endregion

        #region === Properties ======================================================

        /// <summary>All scheduled jobs retrieved for the view.</summary>
        public ObservableCollection<ScheduledJob> ScheduledJobs
        {
            get => _scheduledJobs;
            set { _scheduledJobs = value; OnPropertyChanged(); }
        }

        /// <summary>Filtered projection for the UI grid/list.</summary>
        public ObservableCollection<ScheduledJob> FilteredJobs
        {
            get => _filteredJobs;
            set { _filteredJobs = value; OnPropertyChanged(); }
        }

        /// <summary>Currently selected job (can be null when nothing is selected).</summary>
        public ScheduledJob? SelectedJob
        {
            get => _selectedJob;
            set { _selectedJob = value; OnPropertyChanged(); }
        }

        /// <summary>Free-text search over job name, type, status and entity type.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterJobs(); }
        }

        /// <summary>Selected job-type filter.</summary>
        public string? JobTypeFilter
        {
            get => _jobTypeFilter;
            set { _jobTypeFilter = value; OnPropertyChanged(); FilterJobs(); }
        }

        /// <summary>Selected status filter.</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterJobs(); }
        }

        /// <summary>UI busy flag (disables mutating commands).</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>User-facing status text.</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Allowed job type presets.</summary>
        public string[] AvailableJobTypes => new[] { "maintenance","calibration","notification","report","backup","external_sync","custom" };

        /// <summary>Allowed job statuses.</summary>
        public string[] AvailableStatuses => new[] { "scheduled","in_progress","pending_ack","overdue","completed","failed","canceled" };

        /// <summary>Role gate for job management actions.</summary>
        public bool CanManageJobs => _authService.CurrentUser?.Role is "admin" or "superadmin" or "qa";

        #endregion

        #region === Commands ========================================================

        public ICommand LoadJobsCommand { get; }
        public ICommand AddJobCommand { get; }
        public ICommand UpdateJobCommand { get; }
        public ICommand DeleteJobCommand { get; }
        public ICommand AcknowledgeJobCommand { get; }
        public ICommand ExecuteJobCommand { get; }
        public ICommand ExportJobsCommand { get; }
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region === Methods =========================================================

        /// <summary>Loads all scheduled jobs and applies current filters.</summary>
        public async Task LoadJobsAsync()
        {
            IsBusy = true;
            try
            {
                var jobs = await _dbService.GetAllScheduledJobsFullAsync().ConfigureAwait(false);
                ScheduledJobs = new ObservableCollection<ScheduledJob>(jobs);
                FilterJobs();
                StatusMessage = $"Loaded {ScheduledJobs.Count} jobs.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading jobs: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Adds a new scheduled job with safe defaults and audits the creation.</summary>
        public async Task AddJobAsync()
        {
            IsBusy = true;
            try
            {
                var newJob = new ScheduledJob
                {
                    Name = "New Job",
                    JobType = JobTypeFilter ?? "maintenance",
                    Status = "scheduled",
                    NextDue = DateTime.UtcNow.AddDays(7),
                    RecurrencePattern = "Weekly",
                    // Avoid CS8625: do not assign null to possibly non-nullable model members
                    EntityType = string.Empty,
                    EntityId = 0,
                    CreatedBy = _authService.CurrentUser?.UserName ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    DeviceInfo = _currentDeviceInfo,
                    SessionId = _currentSessionId,
                    IpAddress = _currentIpAddress
                };

                await _dbService.AddScheduledJobAsync(newJob).ConfigureAwait(false);
                await _dbService.LogScheduledJobAuditAsync(newJob, "CREATE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);

                StatusMessage = "Scheduled job added.";
                await LoadJobsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Add job failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Updates the selected job and records a detailed audit trail.</summary>
        public async Task UpdateJobAsync()
        {
            if (SelectedJob is null) { StatusMessage = "No job selected."; return; }
            IsBusy = true;
            try
            {
                var userId = _authService.CurrentUser?.Id ?? 1; // Avoid CS8602
                await _dbService.UpdateScheduledJobAsync(SelectedJob, userId, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogScheduledJobAuditAsync(SelectedJob, "UPDATE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);

                StatusMessage = $"Scheduled job '{SelectedJob.Name}' updated.";
                await LoadJobsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Deletes the selected job and logs the operation.</summary>
        public async Task DeleteJobAsync()
        {
            if (SelectedJob is null) { StatusMessage = "No job selected."; return; }
            IsBusy = true;
            try
            {
                await _dbService.DeleteScheduledJobAsync(SelectedJob.Id, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogScheduledJobAuditAsync(SelectedJob, "DELETE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);

                StatusMessage = $"Scheduled job '{SelectedJob.Name}' deleted.";
                await LoadJobsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Acknowledges the selected job and records the acknowledgement in audit logs.</summary>
        public async Task AcknowledgeJobAsync()
        {
            if (SelectedJob is null) { StatusMessage = "No job selected."; return; }
            IsBusy = true;
            try
            {
                var userId = _authService.CurrentUser?.Id ?? 1; // Avoid CS8602
                await _dbService.AcknowledgeScheduledJobAsync(SelectedJob.Id, userId, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogScheduledJobAuditAsync(SelectedJob, "ACK", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);

                StatusMessage = $"Job '{SelectedJob.Name}' acknowledged.";
                await LoadJobsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Acknowledge failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Executes the selected job and logs execution metadata.</summary>
        public async Task ExecuteJobAsync()
        {
            if (SelectedJob is null) { StatusMessage = "No job selected."; return; }
            IsBusy = true;
            try
            {
                var userId = _authService.CurrentUser?.Id ?? 1; // Avoid CS8602
                await _dbService.ExecuteScheduledJobAsync(SelectedJob.Id, userId, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);
                await _dbService.LogScheduledJobAuditAsync(SelectedJob, "EXECUTE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);

                StatusMessage = $"Job '{SelectedJob.Name}' executed.";
                await LoadJobsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Execution failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Exports filtered jobs and writes an export audit record.</summary>
        public async Task ExportJobsAsync()
        {
            IsBusy = true;
            try
            {
                // Disambiguate overloads by naming 'format'; also null-safe ToList
                var fmt = await YasGMP.Helpers.ExportFormatPrompt.PromptAsync();
                await _dbService.ExportScheduledJobsAsync(
                    FilteredJobs?.ToList() ?? new System.Collections.Generic.List<ScheduledJob>(),
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId,
                    format: fmt).ConfigureAwait(false);

                await _dbService.LogScheduledJobAuditAsync(null, "EXPORT", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);
                StatusMessage = "Scheduled jobs exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Applies client-side filters based on search and dropdowns.</summary>
        public void FilterJobs()
        {
            var filtered = ScheduledJobs.Where(j =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (j.Name?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (j.JobType?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (j.Status?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (j.EntityType?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(JobTypeFilter) || j.JobType == JobTypeFilter) &&
                (string.IsNullOrWhiteSpace(StatusFilter) || j.Status == StatusFilter));

            FilteredJobs = new ObservableCollection<ScheduledJob>(filtered);
        }

        /// <summary>Loads audit entries for a given job id.</summary>
        /// <param name="jobId">Scheduled job primary key.</param>
        /// <returns>Collection of <see cref="AuditEntryDto"/> entries.</returns>
        public async Task<ObservableCollection<AuditEntryDto>> LoadJobAuditAsync(int jobId)
        {
            var audits = await _dbService.GetAuditLogForEntityAsync("scheduled_jobs", jobId).ConfigureAwait(false);
            return new ObservableCollection<AuditEntryDto>(audits);
        }

        #endregion

        #region === INotifyPropertyChanged ==========================================

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Notifies the UI of a property change in a null-safe manner.</summary>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}
