using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema supplement for <see cref="StockLevel"/> surfacing raw columns used by
    /// reporting, legacy UI bindings, and forensic analytics.
    /// </summary>
    public partial class StockLevel
    {
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("part")]
        [MaxLength(255)]
        public string? LegacyPartLabel { get; set; }

        [Column("warehouse")]
        [MaxLength(255)]
        public string? LegacyWarehouseLabel { get; set; }

        [Column("last_modified_by")]
        [MaxLength(255)]
        public string? LastModifiedByName { get; set; }
    }
}
