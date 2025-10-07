using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents the Part.
    /// </summary>
    [Table("parts")]
    public partial class Part
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the default supplier id.
        /// </summary>
        [Column("default_supplier_id")]
        public int? DefaultSupplierId { get; set; }

        /// <summary>
        /// Gets or sets the default supplier.
        /// </summary>
        [ForeignKey(nameof(DefaultSupplierId))]
        public Supplier? DefaultSupplier { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [Column("status")]
        [MaxLength(30)]
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        [Column("category")]
        [MaxLength(255)]
        public string? Category { get; set; }

        /// <summary>
        /// Gets or sets the barcode.
        /// </summary>
        [Column("barcode")]
        [MaxLength(255)]
        public string? Barcode { get; set; }

        /// <summary>
        /// Gets or sets the rfid.
        /// </summary>
        [Column("rfid")]
        [MaxLength(255)]
        public string? RFID { get; set; }

        /// <summary>
        /// Gets or sets the serial or lot.
        /// </summary>
        [Column("serial_or_lot")]
        [MaxLength(255)]
        public string? SerialOrLot { get; set; }

        /// <summary>
        /// Gets or sets the default supplier name.
        /// </summary>
        [Column("default_supplier")]
        [MaxLength(255)]
        public string? DefaultSupplierName { get; set; }

        /// <summary>
        /// Gets or sets the supplier prices raw.
        /// </summary>
        [Column("supplier_prices")]
        public string? SupplierPricesRaw { get; set; }

        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        [Column("price")]
        public decimal? Price { get; set; }

        /// <summary>
        /// Gets or sets the stock.
        /// </summary>
        [Column("stock")]
        public int? Stock { get; set; }

        /// <summary>
        /// Gets or sets the min stock alert.
        /// </summary>
        [Column("min_stock_alert")]
        public int? MinStockAlert { get; set; }

        /// <summary>
        /// Gets or sets the warehouse stocks raw.
        /// </summary>
        [Column("warehouse_stocks")]
        public string? WarehouseStocksRaw { get; set; }

        /// <summary>
        /// Gets or sets the stock history raw.
        /// </summary>
        [Column("stock_history")]
        public string? StockHistoryRaw { get; set; }

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        [Column("location")]
        [MaxLength(255)]
        public string? Location { get; set; }

        /// <summary>
        /// Gets or sets the image.
        /// </summary>
        [Column("image")]
        [MaxLength(255)]
        public string? Image { get; set; }

        /// <summary>
        /// Gets or sets the images raw.
        /// </summary>
        [Column("images")]
        public string? ImagesRaw { get; set; }

        /// <summary>
        /// Gets or sets the documents raw.
        /// </summary>
        [Column("documents")]
        public string? DocumentsRaw { get; set; }

        /// <summary>
        /// Gets or sets the warranty until.
        /// </summary>
        [Column("warranty_until")]
        public DateTime? WarrantyUntil { get; set; }

        /// <summary>
        /// Gets or sets the expiry date.
        /// </summary>
        [Column("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Gets or sets the blocked.
        /// </summary>
        [Column("blocked")]
        public bool? Blocked { get; set; }

        /// <summary>
        /// Gets or sets the regulatory certificates.
        /// </summary>
        [Column("regulatory_certificates")]
        [MaxLength(255)]
        public string? RegulatoryCertificates { get; set; }

        /// <summary>
        /// Gets or sets the digital signature id.
        /// </summary>
        [Column("digital_signature_id")]
        public int? DigitalSignatureId { get; set; }

        /// <summary>
        /// Gets or sets the digital signature.
        /// </summary>
        [Column("digital_signature")]
        [MaxLength(255)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Gets or sets the last modified.
        /// </summary>
        [Column("last_modified")]
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// Gets or sets the last modified by id.
        /// </summary>
        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        /// <summary>
        /// Gets or sets the last modified by.
        /// </summary>
        [ForeignKey(nameof(LastModifiedById))]
        public User? LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the last modified by name.
        /// </summary>
        [Column("last_modified_by")]
        [MaxLength(255)]
        public string? LastModifiedByName { get; set; }

        /// <summary>
        /// Gets or sets the source ip.
        /// </summary>
        [Column("source_ip")]
        [MaxLength(255)]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Gets or sets the change logs raw.
        /// </summary>
        [Column("change_logs")]
        public string? ChangeLogsRaw { get; set; }

        /// <summary>
        /// Gets or sets the work order parts raw.
        /// </summary>
        [Column("work_order_parts")]
        public string? WorkOrderPartsRaw { get; set; }

        /// <summary>
        /// Gets or sets the warehouses raw.
        /// </summary>
        [Column("warehouses")]
        public string? WarehousesRaw { get; set; }

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        [Column("note")]
        [MaxLength(255)]
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets the anomaly score.
        /// </summary>
        [Column("anomaly_score")]
        public decimal? AnomalyScore { get; set; }

        /// <summary>
        /// Gets or sets the supplier.
        /// </summary>
        [Column("supplier")]
        [MaxLength(255)]
        public string? Supplier { get; set; }

        /// <summary>
        /// Gets or sets the sku.
        /// </summary>
        [Column("sku")]
        [MaxLength(100)]
        public string? Sku { get; set; }

        /// <summary>
        /// Gets or sets the sku normalized.
        /// </summary>
        [Column("sku_norm")]
        [MaxLength(100)]
        public string? SkuNormalized { get; set; }

        /// <summary>
        /// Gets or sets the supplier prices.
        /// </summary>
        [NotMapped]
        public List<PartSupplierPrice> SupplierPrices { get; set; } = new();

        /// <summary>
        /// Gets or sets the warehouse stocks.
        /// </summary>
        [NotMapped]
        public List<WarehouseStock> WarehouseStocks { get; set; } = new();

        /// <summary>
        /// Gets or sets the stock history.
        /// </summary>
        [NotMapped]
        public List<StockChangeLog> StockHistory { get; set; } = new();

        /// <summary>
        /// Gets or sets the stock levels.
        /// </summary>
        [NotMapped]
        public List<StockLevel> StockLevels { get; set; } = new();

        /// <summary>
        /// Gets or sets the images.
        /// </summary>
        [NotMapped]
        public List<string> Images { get; set; } = new();

        /// <summary>
        /// Gets or sets the documents.
        /// </summary>
        [NotMapped]
        public List<string> Documents { get; set; } = new();

        /// <summary>
        /// Gets or sets the change logs.
        /// </summary>
        [NotMapped]
        public List<PartChangeLog> ChangeLogs { get; set; } = new();

        /// <summary>
        /// Gets or sets the work order parts.
        /// </summary>
        [NotMapped]
        public List<WorkOrderPart> WorkOrderParts { get; set; } = new();

        /// <summary>
        /// Gets or sets the warehouses.
        /// </summary>
        [NotMapped]
        public List<Warehouse> Warehouses { get; set; } = new();

        /// <summary>
        /// Gets or sets the supplier list.
        /// </summary>
        [NotMapped]
        public List<string> SupplierList { get; set; } = new();
    }
}
