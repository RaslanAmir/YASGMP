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
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required, StringLength(200)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [StringLength(60)]
        [Column("type")]
        public string? Type { get; set; }

        [StringLength(20)]
        [Column("priority")]
        public string? Priority { get; set; }

        [Required]
        [Column("detected_at")]
        public DateTime DetectedAt { get; set; }

        [Column("reported_at")]
        public DateTime? ReportedAt { get; set; }

        [Column("reported_by_id")]
        public int? ReportedById { get; set; }
        [ForeignKey(nameof(ReportedById))]
        public virtual User? ReportedBy { get; set; }

        [Column("assigned_to_id")]
        public int? AssignedToId { get; set; }
        [ForeignKey(nameof(AssignedToId))]
        public virtual User? AssignedTo { get; set; }

        [Column("work_order_id")]
        public int? WorkOrderId { get; set; }
        public virtual WorkOrder? WorkOrder { get; set; }

        [Column("capa_case_id")]
        public int? CapaCaseId { get; set; }
        [ForeignKey(nameof(CapaCaseId))]
        public virtual CapaCase? CapaCase { get; set; }

        public virtual ICollection<IncidentAction> Actions { get; set; } = new List<IncidentAction>();
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

        [StringLength(30)]
        [Column("status")]
        public string Status { get; set; } = "open";

        [StringLength(200)]
        [Column("root_cause")]
        public string? RootCause { get; set; }

        [Column("closed_at")]
        public DateTime? ClosedAt { get; set; }

        [Column("closed_by_id")]
        public int? ClosedById { get; set; }
        [ForeignKey(nameof(ClosedById))]
        public virtual User? ClosedBy { get; set; }

        [StringLength(128)]
        [Column("digital_signature")]
        public string? DigitalSignature { get; set; }

        [Column("last_modified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }
        [ForeignKey(nameof(LastModifiedById))]
        public virtual User? LastModifiedBy { get; set; }

        [StringLength(45)]
        [Column("source_ip")]
        public string? SourceIp { get; set; }

        [StringLength(2000)]
        [Column("notes")]
        public string? Notes { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("anomaly_score")]
        public double? AnomalyScore { get; set; }

        // ======== properties referenced by IncidentService ========

        [Column("risk_level")]
        public int RiskLevel { get; set; }

        [StringLength(120)]
        [Column("assigned_investigator")]
        public string? AssignedInvestigator { get; set; }

        [StringLength(40)]
        [Column("classification")]
        public string? Classification { get; set; }

        [Column("linked_deviation_id")]
        public int? LinkedDeviationId { get; set; }

        [Column("linked_capa_id")]
        public int? LinkedCapaId { get; set; }

        [StringLength(1000)]
        [Column("closure_comment")]
        public string? ClosureComment { get; set; }

        [Column("is_critical")]
        public bool IsCritical { get; set; }

        // Convenience flags
        [NotMapped] public bool IsOpen =>
            string.IsNullOrEmpty(Status) ||
            Status.Equals("open", StringComparison.OrdinalIgnoreCase) ||
            Status.Equals("investigating", StringComparison.OrdinalIgnoreCase);

        [NotMapped] public bool HasActions => Actions?.Count > 0;
        [NotMapped] public bool HasEvidence => Attachments?.Count > 0;
    }
}
