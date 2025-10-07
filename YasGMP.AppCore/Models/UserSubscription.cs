using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `user_subscriptions` table.</summary>
    [Table("user_subscriptions")]
    public class UserSubscription
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the user id.</summary>
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>Gets or sets the template id.</summary>
        [Column("template_id")]
        public int TemplateId { get; set; }

        /// <summary>Gets or sets the enabled.</summary>
        [Column("enabled")]
        public bool? Enabled { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        [ForeignKey(nameof(TemplateId))]
        public virtual NotificationTemplate? Template { get; set; }
    }
}
