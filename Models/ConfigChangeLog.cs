using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("config_change_log")]
    public class ConfigChangeLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("change_time")]
        public DateTime? ChangeTime { get; set; }

        [Column("changed_by")]
        public int? ChangedBy { get; set; }

        [Column("config_name")]
        [StringLength(255)]
        public string? ConfigName { get; set; }

        [Column("old_value")]
        public string? OldValue { get; set; }

        [Column("new_value")]
        public string? NewValue { get; set; }

        [Column("change_type")]
        public string? ChangeType { get; set; }

        [Column("note")]
        public string? Note { get; set; }

        [Column("source_ip")]
        [StringLength(45)]
        public string? SourceIp { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("timestamp")]
        public DateTime? Timestamp { get; set; }

        [Column("action")]
        [StringLength(20)]
        public string? Action { get; set; }

        [Column("details")]
        public string? Details { get; set; }
    }
}
