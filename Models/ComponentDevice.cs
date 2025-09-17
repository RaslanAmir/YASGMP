using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `component_devices` table.</summary>
    [Table("component_devices")]
    public class ComponentDevice
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the component id.</summary>
        [Column("component_id")]
        public int ComponentId { get; set; }

        /// <summary>Gets or sets the device id.</summary>
        [Column("device_id")]
        public int DeviceId { get; set; }

        /// <summary>Gets or sets the sensor model id.</summary>
        [Column("sensor_model_id")]
        public int? SensorModelId { get; set; }

        /// <summary>Gets or sets the started at.</summary>
        [Column("started_at")]
        public DateTime? StartedAt { get; set; }

        /// <summary>Gets or sets the ended at.</summary>
        [Column("ended_at")]
        public DateTime? EndedAt { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(ComponentId))]
        public virtual MachineComponent? Component { get; set; }

        [ForeignKey(nameof(DeviceId))]
        public virtual IotDevice? Device { get; set; }

        [ForeignKey(nameof(SensorModelId))]
        public virtual SensorModel? SensorModel { get; set; }
    }
}
