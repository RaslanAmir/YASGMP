using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `iot_sensor_data` table.</summary>
    [Table("iot_sensor_data")]
    public class IotSensorData
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the device id.</summary>
        [Column("device_id")]
        [StringLength(80)]
        public string? DeviceId { get; set; }

        /// <summary>Gets or sets the component id.</summary>
        [Column("component_id")]
        public int? ComponentId { get; set; }

        /// <summary>Gets or sets the data type.</summary>
        [Column("data_type")]
        [StringLength(50)]
        public string? DataType { get; set; }

        /// <summary>Gets or sets the value.</summary>
        [Column("value")]
        public decimal? Value { get; set; }

        /// <summary>Gets or sets the unit.</summary>
        [Column("unit")]
        [StringLength(20)]
        public string? Unit { get; set; }

        /// <summary>Gets or sets the timestamp.</summary>
        [Column("timestamp")]
        public DateTime? Timestamp { get; set; }

        /// <summary>Gets or sets the status.</summary>
        [Column("status")]
        public string? Status { get; set; }

        /// <summary>Gets or sets the anomaly detected.</summary>
        [Column("anomaly_detected")]
        public bool? AnomalyDetected { get; set; }

        /// <summary>Gets or sets the processed.</summary>
        [Column("processed")]
        public bool? Processed { get; set; }

        /// <summary>Gets or sets the note.</summary>
        [Column("note")]
        public string? Note { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Gets or sets the unit id.</summary>
        [Column("unit_id")]
        public int? UnitId { get; set; }

        [ForeignKey(nameof(ComponentId))]
        public virtual MachineComponent? Component { get; set; }

        [ForeignKey(nameof(UnitId))]
        public virtual MeasurementUnit? MeasurementUnit { get; set; }
    }
}
