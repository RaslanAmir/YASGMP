using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema refinement for <see cref="SchemaMigrationLog"/> surfacing auxiliary columns from the MySQL dump.
    /// </summary>
    public partial class SchemaMigrationLog
    {
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("timestamp")]
        public DateTime? LegacyTimestamp { get; set; }

        [Column("migrated_by_id")]
        public int? MigratedByLegacyId { get; set; }
    }
}
