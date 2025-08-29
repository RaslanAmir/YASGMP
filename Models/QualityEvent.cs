using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>QualityEvent</b> – Comprehensive GMP/CMMS record of all quality-related events (deviations, complaints, recalls, OOS, change control, audit, training, etc).
    /// <para>
    /// ✅ Tracks all required data for root cause analysis, compliance, audit, recall, and reporting.<br/>
    /// ✅ Fully supports attachments, signatures, cross-linking to machines/components, and workflow.
    /// </para>
    /// </summary>
    public class QualityEvent
    {
        /// <summary>
        /// Unique identifier for the quality event (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Type of event (deviation, complaint, recall, out_of_spec, change_control, audit, training).
        /// </summary>
        [Required]
        [Display(Name = "Vrsta događaja")]
        public QualityEventType EventType { get; set; }

        /// <summary>
        /// Date when the event was opened/recorded.
        /// </summary>
        [Display(Name = "Datum otvaranja")]
        public DateTime? DateOpen { get; set; }

        /// <summary>
        /// Date when the event was closed/resolved (if applicable).
        /// </summary>
        [Display(Name = "Datum zatvaranja")]
        public DateTime? DateClose { get; set; }

        /// <summary>
        /// Detailed description of the event, incident, or issue.
        /// </summary>
        [Display(Name = "Opis događaja")]
        [MaxLength(2000)]
        public string? Description { get; set; }

        /// <summary>
        /// Foreign key to the related machine (if any).
        /// </summary>
        [Display(Name = "Stroj / Oprema")]
        public int? RelatedMachineId { get; set; }

        /// <summary>
        /// Navigation property to the related machine/asset.
        /// </summary>
        public Machine? RelatedMachine { get; set; }

        /// <summary>
        /// Foreign key to the related component (if any).
        /// </summary>
        [Display(Name = "Komponenta")]
        public int? RelatedComponentId { get; set; }

        /// <summary>
        /// Navigation property to the related component.
        /// </summary>
        public MachineComponent? RelatedComponent { get; set; }

        /// <summary>
        /// Current status of the quality event (open, closed, under_review).
        /// </summary>
        [Required]
        [Display(Name = "Status događaja")]
        public QualityEventStatus Status { get; set; }

        /// <summary>
        /// Actions taken or planned (for closure, CAPA, etc).
        /// </summary>
        [Display(Name = "Akcije / Korektivne mjere")]
        [MaxLength(2000)]
        public string? Actions { get; set; }

        /// <summary>
        /// File path to supporting documentation (CAPA form, complaint evidence, etc).
        /// </summary>
        [Display(Name = "Dokumentacija")]
        [MaxLength(255)]
        public string? DocFile { get; set; }

        /// <summary>
        /// Digital signature hash (if signed).
        /// </summary>
        [Display(Name = "Digitalni potpis")]
        [MaxLength(128)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// User who created this event (optional, for tracking/audit).
        /// </summary>
        public int? CreatedById { get; set; }

        /// <summary>
        /// Navigation property for the user who created the event.
        /// </summary>
        public User? CreatedBy { get; set; }

        /// <summary>
        /// Last user who modified this record.
        /// </summary>
        public int? LastModifiedById { get; set; }

        /// <summary>
        /// Navigation property for the user who last modified the event.
        /// </summary>
        public User? LastModifiedBy { get; set; }

        /// <summary>
        /// Date/time this record was last modified (for audit trail).
        /// </summary>
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
