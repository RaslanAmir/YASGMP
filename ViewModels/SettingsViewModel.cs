using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Models.DTO; // <- audits come back as DTOs
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// Robust ViewModel for system/app/user/security settings with audit & rollback (GMP/Annex 11/21 CFR Part 11).
    /// </summary>
    public class SettingsViewModel : INotifyPropertyChanged
    {
        #region === Fields & Ctor ===================================================

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<Setting> _settings = new();
        private ObservableCollection<Setting> _filteredSettings = new();

        private Setting? _selectedSetting;
        private string? _searchTerm;
        private string? _categoryFilter;

        private bool _isBusy;
        private string? _statusMessage;

        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        /// <summary>
        /// Initializes a new instance with required services.
        /// </summary>
        public SettingsViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            // Null-safe local copies (avoid CS8601 in ctor)
            _currentSessionId = _authService.CurrentSessionId ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? "ui";
            _currentIpAddress  = _authService.CurrentIpAddress  ?? "0.0.0.0";

            LoadSettingsCommand     = new AsyncRelayCommand(LoadSettingsAsync);
            SaveSettingCommand      = new AsyncRelayCommand(SaveSettingAsync,    () => !IsBusy && SelectedSetting is not null);
            DeleteSettingCommand    = new AsyncRelayCommand(DeleteSettingAsync,  () => !IsBusy && SelectedSetting is not null);
            RollbackSettingCommand  = new AsyncRelayCommand(RollbackSettingAsync,() => !IsBusy && SelectedSetting is not null);
            ExportSettingsCommand   = new AsyncRelayCommand(ExportSettingsAsync, () => !IsBusy);
            FilterChangedCommand    = new RelayCommand(FilterSettings);

            _ = LoadSettingsAsync();
        }

        #endregion

        #region === Properties ======================================================

        /// <summary>All settings loaded from persistence.</summary>
        public ObservableCollection<Setting> Settings
        {
            get => _settings;
            set { _settings = value; OnPropertyChanged(); }
        }

        /// <summary>Filtered projection for the UI.</summary>
        public ObservableCollection<Setting> FilteredSettings
        {
            get => _filteredSettings;
            set { _filteredSettings = value; OnPropertyChanged(); }
        }

        /// <summary>Currently selected setting (nullable during initial load or when cleared).</summary>
        public Setting? SelectedSetting
        {
            get => _selectedSetting;
            set { _selectedSetting = value; OnPropertyChanged(); }
        }

        /// <summary>Search term for key/value/category.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterSettings(); }
        }

        /// <summary>Category filter.</summary>
        public string? CategoryFilter
        {
            get => _categoryFilter;
            set { _categoryFilter = value; OnPropertyChanged(); FilterSettings(); }
        }

        /// <summary>UI busy flag.</summary>
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

        /// <summary>Available setting categories.</summary>
        public string[] AvailableCategories => new[]
        {
            "UI","User","Notification","Integration","Backup","Legal",
            "Security","Compliance","Smtp","Ldap","Api","Audit","Other"
        };

        /// <summary>Role gate for mutating settings.</summary>
        public bool CanModifySettings => _authService.CurrentUser?.Role is "admin" or "superadmin";

        #endregion

        #region === Commands ========================================================
        /// <summary>
        /// Gets or sets the load settings command.
        /// </summary>

        public ICommand LoadSettingsCommand { get; }
        /// <summary>
        /// Gets or sets the save setting command.
        /// </summary>
        public ICommand SaveSettingCommand { get; }
        /// <summary>
        /// Gets or sets the delete setting command.
        /// </summary>
        public ICommand DeleteSettingCommand { get; }
        /// <summary>
        /// Gets or sets the rollback setting command.
        /// </summary>
        public ICommand RollbackSettingCommand { get; }
        /// <summary>
        /// Gets or sets the export settings command.
        /// </summary>
        public ICommand ExportSettingsCommand { get; }
        /// <summary>
        /// Gets or sets the filter changed command.
        /// </summary>
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region === Methods =========================================================

        /// <summary>Loads all settings and applies current filters.</summary>
        public async Task LoadSettingsAsync()
        {
            IsBusy = true;
            try
            {
                var settings = await _dbService.GetAllSettingsFullAsync().ConfigureAwait(false);
                Settings = new ObservableCollection<Setting>(settings);
                FilterSettings();
                StatusMessage = $"Loaded {Settings.Count} settings.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading settings: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Creates or updates the selected setting with full audit trail.</summary>
        public async Task SaveSettingAsync()
        {
            if (SelectedSetting is null) { StatusMessage = "No setting selected."; return; }
            IsBusy = true;
            try
            {
                var userId = _authService.CurrentUser?.Id ?? 1; // Avoid CS8602
                await _dbService.UpsertSettingAsync(
                    SelectedSetting,
                    update: SelectedSetting.Id > 0,
                    actorUserId: userId,
                    ip: _currentIpAddress,
                    device: _currentDeviceInfo,
                    sessionId: _currentSessionId).ConfigureAwait(false);

                await _dbService.LogSettingAuditAsync(
                    SelectedSetting, "SAVE",
                    _currentIpAddress, _currentDeviceInfo, _currentSessionId, SelectedSetting.Value).ConfigureAwait(false);

                StatusMessage = $"Setting '{SelectedSetting.Key}' saved.";
                await LoadSettingsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Save failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Deletes the selected setting and logs the operation.</summary>
        public async Task DeleteSettingAsync()
        {
            if (SelectedSetting is null) { StatusMessage = "No setting selected."; return; }
            IsBusy = true;
            try
            {
                var userId = _authService.CurrentUser?.Id ?? 1; // Avoid CS8602
                await _dbService.DeleteSettingAsync(
                    SelectedSetting.Id,
                    actorUserId: userId,
                    ip: _currentIpAddress,
                    device: _currentDeviceInfo).ConfigureAwait(false);

                await _dbService.LogSettingAuditAsync(
                    SelectedSetting, "DELETE",
                    _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);

                StatusMessage = $"Setting '{SelectedSetting.Key}' deleted.";
                await LoadSettingsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Requests rollback for the selected setting (audit-only action).</summary>
        public async Task RollbackSettingAsync()
        {
            if (SelectedSetting is null) { StatusMessage = "No setting selected."; return; }
            IsBusy = true;
            try
            {
                var userId = _authService.CurrentUser?.Id ?? 1; // Avoid CS8602
                await _dbService.RollbackSettingAsync(
                    SelectedSetting.Id,
                    actorUserId: userId,
                    ip: _currentIpAddress,
                    device: _currentDeviceInfo,
                    sessionId: _currentSessionId).ConfigureAwait(false);

                StatusMessage = $"Rollback completed for setting '{SelectedSetting.Key}'.";
                await LoadSettingsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Rollback failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Exports filtered settings (format chosen by user) and logs an export entry.</summary>
        public async Task ExportSettingsAsync()
        {
            IsBusy = true;
            try
            {
                var userId = _authService.CurrentUser?.Id ?? 1; // Avoid CS8602
                var fmt = await YasGMP.Helpers.ExportFormatPrompt.PromptAsync();
                await _dbService.ExportSettingsAsync(
                    FilteredSettings.ToList(),
                    fmt,
                    userId,
                    _currentIpAddress,
                    _currentDeviceInfo,
                    _currentSessionId).ConfigureAwait(false);

                await _dbService.LogSettingAuditAsync(
                    null, "EXPORT",
                    _currentIpAddress, _currentDeviceInfo, _currentSessionId, null).ConfigureAwait(false);

                StatusMessage = "Settings exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Applies client-side filters on key/value/category.</summary>
        public void FilterSettings()
        {
            var filtered = Settings.Where(s =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (s.Key?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Value?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Category?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(CategoryFilter) || s.Category == CategoryFilter));

            FilteredSettings = new ObservableCollection<Setting>(filtered);
        }

        /// <summary>Returns audit entries for a given setting id.</summary>
        public async Task<ObservableCollection<AuditEntryDto>> LoadSettingAuditAsync(int settingId)
        {
            var audits = await _dbService.GetAuditLogForEntityAsync("settings", settingId).ConfigureAwait(false);
            return new ObservableCollection<AuditEntryDto>(audits);
        }

        #endregion

        #region === INotifyPropertyChanged ==========================================

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Null-safe property changed notifier.</summary>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}
