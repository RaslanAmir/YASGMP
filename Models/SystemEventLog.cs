using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <see cref="SystemEventLog"/> – Ultimate, ultra-robust GMP/CSV/Annex 11/21 CFR Part 11-compliant audit &amp; event log.
    /// Tracks: who, what, when, where, how, old/new values, device, session, severity, AI/ML flags, compliance, chain-of-custody, rollback, digital signature, and more.
    /// Forensic, regulatory, and analytics ready!
    /// </summary>
    public class SystemEventLog
    {
        /// <summary>
        /// Unique log entry ID (primary key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Date and time of the event (always UTC, for compliance).
        /// </summary>
        [Required]
        public DateTime EventTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ID of the user who initiated the event (foreign key; nullable for system/bot/anonymous).
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Navigation property to the <see cref="User"/> entity.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>
        /// Snapshot of the username at the time of the event (for forensic purposes).
        /// </summary>
        [MaxLength(128)]
        public string? Username { get; set; }

        /// <summary>
        /// Type of the event (e.g., CREATE, UPDATE, DELETE, LOGIN, LOGOUT, EXPORT, PRINT, AI, API, SYSTEM, CUSTOM).
        /// </summary>
        [Required, MaxLength(40)]
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Name of the table to which the event applies.
        /// </summary>
        [MaxLength(64)]
        public string? TableName { get; set; }

        /// <summary>
        /// ID of the record within the table to which the event applies.
        /// </summary>
        public int? RecordId { get; set; }

        /// <summary>
        /// Name of the field that was changed (if applicable).
        /// </summary>
        [MaxLength(64)]
        public string? FieldName { get; set; }

        /// <summary>
        /// Previous value of the field before the change (serialized or as a string).
        /// </summary>
        [MaxLength(2000)]
        public string? OldValue { get; set; }

        /// <summary>
        /// New value of the field after the change (serialized or as a string).
        /// </summary>
        [MaxLength(2000)]
        public string? NewValue { get; set; }

        /// <summary>
        /// Detailed description of the event or change (JSON, reasons, diff, CAPA, incident reference).
        /// </summary>
        [MaxLength(2000)]
        public string? Description { get; set; }

        /// <summary>
        /// Hash or digital signature for chain-of-custody (Part 11/Blockchain-ready).
        /// </summary>
        [MaxLength(256)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Hash of the entire entry (integrity, rollback support, blockchain anchoring).
        /// </summary>
        [MaxLength(256)]
        public string? EntryHash { get; set; }

        /// <summary>
        /// IP address of the user or device that initiated the event.
        /// </summary>
        [MaxLength(64)]
        public string? SourceIp { get; set; }

        /// <summary>
        /// MAC address of the device (for forensic trace if available).
        /// </summary>
        [MaxLength(64)]
        public string? MacAddress { get; set; }

        /// <summary>
        /// Device information (user agent, OS, browser, mobile/desktop, geolocation).
        /// </summary>
        [MaxLength(256)]
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// Geolocation (city, country, coordinates) if available.
        /// </summary>
        [MaxLength(128)]
        public string? GeoLocation { get; set; }

        /// <summary>
        /// Session identifier in which the event occurred (token, JWT, API key, etc.).
        /// </summary>
        [MaxLength(64)]
        public string? SessionId { get; set; }

        /// <summary>
        /// AI/ML anomaly score for future analytics and risk scoring.
        /// </summary>
        public double? AnomalyScore { get; set; }

        /// <summary>
        /// Severity or risk level (info, warning, error, critical, audit, security, forensic, anomaly).
        /// </summary>
        [MaxLength(32)]
        public string? Severity { get; set; }

        /// <summary>
        /// Regulatory body (e.g., HALMED, FDA, EMA, GxP, Internal, External).
        /// </summary>
        [MaxLength(64)]
        public string? Regulator { get; set; }

        /// <summary>
        /// ID of a related incident, CAPA, audit, or investigation (for forensics and compliance).
        /// </summary>
        public int? RelatedCaseId { get; set; }

        /// <summary>
        /// Type of the related case or situation.
        /// </summary>
        [MaxLength(64)]
        public string? RelatedCaseType { get; set; }

        /// <summary>
        /// Indicates whether the event has been processed (automation, export, synchronization, etc.).
        /// </summary>
        public bool Processed { get; set; } = false;

        /// <summary>
        /// Free-text note (inspector, user, AI, notification).
        /// </summary>
        [MaxLength(512)]
        public string? Note { get; set; }

        /// <summary>
        /// Version number for event sourcing and rollback.
        /// </summary>
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Indicates if the record is soft-deleted (for GDPR/archive).
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Returns a human-readable summary of the event for dashboards and logs.
        /// </summary>
        /// <returns>
        /// A string containing the event timestamp, type, user, table and record, 
        /// field change, and severity.
        /// </returns>
        public override string ToString()
        {
            var userPart = !string.IsNullOrEmpty(Username) 
                           ? Username 
                           : $"User#{UserId}";
            return $"[{EventTime:yyyy-MM-dd HH:mm:ss}] {EventType} by {userPart} on {TableName}#{RecordId}: {FieldName} {OldValue}→{NewValue} (Severity: {Severity})";
        }
    }
}
