using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    [Table("preventive_maintenance_plans")]
    public partial class PreventiveMaintenancePlan
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("code")]
        [MaxLength(50)]
        public string? Code { get; set; }

        [Column("name")]
        [MaxLength(100)]
        public string? Name { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("machine_id")]
        public int? MachineId { get; set; }

        [ForeignKey(nameof(MachineId))]
        public Machine? Machine { get; set; }

        [Column("component_id")]
        public int? ComponentId { get; set; }

        [ForeignKey(nameof(ComponentId))]
        public MachineComponent? Component { get; set; }

        [Column("frequency")]
        [MaxLength(40)]
        public string? Frequency { get; set; }

        [Column("checklist_file")]
        [MaxLength(255)]
        public string? ChecklistFile { get; set; }

        [Column("responsible_user_id")]
        public int? ResponsibleUserId { get; set; }

        [ForeignKey(nameof(ResponsibleUserId))]
        public User? ResponsibleUser { get; set; }

        [Column("last_executed")]
        public DateTime? LastExecuted { get; set; }

        [Column("next_due")]
        public DateTime? NextDue { get; set; }

        [Column("status")]
        [MaxLength(40)]
        public string? Status { get; set; }

        [Column("digital_signature")]
        [MaxLength(128)]
        public string? DigitalSignature { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("checklist_template_id")]
        public int? ChecklistTemplateId { get; set; }

        [Column("machine")]
        [MaxLength(255)]
        public string? MachineLabel { get; set; }

        [Column("component")]
        [MaxLength(255)]
        public string? ComponentLabel { get; set; }

        [Column("responsible_user")]
        [MaxLength(255)]
        public string? ResponsibleUserLabel { get; set; }

        [Column("execution_history")]
        public string? ExecutionHistoryRaw { get; set; }

        [Column("risk_score")]
        public decimal? RiskScore { get; set; }

        [Column("ai_recommendation")]
        [MaxLength(2048)]
        public string? AiRecommendation { get; set; }

        [Column("last_modified")]
        public DateTime? LastModified { get; set; }

        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        [ForeignKey(nameof(LastModifiedById))]
        public User? LastModifiedBy { get; set; }

        [Column("last_modified_by")]
        [MaxLength(255)]
        public string? LastModifiedByName { get; set; }

        [Column("source_ip")]
        [MaxLength(45)]
        public string? SourceIp { get; set; }

        [Column("session_id")]
        [MaxLength(80)]
        public string? SessionId { get; set; }

        [Column("geo_location")]
        [MaxLength(100)]
        public string? GeoLocation { get; set; }

        [Column("attachments")]
        [MaxLength(255)]
        public string? AttachmentsRaw { get; set; }

        [Column("version")]
        public int? Version { get; set; }

        [Column("previous_version_id")]
        public int? PreviousVersionId { get; set; }

        [Column("previous_version")]
        [MaxLength(255)]
        public string? PreviousVersionLabel { get; set; }

        [Column("is_active_version")]
        public bool? IsActiveVersion { get; set; }

        [Column("linked_work_orders")]
        [MaxLength(255)]
        public string? LinkedWorkOrdersRaw { get; set; }

        [Column("is_automated")]
        public bool? IsAutomated { get; set; }

        [Column("requires_notification")]
        public bool? RequiresNotification { get; set; }

        [Column("anomaly_score")]
        public decimal? AnomalyScore { get; set; }

        [Column("note")]
        [MaxLength(512)]
        public string? Note { get; set; }

        [NotMapped]
        public string? Title
        {
            get => Name;
            set => Name = value;
        }

        [NotMapped]
        public DateTime? DueDate
        {
            get => NextDue;
            set => NextDue = value;
        }
    }
}

