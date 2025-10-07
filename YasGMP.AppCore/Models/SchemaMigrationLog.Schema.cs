using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema refinement for <see cref="SchemaMigrationLog"/> surfacing auxiliary columns from the MySQL dump.
    /// </summary>
    public partial class SchemaMigrationLog
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
        /// Gets or sets the legacy timestamp.
        /// </summary>
        [Column("timestamp")]
        public DateTime? LegacyTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the migrated by legacy id.
        /// </summary>
        [Column("migrated_by_id")]
        public int? MigratedByLegacyId { get; set; }
    }
}
