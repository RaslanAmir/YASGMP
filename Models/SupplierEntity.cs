using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `suppliers` table.</summary>
    [Table("suppliers")]
    public class SupplierEntity
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the name.</summary>
        [Column("name")]
        [StringLength(100)]
        public string? Name { get; set; }

        /// <summary>Gets or sets the vat number.</summary>
        [Column("vat_number")]
        [StringLength(40)]
        public string? VatNumber { get; set; }

        /// <summary>Gets or sets the code.</summary>
        [Column("code")]
        [StringLength(50)]
        public string? Code { get; set; }

        /// <summary>Gets or sets the oib.</summary>
        [Column("oib")]
        [StringLength(40)]
        public string? Oib { get; set; }

        /// <summary>Gets or sets the contact.</summary>
        [Column("contact")]
        [StringLength(100)]
        public string? Contact { get; set; }

        /// <summary>Gets or sets the email.</summary>
        [Column("email")]
        [StringLength(100)]
        public string? Email { get; set; }

        /// <summary>Gets or sets the phone.</summary>
        [Column("phone")]
        [StringLength(50)]
        public string? Phone { get; set; }

        /// <summary>Gets or sets the website.</summary>
        [Column("website")]
        [StringLength(200)]
        public string? Website { get; set; }

        /// <summary>Gets or sets the supplier type.</summary>
        [Column("supplier_type")]
        [StringLength(40)]
        public string? SupplierType { get; set; }

        /// <summary>Gets or sets the notes.</summary>
        [Column("notes")]
        public string? Notes { get; set; }

        /// <summary>Gets or sets the contract file.</summary>
        [Column("contract_file")]
        [StringLength(255)]
        public string? ContractFile { get; set; }

        /// <summary>Gets or sets the address.</summary>
        [Column("address")]
        public string? Address { get; set; }

        /// <summary>Gets or sets the city.</summary>
        [Column("city")]
        [StringLength(80)]
        public string? City { get; set; }

        /// <summary>Gets or sets the country.</summary>
        [Column("country")]
        [StringLength(80)]
        public string? Country { get; set; }

        /// <summary>Gets or sets the type.</summary>
        [Column("type")]
        [StringLength(40)]
        public string? Type { get; set; }

        /// <summary>Gets or sets the status.</summary>
        [Column("status")]
        [StringLength(40)]
        public string? Status { get; set; }

        /// <summary>Gets or sets the contract start.</summary>
        [Column("contract_start")]
        public DateTime? ContractStart { get; set; }

        /// <summary>Gets or sets the contract end.</summary>
        [Column("contract_end")]
        public DateTime? ContractEnd { get; set; }

        /// <summary>Gets or sets the cert doc.</summary>
        [Column("cert_doc")]
        [StringLength(255)]
        public string? CertDoc { get; set; }

        /// <summary>Gets or sets the valid until.</summary>
        [Column("valid_until")]
        public DateTime? ValidUntil { get; set; }

        /// <summary>Gets or sets the risk score.</summary>
        [Column("risk_score")]
        public int? RiskScore { get; set; }

        /// <summary>Gets or sets the last audit.</summary>
        [Column("last_audit")]
        public DateTime? LastAudit { get; set; }

        /// <summary>Gets or sets the comment.</summary>
        [Column("comment")]
        public string? Comment { get; set; }

        /// <summary>Gets or sets the digital signature.</summary>
        [Column("digital_signature")]
        [StringLength(128)]
        public string? DigitalSignature { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Gets or sets the status id.</summary>
        [Column("status_id")]
        public int? StatusId { get; set; }

        /// <summary>Gets or sets the type id.</summary>
        [Column("type_id")]
        public int? TypeId { get; set; }

        [ForeignKey(nameof(StatusId))]
        public virtual RefValue? StatusNavigation { get; set; }

        [ForeignKey(nameof(TypeId))]
        public virtual RefValue? TypeNavigation { get; set; }
    }
}
