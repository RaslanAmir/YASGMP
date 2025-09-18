using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>ChangeControl</b> – Ultra-robust, GMP/ISO/21 CFR Part 11/Annex 11 compliant entity for recording change controls (requests, reviews, approvals, implementation).
    /// <para>
    /// ✅ Tracks entire change lifecycle, reason, impact, risk, linked equipment, SOPs, CAPA, digital signatures, attachments, workflow, audit, and more.<br/>
    /// ✅ ViewModel-ready: Includes all extension/virtual properties for dashboard, reporting, and ML/IoT integrations.
    /// </para>
    /// </summary>
    public class ChangeControl
    {
        /// <summary>
        /// Unique identifier for this change control record (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Internal change control code (e.g. CC-2024-001).
        /// </summary>
        [Required]
        [MaxLength(40)]
        [Display(Name = "Šifra promjene")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Short summary/title of the change request.
        /// </summary>
        [Required]
        [MaxLength(200)]
        [Display(Name = "Naslov / Sažetak")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the proposed change.
        /// </summary>
        [Required]
        [MaxLength(2000)]
        [Display(Name = "Opis promjene")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Date/time the change was requested.
        /// </summary>
        [Display(Name = "Datum zahtjeva")]
        public DateTime? DateRequested { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who submitted the change request.
        /// </summary>
        public int? RequestedById { get; set; }
        public User RequestedBy { get; set; } = null!;

        /// <summary>
        /// Foreign key to the machine/equipment affected (optional).
        /// </summary>
        public int? MachineId { get; set; }
        public Machine Machine { get; set; } = null!;

        /// <summary>
        /// Foreign key to the affected component (optional).
        /// </summary>
        public int? ComponentId { get; set; }
        public MachineComponent Component { get; set; } = null!;

        /// <summary>
        /// Reason for change (regulatory, CAPA, improvement, incident, periodic review, etc).
        /// </summary>
        [MaxLength(512)]
        [Display(Name = "Razlog promjene")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Impact assessment (on product, process, validation, regulatory, quality, etc).
        /// </summary>
        [MaxLength(2000)]
        [Display(Name = "Procjena utjecaja")]
        public string ImpactAssessment { get; set; } = string.Empty;

        /// <summary>
        /// Risk assessment summary.
        /// </summary>
        [MaxLength(1000)]
        [Display(Name = "Procjena rizika")]
        public string RiskAssessment { get; set; } = string.Empty;

        /// <summary>
        /// Linked CAPA case, if applicable.
        /// </summary>
        public int? CapaCaseId { get; set; }
        public CapaCase CapaCase { get; set; } = null!;

        /// <summary>
        /// Status of the change control (draft, under_review, approved, implemented, rejected, closed).
        /// </summary>
        [Required]
        [Display(Name = "Status")]
        public ChangeControlStatus Status { get; set; } = ChangeControlStatus.Draft;

        /// <summary>
        /// Current assigned user (for workflow routing).
        /// </summary>
        public int? AssignedToId { get; set; }
        public User AssignedTo { get; set; } = null!;

        /// <summary>
        /// Date/time when the change control was assigned to the current owner.
        /// </summary>
        [Display(Name = "Datum dodjele")]
        public DateTime? DateAssigned { get; set; }

        /// <summary>
        /// Date when change was reviewed.
        /// </summary>
        [Display(Name = "Datum pregleda")]
        public DateTime? DateReviewed { get; set; }

        /// <summary>
        /// Reviewer user (could be QA, regulatory, responsible).
        /// </summary>
        public int? ReviewedById { get; set; }
        public User ReviewedBy { get; set; } = null!;

        /// <summary>
        /// Review/approval notes.
        /// </summary>
        [MaxLength(1000)]
        [Display(Name = "Napomena pregleda")]
        public string ReviewNotes { get; set; } = string.Empty;

        /// <summary>
        /// Date when change was approved.
        /// </summary>
        [Display(Name = "Datum odobrenja")]
        public DateTime? DateApproved { get; set; }

        /// <summary>
        /// Approver user.
        /// </summary>
        public int? ApprovedById { get; set; }
        public User ApprovedBy { get; set; } = null!;

        /// <summary>
        /// Date when implementation was completed.
        /// </summary>
        [Display(Name = "Datum implementacije")]
        public DateTime? DateImplemented { get; set; }

        /// <summary>
        /// Implementation notes / evidence.
        /// </summary>
        [MaxLength(1000)]
        [Display(Name = "Napomena implementacije")]
        public string ImplementationNotes { get; set; } = string.Empty;

        /// <summary>
        /// Supporting documentation file (SOP, protocol, evidence, etc).
        /// </summary>
        [MaxLength(255)]
        [Display(Name = "Dokumentacija")]
        public string DocFile { get; set; } = string.Empty;

        /// <summary>
        /// Digital signature hash for regulatory sign-off.
        /// </summary>
        [MaxLength(128)]
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Date/time of last modification (for full audit trace).
        /// </summary>
        [Display(Name = "Zadnja izmjena")]
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// User who last modified the record.
        /// </summary>
        public int? LastModifiedById { get; set; }
        public User LastModifiedBy { get; set; } = null!;

        /// <summary>
        /// All attached files (navigation for UI/file management, optional).
        /// </summary>
        public List<Attachment> Attachments { get; set; } = new();

        // --------- EXTENSIONS, VIEWMODEL/DTO-ONLY & BONUS FUTURE FIELDS ---------

        /// <summary>
        /// Type of change (e.g., Document, Equipment, Process, Software, SOP, Facility, etc).
        /// </summary>
        [NotMapped]
        public string ChangeType { get; set; } = string.Empty;

        /// <summary>
        /// Device or workstation info for forensics/audit.
        /// </summary>
        [NotMapped]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>
        /// Short summary of expected/actual impact (can be mapped from ImpactAssessment).
        /// </summary>
        [NotMapped]
        public string ImpactSummary { get; set; } = string.Empty;

        /// <summary>
        /// Date/time when the change was formally initiated (for workflows).
        /// </summary>
        [NotMapped]
        public DateTime? InitiatedAt { get; set; }

        /// <summary>
        /// User who initiated the change.
        /// </summary>
        [NotMapped]
        public int? InitiatedBy { get; set; }

        /// <summary>
        /// Initiator navigation property (optional for UI).
        /// </summary>
        [NotMapped]
        public User InitiatedByUser { get; set; } = null!;

        /// <summary>
        /// IP Address for the change (bonus for quick forensic filtering).
        /// </summary>
        [NotMapped]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// Computed/assigned risk score (0.0–1.0 or custom scale).
        /// </summary>
        [NotMapped]
        public double? RiskScore { get; set; }

        /// <summary>
        /// Session or workflow instance identifier.
        /// </summary>
        [NotMapped]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// JSON or object list of workflow steps and events.
        /// </summary>
        [NotMapped]
        public string WorkflowHistory { get; set; } = string.Empty;

        /// <summary>
        /// For UI compatibility, mapped from Code for legacy/alias code.
        /// </summary>
        [NotMapped]
        public string ChangeCode => Code;

        /// <summary>
        /// For UI compatibility, maps to DateRequested for legacy/alias.
        /// </summary>
        [NotMapped]
        public DateTime? DateOpen => DateRequested;

        /// <summary>
        /// For UI compatibility, maps to DateImplemented for legacy/alias.
        /// </summary>
        [NotMapped]
        public DateTime? DateClose => DateImplemented;

        /// <summary>
        /// For ViewModel/DTO compatibility, returns IsEffective (custom logic or status check).
        /// </summary>
        [NotMapped]
        public bool IsEffective => Status == ChangeControlStatus.Implemented || Status == ChangeControlStatus.Closed;

        /// <summary>
        /// Diagnostic summary for debug/logging.
        /// </summary>
        public override string ToString()
        {
            return $"ChangeControl {Code} (Status: {Status}, Title: {Title}, Opened: {DateRequested:yyyy-MM-dd})";
        }
    }

    /// <summary>
    /// Statuses of a change control record (for workflow/routing).
    /// </summary>
    public enum ChangeControlStatus
    {
        Draft,
        UnderReview,
        Approved,
        Implemented,
        Rejected,
        Closed
    }
}
