using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema extension for <see cref="SqlQueryAuditLog"/> exposing additional timestamps from the dump.
    /// </summary>
    public partial class SqlQueryAuditLog
    {
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
