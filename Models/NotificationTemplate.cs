using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("notification_templates")]
    public class NotificationTemplate
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("code")]
        [StringLength(80)]
        public string? Code { get; set; }

        [Column("name")]
        [StringLength(150)]
        public string? Name { get; set; }

        [Column("subject")]
        [StringLength(255)]
        public string? Subject { get; set; }

        [Column("body")]
        public string? Body { get; set; }

        [Column("channel")]
        public string? Channel { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
