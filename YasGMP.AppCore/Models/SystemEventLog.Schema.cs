using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema-level mappings for <see cref="SystemEventLog"/> ensuring raw database columns are exposed alongside the domain-friendly surface.
    /// </summary>
    public partial class SystemEventLog
    {
        [Column("timestamp")]
        public DateTime? LegacyTimestamp { get; set; }

        [Column("action")]
        [MaxLength(100)]
        public string? Action { get; set; }

        [Column("related_module")]
        [MaxLength(100)]
        public string? RelatedModule { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("ts_utc")]
        public DateTime TsUtc { get; set; } = DateTime.UtcNow;

        [Column("user")]
        [MaxLength(255)]
        public string? LegacyUserLabel { get; set; }
    }
}
