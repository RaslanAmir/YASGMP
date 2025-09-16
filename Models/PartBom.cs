using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("part_bom")]
    public class PartBom
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("parent_part_id")]
        public int ParentPartId { get; set; }

        [Column("child_part_id")]
        public int ChildPartId { get; set; }

        [Column("quantity")]
        [Precision(10, 3)]
        public decimal Quantity { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
