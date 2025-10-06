using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `departments` table.</summary>
    [Table("departments")]
    public class Department
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the code.</summary>
        [Column("code")]
        [StringLength(50)]
        public string? Code { get; set; }

        /// <summary>Gets or sets the name.</summary>
        [Column("name")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the manager id.</summary>
        [Column("manager_id")]
        public int? ManagerId { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(ManagerId))]
        public virtual User? Manager { get; set; }
    }
}

