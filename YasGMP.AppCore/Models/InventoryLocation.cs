using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Named sub-location within a warehouse (rack, aisle, shelf) used for precise stock placement.
    /// </summary>
    [Table("inventory_locations")]
    public partial class InventoryLocation
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
        [MaxLength(150)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional link to a broader site/location record.</summary>
        [Column("location_id")]
        public int? LocationId { get; set; }

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        [ForeignKey(nameof(LocationId))]
        public virtual Location? Location { get; set; }
        /// <summary>
        /// Executes the to string operation.
        /// </summary>

        public override string ToString() => string.IsNullOrWhiteSpace(Name) ? $"Location #{Id}" : Name;
    }
}
