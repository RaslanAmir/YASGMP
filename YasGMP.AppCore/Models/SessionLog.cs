using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>SessionLog</b> – Advanced log of user sessions, forensics, impersonation, temporary escalation, and device/IP tracking.
    /// </summary>
    [Table("session_log")]
    public partial class SessionLog
    {
        /// <summary>Unique identifier for the session log (Primary Key).</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>User ID associated with this session (FK).</summary>
        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>Navigation property for the user.</summary>
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        /// <summary>Session/token ID (for cross-system traceability).</summary>
        [MaxLength(128)]
        [Column("session_token")]
        public string SessionToken { get; set; } = string.Empty;

        /// <summary>Login timestamp (UTC).</summary>
        [Column("login_time")]
        public DateTime? LoginTime { get; set; }

        /// <summary>Logout timestamp (UTC, nullable).</summary>
        [Column("logout_time")]
        public DateTime? LogoutTime { get; set; }

        /// <summary>IP address for this session.</summary>
        [MaxLength(45)]
        [Column("ip_address")]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>Device or environment info (browser, OS, host, etc.).</summary>
        [MaxLength(255)]
        [Column("device_info")]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>MFA success flag.</summary>
        [Column("mfa_success")]
        public bool MfaSuccess { get; set; } = true;

        /// <summary>Reason for session termination or note.</summary>
        [Column("reason")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>Indicates whether the session was terminated explicitly.</summary>
        [Column("is_terminated")]
        public bool IsTerminated { get; set; }

        /// <summary>User ID who terminated the session (if any).</summary>
        [Column("terminated_by")]
        public int? TerminatedById { get; set; }

        /// <summary>Navigation to the user who terminated the session.</summary>
        [ForeignKey(nameof(TerminatedById))]
        public User? TerminatedBy { get; set; }

        /// <summary>Creation timestamp.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Last update timestamp.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Convenience field mirroring the login time.</summary>
        [Column("timestamp")]
        public DateTime? Timestamp { get; set; }

        /// <summary>Computed action label.
        /// </summary>
        [MaxLength(20)]
        [Column("action")]
        public string? Action { get; set; }

        /// <summary>Computed details payload.</summary>
        [Column("details")]
        public string? Details { get; set; }

        /// <summary>Snapshot of the username (for audits even if user is removed).</summary>
        [MaxLength(255)]
        [Column("user")]
        public string? UserNameSnapshot { get; set; }

        /// <summary>Session identifier used by upstream systems.</summary>
        [MaxLength(100)]
        [Column("session_id")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>Legacy login timestamp column.</summary>
        [Column("login_at")]
        public DateTime? LoginAt { get; set; }

        /// <summary>Legacy logout timestamp column.</summary>
        [Column("logout_at")]
        public DateTime? LogoutAt { get; set; }

        /// <summary>True if the session started as another user (impersonation/override).</summary>
        [Column("is_impersonated")]
        public bool? IsImpersonated { get; set; }

        /// <summary>If impersonated, the User ID of the original user (FK).</summary>
        [Column("impersonated_by_id")]
        public int? ImpersonatedById { get; set; }

        /// <summary>Navigation property for impersonating user.</summary>
        [ForeignKey(nameof(ImpersonatedById))]
        public User? ImpersonatedBy { get; set; }

        /// <summary>Name snapshot of the impersonating user.</summary>
        [MaxLength(255)]
        [Column("impersonated_by")]
        public string? ImpersonatedByName { get; set; }

        /// <summary>True if temporary escalation was granted for this session.</summary>
        [Column("is_temporary_escalation")]
        public bool? IsTemporaryEscalation { get; set; }

        /// <summary>Optional note for compliance, anomaly, or audit.</summary>
        [MaxLength(400)]
        [Column("note")]
        public string Note { get; set; } = string.Empty;

        /// <summary>Derived helper indicating whether the session is still active.</summary>
        [NotMapped]
        public bool IsActive => LogoutTime == null && !IsTerminated;
    }
}
