// ==============================================================================
// File: Models/Notification.cs
// Purpose: Notification entities and channel events
// ==============================================================================
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Notification</b> – advanced, forensic-friendly notification entity with full context.
    /// <para>
    /// Tracks priority, status, targeting, sender/recipient details, timestamps, device/session info,
    /// and supports attachments. Designed to work across in-app, email, SMS, push, Teams, webhooks, etc.
    /// </para>
    /// </summary>
    public class Notification
    {
        // === Identity ===

        /// <summary>
        /// Unique identifier of the notification.
        /// </summary>
        public int Id { get; set; }

        // === Core content ===

        /// <summary>
        /// Short, human-friendly title shown to recipients.
        /// </summary>
        [StringLength(128)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Primary message/body content.
        /// </summary>
        [StringLength(4000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Logical type (e.g., <c>alert</c>, <c>reminder</c>, <c>escalation</c>, <c>system</c>).
        /// </summary>
        [StringLength(64)]
        public string Type { get; set; } = Types.Alert;

        /// <summary>
        /// Priority (<c>low</c>, <c>normal</c>, <c>high</c>, <c>critical</c>).
        /// </summary>
        [StringLength(32)]
        public string Priority { get; set; } = Priorities.Normal;

        /// <summary>
        /// Status (<c>new</c>, <c>sent</c>, <c>delivered</c>, <c>read</c>, <c>acknowledged</c>, <c>muted</c>, <c>archived</c>, <c>deleted</c>).
        /// </summary>
        [StringLength(32)]
        public string Status { get; set; } = Statuses.New;

        // === Targeting / Entity linkage ===

        /// <summary>
        /// Related entity name (e.g., <c>WorkOrder</c>, <c>Incident</c>, <c>Calibration</c>).
        /// </summary>
        [StringLength(128)]
        public string? Entity { get; set; }

        /// <summary>
        /// Related primary key identifier for the <see cref="Entity"/>.
        /// </summary>
        public int? EntityId { get; set; }

        /// <summary>
        /// Optional deep link (web/app route) for quick navigation.
        /// </summary>
        [StringLength(1024)]
        public string? Link { get; set; }

        // === Recipients ===

        /// <summary>
        /// Comma-separated list of recipients (user IDs, emails, external IDs). Stored as string for schema tolerance.
        /// </summary>
        [StringLength(2048)]
        public string? Recipients { get; set; }

        /// <summary>
        /// Convenience view over <see cref="Recipients"/>: splits on comma (trimmed), setter re-joins.
        /// </summary>
        public List<string> RecipientList
        {
            get => string.IsNullOrWhiteSpace(Recipients)
                ? new List<string>()
                : new List<string>(Recipients.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            set => Recipients = value == null ? null : string.Join(",", value);
        }

        // === Sender / Recipient (legacy compatibility) ===

        /// <summary>
        /// Single target user ID (if not using <see cref="Recipients"/>).
        /// </summary>
        public int? RecipientId { get; set; }

        /// <summary>
        /// Single target user display name (optional).
        /// </summary>
        [StringLength(256)]
        public string? Recipient { get; set; }

        /// <summary>
        /// Sender user ID.
        /// </summary>
        public int? SenderId { get; set; }

        /// <summary>
        /// Sender display name.
        /// </summary>
        [StringLength(256)]
        public string? Sender { get; set; }

        /// <summary>
        /// Compatibility alias for callers expecting <c>SenderUserId</c>.
        /// </summary>
        public int? SenderUserId { get => SenderId; set => SenderId = value; }

        /// <summary>
        /// Compatibility alias for callers expecting <c>SenderUserName</c>.
        /// </summary>
        public string? SenderUserName { get => Sender; set => Sender = value; }

        // === Acknowledgement / mute ===

        /// <summary>
        /// User ID that acknowledged the notification.
        /// </summary>
        public int? AckedByUserId { get; set; }

        /// <summary>
        /// Timestamp (UTC) when the notification was acknowledged.
        /// </summary>
        public DateTime? AckedAt { get; set; }

        /// <summary>
        /// If set, the notification remains muted until this timestamp (UTC).
        /// </summary>
        public DateTime? MutedUntil { get; set; }

        // === Forensics / context ===

        /// <summary>
        /// Last known source IP address for this notification context.
        /// </summary>
        [StringLength(64)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Device information (e.g., user agent, platform).
        /// </summary>
        [StringLength(256)]
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// Session identifier associated with the event.
        /// </summary>
        [StringLength(128)]
        public string? SessionId { get; set; }

        // === Timestamps ===

        /// <summary>
        /// Creation time in UTC.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update time in UTC.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        // === Attachments ===

        /// <summary>
        /// Attachments associated with the notification (schema tolerant).
        /// </summary>
        public List<Attachment> Attachments { get; set; } = new();

        // === Deep copy ===

        /// <summary>
        /// Creates a deep copy of this notification object, including attachments list.
        /// </summary>
        /// <returns>A new <see cref="Notification"/> instance with duplicated values.</returns>
        public Notification DeepCopy()
        {
            return new Notification
            {
                Id = this.Id,
                Title = this.Title,
                Message = this.Message,
                Type = this.Type,
                Priority = this.Priority,
                Status = this.Status,
                Entity = this.Entity,
                EntityId = this.EntityId,
                Link = this.Link,
                Recipients = this.Recipients,
                RecipientId = this.RecipientId,
                Recipient = this.Recipient,
                SenderId = this.SenderId,
                Sender = this.Sender,
                AckedByUserId = this.AckedByUserId,
                AckedAt = this.AckedAt,
                MutedUntil = this.MutedUntil,
                IpAddress = this.IpAddress,
                DeviceInfo = this.DeviceInfo,
                SessionId = this.SessionId,
                CreatedAt = this.CreatedAt,
                UpdatedAt = this.UpdatedAt,
                Attachments = new List<Attachment>(this.Attachments ?? new List<Attachment>())
            };
        }

        // === Constants (helpers) ===

        /// <summary>
        /// Standard status constants for <see cref="Status"/>.
        /// </summary>
        public static class Statuses
        {
            /// <summary>Newly created; not yet dispatched.</summary>
            public const string New = "new";
            /// <summary>Dispatch attempted or in flight.</summary>
            public const string Sent = "sent";
            /// <summary>Delivery confirmed by underlying channel.</summary>
            public const string Delivered = "delivered";
            /// <summary>Recipient read/open confirmation.</summary>
            public const string Read = "read";
            /// <summary>Recipient acknowledged (explicit action).</summary>
            public const string Acknowledged = "acknowledged";
            /// <summary>Temporarily muted until a given time.</summary>
            public const string Muted = "muted";
            /// <summary>Archived for record-keeping.</summary>
            public const string Archived = "archived";
            /// <summary>Deleted (soft or hard, depending on storage).</summary>
            public const string Deleted = "deleted";
        }

        /// <summary>
        /// Standard priority constants for <see cref="Priority"/>.
        /// </summary>
        public static class Priorities
        {
            public const string Low = "low";
            public const string Normal = "normal";
            public const string High = "high";
            public const string Critical = "critical";
        }

        /// <summary>
        /// Standard type constants for <see cref="Type"/>.
        /// </summary>
        public static class Types
        {
            public const string Alert = "alert";
            public const string Reminder = "reminder";
            public const string Escalation = "escalation";
            public const string System = "system";
        }
    }

    // ======== Channel delivery / audit events ========

    /// <summary>
    /// Represents a single per-channel delivery/audit event for a notification (e.g., in-app, email, SMS).
    /// </summary>
    public class NotificationChannelEvent
    {
        /// <summary>
        /// Unique identifier of the channel event (if persisted).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the parent <see cref="Notification"/>.
        /// </summary>
        public int NotificationId { get; set; }

        /// <summary>
        /// Channel used (<c>inapp</c>, <c>email</c>, <c>sms</c>, <c>push</c>, <c>teams</c>, <c>webhook</c>, ...).
        /// </summary>
        [StringLength(32)]
        public string Channel { get; set; } = string.Empty;

        /// <summary>
        /// UTC timestamp for the event.
        /// </summary>
        public DateTime EventTime { get; set; }

        /// <summary>
        /// Event indicator (<c>queued</c>, <c>sent</c>, <c>delivered</c>, <c>read</c>, <c>failed</c>, <c>bounced</c>).
        /// </summary>
        [StringLength(32)]
        public string Event { get; set; } = string.Empty;

        /// <summary>
        /// <b>Alias for compatibility:</b> Status of the channel event. Mirrors <see cref="Event"/>.
        /// This property exists to satisfy callers that expect <c>e.Status</c> instead of <c>e.Event</c>.
        /// </summary>
        [StringLength(32)]
        public string Status
        {
            get => Event;
            set => Event = value ?? string.Empty;
        }

        /// <summary>
        /// Target address/identifier for the channel (e.g., email, phone number, deviceId).
        /// </summary>
        [StringLength(256)]
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// Provider response, delivery receipt, or error payload (truncated as needed).
        /// </summary>
        [StringLength(1024)]
        public string Response { get; set; } = string.Empty;

        /// <summary>
        /// Source IP captured for the event.
        /// </summary>
        [StringLength(64)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Device metadata associated with the event (e.g., user-agent).
        /// </summary>
        [StringLength(256)]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>
        /// Optional free-form note.
        /// </summary>
        [StringLength(512)]
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Creates a shallow copy of this channel event. Suitable because all fields are value types or immutable strings.
        /// </summary>
        /// <returns>A new <see cref="NotificationChannelEvent"/> with identical values.</returns>
        public NotificationChannelEvent DeepCopy()
        {
            return (NotificationChannelEvent)this.MemberwiseClone();
        }
    }
}

