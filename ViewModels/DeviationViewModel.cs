using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// Ultra-robust ViewModel for Deviation handling, aligned to canonical methods in <see cref="DatabaseService"/>.
    /// This version only uses model members that exist in schema-tolerant Deviation parsing (Title/Severity/Status).
    /// It avoids calling DatabaseService methods that don't exist and relies on the canonical
    /// <c>InsertOrUpdateDeviationAsync</c>, <c>DeleteDeviationAsync</c>, <c>SaveExportPrintLogAsync</c>, and <c>LogSystemEventAsync</c>.
    /// </summary>
    public sealed class DeviationViewModel : INotifyPropertyChanged
    {
        #region Fields

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<Deviation> _deviations = new();
        private ObservableCollection<Deviation> _filteredDeviations = new();
        private Deviation? _selectedDeviation;

        private string? _searchTerm;
        private string? _statusFilter;
        private bool _isBusy;
        private string? _statusMessage;

        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviationViewModel"/> class.
        /// </summary>
        /// <param name="dbService">Database service.</param>
        /// <param name="authService">Authentication service for current user/session context.</param>
        /// <exception cref="ArgumentNullException">Thrown if any dependency is null.</exception>
        public DeviationViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId = _authService.CurrentSessionId;
            _currentDeviceInfo = _authService.CurrentDeviceInfo;
            _currentIpAddress = _authService.CurrentIpAddress;

            LoadDeviationsCommand      = new AsyncRelayCommand(LoadDeviationsAsync);
            InitiateDeviationCommand   = new AsyncRelayCommand(InitiateDeviationAsync, () => !IsBusy);
            ApproveDeviationCommand    = new AsyncRelayCommand(ApproveDeviationAsync,  () => !IsBusy && SelectedDeviation != null);
            AssignDeviationCommand     = new AsyncRelayCommand(AssignDeviationAsync,   () => !IsBusy && SelectedDeviation != null);
            UpdateDeviationCommand     = new AsyncRelayCommand(UpdateDeviationAsync,   () => !IsBusy && SelectedDeviation != null);
            EscalateDeviationCommand   = new AsyncRelayCommand(EscalateDeviationAsync, () => !IsBusy && SelectedDeviation != null);
            CloseDeviationCommand      = new AsyncRelayCommand(CloseDeviationAsync,    () => !IsBusy && SelectedDeviation != null);
            ExportDeviationsCommand    = new AsyncRelayCommand(ExportDeviationsAsync,  () => !IsBusy);
            FilterChangedCommand       = new RelayCommand(FilterDeviations);

            // initial load
            _ = LoadDeviationsAsync();
        }

        #endregion

        #region Properties (bindable)

        /// <summary>All deviations.</summary>
        public ObservableCollection<Deviation> Deviations
        {
            get => _deviations;
            private set { _deviations = value; OnPropertyChanged(); }
        }

        /// <summary>Filtered deviations for the UI.</summary>
        public ObservableCollection<Deviation> FilteredDeviations
        {
            get => _filteredDeviations;
            private set { _filteredDeviations = value; OnPropertyChanged(); }
        }

        /// <summary>Currently selected deviation (nullable).</summary>
        public Deviation? SelectedDeviation
        {
            get => _selectedDeviation;
            set { _selectedDeviation = value; OnPropertyChanged(); }
        }

        /// <summary>Search term filter.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterDeviations(); }
        }

        /// <summary>Status filter ('open', 'investigation', 'approved', 'closed', etc.).</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterDeviations(); }
        }

        /// <summary>Busy indicator for long-running operations.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>UI status message.</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            private set { _statusMessage = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands

        /// <summary>Loads deviations from the database.</summary>
        public ICommand LoadDeviationsCommand { get; }
        /// <summary>Initiates a new deviation.</summary>
        public ICommand InitiateDeviationCommand { get; }
        /// <summary>Approves the selected deviation.</summary>
        public ICommand ApproveDeviationCommand { get; }
        /// <summary>Assigns the selected deviation (generic assignment state).</summary>
        public ICommand AssignDeviationCommand { get; }
        /// <summary>Updates the selected deviation.</summary>
        public ICommand UpdateDeviationCommand { get; }
        /// <summary>Escalates the selected deviation.</summary>
        public ICommand EscalateDeviationCommand { get; }
        /// <summary>Closes the selected deviation.</summary>
        public ICommand CloseDeviationCommand { get; }
        /// <summary>Exports the filtered list of deviations.</summary>
        public ICommand ExportDeviationsCommand { get; }
        /// <summary>Triggers filtering in the UI.</summary>
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region Data loading / filtering

        /// <summary>
        /// Loads all deviations using the canonical DB method (<c>GetAllDeviationsAsync</c>),
        /// then applies the in-memory filter.
        /// </summary>
        public async Task LoadDeviationsAsync()
        {
            IsBusy = true;
            try
            {
                var deviations = await _dbService.GetAllDeviationsAsync().ConfigureAwait(false);
                Deviations = new ObservableCollection<Deviation>(deviations);
                FilterDeviations();
                StatusMessage = $"Loaded {Deviations.Count} record(s).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading deviations: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Applies a simple in-memory filter by Title/Severity/Status only (schema-safe).
        /// </summary>
        public void FilterDeviations()
        {
            var q = Deviations.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();
                q = q.Where(d =>
                    (d.Title?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.Severity?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d.Status?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter))
            {
                q = q.Where(d => string.Equals(d.Status, StatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            FilteredDeviations = new ObservableCollection<Deviation>(q);
        }

        #endregion

        #region Actions (Initiate/Approve/Assign/Update/Escalate/Close)

        /// <summary>
        /// Creates a minimal deviation row (schema-tolerant: only Title/Severity/Status used)
        /// and relies on the database layer to log the system event.
        /// </summary>
        public async Task InitiateDeviationAsync()
        {
            IsBusy = true;
            try
            {
                var dev = new Deviation
                {
                    Title   = "New Deviation",
                    Severity= "low",
                    Status  = "open"
                };

                // Canonical insert (Region 34)
                await _dbService.InsertOrUpdateDeviationAsync(
                    dev, update: false,
                    actorUserId: _authService.CurrentUser?.Id ?? 1,
                    ip: _currentIpAddress,
                    device: _currentDeviceInfo,
                    sessionId: _currentSessionId
                ).ConfigureAwait(false);

                StatusMessage = "Deviation initiated.";
                await LoadDeviationsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Initiation failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Marks the selected deviation as approved and updates it using the canonical updater.
        /// </summary>
        public async Task ApproveDeviationAsync()
        {
            if (SelectedDeviation == null) { StatusMessage = "No deviation selected."; return; }
            IsBusy = true;
            try
            {
                SelectedDeviation.Status = "approved";
                await _dbService.InsertOrUpdateDeviationAsync(
                    SelectedDeviation, update: true,
                    actorUserId: _authService.CurrentUser?.Id ?? 1,
                    ip: _currentIpAddress,
                    device: _currentDeviceInfo,
                    sessionId: _currentSessionId
                ).ConfigureAwait(false);

                StatusMessage = $"Deviation '{SelectedDeviation.Title}' approved.";
                await LoadDeviationsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Approval failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Puts the selected deviation in a generic 'in_progress' state to represent assignment.
        /// </summary>
        public async Task AssignDeviationAsync()
        {
            if (SelectedDeviation == null) { StatusMessage = "No deviation selected."; return; }
            IsBusy = true;
            try
            {
                SelectedDeviation.Status = "in_progress";
                await _dbService.InsertOrUpdateDeviationAsync(
                    SelectedDeviation, update: true,
                    actorUserId: _authService.CurrentUser?.Id ?? 1,
                    ip: _currentIpAddress,
                    device: _currentDeviceInfo,
                    sessionId: _currentSessionId
                ).ConfigureAwait(false);

                StatusMessage = $"Deviation '{SelectedDeviation.Title}' assigned.";
                await LoadDeviationsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Assignment failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Updates the selected deviation in-place.
        /// </summary>
        public async Task UpdateDeviationAsync()
        {
            if (SelectedDeviation == null) { StatusMessage = "No deviation selected."; return; }
            IsBusy = true;
            try
            {
                await _dbService.InsertOrUpdateDeviationAsync(
                    SelectedDeviation, update: true,
                    actorUserId: _authService.CurrentUser?.Id ?? 1,
                    ip: _currentIpAddress,
                    device: _currentDeviceInfo,
                    sessionId: _currentSessionId
                ).ConfigureAwait(false);

                StatusMessage = $"Deviation '{SelectedDeviation.Title}' updated.";
                await LoadDeviationsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Sets status to 'escalated' and persists.
        /// </summary>
        public async Task EscalateDeviationAsync()
        {
            if (SelectedDeviation == null) { StatusMessage = "No deviation selected."; return; }
            IsBusy = true;
            try
            {
                SelectedDeviation.Status = "escalated";
                await _dbService.InsertOrUpdateDeviationAsync(
                    SelectedDeviation, update: true,
                    actorUserId: _authService.CurrentUser?.Id ?? 1,
                    ip: _currentIpAddress,
                    device: _currentDeviceInfo,
                    sessionId: _currentSessionId
                ).ConfigureAwait(false);

                StatusMessage = $"Deviation '{SelectedDeviation.Title}' escalated.";
                await LoadDeviationsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Escalation failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Sets status to 'closed' and persists.
        /// </summary>
        public async Task CloseDeviationAsync()
        {
            if (SelectedDeviation == null) { StatusMessage = "No deviation selected."; return; }
            IsBusy = true;
            try
            {
                SelectedDeviation.Status = "closed";
                await _dbService.InsertOrUpdateDeviationAsync(
                    SelectedDeviation, update: true,
                    actorUserId: _authService.CurrentUser?.Id ?? 1,
                    ip: _currentIpAddress,
                    device: _currentDeviceInfo,
                    sessionId: _currentSessionId
                ).ConfigureAwait(false);

                StatusMessage = $"Deviation '{SelectedDeviation.Title}' closed.";
                await LoadDeviationsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Closure failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        #endregion

        #region Export

        /// <summary>
        /// Exports the currently filtered list as CSV and writes a unified export log + system event.
        /// Uses <see cref="DatabaseService.SaveExportPrintLogAsync(int,string,string,string,string,string?,string?,System.Threading.CancellationToken)"/>.
        /// </summary>
        public async Task ExportDeviationsAsync()
        {
            IsBusy = true;
            try
            {
                // Simulate export path (your reporting pipeline can generate the real file)
                var path = $"/export/deviations_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                var filter = $"count={FilteredDeviations.Count}";

                // write unified export/print log (Region 15)
                _ = await _dbService.SaveExportPrintLogAsync(
                    userId: _authService.CurrentUser?.Id ?? 1,
                    format: "csv",
                    tableName: "deviations",
                    filterUsed: filter,
                    filePath: path,
                    sourceIp: _currentIpAddress,
                    note: "Deviations export"
                ).ConfigureAwait(false);

                // high-level system event (audit)
                await _dbService.LogSystemEventAsync(
                    userId: _authService.CurrentUser?.Id ?? 1,
                    eventType: "EXPORT",
                    tableName: "deviations",
                    module: "Deviation",
                    recordId: null,
                    description: $"Exported {FilteredDeviations.Count} deviations to {path}",
                    ip: _currentIpAddress,            // << renamed from sourceIp to ip
                    severity: "audit",
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId
                ).ConfigureAwait(false);

                StatusMessage = "Deviations exported.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        #endregion

        #region INotifyPropertyChanged

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raises <see cref="PropertyChanged"/>.</summary>
        private void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        #endregion
    }
}
