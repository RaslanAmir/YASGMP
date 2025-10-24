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
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("timestamp")]
        public DateTime? LegacyTimestamp { get; set; }

        [Column("details", TypeName = "text")]
        public string? Details { get; set; }

        [Column("changed_by_user")]
        [StringLength(255)]
        public string? ChangedByUserLabel { get; set; }

        [Column("target_user")]
        [StringLength(255)]
        public string? TargetUserLabel { get; set; }
    }
}

