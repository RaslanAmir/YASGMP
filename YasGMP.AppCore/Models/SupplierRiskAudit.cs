using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `supplier_risk_audit` table.</summary>
    [Table("supplier_risk_audit")]
    public class SupplierRiskAudit
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the supplier id.</summary>
        [Column("supplier_id")]
        public int? SupplierId { get; set; }

        /// <summary>Gets or sets the audit date.</summary>
        [Column("audit_date")]
        public DateTime? AuditDate { get; set; }

        /// <summary>Gets or sets the score.</summary>
        [Column("score")]
        public int? Score { get; set; }

        /// <summary>Gets or sets the performed by.</summary>
        [Column("performed_by")]
        public int? PerformedBy { get; set; }

        /// <summary>Gets or sets the findings.</summary>
        [Column("findings")]
        public string? Findings { get; set; }

        /// <summary>Gets or sets the corrective actions.</summary>
        [Column("corrective_actions")]
        public string? CorrectiveActions { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier? Supplier { get; set; }

        [ForeignKey(nameof(PerformedBy))]
        public virtual User? PerformedByNavigation { get; set; }
    }
}
