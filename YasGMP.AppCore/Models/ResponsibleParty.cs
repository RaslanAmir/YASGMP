using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents a person, department, or external party responsible for a machine.
    /// </summary>
    [Table("responsible_parties")]
    public class ResponsibleParty
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
