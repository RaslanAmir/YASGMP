using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema-level extensions for <see cref="Attachment"/> ensuring every database column is surfaced.
    /// </summary>
    public partial class Attachment
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
        /// Gets or sets the change control id.
        /// </summary>
        [Column("ChangeControlId")]
        public int? ChangeControlId { get; set; }

        /// <summary>
        /// Gets or sets the deviation id.
        /// </summary>
        [Column("DeviationId")]
        public int? DeviationId { get; set; }

        /// <summary>
        /// Gets or sets the external contractor id.
        /// </summary>
        [Column("ExternalContractorId")]
        public int? ExternalContractorId { get; set; }

        /// <summary>
        /// Gets or sets the incident action id.
        /// </summary>
        [Column("IncidentActionId")]
        public int? IncidentActionId { get; set; }

        /// <summary>
        /// Gets or sets the incident id.
        /// </summary>
        [Column("IncidentId")]
        public int? IncidentId { get; set; }

        /// <summary>
        /// Gets or sets the incident report id.
        /// </summary>
        [Column("IncidentReportId")]
        public int? IncidentReportId { get; set; }

        /// <summary>
        /// Gets or sets the maintenance execution log id.
        /// </summary>
        [Column("MaintenanceExecutionLogId")]
        public int? MaintenanceExecutionLogId { get; set; }

        /// <summary>
        /// Gets or sets the notification id.
        /// </summary>
        [Column("NotificationId")]
        public int? NotificationId { get; set; }

        /// <summary>
        /// Gets or sets the notification log id.
        /// </summary>
        [Column("NotificationLogId")]
        public int? NotificationLogId { get; set; }

        /// <summary>
        /// Gets or sets the part usage id.
        /// </summary>
        [Column("PartUsageId")]
        public int? PartUsageId { get; set; }

        /// <summary>
        /// Gets or sets the spare part id.
        /// </summary>
        [Column("SparePartId")]
        public int? SparePartId { get; set; }

        /// <summary>
        /// Gets or sets the training record id.
        /// </summary>
        [Column("TrainingRecordId")]
        public int? TrainingRecordId { get; set; }
    }
}
