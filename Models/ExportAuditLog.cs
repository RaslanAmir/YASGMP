using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("export_audit_log")]
    public class ExportAuditLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("device_info")]
        [StringLength(255)]
        public string? DeviceInfo { get; set; }

        [Column("source_ip")]
        [StringLength(255)]
        public string? SourceIp { get; set; }

        [Column("filter_criteria")]
        [StringLength(255)]
        public string? FilterCriteria { get; set; }

        [Column("file_path")]
        [StringLength(255)]
        public string? FilePath { get; set; }

        [Column("timestamp")]
        [StringLength(255)]
        public string? Timestamp { get; set; }

        [Column("export_type")]
        [StringLength(255)]
        public string? ExportType { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }
    }
}
