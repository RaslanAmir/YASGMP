using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `scheduled_job_audit_log` table.</summary>
    [Table("scheduled_job_audit_log")]
    public class ScheduledJobAuditLog
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the scheduled job id.</summary>
        [Column("scheduled_job_id")]
        public int ScheduledJobId { get; set; }

        /// <summary>Gets or sets the user id.</summary>
        [Column("user_id")]
        public int? UserId { get; set; }

        /// <summary>Gets or sets the action.</summary>
        [Column("action")]
        public string Action { get; set; } = string.Empty;

        /// <summary>Gets or sets the old value.</summary>
        [Column("old_value")]
        public string? OldValue { get; set; }

        /// <summary>Gets or sets the new value.</summary>
        [Column("new_value")]
        public string? NewValue { get; set; }

        /// <summary>Gets or sets the changed at.</summary>
        [Column("changed_at")]
        public DateTime? ChangedAt { get; set; }

        /// <summary>Gets or sets the source ip.</summary>
        [Column("source_ip")]
        [StringLength(45)]
        public string? SourceIp { get; set; }

        /// <summary>Gets or sets the device info.</summary>
        [Column("device_info")]
        [StringLength(255)]
        public string? DeviceInfo { get; set; }

        /// <summary>Gets or sets the session id.</summary>
        [Column("session_id")]
        [StringLength(100)]
        public string? SessionId { get; set; }

        /// <summary>Gets or sets the digital signature.</summary>
        [Column("digital_signature")]
        [StringLength(255)]
        public string? DigitalSignature { get; set; }

        /// <summary>Gets or sets the note.</summary>
        [Column("note")]
        public string? Note { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Gets or sets the timestamp.</summary>
        [Column("timestamp")]
        public DateTime? Timestamp { get; set; }

        /// <summary>Gets or sets the details.</summary>
        [Column("details")]
        public string? Details { get; set; }

        [ForeignKey(nameof(ScheduledJobId))]
        public virtual ScheduledJob? ScheduledJob { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }
    }
}
