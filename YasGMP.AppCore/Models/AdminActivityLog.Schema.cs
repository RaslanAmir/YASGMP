using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema extensions for <see cref="AdminActivityLog"/> bringing in persistence-only fields.
    /// </summary>
    [Table("admin_activity_log")]
    public partial class AdminActivityLog
    {
        /// <summary>Creation timestamp maintained by the database.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Last update timestamp maintained by the database.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Generated timestamp shadowing <see cref="ActivityTime"/>.</summary>
        [Column("timestamp")]
        public DateTime? Timestamp { get; set; }

        /// <summary>Generated action alias used by legacy consumers.</summary>
        [Column("action")]
        [MaxLength(255)]
        public string? Action { get; set; }
    }
}
