using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `notification_queue` table.</summary>
    [Table("notification_queue")]
    public class NotificationQueue
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the template id.</summary>
        [Column("template_id")]
        public int? TemplateId { get; set; }

        /// <summary>Gets or sets the recipient user id.</summary>
        [Column("recipient_user_id")]
        public int? RecipientUserId { get; set; }

        /// <summary>Gets or sets the channel.</summary>
        [Column("channel")]
        public string? Channel { get; set; }

        /// <summary>Gets or sets the payload.</summary>
        [Column("payload")]
        public string? Payload { get; set; }

        /// <summary>Gets or sets the scheduled at.</summary>
        [Column("scheduled_at")]
        public DateTime? ScheduledAt { get; set; }

        /// <summary>Gets or sets the sent at.</summary>
        [Column("sent_at")]
        public DateTime? SentAt { get; set; }

        /// <summary>Gets or sets the status.</summary>
        [Column("status")]
        public string? Status { get; set; }

        /// <summary>Gets or sets the last error.</summary>
        [Column("last_error")]
        public string? LastError { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(TemplateId))]
        public virtual NotificationTemplate? Template { get; set; }

        [ForeignKey(nameof(RecipientUserId))]
        public virtual User? RecipientUser { get; set; }
    }
}

