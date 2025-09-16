using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("dashboards")]
    public class Dashboard
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("dashboard_name")]
        [StringLength(100)]
        public string? DashboardName { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("config_json")]
        public string? ConfigJson { get; set; }

        [Column("is_active")]
        public bool? IsActive { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
