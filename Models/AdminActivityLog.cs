using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>AdminActivityLog</b> – Forensic log of privileged/admin actions with full GMP/CSV/21 CFR Part 11 context.
    /// Tracks device, session, IP, digital signature, change version, and relational links for rollback and audits.
    /// </summary>
    public partial class AdminActivityLog
    {
        /// <summary>Unique identifier for this log entry (primary key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>ID of the admin user who performed the action (nullable for system/bot events).</summary>
        public int? AdminId { get; set; }

        /// <summary>Navigation property for the admin user who performed the action.</summary>
        [ForeignKey(nameof(AdminId))]
        public User? Admin { get; set; }

        /// <summary>UTC timestamp of the action (defaults to now for new entries).</summary>
        public DateTime ActivityTime { get; set; } = DateTime.UtcNow;

        /// <summary>Activity type performed (e.g., create_user, delete, reset_password).</summary>
        [MaxLength(255)]
        public string Activity { get; set; } = string.Empty;

        /// <summary>Target table/entity name (e.g., users, work_orders, settings).</summary>
        [MaxLength(100)]
        public string AffectedTable { get; set; } = string.Empty;

        /// <summary>ID of the affected record (nullable for configuration/global operations).</summary>
        public int? AffectedRecordId { get; set; }

        /// <summary>JSON with before/after diff, snapshot, raw SQL, or change summary.</summary>
        [Column(TypeName = "text")]
        public string Details { get; set; } = string.Empty;

        /// <summary>IP address or device hostname where action was performed (forensics).</summary>
        [MaxLength(45)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Device name/ID or hostname for multi-device traceability.</summary>
        [MaxLength(128)]
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>Session identifier for the admin login (links to SessionLog when available).</summary>
        [MaxLength(64)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>Digital signature, hash, or PIN for this action (for audit/GMP compliance).</summary>
        [MaxLength(256)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Change version for event sourcing, rollback, and traceability.</summary>
        public int ChangeVersion { get; set; } = 1;

        /// <summary>Soft delete flag (GDPR/archive support).</summary>
        public bool IsDeleted { get; set; }

        /// <summary>Free-form note, audit reference, incident, or CAPA link.</summary>
        [Column(TypeName = "text")]
        public string Note { get; set; } = string.Empty;
    }
}
