using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema-level additions for <see cref="ForensicUserChangeLog"/> exposing generated and snapshot columns.
    /// </summary>
    public partial class ForensicUserChangeLog
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
        /// Gets or sets the details.
        /// </summary>
        [Column("details", TypeName = "text")]
        public string? Details { get; set; }

        /// <summary>
        /// Gets or sets the changed by user label.
        /// </summary>
        [Column("changed_by_user")]
        [StringLength(255)]
        public string? ChangedByUserLabel { get; set; }

        /// <summary>
        /// Gets or sets the target user label.
        /// </summary>
        [Column("target_user")]
        [StringLength(255)]
        public string? TargetUserLabel { get; set; }
    }
}
