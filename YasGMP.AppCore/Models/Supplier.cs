using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Supplier</b> — GMP/CSV/21 CFR Part 11 compliant vendor/partner model with compatibility aliases.
    /// Includes rich metadata, qualification lifecycle, digital signatures and hash chain.
    /// </summary>
    [Table("suppliers")]
    public partial class Supplier
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Full supplier or external service name.</summary>
        [Required, StringLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>VAT/tax/OIB equivalent.</summary>
        [StringLength(40)]
        [Column("vat_number")]
        public string VatNumber { get; set; } = string.Empty;

        [Column("address")]
        public string Address { get; set; } = string.Empty;

        [MaxLength(80)]
        [Column("city")]
        public string City { get; set; } = string.Empty;

        [MaxLength(80)]
        [Column("country")]
        public string Country { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        [Column("phone")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(200)]
        [Column("website")]
        public string Website { get; set; } = string.Empty;

        /// <summary>Vendor category: parts, equipment, service, lab, validation…</summary>
        [StringLength(40)]
        [Column("supplier_type")]
        public string SupplierType { get; set; } = string.Empty;

        /// <summary>General notes/comments (legacy alias provided below).</summary>
        [Column("notes")]
        public string Notes { get; set; } = string.Empty;

        /// <summary>Path(s) to contracts, certificates, audits...</summary>
        [MaxLength(255)]
        [Column("contract_file")]
        public string ContractFile { get; set; } = string.Empty;

        [NotMapped]
        public List<string> Documents { get; set; } = new();

        /// <summary>Collaboration/qualification status.</summary>
        [StringLength(40)]
        [Column("status")]
        public string Status { get; set; } = "active";

        /// <summary>Start date of cooperation (contract).</summary>
        [NotMapped]
        public DateTime? CooperationStart { get; set; }

        /// <summary>Cooperation end/expiration.</summary>
        [NotMapped]
        public DateTime? CooperationEnd { get; set; }

        /// <summary>GMP/audit comment (legacy alias provided below).</summary>
        [MaxLength(255)]
        [NotMapped]
        public string GmpComment { get; set; } = string.Empty;

        [Column("digital_signature_id")]
        public int? DigitalSignatureId { get; set; }

        [MaxLength(128)]
        [Column("digital_signature")]
        public string DigitalSignature { get; set; } = string.Empty;

        [Column("entry_hash")]
        [MaxLength(128)]
        public string EntryHash { get; set; } = string.Empty;

        [Column("last_modified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        [ForeignKey(nameof(LastModifiedById))]
        public virtual User? LastModifiedBy { get; set; }

        [MaxLength(45)]
        [Column("source_ip")]
        public string SourceIp { get; set; } = string.Empty;

        [Column("is_qualified")]
        public bool IsQualified { get; set; } = false;

        [Column("last_qualification_check")]
        public DateTime? LastQualificationCheck { get; set; }

        [MaxLength(100)]
        [Column("external_certification_id")]
        public string ExternalCertificationId { get; set; } = string.Empty;

        [NotMapped]
        public List<Inspection> AuditHistory { get; set; } = new();
        [NotMapped]
        public List<CapaCase> CapaCases { get; set; } = new();
        [NotMapped]
        public List<Part> PartsSupplied { get; set; } = new();

        [MaxLength(255)]
        [Column("registered_authorities")]
        public string RegisteredAuthorities { get; set; } = string.Empty;

        [MaxLength(40)]
        [Column("risk_level")]
        public string RiskLevel { get; set; } = string.Empty;

        [Column("anomaly_score")]
        public double? AnomalyScore { get; set; }

        [MaxLength(80)]
        [Column("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [Column("is_ai_flagged")]
        public bool IsAiFlagged { get; set; }

        public override string ToString()
            => $"[{Name}] ({SupplierType}) – {Status} – {Country} – GMP: {IsQualified}";

        // ---------------------------------------------------------------------
        // Compatibility Aliases (NotMapped) to eliminate CS1061/CSxxxx from older code
        // ---------------------------------------------------------------------

        /// <summary>Alias for OIB (maps to <see cref="VatNumber"/>).</summary>
        [NotMapped]
        public string Oib
        {
            get => VatNumber;
            set => VatNumber = value;
        }

        /// <summary>Alias for general comment (maps to <see cref="GmpComment"/>).</summary>
        [NotMapped]
        public string Comment
        {
            get => GmpComment;
            set => GmpComment = value;
        }

        /// <summary>
        /// Alias for supplier code used by legacy UIs. Not persisted; defaults to <see cref="Name"/>.
        /// Setting this will update <see cref="Name"/> for simplest backward compatibility.
        /// </summary>
        [NotMapped]
        public string Code
        {
            get => Name;
            set => Name = value;
        }

        /// <summary>Alias for <see cref="Code"/> used by some grids.</summary>
        [NotMapped]
        public string SupplierCode
        {
            get => Code;
            set => Code = value;
        }

        /// <summary>Alias for cooperation start (maps to <see cref="CooperationStart"/>).</summary>
        [NotMapped]
        public DateTime? ContractStart
        {
            get => CooperationStart;
            set => CooperationStart = value;
        }

        /// <summary>Alias for cooperation end (maps to <see cref="CooperationEnd"/>).</summary>
        [NotMapped]
        public DateTime? ContractEnd
        {
            get => CooperationEnd;
            set => CooperationEnd = value;
        }

        /// <summary>Alias for name used in some lists.</summary>
        [NotMapped]
        public string CompanyName
        {
            get => Name;
            set => Name = value;
        }

        /// <summary>Alias so generic list templates compile (supplier isn't an asset/part).</summary>
        [NotMapped] public string AssetName => Name;
        [NotMapped] public string PartName => Name;

        /// <summary>Alias for risk status used by some VMs (maps to <see cref="RiskLevel"/>).</summary>
        [NotMapped]
        public string RiskRating
        {
            get => RiskLevel;
            set => RiskLevel = value;
        }

        /// <summary>Alias for compliance status (maps to <see cref="Status"/>).</summary>
        [NotMapped]
        public string ComplianceStatus
        {
            get => Status;
            set => Status = value;
        }
    }
}


