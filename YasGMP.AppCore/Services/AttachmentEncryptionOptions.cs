using System;

namespace YasGMP.Services
{
    /// <summary>
    /// Configuration options for attachment envelope encryption.
    /// </summary>
    public class AttachmentEncryptionOptions
    {
        /// <summary>
        /// Base64 or hex encoded master key material used for AES-GCM encryption.
        /// </summary>
        public string? KeyMaterial { get; set; }

        /// <summary>
        /// Logical identifier for the key material (logged in audit events).
        /// </summary>
        public string KeyId { get; set; } = "default";

        /// <summary>
        /// Preferred chunk size (in bytes) when streaming attachment payloads.
        /// </summary>
        public int ChunkSize { get; set; } = 128 * 1024;
    }
}

