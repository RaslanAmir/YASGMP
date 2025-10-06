using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `notification_templates` table.</summary>
    [Table("notification_templates")]
    public class NotificationTemplate
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the code.</summary>
        [Column("code")]
        [StringLength(80)]
        public string? Code { get; set; }

        /// <summary>Gets or sets the name.</summary>
        [Column("name")]
        [StringLength(150)]
        public string? Name { get; set; }

        /// <summary>Gets or sets the subject.</summary>
        [Column("subject")]
        [StringLength(255)]
        public string? Subject { get; set; }

        /// <summary>Gets or sets the body.</summary>
        [Column("body")]
        public string? Body { get; set; }

        /// <summary>Gets or sets the channel.</summary>
        [Column("channel")]
        public string? Channel { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}

