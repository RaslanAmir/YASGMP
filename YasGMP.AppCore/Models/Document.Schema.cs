using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    public partial class Document
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("file_name")]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Column("content_type")]
        [StringLength(100)]
        public string? ContentType { get; set; }

        [Column("storage_provider")]
        [StringLength(40)]
        public string StorageProvider { get; set; } = "FileSystem";

        [Column("storage_path")]
        [StringLength(255)]
        public string StoragePath { get; set; } = string.Empty;

        [Column("sha256")]
        [StringLength(64)]
        public string? Sha256 { get; set; }

        [Column("uploaded_by")]
        public int? UploadedBy { get; set; }

        [Column("uploaded_at")]
        public DateTime UploadedAt { get; set; }
    }
}
