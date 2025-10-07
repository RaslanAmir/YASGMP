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
    /// Ultra-robust ViewModel for supplier/vendor management (GMP/Annex 11/21 CFR Part 11 ready).
    /// Provides: load, add, update, delete, rollback, filtering, export, and audit retrieval.
    /// </summary>
    public class SupplierViewModel : INotifyPropertyChanged
    {
        #region === Fields & Constructor ============================================

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<Supplier> _suppliers = new();
        private ObservableCollection<Supplier> _filteredSuppliers = new();

        // Selections / filters may legitimately be null at runtime → mark nullable to reflect reality
        private Supplier? _selectedSupplier;
        private string? _searchTerm;
        private string? _statusFilter;
        private string? _riskFilter;
        private string? _complianceFilter;

        private bool _isBusy;
        private string? _statusMessage;

        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        /// <summary>
        /// Creates a new <see cref="SupplierViewModel"/>.
        /// </summary>
        /// <param name="dbService">Database gateway.</param>
        /// <param name="authService">Authentication/session context.</param>
        public SupplierViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService  = dbService  ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            // Harden session/device/IP against nulls (prevents CS8601 elsewhere)
            _currentSessionId = _authService.CurrentSessionId ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? "ui";
            _currentIpAddress  = _authService.CurrentIpAddress  ?? "0.0.0.0";

            LoadSuppliersCommand   = new AsyncRelayCommand(LoadSuppliersAsync);
            AddSupplierCommand     = new AsyncRelayCommand(AddSupplierAsync,     () => !IsBusy);
            UpdateSupplierCommand  = new AsyncRelayCommand(UpdateSupplierAsync,  () => !IsBusy && SelectedSupplier is not null);
            DeleteSupplierCommand  = new AsyncRelayCommand(DeleteSupplierAsync,  () => !IsBusy && SelectedSupplier is not null);
            RollbackSupplierCommand= new AsyncRelayCommand(RollbackSupplierAsync,() => !IsBusy && SelectedSupplier is not null);
            ExportSuppliersCommand = new AsyncRelayCommand(ExportSuppliersAsync, () => !IsBusy);
            FilterChangedCommand   = new RelayCommand(FilterSuppliers);

            _ = LoadSuppliersAsync();
        }

        #endregion

        #region === Properties ======================================================

        /// <summary>All suppliers loaded from the database.</summary>
        public ObservableCollection<Supplier> Suppliers
        {
            get => _suppliers;
            set { _suppliers = value; OnPropertyChanged(); }
        }

        /// <summary>Filtered projection for the UI list/grid.</summary>
        public ObservableCollection<Supplier> FilteredSuppliers
        {
            get => _filteredSuppliers;
            set { _filteredSuppliers = value; OnPropertyChanged(); }
        }

        /// <summary>Currently selected supplier or <c>null</c>.</summary>
        public Supplier? SelectedSupplier
        {
            get => _selectedSupplier;
            set { _selectedSupplier = value; OnPropertyChanged(); }
        }

        /// <summary>Search phrase for suppliers.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); FilterSuppliers(); }
        }

        /// <summary>Status filter.</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); FilterSuppliers(); }
        }

        /// <summary>Risk level filter.</summary>
        public string? RiskFilter
        {
            get => _riskFilter;
            set { _riskFilter = value; OnPropertyChanged(); FilterSuppliers(); }
        }

        /// <summary>Compliance filter.</summary>
        public string? ComplianceFilter
        {
            get => _complianceFilter;
            set { _complianceFilter = value; OnPropertyChanged(); FilterSuppliers(); }
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

        /// <summary>Available status values.</summary>
        public string[] AvailableStatuses   => new[] { "active", "blocked", "expired", "pending", "SUSPENDED" };

        /// <summary>Available risk levels.</summary>
        public string[] AvailableRisks      => new[] { "low", "medium", "high", "critical" };

        /// <summary>Compliance filter options.</summary>
        public string[] AvailableCompliance => new[] { "qualified", "unqualified" };

        #endregion

        #region === Commands ========================================================
        /// <summary>
        /// Gets or sets the load suppliers command.
        /// </summary>

        public ICommand LoadSuppliersCommand { get; }
        /// <summary>
        /// Gets or sets the add supplier command.
        /// </summary>
        public ICommand AddSupplierCommand { get; }
        /// <summary>
        /// Gets or sets the update supplier command.
        /// </summary>
        public ICommand UpdateSupplierCommand { get; }
        /// <summary>
        /// Gets or sets the delete supplier command.
        /// </summary>
        public ICommand DeleteSupplierCommand { get; }
        /// <summary>
        /// Gets or sets the rollback supplier command.
        /// </summary>
        public ICommand RollbackSupplierCommand { get; }
        /// <summary>
        /// Gets or sets the export suppliers command.
        /// </summary>
        public ICommand ExportSuppliersCommand { get; }
        /// <summary>
        /// Gets or sets the filter changed command.
        /// </summary>
        public ICommand FilterChangedCommand { get; }

        #endregion

        #region === Methods =========================================================

        /// <summary>Loads suppliers and applies active filters.</summary>
        public async Task LoadSuppliersAsync()
        {
            IsBusy = true;
            try
            {
                var suppliers = await _dbService.GetAllSuppliersFullAsync().ConfigureAwait(false);
                Suppliers = new ObservableCollection<Supplier>(suppliers ?? new List<Supplier>());
                FilterSuppliers();
                StatusMessage = $"Loaded {Suppliers.Count} suppliers.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading suppliers: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Adds a supplier and records an audit entry.</summary>
        public async Task AddSupplierAsync()
        {
            if (SelectedSupplier is null) { StatusMessage = "No supplier selected."; return; }
            IsBusy = true;
            try
            {
                var userId = _authService.CurrentUser?.Id ?? 1;

                var signatureHash = DigitalSignatureHelper.GenerateSignatureHash(
                    $"{SelectedSupplier.Id}|{SelectedSupplier.Name}|{_currentSessionId}|{_currentDeviceInfo}");

                var newId = await _dbService.AddSupplierAsync(
                    SelectedSupplier, userId, _currentIpAddress, _currentDeviceInfo).ConfigureAwait(false);

                if (newId > 0) SelectedSupplier.Id = newId;

                await _dbService.LogSupplierAuditAsync(
                    SelectedSupplier.Id, "CREATE", userId, $"sig={signatureHash}; session={_currentSessionId}",
                    _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);

                StatusMessage = $"Supplier '{SelectedSupplier.Name}' added.";
                await LoadSuppliersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Add failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Updates the selected supplier and logs the change.</summary>
        public async Task UpdateSupplierAsync()
        {
            if (SelectedSupplier is null) { StatusMessage = "No supplier selected."; return; }
            IsBusy = true;
            try
            {
                var userId = _authService.CurrentUser?.Id ?? 1;
                var signatureHash = DigitalSignatureHelper.GenerateSignatureHash(
                    $"{SelectedSupplier.Id}|{SelectedSupplier.Name}|{_currentSessionId}|{_currentDeviceInfo}");

                await _dbService.UpdateSupplierAsync(
                    SelectedSupplier, userId, _currentIpAddress, _currentDeviceInfo).ConfigureAwait(false);

                await _dbService.LogSupplierAuditAsync(
                    SelectedSupplier.Id, "UPDATE", userId, $"sig={signatureHash}; session={_currentSessionId}",
                    _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);

                StatusMessage = $"Supplier '{SelectedSupplier.Name}' updated.";
                await LoadSuppliersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Deletes the selected supplier and writes an audit entry.</summary>
        public async Task DeleteSupplierAsync()
        {
            if (SelectedSupplier is null) { StatusMessage = "No supplier selected."; return; }
            IsBusy = true;
            try
            {
                var userId = _authService.CurrentUser?.Id ?? 1;

                await _dbService.DeleteSupplierAsync(
                    SelectedSupplier.Id, userId, _currentIpAddress, _currentDeviceInfo).ConfigureAwait(false);

                await _dbService.LogSupplierAuditAsync(
                    SelectedSupplier.Id, "DELETE", userId, $"session={_currentSessionId}",
                    _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);

                StatusMessage = $"Supplier '{SelectedSupplier.Name}' deleted.";
                await LoadSuppliersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Requests a rollback (audit-only action).</summary>
        public async Task RollbackSupplierAsync()
        {
            if (SelectedSupplier is null) { StatusMessage = "No supplier selected."; return; }
            IsBusy = true;
            try
            {
                var userId = _authService.CurrentUser?.Id ?? 1;

                await _dbService.RollbackSupplierAsync(
                    SelectedSupplier.Id, userId, _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);

                StatusMessage = $"Rollback completed for supplier '{SelectedSupplier.Name}'.";
                await LoadSuppliersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Rollback failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Exports the filtered suppliers and records an export audit line.</summary>
        public async Task ExportSuppliersAsync()
        {
            IsBusy = true;
            try
            {
                var userId = _authService.CurrentUser?.Id ?? 1;

                await _dbService.ExportSuppliersAsync(
                    FilteredSuppliers?.ToList() ?? new List<Supplier>(),
                    format: "csv",
                    actorUserId: userId,
                    ip: _currentIpAddress,
                    device: _currentDeviceInfo,
                    sessionId: _currentSessionId).ConfigureAwait(false);

                await _dbService.LogSupplierAuditAsync(
                    0, "EXPORT", userId,
                    $"Exported {FilteredSuppliers?.Count ?? 0} items; session={_currentSessionId}",
                    _currentIpAddress, _currentDeviceInfo, _currentSessionId).ConfigureAwait(false);

                StatusMessage = "Suppliers exported successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Applies client-side filtering across several fields.</summary>
        public void FilterSuppliers()
        {
            var filtered = Suppliers.Where(s =>
                (string.IsNullOrWhiteSpace(SearchTerm) ||
                    (s.Name?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.VatNumber?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.SupplierType?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Address?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                (string.IsNullOrWhiteSpace(StatusFilter) || string.Equals(s.Status, StatusFilter, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(RiskFilter) || string.Equals(s.RiskLevel, RiskFilter, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(ComplianceFilter) ||
                    (ComplianceFilter == "qualified"   && s.IsQualified) ||
                    (ComplianceFilter == "unqualified" && !s.IsQualified))
            );

            FilteredSuppliers = new ObservableCollection<Supplier>(filtered);
        }

        /// <summary>Gate to enable edit actions for privileged roles.</summary>
        public bool CanEditSuppliers =>
            _authService.CurrentUser?.Role == "admin" || _authService.CurrentUser?.Role == "superadmin";

        #endregion

        #region === Audit/Auxiliary =================================================

        /// <summary>
        /// Safely maps an <see cref="YasGMP.Models.DTO.AuditEntryDto"/> to <see cref="AuditLogEntry"/>.
        /// <para>
        /// Coalesces nullable DTO strings to empty strings for non-nullable target properties to avoid CS8601,
        /// while preserving forensics fidelity and GMP compliance.
        /// </para>
        /// </summary>
        private static AuditLogEntry? MapAuditEntryDtoToAuditLogEntry(YasGMP.Models.DTO.AuditEntryDto dto)
        {
            if (dto == null) return null;

            return new AuditLogEntry
            {
                Id               = dto.Id ?? 0,
                // ↓↓↓ CS8601 root fixes: coalesce nullable DTO strings to non-null strings
                EntityType       = dto.Entity ?? string.Empty,                 // maps to TableName (non-nullable)
                EntityId         = int.TryParse(dto.EntityId, out var eid) ? eid : 0,
                PerformedBy      = !string.IsNullOrEmpty(dto.Username) ? dto.Username : (dto.UserId?.ToString() ?? string.Empty),
                Action           = dto.Action ?? string.Empty,                 // non-nullable
                OldValue         = dto.OldValue,
                NewValue         = dto.NewValue,
                ChangedAt        = dto.Timestamp,
                DeviceInfo       = dto.DeviceInfo,
                IpAddress        = dto.IpAddress ?? string.Empty,             // setter expects non-null
                SessionId        = dto.SessionId,
                DigitalSignature = dto.DigitalSignature,
                Note             = dto.Note ?? string.Empty                   // setter expects non-null
            };
        }

        /// <summary>Loads audit entries for a supplier and maps them to UI model.</summary>
        public async Task<ObservableCollection<AuditLogEntry>> LoadSupplierAuditAsync(int supplierId)
        {
            var dtos = await _dbService.GetAuditLogForEntityAsync("suppliers", supplierId).ConfigureAwait(false);
            var audits = (dtos?.Select(MapAuditEntryDtoToAuditLogEntry)
                              .Where(x => x != null)
                              .Select(x => x!) ?? Enumerable.Empty<AuditLogEntry>());
            return new ObservableCollection<AuditLogEntry>(audits);
        }

        #endregion

        #region === INotifyPropertyChanged ==========================================

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Safely notifies the UI of a property change.</summary>
        /// <param name="propName">Property name (auto-filled by compiler).</param>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}
