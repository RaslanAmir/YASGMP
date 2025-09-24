using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>UserActivityLog</b> – Ultra-robust, forensic, AI-ready audit log for ALL user activities in the system.
    /// Covers action traceability, timestamp, session/device, digital signatures, entity mapping, and context for GMP, 21 CFR Part 11, HALMED, ISO, GDPR, and future AI/ML analytics.
    /// </summary>
    public class UserActivityLog
    {
        /// <summary>
        /// Unique log entry ID (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// User ID who performed the action (FK).
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Navigation to the user.
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// Main action name, type or category (LOGIN, LOGOUT, UPDATE, EXPORT, SIGN, etc.).
        /// </summary>
        [Required, MaxLength(80)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp (UTC) of action (for audit chain).
        /// </summary>
        [Required]
        public DateTime ActionTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Extra details, parameters, JSON, or context for the action (field changed, object, etc.).
        /// </summary>
        [MaxLength(2048)]
        public string Details { get; set; } = string.Empty;

        /// <summary>
        /// Name of the affected entity/table (optional, for traceability).
        /// </summary>
        [MaxLength(80)]
        public string Entity { get; set; } = string.Empty;

        /// <summary>
        /// Affected record/object ID (if applicable).
        /// </summary>
        [MaxLength(64)]
        public string RecordId { get; set; } = string.Empty;

        /// <summary>
        /// IP address/device of the action (forensic source).
        /// </summary>
        [MaxLength(64)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Device info (browser, OS, app version, user agent, etc.).
        /// </summary>
        [MaxLength(200)]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>
        /// Session ID for correlating activity (if web/app session).
        /// </summary>
        [MaxLength(100)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Digital signature or hash for this entry (21 CFR/CSV compliance, optional).
        /// </summary>
        [MaxLength(128)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Severity/risk level (info, warning, critical, audit, security, forensic, AI-anomaly).
        /// </summary>
        [MaxLength(32)]
        public string Severity { get; set; } = string.Empty;

        /// <summary>
        /// Free text note, comment, or inspector/auditor annotation.
        /// </summary>
        [MaxLength(512)]
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Optional: ML anomaly score for risk detection (future-ready).
        /// </summary>
        public double? AnomalyScore { get; set; }
    }
}
