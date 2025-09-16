using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>PartsStockViewModel</b> – Centralised stock overview for all spare parts across warehouses.
    /// <para>
    /// Provides quick-search by part name/code, warehouse filtering, threshold colour indicators and
    /// scaffolding for future replenishment/order orchestration.
    /// </para>
    /// </summary>
    public class PartsStockViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private readonly List<PartStockDisplay> _allStock = new();

        private ObservableCollection<Part> _parts = new();
        private ObservableCollection<Warehouse> _warehouses = new();
        private ObservableCollection<StockLevel> _stockLevels = new();
        private ObservableCollection<PartStockDisplay> _filteredStock = new();

        private Warehouse? _selectedWarehouse;
        private string? _searchTerm;
        private bool _isBusy;
        private string _statusMessage = string.Empty;

        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartsStockViewModel"/> class.
        /// </summary>
        /// <param name="dbService">Database access service.</param>
        /// <param name="authService">Authentication context (current user, session, forensic info).</param>
        /// <exception cref="ArgumentNullException">Thrown when dependencies are <c>null</c>.</exception>
        public PartsStockViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId = _authService.CurrentSessionId ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? string.Empty;
            _currentIpAddress = _authService.CurrentIpAddress ?? string.Empty;

            LoadStockCommand = new AsyncRelayCommand(LoadStockAsync, () => !IsBusy);
            RequestReplenishmentCommand = new AsyncRelayCommand<PartStockDisplay>(RequestReplenishmentAsync);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            FilterChangedCommand = new RelayCommand(ApplyFilters);

            _ = LoadStockAsync();
        }

        /// <summary>
        /// Raised when properties change (required by <see cref="INotifyPropertyChanged"/>).
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets or sets the cached list of parts (distinct across all warehouses).
        /// </summary>
        public ObservableCollection<Part> Parts
        {
            get => _parts;
            private set => SetProperty(ref _parts, value ?? new ObservableCollection<Part>());
        }

        /// <summary>
        /// Gets or sets the list of warehouses available for filtering.
        /// </summary>
        public ObservableCollection<Warehouse> Warehouses
        {
            get => _warehouses;
            private set => SetProperty(ref _warehouses, value ?? new ObservableCollection<Warehouse>());
        }

        /// <summary>
        /// Gets or sets the raw stock level entries retrieved from the database.
        /// </summary>
        public ObservableCollection<StockLevel> StockLevels
        {
            get => _stockLevels;
            private set => SetProperty(ref _stockLevels, value ?? new ObservableCollection<StockLevel>());
        }

        /// <summary>
        /// Gets or sets the filtered view used for UI binding (includes threshold metadata).
        /// </summary>
        public ObservableCollection<PartStockDisplay> FilteredStock
        {
            get => _filteredStock;
            private set
            {
                if (SetProperty(ref _filteredStock, value ?? new ObservableCollection<PartStockDisplay>()))
                {
                    OnPropertyChanged(nameof(ResultCount));
                }
            }
        }

        /// <summary>
        /// Gets the number of rows currently displayed after filters are applied.
        /// </summary>
        public int ResultCount => FilteredStock.Count;

        /// <summary>
        /// Gets or sets the currently selected warehouse for filtering (nullable).
        /// </summary>
        public Warehouse? SelectedWarehouse
        {
            get => _selectedWarehouse;
            set
            {
                if (SetProperty(ref _selectedWarehouse, value))
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>
        /// Gets or sets the quick search term (filters part name and code).
        /// </summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (SetProperty(ref _searchTerm, value))
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the view model is executing a database operation.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    (LoadStockCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the latest status message (info/error) for UI display.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value ?? string.Empty);
        }

        /// <summary>
        /// Gets a queue of replenishment requests staged locally when no integration handler is attached.
        /// </summary>
        public ObservableCollection<ReplenishmentRequest> PendingReplenishmentQueue { get; } = new();

        /// <summary>
        /// Gets or sets the asynchronous handler invoked when the user requests replenishment.
        /// </summary>
        public Func<ReplenishmentRequest, Task>? ReplenishmentRequestHandler { get; set; }

        /// <summary>
        /// Raised whenever a replenishment request is constructed (regardless of handler availability).
        /// </summary>
        public event EventHandler<ReplenishmentRequest>? ReplenishmentRequestCreated;

        /// <summary>
        /// Command that loads (or reloads) stock data from the database.
        /// </summary>
        public IAsyncRelayCommand LoadStockCommand { get; }

        /// <summary>
        /// Command invoked by UI actions to ask for replenishment of a selected stock row.
        /// </summary>
        public IAsyncRelayCommand<PartStockDisplay> RequestReplenishmentCommand { get; }

        /// <summary>
        /// Command that resets search/warehouse filters.
        /// </summary>
        public ICommand ClearFiltersCommand { get; }

        /// <summary>
        /// Command that forces re-application of current filters (useful for UI events).
        /// </summary>
        public ICommand FilterChangedCommand { get; }

        /// <summary>
        /// Loads the stock overview with joins against parts and warehouses.
        /// </summary>
        public async Task LoadStockAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            try
            {
                StatusMessage = "Loading stock levels...";

                const string sql = @"SELECT
    sl.id,
    sl.part_id,
    p.code            AS part_code,
    p.name            AS part_name,
    p.min_stock_alert AS part_min_stock_alert,
    sl.warehouse_id,
    COALESCE(w.name, CONCAT('WH-', sl.warehouse_id)) AS warehouse_name,
    w.location        AS warehouse_location,
    sl.quantity,
    sl.min_threshold,
    sl.max_threshold,
    sl.auto_reorder_triggered,
    sl.days_below_min,
    sl.alarm_status,
    sl.anomaly_score,
    sl.last_modified
FROM stock_levels sl
LEFT JOIN parts p ON p.id = sl.part_id
LEFT JOIN warehouses w ON w.id = sl.warehouse_id
ORDER BY p.name, p.code, w.name, sl.id";

                var table = await _dbService.ExecuteSelectAsync(sql).ConfigureAwait(false);
                var warehouses = await _dbService.GetWarehousesAsync().ConfigureAwait(false);

                var warehouseMap = warehouses?.ToDictionary(w => w.Id) ?? new Dictionary<int, Warehouse>();
                var partsMap = new Dictionary<int, Part>();
                var stockLevels = new List<StockLevel>(table.Rows.Count);
                var displays = new List<PartStockDisplay>(table.Rows.Count);

                foreach (DataRow row in table.Rows)
                {
                    int partId = SafeInt(row, "part_id") ?? 0;
                    int warehouseId = SafeInt(row, "warehouse_id") ?? 0;
                    int quantity = SafeInt(row, "quantity") ?? 0;
                    int? minThreshold = SafeInt(row, "min_threshold");
                    int? maxThreshold = SafeInt(row, "max_threshold");

                    string partCode = SafeString(row, "part_code");
                    string partName = SafeString(row, "part_name");
                    int? partMinAlert = SafeInt(row, "part_min_stock_alert");

                    if (!partsMap.TryGetValue(partId, out var part))
                    {
                        part = new Part
                        {
                            Id = partId,
                            Code = partCode,
                            Name = partName,
                            MinStockAlert = partMinAlert,
                            Stock = 0,
                        };
                        partsMap[partId] = part;
                    }

                    part.Stock += quantity;

                    string warehouseName = SafeString(row, "warehouse_name", warehouseId == 0 ? "" : $"WH-{warehouseId}");
                    string warehouseLocation = SafeString(row, "warehouse_location");

                    if (!warehouseMap.TryGetValue(warehouseId, out var warehouse))
                    {
                        warehouse = new Warehouse
                        {
                            Id = warehouseId,
                            Name = warehouseName,
                            Location = warehouseLocation,
                        };
                        warehouseMap[warehouseId] = warehouse;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(warehouseName) && string.IsNullOrWhiteSpace(warehouse.Name))
                        {
                            warehouse.Name = warehouseName;
                        }
                        if (!string.IsNullOrWhiteSpace(warehouseLocation) && string.IsNullOrWhiteSpace(warehouse.Location))
                        {
                            warehouse.Location = warehouseLocation;
                        }
                    }

                    var stockLevel = new StockLevel
                    {
                        Id = SafeInt(row, "id") ?? 0,
                        PartId = partId,
                        WarehouseId = warehouseId,
                        Quantity = quantity,
                        MinThreshold = minThreshold ?? 0,
                        MaxThreshold = maxThreshold ?? 0,
                        AutoReorderTriggered = SafeBool(row, "auto_reorder_triggered") ?? false,
                        DaysBelowMin = SafeInt(row, "days_below_min") ?? 0,
                        AlarmStatus = SafeString(row, "alarm_status", "none"),
                        AnomalyScore = SafeDouble(row, "anomaly_score"),
                        LastModified = SafeDateTime(row, "last_modified") ?? DateTime.UtcNow,
                        Part = part,
                        Warehouse = warehouse,
                    };

                    stockLevels.Add(stockLevel);
                    displays.Add(new PartStockDisplay(stockLevel, minThreshold, maxThreshold));
                }

                Parts = new ObservableCollection<Part>(partsMap.Values
                    .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(p => p.Code, StringComparer.CurrentCultureIgnoreCase));

                Warehouses = new ObservableCollection<Warehouse>(warehouseMap.Values
                    .OrderBy(w => w.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(w => w.Id));

                StockLevels = new ObservableCollection<StockLevel>(stockLevels);

                _allStock.Clear();
                _allStock.AddRange(displays);

                ApplyFilters();

                StatusMessage = $"Loaded {_allStock.Count} stock rows across {Parts.Count} parts.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading stock levels: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Clears current filters and reapplies them (showing all stock rows).
        /// </summary>
        public void ClearFilters()
        {
            bool changed = false;

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                SearchTerm = null;
                changed = true;
            }

            if (SelectedWarehouse != null)
            {
                SelectedWarehouse = null;
                changed = true;
            }

            if (!changed)
            {
                ApplyFilters();
            }
        }

        /// <summary>
        /// Applies the search and warehouse filters to the in-memory cache.
        /// </summary>
        public void ApplyFilters()
        {
            if (_allStock.Count == 0)
            {
                FilteredStock = new ObservableCollection<PartStockDisplay>();
                StatusMessage = "No stock data available.";
                return;
            }

            IEnumerable<PartStockDisplay> query = _allStock;

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                string term = SearchTerm.Trim();
                query = query.Where(item => item.Matches(term));
            }

            if (SelectedWarehouse != null)
            {
                query = query.Where(item => item.Stock.WarehouseId == SelectedWarehouse.Id);
            }

            var filtered = query
                .OrderBy(item => item.PartName, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(item => item.PartCode, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(item => item.WarehouseName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            FilteredStock = new ObservableCollection<PartStockDisplay>(filtered);

            StatusMessage = filtered.Count == _allStock.Count
                ? $"Showing all {filtered.Count} stock rows."
                : $"Showing {filtered.Count} of {_allStock.Count} stock rows.";
        }

        /// <summary>
        /// Builds and forwards a replenishment request for the provided stock row.
        /// </summary>
        /// <param name="stock">Selected stock row (may be <c>null</c>).</param>
        private async Task RequestReplenishmentAsync(PartStockDisplay? stock)
        {
            if (stock == null)
            {
                StatusMessage = "No stock row selected for replenishment.";
                return;
            }

            var request = new ReplenishmentRequest(
                stock.Stock.PartId,
                stock.PartCode,
                stock.PartName,
                stock.Stock.WarehouseId,
                stock.WarehouseName,
                stock.Quantity,
                stock.MinThreshold,
                stock.MaxThreshold,
                stock.SuggestedReorderQuantity,
                stock.ShouldProposeReplenishment,
                _currentSessionId,
                _currentDeviceInfo,
                _currentIpAddress);

            UpsertPendingRequest(request);
            ReplenishmentRequestCreated?.Invoke(this, request);

            if (ReplenishmentRequestHandler != null)
            {
                try
                {
                    await ReplenishmentRequestHandler(request).ConfigureAwait(false);
                    PendingReplenishmentQueue.Remove(request);
                    StatusMessage = $"Replenishment request forwarded for {stock.PartName} ({stock.WarehouseName}).";
                    return;
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Replenishment hand-off failed: {ex.Message}. Request kept in local queue.";
                }
            }
            else
            {
                StatusMessage = $"Replenishment request staged locally for {stock.PartName} ({stock.WarehouseName}).";
            }
        }

        private void UpsertPendingRequest(ReplenishmentRequest request)
        {
            var existing = PendingReplenishmentQueue
                .FirstOrDefault(r => r.PartId == request.PartId && r.WarehouseId == request.WarehouseId);

            if (existing != null)
            {
                int index = PendingReplenishmentQueue.IndexOf(existing);
                if (index >= 0)
                {
                    PendingReplenishmentQueue[index] = request;
                }
            }
            else
            {
                PendingReplenishmentQueue.Add(request);
            }
        }

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

        /// <summary>
        /// Utility setter with <see cref="INotifyPropertyChanged"/> semantics.
        /// </summary>
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

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Combines a <see cref="StockLevel"/> with threshold metadata and display helpers.
        /// </summary>
        public sealed class PartStockDisplay
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PartStockDisplay"/> class.
            /// </summary>
            /// <param name="stockLevel">Underlying stock level.</param>
            /// <param name="minThreshold">Minimum threshold (nullable).</param>
            /// <param name="maxThreshold">Maximum threshold (nullable).</param>
            public PartStockDisplay(StockLevel stockLevel, int? minThreshold, int? maxThreshold)
            {
                Stock = stockLevel ?? throw new ArgumentNullException(nameof(stockLevel));
                MinThreshold = minThreshold;
                MaxThreshold = maxThreshold;
            }

            /// <summary>Gets the underlying stock level entity.</summary>
            public StockLevel Stock { get; }

            /// <summary>Gets the part associated with this stock row.</summary>
            public Part Part => Stock.Part;

            /// <summary>Gets the warehouse associated with this stock row.</summary>
            public Warehouse Warehouse => Stock.Warehouse;

            /// <summary>Gets the quantity currently available in the warehouse.</summary>
            public int Quantity => Stock.Quantity;

            /// <summary>Gets the nullable minimum threshold used for alerts.</summary>
            public int? MinThreshold { get; }

            /// <summary>Gets the nullable maximum threshold used for alerts.</summary>
            public int? MaxThreshold { get; }

            /// <summary>Gets the part name (safe for null parts).</summary>
            public string PartName => Part?.Name ?? string.Empty;

            /// <summary>Gets the part code (safe for null parts).</summary>
            public string PartCode => Part?.Code ?? string.Empty;

            /// <summary>Gets the warehouse display name.</summary>
            public string WarehouseName => Warehouse?.Name ?? $"WH-{Stock.WarehouseId}";

            /// <summary>Gets a value indicating whether the stock is below the minimum threshold.</summary>
            public bool IsBelowMin => MinThreshold.HasValue && Quantity < MinThreshold.Value;

            /// <summary>Gets a value indicating whether the stock is near the minimum threshold (within 10%).</summary>
            public bool IsNearMin
            {
                get
                {
                    if (!MinThreshold.HasValue || IsBelowMin)
                    {
                        return false;
                    }

                    int buffer = Math.Max(1, (int)Math.Round(MinThreshold.Value * 0.1, MidpointRounding.AwayFromZero));
                    return Quantity <= MinThreshold.Value + buffer;
                }
            }

            /// <summary>Gets a value indicating whether the stock exceeds the maximum threshold.</summary>
            public bool IsAboveMax => MaxThreshold.HasValue && Quantity > MaxThreshold.Value;

            /// <summary>Gets a display state describing the threshold condition.</summary>
            public string ThresholdState
            {
                get
                {
                    if (Stock.AutoReorderTriggered)
                    {
                        return "auto_reorder";
                    }

                    if (IsBelowMin)
                    {
                        return "below_min";
                    }

                    if (IsNearMin)
                    {
                        return "near_min";
                    }

                    if (IsAboveMax)
                    {
                        return "above_max";
                    }

                    if (MinThreshold.HasValue || MaxThreshold.HasValue)
                    {
                        return "optimal";
                    }

                    return "no_threshold";
                }
            }

            /// <summary>Gets the recommended colour (HEX) representing the threshold state.</summary>
            public string ThresholdColor => ResolveThresholdColor(ThresholdState, Stock.AlarmStatus);

            /// <summary>Gets a value indicating whether the row should propose replenishment.</summary>
            public bool ShouldProposeReplenishment => ThresholdState is "below_min" or "near_min" || Stock.AutoReorderTriggered;

            /// <summary>Gets the suggested quantity to order to return to the minimum threshold (if defined).</summary>
            public int SuggestedReorderQuantity
            {
                get
                {
                    if (!MinThreshold.HasValue)
                    {
                        return 0;
                    }

                    if (Quantity >= MinThreshold.Value)
                    {
                        return 0;
                    }

                    return Math.Max(MinThreshold.Value - Quantity, 1);
                }
            }

            /// <summary>
            /// Determines whether the stock row matches the provided search term (part name/code).
            /// </summary>
            /// <param name="term">Search term to compare.</param>
            /// <returns>True when it matches, false otherwise.</returns>
            public bool Matches(string term)
            {
                if (string.IsNullOrWhiteSpace(term))
                {
                    return true;
                }

                return PartName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || PartCode.Contains(term, StringComparison.OrdinalIgnoreCase);
            }

            private static string ResolveThresholdColor(string state, string alarmStatus)
            {
                if (string.Equals(alarmStatus, "pending_approval", StringComparison.OrdinalIgnoreCase))
                {
                    return "#6D4C41"; // brown – awaiting manual approval
                }

                return state switch
                {
                    "auto_reorder" => "#6A1B9A", // purple
                    "below_min" => "#C62828",    // red
                    "near_min" => "#EF6C00",     // orange
                    "above_max" => "#1565C0",    // blue
                    "optimal" => "#2E7D32",      // green
                    _ => "#546E7A",               // grey (no thresholds)
                };
            }

            /// <inheritdoc />
            public override string ToString()
                => $"{PartCode} – {PartName} @ {WarehouseName}: {Quantity}";
        }

        /// <summary>
        /// DTO describing an order-replenishment hook request.
        /// </summary>
        /// <param name="PartId">Part identifier.</param>
        /// <param name="PartCode">Part code.</param>
        /// <param name="PartName">Part name.</param>
        /// <param name="WarehouseId">Warehouse identifier.</param>
        /// <param name="WarehouseName">Warehouse display name.</param>
        /// <param name="CurrentQuantity">Current quantity on hand.</param>
        /// <param name="MinThreshold">Minimum threshold (nullable).</param>
        /// <param name="MaxThreshold">Maximum threshold (nullable).</param>
        /// <param name="SuggestedQuantity">Suggested replenishment quantity.</param>
        /// <param name="IsCritical">Whether the stock is in a critical state.</param>
        /// <param name="SessionId">Current session identifier (audit context).</param>
        /// <param name="DeviceInfo">Device forensic info.</param>
        /// <param name="IpAddress">Source IP address.</param>
        public sealed record ReplenishmentRequest(
            int PartId,
            string PartCode,
            string PartName,
            int WarehouseId,
            string WarehouseName,
            int CurrentQuantity,
            int? MinThreshold,
            int? MaxThreshold,
            int SuggestedQuantity,
            bool IsCritical,
            string SessionId,
            string DeviceInfo,
            string IpAddress);
    }
}

