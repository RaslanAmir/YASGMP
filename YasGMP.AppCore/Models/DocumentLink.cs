using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Link between a managed document and another entity (machine, component, work order, etc.).
    /// </summary>
    [Table("document_links")]
    public class DocumentLink
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the document id.
        /// </summary>
        [Column("document_id")]
        [Required]
        public int DocumentId { get; set; }

        /// <summary>
        /// Gets or sets the document.
        /// </summary>
        [ForeignKey(nameof(DocumentId))]
        public Document? Document { get; set; }

        /// <summary>
        /// Gets or sets the entity type.
        /// </summary>
        [Column("entity_type")]
        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the entity id.
        /// </summary>
        [Column("entity_id")]
        [Required]
        public int EntityId { get; set; }

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        [Column("note")]
        [MaxLength(255)]
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
