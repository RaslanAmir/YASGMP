using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Part</b> – Master record for every spare part in the GMP/CMMS system.
    /// <para>
    /// • Full audit, digital signatures, multi-supplier, price history, attachments<br/>
    /// • Barcode/RFID/serial/lot/expiry/warranty/certificates<br/>
    /// • Linked to warehouses, work orders, change logs, regulatory notes
    /// </para>
    /// <remarks>
    /// Includes legacy-compat properties (<see cref="Supplier"/>, <see cref="Image"/>, <see cref="Description"/>, <see cref="Category"/>)
    /// so older pages and view models continue to compile and run.
    /// </remarks>
    /// </summary>
    public class Part
    {
        /// <summary>Unique part identifier (primary key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Internal code/part number (barcode, QR, SAP/ERP, etc.).</summary>
        [Required, StringLength(50)]
        [Display(Name = "Šifra dijela")]
        public string Code { get; set; } = string.Empty;

        /// <summary>Part name (human-readable).</summary>
        [Required, StringLength(100)]
        [Display(Name = "Naziv dijela")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// <b>Legacy/UI compat:</b> Free-text description used by older views and filters.
        /// </summary>
        [StringLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// <b>Legacy/UI compat:</b> Free-text category (used only for filtering/search in some pages).
        /// </summary>
        [StringLength(100)]
        public string? Category { get; set; }

        /// <summary>Barcode value (EAN/QR/custom), if any.</summary>
        [StringLength(64)]
        public string? Barcode { get; set; }

        /// <summary>RFID/IoT tag.</summary>
        [StringLength(64)]
        public string? RFID { get; set; }

        /// <summary>Serial or lot number (traceability).</summary>
        [StringLength(64)]
        public string? SerialOrLot { get; set; }

        /// <summary>Default supplier (FK).</summary>
        public int? DefaultSupplierId { get; set; }

        /// <summary>Navigation to default supplier (optional).</summary>
        public Supplier? DefaultSupplier { get; set; }

        /// <summary>Supplier price history and suppliers linked to this part.</summary>
        public List<PartSupplierPrice> SupplierPrices { get; set; } = new();

        /// <summary>Current/default price (for UI/reporting; does not replace full price history).</summary>
        [Display(Name = "Cijena")]
        public decimal? Price { get; set; }

        /// <summary>Total quantity on hand (across warehouses).</summary>
        [Display(Name = "Količina ukupno")]
        public int Stock { get; set; }

        /// <summary>Minimum/critical stock alert threshold.</summary>
        public int? MinStockAlert { get; set; }

        /// <summary>Per-warehouse stock snapshot.</summary>
        public List<WarehouseStock> WarehouseStocks { get; set; } = new();

        /// <summary>Stock movement/change history.</summary>
        public List<StockChangeLog> StockHistory { get; set; } = new();

        /// <summary>Primary storage location (quick view).</summary>
        [StringLength(100)]
        [Display(Name = "Lokacija")]
        public string? Location { get; set; }

        /// <summary>
        /// <b>Legacy/UI compat:</b> Preferred single image path for pages that bind to a single string.
        /// If you also use <see cref="Images"/>, keep this as the first/primary image path.
        /// </summary>
        [StringLength(512)]
        public string? Image { get; set; }

        /// <summary>All image paths (catalog/inspection).</summary>
        public List<string> Images { get; set; } = new();

        /// <summary>Paths to attached documents (datasheets, certificates, MSDS…).</summary>
        public List<string> Documents { get; set; } = new();

        /// <summary>Warranty expiry date.</summary>
        public DateTime? WarrantyUntil { get; set; }

        /// <summary>Expiry date (shelf-life/regulatory).</summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>Status (active, archived, blocked, recall, …).</summary>
        [StringLength(30)]
        public string? Status { get; set; }

        /// <summary>Is this part currently blocked for use.</summary>
        public bool Blocked { get; set; }

        /// <summary>Regulatory/certification info (CE/ISO/FDA…).</summary>
        [StringLength(200)]
        public string? RegulatoryCertificates { get; set; }

        /// <summary>Digital signature for the last change (hash/e-sig).</summary>
        [Display(Name = "Digitalni potpis")]
        public string? DigitalSignature { get; set; }

        /// <summary>Last modification timestamp (UTC).</summary>
        [Display(Name = "Zadnja izmjena")]
        public DateTime LastModified { get; set; }

        /// <summary>User ID of last modifier (FK).</summary>
        [Display(Name = "Zadnji izmijenio")]
        public int LastModifiedById { get; set; }

        /// <summary>Navigation to last modifying user (optional).</summary>
        public User? LastModifiedBy { get; set; }

        /// <summary>IP address for the last modification.</summary>
        [Display(Name = "IP adresa")]
        public string? SourceIp { get; set; }

        /// <summary>Structured change log (domain audit).</summary>
        public List<PartChangeLog> ChangeLogs { get; set; } = new();

        /// <summary>Links to work orders where this part was used.</summary>
        public List<WorkOrderPart> WorkOrderParts { get; set; } = new();

        /// <summary>List of warehouses this part is registered in.</summary>
        public List<Warehouse> Warehouses { get; set; } = new();

        /// <summary>Inspector/ML notes, alerts, or comments.</summary>
        [StringLength(1000)]
        public string? Note { get; set; }

        /// <summary>ML/AI supply risk or anomaly score (optional).</summary>
        public double? AnomalyScore { get; set; }

        /// <summary>
        /// <b>Legacy/UI compat:</b> Free-text supplier name for simple grids/forms.
        /// When using relational suppliers, keep this as a denormalized display value (e.g., from <see cref="DefaultSupplier"/> or <see cref="SupplierPrices"/>).
        /// </summary>
        [StringLength(256)]
        public string? Supplier { get; set; }

        /// <summary>Returns true if the part is expired.</summary>
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;

        /// <summary>Returns true if stock is below the alert threshold.</summary>
        public bool IsStockCritical => MinStockAlert.HasValue && Stock < MinStockAlert.Value;

        /// <summary>Returns true if the part is blocked for use.</summary>
        public bool IsBlocked => Blocked || (!string.IsNullOrWhiteSpace(Status) && Status.ToLowerInvariant().Contains("block"));

        /// <summary>Main supplier name resolved from <see cref="DefaultSupplier"/> or first <see cref="SupplierPrices"/> row.</summary>
        public string? MainSupplierName =>
            !string.IsNullOrWhiteSpace(DefaultSupplier?.Name) ? DefaultSupplier.Name :
            (SupplierPrices.Count > 0 ? (SupplierPrices[0].SupplierName ?? SupplierPrices[0].Supplier?.Name) : null) ??
            Supplier;

        /// <summary>Human-readable string for logs and inspectors.</summary>
        public override string ToString()
            => $"Part: {Name} [{Code}] (Stock: {Stock}, Price: {Price}, Blocked: {IsBlocked})";
    }
}
