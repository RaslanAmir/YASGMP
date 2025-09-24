using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `entity_tags` table.</summary>
    [Table("entity_tags")]
    public class EntityTag
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the entity.</summary>
        [Column("entity")]
        [StringLength(60)]
        public string Entity { get; set; } = string.Empty;

        /// <summary>Gets or sets the entity id.</summary>
        [Column("entity_id")]
        public int EntityId { get; set; }

        /// <summary>Gets or sets the tag id.</summary>
        [Column("tag_id")]
        public int TagId { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(TagId))]
        public virtual Tag? Tag { get; set; }
    }
}
