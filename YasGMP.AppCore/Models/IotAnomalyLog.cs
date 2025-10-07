using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `iot_anomaly_log` table.</summary>
    [Table("iot_anomaly_log")]
    public class IotAnomalyLog
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the sensor data id.</summary>
        [Column("sensor_data_id")]
        public int? SensorDataId { get; set; }

        /// <summary>Gets or sets the detected at.</summary>
        [Column("detected_at")]
        public DateTime? DetectedAt { get; set; }

        /// <summary>Gets or sets the detected by.</summary>
        [Column("detected_by")]
        [StringLength(100)]
        public string? DetectedBy { get; set; }

        /// <summary>Gets or sets the description.</summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>Gets or sets the severity.</summary>
        [Column("severity")]
        public string? Severity { get; set; }

        /// <summary>Gets or sets the resolved.</summary>
        [Column("resolved")]
        public bool? Resolved { get; set; }

        /// <summary>Gets or sets the resolved at.</summary>
        [Column("resolved_at")]
        public DateTime? ResolvedAt { get; set; }

        /// <summary>Gets or sets the resolution note.</summary>
        [Column("resolution_note")]
        public string? ResolutionNote { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the sensor data.
        /// </summary>
        [ForeignKey(nameof(SensorDataId))]
        public virtual IotSensorData? SensorData { get; set; }
    }
}
