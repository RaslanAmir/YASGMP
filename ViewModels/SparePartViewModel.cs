using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using YasGMP.Models;
using YasGMP.Models.DTO; // audits
using YasGMP.Services;
using YasGMP.Helpers;
using CommunityToolkit.Mvvm.Input;

// alias VM SparePart → domain Part
using SparePart = YasGMP.Models.Part;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// Spare part management (GMP-ready): CRUD, audit, rollback, export.
    /// </summary>
    public class SparePartViewModel : INotifyPropertyChanged
    {
        #region Fields & Ctor

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<SparePart> _spareParts = new();
        private ObservableCollection<SparePart> _filteredSpareParts = new();

        // CS8618 FIX: nullable selection/filters, status text initialized.
        private SparePart? _selectedSparePart;
        private string? _searchTerm;
        private string? _statusFilter;
        private string? _stockFilter;
        private string? _assetFilter;
        private string? _supplierFilter;
        private bool _isBusy;
        private string _statusMessage = string.Empty;

        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        /// <summary>Initializes the view model.</summary>
        public SparePartViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId = _authService.CurrentSessionId ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? string.Empty;
            _currentIpAddress  = _authService.CurrentIpAddress  ?? string.Empty;

            LoadSparePartsCommand     = new AsyncRelayCommand(LoadSparePartsAsync);
            AddSparePartCommand       = new AsyncRelayCommand(AddSparePartAsync,      () => !IsBusy && SelectedSparePart != null);
            UpdateSparePartCommand    = new AsyncRelayCommand(UpdateSparePartAsync,   () => !IsBusy && SelectedSparePart != null);
            DeleteSparePartCommand    = new AsyncRelayCommand(DeleteSparePartAsync,   () => !IsBusy && SelectedSparePart != null);
            RollbackSparePartCommand  = new AsyncRelayCommand(RollbackSparePartAsync, () => !IsBusy && SelectedSparePart != null);
            ExportSparePartsCommand   = new AsyncRelayCommand(ExportSparePartsAsync,  () => !IsBusy);
            FilterChangedCommand      = new RelayCommand(FilterSpareParts);

            _ = LoadSparePartsAsync();
        }

        #endregion

        #region Properties

        /// <summary>All spare parts.</summary>
        public ObservableCollection<SparePart> SpareParts
        {
            get => _spareParts;
            set { _spareParts = value; OnPropertyChanged(); }
        }

        /// <summary>Filtered view of <see cref="SpareParts"/>.</summary>
        public ObservableCollection<SparePart> FilteredSpareParts
        {
            get => _filteredSpareParts;
            set { _filteredSpareParts = value; OnPropertyChanged(); }
        }

        /// <summary>Currently selected spare part (nullable).</summary>
        public SparePart? SelectedSparePart
        {
            get => _selectedSparePart;
            set { _selectedSparePart = value; OnPropertyChanged(); }
        }

        /// <summary>Free-text search term.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterSpareParts(); }
        }

        /// <summary>Status filter (e.g., active/obsolete/critical).</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterSpareParts(); }
        }

        /// <summary>Stock status filter.</summary>
        public string? StockFilter
        {
            get => _stockFilter;
            set { _stockFilter = value; OnPropertyChanged(); FilterSpareParts(); }
        }

        /// <summary>Related asset filter.</summary>
        public string? AssetFilter
        {
            get => _assetFilter;
            set { _assetFilter = value; OnPropertyChanged(); FilterSpareParts(); }
        }

        /// <summary>Supplier filter.</summary>
        public string? SupplierFilter
        {
            get => _supplierFilter;
            set { _supplierFilter = value; OnPropertyChanged(); FilterSpareParts(); }
        }

        /// <summary>Busy indicator.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>Last status or error message.</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Available status values.</summary>
        public string[] AvailableStatuses => new[] { "active", "obsolete", "critical" };

        /// <summary>Available stock states.</summary>
        public string[] AvailableStockStatuses => new[] { "in_stock", "low", "out_of_stock" };

        /// <summary>Whether current user can change spare parts.</summary>
        public bool CanEditSpareParts => _authService.CurrentUser?.Role is "admin" or "superadmin";

        #endregion

        #region Commands

        public ICommand LoadSparePartsCommand { get; }
        public ICommand AddSparePartCommand { get; }
        public ICommand UpdateSparePartCommand { get; }
        public ICommand DeleteSparePartCommand { get; }
        public ICommand RollbackSparePartCommand { get; }
        public ICommand ExportSparePartsCommand { get; }
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region Methods

        /// <summary>Loads all spare parts.</summary>
        public async Task LoadSparePartsAsync()
        {
            IsBusy = true;
            try
            {
                var spareParts = await _dbService.GetAllSparePartsFullAsync();
                SpareParts = new ObservableCollection<SparePart>(spareParts ?? Enumerable.Empty<SparePart>());
                FilterSpareParts();
                StatusMessage = $"Loaded {SpareParts.Count} spare parts.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading spare parts: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Adds a new spare part and logs audit with signature.</summary>
        public async Task AddSparePartAsync()
        {
            if (SelectedSparePart == null) { StatusMessage = "No spare part selected."; return; }
            IsBusy = true;
            try
            {
                string sig = DigitalSignatureHelper.GenerateSignatureHash(SelectedSparePart, _currentSessionId, _currentDeviceInfo);
                int actor = _authService.CurrentUser?.Id ?? 0;

                // DatabaseService signature: (Part part, int actorUserId, string? ip = null, string? device = null, ...)
                await _dbService.AddSparePartAsync(SelectedSparePart, actor, _currentIpAddress, _currentDeviceInfo);

                // Log: partId + action + actor + optional note/ip/device/session
                await _dbService.LogSparePartAuditAsync(SelectedSparePart.Id, "CREATE", actor, sig, _currentIpAddress, _currentDeviceInfo, _currentSessionId);

                StatusMessage = $"Spare part '{SelectedSparePart.Name}' added.";
                await LoadSparePartsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Add failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Updates the selected spare part with audit.</summary>
        public async Task UpdateSparePartAsync()
        {
            if (SelectedSparePart == null) { StatusMessage = "No spare part selected."; return; }
            IsBusy = true;
            try
            {
                string sig = DigitalSignatureHelper.GenerateSignatureHash(SelectedSparePart, _currentSessionId, _currentDeviceInfo);
                int actor = _authService.CurrentUser?.Id ?? 0;

                await _dbService.UpdateSparePartAsync(SelectedSparePart, actor, _currentIpAddress, _currentDeviceInfo);
                await _dbService.LogSparePartAuditAsync(SelectedSparePart.Id, "UPDATE", actor, sig, _currentIpAddress, _currentDeviceInfo, _currentSessionId);

                StatusMessage = $"Spare part '{SelectedSparePart.Name}' updated.";
                await LoadSparePartsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Deletes the selected spare part with audit.</summary>
        public async Task DeleteSparePartAsync()
        {
            if (SelectedSparePart == null) { StatusMessage = "No spare part selected."; return; }
            IsBusy = true;
            try
            {
                int actor = _authService.CurrentUser?.Id ?? 0;

                await _dbService.DeleteSparePartAsync(SelectedSparePart.Id, actor, _currentIpAddress);
                await _dbService.LogSparePartAuditAsync(SelectedSparePart.Id, "DELETE", actor, null, _currentIpAddress, _currentDeviceInfo, _currentSessionId);

                StatusMessage = $"Spare part '{SelectedSparePart.Name}' deleted.";
                await LoadSparePartsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Queues rollback for the selected spare part.</summary>
        public async Task RollbackSparePartAsync()
        {
            if (SelectedSparePart == null) { StatusMessage = "No spare part selected."; return; }
            IsBusy = true;
            try
            {
                int actor = _authService.CurrentUser?.Id ?? 0;

                await _dbService.RollbackSparePartAsync(SelectedSparePart.Id, actor, _currentIpAddress, _currentDeviceInfo, _currentSessionId);
                StatusMessage = $"Rollback completed for spare part '{SelectedSparePart.Name}'.";
                await LoadSparePartsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Rollback failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Exports the filtered spare parts and logs audit.</summary>
        public async Task ExportSparePartsAsync()
        {
            IsBusy = true;
            try
            {
                int actor = _authService.CurrentUser?.Id ?? 0;
                var fmt = await YasGMP.Helpers.ExportFormatPrompt.PromptAsync();
                await _dbService.ExportSparePartsAsync(FilteredSpareParts.ToList(), fmt, actor, _currentIpAddress, _currentDeviceInfo, _currentSessionId);
                await _dbService.LogSparePartAuditAsync(0, "EXPORT", actor, null, _currentIpAddress, _currentDeviceInfo, _currentSessionId);
                StatusMessage = "Spare parts exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Applies free-text filter to the spare parts list.</summary>
        public void FilterSpareParts()
        {
            var filtered = SpareParts.Where(p =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (p.Name?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Code?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (p.Description?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)));

            FilteredSpareParts = new ObservableCollection<SparePart>(filtered);
        }

        /// <summary>Loads audit history for a spare part.</summary>
        public async Task<ObservableCollection<AuditEntryDto>> LoadSparePartAuditAsync(int sparePartId)
        {
            var audits = await _dbService.GetAuditLogForEntityAsync("parts", sparePartId);
            return new ObservableCollection<AuditEntryDto>(audits ?? Enumerable.Empty<AuditEntryDto>());
        }

        #endregion

        #region INotifyPropertyChanged

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raises <see cref="PropertyChanged"/>.</summary>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}
