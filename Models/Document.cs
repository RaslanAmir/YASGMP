using System;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents a stored document or attachment.
    /// Mirrors the <c>documents</c> table.
    /// </summary>
    public class Document
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public string StorageProvider { get; set; } = "FileSystem";
        public string StoragePath { get; set; } = string.Empty;
        public string? Sha256 { get; set; }
        public int? UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}