using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema-level extensions for <see cref="Attachment"/> ensuring every database column is surfaced.
    /// </summary>
    public partial class Attachment
    {
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("ChangeControlId")]
        public int? ChangeControlId { get; set; }

        [Column("DeviationId")]
        public int? DeviationId { get; set; }

        [Column("ExternalContractorId")]
        public int? ExternalContractorId { get; set; }

        [Column("IncidentActionId")]
        public int? IncidentActionId { get; set; }

        [Column("IncidentId")]
        public int? IncidentId { get; set; }

        [Column("IncidentReportId")]
        public int? IncidentReportId { get; set; }

        [Column("MaintenanceExecutionLogId")]
        public int? MaintenanceExecutionLogId { get; set; }

        [Column("NotificationId")]
        public int? NotificationId { get; set; }

        [Column("NotificationLogId")]
        public int? NotificationLogId { get; set; }

        [Column("PartUsageId")]
        public int? PartUsageId { get; set; }

        [Column("SparePartId")]
        public int? SparePartId { get; set; }

        [Column("TrainingRecordId")]
        public int? TrainingRecordId { get; set; }
    }
}
