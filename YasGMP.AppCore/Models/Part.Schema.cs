using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;

namespace YasGMP.Models
{
    public partial class Part
    {
        [NotMapped]
        public bool IsBlocked => (Blocked ?? false) || (!string.IsNullOrWhiteSpace(Status) && Status!.IndexOf("block", StringComparison.OrdinalIgnoreCase) >= 0);

        [NotMapped]
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;

        [NotMapped]
        public bool IsStockCritical => MinStockAlert.HasValue && Stock.HasValue && Stock.Value < MinStockAlert.Value;

        [NotMapped]
        public string? MainSupplierName =>
            !string.IsNullOrWhiteSpace(DefaultSupplier?.Name) ? DefaultSupplier!.Name :
            SupplierPrices.FirstOrDefault()?.SupplierName ?? Supplier ?? DefaultSupplierName;

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

        [NotMapped]
        public string WarehouseSummary { get; set; } = string.Empty;

        [NotMapped]
        public bool IsWarehouseStockCritical { get; set; }

        [NotMapped]
        public int LowWarehouseCount { get; set; } = 0;

        [NotMapped]
        public int LinkedWorkOrdersCount { get; set; } = 0;

        private List<string> Split(string? raw) => string.IsNullOrWhiteSpace(raw)
            ? new List<string>()
            : raw!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        private string? Join(IEnumerable<string> values)
            => values == null ? null : string.Join(',', values.Where(v => !string.IsNullOrWhiteSpace(v)));

        [NotMapped]
        public List<string> WarehouseStockSnapshots
        {
            get => Split(WarehouseStocksRaw);
            set => WarehouseStocksRaw = Join(value);
        }

        [NotMapped]
        public List<string> StockHistorySnapshots
        {
            get => Split(StockHistoryRaw);
            set => StockHistoryRaw = Join(value);
        }

        [NotMapped]
        public List<string> ImagePaths
        {
            get => Split(ImagesRaw);
            set => ImagesRaw = Join(value);
        }

        [NotMapped]
        public List<string> DocumentPaths
        {
            get => Split(DocumentsRaw);
            set => DocumentsRaw = Join(value);
        }

        [NotMapped]
        public List<string> ChangeLogSnapshots
        {
            get => Split(ChangeLogsRaw);
            set => ChangeLogsRaw = Join(value);
        }

        [NotMapped]
        public List<string> WorkOrderPartSnapshots
        {
            get => Split(WorkOrderPartsRaw);
            set => WorkOrderPartsRaw = Join(value);
        }

        [NotMapped]
        public List<string> WarehouseSnapshots
        {
            get => Split(WarehousesRaw);
            set => WarehousesRaw = Join(value);
        }

        [NotMapped]
        public List<string> SupplierPriceSnapshots
        {
            get => Split(SupplierPricesRaw);
            set => SupplierPricesRaw = Join(value);
        }
    }
}

