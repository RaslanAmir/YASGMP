using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("calibration_sensors")]
    public class CalibrationSensor
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("component_id")]
        public int? ComponentId { get; set; }

        [Column("sensor_type")]
        public string? SensorType { get; set; }

        [Column("range_min")]
        [Precision(10, 2)]
        public decimal? RangeMin { get; set; }

        [Column("range_max")]
        [Precision(10, 2)]
        public decimal? RangeMax { get; set; }

        [Column("unit")]
        [StringLength(20)]
        public string? Unit { get; set; }

        [Column("calibration_interval_days")]
        public int? CalibrationIntervalDays { get; set; }

        [Column("iot_device_id")]
        [StringLength(80)]
        public string? IotDeviceId { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("sensor_type_id")]
        public int? SensorTypeId { get; set; }
    }
}