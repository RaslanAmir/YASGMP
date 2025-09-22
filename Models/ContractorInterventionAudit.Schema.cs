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
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("description")]
        [MaxLength(255)]
        public string? Description { get; set; }

        [Column("timestamp")]
        [MaxLength(255)]
        public string? LegacyTimestamp { get; set; }
    }
}
