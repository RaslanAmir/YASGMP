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
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }
}
