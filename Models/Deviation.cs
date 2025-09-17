using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YasGMP.Models.Enums;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Deviation</b> — Ultra-mega robust GMP deviation/non-conformance record (audit, workflow, root cause, CAPA, digital signature, AI-ready).
    /// <para>Supports complete GMP/Annex 11/21 CFR Part 11 requirements, forensics, workflow, advanced escalation, custom fields, attachments, and AI/ML hooks.</para>
    /// </summary>
    public class Deviation
    {
        /// <summary>
        /// Unique identifier for the deviation (Primary Key).
        /// </summary>
        [Key]
        [Display(Name = "ID odstupanja")]
        public int Id { get; set; }

        /// <summary>
        /// Short, unique code (e.g., DEVI-2025-001).
        /// </summary>
        [MaxLength(40)]
        [Display(Name = "Kod odstupanja")]
        public string? Code { get; set; }

        /// <summary>
        /// Title/subject of deviation (required).
        /// </summary>
        [Required]
        [MaxLength(200)]
        [Display(Name = "Naslov odstupanja")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Free text deviation description (required).
        /// </summary>
        [Required]
        [MaxLength(4000)]
        [Display(Name = "Opis odstupanja")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Date/time when deviation was reported (UTC).
        /// </summary>
        [Required]
        [Display(Name = "Vrijeme prijave")]
        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User ID who reported (FK to Users).
        /// </summary>
        [Required]
        [Display(Name = "Prijavio korisnik")]
        public int ReportedById { get; set; }

        /// <summary>
        /// Navigation to reporter user (EF populated).
        /// </summary>
        public virtual User ReportedBy { get; set; } = null!;

        /// <summary>
        /// Severity level (LOW/MEDIUM/HIGH/CRITICAL/GMP).
        /// </summary>
        [Required]
        [MaxLength(16)]
        [Display(Name = "Težina")]
        public string Severity { get; set; } = "LOW";

        /// <summary>
        /// True if deviation is GMP/Compliance critical.
        /// </summary>
        [Display(Name = "Kritično za GMP")]
        public bool IsCritical { get; set; }

        /// <summary>
        /// Current status in the workflow.
        /// </summary>
        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = DeviationStatus.OPEN.ToString();

        /// <summary>
        /// User ID currently responsible/investigator (FK to Users).
        /// </summary>
        [Display(Name = "Istražitelj")]
        public int? AssignedInvestigatorId { get; set; }

        /// <summary>
        /// Navigation property to the system user investigator.
        /// </summary>
        public virtual User? AssignedInvestigator { get; set; }

        /// <summary>
        /// Investigator name (external, if not a system user).
        /// </summary>
        [MaxLength(100)]
        public string? AssignedInvestigatorName { get; set; }

        /// <summary>
        /// Date/time investigation started (if any).
        /// </summary>
        public DateTime? InvestigationStartedAt { get; set; }

        /// <summary>
        /// Root cause of deviation (RCA).
        /// </summary>
        [MaxLength(800)]
        [Display(Name = "Korijenski uzrok")]
        public string? RootCause { get; set; }

        /// <summary>
        /// List of corrective actions taken (for display only; see CAPA for full linkage).
        /// </summary>
        public List<string> CorrectiveActions { get; set; } = new();

        /// <summary>
        /// Linked CAPA ID (if investigation led to CAPA creation).
        /// </summary>
        public int? LinkedCapaId { get; set; }

        /// <summary>
        /// CAPA entity navigation property (optional).
        /// </summary>
        public virtual CapaCase? LinkedCapa { get; set; }

        /// <summary>
        /// Closure comment.
        /// </summary>
        [MaxLength(2000)]
        public string? ClosureComment { get; set; }

        /// <summary>
        /// Date/time deviation closed.
        /// </summary>
        public DateTime? ClosedAt { get; set; }

        /// <summary>
        /// Attachments (file IDs).
        /// </summary>
        public List<int> AttachmentIds { get; set; } = new();

        /// <summary>
        /// List of attached files (audit/forensics).
        /// </summary>
        public List<Attachment> Attachments { get; set; } = new();

        /// <summary>
        /// Digital signature/hash (for 21 CFR Part 11).
        /// </summary>
        [MaxLength(128)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Risk assessment score (AI/ML extensible).
        /// </summary>
        [Display(Name = "Risk Score")]
        public int RiskScore { get; set; }

        /// <summary>
        /// AI/ML anomaly/fraud score (optional, for analytics).
        /// </summary>
        public double? AnomalyScore { get; set; }

        /// <summary>
        /// Last modification timestamp (audit).
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who last modified the deviation.
        /// </summary>
        public int? LastModifiedById { get; set; }
        public virtual User? LastModifiedBy { get; set; }

        /// <summary>
        /// Source device info or IP (forensics).
        /// </summary>
        [MaxLength(64)]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Inspector/audit note (free text, for QA comments).
        /// </summary>
        [MaxLength(1000)]
        public string? AuditNote { get; set; }

        /// <summary>
        /// List of audit trail records for this deviation.
        /// </summary>
        public List<DeviationAudit> AuditTrail { get; set; } = new();

        /// <summary>
        /// Custom extension fields (for customer-specific attributes).
        /// </summary>
        [NotMapped]
        public Dictionary<string, string> CustomFields { get; set; } = new();

        /// <summary>
        /// Returns true if closed.
        /// </summary>
        public bool IsClosed => Status == DeviationStatus.CLOSED.ToString();

        /// <summary>
        /// Returns true if investigation started.
        /// </summary>
        public bool IsInInvestigation => Status == DeviationStatus.INVESTIGATION.ToString();

        /// <summary>
        /// Returns true if linked to a CAPA.
        /// </summary>
        public bool IsCapaLinked => LinkedCapaId.HasValue;

        /// <summary>
        /// Returns true if deviation is critical (GMP/Compliance).
        /// </summary>
        public bool IsGmpCritical => IsCritical || (Severity?.ToUpperInvariant() == "CRITICAL");

        public override string ToString()
            => $"[{Code ?? ("DEV-" + Id)}] {Title} — Status: {Status}";
    }
}
