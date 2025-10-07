using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// End-to-end incident record capturing event details, severity, related entities, and closure metadata.
    /// </summary>
    [Table("incidents")]
    public class Incident
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [Required, StringLength(200)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [Required, StringLength(2000)]
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        [StringLength(60)]
        [Column("type")]
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the priority.
        /// </summary>
        [StringLength(20)]
        [Column("priority")]
        public string? Priority { get; set; }

        /// <summary>
        /// Gets or sets the detected at.
        /// </summary>
        [Required]
        [Column("detected_at")]
        public DateTime DetectedAt { get; set; }

        /// <summary>
        /// Gets or sets the reported at.
        /// </summary>
        [Column("reported_at")]
        public DateTime? ReportedAt { get; set; }

        /// <summary>
        /// Gets or sets the reported by id.
        /// </summary>
        [Column("reported_by_id")]
        public int? ReportedById { get; set; }
        /// <summary>
        /// Gets or sets the reported by.
        /// </summary>
        [ForeignKey(nameof(ReportedById))]
        public virtual User? ReportedBy { get; set; }

        /// <summary>
        /// Gets or sets the assigned to id.
        /// </summary>
        [Column("assigned_to_id")]
        public int? AssignedToId { get; set; }
        /// <summary>
        /// Gets or sets the assigned to.
        /// </summary>
        [ForeignKey(nameof(AssignedToId))]
        public virtual User? AssignedTo { get; set; }

        /// <summary>
        /// Gets or sets the work order id.
        /// </summary>
        [Column("work_order_id")]
        public int? WorkOrderId { get; set; }
        /// <summary>
        /// Gets or sets the work order.
        /// </summary>
        public virtual WorkOrder? WorkOrder { get; set; }

        /// <summary>
        /// Gets or sets the capa case id.
        /// </summary>
        [Column("capa_case_id")]
        public int? CapaCaseId { get; set; }
        /// <summary>
        /// Gets or sets the capa case.
        /// </summary>
        [ForeignKey(nameof(CapaCaseId))]
        public virtual CapaCase? CapaCase { get; set; }
        /// <summary>
        /// Gets or sets the actions.
        /// </summary>

        public virtual ICollection<IncidentAction> Actions { get; set; } = new List<IncidentAction>();
        /// <summary>
        /// Gets or sets the attachments.
        /// </summary>
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [StringLength(30)]
        [Column("status")]
        public string Status { get; set; } = "open";

        /// <summary>
        /// Gets or sets the root cause.
        /// </summary>
        [StringLength(200)]
        [Column("root_cause")]
        public string? RootCause { get; set; }

        /// <summary>
        /// Gets or sets the closed at.
        /// </summary>
        [Column("closed_at")]
        public DateTime? ClosedAt { get; set; }

        /// <summary>
        /// Gets or sets the closed by id.
        /// </summary>
        [Column("closed_by_id")]
        public int? ClosedById { get; set; }
        /// <summary>
        /// Gets or sets the closed by.
        /// </summary>
        [ForeignKey(nameof(ClosedById))]
        public virtual User? ClosedBy { get; set; }

        /// <summary>
        /// Gets or sets the digital signature.
        /// </summary>
        [StringLength(128)]
        [Column("digital_signature")]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Gets or sets the last modified.
        /// </summary>
        [Column("last_modified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last modified by id.
        /// </summary>
        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }
        /// <summary>
        /// Gets or sets the last modified by.
        /// </summary>
        [ForeignKey(nameof(LastModifiedById))]
        public virtual User? LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the source ip.
        /// </summary>
        [StringLength(45)]
        [Column("source_ip")]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Gets or sets the notes.
        /// </summary>
        [StringLength(2000)]
        [Column("notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets the is deleted.
        /// </summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Gets or sets the anomaly score.
        /// </summary>
        [Column("anomaly_score")]
        public double? AnomalyScore { get; set; }

        // ======== properties referenced by IncidentService ========

        /// <summary>
        /// Gets or sets the risk level.
        /// </summary>
        [Column("risk_level")]
        public int RiskLevel { get; set; }

        /// <summary>
        /// Gets or sets the assigned investigator.
        /// </summary>
        [StringLength(120)]
        [Column("assigned_investigator")]
        public string? AssignedInvestigator { get; set; }

        /// <summary>
        /// Gets or sets the classification.
        /// </summary>
        [StringLength(40)]
        [Column("classification")]
        public string? Classification { get; set; }

        /// <summary>
        /// Gets or sets the linked deviation id.
        /// </summary>
        [Column("linked_deviation_id")]
        public int? LinkedDeviationId { get; set; }

        /// <summary>
        /// Gets or sets the linked capa id.
        /// </summary>
        [Column("linked_capa_id")]
        public int? LinkedCapaId { get; set; }

        /// <summary>
        /// Gets or sets the closure comment.
        /// </summary>
        [StringLength(1000)]
        [Column("closure_comment")]
        public string? ClosureComment { get; set; }

        /// <summary>
        /// Gets or sets the is critical.
        /// </summary>
        [Column("is_critical")]
        public bool IsCritical { get; set; }

        // Convenience flags
        /// <summary>
        /// Gets or sets the is open.
        /// </summary>
        [NotMapped] public bool IsOpen =>
            string.IsNullOrEmpty(Status) ||
            Status.Equals("open", StringComparison.OrdinalIgnoreCase) ||
            Status.Equals("investigating", StringComparison.OrdinalIgnoreCase);
        /// <summary>
        /// Gets or sets the has actions.
        /// </summary>

        /// <summary>
        /// Gets or sets the has evidence.
        /// </summary>
        [NotMapped] public bool HasActions => Actions?.Count > 0;
        [NotMapped] public bool HasEvidence => Attachments?.Count > 0;
    }
}
