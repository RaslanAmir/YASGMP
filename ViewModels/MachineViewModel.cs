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
    /// • IoT/forensics context (IP, device info, session) propagated to the data layer
    /// </para>
    /// </summary>
    public sealed class MachineViewModel : INotifyPropertyChanged
    {
        #region === Fields & Constructor =======================================================

        private readonly DatabaseService _dbService = null!;
        private readonly AuthService _authService = null!;

        private ObservableCollection<Machine> _machines = new();
        private ObservableCollection<Machine> _filteredMachines = new();

        private Machine? _selectedMachine;
        private string? _searchTerm;
        private string? _statusFilter;

        private bool _isBusy;
        private string _statusMessage = string.Empty;

        private readonly string _currentSessionId = string.Empty;
        private readonly string _currentDeviceInfo = string.Empty;
        private readonly string _currentIpAddress = string.Empty;

        private readonly AsyncRelayCommand _loadCmd = null!;
        private readonly AsyncRelayCommand _addCmd = null!;
        private readonly AsyncRelayCommand _updateCmd = null!;
        private readonly AsyncRelayCommand _deleteCmd = null!;
        private readonly AsyncRelayCommand _rollbackCmd = null!;
        private readonly AsyncRelayCommand _exportCmd = null!;

        /// <summary>DI konstruktor.</summary>
        public MachineViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService   = dbService   ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId = _authService.CurrentSessionId  ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? string.Empty;
            _currentIpAddress  = _authService.CurrentIpAddress  ?? string.Empty;

            _loadCmd     = new AsyncRelayCommand(LoadMachinesAsync,    () => !IsBusy);
            _addCmd      = new AsyncRelayCommand(AddMachineAsync,      () => !IsBusy && SelectedMachine != null);
            _updateCmd   = new AsyncRelayCommand(UpdateMachineAsync,   () => !IsBusy && SelectedMachine != null);
            _deleteCmd   = new AsyncRelayCommand(DeleteMachineAsync,   () => !IsBusy && SelectedMachine != null);
            _rollbackCmd = new AsyncRelayCommand(RollbackMachineAsync, () => !IsBusy && SelectedMachine != null);
            _exportCmd   = new AsyncRelayCommand(ExportMachinesAsync,  () => !IsBusy);

            LoadMachinesCommand    = _loadCmd;
            AddMachineCommand      = _addCmd;
            UpdateMachineCommand   = _updateCmd;
            DeleteMachineCommand   = _deleteCmd;
            RollbackMachineCommand = _rollbackCmd;
            ExportMachinesCommand  = _exportCmd;
            FilterChangedCommand   = new RelayCommand(FilterMachines);

            _ = LoadMachinesAsync();
        }

        #endregion

        #region === Properties ================================================================

        /// <summary>Svi strojevi (nefiltrirani).</summary>
        public ObservableCollection<Machine> Machines
        {
            get => _machines;
            set { _machines = value; OnPropertyChanged(); }
        }

        /// <summary>Filtrirani pogled liste.</summary>
        public ObservableCollection<Machine> FilteredMachines
        {
            get => _filteredMachines;
            set { _filteredMachines = value; OnPropertyChanged(); }
        }

        /// <summary>Odabrani stroj u UI-ju.</summary>
        public Machine? SelectedMachine
        {
            get => _selectedMachine;
            set { _selectedMachine = value; OnPropertyChanged(); InvalidateCommands(); }
        }

        /// <summary>Tekst za traženje (Name/Code).</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterMachines(); }
        }

        /// <summary>Filter statusa (kanonske vrijednosti).</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterMachines(); }
        }

        /// <summary>Dugotrajna operacija?</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); InvalidateCommands(); }
        }

        /// <summary>Poruka za status-bar.</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Popis dostupnih kanonskih statusa.</summary>
        public string[] AvailableStatuses => new[] { "active", "maintenance", "decommissioned", "reserved", "scrapped" };

        /// <summary>True za administratore.</summary>
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

        /// <summary>Učitava sve strojeve.</summary>
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

        /// <summary>Dodaje novi stroj i bilježi audit.</summary>
        public async Task AddMachineAsync()
        {
            if (SelectedMachine == null) { StatusMessage = "No machine selected."; return; }

            IsBusy = true;
            try
            {
                int actorUserId = _authService.CurrentUser?.Id ?? 0;

                // Sigurnost: normaliziraj status prije slanja u servis
                SelectedMachine.Status = MachineService.NormalizeStatus(SelectedMachine.Status);

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

        /// <summary>Ažurira odabrani stroj i bilježi audit.</summary>
        public async Task UpdateMachineAsync()
        {
            if (SelectedMachine == null) { StatusMessage = "No machine selected."; return; }

            IsBusy = true;
            try
            {
                int actorUserId = _authService.CurrentUser?.Id ?? 0;

                SelectedMachine.Status = MachineService.NormalizeStatus(SelectedMachine.Status);

                string sig = DigitalSignatureHelper.GenerateSignatureHash(SelectedMachine, _currentSessionId, _currentDeviceInfo);

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

        /// <summary>Briše odabrani stroj (audit).</summary>
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

        /// <summary>Rollback primjenom snimke.</summary>
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

        /// <summary>Export filtrirane liste.</summary>
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

        /// <summary>Primjeni filtere na listu.</summary>
        public void FilterMachines()
        {
            var filtered = Machines.Where(m =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (m.Name?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (m.Code?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(StatusFilter) ||
                    string.Equals(MachineService.NormalizeStatus(m.Status), StatusFilter, StringComparison.OrdinalIgnoreCase)));

            FilteredMachines = new ObservableCollection<Machine>(filtered);
        }

        /// <summary>Učitava audit povijest za zadani stroj.</summary>
        public async Task<ObservableCollection<AuditEntryDto>> LoadMachineAuditAsync(int machineId)
        {
            var audits = await _dbService.GetAuditLogForEntityAsync("machines", machineId);
            return new ObservableCollection<AuditEntryDto>(audits ?? new List<AuditEntryDto>());
        }

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

        /// <summary>Podigne <see cref="PropertyChanged"/>.</summary>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}
