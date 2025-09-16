using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("report_schedule")]
    public class ReportSchedule
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("report_name")]
        [StringLength(100)]
        public string? ReportName { get; set; }

        [Column("schedule_type")]
        public string? ScheduleType { get; set; }

        [Column("format")]
        public string? Format { get; set; }

        [Column("recipients")]
        public string? Recipients { get; set; }

        [Column("last_generated")]
        public DateTime? LastGenerated { get; set; }

        [Column("next_due")]
        public DateTime? NextDue { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("generated_by")]
        public int? GeneratedBy { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
