using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents the Inventory Transaction.
    /// </summary>
    public partial class InventoryTransaction
    {
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

        /// <summary>
        /// Gets or sets the performed by legacy.
        /// </summary>
        [Column("performed_by")]
        public int? PerformedByLegacy { get; set; }

        /// <summary>
        /// Gets or sets the spare part label.
        /// </summary>
        [Column("spare_part?")]
        [StringLength(255)]
        public string? SparePartLabel { get; set; }

        /// <summary>
        /// Gets or sets the warehouse label.
        /// </summary>
        [Column("warehouse?")]
        [StringLength(255)]
        public string? WarehouseLabel { get; set; }

        /// <summary>
        /// Gets or sets the user label.
        /// </summary>
        [Column("user?")]
        [StringLength(255)]
        public string? UserLabel { get; set; }
    }
}
