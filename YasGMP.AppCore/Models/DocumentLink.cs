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
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("document_id")]
        [Required]
        public int DocumentId { get; set; }

        [ForeignKey(nameof(DocumentId))]
        public Document? Document { get; set; }

        [Column("entity_type")]
        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty;

        [Column("entity_id")]
        [Required]
        public int EntityId { get; set; }

        [Column("note")]
        [MaxLength(255)]
        public string? Note { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}

