using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>UserLoginLog</b> – Ultra-robust, forensic log for user login and logout activity.
    /// Covers full audit, device/session/IP tracing, 2FA/SSO/biometric status, geolocation, risk/AI fields, and security event mapping for GMP/CSV/21 CFR/ISO/IT/Banking.
    /// </summary>
    [Table("user_login_audit")]
    public class UserLoginLog
    {
        /// <summary>
        /// Unique log entry ID (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID of the user who logged in (FK).
        /// </summary>
        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>
        /// Navigation to user.
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// Timestamp when the login started (UTC).
        /// </summary>
        [Required]
        [Column("login_time")]
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the logout occurred (nullable if session still open).
        /// </summary>
        [Column("logout_time")]
        public DateTime? LogoutTime { get; set; }

        /// <summary>
        /// IP address used for login (forensic source, may be IPv4 or IPv6).
        /// </summary>
        [MaxLength(45)]
        [Column("ip_address")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Device information (browser, OS, device type, user agent).
        /// </summary>
        [MaxLength(255)]
        [Column("device_info")]
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// Was login successful? (for attempted/failed logins).
        /// </summary>
        [Column("success")]
        public bool LoginSuccess { get; set; }

        /// <summary>
        /// Was 2FA/MFA completed successfully?
        /// </summary>
        [Column("two_factor_ok")]
        public bool TwoFactorAuthSuccess { get; set; }

        /// <summary>
        /// Was SSO (Single Sign-On) used?
        /// </summary>
        [Column("sso_used")]
        public bool IsSsoUsed { get; set; }

        /// <summary>
        /// Was biometric authentication used?
        /// </summary>
        [Column("biometric_used")]
        public bool IsBiometricUsed { get; set; }

        /// <summary>
        /// Session ID or authentication token (for session tracing).
        /// </summary>
        [MaxLength(128)]
        [Column("session_token")]
        public string? SessionId { get; set; }

        /// <summary>
        /// Geolocation (city/country or coordinates if available).
        /// </summary>
        [MaxLength(128)]
        [Column("geo_location")]
        public string? GeoLocation { get; set; }

        /// <summary>
        /// Login risk score (AI/ML anomaly detection, future expansion).
        /// </summary>
        [Column("risk_score")]
        public double? RiskScore { get; set; }

        /// <summary>
        /// Result status (success, failed, locked, password_expired, mfa_failed, etc.).
        /// </summary>
        [MaxLength(32)]
        [Column("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Reason for login failure, lockout, etc. (optional).
        /// </summary>
        [MaxLength(255)]
        [Column("reason")]
        public string? FailReason { get; set; }

        /// <summary>
        /// Time when login/attempt was last modified (audit, UTC).
        /// </summary>
        [Column("updated_at")]
        public DateTime? LastModified { get; set; }
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Digital signature or hash of this entry (for audit/forensic compliance).
        /// </summary>
        [MaxLength(128)]
        [Column("digital_signature")]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Free text note or comment (inspector, security, user feedback).
        /// </summary>
        [MaxLength(512)]
        [Column("note")]
        public string? Note { get; set; }
    }
}
