using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema extensions for <see cref="ContractorInterventionAudit"/> covering auxiliary columns from the SQL dump.
    /// </summary>
    public partial class ContractorInterventionAudit
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
        /// Gets or sets the description.
        /// </summary>
        [Column("description")]
        [MaxLength(255)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the legacy timestamp.
        /// </summary>
        [Column("timestamp")]
        [MaxLength(255)]
        public string? LegacyTimestamp { get; set; }
    }
}
