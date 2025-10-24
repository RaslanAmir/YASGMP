using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YasGMP.Models.Enums;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Deviation</b> – Ultra-mega robust GMP deviation/non-conformance record (audit, workflow, root cause, CAPA, digital signature, AI-ready).
    /// <para>Supports complete GMP/Annex 11/21 CFR Part 11 requirements, forensics, workflow, advanced escalation, custom fields, attachments, and AI/ML hooks.</para>
    /// </summary>
    [Table("deviations")]
    public partial class Deviation
    {
        /// <summary>
        /// Unique identifier for the deviation (Primary Key).
        /// </summary>
        [Key]
        [Column("id")]
        [Display(Name = "ID odstupanja")]
        public int Id { get; set; }

        /// <summary>
        /// Short, unique code (e.g., DEVI-2025-001).
        /// </summary>
        [MaxLength(40)]
        [Column("code")]
        [Display(Name = "Kod odstupanja")]
        public string? Code { get; set; }

        /// <summary>
        /// Title/subject of deviation (required).
        /// </summary>
        [Required]
        [MaxLength(200)]
        [Column("title")]
        [Display(Name = "Naslov odstupanja")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Free text deviation description (required).
        /// </summary>
        [Required]
        [MaxLength(4000)]
        [Column("description")]
        [Display(Name = "Opis odstupanja")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Date/time when deviation was reported (UTC).
        /// </summary>
        [Column("reported_at")]
        [Display(Name = "Vrijeme prijave")]
        public DateTime? ReportedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User ID who reported (FK to Users).
        /// </summary>
        [Column("reported_by_id")]
        [Display(Name = "Prijavio korisnik")]
        public int? ReportedById { get; set; }

        /// <summary>
        /// Navigation to reporter user (EF populated).
        /// </summary>
        [ForeignKey(nameof(ReportedById))]
        public virtual User? ReportedBy { get; set; }

        /// <summary>
        /// Reporter name snapshot (maintained even if user is deleted).
        /// </summary>
        [MaxLength(255)]
        [Column("user")]
        public string? ReporterName { get; set; }

        /// <summary>
        /// Severity level (LOW/MEDIUM/HIGH/CRITICAL/GMP).
        /// </summary>
        [Required]
        [MaxLength(16)]
        [Column("severity")]
        [Display(Name = "Težina")]
        public string Severity { get; set; } = "LOW";

        /// <summary>
        /// True if deviation is GMP/Compliance critical.
        /// </summary>
        [Column("is_critical")]
        [Display(Name = "Kritièno za GMP")]
        public bool IsCritical { get; set; }

        /// <summary>
        /// Current status in the workflow.
        /// </summary>
        [Required]
        [MaxLength(255)]
        [Column("status")]
        [Display(Name = "Status")]
        public string Status { get; set; } = DeviationStatus.OPEN.ToString();

        /// <summary>
        /// User ID currently responsible/investigator (FK to Users).
        /// </summary>
        [Column("assigned_investigator_id")]
        [Display(Name = "Istražitelj")]
        public int? AssignedInvestigatorId { get; set; }

        /// <summary>
        /// Navigation property to the system user investigator.
        /// </summary>
        [ForeignKey(nameof(AssignedInvestigatorId))]
        public virtual User? AssignedInvestigator { get; set; }

        /// <summary>
        /// Investigator name (external, if not a system user).
        /// </summary>
        [MaxLength(100)]
        [Column("assigned_investigator_name")]
        public string? AssignedInvestigatorName { get; set; }

        /// <summary>
        /// Date/time investigation started (if any).
        /// </summary>
        [Column("investigation_started_at")]
        public DateTime? InvestigationStartedAt { get; set; }

        /// <summary>
        /// Root cause of deviation (RCA).
        /// </summary>
        [MaxLength(800)]
        [Column("root_cause")]
        [Display(Name = "Korijenski uzrok")]
        public string? RootCause { get; set; }

        /// <summary>
        /// List of corrective actions taken (for display only; see CAPA for full linkage).
        /// </summary>
        [NotMapped]
        public List<string> CorrectiveActions { get; set; } = new();

        /// <summary>
        /// Linked CAPA ID (if investigation led to CAPA creation).
        /// </summary>
        [Column("linked_capa_id")]
        public int? LinkedCapaId { get; set; }

        /// <summary>
        /// CAPA entity navigation property (optional).
        /// </summary>
        [ForeignKey(nameof(LinkedCapaId))]
        public virtual CapaCase? LinkedCapa { get; set; }

        /// <summary>
        /// Closure comment.
        /// </summary>
        [MaxLength(2000)]
        [Column("closure_comment")]
        public string? ClosureComment { get; set; }

        /// <summary>
        /// Date/time deviation closed.
        /// </summary>
        [Column("closed_at")]
        public DateTime? ClosedAt { get; set; }

        /// <summary>
        /// Attachments (file IDs).
        /// </summary>
        [NotMapped]
        public List<int> AttachmentIds { get; set; } = new();

        /// <summary>
        /// List of attached files (audit/forensics).
        /// </summary>
        [NotMapped]
        public List<Attachment> Attachments { get; set; } = new();

        /// <summary>
        /// Digital signature/hash (for 21 CFR Part 11).
        /// </summary>
        [MaxLength(128)]
        [Column("digital_signature")]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Risk assessment score (AI/ML extensible).
        /// </summary>
        [Column("risk_score")]
        [Display(Name = "Risk Score")]
        public int RiskScore { get; set; }

        /// <summary>
        /// AI/ML anomaly/fraud score (optional, for analytics).
        /// </summary>
        [Column("anomaly_score")]
        public double? AnomalyScore { get; set; }

        /// <summary>
        /// Last modification timestamp (audit).
        /// </summary>
        [Column("last_modified")]
        public DateTime? LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who last modified the deviation.
        /// </summary>
        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        /// <summary>
        /// Navigation to last modifying user.
        /// </summary>
        [ForeignKey(nameof(LastModifiedById))]
        public virtual User? LastModifiedBy { get; set; }

        /// <summary>
        /// Source device info or IP (forensics).
        /// </summary>
        [MaxLength(64)]
        [Column("source_ip")]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Inspector/audit note (free text, for QA comments).
        /// </summary>
        [MaxLength(1000)]
        [Column("audit_note")]
        public string? AuditNote { get; set; }

        /// <summary>
        /// List of audit trail records for this deviation (in-memory helper).
        /// </summary>
        [NotMapped]
        public List<DeviationAudit> AuditTrail { get; set; } = new();

        /// <summary>
        /// Custom extension fields (for customer-specific attributes).
        /// </summary>
        [NotMapped]
        public Dictionary<string, string> CustomFields { get; set; } = new();

        /// <summary>
        /// Returns true if closed.
        /// </summary>
        [NotMapped]
        public bool IsClosed => Status == DeviationStatus.CLOSED.ToString();

        /// <summary>
        /// Returns true if investigation started.
        /// </summary>
        [NotMapped]
        public bool IsInInvestigation => Status == DeviationStatus.INVESTIGATION.ToString();

        /// <summary>
        /// Returns true if linked to a CAPA.
        /// </summary>
        [NotMapped]
        public bool IsCapaLinked => LinkedCapaId.HasValue;

        /// <summary>
        /// Returns true if deviation is critical (GMP/Compliance).
        /// </summary>
        [NotMapped]
        public bool IsGmpCritical => IsCritical || (Severity?.ToUpperInvariant() == "CRITICAL");

        public override string ToString()
            => $"[{Code ?? ("DEV-" + Id)}] {Title} – Status: {Status}";
    }
}

