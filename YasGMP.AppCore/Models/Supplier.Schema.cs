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
        /// <summary>
        /// Gets or sets the code raw.
        /// </summary>
        [Column("code")]
        [StringLength(50)]
        public string? CodeRaw { get; set; }

        /// <summary>
        /// Gets or sets the oib raw.
        /// </summary>
        [Column("oib")]
        [StringLength(40)]
        public string? OibRaw { get; set; }

        /// <summary>
        /// Gets or sets the contact person.
        /// </summary>
        [Column("contact")]
        [StringLength(100)]
        public string? ContactPerson { get; set; }

        /// <summary>
        /// Gets or sets the supplier type legacy.
        /// </summary>
        [Column("type")]
        [StringLength(40)]
        public string? SupplierTypeLegacy { get; set; }

        /// <summary>
        /// Represents the contract start raw value.
        /// </summary>
        [Column("contract_start")]
        public DateTime? ContractStartRaw
        {
            get => CooperationStart;
            set => CooperationStart = value;
        }

        /// <summary>
        /// Represents the contract end raw value.
        /// </summary>
        [Column("contract_end")]
        public DateTime? ContractEndRaw
        {
            get => CooperationEnd;
            set => CooperationEnd = value;
        }

        /// <summary>
        /// Gets or sets the certificate document.
        /// </summary>
        [Column("cert_doc")]
        [StringLength(255)]
        public string? CertificateDocument { get; set; }

        /// <summary>
        /// Gets or sets the valid until.
        /// </summary>
        [Column("valid_until")]
        public DateTime? ValidUntil { get; set; }

        /// <summary>
        /// Gets or sets the risk score.
        /// </summary>
        [Column("risk_score")]
        public int? RiskScore { get; set; }

        /// <summary>
        /// Gets or sets the last audit.
        /// </summary>
        [Column("last_audit")]
        public DateTime? LastAudit { get; set; }

        /// <summary>
        /// Represents the comment raw value.
        /// </summary>
        [Column("comment")]
        public string? CommentRaw
        {
            get => GmpComment;
            set => GmpComment = value ?? string.Empty;
        }

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
        /// Gets or sets the status id.
        /// </summary>
        [Column("status_id")]
        public int? StatusId { get; set; }

        /// <summary>
        /// Gets or sets the status ref.
        /// </summary>
        [ForeignKey(nameof(StatusId))]
        public virtual RefValue? StatusRef { get; set; }

        /// <summary>
        /// Gets or sets the type id.
        /// </summary>
        [Column("type_id")]
        public int? TypeId { get; set; }

        /// <summary>
        /// Gets or sets the type ref.
        /// </summary>
        [ForeignKey(nameof(TypeId))]
        public virtual RefValue? TypeRef { get; set; }
    }
}
