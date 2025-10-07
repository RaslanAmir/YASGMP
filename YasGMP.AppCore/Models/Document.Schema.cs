using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents the Document.
    /// </summary>
    public partial class Document
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        [Column("file_name")]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content type.
        /// </summary>
        [Column("content_type")]
        [StringLength(100)]
        public string? ContentType { get; set; }

        /// <summary>
        /// Gets or sets the storage provider.
        /// </summary>
        [Column("storage_provider")]
        [StringLength(40)]
        public string StorageProvider { get; set; } = "FileSystem";

        /// <summary>
        /// Gets or sets the storage path.
        /// </summary>
        [Column("storage_path")]
        [StringLength(255)]
        public string StoragePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sha256.
        /// </summary>
        [Column("sha256")]
        [StringLength(64)]
        public string? Sha256 { get; set; }

        /// <summary>
        /// Gets or sets the uploaded by.
        /// </summary>
        [Column("uploaded_by")]
        public int? UploadedBy { get; set; }

        /// <summary>
        /// Gets or sets the uploaded at.
        /// </summary>
        [Column("uploaded_at")]
        public DateTime UploadedAt { get; set; }
    }
}
