using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents the Preventive Maintenance Plan.
    /// </summary>
    [Table("preventive_maintenance_plans")]
    public partial class PreventiveMaintenancePlan
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        [Column("code")]
        [MaxLength(50)]
        public string? Code { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Column("name")]
        [MaxLength(100)]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the machine id.
        /// </summary>
        [Column("machine_id")]
        public int? MachineId { get; set; }

        /// <summary>
        /// Gets or sets the machine.
        /// </summary>
        [ForeignKey(nameof(MachineId))]
        public Machine? Machine { get; set; }

        /// <summary>
        /// Gets or sets the component id.
        /// </summary>
        [Column("component_id")]
        public int? ComponentId { get; set; }

        /// <summary>
        /// Gets or sets the component.
        /// </summary>
        [ForeignKey(nameof(ComponentId))]
        public MachineComponent? Component { get; set; }

        /// <summary>
        /// Gets or sets the frequency.
        /// </summary>
        [Column("frequency")]
        [MaxLength(40)]
        public string? Frequency { get; set; }

        /// <summary>
        /// Gets or sets the checklist file.
        /// </summary>
        [Column("checklist_file")]
        [MaxLength(255)]
        public string? ChecklistFile { get; set; }

        /// <summary>
        /// Gets or sets the responsible user id.
        /// </summary>
        [Column("responsible_user_id")]
        public int? ResponsibleUserId { get; set; }

        /// <summary>
        /// Gets or sets the responsible user.
        /// </summary>
        [ForeignKey(nameof(ResponsibleUserId))]
        public User? ResponsibleUser { get; set; }

        /// <summary>
        /// Gets or sets the last executed.
        /// </summary>
        [Column("last_executed")]
        public DateTime? LastExecuted { get; set; }

        /// <summary>
        /// Gets or sets the next due.
        /// </summary>
        [Column("next_due")]
        public DateTime? NextDue { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [Column("status")]
        [MaxLength(40)]
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the digital signature.
        /// </summary>
        [Column("digital_signature")]
        [MaxLength(128)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the checklist template id.
        /// </summary>
        [Column("checklist_template_id")]
        public int? ChecklistTemplateId { get; set; }

        /// <summary>
        /// Gets or sets the machine label.
        /// </summary>
        [Column("machine")]
        [MaxLength(255)]
        public string? MachineLabel { get; set; }

        /// <summary>
        /// Gets or sets the component label.
        /// </summary>
        [Column("component")]
        [MaxLength(255)]
        public string? ComponentLabel { get; set; }

        /// <summary>
        /// Gets or sets the responsible user label.
        /// </summary>
        [Column("responsible_user")]
        [MaxLength(255)]
        public string? ResponsibleUserLabel { get; set; }

        /// <summary>
        /// Gets or sets the execution history raw.
        /// </summary>
        [Column("execution_history")]
        public string? ExecutionHistoryRaw { get; set; }

        /// <summary>
        /// Gets or sets the risk score.
        /// </summary>
        [Column("risk_score")]
        public decimal? RiskScore { get; set; }

        /// <summary>
        /// Gets or sets the ai recommendation.
        /// </summary>
        [Column("ai_recommendation")]
        [MaxLength(2048)]
        public string? AiRecommendation { get; set; }

        /// <summary>
        /// Gets or sets the last modified.
        /// </summary>
        [Column("last_modified")]
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// Gets or sets the last modified by id.
        /// </summary>
        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        /// <summary>
        /// Gets or sets the last modified by.
        /// </summary>
        [ForeignKey(nameof(LastModifiedById))]
        public User? LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the last modified by name.
        /// </summary>
        [Column("last_modified_by")]
        [MaxLength(255)]
        public string? LastModifiedByName { get; set; }

        /// <summary>
        /// Gets or sets the source ip.
        /// </summary>
        [Column("source_ip")]
        [MaxLength(45)]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Gets or sets the session id.
        /// </summary>
        [Column("session_id")]
        [MaxLength(80)]
        public string? SessionId { get; set; }

        /// <summary>
        /// Gets or sets the geo location.
        /// </summary>
        [Column("geo_location")]
        [MaxLength(100)]
        public string? GeoLocation { get; set; }

        /// <summary>
        /// Gets or sets the attachments raw.
        /// </summary>
        [Column("attachments")]
        [MaxLength(255)]
        public string? AttachmentsRaw { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        [Column("version")]
        public int? Version { get; set; }

        /// <summary>
        /// Gets or sets the previous version id.
        /// </summary>
        [Column("previous_version_id")]
        public int? PreviousVersionId { get; set; }

        /// <summary>
        /// Gets or sets the previous version label.
        /// </summary>
        [Column("previous_version")]
        [MaxLength(255)]
        public string? PreviousVersionLabel { get; set; }

        /// <summary>
        /// Gets or sets the is active version.
        /// </summary>
        [Column("is_active_version")]
        public bool? IsActiveVersion { get; set; }

        /// <summary>
        /// Gets or sets the linked work orders raw.
        /// </summary>
        [Column("linked_work_orders")]
        [MaxLength(255)]
        public string? LinkedWorkOrdersRaw { get; set; }

        /// <summary>
        /// Gets or sets the is automated.
        /// </summary>
        [Column("is_automated")]
        public bool? IsAutomated { get; set; }

        /// <summary>
        /// Gets or sets the requires notification.
        /// </summary>
        [Column("requires_notification")]
        public bool? RequiresNotification { get; set; }

        /// <summary>
        /// Gets or sets the anomaly score.
        /// </summary>
        [Column("anomaly_score")]
        public decimal? AnomalyScore { get; set; }

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        [Column("note")]
        [MaxLength(512)]
        public string? Note { get; set; }

        /// <summary>
        /// Represents the title value.
        /// </summary>
        [NotMapped]
        public string? Title
        {
            get => Name;
            set => Name = value;
        }

        /// <summary>
        /// Represents the due date value.
        /// </summary>
        [NotMapped]
        public DateTime? DueDate
        {
            get => NextDue;
            set => NextDue = value;
        }
    }
}
