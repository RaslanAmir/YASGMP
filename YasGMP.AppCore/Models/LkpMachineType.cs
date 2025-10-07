using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Lookup entry describing a machine category/type.
    /// </summary>
    [Table("lkp_machine_types")]
    public class LkpMachineType
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        [MaxLength(16)]
        [Column("code")]
        public string? Code { get; set; }

        /// <summary>
        /// Gets or sets the is active.
        /// </summary>
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
        /// <summary>
        /// Executes the to string operation.
        /// </summary>

        public override string ToString() => Code is { Length: > 0 } ? $"{Code} - {Name}" : Name;
    }
}
