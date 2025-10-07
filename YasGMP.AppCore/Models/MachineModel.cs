using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `machine_models` table.</summary>
    [Table("machine_models")]
    public class MachineModel
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the manufacturer id.</summary>
        [Column("manufacturer_id")]
        public int? ManufacturerId { get; set; }

        /// <summary>Gets or sets the machine type id.</summary>
        [Column("machine_type_id")]
        public int? MachineTypeId { get; set; }

        /// <summary>Gets or sets the model code.</summary>
        [Column("model_code")]
        [StringLength(100)]
        public string? ModelCode { get; set; }

        /// <summary>Gets or sets the model name.</summary>
        [Column("model_name")]
        [StringLength(150)]
        public string? ModelName { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the manufacturer.
        /// </summary>
        [ForeignKey(nameof(ManufacturerId))]
        public virtual Manufacturer? Manufacturer { get; set; }

        /// <summary>
        /// Gets or sets the machine type.
        /// </summary>
        [ForeignKey(nameof(MachineTypeId))]
        public virtual MachineType? MachineType { get; set; }
    }
}
