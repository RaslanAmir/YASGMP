using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `failure_modes` table.</summary>
    [Table("failure_modes")]
    public class FailureMode
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the component type id.</summary>
        [Column("component_type_id")]
        public int? ComponentTypeId { get; set; }

        /// <summary>Gets or sets the code.</summary>
        [Column("code")]
        [StringLength(40)]
        public string? Code { get; set; }

        /// <summary>Gets or sets the description.</summary>
        [Column("description")]
        [StringLength(255)]
        public string? Description { get; set; }

        /// <summary>Gets or sets the severity default.</summary>
        [Column("severity_default")]
        public int? SeverityDefault { get; set; }

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
