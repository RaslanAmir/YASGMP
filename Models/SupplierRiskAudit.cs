using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("supplier_risk_audit")]
    public class SupplierRiskAudit
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("supplier_id")]
        public int? SupplierId { get; set; }

        [Column("audit_date")]
        public DateTime? AuditDate { get; set; }

        [Column("score")]
        public int? Score { get; set; }

        [Column("performed_by")]
        public int? PerformedBy { get; set; }

        [Column("findings")]
        public string? Findings { get; set; }

        [Column("corrective_actions")]
        public string? CorrectiveActions { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
