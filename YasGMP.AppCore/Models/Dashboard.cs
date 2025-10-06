using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `dashboards` table.</summary>
    [Table("dashboards")]
    public class Dashboard
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the dashboard name.</summary>
        [Column("dashboard_name")]
        [StringLength(100)]
        public string? DashboardName { get; set; }

        /// <summary>Gets or sets the description.</summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>Gets or sets the created by.</summary>
        [Column("created_by")]
        public int? CreatedBy { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the config json.</summary>
        [Column("config_json")]
        public string? ConfigJson { get; set; }

        /// <summary>Gets or sets the is active.</summary>
        [Column("is_active")]
        public bool? IsActive { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public virtual User? CreatedByNavigation { get; set; }
    }
}

