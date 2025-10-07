using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    /// <summary>
    /// Generic comment entity that links user-authored text to any tracked entity with timestamps.
    /// </summary>
    [Table("comments")]
    public class Comment
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the entity.
        /// </summary>
        [Column("entity")]
        [StringLength(60)]
        public string Entity { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the entity id.
        /// </summary>
        [Column("entity_id")]
        public int EntityId { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        [Column("user_id")]
        public int? UserId { get; set; }

        /// <summary>
        /// Gets or sets the comment text.
        /// </summary>
        [Column("comment")]
        public string? CommentText { get; set; }

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
