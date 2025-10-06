using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents the quantity of a part stored at a specific inventory location.
    /// </summary>
    [Table("part_stocks")]
    public partial class PartStock
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("part_id")]
        public int PartId { get; set; }

        [ForeignKey(nameof(PartId))]
        public virtual Part Part { get; set; } = null!;

        [Column("inventory_location_id")]
        public int InventoryLocationId { get; set; }

        [ForeignKey(nameof(InventoryLocationId))]
        public virtual InventoryLocation InventoryLocation { get; set; } = null!;

        [Column("qty")]
        public int Quantity { get; set; }

        [Column("min_qty")]
        public int MinimumQuantity { get; set; }
    }
}

