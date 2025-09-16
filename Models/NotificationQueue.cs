using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("notification_queue")]
    public class NotificationQueue
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("template_id")]
        public int? TemplateId { get; set; }

        [Column("recipient_user_id")]
        public int? RecipientUserId { get; set; }

        [Column("channel")]
        public string? Channel { get; set; }

        [Column("payload")]
        public string? Payload { get; set; }

        [Column("scheduled_at")]
        public DateTime? ScheduledAt { get; set; }

        [Column("sent_at")]
        public DateTime? SentAt { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("last_error")]
        public string? LastError { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
