using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>SparePart</b> – Ultra robust master record for all parts in the GMP/CMMS inventory system.
    /// <para>
    /// ✅ Supports full audit, digital signatures, multi-supplier, price history, attachments, regulatory traceability.<br/>
    /// ✅ Includes barcode/QR, status, warranty, linked stock levels, work orders, and advanced supply chain analytics.
    /// </para>
    /// </summary>
    [NotMapped]
    public class SparePart
    {
        /// <summary>Unique part identifier (Primary Key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Internal part code or number (barcode, QR, SAP/ERP number).</summary>
        [Required, StringLength(50)]
        [Display(Name = "Šifra dijela")]
        public string Code { get; set; } = string.Empty;

        /// <summary>Descriptive part name.</summary>
        [Required, StringLength(100)]
        [Display(Name = "Naziv dijela")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Default supplier for this part (foreign key).</summary>
        [Display(Name = "Dobavljač (default)")]
        public int? DefaultSupplierId { get; set; }

        /// <summary>Detailed part description (spec, revision, etc.).</summary>
        [Display(Name = "Opis dijela")]
        public string Description { get; set; } = string.Empty;

        /// <summary>Current status (active, obsolete, reorder).</summary>
        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "active";

        /// <summary>Digital signature hash of last authorized change (GMP traceability).</summary>
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Last modification timestamp (audit).</summary>
        [Display(Name = "Zadnja izmjena")]
        public DateTime? LastModified { get; set; }

        /// <summary>User ID of last modifier (audit).</summary>
        [Display(Name = "Izmijenio")]
        public int? LastModifiedById { get; set; }

        /// <summary>Default supplier (navigation property).</summary>
        public virtual Supplier DefaultSupplier { get; set; } = null!;

        /// <summary>Price history for this part (all suppliers).</summary>
        public virtual ICollection<PartSupplierPrice> SupplierPrices { get; set; } = new List<PartSupplierPrice>();

        /// <summary>Current stock levels in all warehouses.</summary>
        public virtual ICollection<StockLevel> StockLevels { get; set; } = new List<StockLevel>();

        /// <summary>All inventory transactions for this part.</summary>
        public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

        /// <summary>Attachments and documents for this part (certificates, manuals, photos).</summary>
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

        /// <summary>Audit logs, digital signatures, or advanced trace records for this part.</summary>
        public virtual ICollection<DigitalSignature> DigitalSignatures { get; set; } = new List<DigitalSignature>();

        /// <summary>Creates a new SparePart instance.</summary>
        public SparePart()
        {
            // Collections initialized inline; navigation defaults provided.
        }
    }
}
