using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents a record of any GMP inspection (HALMED, internal, external, validation, etc.).
    /// Tracks results, documentation, audit trail, digital signatures, and links to related equipment or CAPA cases.
    /// </summary>
    /// <remarks>
    /// Digital signature and forensic fields; CAPA/audit finding linkage; 21 CFR Part 11/Annex 11/HALMED traceability;
    /// extendable for attachments, workflow approvals, and AI-based analysis.
    /// </remarks>
    [Table("inspection")]
    public class Inspection
    {
        /// <summary>
        /// Unique inspection identifier (Primary Key).
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Date when the inspection was performed.
        /// </summary>
        [Required]
        [Column("inspection_date")]
        [Display(Name = "Datum inspekcije")]
        public DateTime InspectionDate { get; set; }

        /// <summary>
        /// Name of the inspector or inspecting organization.
        /// </summary>
        [Required]
        [StringLength(100)]
        [Column("inspector_name")]
        [Display(Name = "Inspektor/Organizacija")]
        public string InspectorName { get; set; } = string.Empty;

        /// <summary>
        /// Type of inspection (e.g., HALMED, internal, external, validation, audit).
        /// </summary>
        [Required]
        [StringLength(30)]
        [Column("type")]
        [Display(Name = "Vrsta inspekcije")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Outcome of the inspection (pass, fail, remark).
        /// </summary>
        [Required]
        [StringLength(20)]
        [Column("result")]
        [Display(Name = "Rezultat")]
        public string Result { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to a related machine or equipment (nullable).
        /// </summary>
        [Column("related_machine_id")]
        [Display(Name = "Stroj/oprema")]
        public int? RelatedMachineId { get; set; }

        /// <summary>
        /// Navigation to the related machine or equipment.
        /// </summary>
        [ForeignKey(nameof(RelatedMachineId))]
        public virtual Machine? RelatedMachine { get; set; }

        /// <summary>
        /// File path or URL to the inspection documentation (PDF, image, etc.).
        /// </summary>
        [Column("doc_file")]
        [Display(Name = "Dokumentacija")]
        public string DocFile { get; set; } = string.Empty;

        /// <summary>
        /// Detailed notes or remarks from the inspection (up to 1000 chars).
        /// </summary>
        [Column("notes")]
        [StringLength(1000)]
        [Display(Name = "Napomene")]
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Digital signature of the inspector or user who created/updated this record.
        /// </summary>
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp of the last modification for audit tracking.
        /// </summary>
        [Column("last_modified")]
        [Display(Name = "Zadnja izmjena")]
        public DateTime LastModified { get; set; }

        /// <summary>
        /// ID of the user who last modified this inspection.
        /// </summary>
        [Column("last_modified_by_id")]
        [Display(Name = "Zadnji izmijenio")]
        public int LastModifiedById { get; set; }

        /// <summary>
        /// Navigation to the user who last modified this inspection.
        /// </summary>
        [ForeignKey(nameof(LastModifiedById))]
        public virtual User? LastModifiedBy { get; set; }

        /// <summary>
        /// IP address or device identifier for the last modification.
        /// </summary>
        [Column("source_ip")]
        [Display(Name = "IP adresa")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to a CAPA case if corrective action was initiated post-inspection.
        /// </summary>
        [Column("capa_case_id")]
        public int? CapaCaseId { get; set; }

        /// <summary>
        /// Navigation to the linked CAPA case.
        /// </summary>
        [ForeignKey(nameof(CapaCaseId))]
        public virtual CapaCase? CapaCase { get; set; }

        /// <summary>
        /// Reference code for an audit finding related to this inspection.
        /// </summary>
        [Column("audit_finding_ref")]
        [StringLength(100)]
        public string AuditFindingRef { get; set; } = string.Empty;

        /// <summary>
        /// JSON payload storing file attachments (photos, certificates, evidence).
        /// </summary>
        [Column("attachments_json")]
        public string AttachmentsJson { get; set; } = string.Empty;

        /// <summary>
        /// JSON payload representing the multi-level approval workflow and e-signatures.
        /// </summary>
        [Column("approval_workflow_json")]
        public string ApprovalWorkflowJson { get; set; } = string.Empty;

        /// <summary>
        /// JSON payload of AI analysis results or flags for rapid inspection review.
        /// </summary>
        [Column("ai_analysis")]
        public string AiAnalysis { get; set; } = string.Empty;
    }
}
