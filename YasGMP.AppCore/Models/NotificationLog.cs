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
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public User? User { get; set; }

        [Required, StringLength(40)]
        public string NotificationType { get; set; } = string.Empty;

        [StringLength(64)]
        public string? RelatedType { get; set; }

        public int? RelatedId { get; set; }

        [Required, StringLength(4000)]
        public string Message { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Title { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public DateTime? DeliveredAt { get; set; }

        public DateTime? ReadAt { get; set; }

        public DateTime? ActionedAt { get; set; }

        [StringLength(32)]
        public string? Status { get; set; }

        public List<NotificationChannelEvent> ChannelHistory { get; set; } = new();

        [StringLength(32)]
        public string? Priority { get; set; }

        public bool IsEscalated { get; set; }

        [StringLength(64)]
        public string? FallbackChannel { get; set; }

        public List<Attachment> Attachments { get; set; } = new();

        [StringLength(128)]
        public string? DigitalSignature { get; set; }

        [StringLength(64)]
        public string? SourceIp { get; set; }

        [StringLength(128)]
        public string? GeoLocation { get; set; }

        public double? AnomalyScore { get; set; }

        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        public int? LastModifiedById { get; set; }

        public User? LastModifiedBy { get; set; }

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

        public override string ToString()
        {
            return $"[{NotificationType}] {Title ?? "(No Title)"} → User#{UserId} (Status: {Status})";
        }
    }
}
