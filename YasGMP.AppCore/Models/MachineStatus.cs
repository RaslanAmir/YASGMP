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
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required, MaxLength(50)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }
}