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
        /// <summary>
        /// Gets or sets the legacy timestamp.
        /// </summary>
        [Column("timestamp")]
        public DateTime? LegacyTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        [Column("action")]
        [MaxLength(100)]
        public string? Action { get; set; }

        /// <summary>
        /// Gets or sets the related module.
        /// </summary>
        [Column("related_module")]
        [MaxLength(100)]
        public string? RelatedModule { get; set; }

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
        /// Gets or sets the ts utc.
        /// </summary>
        [Column("ts_utc")]
        public DateTime TsUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the legacy user label.
        /// </summary>
        [Column("user")]
        [MaxLength(255)]
        public string? LegacyUserLabel { get; set; }
    }
}
