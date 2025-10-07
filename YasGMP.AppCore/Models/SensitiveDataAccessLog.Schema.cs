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
        /// Gets or sets the accessed at.
        /// </summary>
        [Column("accessed_at")]
        public DateTime? AccessedAt { get; set; }

        /// <summary>
        /// Gets or sets the legacy timestamp.
        /// </summary>
        [Column("timestamp")]
        public DateTime? LegacyTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        [Column("action")]
        [MaxLength(30)]
        public string? Action { get; set; }

        /// <summary>
        /// Gets or sets the details.
        /// </summary>
        [Column("details", TypeName = "text")]
        public string? Details { get; set; }

        /// <summary>
        /// Gets or sets the legacy user label.
        /// </summary>
        [Column("user")]
        [MaxLength(255)]
        public string? LegacyUserLabel { get; set; }

        /// <summary>
        /// Gets or sets the approved by legacy id.
        /// </summary>
        [Column("approved_by_id")]
        public int? ApprovedByLegacyId { get; set; }
    }
}
