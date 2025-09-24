using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `component_models` table.</summary>
    [Table("component_models")]
    public class ComponentModel
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the component type id.</summary>
        [Column("component_type_id")]
        public int? ComponentTypeId { get; set; }

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

        [ForeignKey(nameof(ComponentTypeId))]
        public virtual ComponentType? ComponentType { get; set; }
    }
}
