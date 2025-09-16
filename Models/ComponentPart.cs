using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("component_parts")]
    public class ComponentPart
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("component_id")]
        public int ComponentId { get; set; }

        [Column("part_id")]
        public int PartId { get; set; }

        [Column("nominal_qty")]
        [Precision(10, 3)]
        public decimal NominalQty { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
