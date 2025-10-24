using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>ScheduledJob</b> – Universal job/task scheduling model for all automated, recurring, and ad-hoc jobs.
    /// Supports: maintenance, calibration, notifications, exports, reports, custom scripts, IoT sync.
    /// Designed for GMP/Annex 11/21 CFR Part 11 compliance (full audit, digital signature, escalation, traceability).
    /// </summary>
    [Table("scheduled_jobs")]
    public partial class ScheduledJob
    {
        /// <summary>Unique ID for the job/task (if stored in DB).</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Job name (display for UI/search).</summary>
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Job type (e.g., maintenance, calibration, notification, report, backup, external_sync, custom).</summary>
        [MaxLength(40)]
        [Column("job_type")]
        public string JobType { get; set; } = string.Empty;

        /// <summary>Status (scheduled, in_progress, pending_ack, overdue, completed, failed, canceled).</summary>
        [MaxLength(32)]
        [Column("status")]
        public string Status { get; set; } = "scheduled";

        /// <summary>Next due date/time for execution.</summary>
        [Column("next_due")]
        public DateTime NextDue { get; set; } = DateTime.UtcNow;

        /// <summary>Recurrence pattern (Daily, Weekly, Monthly, CRON, etc.).</summary>
        [MaxLength(100)]
        [Column("recurrence_pattern")]
        public string RecurrencePattern { get; set; } = string.Empty;

        /// <summary>Optional: entity type (e.g., 'machine', 'component', 'report', 'user', etc.).</summary>
        [MaxLength(40)]
        [Column("entity_type")]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>Optional: ID of the related entity.</summary>
        [Column("entity_id")]
        public int? EntityId { get; set; }

        /// <summary>CRON expression for fine-grained scheduling.</summary>
        [MaxLength(100)]
        [Column("cron_expression")]
        public string? CronExpression { get; set; }

        /// <summary>Last execution timestamp.</summary>
        [Column("last_executed")]
        public DateTime? LastExecuted { get; set; }

        /// <summary>Textual result of last execution.</summary>
        [MaxLength(255)]
        [Column("last_result")]
        public string? LastResult { get; set; }

        /// <summary>Escalation level indicator.</summary>
        [Column("escalation_level")]
        public int? EscalationLevel { get; set; }

        /// <summary>UI: Escalation flag or notes.</summary>
        [Column("escalation_note")]
        public string EscalationNote { get; set; } = string.Empty;

        /// <summary>Chained job identifier (for job pipelines).</summary>
        [Column("chain_job_id")]
        public int? ChainJobId { get; set; }

        /// <summary>Marks if the job is critical.</summary>
        [Column("is_critical")]
        public bool IsCritical { get; set; }

        /// <summary>Marks if acknowledgment is required after execution.</summary>
        [Column("needs_acknowledgment")]
        public bool NeedsAcknowledgment { get; set; }

        /// <summary>User ID that acknowledged the job.</summary>
        [Column("acknowledged_by")]
        public int? AcknowledgedById { get; set; }

        /// <summary>Navigation to the acknowledging user.</summary>
        [ForeignKey(nameof(AcknowledgedById))]
        public User? AcknowledgedBy { get; set; }

        /// <summary>Timestamp when the job was acknowledged.</summary>
        [Column("acknowledged_at")]
        public DateTime? AcknowledgedAt { get; set; }

        /// <summary>Alerts should be emitted on failures.</summary>
        [Column("alert_on_failure")]
        public bool AlertOnFailure { get; set; } = true;

        /// <summary>Current retry count.</summary>
        [Column("retries")]
        public int Retries { get; set; }

        /// <summary>Maximum retry attempts.</summary>
        [Column("max_retries")]
        public int MaxRetries { get; set; } = 3;

        /// <summary>Last recorded error payload.</summary>
        [Column("last_error")]
        public string LastError { get; set; } = string.Empty;

        /// <summary>Associated IoT device identifier.</summary>
        [MaxLength(80)]
        [Column("iot_device_id")]
        public string IotDeviceId { get; set; } = string.Empty;

        /// <summary>JSON payload with additional parameters.</summary>
        [Column("extra_params")]
        public string ExtraParams { get; set; } = string.Empty;

        /// <summary>User ID that created the job.</summary>
        [Column("created_by")]
        public int? CreatedById { get; set; }

        /// <summary>Navigation to the creator.</summary>
        [ForeignKey(nameof(CreatedById))]
        public User? CreatedByUser { get; set; }

        /// <summary>Job creation timestamp.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Timestamp of last modification (legacy column).</summary>
        [Column("last_modified")]
        public DateTime? LastModified { get; set; }

        /// <summary>User ID that last modified the job.</summary>
        [Column("last_modified_by")]
        public int? LastModifiedById { get; set; }

        /// <summary>Navigation to the last modifying user.</summary>
        [ForeignKey(nameof(LastModifiedById))]
        public User? LastModifiedBy { get; set; }

        /// <summary>GMP digital signature hash.</summary>
        [MaxLength(255)]
        [Column("digital_signature")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Device info (for audit/tracing).</summary>
        [MaxLength(255)]
        [Column("device_info")]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>Session ID (for audit).</summary>
        [MaxLength(100)]
        [Column("session_id")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>IP address (for audit).</summary>
        [MaxLength(45)]
        [Column("ip_address")]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>Extra notes/comments.</summary>
        [Column("comment")]
        public string Comment { get; set; } = string.Empty;

        /// <summary>Tracking column updated via ON UPDATE trigger.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Last modified timestamp (shadow column).</summary>
        [Column("last_modified_at")]
        public DateTime? LastModifiedAt { get; set; }

        /// <summary>User who created the job (username or ID). In-memory helper.</summary>
        [NotMapped]
        public string CreatedBy { get; set; } = string.Empty;
    }
}

