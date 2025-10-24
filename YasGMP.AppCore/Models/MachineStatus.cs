using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Lookup for machine operational statuses.
    /// </summary>
    [Table("machine_statuses")]
    public class MachineStatus
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }
}
