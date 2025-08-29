// C:\Projects\YasGMP\ViewModels\QualificationViewModel.cs
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
using YasGMP.Models.DTO; // audits
using YasGMP.Services;
using YasGMP.Helpers;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>QualificationViewModel</b> — ViewModel for equipment/component qualification management (IQ/OQ/PQ…).
    /// <para>
    /// Features:
    /// <list type="bullet">
    ///   <item><description>Async CRUD with digital signatures and audit logging hooks.</description></item>
    ///   <item><description>Rollback/versioning routing to the data layer.</description></item>
    ///   <item><description>Advanced filtering by status and type with free-text search.</description></item>
    ///   <item><description>Export of filtered qualifications.</description></item>
    ///   <item><description>Robust <see cref="INotifyPropertyChanged"/> with complete XML docs.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public class QualificationViewModel : INotifyPropertyChanged
    {
        #region === Fields & Constructor =======================================================

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<Qualification> _qualifications = new();
        private ObservableCollection<Qualification> _filteredQualifications = new();

        // CS8618 fixes: use nullable for values not initialized in ctor.
        private Qualification? _selectedQualification;
        private string? _searchTerm;
        private string? _statusFilter;
        private string? _typeFilter;

        private bool _isBusy;

        // CS8618 fix: initialize to non-null.
        private string _statusMessage = string.Empty;

        // Coalesce to non-null strings to align with DB layer expectations.
        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        // Keep direct references to notify CanExecute changes.
        private readonly AsyncRelayCommand _loadCmd;
        private readonly AsyncRelayCommand _addCmd;
        private readonly AsyncRelayCommand _updateCmd;
        private readonly AsyncRelayCommand _deleteCmd;
        private readonly AsyncRelayCommand _rollbackCmd;
        private readonly AsyncRelayCommand _exportCmd;

        /// <summary>
        /// Initializes a new instance of the <see cref="QualificationViewModel"/> class.
        /// </summary>
        /// <param name="dbService">Database service for persistence and export.</param>
        /// <param name="authService">Authentication/context provider.</param>
        /// <exception cref="ArgumentNullException">Thrown if a dependency is <c>null</c>.</exception>
        public QualificationViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService   = dbService   ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId = _authService.CurrentSessionId  ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? string.Empty;
            _currentIpAddress  = _authService.CurrentIpAddress  ?? string.Empty;

            _loadCmd     = new AsyncRelayCommand(LoadQualificationsAsync,    () => !IsBusy);
            _addCmd      = new AsyncRelayCommand(AddQualificationAsync,      () => !IsBusy && SelectedQualification != null);
            _updateCmd   = new AsyncRelayCommand(UpdateQualificationAsync,   () => !IsBusy && SelectedQualification != null);
            _deleteCmd   = new AsyncRelayCommand(DeleteQualificationAsync,   () => !IsBusy && SelectedQualification != null);
            _rollbackCmd = new AsyncRelayCommand(RollbackQualificationAsync, () => !IsBusy && SelectedQualification != null);
            _exportCmd   = new AsyncRelayCommand(ExportQualificationsAsync,  () => !IsBusy);

            LoadQualificationsCommand    = _loadCmd;
            AddQualificationCommand      = _addCmd;
            UpdateQualificationCommand   = _updateCmd;
            DeleteQualificationCommand   = _deleteCmd;
            RollbackQualificationCommand = _rollbackCmd;
            ExportQualificationsCommand  = _exportCmd;
            FilterChangedCommand         = new RelayCommand(FilterQualifications);

            // Initial load.
            _ = LoadQualificationsAsync();
        }

        #endregion

        #region === Properties ================================================================

        /// <summary>Gets or sets the complete list of qualifications.</summary>
        public ObservableCollection<Qualification> Qualifications
        {
            get => _qualifications;
            set { _qualifications = value; OnPropertyChanged(); }
        }

        /// <summary>Gets or sets the filtered list for UI binding.</summary>
        public ObservableCollection<Qualification> FilteredQualifications
        {
            get => _filteredQualifications;
            set { _filteredQualifications = value; OnPropertyChanged(); }
        }

        /// <summary>Gets or sets the currently selected qualification (may be <c>null</c>).</summary>
        public Qualification? SelectedQualification
        {
            get => _selectedQualification;
            set { _selectedQualification = value; OnPropertyChanged(); InvalidateCommands(); }
        }

        /// <summary>Gets or sets the search term applied to EquipmentName/QualificationType/CertificateNumber.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterQualifications(); }
        }

        /// <summary>Gets or sets the status filter (e.g., valid/expired/scheduled).</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterQualifications(); }
        }

        /// <summary>Gets or sets the qualification type filter (e.g., IQ/OQ/PQ).</summary>
        public string? TypeFilter
        {
            get => _typeFilter;
            set { _typeFilter = value; OnPropertyChanged(); FilterQualifications(); }
        }

        /// <summary>Gets or sets whether an operation is in progress.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); InvalidateCommands(); }
        }

        /// <summary>Gets or sets the last status/error message for the UI.</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Gets available statuses for filtering.</summary>
        public string[] AvailableStatuses => new[] { "valid", "expired", "scheduled", "in_progress", "rejected" };

        /// <summary>Gets available qualification types for filtering.</summary>
        public string[] AvailableTypes => new[] { "IQ", "OQ", "PQ", "DQ", "VQ", "SAT", "FAT", "Requalification" };

        /// <summary>Gets whether the current user can perform edit operations.</summary>
        public bool CanEditQualifications => _authService.CurrentUser?.Role is "admin" or "superadmin";

        #endregion

        #region === Commands ==================================================================

        /// <summary>Command: loads all qualifications.</summary>
        public ICommand LoadQualificationsCommand { get; }

        /// <summary>Command: adds the selected qualification.</summary>
        public ICommand AddQualificationCommand { get; }

        /// <summary>Command: updates the selected qualification.</summary>
        public ICommand UpdateQualificationCommand { get; }

        /// <summary>Command: deletes the selected qualification.</summary>
        public ICommand DeleteQualificationCommand { get; }

        /// <summary>Command: rolls back the selected qualification.</summary>
        public ICommand RollbackQualificationCommand { get; }

        /// <summary>Command: exports the filtered qualifications.</summary>
        public ICommand ExportQualificationsCommand { get; }

        /// <summary>Command: triggers the filtering logic.</summary>
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region === Methods ===================================================================

        /// <summary>
        /// Loads all qualifications from the database and applies current filters.
        /// </summary>
        public async Task LoadQualificationsAsync()
        {
            IsBusy = true;
            try
            {
                var items = await _dbService.GetAllQualificationsAsync(
                    includeAudit: true,
                    includeCertificates: true,
                    includeAttachments: true);

                Qualifications = new ObservableCollection<Qualification>(items ?? Enumerable.Empty<Qualification>());
                FilterQualifications();
                StatusMessage = $"Loaded {Qualifications.Count} qualifications.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading qualifications: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Adds the selected qualification and logs audit via DB layer.
        /// </summary>
        public async Task AddQualificationAsync()
        {
            if (SelectedQualification == null) { StatusMessage = "No qualification selected."; return; }
            IsBusy = true;
            try
            {
                string sig = DigitalSignatureHelper.GenerateSignatureHash(SelectedQualification, _currentSessionId, _currentDeviceInfo);

                await _dbService.AddQualificationAsync(SelectedQualification, sig, _currentIpAddress, _currentDeviceInfo, _currentSessionId);
                await _dbService.LogQualificationAuditAsync(SelectedQualification, "CREATE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, sig);

                StatusMessage = $"Qualification '{SelectedQualification.QualificationType}' added.";
                await LoadQualificationsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Add failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Updates the selected qualification and emits an audit entry.
        /// </summary>
        public async Task UpdateQualificationAsync()
        {
            if (SelectedQualification == null) { StatusMessage = "No qualification selected."; return; }
            IsBusy = true;
            try
            {
                string sig = DigitalSignatureHelper.GenerateSignatureHash(SelectedQualification, _currentSessionId, _currentDeviceInfo);

                await _dbService.UpdateQualificationAsync(SelectedQualification, sig, _currentIpAddress, _currentDeviceInfo, _currentSessionId);
                await _dbService.LogQualificationAuditAsync(SelectedQualification, "UPDATE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, sig);

                StatusMessage = $"Qualification '{SelectedQualification.QualificationType}' updated.";
                await LoadQualificationsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Deletes the selected qualification and logs the operation.
        /// </summary>
        public async Task DeleteQualificationAsync()
        {
            if (SelectedQualification == null) { StatusMessage = "No qualification selected."; return; }
            IsBusy = true;
            try
            {
                await _dbService.DeleteQualificationAsync(SelectedQualification.Id, _currentIpAddress, _currentDeviceInfo, _currentSessionId);
                await _dbService.LogQualificationAuditAsync(SelectedQualification, "DELETE", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null);

                StatusMessage = $"Qualification '{SelectedQualification.QualificationType}' deleted.";
                await LoadQualificationsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Rolls back the selected qualification.
        /// </summary>
        public async Task RollbackQualificationAsync()
        {
            if (SelectedQualification == null) { StatusMessage = "No qualification selected."; return; }
            IsBusy = true;
            try
            {
                await _dbService.RollbackQualificationAsync(SelectedQualification.Id, _currentIpAddress, _currentDeviceInfo, _currentSessionId);
                StatusMessage = $"Rollback completed for qualification '{SelectedQualification.QualificationType}'.";
                await LoadQualificationsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Rollback failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Exports the filtered qualifications and writes an audit entry.
        /// </summary>
        public async Task ExportQualificationsAsync()
        {
            IsBusy = true;
            try
            {
                await _dbService.ExportQualificationsAsync(FilteredQualifications.ToList(), _currentIpAddress, _currentDeviceInfo, _currentSessionId);
                await _dbService.LogQualificationAuditAsync(null, "EXPORT", _currentIpAddress, _currentDeviceInfo, _currentSessionId, null);
                StatusMessage = "Qualifications exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Applies <see cref="SearchTerm"/>, <see cref="StatusFilter"/>, and <see cref="TypeFilter"/> to the <see cref="Qualifications"/> list.
        /// </summary>
        public void FilterQualifications()
        {
            var filtered = Qualifications.Where(q =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (q.EquipmentName?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (q.QualificationType?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (q.CertificateNumber?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(StatusFilter) || q.Status == StatusFilter) &&
                (string.IsNullOrWhiteSpace(TypeFilter)   || q.QualificationType == TypeFilter));

            FilteredQualifications = new ObservableCollection<Qualification>(filtered);
        }

        /// <summary>
        /// Loads the audit history for a specific qualification using the generic audit API.
        /// </summary>
        /// <param name="qualificationId">Primary key of the qualification record.</param>
        /// <returns>Observable collection of <see cref="AuditEntryDto"/> entries.</returns>
        public async Task<ObservableCollection<AuditEntryDto>> LoadQualificationAuditAsync(int qualificationId)
        {
            var audits = await _dbService.GetAuditLogForEntityAsync("qualifications", qualificationId);
            return new ObservableCollection<AuditEntryDto>(audits ?? new List<AuditEntryDto>());
        }

        /// <summary>
        /// Re-evaluates all command <c>CanExecute</c> predicates to update UI state.
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
        public event PropertyChangedEventHandler? PropertyChanged; // CS8612/CS8618: nullable delegate matches interface contract.

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the specified property name.
        /// </summary>
        /// <param name="propName">The name of the property that changed (optional).</param>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty)); // CS8625 fix.

        #endregion
    }
}
