using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("photos")]
    public class Photos
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("work_order_id")]
        public int? WorkOrderId { get; set; }

        [Column("component_id")]
        public int? ComponentId { get; set; }

        [Column("file_name")]
        [StringLength(255)]
        public string? FileName { get; set; }

        [Column("file_path")]
        [StringLength(255)]
        public string? FilePath { get; set; }

        [Column("type")]
        public string? Type { get; set; }

        [Column("uploaded_by")]
        public int? UploadedBy { get; set; }

        [Column("uploaded_at")]
        public DateTime? UploadedAt { get; set; }

        [Column("watermark_applied")]
        public bool? WatermarkApplied { get; set; }

        [Column("note")]
        public string? Note { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
