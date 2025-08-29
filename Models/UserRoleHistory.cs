using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>UserRoleHistory</b> – Ultra-robust audit trail of all user role changes.
    /// Tracks old/new role, who changed, when, reason, digital signatures, IP, device, and compliance metadata for GMP/CSV/21 CFR/ISO/IT/security.
    /// </summary>
    public class UserRoleHistory
    {
        /// <summary>
        /// Unique record ID (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// User ID whose role was changed (FK).
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Navigation to the affected user.
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// Previous role (before change).
        /// </summary>
        [MaxLength(60)]
        public string OldRole { get; set; } = string.Empty;

        /// <summary>
        /// New role (after change).
        /// </summary>
        [Required]
        [MaxLength(60)]
        public string NewRole { get; set; } = string.Empty;

        /// <summary>
        /// User ID who performed the change (FK).
        /// </summary>
        [Required]
        public int ChangedById { get; set; }

        /// <summary>
        /// Navigation to the user who performed the change.
        /// </summary>
        public User ChangedBy { get; set; } = null!;

        /// <summary>
        /// Date and time of the change (UTC, audit).
        /// </summary>
        [Required]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Reason for the role change (mandatory for traceability and legal defense).
        /// </summary>
        [MaxLength(255)]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Inspector/security comment or bonus note.
        /// </summary>
        [MaxLength(400)]
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Digital signature or hash for full forensic audit.
        /// </summary>
        [MaxLength(128)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Forensics: IP address/device from which the change was made.
        /// </summary>
        [MaxLength(64)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Device info (browser, OS, user agent – bonus for advanced audits).
        /// </summary>
        [MaxLength(128)]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>
        /// Session ID for deep audit chain mapping.
        /// </summary>
        [MaxLength(100)]
        public string SessionId { get; set; } = string.Empty;
    }
}
