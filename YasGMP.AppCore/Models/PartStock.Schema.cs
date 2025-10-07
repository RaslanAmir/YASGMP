using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema-level columns for <see cref="PartStock"/> (timestamps, etc.).
    /// </summary>
    public partial class PartStock
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
    }
}
