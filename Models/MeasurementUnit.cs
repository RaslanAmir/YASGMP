using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("measurement_units")]
    public class MeasurementUnit
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("code")]
        [StringLength(10)]
        public string? Code { get; set; }

        [Column("name")]
        [StringLength(50)]
        public string? Name { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
