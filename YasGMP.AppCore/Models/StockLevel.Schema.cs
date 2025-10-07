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
        /// Gets or sets the legacy part label.
        /// </summary>
        [Column("part")]
        [MaxLength(255)]
        public string? LegacyPartLabel { get; set; }

        /// <summary>
        /// Gets or sets the legacy warehouse label.
        /// </summary>
        [Column("warehouse")]
        [MaxLength(255)]
        public string? LegacyWarehouseLabel { get; set; }

        /// <summary>
        /// Gets or sets the last modified by name.
        /// </summary>
        [Column("last_modified_by")]
        [MaxLength(255)]
        public string? LastModifiedByName { get; set; }
    }
}
