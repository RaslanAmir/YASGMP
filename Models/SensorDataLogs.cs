using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("sensor_data_logs")]
    public class SensorDataLogs
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("component_id")]
        public int? ComponentId { get; set; }

        [Column("sensor_type")]
        public string? SensorType { get; set; }

        [Column("value")]
        [Precision(10, 2)]
        public decimal? Value { get; set; }

        [Column("unit")]
        [StringLength(10)]
        public string? Unit { get; set; }

        [Column("timestamp")]
        public DateTime? Timestamp { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("unit_id")]
        public int? UnitId { get; set; }

        [Column("sensor_type_id")]
        public int? SensorTypeId { get; set; }
    }
}
