using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("iot_sensor_data")]
    public class IotSensorData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("device_id")]
        [StringLength(80)]
        public string? DeviceId { get; set; }

        [Column("component_id")]
        public int? ComponentId { get; set; }

        [Column("data_type")]
        [StringLength(50)]
        public string? DataType { get; set; }

        [Column("value")]
        [Precision(12, 4)]
        public decimal? Value { get; set; }

        [Column("unit")]
        [StringLength(20)]
        public string? Unit { get; set; }

        [Column("timestamp")]
        public DateTime? Timestamp { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("anomaly_detected")]
        public bool? AnomalyDetected { get; set; }

        [Column("processed")]
        public bool? Processed { get; set; }

        [Column("note")]
        public string? Note { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("unit_id")]
        public int? UnitId { get; set; }
    }
}
