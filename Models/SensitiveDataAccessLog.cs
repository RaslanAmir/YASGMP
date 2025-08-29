using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>SensitiveDataAccessLog</b> – Ultra-robust forensic log for every access to sensitive data.
    /// </summary>
    public class SensitiveDataAccessLog
    {
        /// <summary>Unique log entry ID (Primary Key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>User ID who accessed the data.</summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>Navigacija na korisnika.</summary>
        public User User { get; set; } = null!;

        /// <summary>Username snapshot (for trace even if user deleted).</summary>
        [MaxLength(80)]
        public string Username { get; set; } = string.Empty;

        /// <summary>Timestamp of access (UTC).</summary>
        [Required]
        public DateTime AccessTime { get; set; } = DateTime.UtcNow;

        /// <summary>Table/entity name.</summary>
        [Required, MaxLength(80)]
        public string TableName { get; set; } = string.Empty;

        /// <summary>Record ID (if access is object-level).</summary>
        public int RecordId { get; set; }

        /// <summary>Field/column name (if field-level, optional).</summary>
        [MaxLength(80)]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>Type of access (view, export, print, api, edit, delete, audit, etc).</summary>
        [Required, MaxLength(24)]
        public string AccessType { get; set; } = string.Empty;

        /// <summary>IP address or device identifier.</summary>
        [MaxLength(64)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Device info/user agent (browser, OS, machine, etc).</summary>
        [MaxLength(255)]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>Session/token ID for this action.</summary>
        [MaxLength(100)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>Purpose/reason for access (e.g. investigation, reporting, support, legal).</summary>
        [MaxLength(255)]
        public string Purpose { get; set; } = string.Empty;

        /// <summary>ID of user who approved access (if required).</summary>
        public int? ApprovedById { get; set; }

        /// <summary>Navigacija na odobravatelja.</summary>
        public User ApprovedBy { get; set; } = null!;

        /// <summary>Name or role of approver (snapshot, for forensics).</summary>
        [MaxLength(100)]
        public string ApproverName { get; set; } = string.Empty;

        /// <summary>Timestamp when approval was granted (if any).</summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>Method of approval (manual, workflow, email, e-signature, etc).</summary>
        [MaxLength(40)]
        public string ApprovalMethod { get; set; } = string.Empty;

        /// <summary>Cryptographic digital signature or hash for this log entry.</summary>
        [MaxLength(128)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Integrity hash for blockchain/chain-of-custody compliance.</summary>
        [MaxLength(128)]
        public string EntryHash { get; set; } = string.Empty;

        /// <summary>Optional: Geolocation info (city, country, coordinates).</summary>
        [MaxLength(100)]
        public string GeoLocation { get; set; } = string.Empty;

        /// <summary>Severity/risk of access (normal, sensitive, critical, alert, ML-anomaly).</summary>
        [MaxLength(24)]
        public string Severity { get; set; } = string.Empty;

        /// <summary>ML/AI anomaly score (for future smart analytics/audit).</summary>
        public double? AnomalyScore { get; set; }

        /// <summary>Free-text note for auditor, inspector, or system comment.</summary>
        [MaxLength(1000)]
        public string Note { get; set; } = string.Empty;

        /// <summary>Human-friendly ToString for audit logs or dashboard.</summary>
        public override string ToString()
        {
            return $"SensitiveDataAccessLog: {AccessType} on {TableName}[{RecordId}] by {Username ?? UserId.ToString()} at {AccessTime:u}";
        }
    }
}
