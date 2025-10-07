using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents an individual CAPA action, covering description, timing, ownership, and completion state.
    /// </summary>
    [Table("capa_actions")]
    public class CapaAction
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the capa id.
        /// </summary>
        [Column("capa_id")]
        public int CapaId { get; set; }

        /// <summary>
        /// Gets or sets the action type.
        /// </summary>
        [Column("action_type")]
        [StringLength(80)]
        public string? ActionType { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the due date.
        /// </summary>
        [Column("due_date")]
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Gets or sets the completed at.
        /// </summary>
        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

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
    }
}
