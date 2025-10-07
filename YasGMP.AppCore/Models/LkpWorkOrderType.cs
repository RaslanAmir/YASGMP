using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Lookup work-order type (e.g., preventive, corrective, validation).
    /// </summary>
    [Table("lkp_work_order_types")]
    public class LkpWorkOrderType
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
        [MaxLength(60)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
        /// <summary>
        /// Executes the to string operation.
        /// </summary>

        public override string ToString() => Name;
    }
}
