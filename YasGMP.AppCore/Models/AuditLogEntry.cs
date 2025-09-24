using System;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>AuditLogEntry</b> – Universal, forensics-grade audit log entry used across entities/modules.
    /// <para>
    /// Designed to be tolerant with historic code: exposes both the new, consistent property names
    /// (<see cref="TableName"/>, <see cref="EntityId"/>, <see cref="Description"/>, <see cref="Timestamp"/>,
    /// <see cref="UserId"/>, <see cref="SourceIp"/>) and legacy synonyms
    /// (<see cref="EntityType"/>, <see cref="ChangedAt"/>, <see cref="IpAddress"/>, <see cref="Note"/>).
    /// </para>
    /// </summary>
    public class AuditLogEntry
    {
        /// <summary>Unique audit log entry ID.</summary>
        public int Id { get; set; }

        /// <summary>Normalized table/entity name (e.g., "users", "roles", "calibrations").</summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>ID of the related entity (nullable to support system-wide events).</summary>
        public int? EntityId { get; set; }

        /// <summary>Audit action (CREATE, UPDATE, DELETE, LOCK, UNLOCK, EXPORT, etc.).</summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>Human-readable description/details of the event.</summary>
        public string? Description { get; set; }

        /// <summary>UTC timestamp of the event.</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Actor user ID (nullable for system or unknown).</summary>
        public int? UserId { get; set; }

        /// <summary>Source IP address (if available).</summary>
        public string? SourceIp { get; set; }

        /// <summary>Originating device information (if available).</summary>
        public string? DeviceInfo { get; set; }

        /// <summary>Session identifier (if available).</summary>
        public string? SessionId { get; set; }

        /// <summary>Digital signature hash (e.g., for 21 CFR Part 11).</summary>
        public string? DigitalSignature { get; set; }

        /// <summary>Optional old value snapshot (JSON/serialized).</summary>
        public string? OldValue { get; set; }

        /// <summary>Optional new value snapshot (JSON/serialized).</summary>
        public string? NewValue { get; set; }

        // ---------------------------------------------------------------------
        // Backward-compatibility aliases (map to normalized properties)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Legacy alias that maps to <see cref="TableName"/> (e.g., "scheduled_jobs", "work_orders").
        /// </summary>
        public string EntityType
        {
            get => TableName;
            set => TableName = value ?? string.Empty;
        }

        /// <summary>
        /// Legacy alias that maps to <see cref="Timestamp"/>.
        /// </summary>
        public DateTime ChangedAt
        {
            get => Timestamp;
            set => Timestamp = value;
        }

        /// <summary>
        /// Legacy alias that maps to <see cref="SourceIp"/>.
        /// </summary>
        public string IpAddress
        {
            get => SourceIp ?? string.Empty;
            set => SourceIp = value;
        }

        /// <summary>
        /// Legacy alias that maps to <see cref="Description"/>.
        /// </summary>
        public string Note
        {
            get => Description ?? string.Empty;
            set => Description = value;
        }

        /// <summary>
        /// Optional "performed by" display string — preserved for compatibility, does not override <see cref="UserId"/>.
        /// </summary>
        public string? PerformedBy { get; set; }
    }
}
