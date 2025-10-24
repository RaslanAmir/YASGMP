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
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("capa_id")]
        public int CapaId { get; set; }

        [Column("action_type")]
        [StringLength(80)]
        public string? ActionType { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("due_date")]
        public DateTime? DueDate { get; set; }

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}

