using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("iot_anomaly_log")]
    public class IotAnomalyLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("sensor_data_id")]
        public int? SensorDataId { get; set; }

        [Column("detected_at")]
        public DateTime? DetectedAt { get; set; }

        [Column("detected_by")]
        [StringLength(100)]
        public string? DetectedBy { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("severity")]
        public string? Severity { get; set; }

        [Column("resolved")]
        public bool? Resolved { get; set; }

        [Column("resolved_at")]
        public DateTime? ResolvedAt { get; set; }

        [Column("resolution_note")]
        public string? ResolutionNote { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
