using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `sensor_types` table.</summary>
    [Table("sensor_types")]
    public class SensorType
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the code.</summary>
        [Column("code")]
        [StringLength(20)]
        public string? Code { get; set; }

        /// <summary>Gets or sets the name.</summary>
        [Column("name")]
        [StringLength(100)]
        public string? Name { get; set; }

        /// <summary>Gets or sets the default unit id.</summary>
        [Column("default_unit_id")]
        public int? DefaultUnitId { get; set; }

        /// <summary>Gets or sets the accuracy.</summary>
        [Column("accuracy")]
        public decimal? Accuracy { get; set; }

        /// <summary>Gets or sets the range min.</summary>
        [Column("range_min")]
        public decimal? RangeMin { get; set; }

        /// <summary>Gets or sets the range max.</summary>
        [Column("range_max")]
        public decimal? RangeMax { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(DefaultUnitId))]
        public virtual MeasurementUnit? DefaultUnit { get; set; }
    }
}
