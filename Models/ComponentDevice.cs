using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("component_devices")]
    public class ComponentDevice
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("component_id")]
        public int ComponentId { get; set; }

        [Column("device_id")]
        public int DeviceId { get; set; }

        [Column("sensor_model_id")]
        public int? SensorModelId { get; set; }

        [Column("started_at")]
        public DateTime? StartedAt { get; set; }

        [Column("ended_at")]
        public DateTime? EndedAt { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
