using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>QualityEvent</b> - Comprehensive GMP/CMMS record of quality-related events
    /// (deviations, complaints, recalls, OOS, change control, audit, training, etc.).
    /// Tracks data needed for root cause analysis, compliance, audit, recall, and reporting.
    /// Supports attachments, signatures, cross-linking to machines/components, and workflow.
    /// </summary>
    [Table("quality_events")]
    public partial class QualityEvent
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [NotMapped]
        [Display(Name = "Vrsta dogaðaja")]
        public QualityEventType EventType
        {
            get => _eventType;
            set => _eventType = value;
        }
        private QualityEventType _eventType;

        [Column("date_open")]
        [Display(Name = "Datum otvaranja")]
        public DateTime? DateOpen { get; set; }

        [Column("date_close")]
        [Display(Name = "Datum zatvaranja")]
        public DateTime? DateClose { get; set; }

        [Column("description")]
        [Display(Name = "Opis dogaðaja")]
        [MaxLength(2000)]
        public string? Description { get; set; }

        [Column("related_machine_id")]
        [Display(Name = "Stroj / Oprema")]
        public int? RelatedMachineId { get; set; }

        [ForeignKey(nameof(RelatedMachineId))]
        public virtual Machine? RelatedMachine { get; set; }

        [Column("related_component_id")]
        [Display(Name = "Komponenta")]
        public int? RelatedComponentId { get; set; }

        [ForeignKey(nameof(RelatedComponentId))]
        public virtual MachineComponent? RelatedComponent { get; set; }

        [Required]
        [NotMapped]
        [Display(Name = "Status dogaðaja")]
        public QualityEventStatus Status
        {
            get => _status;
            set => _status = value;
        }
        private QualityEventStatus _status;

        [Column("actions")]
        [Display(Name = "Akcije / Korektivne mjere")]
        [MaxLength(2000)]
        public string? Actions { get; set; }

        [Column("doc_file")]
        [Display(Name = "Dokumentacija")]
        [MaxLength(255)]
        public string? DocFile { get; set; }

        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        [MaxLength(128)]
        public string? DigitalSignature { get; set; }

        [Column("created_by_id")]
        public int? CreatedById { get; set; }

        [ForeignKey(nameof(CreatedById))]
        public virtual User? CreatedBy { get; set; }

        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        [ForeignKey(nameof(LastModifiedById))]
        public virtual User? LastModifiedBy { get; set; }

        [Column("last_modified")]
        [Display(Name = "Zadnja izmjena")]
        public DateTime? LastModified { get; set; }
    }

    /// <summary>
    /// Enumeration of all possible types of quality events.
    /// </summary>
    public enum QualityEventType
    {
        Deviation,
        Complaint,
        Recall,
        OutOfSpec,
        ChangeControl,
        Audit,
        Training,
        Other
    }

    /// <summary>
    /// Enumeration of all possible statuses for a quality event.
    /// </summary>
    public enum QualityEventStatus
    {
        Open,
        Closed,
        UnderReview
    }
}
