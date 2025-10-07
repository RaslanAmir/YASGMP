using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Record describing negotiated pricing and contract metadata between a part and a supplier.
    /// Includes commercial, compliance, and audit details for full GMP/CSV traceability.
    /// </summary>
    [Table("part_supplier_prices")]
    public partial class PartSupplierPrice
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the part id.
        /// </summary>
        [Column("part_id")]
        public int? PartId { get; set; }

        /// <summary>
        /// Gets or sets the supplier id.
        /// </summary>
        [Column("supplier_id")]
        public int? SupplierId { get; set; }

        /// <summary>
        /// Gets or sets the unit price.
        /// </summary>
        [Column("unit_price", TypeName = "decimal(10,2)")]
        public decimal? UnitPrice { get; set; }

        /// <summary>
        /// Gets or sets the currency.
        /// </summary>
        [Column("currency")]
        [StringLength(10)]
        public string? Currency { get; set; }

        /// <summary>
        /// Gets or sets the valid until.
        /// </summary>
        [Column("valid_until")]
        public DateTime? ValidUntil { get; set; }

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
        /// Gets or sets the part code.
        /// </summary>
        [Column("part")]
        [StringLength(255)]
        public string? PartCode { get; set; }

        /// <summary>
        /// Gets or sets the supplier code.
        /// </summary>
        [Column("supplier")]
        [StringLength(255)]
        public string? SupplierCode { get; set; }

        /// <summary>
        /// Gets or sets the supplier name.
        /// </summary>
        [Column("supplier_name")]
        [StringLength(255)]
        public string? SupplierName { get; set; }

        /// <summary>
        /// Gets or sets the vat percent.
        /// </summary>
        [Column("vat_percent", TypeName = "decimal(10,2)")]
        public decimal? VatPercent { get; set; }

        /// <summary>
        /// Gets or sets the price with vat.
        /// </summary>
        [Column("price_with_vat", TypeName = "decimal(10,2)")]
        public decimal? PriceWithVat { get; set; }

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        [Column("region")]
        [StringLength(255)]
        public string? Region { get; set; }

        /// <summary>
        /// Gets or sets the discount percent.
        /// </summary>
        [Column("discount_percent", TypeName = "decimal(10,2)")]
        public decimal? DiscountPercent { get; set; }

        /// <summary>
        /// Gets or sets the surcharge.
        /// </summary>
        [Column("surcharge", TypeName = "decimal(10,2)")]
        public decimal? Surcharge { get; set; }

        /// <summary>
        /// Gets or sets the min order quantity.
        /// </summary>
        [Column("min_order_quantity")]
        public int? MinOrderQuantity { get; set; }

        /// <summary>
        /// Gets or sets the lead time days.
        /// </summary>
        [Column("lead_time_days")]
        public int? LeadTimeDays { get; set; }

        /// <summary>
        /// Gets or sets the valid from.
        /// </summary>
        [Column("valid_from")]
        public DateTime? ValidFrom { get; set; }

        /// <summary>
        /// Gets or sets the is blocked.
        /// </summary>
        [Column("is_blocked")]
        public bool? IsBlocked { get; set; }

        /// <summary>
        /// Gets or sets the block reason.
        /// </summary>
        [Column("block_reason")]
        [StringLength(255)]
        public string? BlockReason { get; set; }

        /// <summary>
        /// Gets or sets the contract document.
        /// </summary>
        [Column("contract_document")]
        [StringLength(255)]
        public string? ContractDocument { get; set; }

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
        [Column("last_modified_by")]
        [StringLength(255)]
        public string? LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the digital signature.
        /// </summary>
        [Column("digital_signature")]
        [StringLength(255)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Gets or sets the source ip.
        /// </summary>
        [Column("source_ip")]
        [StringLength(255)]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Gets or sets the anomaly score.
        /// </summary>
        [Column("anomaly_score", TypeName = "decimal(10,2)")]
        public decimal? AnomalyScore { get; set; }

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        [Column("note")]
        [StringLength(255)]
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets the part.
        /// </summary>
        [ForeignKey(nameof(PartId))]
        public virtual Part? Part { get; set; }

        /// <summary>
        /// Gets or sets the supplier.
        /// </summary>
        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier? Supplier { get; set; }

        /// <summary>
        /// Gets or sets the last modified by user.
        /// </summary>
        [ForeignKey(nameof(LastModifiedById))]
        public virtual User? LastModifiedByUser { get; set; }
    }
}
