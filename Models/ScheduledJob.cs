using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>ScheduledJob</b> – Universal job/task scheduling model for all automated, recurring, and ad-hoc jobs.
    /// Supports: maintenance, calibration, notifications, exports, reports, custom scripts, IoT sync.
    /// Designed for GMP/Annex 11/21 CFR Part 11 compliance (full audit, digital signature, escalation, traceability).
    /// </summary>
    public class ScheduledJob
    {
        /// <summary>Unique ID for the job/task (if stored in DB).</summary>
        public int Id { get; set; }

        /// <summary>Job name (display for UI/search).</summary>
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Job type (e.g., maintenance, calibration, notification, report, backup, external_sync, custom).</summary>
        [MaxLength(40)]
        public string JobType { get; set; } = string.Empty;

        /// <summary>Status (scheduled, in_progress, pending_ack, overdue, completed, failed, canceled).</summary>
        [MaxLength(32)]
        public string Status { get; set; } = "scheduled";

        /// <summary>Next due date/time for execution.</summary>
        public DateTime NextDue { get; set; } = DateTime.UtcNow;

        /// <summary>Recurrence pattern (Daily, Weekly, Monthly, CRON, etc.).</summary>
        [MaxLength(40)]
        public string RecurrencePattern { get; set; } = string.Empty;

        /// <summary>Optional: entity type (e.g., 'machine', 'component', 'report', 'user', etc.).</summary>
        [MaxLength(40)]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>Optional: ID of the related entity.</summary>
        public int? EntityId { get; set; }

        /// <summary>User who created the job (username or ID).</summary>
        [MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>Job creation timestamp.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Last modified timestamp.</summary>
        public DateTime? LastModifiedAt { get; set; }

        /// <summary>Device info (for audit/tracing).</summary>
        [MaxLength(255)]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>Session ID (for audit).</summary>
        [MaxLength(100)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>IP address (for audit).</summary>
        [MaxLength(45)]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>GMP digital signature hash.</summary>
        [MaxLength(255)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>UI: Escalation flag or notes.</summary>
        public string EscalationNote { get; set; } = string.Empty;

        /// <summary>Extra notes/comments.</summary>
        public string Comment { get; set; } = string.Empty;
    }
}
