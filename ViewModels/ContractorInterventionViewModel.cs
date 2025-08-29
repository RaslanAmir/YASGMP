using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using System.Collections.Generic;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Helpers;
using CommunityToolkit.Mvvm.Input;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// Ultra-robust ViewModel for contractor/servicer interventions (outsourced maintenance, calibrations, etc).
    /// GMP/Annex 11/21 CFR Part 11 ready.
    /// </summary>
    public class ContractorInterventionViewModel : INotifyPropertyChanged
    {
        #region Fields & Constructor

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<ContractorIntervention> _interventions = new();
        private ObservableCollection<ContractorIntervention> _filteredInterventions = new();

        // Nullable backing fields to avoid CS8618; UI and logic handle nulls safely.
        private ContractorIntervention? _selectedIntervention;
        private string? _searchTerm;
        private string? _statusFilter;
        private string? _contractorFilter;

        private bool _isBusy;
        private string? _statusMessage;

        // Session info (coalesced to non-null values on assignment)
        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        /// <summary>
        /// Initializes a new instance of <see cref="ContractorInterventionViewModel"/>.
        /// </summary>
        /// <param name="dbService">Database service instance.</param>
        /// <param name="authService">Authentication/session service.</param>
        public ContractorInterventionViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService    = dbService  ?? throw new ArgumentNullException(nameof(dbService));
            _authService  = authService?? throw new ArgumentNullException(nameof(authService));

            // CS8618/CS8601: force-safe, non-null session strings.
            _currentSessionId  = _authService.CurrentSessionId  ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? string.Empty;
            _currentIpAddress  = _authService.CurrentIpAddress  ?? string.Empty;

            LoadInterventionsCommand      = new AsyncRelayCommand(LoadInterventionsAsync);
            AddInterventionCommand        = new AsyncRelayCommand(AddInterventionAsync,        () => !IsBusy && SelectedIntervention != null);
            UpdateInterventionCommand     = new AsyncRelayCommand(UpdateInterventionAsync,     () => !IsBusy && SelectedIntervention != null);
            DeleteInterventionCommand     = new AsyncRelayCommand(DeleteInterventionAsync,     () => !IsBusy && SelectedIntervention != null);
            RollbackInterventionCommand   = new AsyncRelayCommand(RollbackInterventionAsync,   () => !IsBusy && SelectedIntervention != null);
            ExportInterventionsCommand    = new AsyncRelayCommand(ExportInterventionsAsync,    () => !IsBusy);
            FilterChangedCommand          = new RelayCommand(FilterInterventions);

            _ = LoadInterventionsAsync();
        }

        #endregion

        #region Properties

        /// <summary>All contractor interventions loaded from the database.</summary>
        public ObservableCollection<ContractorIntervention> Interventions
        {
            get => _interventions;
            set { _interventions = value ?? new(); OnPropertyChanged(); }
        }

        /// <summary>Interventions after applying current filters.</summary>
        public ObservableCollection<ContractorIntervention> FilteredInterventions
        {
            get => _filteredInterventions;
            set { _filteredInterventions = value ?? new(); OnPropertyChanged(); }
        }

        /// <summary>Currently selected intervention (nullable).</summary>
        public ContractorIntervention? SelectedIntervention
        {
            get => _selectedIntervention;
            set { _selectedIntervention = value; OnPropertyChanged(); }
        }

        /// <summary>Free-text search term (nullable for safe resets).</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterInterventions(); }
        }

        /// <summary>Status filter value (nullable for safe resets).</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterInterventions(); }
        }

        /// <summary>Contractor filter value (nullable for safe resets).</summary>
        public string? ContractorFilter
        {
            get => _contractorFilter;
            set { _contractorFilter = value; OnPropertyChanged(); FilterInterventions(); }
        }

        /// <summary>Busy flag for async operations.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>Status/info message for the UI (nullable).</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Allowed statuses for pickers.</summary>
        public string[] AvailableStatuses => new[] { "planned", "completed", "approved", "rejected", "overdue" };

        #endregion

        #region Commands

        public ICommand LoadInterventionsCommand { get; }
        public ICommand AddInterventionCommand { get; }
        public ICommand UpdateInterventionCommand { get; }
        public ICommand DeleteInterventionCommand { get; }
        public ICommand RollbackInterventionCommand { get; }
        public ICommand ExportInterventionsCommand { get; }
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region Methods

        /// <summary>Loads interventions from the database and applies current filters.</summary>
        public async Task LoadInterventionsAsync()
        {
            IsBusy = true;
            try
            {
                var interventions = await _dbService.GetAllContractorInterventionsAsync().ConfigureAwait(false);
                Interventions = new ObservableCollection<ContractorIntervention>(interventions ?? new List<ContractorIntervention>());
                FilterInterventions();
                StatusMessage = $"Loaded {Interventions.Count} interventions.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading interventions: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Adds the currently selected intervention.</summary>
        public async Task AddInterventionAsync()
        {
            if (SelectedIntervention == null) { StatusMessage = "No intervention selected."; return; }
            IsBusy = true;
            try
            {
                var userId        = _authService.CurrentUser?.Id ?? 1;
                var ip            = _currentIpAddress  ?? string.Empty;
                var device        = _currentDeviceInfo ?? string.Empty;
                string signature  = DigitalSignatureHelper.GenerateSignatureHash(SelectedIntervention, _currentSessionId, device);
                string comment    = $"sig={signature}; session={_currentSessionId}";

                var newId = await _dbService.AddContractorInterventionAsync(
                    SelectedIntervention, userId, ip, device, comment).ConfigureAwait(false);

                if (newId > 0) SelectedIntervention.Id = newId;

                await _dbService.LogContractorInterventionAuditAsync(
                    SelectedIntervention.Id, userId, "CREATE", comment).ConfigureAwait(false);

                StatusMessage = $"Contractor intervention '{SelectedIntervention.InterventionType}' added.";
                await LoadInterventionsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Add failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Updates the currently selected intervention.</summary>
        public async Task UpdateInterventionAsync()
        {
            if (SelectedIntervention == null) { StatusMessage = "No intervention selected."; return; }
            IsBusy = true;
            try
            {
                var userId        = _authService.CurrentUser?.Id ?? 1;
                var ip            = _currentIpAddress  ?? string.Empty;
                var device        = _currentDeviceInfo ?? string.Empty;
                string signature  = DigitalSignatureHelper.GenerateSignatureHash(SelectedIntervention, _currentSessionId, device);
                string comment    = $"sig={signature}; session={_currentSessionId}";

                await _dbService.UpdateContractorInterventionAsync(
                    SelectedIntervention, userId, ip, device, comment).ConfigureAwait(false);

                await _dbService.LogContractorInterventionAuditAsync(
                    SelectedIntervention.Id, userId, "UPDATE", comment).ConfigureAwait(false);

                StatusMessage = $"Contractor intervention '{SelectedIntervention.InterventionType}' updated.";
                await LoadInterventionsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Deletes the currently selected intervention.</summary>
        public async Task DeleteInterventionAsync()
        {
            if (SelectedIntervention == null) { StatusMessage = "No intervention selected."; return; }
            IsBusy = true;
            try
            {
                var userId     = _authService.CurrentUser?.Id ?? 1;
                var ip         = _currentIpAddress  ?? string.Empty;
                var device     = _currentDeviceInfo ?? string.Empty;
                string comment = $"session={_currentSessionId}";

                await _dbService.DeleteContractorInterventionAsync(
                    SelectedIntervention.Id, userId, ip, device, comment).ConfigureAwait(false);

                await _dbService.LogContractorInterventionAuditAsync(
                    SelectedIntervention.Id, userId, "DELETE", comment).ConfigureAwait(false);

                StatusMessage = $"Contractor intervention '{SelectedIntervention.InterventionType}' deleted.";
                await LoadInterventionsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Rolls back the selected intervention to a previous snapshot.</summary>
        public async Task RollbackInterventionAsync()
        {
            if (SelectedIntervention == null) { StatusMessage = "No intervention selected."; return; }
            IsBusy = true;
            try
            {
                var userId           = _authService.CurrentUser?.Id ?? 1;
                var ip               = _currentIpAddress  ?? string.Empty;
                var device           = _currentDeviceInfo ?? string.Empty;
                string previousSnap  = "AUTO";
                string comment       = $"rollback; session={_currentSessionId}";

                await _dbService.RollbackContractorInterventionAsync(
                    SelectedIntervention.Id, previousSnap, userId, ip, device, comment).ConfigureAwait(false);

                await _dbService.LogContractorInterventionAuditAsync(
                    SelectedIntervention.Id, userId, "ROLLBACK", comment).ConfigureAwait(false);

                StatusMessage = $"Rollback completed for intervention '{SelectedIntervention.InterventionType}'.";
                await LoadInterventionsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Rollback failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Exports filtered interventions (CSV) and logs the action.</summary>
        public async Task ExportInterventionsAsync()
        {
            IsBusy = true;
            try
            {
                await _dbService.ExportContractorInterventionsAsync("csv").ConfigureAwait(false);

                await _dbService.LogContractorInterventionAuditAsync(
                    0,
                    _authService.CurrentUser?.Id ?? 1,
                    "EXPORT",
                    $"Exported {FilteredInterventions?.Count ?? 0} items; session={_currentSessionId}"
                ).ConfigureAwait(false);

                StatusMessage = "Contractor interventions exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Applies current filters to the intervention list.</summary>
        public void FilterInterventions()
        {
            var filtered = Interventions.Where(i =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (i.AssetName       ?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.ContractorName  ?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.InterventionType?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(StatusFilter)     || string.Equals(i.Status, StatusFilter, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(ContractorFilter) || string.Equals(i.ContractorName, ContractorFilter, StringComparison.OrdinalIgnoreCase))
            );

            FilteredInterventions = new ObservableCollection<ContractorIntervention>(filtered);
        }

        /// <summary>Returns true if the current user can perform edit operations.</summary>
        public bool CanEditInterventions =>
            _authService.CurrentUser?.Role == "admin" || _authService.CurrentUser?.Role == "superadmin";

        #endregion

        #region Audit mapping helpers

        /// <summary>
        /// Maps a DTO to a non-null <see cref="AuditLogEntry"/> instance.
        /// Returns an empty object when <paramref name="dto"/> is null (avoids CS8603).
        /// </summary>
        private static AuditLogEntry MapAuditEntryDtoToAuditLogEntry(YasGMP.Models.DTO.AuditEntryDto? dto)
        {
            if (dto == null)
            {
                return new AuditLogEntry
                {
                    Id = 0,
                    EntityType = string.Empty,
                    EntityId = 0,
                    PerformedBy = string.Empty,
                    Action = string.Empty,
                    OldValue = string.Empty,
                    NewValue = string.Empty,
                    ChangedAt = DateTime.MinValue,
                    DeviceInfo = string.Empty,
                    IpAddress = string.Empty,
                    SessionId = string.Empty,
                    DigitalSignature = string.Empty,
                    Note = string.Empty
                };
            }

            return new AuditLogEntry
            {
                Id               = dto.Id ?? 0,
                EntityType       = dto.Entity ?? string.Empty,
                EntityId         = int.TryParse(dto.EntityId, out var eid) ? eid : 0,
                PerformedBy      = !string.IsNullOrEmpty(dto.Username) ? dto.Username! : (dto.UserId?.ToString() ?? string.Empty),
                Action           = dto.Action ?? string.Empty,
                OldValue         = dto.OldValue ?? string.Empty,
                NewValue         = dto.NewValue ?? string.Empty,
                ChangedAt        = dto.Timestamp,
                DeviceInfo       = dto.DeviceInfo ?? string.Empty,
                IpAddress        = dto.IpAddress ?? string.Empty,
                SessionId        = dto.SessionId ?? string.Empty,
                DigitalSignature = dto.DigitalSignature ?? string.Empty,
                Note             = dto.Note ?? string.Empty
            };
        }

        /// <summary>
        /// Loads audit entries for the given intervention and maps to domain model.
        /// </summary>
        public async Task<ObservableCollection<AuditLogEntry>> LoadInterventionAuditAsync(int interventionId)
        {
            var dtos   = await _dbService.GetAuditLogForEntityAsync("contractor_interventions", interventionId).ConfigureAwait(false)
                      ?? new List<YasGMP.Models.DTO.AuditEntryDto>();
            var audits = dtos.Select(MapAuditEntryDtoToAuditLogEntry);
            return new ObservableCollection<AuditLogEntry>(audits);
        }

        #endregion

        #region INotifyPropertyChanged

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raises <see cref="PropertyChanged"/> notifications for MVVM binding.</summary>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}
