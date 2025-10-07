using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents a link between an <see cref="Attachment"/> and an arbitrary entity
    /// within the system (machine, component, work order, etc.).
    /// </summary>
    [Table("attachment_links")]
    public class AttachmentLink
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the attachment id.
        /// </summary>
        [Column("attachment_id")]
        public int AttachmentId { get; set; }

        /// <summary>
        /// Gets or sets the entity type.
        /// </summary>
        [Required]
        [StringLength(64)]
        [Column("entity_type")]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the entity id.
        /// </summary>
        [Column("entity_id")]
        public int EntityId { get; set; }

        /// <summary>
        /// Gets or sets the linked at.
        /// </summary>
        [Column("linked_at")]
        public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the linked by id.
        /// </summary>
        [Column("linked_by_id")]
        public int? LinkedById { get; set; }

        /// <summary>
        /// Gets or sets the attachment.
        /// </summary>
        [ForeignKey(nameof(AttachmentId))]
        public virtual Attachment? Attachment { get; set; }

        /// <summary>
        /// Gets or sets the linked by.
        /// </summary>
        [ForeignKey(nameof(LinkedById))]
        public virtual User? LinkedBy { get; set; }
    }
}
