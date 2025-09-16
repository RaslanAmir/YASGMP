using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("scheduled_job_audit_log")]
    public class ScheduledJobAuditLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("scheduled_job_id")]
        public int ScheduledJobId { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("action")]
        public string Action { get; set; } = string.Empty;

        [Column("old_value")]
        public string? OldValue { get; set; }

        [Column("new_value")]
        public string? NewValue { get; set; }

        [Column("changed_at")]
        public DateTime? ChangedAt { get; set; }

        [Column("source_ip")]
        [StringLength(45)]
        public string? SourceIp { get; set; }

        [Column("device_info")]
        [StringLength(255)]
        public string? DeviceInfo { get; set; }

        [Column("session_id")]
        [StringLength(100)]
        public string? SessionId { get; set; }

        [Column("digital_signature")]
        [StringLength(255)]
        public string? DigitalSignature { get; set; }

        [Column("note")]
        public string? Note { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("timestamp")]
        public DateTime? Timestamp { get; set; }

        [Column("details")]
        public string? Details { get; set; }
    }
}
