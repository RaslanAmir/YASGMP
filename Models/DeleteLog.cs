using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("delete_log")]
    public class DeleteLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        [Column("deleted_by")]
        public int? DeletedBy { get; set; }

        [Column("table_name")]
        [StringLength(100)]
        public string? TableName { get; set; }

        [Column("record_id")]
        public int? RecordId { get; set; }

        [Column("delete_type")]
        public string? DeleteType { get; set; }

        [Column("reason")]
        public string? Reason { get; set; }

        [Column("recoverable")]
        public bool? Recoverable { get; set; }

        [Column("backup_file")]
        [StringLength(255)]
        public string? BackupFile { get; set; }

        [Column("source_ip")]
        [StringLength(45)]
        public string? SourceIp { get; set; }

        [Column("note")]
        public string? Note { get; set; }

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
