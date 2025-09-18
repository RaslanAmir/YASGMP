using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>UserPermission</b> – Maps a <see cref="Permission"/> directly to a <see cref="User"/> (RBAC override table).
    /// <para>
    /// • Ultra-robust: GMP, 21 CFR Part 11, CSV, forensics, escalation, digital signature, expiry, and device/session trace.<br/>
    /// • Used for direct overrides (escalations, incident response, temporary access) that bypass role-based permission matrix.<br/>
    /// • All grants, revokes, and changes should be logged in the audit trail for full legal and regulatory traceability.
    /// </para>
    /// <remarks>
    /// - Enables granular, temporary, or emergency user rights for incident, investigation, or legal reasons.<br/>
    /// - Ideal for highly regulated and forensic-grade environments.<br/>
    /// - Supports audit, rollback, SOD, and regulator queries.
    /// </remarks>
    /// </summary>
    [Table("user_permissions")]
    public class UserPermission
    {
        /// <summary>
        /// User ID (Foreign Key, part of composite key).
        /// </summary>
        [Column("user_id")]
        [Display(Name = "Korisnik")]
        public int UserId { get; set; }

        /// <summary>
        /// Navigation property for the user.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// Permission ID (Foreign Key, part of composite key).
        /// </summary>
        [Column("permission_id")]
        [Display(Name = "Dozvola")]
        public int PermissionId { get; set; }

        /// <summary>
        /// Navigation property for the permission.
        /// </summary>
        [ForeignKey("PermissionId")]
        public virtual Permission Permission { get; set; } = null!;

        /// <summary>
        /// True if this permission is allowed (grant), false if denied (override/revoke).
        /// </summary>
        [Column("allowed")]
        [Display(Name = "Dozvoljeno")]
        public bool Allowed { get; set; } = true;

        /// <summary>
        /// UTC timestamp when this assignment was granted (audit/compliance).
        /// </summary>
        [Column("assigned_at")]
        [Display(Name = "Dodijeljeno")]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User ID who assigned this permission (FK, audit/compliance).
        /// </summary>
        [Column("assigned_by")]
        [Display(Name = "Dodijelio")]
        public int? AssignedById { get; set; }

        /// <summary>
        /// Navigation to the assigning user (for audit trace).
        /// </summary>
        [ForeignKey("AssignedById")]
        public virtual User? AssignedBy { get; set; }

        /// <summary>
        /// Expiry date/time for this assignment (null = permanent).
        /// </summary>
        [Column("expires_at")]
        [Display(Name = "Vrijedi do")]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Is this assignment currently active? (soft delete/archive/expiry).
        /// </summary>
        [Column("is_active")]
        [Display(Name = "Aktivno")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Audit reason for assignment/removal (incident, escalation, delegation).
        /// </summary>
        [MaxLength(255)]
        [Column("reason")]
        [Display(Name = "Napomena")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Change version (event sourcing/rollback, forensics).
        /// </summary>
        [Column("change_version")]
        [Display(Name = "Verzija promjene")]
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Digital signature/hash for forensic/legal audit.
        /// </summary>
        [MaxLength(128)]
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Session ID for traceability (device/session/incident/audit).
        /// </summary>
        [MaxLength(64)]
        [Column("session_id")]
        [Display(Name = "ID Sesije")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Source IP/device for forensic audit and incident trace.
        /// </summary>
        [MaxLength(64)]
        [Column("source_ip")]
        [Display(Name = "Izvorni IP/Uređaj")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Freeform audit note/comment (GMP, 21 CFR, incident, regulator findings).
        /// </summary>
        [MaxLength(512)]
        [Column("note")]
        [Display(Name = "Bilješka")]
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Returns true if this assignment is currently valid and not expired.
        /// </summary>
        [NotMapped]
        public bool IsCurrentlyActive => IsActive && (!ExpiresAt.HasValue || ExpiresAt > DateTime.UtcNow);
    }
}
