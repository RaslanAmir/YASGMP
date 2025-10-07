using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>NotificationLog</b> – THE ultimate, full-audit, multi-channel log of all user/system notifications.
    /// Tracks every field for regulatory, forensic, and analytics compliance.
    /// </summary>
    public class NotificationLog
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        [Required]
        public int UserId { get; set; }
        /// <summary>
        /// Gets or sets the user.
        /// </summary>

        public User? User { get; set; }

        /// <summary>
        /// Gets or sets the notification type.
        /// </summary>
        [Required, StringLength(40)]
        public string NotificationType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the related type.
        /// </summary>
        [StringLength(64)]
        public string? RelatedType { get; set; }
        /// <summary>
        /// Gets or sets the related id.
        /// </summary>

        public int? RelatedId { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        [Required, StringLength(4000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [StringLength(200)]
        public string? Title { get; set; }
        /// <summary>
        /// Gets or sets the sent at.
        /// </summary>

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets the delivered at.
        /// </summary>

        public DateTime? DeliveredAt { get; set; }
        /// <summary>
        /// Gets or sets the read at.
        /// </summary>

        public DateTime? ReadAt { get; set; }
        /// <summary>
        /// Gets or sets the actioned at.
        /// </summary>

        public DateTime? ActionedAt { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [StringLength(32)]
        public string? Status { get; set; }
        /// <summary>
        /// Gets or sets the channel history.
        /// </summary>

        public List<NotificationChannelEvent> ChannelHistory { get; set; } = new();

        /// <summary>
        /// Gets or sets the priority.
        /// </summary>
        [StringLength(32)]
        public string? Priority { get; set; }
        /// <summary>
        /// Gets or sets the is escalated.
        /// </summary>

        public bool IsEscalated { get; set; }

        /// <summary>
        /// Gets or sets the fallback channel.
        /// </summary>
        [StringLength(64)]
        public string? FallbackChannel { get; set; }
        /// <summary>
        /// Gets or sets the attachments.
        /// </summary>

        public List<Attachment> Attachments { get; set; } = new();

        /// <summary>
        /// Gets or sets the digital signature.
        /// </summary>
        [StringLength(128)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Gets or sets the source ip.
        /// </summary>
        [StringLength(64)]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Gets or sets the geo location.
        /// </summary>
        [StringLength(128)]
        public string? GeoLocation { get; set; }
        /// <summary>
        /// Gets or sets the anomaly score.
        /// </summary>

        public double? AnomalyScore { get; set; }
        /// <summary>
        /// Gets or sets the last modified.
        /// </summary>

        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets the last modified by id.
        /// </summary>

        public int? LastModifiedById { get; set; }
        /// <summary>
        /// Gets or sets the last modified by.
        /// </summary>

        public User? LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the audit note.
        /// </summary>
        [StringLength(1000)]
        public string? AuditNote { get; set; }

        /// <summary>True if notification has been read.</summary>
        public bool IsRead => ReadAt.HasValue;

        /// <summary>True if notification delivered either by timestamp or channel event.</summary>
        public bool IsDelivered => DeliveredAt.HasValue || (ChannelHistory?.Exists(e => e.Status == "delivered") ?? false);

        /// <summary>True if notification has an action timestamp.</summary>
        public bool IsActioned => ActionedAt.HasValue;

        /// <summary>Creates a deep copy for audit/export workflows.</summary>
        public NotificationLog DeepCopy()
        {
            return new NotificationLog
            {
                Id = this.Id,
                UserId = this.UserId,
                User = this.User,
                NotificationType = this.NotificationType,
                RelatedType = this.RelatedType,
                RelatedId = this.RelatedId,
                Message = this.Message,
                Title = this.Title,
                SentAt = this.SentAt,
                DeliveredAt = this.DeliveredAt,
                ReadAt = this.ReadAt,
                ActionedAt = this.ActionedAt,
                Status = this.Status,
                ChannelHistory = new List<NotificationChannelEvent>(this.ChannelHistory.ConvertAll(e => e.DeepCopy())),
                Priority = this.Priority,
                IsEscalated = this.IsEscalated,
                FallbackChannel = this.FallbackChannel,
                Attachments = new List<Attachment>(this.Attachments.ConvertAll(a => a.DeepCopy())),
                DigitalSignature = this.DigitalSignature,
                SourceIp = this.SourceIp,
                GeoLocation = this.GeoLocation,
                AnomalyScore = this.AnomalyScore,
                LastModified = this.LastModified,
                LastModifiedById = this.LastModifiedById,
                LastModifiedBy = this.LastModifiedBy,
                AuditNote = this.AuditNote
            };
        }
        /// <summary>
        /// Executes the to string operation.
        /// </summary>

        public override string ToString()
        {
            return $"[{NotificationType}] {Title ?? "(No Title)"} → User#{UserId} (Status: {Status})";
        }
    }
}
