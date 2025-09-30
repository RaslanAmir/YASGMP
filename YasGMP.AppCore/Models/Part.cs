using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    [Table("parts")]
    public partial class Part
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("default_supplier_id")]
        public int? DefaultSupplierId { get; set; }

        [ForeignKey(nameof(DefaultSupplierId))]
        public Supplier? DefaultSupplier { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("status")]
        [MaxLength(30)]
        public string? Status { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("category")]
        [MaxLength(255)]
        public string? Category { get; set; }

        [Column("barcode")]
        [MaxLength(255)]
        public string? Barcode { get; set; }

        [Column("rfid")]
        [MaxLength(255)]
        public string? RFID { get; set; }

        [Column("serial_or_lot")]
        [MaxLength(255)]
        public string? SerialOrLot { get; set; }

        [Column("default_supplier")]
        [MaxLength(255)]
        public string? DefaultSupplierName { get; set; }

        [Column("supplier_prices")]
        public string? SupplierPricesRaw { get; set; }

        [Column("price")]
        public decimal? Price { get; set; }

        [Column("stock")]
        public int? Stock { get; set; }

        [Column("min_stock_alert")]
        public int? MinStockAlert { get; set; }

        [Column("warehouse_stocks")]
        public string? WarehouseStocksRaw { get; set; }

        [Column("stock_history")]
        public string? StockHistoryRaw { get; set; }

        [Column("location")]
        [MaxLength(255)]
        public string? Location { get; set; }

        [Column("image")]
        [MaxLength(255)]
        public string? Image { get; set; }

        [Column("images")]
        public string? ImagesRaw { get; set; }

        [Column("documents")]
        public string? DocumentsRaw { get; set; }

        [Column("warranty_until")]
        public DateTime? WarrantyUntil { get; set; }

        [Column("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        [Column("blocked")]
        public bool? Blocked { get; set; }

        [Column("regulatory_certificates")]
        [MaxLength(255)]
        public string? RegulatoryCertificates { get; set; }

        [Column("digital_signature_id")]
        public int? DigitalSignatureId { get; set; }

        [Column("digital_signature")]
        [MaxLength(255)]
        public string? DigitalSignature { get; set; }

        [Column("last_modified")]
        public DateTime? LastModified { get; set; }

        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        [ForeignKey(nameof(LastModifiedById))]
        public User? LastModifiedBy { get; set; }

        [Column("last_modified_by")]
        [MaxLength(255)]
        public string? LastModifiedByName { get; set; }

        [Column("source_ip")]
        [MaxLength(255)]
        public string? SourceIp { get; set; }

        [Column("change_logs")]
        public string? ChangeLogsRaw { get; set; }

        [Column("work_order_parts")]
        public string? WorkOrderPartsRaw { get; set; }

        [Column("warehouses")]
        public string? WarehousesRaw { get; set; }

        [Column("note")]
        [MaxLength(255)]
        public string? Note { get; set; }

        [Column("anomaly_score")]
        public decimal? AnomalyScore { get; set; }

        [Column("supplier")]
        [MaxLength(255)]
        public string? Supplier { get; set; }

        [Column("sku")]
        [MaxLength(100)]
        public string? Sku { get; set; }

        [Column("sku_norm")]
        [MaxLength(100)]
        public string? SkuNormalized { get; set; }

        [NotMapped]
        public List<PartSupplierPrice> SupplierPrices { get; set; } = new();

        [NotMapped]
        public List<WarehouseStock> WarehouseStocks { get; set; } = new();

        [NotMapped]
        public List<StockChangeLog> StockHistory { get; set; } = new();

        [NotMapped]
        public List<StockLevel> StockLevels { get; set; } = new();

        [NotMapped]
        public List<string> Images { get; set; } = new();

        [NotMapped]
        public List<string> Documents { get; set; } = new();

        [NotMapped]
        public List<PartChangeLog> ChangeLogs { get; set; } = new();

        [NotMapped]
        public List<WorkOrderPart> WorkOrderParts { get; set; } = new();

        [NotMapped]
        public List<Warehouse> Warehouses { get; set; } = new();

        [NotMapped]
        public List<string> SupplierList { get; set; } = new();
    }
}
