using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema augmentations for <see cref="WorkOrderAudit"/> exposing additional columns from the database dump.
    /// </summary>
    public partial class WorkOrderAudit
    {
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("work_order")]
        [MaxLength(255)]
        public string? WorkOrderLabel { get; set; }

        [Column("user")]
        [MaxLength(255)]
        public string? UserLabel { get; set; }
    }
}

