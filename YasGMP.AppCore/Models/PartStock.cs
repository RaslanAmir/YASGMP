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
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the part id.
        /// </summary>
        [Column("part_id")]
        public int PartId { get; set; }

        /// <summary>
        /// Gets or sets the part.
        /// </summary>
        [ForeignKey(nameof(PartId))]
        public virtual Part Part { get; set; } = null!;

        /// <summary>
        /// Gets or sets the inventory location id.
        /// </summary>
        [Column("inventory_location_id")]
        public int InventoryLocationId { get; set; }

        /// <summary>
        /// Gets or sets the inventory location.
        /// </summary>
        [ForeignKey(nameof(InventoryLocationId))]
        public virtual InventoryLocation InventoryLocation { get; set; } = null!;

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        [Column("qty")]
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the minimum quantity.
        /// </summary>
        [Column("min_qty")]
        public int MinimumQuantity { get; set; }
    }
}
