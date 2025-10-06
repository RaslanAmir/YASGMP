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
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("attachment_id")]
        public int AttachmentId { get; set; }

        [Required]
        [StringLength(64)]
        [Column("entity_type")]
        public string EntityType { get; set; } = string.Empty;

        [Column("entity_id")]
        public int EntityId { get; set; }

        [Column("linked_at")]
        public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

        [Column("linked_by_id")]
        public int? LinkedById { get; set; }

        [ForeignKey(nameof(AttachmentId))]
        public virtual Attachment? Attachment { get; set; }

        [ForeignKey(nameof(LinkedById))]
        public virtual User? LinkedBy { get; set; }
    }
}

