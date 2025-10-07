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
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the work order id.
        /// </summary>
        [Column("work_order_id")]
        public int? WorkOrderId { get; set; }
        /// <summary>
        /// Gets or sets the work order.
        /// </summary>

        public virtual WorkOrder? WorkOrder { get; set; }

        /// <summary>
        /// Gets or sets the component id.
        /// </summary>
        [Column("component_id")]
        public int? ComponentId { get; set; }
        /// <summary>
        /// Gets or sets the component.
        /// </summary>

        public virtual MachineComponent? Component { get; set; }

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        [Column("file_name")]
        [StringLength(255)]
        public string? FileName { get; set; }

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        [Column("file_path")]
        [StringLength(255)]
        public string? FilePath { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        [Column("type")]
        public PhotoType Type { get; set; } = PhotoType.Dokumentacija;

        /// <summary>
        /// Gets or sets the uploaded by.
        /// </summary>
        [Column("uploaded_by")]
        public int? UploadedBy { get; set; }

        /// <summary>
        /// Gets or sets the uploader.
        /// </summary>
        [ForeignKey(nameof(UploadedBy))]
        public virtual User? Uploader { get; set; }

        /// <summary>
        /// Gets or sets the uploaded at.
        /// </summary>
        [Column("uploaded_at")]
        public DateTime? UploadedAt { get; set; }

        /// <summary>
        /// Gets or sets the watermark applied.
        /// </summary>
        [Column("watermark_applied")]
        public bool WatermarkApplied { get; set; }

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        [Column("note")]
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Executes the deep copy operation.
        /// </summary>

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
