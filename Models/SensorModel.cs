using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("sensor_models")]
    public class SensorModel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("vendor")]
        [StringLength(120)]
        public string? Vendor { get; set; }

        [Column("model_code")]
        [StringLength(100)]
        public string? ModelCode { get; set; }

        [Column("sensor_type_code")]
        [StringLength(80)]
        public string? SensorTypeCode { get; set; }

        [Column("unit_code")]
        [StringLength(20)]
        public string? UnitCode { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
