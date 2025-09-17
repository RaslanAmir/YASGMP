using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// ViewModel za rad sa skladištima – prikaz, dodavanje i ažuriranje skladišta te vezanih stanja zaliha.
    /// </summary>
    public class WarehouseViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<Warehouse> _warehouses = new();
        private ObservableCollection<StockLevel> _stockLevels = new();
        private ObservableCollection<WarehousePartRow> _filteredParts = new();
        private ObservableCollection<WarehousePartRow> _lowStockHighlights = new();
        private ObservableCollection<User> _responsibleUsers = new();
        private ObservableCollection<MovementPreview> _movementHistory = new();

        private readonly List<WarehousePartRow> _allPartRows = new();

        private Warehouse? _selectedWarehouse;
        private WarehousePartRow? _selectedPart;
        private string _partSearchTerm = string.Empty;
        private bool _showOnlyLowStock;
        private bool _isBusy;
        private string _statusMessage = string.Empty;

        private string _editName = string.Empty;
        private string _editLocation = string.Empty;
        private string _editStatus = string.Empty;
        private string _editNote = string.Empty;
        private User? _selectedResponsibleUser;

        private int _totalLowStockCount;

        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        private bool _schemaEnsured;
        private bool _isMovementHistoryLoading;
        private string _movementHistoryStatus = "Odaberite skladište ili artikl za prikaz povijesti.";
        private CancellationTokenSource? _movementHistoryRefreshCts;

        /// <summary>
        /// Initializes a new instance of the <see cref="WarehouseViewModel"/> class.
        /// </summary>
        /// <param name="dbService">Database gateway.</param>
        /// <param name="authService">Authentication/session context.</param>
        /// <exception cref="ArgumentNullException">Thrown when dependencies are null.</exception>
        public WarehouseViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId = _authService.CurrentSessionId ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? string.Empty;
            _currentIpAddress = _authService.CurrentIpAddress ?? string.Empty;

            LoadCommand = new AsyncRelayCommand(LoadAsync, () => !IsBusy);
            RefreshStockCommand = new AsyncRelayCommand(LoadWarehousePartsAsync, () => !IsBusy);
            AddWarehouseCommand = new AsyncRelayCommand(AddWarehouseAsync, CanAddWarehouse);
            UpdateWarehouseCommand = new AsyncRelayCommand(UpdateWarehouseAsync, CanUpdateWarehouse);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            BeginNewWarehouseCommand = new RelayCommand(BeginNewWarehouse);
            ToggleLowStockCommand = new RelayCommand(() => ShowOnlyLowStock = !ShowOnlyLowStock);
            RefreshMovementHistoryCommand = new AsyncRelayCommand(RefreshMovementHistoryPreviewAsync, () => !IsMovementHistoryLoading);

            _ = LoadAsync();
        }

        /// <summary>Occurs when a property value changes.</summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Lista svih skladišta.</summary>
        public ObservableCollection<Warehouse> Warehouses
        {
            get => _warehouses;
            private set => SetProperty(ref _warehouses, value ?? new ObservableCollection<Warehouse>());
        }

        /// <summary>Raw stock level entries (bez filtriranja).</summary>
        public ObservableCollection<StockLevel> StockLevels
        {
            get => _stockLevels;
            private set => SetProperty(ref _stockLevels, value ?? new ObservableCollection<StockLevel>());
        }

        /// <summary>Parts filtered by UI kriterije.</summary>
        public ObservableCollection<WarehousePartRow> FilteredParts
        {
            get => _filteredParts;
            private set
            {
                if (SetProperty(ref _filteredParts, value ?? new ObservableCollection<WarehousePartRow>()))
                {
                    OnPropertyChanged(nameof(FilteredPartCount));
                    OnPropertyChanged(nameof(FilteredLowStockCount));
                    OnPropertyChanged(nameof(HasFilteredLowStock));
                    OnPropertyChanged(nameof(LowStockBannerMessage));
                    OnPropertyChanged(nameof(SelectedWarehouseLowStockSummary));
                }
            }
        }

        /// <summary>Highlighted parts that are currently low.</summary>
        public ObservableCollection<WarehousePartRow> LowStockHighlights
        {
            get => _lowStockHighlights;
            private set => SetProperty(ref _lowStockHighlights, value ?? new ObservableCollection<WarehousePartRow>());
        }

        /// <summary>Sažetak povijesti kretanja zaliha.</summary>
        public ObservableCollection<MovementPreview> MovementHistory
        {
            get => _movementHistory;
            private set
            {
                if (SetProperty(ref _movementHistory, value ?? new ObservableCollection<MovementPreview>()))
                {
                    OnPropertyChanged(nameof(HasMovementHistory));
                }
            }
        }

        /// <summary>True ako postoji barem jedan zapis u povijesti.</summary>
        public bool HasMovementHistory => MovementHistory.Count > 0;

        /// <summary>Indikator dohvaća li se trenutno povijest kretanja.</summary>
        public bool IsMovementHistoryLoading
        {
            get => _isMovementHistoryLoading;
            private set
            {
                if (SetProperty(ref _isMovementHistoryLoading, value))
                {
                    (RefreshMovementHistoryCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>Statusna poruka za povijest kretanja.</summary>
        public string MovementHistoryStatus
        {
            get => _movementHistoryStatus;
            private set => SetProperty(ref _movementHistoryStatus, value ?? string.Empty);
        }

        /// <summary>Lista korisnika koji mogu biti odgovorne osobe.</summary>
        public ObservableCollection<User> ResponsibleUsers
        {
            get => _responsibleUsers;
            private set => SetProperty(ref _responsibleUsers, value ?? new ObservableCollection<User>());
        }

        /// <summary>Trenutno odabrano skladište (null = pregled svih).</summary>
        public Warehouse? SelectedWarehouse
        {
            get => _selectedWarehouse;
            set
            {
                if (SetProperty(ref _selectedWarehouse, value))
                {
                    PopulateEditorFromWarehouse(value);
                    ApplyPartFilters();
                    OnPropertyChanged(nameof(SelectedWarehouseLowStockSummary));
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>Trenutno odabrani artikl u gridu.</summary>
        public WarehousePartRow? SelectedPart
        {
            get => _selectedPart;
            set
            {
                if (SetProperty(ref _selectedPart, value))
                {
                    QueueMovementHistoryRefresh();
                }
            }
        }

        /// <summary>Tražilica po šifri/nazivu dijela.</summary>
        public string PartSearchTerm
        {
            get => _partSearchTerm;
            set
            {
                if (SetProperty(ref _partSearchTerm, value ?? string.Empty))
                {
                    ApplyPartFilters();
                }
            }
        }

        /// <summary>Prikazati samo artikle ispod min. praga?</summary>
        public bool ShowOnlyLowStock
        {
            get => _showOnlyLowStock;
            set
            {
                if (SetProperty(ref _showOnlyLowStock, value))
                {
                    ApplyPartFilters();
                }
            }
        }

        /// <summary>UI busy flag.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>Status poruka za korisnika.</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value ?? string.Empty);
        }

        /// <summary>Naziv skladišta u editoru.</summary>
        public string EditName
        {
            get => _editName;
            set
            {
                if (SetProperty(ref _editName, value ?? string.Empty))
                {
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>Lokacija skladišta u editoru.</summary>
        public string EditLocation
        {
            get => _editLocation;
            set => SetProperty(ref _editLocation, value ?? string.Empty);
        }

        /// <summary>Status skladišta (npr. active, archived).</summary>
        public string EditStatus
        {
            get => _editStatus;
            set => SetProperty(ref _editStatus, value ?? string.Empty);
        }

        /// <summary>Bilješka o skladištu.</summary>
        public string EditNote
        {
            get => _editNote;
            set => SetProperty(ref _editNote, value ?? string.Empty);
        }

        /// <summary>Odabrani odgovorni korisnik.</summary>
        public User? SelectedResponsibleUser
        {
            get => _selectedResponsibleUser;
            set => SetProperty(ref _selectedResponsibleUser, value);
        }

        /// <summary>Ukupno artikala koji su ispod praga (sva skladišta).</summary>
        public int TotalLowStockCount
        {
            get => _totalLowStockCount;
            private set
            {
                if (SetProperty(ref _totalLowStockCount, value))
                {
                    OnPropertyChanged(nameof(HasAnyLowStock));
                    OnPropertyChanged(nameof(GlobalLowStockSummary));
                }
            }
        }

        /// <summary>True ako postoji barem jedan artikl ispod praga.</summary>
        public bool HasAnyLowStock => TotalLowStockCount > 0;

        /// <summary>Broj prikazanih artikala.</summary>
        public int FilteredPartCount => FilteredParts.Count;

        /// <summary>Broj prikazanih artikala koji su ispod praga.</summary>
        public int FilteredLowStockCount => FilteredParts.Count(p => p.IsLowStock);

        /// <summary>Indikator da li filtrirani prikaz sadrži rizične artikle.</summary>
        public bool HasFilteredLowStock => FilteredLowStockCount > 0;

        /// <summary>Sažetak za globalni banner upozorenja.</summary>
        public string GlobalLowStockSummary => HasAnyLowStock
            ? $"{TotalLowStockCount} artikala je ispod definiranih pragova u svim skladištima."
            : "Sve zalihe su iznad definiranih pragova.";

        /// <summary>Sažetak za trenutno odabrano skladište.</summary>
        public string SelectedWarehouseLowStockSummary => SelectedWarehouse is null
            ? GlobalLowStockSummary
            : HasFilteredLowStock
                ? $"{FilteredLowStockCount} artikala u '{SelectedWarehouse.Name}' zahtijeva nadopunu."
                : $"Svi artikli u '{SelectedWarehouse.Name}' su iznad minimalnih pragova.";

        /// <summary>Poruka za UI banner kada je filter aktivan.</summary>
        public string LowStockBannerMessage => HasFilteredLowStock
            ? $"⚠️ {FilteredLowStockCount} artikala traži pažnju ({(SelectedWarehouse is null ? "sva skladišta" : SelectedWarehouse.Name)})."
            : string.Empty;

        /// <summary>Predefinirane opcije statusa (hint za UI).</summary>
        public string[] WarehouseStatusOptions { get; } = new[] { "active", "standby", "maintenance", "archived" };

        /// <summary>Komanda za inicijalno učitavanje.</summary>
        public IAsyncRelayCommand LoadCommand { get; }

        /// <summary>Komanda za osvježenje stanja zaliha.</summary>
        public IAsyncRelayCommand RefreshStockCommand { get; }

        /// <summary>Komanda za dodavanje skladišta.</summary>
        public IAsyncRelayCommand AddWarehouseCommand { get; }

        /// <summary>Komanda za ažuriranje skladišta.</summary>
        public IAsyncRelayCommand UpdateWarehouseCommand { get; }

        /// <summary>Komanda za reset filtera.</summary>
        public ICommand ClearFiltersCommand { get; }

        /// <summary>Komanda za pripremu forme za novo skladište.</summary>
        public ICommand BeginNewWarehouseCommand { get; }

        /// <summary>Komanda za brzi toggle "prikaži samo low stock".</summary>
        public ICommand ToggleLowStockCommand { get; }

        /// <summary>Komanda za ručno osvježenje povijesti kretanja.</summary>
        public IAsyncRelayCommand RefreshMovementHistoryCommand { get; }

        /// <summary>
        /// Učitava skladišta, odgovorne osobe i zalihe.
        /// </summary>
        public async Task LoadAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                StatusMessage = "Učitavanje skladišta...";
                await EnsureWarehouseSchemaAsync().ConfigureAwait(false);
                await LoadResponsibleUsersAsync().ConfigureAwait(false);
                await LoadWarehouseListAsync().ConfigureAwait(false);
                await LoadWarehousePartsInternalAsync().ConfigureAwait(false);
                StatusMessage = $"Učitano {Warehouses.Count} skladišta i {StockLevels.Count} zapisa o zalihama.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Greška pri učitavanju: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Ručno osvježavanje stanja zaliha (bez ponovnog učitavanja korisnika).</summary>
        public async Task LoadWarehousePartsAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                await EnsureWarehouseSchemaAsync().ConfigureAwait(false);
                await LoadWarehousePartsInternalAsync().ConfigureAwait(false);
                StatusMessage = $"Zalihe osvježene ({FilteredPartCount} artikala u prikazu).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Neuspjelo učitavanje zaliha: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Dodaje novo skladište s trenutnim vrijednostima iz editor forme.</summary>
        public async Task AddWarehouseAsync()
        {
            if (IsBusy)
            {
                return;
            }

            var trimmedName = (EditName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                StatusMessage = "Naziv skladišta je obvezan.";
                return;
            }

            IsBusy = true;
            try
            {
                await EnsureWarehouseSchemaAsync().ConfigureAwait(false);

                var trimmedLocation = string.IsNullOrWhiteSpace(EditLocation) ? null : EditLocation.Trim();
                var trimmedStatus = string.IsNullOrWhiteSpace(EditStatus) ? null : EditStatus.Trim();
                var trimmedNote = string.IsNullOrWhiteSpace(EditNote) ? null : EditNote.Trim();
                var responsibleId = SelectedResponsibleUser?.Id;

                var newId = await _dbService.AddWarehouseAsync(trimmedName, trimmedLocation).ConfigureAwait(false);
                await UpdateWarehouseMetadataAsync(newId, trimmedName, trimmedLocation, responsibleId, trimmedStatus, trimmedNote).ConfigureAwait(false);

                await _dbService.LogSystemEventAsync(
                    _authService.CurrentUser?.Id ?? 0,
                    "WAREHOUSE_CREATE",
                    "warehouses",
                    "Warehouse",
                    newId,
                    $"name={trimmedName}; responsible={responsibleId}",
                    _currentIpAddress,
                    "audit",
                    _currentDeviceInfo,
                    _currentSessionId
                ).ConfigureAwait(false);

                await LoadWarehouseListAsync(newId).ConfigureAwait(false);
                await LoadWarehousePartsInternalAsync().ConfigureAwait(false);

                StatusMessage = $"Skladište '{trimmedName}' dodano.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Dodavanje skladišta nije uspjelo: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Ažurira trenutno odabrano skladište.</summary>
        public async Task UpdateWarehouseAsync()
        {
            if (IsBusy)
            {
                return;
            }

            if (SelectedWarehouse is null)
            {
                StatusMessage = "Odaberite skladište za ažuriranje.";
                return;
            }

            var trimmedName = (EditName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                StatusMessage = "Naziv skladišta je obvezan.";
                return;
            }

            IsBusy = true;
            try
            {
                await EnsureWarehouseSchemaAsync().ConfigureAwait(false);

                var trimmedLocation = string.IsNullOrWhiteSpace(EditLocation) ? null : EditLocation.Trim();
                var trimmedStatus = string.IsNullOrWhiteSpace(EditStatus) ? null : EditStatus.Trim();
                var trimmedNote = string.IsNullOrWhiteSpace(EditNote) ? null : EditNote.Trim();
                var responsibleId = SelectedResponsibleUser?.Id;
                var warehouseId = SelectedWarehouse.Id;

                await UpdateWarehouseMetadataAsync(warehouseId, trimmedName, trimmedLocation, responsibleId, trimmedStatus, trimmedNote).ConfigureAwait(false);

                await _dbService.LogSystemEventAsync(
                    _authService.CurrentUser?.Id ?? 0,
                    "WAREHOUSE_UPDATE",
                    "warehouses",
                    "Warehouse",
                    warehouseId,
                    $"name={trimmedName}; responsible={responsibleId}",
                    _currentIpAddress,
                    "audit",
                    _currentDeviceInfo,
                    _currentSessionId
                ).ConfigureAwait(false);

                await LoadWarehouseListAsync(warehouseId).ConfigureAwait(false);
                await LoadWarehousePartsInternalAsync().ConfigureAwait(false);

                StatusMessage = $"Skladište '{trimmedName}' ažurirano.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ažuriranje skladišta nije uspjelo: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Resetira filtere pretrage (bez promjene odabranog skladišta).</summary>
        public void ClearFilters()
        {
            var changed = false;
            if (!string.IsNullOrEmpty(PartSearchTerm))
            {
                _partSearchTerm = string.Empty;
                OnPropertyChanged(nameof(PartSearchTerm));
                changed = true;
            }

            if (ShowOnlyLowStock)
            {
                _showOnlyLowStock = false;
                OnPropertyChanged(nameof(ShowOnlyLowStock));
                changed = true;
            }

            if (changed)
            {
                ApplyPartFilters();
            }
        }

        /// <summary>Priprema editor za unos novog skladišta.</summary>
        public void BeginNewWarehouse()
        {
            SelectedWarehouse = null;
            PopulateEditorFromWarehouse(null);
        }

        /// <summary>
        /// Outline for future inventory history (ULAZ/IZLAZ) module. Currently returns an empty preview list.
        /// </summary>
        /// <param name="warehouse">Optional warehouse filter.</param>
        /// <param name="partId">Optional part filter.</param>
        /// <param name="take">Number of latest transactions to fetch when implemented.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Lista transakcija sortirana po datumu (DESC).</returns>
        public async Task<IReadOnlyList<MovementPreview>> LoadMovementHistoryPreviewAsync(Warehouse? warehouse, int? partId = null, int take = 20, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            int? warehouseId = warehouse?.Id;
            if (warehouseId.HasValue && warehouseId.Value <= 0)
            {
                warehouseId = null;
            }

            int? normalizedPartId = partId.HasValue && partId.Value > 0 ? partId : null;

            var table = await _dbService.GetInventoryMovementPreviewAsync(warehouseId, normalizedPartId, take, token).ConfigureAwait(false);
            var list = new List<MovementPreview>(table.Rows.Count);

            static string? NullableString(DataRow row, string column)
                => row.Table.Columns.Contains(column) && row[column] != DBNull.Value
                    ? row[column]?.ToString()
                    : null;

            foreach (DataRow row in table.Rows)
            {
                token.ThrowIfCancellationRequested();

                var timestamp = SafeDateTime(row, "transaction_date") ?? DateTime.UtcNow;
                var transactionType = SafeString(row, "transaction_type", "unknown");
                if (string.IsNullOrWhiteSpace(transactionType))
                {
                    transactionType = "unknown";
                }

                int quantity = SafeInt(row, "quantity") ?? 0;
                string? relatedDocument = NullableString(row, "related_document");
                if (string.IsNullOrWhiteSpace(relatedDocument))
                {
                    relatedDocument = null;
                }

                string? note = NullableString(row, "note");
                if (string.IsNullOrWhiteSpace(note))
                {
                    note = null;
                }

                int? performedById = SafeInt(row, "performed_by_id");

                list.Add(new MovementPreview(timestamp, transactionType, quantity, relatedDocument, note, performedById));
            }

            token.ThrowIfCancellationRequested();

            try
            {
                var details = $"warehouse={(warehouseId?.ToString() ?? "*")}; part={(normalizedPartId?.ToString() ?? "*")}; take={take}; count={list.Count}";
                await _dbService.LogSystemEventAsync(
                    _authService.CurrentUser?.Id,
                    "WAREHOUSE_HISTORY_PREVIEW",
                    "inventory_transactions",
                    "Warehouse",
                    warehouseId,
                    details,
                    _currentIpAddress,
                    "info",
                    _currentDeviceInfo,
                    _currentSessionId,
                    token: token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Telemetry nije kritičan – ignoriraj greške prilikom logiranja.
            }

            return list;
        }

        private async Task LoadResponsibleUsersAsync()
        {
            try
            {
                var users = await _dbService.GetAllUsersBasicAsync().ConfigureAwait(false);
                ResponsibleUsers = new ObservableCollection<User>((users ?? new List<User>())
                    .OrderBy(u => u.FullName, StringComparer.CurrentCultureIgnoreCase));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Učitavanje korisnika nije uspjelo: {ex.Message}";
            }
        }

        private async Task LoadWarehouseListAsync(int? preferredSelectionId = null)
        {
            int? previousId = preferredSelectionId ?? SelectedWarehouse?.Id;

            const string sql = @"SELECT
    w.id,
    w.name,
    COALESCE(w.location, '')       AS location,
    w.responsible_id,
    COALESCE(w.status, '')         AS status,
    COALESCE(w.note, '')           AS note,
    w.last_modified,
    w.last_modified_by_id
FROM warehouses w
ORDER BY w.name, w.id";

            var table = await _dbService.ExecuteSelectAsync(sql).ConfigureAwait(false);
            var list = new List<Warehouse>(table.Rows.Count);

            foreach (DataRow row in table.Rows)
            {
                int id = SafeInt(row, "id") ?? 0;
                int? responsibleId = SafeInt(row, "responsible_id");
                var responsible = responsibleId.HasValue && responsibleId.Value > 0
                    ? ResponsibleUsers.FirstOrDefault(u => u.Id == responsibleId.Value)
                    : null;

                var warehouse = new Warehouse
                {
                    Id = id,
                    Name = SafeString(row, "name"),
                    Location = SafeString(row, "location"),
                    ResponsibleId = responsibleId ?? 0,
                    Responsible = responsible,
                    Status = SafeString(row, "status"),
                    Note = SafeString(row, "note"),
                    LastModified = SafeDateTime(row, "last_modified"),
                    LastModifiedById = SafeInt(row, "last_modified_by_id"),
                };

                list.Add(warehouse);
            }

            Warehouses = new ObservableCollection<Warehouse>(list
                .OrderBy(w => w.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(w => w.Id));

            if (Warehouses.Count == 0)
            {
                SelectedWarehouse = null;
                return;
            }

            var match = previousId.HasValue ? Warehouses.FirstOrDefault(w => w.Id == previousId.Value) : null;
            SelectedWarehouse = match ?? Warehouses.First();
        }

        private async Task LoadWarehousePartsInternalAsync()
        {
            const string sql = @"SELECT
    sl.id                       AS stock_id,
    sl.part_id,
    p.code                      AS part_code,
    p.name                      AS part_name,
    p.min_stock_alert           AS part_min_stock_alert,
    sl.warehouse_id,
    COALESCE(w.name, CONCAT('WH-', sl.warehouse_id)) AS warehouse_name,
    w.location                  AS warehouse_location,
    w.status                    AS warehouse_status,
    w.note                      AS warehouse_note,
    w.responsible_id,
    u.full_name                 AS responsible_name,
    sl.quantity,
    sl.min_threshold,
    sl.max_threshold,
    sl.auto_reorder_triggered,
    sl.days_below_min,
    sl.alarm_status,
    sl.anomaly_score,
    sl.last_modified
FROM stock_levels sl
LEFT JOIN parts p      ON p.id = sl.part_id
LEFT JOIN warehouses w ON w.id = sl.warehouse_id
LEFT JOIN users u      ON u.id = w.responsible_id
ORDER BY w.name, p.name, p.code, sl.id";

            var table = await _dbService.ExecuteSelectAsync(sql).ConfigureAwait(false);

            var warehouseMap = Warehouses.ToDictionary(w => w.Id);
            var partRows = new List<WarehousePartRow>(table.Rows.Count);
            var stockRows = new List<StockLevel>(table.Rows.Count);

            foreach (DataRow row in table.Rows)
            {
                int warehouseId = SafeInt(row, "warehouse_id") ?? 0;
                int partId = SafeInt(row, "part_id") ?? 0;

                warehouseMap.TryGetValue(warehouseId, out var warehouse);
                if (warehouse == null)
                {
                    warehouse = new Warehouse
                    {
                        Id = warehouseId,
                        Name = SafeString(row, "warehouse_name"),
                        Location = SafeString(row, "warehouse_location"),
                        Status = SafeString(row, "warehouse_status"),
                        Note = SafeString(row, "warehouse_note"),
                    };

                    int? responsibleId = SafeInt(row, "responsible_id");
                    if (responsibleId.HasValue && responsibleId.Value > 0)
                    {
                        warehouse.ResponsibleId = responsibleId.Value;
                        warehouse.Responsible = ResponsibleUsers.FirstOrDefault(u => u.Id == responsibleId.Value)
                            ?? new User
                            {
                                Id = responsibleId.Value,
                                FullName = SafeString(row, "responsible_name"),
                                Username = SafeString(row, "responsible_name"),
                            };
                    }

                    warehouseMap[warehouseId] = warehouse;
                }
                else
                {
                    int? responsibleId = SafeInt(row, "responsible_id");
                    if (responsibleId.HasValue && responsibleId.Value > 0)
                    {
                        warehouse.ResponsibleId = responsibleId.Value;
                        warehouse.Responsible = ResponsibleUsers.FirstOrDefault(u => u.Id == responsibleId.Value)
                            ?? warehouse.Responsible
                            ?? new User
                            {
                                Id = responsibleId.Value,
                                FullName = SafeString(row, "responsible_name"),
                                Username = SafeString(row, "responsible_name"),
                            };
                    }

                    if (string.IsNullOrWhiteSpace(warehouse.Status))
                    {
                        var status = SafeString(row, "warehouse_status");
                        if (!string.IsNullOrWhiteSpace(status)) warehouse.Status = status;
                    }

                    if (string.IsNullOrWhiteSpace(warehouse.Note))
                    {
                        var note = SafeString(row, "warehouse_note");
                        if (!string.IsNullOrWhiteSpace(note)) warehouse.Note = note;
                    }
                }

                var part = new Part
                {
                    Id = partId,
                    Code = SafeString(row, "part_code"),
                    Name = SafeString(row, "part_name"),
                    MinStockAlert = SafeInt(row, "part_min_stock_alert"),
                };

                var stock = new StockLevel
                {
                    Id = SafeInt(row, "stock_id") ?? 0,
                    PartId = partId,
                    Part = part,
                    WarehouseId = warehouseId,
                    Warehouse = warehouse,
                    Quantity = SafeInt(row, "quantity") ?? 0,
                    MinThreshold = SafeInt(row, "min_threshold") ?? 0,
                    MaxThreshold = SafeInt(row, "max_threshold") ?? 0,
                    AutoReorderTriggered = SafeBool(row, "auto_reorder_triggered") ?? false,
                    DaysBelowMin = SafeInt(row, "days_below_min") ?? 0,
                    AlarmStatus = SafeString(row, "alarm_status", "none"),
                    AnomalyScore = SafeDouble(row, "anomaly_score"),
                    LastModified = SafeDateTime(row, "last_modified") ?? DateTime.UtcNow,
                };

                stockRows.Add(stock);
                partRows.Add(new WarehousePartRow(stock, part));
            }

            StockLevels = new ObservableCollection<StockLevel>(stockRows);
            _allPartRows.Clear();
            _allPartRows.AddRange(partRows);
            TotalLowStockCount = _allPartRows.Count(r => r.IsLowStock);
            ApplyPartFilters();
        }

        private async Task UpdateWarehouseMetadataAsync(int warehouseId, string name, string? location, int? responsibleId, string? status, string? note, CancellationToken token = default)
        {
            var parameters = new List<MySqlParameter>
            {
                new("@id", warehouseId),
                new("@name", name),
                new("@location", (object?)location ?? DBNull.Value),
                new("@responsible", (object?)responsibleId ?? DBNull.Value),
                new("@status", (object?)status ?? DBNull.Value),
                new("@note", (object?)note ?? DBNull.Value),
                new("@actor", _authService.CurrentUser?.Id ?? 0),
            };

            const string sql = @"UPDATE warehouses
SET name = @name,
    location = @location,
    responsible_id = @responsible,
    status = @status,
    note = @note,
    last_modified = UTC_TIMESTAMP(),
    last_modified_by_id = @actor
WHERE id = @id";

            await _dbService.ExecuteNonQueryAsync(sql, parameters, token).ConfigureAwait(false);
        }

        private void PopulateEditorFromWarehouse(Warehouse? warehouse)
        {
            if (warehouse is null)
            {
                EditName = string.Empty;
                EditLocation = string.Empty;
                EditStatus = string.Empty;
                EditNote = string.Empty;
                SelectedResponsibleUser = null;
                return;
            }

            EditName = warehouse.Name ?? string.Empty;
            EditLocation = warehouse.Location ?? string.Empty;
            EditStatus = warehouse.Status ?? string.Empty;
            EditNote = warehouse.Note ?? string.Empty;
            if (warehouse.ResponsibleId > 0)
            {
                SelectedResponsibleUser = ResponsibleUsers.FirstOrDefault(u => u.Id == warehouse.ResponsibleId)
                    ?? warehouse.Responsible;
            }
            else
            {
                SelectedResponsibleUser = null;
            }
        }

        private void ApplyPartFilters()
        {
            IEnumerable<WarehousePartRow> query = _allPartRows;

            if (SelectedWarehouse is not null)
            {
                query = query.Where(r => r.Warehouse?.Id == SelectedWarehouse.Id);
            }

            if (!string.IsNullOrWhiteSpace(PartSearchTerm))
            {
                var term = PartSearchTerm.Trim();
                query = query.Where(r => r.MatchesSearch(term));
            }

            if (ShowOnlyLowStock)
            {
                query = query.Where(r => r.IsLowStock);
            }

            var result = query
                .OrderBy(r => r.WarehouseName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(r => r.PartName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(r => r.PartCode, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            FilteredParts = new ObservableCollection<WarehousePartRow>(result);
            LowStockHighlights = new ObservableCollection<WarehousePartRow>(result.Where(r => r.IsLowStock));

            var currentSelection = SelectedPart;
            var desiredSelection = currentSelection is not null && result.Contains(currentSelection)
                ? currentSelection
                : result.FirstOrDefault();

            if (!EqualityComparer<WarehousePartRow>.Default.Equals(currentSelection, desiredSelection))
            {
                SelectedPart = desiredSelection;
            }
            else
            {
                QueueMovementHistoryRefresh();
            }
        }

        private void QueueMovementHistoryRefresh()
        {
            var cts = new CancellationTokenSource();
            var previous = Interlocked.Exchange(ref _movementHistoryRefreshCts, cts);
            if (previous is not null)
            {
                try
                {
                    previous.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Već zbrisan.
                }
            }

            IsMovementHistoryLoading = true;
            MovementHistoryStatus = "Učitavanje povijesti...";
            _ = RefreshMovementHistoryPreviewInternalAsync(cts);
        }

        private Task RefreshMovementHistoryPreviewAsync()
        {
            QueueMovementHistoryRefresh();
            return Task.CompletedTask;
        }

        private async Task RefreshMovementHistoryPreviewInternalAsync(CancellationTokenSource cts)
        {
            try
            {
                var previews = await LoadMovementHistoryPreviewAsync(SelectedWarehouse, SelectedPart?.Part?.Id, 25, cts.Token).ConfigureAwait(false);
                if (cts.IsCancellationRequested)
                {
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MovementHistory = new ObservableCollection<MovementPreview>(previews);
                    MovementHistoryStatus = MovementHistory.Count == 0
                        ? "Nema nedavnih transakcija za odabrani filter."
                        : $"Prikazano {MovementHistory.Count} zadnjih transakcija.";
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Ignored – novi zahtjev je pokrenut.
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    MovementHistory = new ObservableCollection<MovementPreview>();
                    MovementHistoryStatus = $"Povijest nije dostupna: {ex.Message}";
                }).ConfigureAwait(false);
            }
            finally
            {
                if (ReferenceEquals(_movementHistoryRefreshCts, cts))
                {
                    _movementHistoryRefreshCts = null;
                    IsMovementHistoryLoading = false;
                }

                cts.Dispose();
            }
        }

        private async Task EnsureWarehouseSchemaAsync(CancellationToken token = default)
        {
            if (_schemaEnsured)
            {
                return;
            }

            const string ensureCore = @"CREATE TABLE IF NOT EXISTS warehouses (
  id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL,
  location VARCHAR(255) NULL
);";

            await _dbService.ExecuteNonQueryAsync(ensureCore, null, token).ConfigureAwait(false);

            var alterations = new[]
            {
                "ALTER TABLE warehouses ADD COLUMN IF NOT EXISTS responsible_id INT NULL",
                "ALTER TABLE warehouses ADD COLUMN IF NOT EXISTS note VARCHAR(500) NULL",
                "ALTER TABLE warehouses ADD COLUMN IF NOT EXISTS status VARCHAR(30) NULL",
                "ALTER TABLE warehouses ADD COLUMN IF NOT EXISTS last_modified DATETIME NULL",
                "ALTER TABLE warehouses ADD COLUMN IF NOT EXISTS last_modified_by_id INT NULL"
            };

            foreach (var sql in alterations)
            {
                try
                {
                    await _dbService.ExecuteNonQueryAsync(sql, null, token).ConfigureAwait(false);
                }
                catch (Exception ex) when (IsIgnorableSchemaException(ex))
                {
                    // Ignore when column already exists or privilege restrictions.
                }
            }

            _schemaEnsured = true;
        }

        private static bool IsIgnorableSchemaException(Exception ex)
            => ex is MySqlException mysql && (mysql.Number == 1060 || mysql.Number == 1091);

        private static int? SafeInt(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? Convert.ToInt32(row[column]) : (int?)null;

        private static bool? SafeBool(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? Convert.ToBoolean(row[column]) : (bool?)null;

        private static double? SafeDouble(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? Convert.ToDouble(row[column]) : (double?)null;

        private static DateTime? SafeDateTime(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? Convert.ToDateTime(row[column]) : (DateTime?)null;

        private static string SafeString(DataRow row, string column, string fallback = "")
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? row[column]?.ToString() ?? fallback : fallback;

        private bool CanAddWarehouse() => !IsBusy && !string.IsNullOrWhiteSpace(EditName);

        private bool CanUpdateWarehouse() => !IsBusy && SelectedWarehouse is not null && !string.IsNullOrWhiteSpace(EditName);

        private void UpdateCommandStates()
        {
            (LoadCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (RefreshStockCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (AddWarehouseCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (UpdateWarehouseCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            (RefreshMovementHistoryCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
        }

        /// <summary>Helper za PropertyChanged.</summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>Raise PropertyChanged event.</summary>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Kombinirani prikaz skladišta i artikla s pragovima i indikatorima.
        /// </summary>
        public sealed class WarehousePartRow
        {
            /// <summary>Initializes a new instance of the <see cref="WarehousePartRow"/> class.</summary>
            public WarehousePartRow(StockLevel stockLevel, Part part)
            {
                Stock = stockLevel ?? throw new ArgumentNullException(nameof(stockLevel));
                Part = part ?? throw new ArgumentNullException(nameof(part));
                Warehouse = Stock.Warehouse ?? new Warehouse { Id = Stock.WarehouseId };
                Stock.Warehouse = Warehouse;
            }

            /// <summary>Underlying stock level.</summary>
            public StockLevel Stock { get; }

            /// <summary>Part informacije.</summary>
            public Part Part { get; }

            /// <summary>Skladište.</summary>
            public Warehouse Warehouse { get; }

            /// <summary>Naziv skladišta.</summary>
            public string WarehouseName => Warehouse?.Name ?? string.Empty;

            /// <summary>Lokacija skladišta.</summary>
            public string WarehouseLocation => Warehouse?.Location ?? string.Empty;

            /// <summary>Odgovorna osoba.</summary>
            public string ResponsibleName => Warehouse?.Responsible?.FullName ?? string.Empty;

            /// <summary>Naziv artikla.</summary>
            public string PartName => Part?.Name ?? string.Empty;

            /// <summary>Šifra artikla.</summary>
            public string PartCode => Part?.Code ?? string.Empty;

            /// <summary>Količina u skladištu.</summary>
            public int Quantity => Stock.Quantity;

            /// <summary>Minimalni prag u skladištu.</summary>
            public int WarehouseMin => Stock.MinThreshold;

            /// <summary>Maksimalni prag u skladištu.</summary>
            public int WarehouseMax => Stock.MaxThreshold;

            /// <summary>Globalni prag s artikla.</summary>
            public int? PartAlert => Part?.MinStockAlert;

            /// <summary>Indikator za niski stock prema skladištu.</summary>
            public bool IsBelowWarehouseThreshold => WarehouseMin > 0 && Quantity < WarehouseMin;

            /// <summary>Indikator za niski stock prema artiklu.</summary>
            public bool IsBelowPartThreshold => PartAlert.HasValue && Quantity < PartAlert.Value;

            /// <summary>True ako je bilo koji prag prekršen.</summary>
            public bool IsLowStock => IsBelowWarehouseThreshold || IsBelowPartThreshold;

            /// <summary>Heuristika ozbiljnosti.</summary>
            public string Severity => IsBelowWarehouseThreshold ? "critical" : IsBelowPartThreshold ? "warning" : "ok";

            /// <summary>Provjera matcha na brzu pretragu.</summary>
            public bool MatchesSearch(string term)
            {
                if (string.IsNullOrWhiteSpace(term)) return true;

                return (PartName?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                    || (PartCode?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                    || (WarehouseName?.IndexOf(term, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
            }

            /// <inheritdoc />
            public override string ToString()
                => $"{PartName} ({PartCode}) – {Quantity} kom u {WarehouseName}";
        }

        /// <summary>DTO za budući modul praćenja povijesti ulaza/izlaza.</summary>
        public sealed record MovementPreview(DateTime Timestamp, string TransactionType, int Quantity, string? RelatedDocument, string? Note, int? PerformedById);
    }
}
