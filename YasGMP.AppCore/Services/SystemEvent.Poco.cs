using System;

namespace YasGMP.Services
{
    /// <summary>
    /// Lightweight POCO representing a row in system_event_log for view-layer consumption.
    /// </summary>
    public class SystemEvent
    {
        /// <summary>Primary key.</summary>
        public int Id { get; set; }
        /// <summary>Event timestamp (UTC).</summary>
        public DateTime EventTime { get; set; }
        /// <summary>User id associated with the event (nullable).</summary>
        public int? UserId { get; set; }
        /// <summary>Event type (e.g., CREATE, UPDATE, DELETE, LOGIN).</summary>
        public string? EventType { get; set; }
        /// <summary>Logical/DB table affected.</summary>
        public string? TableName { get; set; }
        /// <summary>Related module or sub-system.</summary>
        public string? RelatedModule { get; set; }
        /// <summary>Primary key of the affected record (nullable).</summary>
        public int? RecordId { get; set; }
        /// <summary>Affected field name (nullable).</summary>
        public string? FieldName { get; set; }
        /// <summary>Previous field value (nullable).</summary>
        public string? OldValue { get; set; }
        /// <summary>New field value (nullable).</summary>
        public string? NewValue { get; set; }
        /// <summary>Detailed description.</summary>
        public string? Description { get; set; }
        /// <summary>Source IP (nullable).</summary>
        public string? SourceIp { get; set; }
        /// <summary>Device information (nullable).</summary>
        public string? DeviceInfo { get; set; }
        /// <summary>Session id (nullable).</summary>
        public string? SessionId { get; set; }
        /// <summary>Severity level (info, warn, error, critical).</summary>
        public string? Severity { get; set; }
        /// <summary>Processed flag for downstream automation.</summary>
        public bool Processed { get; set; }
    }
}

