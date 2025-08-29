using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>ForensicUserChangeLog</b> – Forensic, auditable log of all user-related changes: create, role/status change, password reset, delete, force logout.
    /// <para>
    /// ⭐ SUPER-MEGA-ULTRA ROBUST:<br/>
    /// - Tracks who, when, what, on whom, before/after (role, status), reason, digital signature, IP/device, event chain, soft delete, and full navigation for AI/analytics/GMP/CSV/21 CFR Part 11.
    /// </para>
    /// </summary>
    public class ForensicUserChangeLog
    {
        /// <summary>Unique ID (Primary Key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Timestamp of the change.</summary>
        [Required]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>ID of the user (admin/supervisor) who made the change.</summary>
        public int? ChangedBy { get; set; }

        /// <summary>Navigation to user who made the change.</summary>
        [ForeignKey(nameof(ChangedBy))]
        public User? ChangedByUser { get; set; }

        /// <summary>
        /// Action: create_user, update_user, disable_user, change_role, reset_password, delete_user, force_logout.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        /// <summary>ID of the user who was changed (target).</summary>
        public int? TargetUserId { get; set; }

        /// <summary>Navigation to the user who was changed.</summary>
        [ForeignKey(nameof(TargetUserId))]
        public User? TargetUser { get; set; }

        /// <summary>Old role (before change).</summary>
        [StringLength(50)]
        public string? OldRole { get; set; }

        /// <summary>New role (after change).</summary>
        [StringLength(50)]
        public string? NewRole { get; set; }

        /// <summary>Old status (active/inactive) before the change.</summary>
        public bool? OldStatus { get; set; }

        /// <summary>New status (active/inactive) after the change.</summary>
        public bool? NewStatus { get; set; }

        /// <summary>Extra note or reason for the change (optional, for audit).</summary>
        [StringLength(1000)]
        public string? Note { get; set; }

        /// <summary>Forensic: IP address/device of the admin who made the change.</summary>
        [StringLength(128)]
        public string? SourceIp { get; set; }

        /// <summary>Digital signature/hash of the log (Part 11, non-repudiation).</summary>
        [StringLength(256)]
        public string? DigitalSignature { get; set; }

        /// <summary>Chain/version (rollback, traceability, event sourcing).</summary>
        public int ChangeVersion { get; set; } = 1;

        /// <summary>Soft delete/archive (GDPR, traceability).</summary>
        public bool IsDeleted { get; set; } = false;
    }
}
