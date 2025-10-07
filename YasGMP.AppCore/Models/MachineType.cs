using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Lookup entity representing a machine type/category.
    /// </summary>
    [Table("machine_types")]
    public class MachineType
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
        [Required, MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }
}