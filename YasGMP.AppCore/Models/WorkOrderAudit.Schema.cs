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
        /// Gets or sets the work order label.
        /// </summary>
        [Column("work_order")]
        [MaxLength(255)]
        public string? WorkOrderLabel { get; set; }

        /// <summary>
        /// Gets or sets the user label.
        /// </summary>
        [Column("user")]
        [MaxLength(255)]
        public string? UserLabel { get; set; }
    }
}
