using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    /// <summary>
    /// Describes calibration metadata for a sensor tied to a component, including ranges, units, and upkeep cadence.
    /// </summary>
    [Table("calibration_sensors")]
    public class CalibrationSensor
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the component id.
        /// </summary>
        [Column("component_id")]
        public int? ComponentId { get; set; }

        /// <summary>
        /// Gets or sets the sensor type.
        /// </summary>
        [Column("sensor_type")]
        public string? SensorType { get; set; }

        /// <summary>
        /// Gets or sets the range min.
        /// </summary>
        [Column("range_min")]
        [Precision(10, 2)]
        public decimal? RangeMin { get; set; }

        /// <summary>
        /// Gets or sets the range max.
        /// </summary>
        [Column("range_max")]
        [Precision(10, 2)]
        public decimal? RangeMax { get; set; }

        /// <summary>
        /// Gets or sets the unit.
        /// </summary>
        [Column("unit")]
        [StringLength(20)]
        public string? Unit { get; set; }

        /// <summary>
        /// Gets or sets the calibration interval days.
        /// </summary>
        [Column("calibration_interval_days")]
        public int? CalibrationIntervalDays { get; set; }

        /// <summary>
        /// Gets or sets the iot device id.
        /// </summary>
        [Column("iot_device_id")]
        [StringLength(80)]
        public string? IotDeviceId { get; set; }

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
        /// Gets or sets the sensor type id.
        /// </summary>
        [Column("sensor_type_id")]
        public int? SensorTypeId { get; set; }
    }
}
