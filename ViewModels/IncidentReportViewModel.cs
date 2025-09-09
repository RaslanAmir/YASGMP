using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Models.DTO; // kept for future audit UI integrations (no compile impact)
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>IncidentReportViewModel</b> – MVVM ViewModel for end-to-end handling of Incident Reports:
    /// listing, workflow (initiate → assign → investigate → approve → close / escalate), filtering
    /// and export. Designed for GMP/Annex 11/21 CFR Part 11 friendly UI flows (audit trail is logged
    /// by <see cref="DatabaseService"/>).
    ///
    /// Dependencies:
    /// • <see cref="DatabaseService"/> – data access methods (region “06A · Incident Reports”).
    /// • <see cref="AuthService"/> – current user/session/device info for audit metadata.
    /// </summary>
    public sealed class IncidentReportViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<IncidentReport> _incidentReports = new();
        private ObservableCollection<IncidentReport> _filteredIncidentReports = new();
        private IncidentReport? _selectedIncidentReport;

        private string? _searchTerm;
        private string? _statusFilter;
        private string? _typeFilter;

        private bool _isBusy;
        private string _statusMessage = string.Empty;

        // Cached audit context from AuthService (used when writing audit logs via DB service)
        // (coalesce to empty strings to satisfy non-nullable call sites)
        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="IncidentReportViewModel"/>.
        /// </summary>
        /// <param name="dbService">Database abstraction for Incident Report operations.</param>
        /// <param name="authService">Authentication/identity service for current user/session/device.</param>
        /// <exception cref="ArgumentNullException">Thrown when required services are not provided.</exception>
        public IncidentReportViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId  = _authService.CurrentSessionId  ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? string.Empty;
            _currentIpAddress  = _authService.CurrentIpAddress  ?? string.Empty;

            LoadIncidentReportsCommand       = new AsyncRelayCommand(LoadIncidentReportsAsync);
            InitiateIncidentReportCommand    = new AsyncRelayCommand(InitiateIncidentReportAsync, () => !IsBusy);
            AssignIncidentReportCommand      = new AsyncRelayCommand(AssignIncidentReportAsync, () => !IsBusy && SelectedIncidentReport != null);
            InvestigateIncidentReportCommand = new AsyncRelayCommand(InvestigateIncidentReportAsync, () => !IsBusy && SelectedIncidentReport != null && SelectedIncidentReport.Status == "reported");
            ApproveIncidentReportCommand     = new AsyncRelayCommand(ApproveIncidentReportAsync, () => !IsBusy && SelectedIncidentReport != null && SelectedIncidentReport.Status == "investigated");
            EscalateIncidentReportCommand    = new AsyncRelayCommand(EscalateIncidentReportAsync, () => !IsBusy && SelectedIncidentReport != null);
            CloseIncidentReportCommand       = new AsyncRelayCommand(CloseIncidentReportAsync, () => !IsBusy && SelectedIncidentReport != null && SelectedIncidentReport.Status == "approved");
            ExportIncidentReportsCommand     = new AsyncRelayCommand(ExportIncidentReportsAsync, () => !IsBusy);

            // initial load
            _ = LoadIncidentReportsAsync();
        }

        #endregion

        #region Properties (Collections & Selection)

        /// <summary>
        /// Full list of incident reports as returned by the data layer
        /// (typically ordered by ReportedAt descending).
        /// </summary>
        public ObservableCollection<IncidentReport> IncidentReports
        {
            get => _incidentReports;
            set { _incidentReports = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// View-projected list after applying search and filter criteria.
        /// </summary>
        public ObservableCollection<IncidentReport> FilteredIncidentReports
        {
            get => _filteredIncidentReports;
            set { _filteredIncidentReports = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Currently selected incident report (bound to UI).
        /// </summary>
        public IncidentReport? SelectedIncidentReport
        {
            get => _selectedIncidentReport;
            set
            {
                _selectedIncidentReport = value;
                OnPropertyChanged();
                RefreshCommandStates();
            }
        }

        #endregion

        #region Properties (Filters & State)

        /// <summary>
        /// Free-text search term (Title / IncidentType / Status / Area / RootCause).
        /// </summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterIncidentReports(); }
        }

        /// <summary>
        /// Selected status filter (e.g. reported / investigated / approved / closed).
        /// </summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterIncidentReports(); }
        }

        /// <summary>
        /// Selected incident type filter (e.g. accident / near-miss / IT / product).
        /// </summary>
        public string? TypeFilter
        {
            get => _typeFilter;
            set { _typeFilter = value; OnPropertyChanged(); FilterIncidentReports(); }
        }

        /// <summary>
        /// Busy indicator to prevent concurrent operations and to gate command execution.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); RefreshCommandStates(); }
        }

        /// <summary>
        /// Status message for end-user feedback (bind to a status label/snackbar).
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Available workflow statuses for UI filters.
        /// </summary>
        public string[] AvailableStatuses => new[]
        {
            "reported", "assigned", "investigated", "pending_approval", "approved", "closed", "escalated", "rejected"
        };

        /// <summary>
        /// Available incident types for UI filters.
        /// </summary>
        public string[] AvailableTypes => new[]
        {
            "accident", "near-miss", "environmental", "facility", "product", "IT", "other"
        };

        #endregion

        #region Commands

        /// <summary>Loads all incident reports from the database and applies current filters.</summary>
        public ICommand LoadIncidentReportsCommand { get; }

        /// <summary>Creates a new Incident Report in status <c>reported</c>.</summary>
        public ICommand InitiateIncidentReportCommand { get; }

        /// <summary>Assigns the selected Incident Report to the current user.</summary>
        public ICommand AssignIncidentReportCommand { get; }

        /// <summary>Moves the selected Incident Report to <c>investigated</c>, updating root cause and impact if provided.</summary>
        public ICommand InvestigateIncidentReportCommand { get; }

        /// <summary>Approves the selected Incident Report (status → <c>approved</c>).</summary>
        public ICommand ApproveIncidentReportCommand { get; }

        /// <summary>Escalates the selected Incident Report (status → <c>escalated</c>).</summary>
        public ICommand EscalateIncidentReportCommand { get; }

        /// <summary>Closes the selected Incident Report (status → <c>closed</c>).</summary>
        public ICommand CloseIncidentReportCommand { get; }

        /// <summary>Exports the currently filtered Incident Reports (audit logged by DB service).</summary>
        public ICommand ExportIncidentReportsCommand { get; }

        #endregion

        #region Methods – Data & Workflow

        /// <summary>
        /// Loads all incident reports via <see cref="DatabaseService.GetAllIncidentReportsFullAsync(System.Threading.CancellationToken)"/>
        /// and applies the current filter/search projection.
        /// </summary>
        public async Task LoadIncidentReportsAsync()
        {
            IsBusy = true;
            try
            {
                var records = await _dbService.GetAllIncidentReportsFullAsync();
                IncidentReports = new ObservableCollection<IncidentReport>(records ?? Enumerable.Empty<IncidentReport>());
                FilterIncidentReports();
                StatusMessage = $"Loaded {IncidentReports.Count} incident reports.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading incident reports: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Creates a new <see cref="IncidentReport"/> in status <c>reported</c>.
        /// Uses <see cref="AuthService"/> to stamp reporter/device/session/ip.
        /// </summary>
        public async Task InitiateIncidentReportAsync()
        {
            IsBusy = true;
            try
            {
                var reporter =
                    _authService.CurrentUser?.FullName ??
                    _authService.CurrentUser?.Username ??
                    "unknown";

                var newIncident = new IncidentReport
                {
                    Title           = "New Incident",
                    IncidentType    = TypeFilter ?? "accident",
                    Status          = "reported",
                    ReportedBy      = reporter,
                    ReportedAt      = DateTime.UtcNow,
                    AssignedTo      = null,
                    Area            = string.Empty,
                    Description     = string.Empty,
                    RootCause       = string.Empty,
                    ImpactScore     = 0,
                    LinkedCAPA      = false,
                    DeviceInfo      = _currentDeviceInfo,
                    SessionId       = _currentSessionId,
                    IpAddress       = _currentIpAddress
                };

                await _dbService.InitiateIncidentReportAsync(newIncident);
                StatusMessage = "Incident report initiated.";
                await LoadIncidentReportsAsync();
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

        /// <summary>
        /// Assigns the <see cref="SelectedIncidentReport"/> to the current user and moves it to <c>assigned</c>.
        /// </summary>
        public async Task AssignIncidentReportAsync()
        {
            if (SelectedIncidentReport is null)
            {
                StatusMessage = "No incident report selected.";
                return;
            }
            // FIX (CS8602): Guard against null CurrentUser before dereferencing Id.
            if (_authService.CurrentUser is null)
            {
                StatusMessage = "No authenticated user for assignment.";
                return;
            }

            IsBusy = true;
            try
            {
                await _dbService.AssignIncidentReportAsync(
                    SelectedIncidentReport.Id,
                    _authService.CurrentUser.Id,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId,
                    "Assigned by workflow");

                StatusMessage = $"Incident report '{SelectedIncidentReport.Title}' assigned.";
                await LoadIncidentReportsAsync();
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

        /// <summary>
        /// Investigates the <see cref="SelectedIncidentReport"/> and moves it to <c>investigated</c>.
        /// Updates <c>RootCause</c>, <c>Description</c> and <c>ImpactScore</c> if present on the selected item.
        /// </summary>
        public async Task InvestigateIncidentReportAsync()
        {
            if (SelectedIncidentReport is null)
            {
                StatusMessage = "No incident report selected.";
                return;
            }
            // FIX (CS8602): Guard against null CurrentUser.
            if (_authService.CurrentUser is null)
            {
                StatusMessage = "No authenticated user for investigation.";
                return;
            }

            IsBusy = true;
            try
            {
                await _dbService.InvestigateIncidentReportAsync(
                    SelectedIncidentReport,
                    _authService.CurrentUser.Id,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId);

                StatusMessage = $"Incident report '{SelectedIncidentReport.Title}' investigated.";
                await LoadIncidentReportsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Investigation failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Approves the <see cref="SelectedIncidentReport"/> (status → <c>approved</c>).
        /// </summary>
        public async Task ApproveIncidentReportAsync()
        {
            if (SelectedIncidentReport is null)
            {
                StatusMessage = "No incident report selected.";
                return;
            }
            // FIX (CS8602): Guard against null CurrentUser.
            if (_authService.CurrentUser is null)
            {
                StatusMessage = "No authenticated user for approval.";
                return;
            }

            IsBusy = true;
            try
            {
                await _dbService.ApproveIncidentReportAsync(
                    SelectedIncidentReport.Id,
                    _authService.CurrentUser.Id,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId);

                StatusMessage = $"Incident report '{SelectedIncidentReport.Title}' approved.";
                await LoadIncidentReportsAsync();
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

        /// <summary>
        /// Escalates the <see cref="SelectedIncidentReport"/> (status → <c>escalated</c>).
        /// </summary>
        public async Task EscalateIncidentReportAsync()
        {
            if (SelectedIncidentReport is null)
            {
                StatusMessage = "No incident report selected.";
                return;
            }
            // FIX (CS8602): Guard against null CurrentUser.
            if (_authService.CurrentUser is null)
            {
                StatusMessage = "No authenticated user for escalation.";
                return;
            }

            IsBusy = true;
            try
            {
                await _dbService.EscalateIncidentReportAsync(
                    SelectedIncidentReport.Id,
                    _authService.CurrentUser.Id,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId,
                    "Escalation triggered");

                StatusMessage = $"Incident report '{SelectedIncidentReport.Title}' escalated.";
                await LoadIncidentReportsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Escalation failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Closes the <see cref="SelectedIncidentReport"/> (status → <c>closed</c>).
        /// </summary>
        public async Task CloseIncidentReportAsync()
        {
            if (SelectedIncidentReport is null)
            {
                StatusMessage = "No incident report selected.";
                return;
            }
            // FIX (CS8602): Guard against null CurrentUser.
            if (_authService.CurrentUser is null)
            {
                StatusMessage = "No authenticated user for closure.";
                return;
            }

            IsBusy = true;
            try
            {
                await _dbService.CloseIncidentReportAsync(
                    SelectedIncidentReport.Id,
                    _authService.CurrentUser.Id,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId,
                    "Closed after investigation and approval");

                StatusMessage = $"Incident report '{SelectedIncidentReport.Title}' closed.";
                await LoadIncidentReportsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Closure failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Exports the current <see cref="FilteredIncidentReports"/> set using a chosen format.
        /// The DB service writes an export_print_log entry and a system event (IR_EXPORT).
        /// </summary>
        public async Task ExportIncidentReportsAsync()
        {
            IsBusy = true;
            try
            {
                var fmt = await YasGMP.Helpers.ExportFormatPrompt.PromptAsync();
                await _dbService.ExportIncidentReportsAsync(
                    FilteredIncidentReports.ToList(),
                    fmt,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId);

                StatusMessage = "Incident reports exported successfully.";
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

        #region Methods – Filtering & Command State

        /// <summary>
        /// Applies the in-memory search and filter projection to <see cref="IncidentReports"/>.
        /// </summary>
        public void FilterIncidentReports()
        {
            var filtered =
                IncidentReports.Where(i =>
                    (string.IsNullOrWhiteSpace(SearchTerm) ||
                     (i.Title?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                     (i.IncidentType?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                     (i.Status?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                     (i.Area?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                     (i.RootCause?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                    (string.IsNullOrWhiteSpace(StatusFilter) || i.Status == StatusFilter) &&
                    (string.IsNullOrWhiteSpace(TypeFilter) || i.IncidentType == TypeFilter));

            FilteredIncidentReports = new ObservableCollection<IncidentReport>(filtered);
        }

        /// <summary>
        /// Notifies all AsyncRelayCommands that their CanExecute state might have changed.
        /// </summary>
        private void RefreshCommandStates()
        {
            (InitiateIncidentReportCommand    as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (AssignIncidentReportCommand      as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (InvestigateIncidentReportCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (ApproveIncidentReportCommand     as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (EscalateIncidentReportCommand    as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (CloseIncidentReportCommand       as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (ExportIncidentReportsCommand     as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }

        #endregion

        #region INotifyPropertyChanged

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for the provided property name.
        /// </summary>
        /// <param name="propName">Name of the changed property (auto-filled by compiler when omitted).</param>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}
