using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("entity_tags")]
    public class EntityTag
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("entity")]
        [StringLength(60)]
        public string Entity { get; set; } = string.Empty;

        [Column("entity_id")]
        public int EntityId { get; set; }

        [Column("tag_id")]
        public int TagId { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
