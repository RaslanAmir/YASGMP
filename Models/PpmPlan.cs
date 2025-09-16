using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("ppm_plans")]
    public class PpmPlan
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("machine_id")]
        public int MachineId { get; set; }

        [Column("title")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Column("plan_json")]
        public string? PlanJson { get; set; }

        [Column("effective_from")]
        public DateTime? EffectiveFrom { get; set; }

        [Column("effective_to")]
        public DateTime? EffectiveTo { get; set; }

        [Column("status")]
        [StringLength(30)]
        public string Status { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("last_modified")]
        public DateTime LastModified { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
