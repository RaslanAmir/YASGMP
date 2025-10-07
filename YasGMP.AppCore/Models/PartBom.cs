using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `part_bom` table.</summary>
    [Table("part_bom")]
    public class PartBom
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the parent part id.</summary>
        [Column("parent_part_id")]
        public int ParentPartId { get; set; }

        /// <summary>Gets or sets the child part id.</summary>
        [Column("child_part_id")]
        public int ChildPartId { get; set; }

        /// <summary>Gets or sets the quantity.</summary>
        [Column("quantity")]
        public decimal Quantity { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the parent part.
        /// </summary>
        [ForeignKey(nameof(ParentPartId))]
        public virtual Part? ParentPart { get; set; }

        /// <summary>
        /// Gets or sets the child part.
        /// </summary>
        [ForeignKey(nameof(ChildPartId))]
        public virtual Part? ChildPart { get; set; }
    }
}
