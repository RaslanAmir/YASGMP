using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Optional vector index for attachments used by AI/RAG scenarios.
    /// Stores an embedding vector (float32[]) as raw bytes plus model metadata.
    /// </summary>
    [Table("attachment_embeddings")]
    public class AttachmentEmbedding
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("attachment_id")]
        public int AttachmentId { get; set; }

        [MaxLength(128)]
        [Column("model")]
        public string Model { get; set; } = string.Empty;

        [Column("dimension")]
        public int Dimension { get; set; }

        /// <summary>
        /// Raw bytes for float32[] embedding vector. Length = 4 * Dimension.
        /// </summary>
        [Column("vector", TypeName = "LONGBLOB")]
        public byte[] Vector { get; set; } = Array.Empty<byte>();

        [MaxLength(64)]
        [Column("source_sha256")]
        public string SourceSha256 { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

