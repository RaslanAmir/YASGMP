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
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Represents the event type value.
        /// </summary>
        [Required]
        [NotMapped]
        [Display(Name = "Vrsta dogaaja")]
        public QualityEventType EventType
        {
            get => _eventType;
            set => _eventType = value;
        }
        private QualityEventType _eventType;

        /// <summary>
        /// Gets or sets the date open.
        /// </summary>
        [Column("date_open")]
        [Display(Name = "Datum otvaranja")]
        public DateTime? DateOpen { get; set; }

        /// <summary>
        /// Gets or sets the date close.
        /// </summary>
        [Column("date_close")]
        [Display(Name = "Datum zatvaranja")]
        public DateTime? DateClose { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [Column("description")]
        [Display(Name = "Opis dogaaja")]
        [MaxLength(2000)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the related machine id.
        /// </summary>
        [Column("related_machine_id")]
        [Display(Name = "Stroj / Oprema")]
        public int? RelatedMachineId { get; set; }

        /// <summary>
        /// Gets or sets the related machine.
        /// </summary>
        [ForeignKey(nameof(RelatedMachineId))]
        public virtual Machine? RelatedMachine { get; set; }

        /// <summary>
        /// Gets or sets the related component id.
        /// </summary>
        [Column("related_component_id")]
        [Display(Name = "Komponenta")]
        public int? RelatedComponentId { get; set; }

        /// <summary>
        /// Gets or sets the related component.
        /// </summary>
        [ForeignKey(nameof(RelatedComponentId))]
        public virtual MachineComponent? RelatedComponent { get; set; }

        /// <summary>
        /// Represents the status value.
        /// </summary>
        [Required]
        [NotMapped]
        [Display(Name = "Status dogaaja")]
        public QualityEventStatus Status
        {
            get => _status;
            set => _status = value;
        }
        private QualityEventStatus _status;

        /// <summary>
        /// Gets or sets the actions.
        /// </summary>
        [Column("actions")]
        [Display(Name = "Akcije / Korektivne mjere")]
        [MaxLength(2000)]
        public string? Actions { get; set; }

        /// <summary>
        /// Gets or sets the doc file.
        /// </summary>
        [Column("doc_file")]
        [Display(Name = "Dokumentacija")]
        [MaxLength(255)]
        public string? DocFile { get; set; }

        /// <summary>
        /// Gets or sets the digital signature.
        /// </summary>
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        [MaxLength(128)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Gets or sets the created by id.
        /// </summary>
        [Column("created_by_id")]
        public int? CreatedById { get; set; }

        /// <summary>
        /// Gets or sets the created by.
        /// </summary>
        [ForeignKey(nameof(CreatedById))]
        public virtual User? CreatedBy { get; set; }

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
        /// Gets or sets the last modified.
        /// </summary>
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
