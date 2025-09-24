using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema-level additions for <see cref="IncidentAudit"/> capturing extra metadata columns present in the MySQL dump.
    /// </summary>
    public partial class IncidentAudit
    {
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
