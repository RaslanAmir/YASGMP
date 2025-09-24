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
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("part_id")]
        public int? PartId { get; set; }

        [Column("supplier_id")]
        public int? SupplierId { get; set; }

        [Column("unit_price", TypeName = "decimal(10,2)")]
        public decimal? UnitPrice { get; set; }

        [Column("currency")]
        [StringLength(10)]
        public string? Currency { get; set; }

        [Column("valid_until")]
        public DateTime? ValidUntil { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("part")]
        [StringLength(255)]
        public string? PartCode { get; set; }

        [Column("supplier")]
        [StringLength(255)]
        public string? SupplierCode { get; set; }

        [Column("supplier_name")]
        [StringLength(255)]
        public string? SupplierName { get; set; }

        [Column("vat_percent", TypeName = "decimal(10,2)")]
        public decimal? VatPercent { get; set; }

        [Column("price_with_vat", TypeName = "decimal(10,2)")]
        public decimal? PriceWithVat { get; set; }

        [Column("region")]
        [StringLength(255)]
        public string? Region { get; set; }

        [Column("discount_percent", TypeName = "decimal(10,2)")]
        public decimal? DiscountPercent { get; set; }

        [Column("surcharge", TypeName = "decimal(10,2)")]
        public decimal? Surcharge { get; set; }

        [Column("min_order_quantity")]
        public int? MinOrderQuantity { get; set; }

        [Column("lead_time_days")]
        public int? LeadTimeDays { get; set; }

        [Column("valid_from")]
        public DateTime? ValidFrom { get; set; }

        [Column("is_blocked")]
        public bool? IsBlocked { get; set; }

        [Column("block_reason")]
        [StringLength(255)]
        public string? BlockReason { get; set; }

        [Column("contract_document")]
        [StringLength(255)]
        public string? ContractDocument { get; set; }

        [Column("last_modified")]
        public DateTime? LastModified { get; set; }

        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        [Column("last_modified_by")]
        [StringLength(255)]
        public string? LastModifiedBy { get; set; }

        [Column("digital_signature")]
        [StringLength(255)]
        public string? DigitalSignature { get; set; }

        [Column("source_ip")]
        [StringLength(255)]
        public string? SourceIp { get; set; }

        [Column("anomaly_score", TypeName = "decimal(10,2)")]
        public decimal? AnomalyScore { get; set; }

        [Column("note")]
        [StringLength(255)]
        public string? Note { get; set; }

        [ForeignKey(nameof(PartId))]
        public virtual Part? Part { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier? Supplier { get; set; }

        [ForeignKey(nameof(LastModifiedById))]
        public virtual User? LastModifiedByUser { get; set; }
    }
}
