using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents the Part.
    /// </summary>
    public partial class Part
    {
        /// <summary>
        /// Executes the is blocked operation.
        /// </summary>
        [NotMapped]
        public bool IsBlocked => (Blocked ?? false) || (!string.IsNullOrWhiteSpace(Status) && Status!.IndexOf("block", StringComparison.OrdinalIgnoreCase) >= 0);

        /// <summary>
        /// Gets or sets the is expired.
        /// </summary>
        [NotMapped]
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the is stock critical.
        /// </summary>
        [NotMapped]
        public bool IsStockCritical => MinStockAlert.HasValue && Stock.HasValue && Stock.Value < MinStockAlert.Value;

        /// <summary>
        /// Gets or sets the main supplier name.
        /// </summary>
        [NotMapped]
        public string? MainSupplierName =>
            !string.IsNullOrWhiteSpace(DefaultSupplier?.Name) ? DefaultSupplier!.Name :
            SupplierPrices.FirstOrDefault()?.SupplierName ?? Supplier ?? DefaultSupplierName;

        /// <summary>
        /// Represents the attachments value.
        /// </summary>
        [NotMapped]
        public List<string> Attachments
        {
            get => Documents;
            set => Documents = value;
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(System.Runtime.Serialization.StreamingContext context)
        {
            SupplierPrices = SupplierPrices ?? new();
            WarehouseStocks = WarehouseStocks ?? new();
            StockHistory = StockHistory ?? new();
            StockLevels = StockLevels ?? new();
            Images = Images ?? new();
            Documents = Documents ?? new();
            ChangeLogs = ChangeLogs ?? new();
            WorkOrderParts = WorkOrderParts ?? new();
            Warehouses = Warehouses ?? new();
            SupplierList = SupplierList ?? new();
        }

        /// <summary>
        /// Gets or sets the warehouse summary.
        /// </summary>
        [NotMapped]
        public string WarehouseSummary { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the is warehouse stock critical.
        /// </summary>
        [NotMapped]
        public bool IsWarehouseStockCritical { get; set; }

        /// <summary>
        /// Gets or sets the low warehouse count.
        /// </summary>
        [NotMapped]
        public int LowWarehouseCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the linked work orders count.
        /// </summary>
        [NotMapped]
        public int LinkedWorkOrdersCount { get; set; } = 0;

        private List<string> Split(string? raw) => string.IsNullOrWhiteSpace(raw)
            ? new List<string>()
            : raw!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        private string? Join(IEnumerable<string> values)
            => values == null ? null : string.Join(',', values.Where(v => !string.IsNullOrWhiteSpace(v)));

        /// <summary>
        /// Represents the warehouse stock snapshots value.
        /// </summary>
        [NotMapped]
        public List<string> WarehouseStockSnapshots
        {
            get => Split(WarehouseStocksRaw);
            set => WarehouseStocksRaw = Join(value);
        }

        /// <summary>
        /// Represents the stock history snapshots value.
        /// </summary>
        [NotMapped]
        public List<string> StockHistorySnapshots
        {
            get => Split(StockHistoryRaw);
            set => StockHistoryRaw = Join(value);
        }

        /// <summary>
        /// Represents the image paths value.
        /// </summary>
        [NotMapped]
        public List<string> ImagePaths
        {
            get => Split(ImagesRaw);
            set => ImagesRaw = Join(value);
        }

        /// <summary>
        /// Represents the document paths value.
        /// </summary>
        [NotMapped]
        public List<string> DocumentPaths
        {
            get => Split(DocumentsRaw);
            set => DocumentsRaw = Join(value);
        }

        /// <summary>
        /// Represents the change log snapshots value.
        /// </summary>
        [NotMapped]
        public List<string> ChangeLogSnapshots
        {
            get => Split(ChangeLogsRaw);
            set => ChangeLogsRaw = Join(value);
        }

        /// <summary>
        /// Represents the work order part snapshots value.
        /// </summary>
        [NotMapped]
        public List<string> WorkOrderPartSnapshots
        {
            get => Split(WorkOrderPartsRaw);
            set => WorkOrderPartsRaw = Join(value);
        }

        /// <summary>
        /// Represents the warehouse snapshots value.
        /// </summary>
        [NotMapped]
        public List<string> WarehouseSnapshots
        {
            get => Split(WarehousesRaw);
            set => WarehousesRaw = Join(value);
        }

        /// <summary>
        /// Represents the supplier price snapshots value.
        /// </summary>
        [NotMapped]
        public List<string> SupplierPriceSnapshots
        {
            get => Split(SupplierPricesRaw);
            set => SupplierPricesRaw = Join(value);
        }
    }
}
