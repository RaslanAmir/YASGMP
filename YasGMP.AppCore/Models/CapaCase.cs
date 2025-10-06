using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>CapaCase</b> (Corrective and Preventive Action) – Ultra-robust central entity for tracking any CAPA in GMP/CMMS.
    /// <para>
    /// ✅ Full traceability: audit, workflow, signatures, timeline, comments, forensics, attachments, workflow history, and advanced risk features.<br/>
    /// ✅ Maximum extensibility for digital, AI, IoT, ML, inspection, and regulatory future needs.
    /// </para>
    /// </summary>
    [Table("capa_cases")]
    public class CapaCase
    {
        /// <summary>Unique identifier for this CAPA case (Primary Key).</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Human-readable CAPA code or reference (for legacy/UI/reporting).</summary>
        [NotMapped]
        public string CapaCode => $"CAPA-{Id:D5}";

        /// <summary>Title of the CAPA case (short summary, required for UI/lookup).</summary>
        [Required]
        [StringLength(200)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>Full description/details of the CAPA case.</summary>
        [Required]
        [StringLength(2000)]
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>Component this CAPA is linked to.</summary>
        [Required]
        [Column("component_id")]
        [Display(Name = "Komponenta")]
        public int ComponentId { get; set; }

        /// <summary>Navigation to linked component (EF will hydrate).</summary>
        [ForeignKey(nameof(ComponentId))]
        public virtual MachineComponent Component { get; set; } = null!;

        /// <summary>Date when CAPA was opened (maps to SQL column 'date_open').</summary>
        [Required]
        [Column("date_open")]
        public DateTime OpenedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date/time CAPA opened (ViewModel compatibility/writable alias).
        /// </summary>
        [NotMapped]
        public DateTime DateOpen
        {
            get => OpenedAt;
            set => OpenedAt = value;
        }

        /// <summary>Date when CAPA was closed (nullable; maps to SQL column 'date_close').</summary>
        [Column("date_close")]
        public DateTime? ClosedAt { get; set; }

        /// <summary>
        /// Date/time CAPA closed (ViewModel compatibility/writable alias).
        /// </summary>
        [NotMapped]
        public DateTime? DateClose
        {
            get => ClosedAt;
            set => ClosedAt = value;
        }

        /// <summary>User assigned to the CAPA (nullable, for workflow).</summary>
        [Column("assigned_to_id")]
        public int? AssignedToId { get; set; }

        /// <summary>Navigation to assigned user (EF will hydrate).</summary>
        [ForeignKey(nameof(AssignedToId))]
        public virtual User AssignedTo { get; set; } = null!;

        /// <summary>CAPA priority (critical, high, medium, low).</summary>
        [StringLength(30)]
        [Column("priority")]
        public string Priority { get; set; } = string.Empty;

        /// <summary>Current CAPA status (open, in progress, closed, etc).</summary>
        [Required]
        [StringLength(30)]
        [Column("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>Root cause analysis summary (required for closure/ISO compliance).</summary>
        [StringLength(2000)]
        [Column("root_cause")]
        public string RootCause { get; set; } = string.Empty;

        /// <summary>Corrective action(s) implemented.</summary>
        [StringLength(2000)]
        [Column("corrective_action")]
        public string CorrectiveAction { get; set; } = string.Empty;

        /// <summary>Preventive action(s) implemented.</summary>
        [StringLength(2000)]
        [Column("preventive_action")]
        public string PreventiveAction { get; set; } = string.Empty;

        /// <summary>Reason for CAPA (legacy compatibility, UI display).</summary>
        [StringLength(500)]
        [Column("reason")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>Actions taken (legacy/compatibility with old data).</summary>
        [StringLength(2000)]
        [Column("actions")]
        public string Actions { get; set; } = string.Empty;

        /// <summary>Main documentation file or JSON array of doc files.</summary>
        [StringLength(255)]
        [Column("doc_file")]
        public string DocFile { get; set; } = string.Empty;

        /// <summary>Cryptographic digital signature for audit integrity.</summary>
        [StringLength(128)]
        [Column("digital_signature")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Date and time of last modification (auto audit).</summary>
        [Column("last_modified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>User who last modified this CAPA.</summary>
        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        /// <summary>Navigation to user who last modified (EF will hydrate).</summary>
        [ForeignKey(nameof(LastModifiedById))]
        public virtual User LastModifiedBy { get; set; } = null!;

        /// <summary>Approval status (for regulatory/forensic compliance).</summary>
        [Column("approved")]
        public bool Approved { get; set; }

        /// <summary>Date/time when CAPA was approved.</summary>
        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        /// <summary>User who approved this CAPA.</summary>
        [Column("approved_by_id")]
        public int? ApprovedById { get; set; }

        /// <summary>Navigation to approving user (EF will hydrate).</summary>
        [ForeignKey(nameof(ApprovedById))]
        public virtual User ApprovedBy { get; set; } = null!;

        /// <summary>Source IP address or device for forensics/audit.</summary>
        [StringLength(45)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Normalized status ID (FK to ref_value in SQL; nullable).
        /// Present in SQL as 'status_id'. Optional here for schema parity.
        /// </summary>
        [Column("status_id")]
        public int? StatusId { get; set; }

        /// <summary>Linked root cause reference (legacy/compatibility field).</summary>
        [StringLength(200)]
        [Column("root_cause_reference")]
        public string RootCauseReference { get; set; } = string.Empty;

        /// <summary>Linked finding(s), e.g., deviations, audit findings, etc.</summary>
        [StringLength(200)]
        [Column("linked_findings")]
        public string LinkedFindings { get; set; } = string.Empty;

        /// <summary>
        /// All user/internal notes and comments (JSON or long text).
        /// </summary>
        [StringLength(4000)]
        [Column("notes")]
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// List of review/approval comments or regulatory notes (future use).
        /// </summary>
        [StringLength(2000)]
        [Column("comments")]
        public string Comments { get; set; } = string.Empty;

        /// <summary>Change version (for rollback/GMP change control).</summary>
        [Column("change_version")]
        public int ChangeVersion { get; set; } = 1;

        /// <summary>Is this record soft-deleted (GDPR/archive).</summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        // ------------------------ EXTENSIONS FOR VIEWMODEL/UI ------------------------

        /// <summary>
        /// List of file attachments (JSON, serialized, or direct reference).
        /// </summary>
        [NotMapped]
        public List<string> Attachments { get; set; } = new List<string>();

        /// <summary>
        /// Type of CAPA (e.g., Corrective, Preventive, Systemic, Process, Supplier, etc).
        /// </summary>
        [NotMapped]
        public string CAPAType { get; set; } = string.Empty;

        /// <summary>
        /// Device or workstation info for forensic/audit purposes.
        /// </summary>
        [NotMapped]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>
        /// When the CAPA was formally initiated.
        /// </summary>
        [NotMapped]
        public DateTime? InitiatedAt { get; set; }

        /// <summary>
        /// User who initiated the CAPA.
        /// </summary>
        [NotMapped]
        public int? InitiatedBy { get; set; }

        /// <summary>
        /// IP Address (redundant/bonus for quick access in UI).
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
        /// Is the CAPA assessed as effective? (for effectiveness checks)
        /// </summary>
        [NotMapped]
        public bool? IsEffective { get; set; }

        /// <summary>
        /// Risk rating (qualitative/category: Low/Medium/High/Critical).
        /// </summary>
        [NotMapped]
        public string RiskRating { get; set; } = string.Empty;

        /// <summary>
        /// Type for ViewModel/enum compatibility, mapped from CAPAType if needed.
        /// </summary>
        [NotMapped]
        public string Type => CAPAType;

        /// <summary>
        /// Returns a summary for diagnostics/logging.
        /// </summary>
        public override string ToString()
        {
            return $"CAPA {Title} (Status: {Status}, AssignedTo: {AssignedToId}, Opened: {OpenedAt:yyyy-MM-dd})";
        }
    }
}

