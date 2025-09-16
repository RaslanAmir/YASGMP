using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Keyless]
    [Table("vw_scheduled_jobs_due")]
    public class VwScheduledJobsDue
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column("job_type")]
        [StringLength(40)]
        public string JobType { get; set; } = string.Empty;

        [Column("entity_type")]
        [StringLength(40)]
        public string? EntityType { get; set; }

        [Column("entity_id")]
        public int? EntityId { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("next_due")]
        public DateTime NextDue { get; set; }

        [Column("recurrence_pattern")]
        [StringLength(100)]
        public string? RecurrencePattern { get; set; }

        [Column("cron_expression")]
        [StringLength(100)]
        public string? CronExpression { get; set; }

        [Column("last_executed")]
        public DateTime? LastExecuted { get; set; }

        [Column("last_result")]
        [StringLength(255)]
        public string? LastResult { get; set; }

        [Column("escalation_level")]
        public int? EscalationLevel { get; set; }

        [Column("escalation_note")]
        [StringLength(255)]
        public string? EscalationNote { get; set; }

        [Column("chain_job_id")]
        public int? ChainJobId { get; set; }

        [Column("is_critical")]
        public bool? IsCritical { get; set; }

        [Column("needs_acknowledgment")]
        public bool? NeedsAcknowledgment { get; set; }

        [Column("acknowledged_by")]
        public int? AcknowledgedBy { get; set; }

        [Column("acknowledged_at")]
        public DateTime? AcknowledgedAt { get; set; }

        [Column("alert_on_failure")]
        public bool? AlertOnFailure { get; set; }

        [Column("retries")]
        public int? Retries { get; set; }

        [Column("max_retries")]
        public int? MaxRetries { get; set; }

        [Column("last_error")]
        public string? LastError { get; set; }

        [Column("iot_device_id")]
        [StringLength(80)]
        public string? IotDeviceId { get; set; }

        [Column("extra_params")]
        public string? ExtraParams { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("last_modified")]
        public DateTime? LastModified { get; set; }

        [Column("last_modified_by")]
        public int? LastModifiedBy { get; set; }

        [Column("digital_signature")]
        [StringLength(255)]
        public string? DigitalSignature { get; set; }

        [Column("device_info")]
        [StringLength(255)]
        public string? DeviceInfo { get; set; }

        [Column("session_id")]
        [StringLength(100)]
        public string? SessionId { get; set; }

        [Column("ip_address")]
        [StringLength(45)]
        public string? IpAddress { get; set; }

        [Column("comment")]
        public string? Comment { get; set; }
    }
}
