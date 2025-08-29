using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>AttachmentAuditLog</b> – Full forensic GMP/Annex 11 audit record for each attachment action (upload, approve, rollback, etc.).
    /// Immutable, includes signature, device, IP, user, action, and timestamp.
    /// </summary>
    public class AttachmentAuditLog
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ID of the related attachment.
        /// </summary>
        public int AttachmentId { get; set; }

        /// <summary>
        /// Audit action (CREATE, APPROVE, ROLLBACK, DELETE, ...).
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// User who performed the action.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Originating IP address (for forensics).
        /// </summary>
        [MaxLength(64)]
        public string Ip { get; set; } = string.Empty;

        /// <summary>
        /// Device/computer name (for forensics).
        /// </summary>
        [MaxLength(128)]
        public string Device { get; set; } = string.Empty;

        /// <summary>
        /// Any audit note or comment.
        /// </summary>
        [MaxLength(512)]
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// SHA256 or other digital signature for integrity (Annex 11/21 CFR Part 11 compliance).
        /// </summary>
        [MaxLength(256)]
        public string SignatureHash { get; set; } = string.Empty;

        /// <summary>
        /// When the action occurred.
        /// </summary>
        public DateTime ActionAt { get; set; } = DateTime.UtcNow;
    }
}
