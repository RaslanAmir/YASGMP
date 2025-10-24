using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YasGMP.Models.Enums;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents a photo record stored in the legacy <c>photos</c> table.
    /// </summary>
    [Table("photos")]
    public partial class Photo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("work_order_id")]
        public int? WorkOrderId { get; set; }

        public virtual WorkOrder? WorkOrder { get; set; }

        [Column("component_id")]
        public int? ComponentId { get; set; }

        public virtual MachineComponent? Component { get; set; }

        [Column("file_name")]
        [StringLength(255)]
        public string? FileName { get; set; }

        [Column("file_path")]
        [StringLength(255)]
        public string? FilePath { get; set; }

        [Column("type")]
        public PhotoType Type { get; set; } = PhotoType.Dokumentacija;

        [Column("uploaded_by")]
        public int? UploadedBy { get; set; }

        [ForeignKey(nameof(UploadedBy))]
        public virtual User? Uploader { get; set; }

        [Column("uploaded_at")]
        public DateTime? UploadedAt { get; set; }

        [Column("watermark_applied")]
        public bool WatermarkApplied { get; set; }

        [Column("note")]
        public string? Note { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Photo DeepCopy()
        {
            return new Photo
            {
                Id = Id,
                WorkOrderId = WorkOrderId,
                WorkOrder = WorkOrder,
                ComponentId = ComponentId,
                Component = Component,
                FileName = FileName,
                FilePath = FilePath,
                Type = Type,
                UploadedBy = UploadedBy,
                Uploader = Uploader,
                UploadedAt = UploadedAt,
                WatermarkApplied = WatermarkApplied,
                Note = Note,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
        }
    }
}

