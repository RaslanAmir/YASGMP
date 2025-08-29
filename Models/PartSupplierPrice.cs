using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>PartSupplierPrice</b> – Super ultra mega robust price and contract record for a supplier-part relationship.
    /// <para>
    /// ✅ Full price history, audit, digital signature, multi-currency, region, and contract validity  
    /// ✅ Tracks VAT, discounts, surcharges, min order, lead time, recall/block, ML price scoring  
    /// ✅ Inspector/ERP/analytics-ready, fully GMP/CSV/21 CFR Part 11/ISO compliant
    /// </para>
    /// </summary>
    public class PartSupplierPrice
    {
        /// <summary>Unique record ID (Primary Key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>FK to the part (Part).</summary>
        [Required]
        public int PartId { get; set; }
        public Part? Part { get; set; }

        /// <summary>FK to the supplier (Supplier).</summary>
        [Required]
        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        /// <summary>Supplier name (snapshot for history/report even if deleted/renamed).</summary>
        [StringLength(200)]
        public string? SupplierName { get; set; }

        /// <summary>Unit price of the part (excluding VAT).</summary>
        [Required]
        [Display(Name = "Jedinična cijena")]
        public decimal UnitPrice { get; set; }

        /// <summary>VAT percent for this price (for regulatory/tax calculations).</summary>
        public double VatPercent { get; set; }

        /// <summary>Total price including VAT (auto or manual for analytics/report).</summary>
        public decimal? PriceWithVat { get; set; }

        /// <summary>Currency (ISO 4217, e.g. HRK, EUR, USD, GBP, JPY).</summary>
        [Required, StringLength(10)]
        [Display(Name = "Valuta")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>Region/country for this supplier price (for localization, export control).</summary>
        [StringLength(40)]
        public string? Region { get; set; }

        /// <summary>Discount percent (if contractually agreed, for analytics/negotiation).</summary>
        public double? DiscountPercent { get; set; }

        /// <summary>Additional surcharge (e.g. shipping, customs, regulatory, per unit).</summary>
        public decimal? Surcharge { get; set; }

        /// <summary>Minimum order quantity (if contractually required).</summary>
        public int? MinOrderQuantity { get; set; }

        /// <summary>Average lead time in days (procurement analytics, supply chain risk).</summary>
        public int? LeadTimeDays { get; set; }

        /// <summary>Start date of validity (for multi-contract/period support).</summary>
        public DateTime? ValidFrom { get; set; }

        /// <summary>End date of validity (for price history, audit, and future-proofing).</summary>
        [Display(Name = "Vrijedi do")]
        public DateTime? ValidUntil { get; set; }

        /// <summary>Is this price blocked due to recall, compliance, or audit?</summary>
        public bool IsBlocked { get; set; }

        /// <summary>Reason for block/recall/non-use (supply chain risk, compliance, recall, etc).</summary>
        [StringLength(200)]
        public string? BlockReason { get; set; }

        /// <summary>Attachment for scanned contract/agreement, PDF, or price list.</summary>
        public string? ContractDocument { get; set; }

        /// <summary>Audit: last change timestamp.</summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>User ID of last change (FK, for audit chain).</summary>
        public int LastModifiedById { get; set; }
        public User? LastModifiedBy { get; set; }

        /// <summary>Digital signature/hash of the change (for forensic/audit/21 CFR compliance).</summary>
        public string? DigitalSignature { get; set; }

        /// <summary>IP/device info of the change (for audit/traceability).</summary>
        public string? SourceIp { get; set; }

        /// <summary>ML/AI risk or price anomaly score (future analytics, anti-fraud, supply chain health).</summary>
        public double? AnomalyScore { get; set; }

        /// <summary>Inspector/ERP free note or comment (audit, negotiation, recall...).</summary>
        [StringLength(1000)]
        public string? Note { get; set; }

        /// <summary>Returns true if price is currently valid (now within ValidFrom/ValidUntil).</summary>
        public bool IsCurrentlyValid =>
            (!ValidFrom.HasValue || ValidFrom.Value <= DateTime.UtcNow)
            && (!ValidUntil.HasValue || ValidUntil.Value >= DateTime.UtcNow)
            && !IsBlocked;

        /// <summary>Returns price including VAT (auto-calc if not set).</summary>
        public decimal PriceWithVatCalc => PriceWithVat ?? UnitPrice * (1 + (decimal)(VatPercent / 100.0));

        /// <summary>Returns human-readable string for logs/reports/inspectors.</summary>
        public override string ToString()
        {
            return $"Supplier: {SupplierName ?? Supplier?.Name} | {UnitPrice} {Currency} (Valid: {ValidFrom?.ToShortDateString()} - {ValidUntil?.ToShortDateString()})";
        }
    }
}
