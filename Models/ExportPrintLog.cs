using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("export_print_log")]
    public class ExportPrintLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("export_time")]
        public DateTime? ExportTime { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("format")]
        public string? Format { get; set; }

        [Column("table_name")]
        [StringLength(100)]
        public string? TableName { get; set; }

        [Column("filter_used")]
        public string? FilterUsed { get; set; }

        [Column("file_path")]
        [StringLength(255)]
        public string? FilePath { get; set; }

        [Column("source_ip")]
        [StringLength(45)]
        public string? SourceIp { get; set; }

        [Column("note")]
        public string? Note { get; set; }

        [Column("timestamp")]
        public DateTime? Timestamp { get; set; }

        [Column("details")]
        public string? Details { get; set; }
    }
}
