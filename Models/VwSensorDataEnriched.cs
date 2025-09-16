using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Keyless]
    [Table("vw_sensor_data_enriched")]
    public class VwSensorDataEnriched
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("component_id")]
        public int? ComponentId { get; set; }

        [Column("sensor_type_id")]
        public int? SensorTypeId { get; set; }

        [Column("unit_id")]
        public int? UnitId { get; set; }

        [Column("value")]
        [Precision(10, 2)]
        public decimal? Value { get; set; }

        [Column("timestamp")]
        public DateTime? Timestamp { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("sensor_type_name")]
        [StringLength(100)]
        public string? SensorTypeName { get; set; }

        [Column("unit_name")]
        [StringLength(50)]
        public string? UnitName { get; set; }
    }
}
