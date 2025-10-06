using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema-level enrichments for <see cref="SensitiveDataAccessLog"/> surfacing generated and auxiliary columns from MySQL.
    /// </summary>
    public partial class SensitiveDataAccessLog
    {
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("accessed_at")]
        public DateTime? AccessedAt { get; set; }

        [Column("timestamp")]
        public DateTime? LegacyTimestamp { get; set; }

        [Column("action")]
        [MaxLength(30)]
        public string? Action { get; set; }

        [Column("details", TypeName = "text")]
        public string? Details { get; set; }

        [Column("user")]
        [MaxLength(255)]
        public string? LegacyUserLabel { get; set; }

        [Column("approved_by_id")]
        public int? ApprovedByLegacyId { get; set; }
    }
}

