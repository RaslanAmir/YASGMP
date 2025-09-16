using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("checklist_items")]
    public class ChecklistItem
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("template_id")]
        public int TemplateId { get; set; }

        [Column("item_order")]
        public int? ItemOrder { get; set; }

        [Column("label")]
        [StringLength(255)]
        public string Label { get; set; } = string.Empty;

        [Column("expected")]
        [StringLength(255)]
        public string? Expected { get; set; }

        [Column("required")]
        public bool? Required { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}