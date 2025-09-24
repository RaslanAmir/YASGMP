using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema supplement for <see cref="Supplier"/> ensuring every column from the `suppliers` table is materialised.
    /// </summary>
    public partial class Supplier
    {
        [Column("code")]
        [StringLength(50)]
        public string? CodeRaw { get; set; }

        [Column("oib")]
        [StringLength(40)]
        public string? OibRaw { get; set; }

        [Column("contact")]
        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [Column("type")]
        [StringLength(40)]
        public string? SupplierTypeLegacy { get; set; }

        [Column("contract_start")]
        public DateTime? ContractStartRaw
        {
            get => CooperationStart;
            set => CooperationStart = value;
        }

        [Column("contract_end")]
        public DateTime? ContractEndRaw
        {
            get => CooperationEnd;
            set => CooperationEnd = value;
        }

        [Column("cert_doc")]
        [StringLength(255)]
        public string? CertificateDocument { get; set; }

        [Column("valid_until")]
        public DateTime? ValidUntil { get; set; }

        [Column("risk_score")]
        public int? RiskScore { get; set; }

        [Column("last_audit")]
        public DateTime? LastAudit { get; set; }

        [Column("comment")]
        public string? CommentRaw
        {
            get => GmpComment;
            set => GmpComment = value ?? string.Empty;
        }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("status_id")]
        public int? StatusId { get; set; }

        [ForeignKey(nameof(StatusId))]
        public virtual RefValue? StatusRef { get; set; }

        [Column("type_id")]
        public int? TypeId { get; set; }

        [ForeignKey(nameof(TypeId))]
        public virtual RefValue? TypeRef { get; set; }
    }
}
