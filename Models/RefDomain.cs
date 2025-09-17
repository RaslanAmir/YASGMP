using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `ref_domain` table.</summary>
    [Table("ref_domain")]
    public class RefDomain
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the name.</summary>
        [Column("name")]
        [StringLength(64)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the description.</summary>
        [Column("description")]
        [StringLength(255)]
        public string? Description { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
