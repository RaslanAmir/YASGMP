using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>SensitiveDataAccessLog</b> - Ultra-robust forensic log for every access to sensitive data.
    /// </summary>
    [Table("sensitive_data_access_log")]
    public partial class SensitiveDataAccessLog
    {
        /// <summary>Unique log entry ID (Primary Key).</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>User ID who accessed the data.</summary>
        [Column("user_id")]
        public int? UserId { get; set; }

        /// <summary>Navigacija na korisnika.</summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>Username snapshot (for trace even if user deleted).</summary>
        [MaxLength(80)]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>Timestamp of access (UTC).</summary>
        [Column("access_time")]
        public DateTime AccessTime { get; set; } = DateTime.UtcNow;

        /// <summary>Table/entity name.</summary>
        [MaxLength(100)]
        [Column("table_name")]
        public string TableName { get; set; } = string.Empty;

        /// <summary>Record ID (if access is object-level).</summary>
        [Column("record_id")]
        public int? RecordId { get; set; }

        /// <summary>Field/column name (if field-level, optional).</summary>
        [MaxLength(100)]
        [Column("field_name")]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>Type of access (view, export, print, api, edit, delete).</summary>
        [MaxLength(24)]
        [Column("access_type")]
        public string AccessType { get; set; } = string.Empty;

        /// <summary>IP address or device identifier.</summary>
        [MaxLength(45)]
        [Column("source_ip")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Device info/user agent (browser, OS, machine, etc).</summary>
        [MaxLength(255)]
        [Column("device_info")]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>Session/token ID for this action.</summary>
        [MaxLength(100)]
        [Column("session_id")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>Purpose/reason for access (e.g. investigation, reporting, support, legal).</summary>
        [MaxLength(255)]
        [Column("purpose")]
        public string Purpose { get; set; } = string.Empty;

        /// <summary>ID of user who approved access (if required).</summary>
        [Column("approved_by")]
        public int? ApprovedById { get; set; }

        /// <summary>Navigacija na odobravatelja.</summary>
        [ForeignKey(nameof(ApprovedById))]
        public User? ApprovedBy { get; set; }

        /// <summary>Name or role of approver (snapshot, for forensics).</summary>
        [MaxLength(100)]
        [Column("approver_name")]
        public string ApproverName { get; set; } = string.Empty;

        /// <summary>Timestamp when approval was granted (if any).</summary>
        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        /// <summary>Method of approval (manual, workflow, email, e-signature, etc).</summary>
        [MaxLength(40)]
        [Column("approval_method")]
        public string ApprovalMethod { get; set; } = string.Empty;

        /// <summary>Cryptographic digital signature or hash for this log entry.</summary>
        [MaxLength(128)]
        [Column("digital_signature")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Integrity hash for blockchain/chain-of-custody compliance.</summary>
        [MaxLength(128)]
        [Column("entry_hash")]
        public string EntryHash { get; set; } = string.Empty;

        /// <summary>Optional: Geolocation info (city, country, coordinates).</summary>
        [MaxLength(100)]
        [Column("geo_location")]
        public string GeoLocation { get; set; } = string.Empty;

        /// <summary>Severity/risk of access (normal, sensitive, critical, alert, ML-anomaly).</summary>
        [MaxLength(24)]
        [Column("severity")]
        public string Severity { get; set; } = string.Empty;

        /// <summary>ML/AI anomaly score (for future smart analytics/audit).</summary>
        [Column("anomaly_score", TypeName = "decimal(10,2)")]
        public double? AnomalyScore { get; set; }

        /// <summary>Free-text note for auditor, inspector, or system comment.</summary>
        [Column("note", TypeName = "text")]
        public string Note { get; set; } = string.Empty;

        /// <summary>Human-friendly ToString for audit logs or dashboard.</summary>
        public override string ToString()
        {
            var actor = !string.IsNullOrWhiteSpace(Username) ? Username : UserId?.ToString() ?? "unknown";
            return $"SensitiveDataAccessLog: {AccessType} on {TableName}[{RecordId}] by {actor} at {AccessTime:u}";
        }
    }
}

