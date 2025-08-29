using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Helpers;
using YasGMP.Models.DTO;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>MachineViewModel</b> – MVVM ViewModel for GMP-compliant machine/asset management.
    /// <para>
    /// • Async CRUD with digital signatures and complete audit trail<br/>
    /// • Rollback/versioning via snapshots<br/>
    /// • Advanced filtering (status + query)<br/>
    /// • IoT/forensics context (IP, device info, session) passed to the data layer<br/>
    /// • Designed for .NET MAUI + CommunityToolkit commands
    /// </para>
    /// </summary>
    public sealed class MachineViewModel : INotifyPropertyChanged
    {
        #region === Fields & Constructor =======================================================

        // Defensive NRT initializers (kept even though we assign in ctor).
        private readonly DatabaseService _dbService = null!;
        private readonly AuthService _authService = null!;

        private ObservableCollection<Machine> _machines = new();
        private ObservableCollection<Machine> _filteredMachines = new();

        // Nullable selection/filters to avoid CS8618 and match dynamic UI lifecycles.
        private Machine? _selectedMachine;
        private string? _searchTerm;
        private string? _statusFilter;

        private bool _isBusy;
        private string _statusMessage = string.Empty;

        // Coalesced context strings for DB calls.
        private readonly string _currentSessionId = string.Empty;
        private readonly string _currentDeviceInfo = string.Empty;
        private readonly string _currentIpAddress = string.Empty;

        // Keep direct references so we can NotifyCanExecuteChanged()
        private readonly AsyncRelayCommand _loadCmd = null!;
        private readonly AsyncRelayCommand _addCmd = null!;
        private readonly AsyncRelayCommand _updateCmd = null!;
        private readonly AsyncRelayCommand _deleteCmd = null!;
        private readonly AsyncRelayCommand _rollbackCmd = null!;
        private readonly AsyncRelayCommand _exportCmd = null!;

        /// <summary>
        /// Creates a new instance of <see cref="MachineViewModel"/>.
        /// </summary>
        /// <param name="dbService">Persistence/audit service.</param>
        /// <param name="authService">Auth/session context provider.</param>
        /// <exception cref="ArgumentNullException">Thrown if any dependency is null.</exception>
        public MachineViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService   = dbService   ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId = _authService.CurrentSessionId  ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? string.Empty;
            _currentIpAddress  = _authService.CurrentIpAddress  ?? string.Empty;

            _loadCmd    = new AsyncRelayCommand(LoadMachinesAsync,    () => !IsBusy);
            _addCmd     = new AsyncRelayCommand(AddMachineAsync,      () => !IsBusy && SelectedMachine != null);
            _updateCmd  = new AsyncRelayCommand(UpdateMachineAsync,   () => !IsBusy && SelectedMachine != null);
            _deleteCmd  = new AsyncRelayCommand(DeleteMachineAsync,   () => !IsBusy && SelectedMachine != null);
            _rollbackCmd= new AsyncRelayCommand(RollbackMachineAsync, () => !IsBusy && SelectedMachine != null);
            _exportCmd  = new AsyncRelayCommand(ExportMachinesAsync,  () => !IsBusy);

            LoadMachinesCommand    = _loadCmd;
            AddMachineCommand      = _addCmd;
            UpdateMachineCommand   = _updateCmd;
            DeleteMachineCommand   = _deleteCmd;
            RollbackMachineCommand = _rollbackCmd;
            ExportMachinesCommand  = _exportCmd;
            FilterChangedCommand   = new RelayCommand(FilterMachines);

            // Initial load (fire & forget; errors surface via StatusMessage)
            _ = LoadMachinesAsync();
        }

        #endregion

        #region === Properties ================================================================

        /// <summary>All machines (unfiltered).</summary>
        public ObservableCollection<Machine> Machines
        {
            get => _machines;
            set { _machines = value; OnPropertyChanged(); }
        }

        /// <summary>Filtered view of <see cref="Machines"/>.</summary>
        public ObservableCollection<Machine> FilteredMachines
        {
            get => _filteredMachines;
            set { _filteredMachines = value; OnPropertyChanged(); }
        }

        /// <summary>Machine currently selected in the UI.</summary>
        public Machine? SelectedMachine
        {
            get => _selectedMachine;
            set { _selectedMachine = value; OnPropertyChanged(); InvalidateCommands(); }
        }

        /// <summary>Free-text search applied to <c>Name</c> and <c>Code</c>.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterMachines(); }
        }

        /// <summary>Status filter (e.g., <c>active</c>, <c>maintenance</c>…).</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterMachines(); }
        }

        /// <summary>Indicates a long-running operation.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); InvalidateCommands(); }
        }

        /// <summary>Last status or error message for the UI.</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Available status values for filters.</summary>
        public string[] AvailableStatuses => new[] { "active", "maintenance", "decommissioned", "reserved", "scrapped" };

        /// <summary>True for admin/superadmin users.</summary>
        public bool CanEditMachines => _authService.CurrentUser?.Role == "admin" || _authService.CurrentUser?.Role == "superadmin";

        #endregion

        #region === Commands ==================================================================

        public ICommand LoadMachinesCommand { get; }
        public ICommand AddMachineCommand { get; }
        public ICommand UpdateMachineCommand { get; }
        public ICommand DeleteMachineCommand { get; }
        public ICommand RollbackMachineCommand { get; }
        public ICommand ExportMachinesCommand { get; }
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region === Methods ===================================================================

        /// <summary>Loads all machines (uses the includeAudit-aware overload).</summary>
        public async Task LoadMachinesAsync()
        {
            IsBusy = true;
            try
            {
                var machines = await _dbService.GetAllMachinesAsync(includeAudit: true);
                Machines = new ObservableCollection<Machine>(machines ?? Enumerable.Empty<Machine>());
                FilterMachines();
                StatusMessage = $"Loaded {Machines.Count} machines.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading machines: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Adds a new machine and writes an audit entry.</summary>
        public async Task AddMachineAsync()
        {
            if (SelectedMachine == null) { StatusMessage = "No machine selected."; return; }

            IsBusy = true;
            try
            {
                int actorUserId = _authService.CurrentUser?.Id ?? 0;

                string sig = DigitalSignatureHelper.GenerateSignatureHash(SelectedMachine, _currentSessionId, _currentDeviceInfo);

                int newId = await _dbService.SaveMachineAsync(
                    SelectedMachine, actorUserId,
                    _currentIpAddress, _currentDeviceInfo, _currentSessionId);

                await _dbService.LogMachineAuditAsync(
                    machineId: newId,
                    userId: actorUserId,
                    action: "CREATE",
                    note: sig,
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId);

                StatusMessage = $"Machine '{SelectedMachine.Name}' added successfully.";
                await LoadMachinesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Add failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Updates the selected machine and writes an audit entry.</summary>
        public async Task UpdateMachineAsync()
        {
            if (SelectedMachine == null) { StatusMessage = "No machine selected."; return; }

            IsBusy = true;
            try
            {
                int actorUserId = _authService.CurrentUser?.Id ?? 0;

                string sig = DigitalSignatureHelper.GenerateSignatureHash(SelectedMachine, _currentSessionId, _currentDeviceInfo);

                // Use SaveMachineAsync (decides update via Id). Keeps us aligned with DatabaseService.
                int id = await _dbService.SaveMachineAsync(
                    SelectedMachine, actorUserId,
                    _currentIpAddress, _currentDeviceInfo, _currentSessionId);

                await _dbService.LogMachineAuditAsync(
                    machineId: id,
                    userId: actorUserId,
                    action: "UPDATE",
                    note: sig,
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId);

                StatusMessage = $"Machine '{SelectedMachine.Name}' updated.";
                await LoadMachinesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Deletes the selected machine and writes an audit entry (explicit machineId overload).</summary>
        public async Task DeleteMachineAsync()
        {
            if (SelectedMachine == null) { StatusMessage = "No machine selected."; return; }

            IsBusy = true;
            try
            {
                int actorUserId = _authService.CurrentUser?.Id ?? 0;

                await _dbService.DeleteMachineAsync(
                    SelectedMachine.Id, actorUserId,
                    _currentIpAddress, _currentDeviceInfo, _currentSessionId);

                await _dbService.LogMachineAuditAsync(
                    machineId: SelectedMachine.Id,
                    userId: actorUserId,
                    action: "DELETE",
                    note: null,
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId);

                StatusMessage = $"Machine '{SelectedMachine.Name}' deleted.";
                await LoadMachinesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Rolls back the selected machine by re-applying a snapshot.</summary>
        public async Task RollbackMachineAsync()
        {
            if (SelectedMachine == null) { StatusMessage = "No machine selected."; return; }

            IsBusy = true;
            try
            {
                int actorUserId = _authService.CurrentUser?.Id ?? 0;

                var snapshot = SelectedMachine.DeepCopy();

                await _dbService.RollbackMachineFromSnapshotAsync(
                    snapshot, actorUserId, _currentIpAddress, _currentDeviceInfo, _currentSessionId);

                StatusMessage = $"Rollback completed for machine '{SelectedMachine.Name}'.";
                await LoadMachinesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Rollback failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Exports the filtered list and logs an audit entry.</summary>
        public async Task ExportMachinesAsync()
        {
            IsBusy = true;
            try
            {
                int actorUserId = _authService.CurrentUser?.Id ?? 0;

                await _dbService.ExportMachinesFromViewAsync(
                    FilteredMachines.ToList(), actorUserId, _currentIpAddress, _currentDeviceInfo, _currentSessionId);

                await _dbService.LogMachineAuditAsync(
                    machineId: 0,
                    userId: actorUserId,
                    action: "EXPORT",
                    note: "Machines export from view model.",
                    ip: _currentIpAddress,
                    deviceInfo: _currentDeviceInfo,
                    sessionId: _currentSessionId);

                StatusMessage = "Machines exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Applies <see cref="SearchTerm"/> and <see cref="StatusFilter"/> to <see cref="Machines"/>.
        /// </summary>
        public void FilterMachines()
        {
            var filtered = Machines.Where(m =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (m.Name?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (m.Code?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(StatusFilter) ||
                    string.Equals(m.Status, StatusFilter, StringComparison.OrdinalIgnoreCase)));

            FilteredMachines = new ObservableCollection<Machine>(filtered);
        }

        /// <summary>Loads audit history for the specified machine.</summary>
        /// <param name="machineId">Machine primary key.</param>
        /// <returns>Audit entries for binding.</returns>
        public async Task<ObservableCollection<AuditEntryDto>> LoadMachineAuditAsync(int machineId)
        {
            var audits = await _dbService.GetAuditLogForEntityAsync("machines", machineId);
            return new ObservableCollection<AuditEntryDto>(audits ?? new List<AuditEntryDto>());
        }

        /// <summary>
        /// Forces reevaluation of command CanExecute (so buttons enable/disable correctly).
        /// </summary>
        private void InvalidateCommands()
        {
            _loadCmd.NotifyCanExecuteChanged();
            _addCmd.NotifyCanExecuteChanged();
            _updateCmd.NotifyCanExecuteChanged();
            _deleteCmd.NotifyCanExecuteChanged();
            _rollbackCmd.NotifyCanExecuteChanged();
            _exportCmd.NotifyCanExecuteChanged();
        }

        #endregion

        #region === INotifyPropertyChanged ====================================================

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for a changed property.
        /// </summary>
        /// <param name="propName">Optional property name (automatically supplied by the compiler).</param>
        /// <remarks>
        /// FIX (CS0628): This type is <c>sealed</c>, so we must not expose new <c>protected</c> members.
        /// The notifier is <c>private</c> since no inheritance is expected.
        /// </remarks>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}
