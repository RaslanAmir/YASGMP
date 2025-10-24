// File: Models/ApiUsageLog.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>ApiUsageLog</b> – Logs every API key usage and call for full audit, security, compliance, and analytics.
    /// Tracks: who, when, what endpoint/method, params, response, error, timing, IP, session, and device. 
    /// Critical for GMP, 21 CFR Part 11, GDPR, security and integration monitoring.
    /// </summary>
    [Table("api_usage_log")]
    public class ApiUsageLog
    {
        /// <summary>
        /// Unique log record ID (primary key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID of API key used (foreign key).
        /// </summary>
        [Required]
        [Column("api_key_id")]
        public int ApiKeyId { get; set; }

        /// <summary>
        /// Navigation property for API key.
        /// </summary>
        [ForeignKey(nameof(ApiKeyId))]
        public ApiKey ApiKey { get; set; } = null!;

        /// <summary>
        /// User ID who initiated the API call (foreign key, optional).
        /// </summary>
        [Column("user_id")]
        public int? UserId { get; set; }

        /// <summary>
        /// Navigation property for the user (optional).
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>
        /// UTC time the API call was made.
        /// </summary>
        [Required]
        [Column("call_time")]
        public DateTime CallTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Name or URL of the endpoint called.
        /// </summary>
        [Required]
        [MaxLength(255)]
        [Column("endpoint")]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// HTTP method used (GET, POST, PUT, DELETE, etc.).
        /// </summary>
        [Required]
        [MaxLength(20)]
        [Column("method")]
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Parameters sent in the API call (JSON/query string, optionally encrypted).
        /// </summary>
        [Column("params")]
        public string? Params { get; set; }

        /// <summary>
        /// HTTP response code (e.g., 200, 404, 500).
        /// </summary>
        [Required]
        [Column("response_code")]
        public int ResponseCode { get; set; }

        /// <summary>
        /// Call duration in milliseconds.
        /// </summary>
        [Column("duration_ms")]
        public int DurationMs { get; set; }

        /// <summary>
        /// Whether the call was successful (true) or not (false).
        /// </summary>
        [Column("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Error message, if present.
        /// </summary>
        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// IP address from which the call was made.
        /// </summary>
        [MaxLength(45)]
        [Column("source_ip")]
        public string? SourceIp { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Device info/client signature (browser, OS, app version, etc).
        /// </summary>
        [NotMapped]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>
        /// Session ID for the API call (for incident/correlation trace).
        /// </summary>
        [NotMapped]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Digital signature/hash for full audit and data integrity.
        /// </summary>
        [NotMapped]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Rollback version (for future event sourcing/incident trace).
        /// </summary>
        [NotMapped]
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Is this record soft-deleted/archived (GDPR/analytics cleanup)?
        /// </summary>
        [NotMapped]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Freeform note or comment (e.g., incident ID, CAPA, trace).
        /// </summary>
        [NotMapped]
        public string Note { get; set; } = string.Empty;
    }
}

