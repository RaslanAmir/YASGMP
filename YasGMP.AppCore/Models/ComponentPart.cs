using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `component_parts` table.</summary>
    [Table("component_parts")]
    public class ComponentPart
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the component id.</summary>
        [Column("component_id")]
        public int ComponentId { get; set; }

        /// <summary>Gets or sets the part id.</summary>
        [Column("part_id")]
        public int PartId { get; set; }

        /// <summary>Gets or sets the nominal qty.</summary>
        [Column("nominal_qty")]
        public decimal NominalQty { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(ComponentId))]
        public virtual MachineComponent? Component { get; set; }

        [ForeignKey(nameof(PartId))]
        public virtual Part? Part { get; set; }
    }
}

