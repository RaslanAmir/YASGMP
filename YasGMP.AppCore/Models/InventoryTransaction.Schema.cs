using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    public partial class InventoryTransaction
    {
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("performed_by")]
        public int? PerformedByLegacy { get; set; }

        [Column("spare_part?")]
        [StringLength(255)]
        public string? SparePartLabel { get; set; }

        [Column("warehouse?")]
        [StringLength(255)]
        public string? WarehouseLabel { get; set; }

        [Column("user?")]
        [StringLength(255)]
        public string? UserLabel { get; set; }
    }
}

