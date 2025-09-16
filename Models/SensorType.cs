using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("sensor_types")]
    public class SensorType
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("code")]
        [StringLength(20)]
        public string? Code { get; set; }

        [Column("name")]
        [StringLength(100)]
        public string? Name { get; set; }

        [Column("default_unit_id")]
        public int? DefaultUnitId { get; set; }

        [Column("accuracy")]
        [Precision(6, 3)]
        public decimal? Accuracy { get; set; }

        [Column("range_min")]
        [Precision(10, 3)]
        public decimal? RangeMin { get; set; }

        [Column("range_max")]
        [Precision(10, 3)]
        public decimal? RangeMax { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
