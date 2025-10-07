using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    /// <summary>
    /// Defines a single checklist item/question within a template with ordering and required flags.
    /// </summary>
    [Table("checklist_items")]
    public class ChecklistItem
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the template id.
        /// </summary>
        [Column("template_id")]
        public int TemplateId { get; set; }

        /// <summary>
        /// Gets or sets the item order.
        /// </summary>
        [Column("item_order")]
        public int? ItemOrder { get; set; }

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        [Column("label")]
        [StringLength(255)]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expected.
        /// </summary>
        [Column("expected")]
        [StringLength(255)]
        public string? Expected { get; set; }

        /// <summary>
        /// Gets or sets the required.
        /// </summary>
        [Column("required")]
        public bool? Required { get; set; }

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
