using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `sensor_models` table.</summary>
    [Table("sensor_models")]
    public class SensorModel
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the vendor.</summary>
        [Column("vendor")]
        [StringLength(120)]
        public string? Vendor { get; set; }

        /// <summary>Gets or sets the model code.</summary>
        [Column("model_code")]
        [StringLength(100)]
        public string? ModelCode { get; set; }

        /// <summary>Gets or sets the sensor type code.</summary>
        [Column("sensor_type_code")]
        [StringLength(80)]
        public string? SensorTypeCode { get; set; }

        /// <summary>Gets or sets the unit code.</summary>
        [Column("unit_code")]
        [StringLength(20)]
        public string? UnitCode { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}

