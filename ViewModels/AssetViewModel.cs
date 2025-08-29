using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Helpers;
using YasGMP.Models.DTO;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>AssetViewModel</b> – GMP-compliant ViewModel for asset/equipment lifecycle.
    /// Async CRUD, digital signatures, audit logging, rollback hooks, filtering, and export logging.
    /// All properties and events are nullability-correct and AOT-safe.
    /// </summary>
    public class AssetViewModel : INotifyPropertyChanged
    {
        #region === Fields & Constructor ===

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<Asset> _assets = new();
        private ObservableCollection<Asset> _filteredAssets = new();
        private Asset? _selectedAsset;
        private string? _searchTerm;
        private string? _statusFilter;
        private string? _riskFilter;
        private string? _typeFilter;
        private bool _isBusy;
        private string? _statusMessage;
        private readonly string? _currentSessionId;
        private readonly string? _currentDeviceInfo;
        private readonly string? _currentIpAddress;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetViewModel"/> class with required services.
        /// </summary>
        /// <param name="dbService">Database access layer.</param>
        /// <param name="authService">Authentication/session context provider.</param>
        /// <exception cref="ArgumentNullException">If a dependency is <c>null</c>.</exception>
        public AssetViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId = _authService.CurrentSessionId;
            _currentDeviceInfo = _authService.CurrentDeviceInfo;
            _currentIpAddress = _authService.CurrentIpAddress;

            LoadAssetsCommand    = new AsyncRelayCommand(LoadAssetsAsync);
            AddAssetCommand      = new AsyncRelayCommand(AddAssetAsync,      () => !IsBusy);
            UpdateAssetCommand   = new AsyncRelayCommand(UpdateAssetAsync,   () => !IsBusy && SelectedAsset != null);
            DeleteAssetCommand   = new AsyncRelayCommand(DeleteAssetAsync,   () => !IsBusy && SelectedAsset != null);
            RollbackAssetCommand = new AsyncRelayCommand(RollbackAssetAsync, () => !IsBusy && SelectedAsset != null);
            ExportAssetsCommand  = new AsyncRelayCommand(ExportAssetsAsync,  () => !IsBusy);
            FilterChangedCommand = new RelayCommand(FilterAssets);

            _ = LoadAssetsAsync();
        }

        #endregion

        #region === Safe Context Helpers ===

        /// <summary>Non-null, normalized IP address for DB/API calls (defaults to "0.0.0.0").</summary>
        private string SafeIp => string.IsNullOrWhiteSpace(_currentIpAddress) ? "0.0.0.0" : _currentIpAddress!;

        /// <summary>Non-null, normalized device info for DB/API calls (defaults to "unknown").</summary>
        private string SafeDeviceInfo => string.IsNullOrWhiteSpace(_currentDeviceInfo) ? "unknown" : _currentDeviceInfo!;

        /// <summary>Non-null, normalized session ID for DB/API calls (defaults to empty string).</summary>
        private string SafeSessionId => _currentSessionId ?? string.Empty;

        #endregion

        #region === Properties ===

        /// <summary>All loaded assets (raw server list).</summary>
        public ObservableCollection<Asset> Assets
        {
            get => _assets;
            set { _assets = value ?? new ObservableCollection<Asset>(); OnPropertyChanged(); }
        }

        /// <summary>Filtered view over <see cref="Assets"/> to display in UI.</summary>
        public ObservableCollection<Asset> FilteredAssets
        {
            get => _filteredAssets;
            set { _filteredAssets = value ?? new ObservableCollection<Asset>(); OnPropertyChanged(); }
        }

        /// <summary>Currently selected asset (nullable).</summary>
        public Asset? SelectedAsset
        {
            get => _selectedAsset;
            set { _selectedAsset = value; OnPropertyChanged(); }
        }

        /// <summary>Free-text search term (nullable). Triggers filtering on change.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterAssets(); }
        }

        /// <summary>Status filter (nullable). Triggers filtering on change.</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterAssets(); }
        }

        /// <summary>Risk filter (nullable). Triggers filtering on change.</summary>
        public string? RiskFilter
        {
            get => _riskFilter;
            set { _riskFilter = value; OnPropertyChanged(); FilterAssets(); }
        }

        /// <summary>Asset type filter (nullable). Triggers filtering on change.</summary>
        public string? TypeFilter
        {
            get => _typeFilter;
            set { _typeFilter = value; OnPropertyChanged(); FilterAssets(); }
        }

        /// <summary>Indicates whether the ViewModel is busy (e.g., during I/O).</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>Status text for UI notifications (nullable).</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Available asset statuses.</summary>
        public string[] AvailableStatuses => new[] { "active", "inactive", "out_of_service", "critical", "maintenance", "pending_approval", "decommissioned" };

        /// <summary>Available risk ratings.</summary>
        public string[] AvailableRisks => new[] { "low", "medium", "high", "critical" };

        /// <summary>Available asset types.</summary>
        public string[] AvailableTypes => new[] { "machine", "instrument", "utility", "facility", "IT", "room", "system", "other" };

        #endregion

        #region === Commands ===

        public ICommand LoadAssetsCommand { get; }
        public ICommand AddAssetCommand { get; }
        public ICommand UpdateAssetCommand { get; }
        public ICommand DeleteAssetCommand { get; }
        public ICommand RollbackAssetCommand { get; }
        public ICommand ExportAssetsCommand { get; }
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region === Methods ===

        /// <summary>
        /// Loads all assets asynchronously, applies current filters, and updates UI state.
        /// </summary>
        public async Task LoadAssetsAsync()
        {
            IsBusy = true;
            try
            {
                var assets = await _dbService.GetAllAssetsFullAsync().ConfigureAwait(false);
                Assets = new ObservableCollection<Asset>(assets ?? Enumerable.Empty<Asset>());
                FilterAssets();
                StatusMessage = $"Loaded {Assets.Count} assets.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading assets: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Adds the currently selected asset (with digital signature and audit log).
        /// </summary>
        public async Task AddAssetAsync()
        {
            if (SelectedAsset == null)
            {
                StatusMessage = "No asset selected.";
                return;
            }
            IsBusy = true;
            try
            {
                string signatureHash = DigitalSignatureHelper.GenerateSignatureHash(SelectedAsset, SafeSessionId, SafeDeviceInfo);
                int userId = _authService.CurrentUser?.Id ?? 1;

                await _dbService.AddAssetAsync(SelectedAsset, signatureHash, SafeIp, SafeDeviceInfo, SafeSessionId, userId).ConfigureAwait(false);
                await _dbService.LogAssetAuditAsync(SelectedAsset.Id, "CREATE", userId, SafeIp, SafeDeviceInfo, SafeSessionId, signatureHash).ConfigureAwait(false);

                StatusMessage = $"Asset '{SelectedAsset.AssetName}' added.";
                await LoadAssetsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Add failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Updates the currently selected asset (with digital signature and audit log).
        /// </summary>
        public async Task UpdateAssetAsync()
        {
            if (SelectedAsset == null)
            {
                StatusMessage = "No asset selected.";
                return;
            }
            IsBusy = true;
            try
            {
                string signatureHash = DigitalSignatureHelper.GenerateSignatureHash(SelectedAsset, SafeSessionId, SafeDeviceInfo);
                int userId = _authService.CurrentUser?.Id ?? 1;

                await _dbService.UpdateAssetAsync(SelectedAsset, signatureHash, SafeIp, SafeDeviceInfo, SafeSessionId, userId).ConfigureAwait(false);
                await _dbService.LogAssetAuditAsync(SelectedAsset.Id, "UPDATE", userId, SafeIp, SafeDeviceInfo, SafeSessionId, signatureHash).ConfigureAwait(false);

                StatusMessage = $"Asset '{SelectedAsset.AssetName}' updated.";
                await LoadAssetsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Deletes the currently selected asset and logs the operation.
        /// </summary>
        public async Task DeleteAssetAsync()
        {
            if (SelectedAsset == null)
            {
                StatusMessage = "No asset selected.";
                return;
            }
            IsBusy = true;
            try
            {
                int userId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.DeleteAssetAsync(SelectedAsset.Id, SafeIp, SafeDeviceInfo, SafeSessionId, userId).ConfigureAwait(false);
                await _dbService.LogAssetAuditAsync(SelectedAsset.Id, "DELETE", userId, SafeIp, SafeDeviceInfo, SafeSessionId, string.Empty).ConfigureAwait(false);

                StatusMessage = $"Asset '{SelectedAsset.AssetName}' deleted.";
                await LoadAssetsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Rolls back the selected asset to a previous state (server-side implementation dependent).
        /// </summary>
        public async Task RollbackAssetAsync()
        {
            if (SelectedAsset == null)
            {
                StatusMessage = "No asset selected.";
                return;
            }
            IsBusy = true;
            try
            {
                int userId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.RollbackAssetAsync(SelectedAsset.Id, SafeIp, SafeDeviceInfo, SafeSessionId, userId).ConfigureAwait(false);
                await _dbService.LogAssetAuditAsync(SelectedAsset.Id, "ROLLBACK", userId, SafeIp, SafeDeviceInfo, SafeSessionId, string.Empty).ConfigureAwait(false);
                StatusMessage = $"Rollback completed for asset '{SelectedAsset.AssetName}'.";
                await LoadAssetsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Rollback failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Exports all filtered assets and logs the export operation.
        /// </summary>
        public async Task ExportAssetsAsync()
        {
            IsBusy = true;
            try
            {
                int userId = _authService.CurrentUser?.Id ?? 1;
                await _dbService.ExportAssetsAsync(FilteredAssets.ToList(), SafeIp, SafeDeviceInfo, SafeSessionId, userId).ConfigureAwait(false);
                await _dbService.LogAssetAuditAsync(0, "EXPORT", userId, SafeIp, SafeDeviceInfo, SafeSessionId, $"Exported {FilteredAssets.Count} assets.").ConfigureAwait(false);
                StatusMessage = "Assets exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Filters the assets collection based on search and filter parameters.
        /// </summary>
        public void FilterAssets()
        {
            var filtered = Assets.Where(a =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (a.AssetName?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (a.AssetCode?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (a.Location?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(StatusFilter) || string.Equals(Convert.ToString(a.Status), StatusFilter, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(RiskFilter) || string.Equals(Convert.ToString(a.RiskRating), RiskFilter, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(TypeFilter) || string.Equals(Convert.ToString(a.AssetType), TypeFilter, StringComparison.OrdinalIgnoreCase))
            );
            FilteredAssets = new ObservableCollection<Asset>(filtered);
        }

        /// <summary>
        /// Indicates whether the current user can edit assets (admin or superadmin only).
        /// </summary>
        public bool CanEditAssets => _authService.CurrentUser?.Role == "admin" || _authService.CurrentUser?.Role == "superadmin";

        #endregion

        #region === Audit/Auxiliary ===

        /// <summary>
        /// Loads the audit history for a specific asset.
        /// </summary>
        /// <param name="assetId">The asset identifier.</param>
        /// <returns>An observable collection of <see cref="AuditEntryDto"/>.</returns>
        public async Task<ObservableCollection<AuditEntryDto>> LoadAssetAuditAsync(int assetId)
        {
            var audits = await _dbService.GetAuditLogForEntityAsync("assets", assetId).ConfigureAwait(false);
            var list = audits?.Cast<AuditEntryDto>() ?? Enumerable.Empty<AuditEntryDto>();
            return new ObservableCollection<AuditEntryDto>(list);
        }

        #endregion

        #region === INotifyPropertyChanged ===

        /// <summary>Raised when a property value changes.</summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raises <see cref="PropertyChanged"/> for the given property.</summary>
        /// <param name="propName">Property name (auto-filled by compiler).</param>
        protected void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        #endregion
    }
}
