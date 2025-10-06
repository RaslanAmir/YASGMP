using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema-level extensions for <see cref="ApiKey"/> ensuring every database column is surfaced.
    /// </summary>
    [Table("api_keys")]
    public partial class ApiKey
    {
        /// <summary>Timestamp of the last update (maintained by database triggers).</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Legacy snapshot field capturing serialized usage details.</summary>
        [Column("usage_logs")]
        [MaxLength(255)]
        public string? UsageLogsSnapshot { get; set; }
    }
}

