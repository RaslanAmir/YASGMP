using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `ppm_plans` table.</summary>
    [Table("ppm_plans")]
    public class PpmPlan
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the machine id.</summary>
        [Column("machine_id")]
        public int MachineId { get; set; }

        /// <summary>Gets or sets the title.</summary>
        [Column("title")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Gets or sets the plan json.</summary>
        [Column("plan_json")]
        public string? PlanJson { get; set; }

        /// <summary>Gets or sets the effective from.</summary>
        [Column("effective_from")]
        public DateTime? EffectiveFrom { get; set; }

        /// <summary>Gets or sets the effective to.</summary>
        [Column("effective_to")]
        public DateTime? EffectiveTo { get; set; }

        /// <summary>Gets or sets the status.</summary>
        [Column("status")]
        [StringLength(30)]
        public string Status { get; set; } = string.Empty;

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Gets or sets the last modified.</summary>
        [Column("last_modified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(MachineId))]
        public virtual Machine? Machine { get; set; }
    }
}
