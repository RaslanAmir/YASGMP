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
        public int ApiKeyId { get; set; }

        /// <summary>
        /// Navigation property for API key.
        /// </summary>
        [ForeignKey(nameof(ApiKeyId))]
        public ApiKey ApiKey { get; set; } = null!;

        /// <summary>
        /// User ID who initiated the API call (foreign key, optional).
        /// </summary>
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
        public DateTime CallTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Name or URL of the endpoint called.
        /// </summary>
        [Required]
        [MaxLength(256)]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// HTTP method used (GET, POST, PUT, DELETE, etc.).
        /// </summary>
        [Required]
        [MaxLength(8)]
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Parameters sent in the API call (JSON/query string, optionally encrypted).
        /// </summary>
        [MaxLength(2000)]
        public string Params { get; set; } = string.Empty;

        /// <summary>
        /// HTTP response code (e.g., 200, 404, 500).
        /// </summary>
        [Required]
        public int ResponseCode { get; set; }

        /// <summary>
        /// Call duration in milliseconds.
        /// </summary>
        public int DurationMs { get; set; }

        /// <summary>
        /// Whether the call was successful (true) or not (false).
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message, if present.
        /// </summary>
        [MaxLength(2000)]
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// IP address from which the call was made.
        /// </summary>
        [MaxLength(64)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Device info/client signature (browser, OS, app version, etc).
        /// </summary>
        [MaxLength(256)]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>
        /// Session ID for the API call (for incident/correlation trace).
        /// </summary>
        [MaxLength(64)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Digital signature/hash for full audit and data integrity.
        /// </summary>
        [MaxLength(256)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Rollback version (for future event sourcing/incident trace).
        /// </summary>
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Is this record soft-deleted/archived (GDPR/analytics cleanup)?
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Freeform note or comment (e.g., incident ID, CAPA, trace).
        /// </summary>
        [MaxLength(512)]
        public string Note { get; set; } = string.Empty;
    }
}
