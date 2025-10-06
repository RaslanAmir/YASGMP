using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>UserRoleMapping</b> – Represents the assignment of a <see cref="Role"/> to a <see cref="User"/> (many-to-many, RBAC mapping table).
    /// Ultra-robust: supports full GMP, CSV, 21 CFR Part 11, Annex 11, forensics, digital signature, audit, expiry, and rollback/versioning.
    /// </summary>
    [Table("user_roles")]
    public partial class UserRoleMapping
    {
        /// <summary>
        /// User ID (Foreign Key, part of composite key).
        /// </summary>
        [Column("user_id")]
        [Display(Name = "Korisnik")]
        public int UserId { get; set; }

        /// <summary>
        /// Navigation property for the assigned user.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;


        /// <summary>
        /// Role ID (Foreign Key, part of composite key).
        /// </summary>
        [Column("role_id")]
        [Display(Name = "Uloga")]
        public int RoleId { get; set; }

        /// <summary>
        /// Navigation property for the assigned role.
        /// </summary>
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;

        /// <summary>
        /// UTC date/time when this assignment was made (for audit and compliance).
        /// </summary>
        [Column("assigned_at")]
        [Display(Name = "Dodijeljeno")]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User ID of who assigned this role (for audit and incident traceability).
        /// </summary>
        [Column("assigned_by_id")]
        [Display(Name = "Dodijelio")]
        public int? AssignedById { get; set; }

        /// <summary>
        /// Navigation to the user who granted/assigned the role.
        /// </summary>
        [ForeignKey("AssignedById")]
        public virtual User AssignedBy { get; set; } = null!;

        /// <summary>
        /// Optional: expiry date/time for this assignment (null = no expiry, permanent assignment).
        /// </summary>
        [Column("expires_at")]
        [Display(Name = "Vrijedi do")]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Is this role assignment currently active?
        /// </summary>
        [Column("is_active")]
        [Display(Name = "Aktivan")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Optional: Reason for assignment, removal, escalation, or audit trail.
        /// </summary>
        [MaxLength(255)]
        [Column("reason")]
        [Display(Name = "Razlog")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Change/version number (event sourcing/rollback, forensics).
        /// </summary>
        [Column("change_version")]
        [Display(Name = "Verzija promjene")]
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Digital signature/hash for forensic audit, legal defense, and integrity checks.
        /// </summary>
        [MaxLength(256)]
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Session ID for traceability (incident/forensics/audit).
        /// </summary>
        [MaxLength(64)]
        [Column("session_id")]
        [Display(Name = "Sesija")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Source IP/device (for forensic audit, incident/trace, and compliance).
        /// </summary>
        [MaxLength(64)]
        [Column("source_ip")]
        [Display(Name = "Izvorni IP")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Audit note/comment (GMP, 21 CFR Part 11, incident linking).
        /// </summary>
        [MaxLength(512)]
        [Column("note")]
        [Display(Name = "Napomena")]
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Returns true if this assignment is currently valid and not expired.
        /// </summary>
        [NotMapped]
        public bool IsCurrentlyActive => IsActive && (!ExpiresAt.HasValue || ExpiresAt > DateTime.UtcNow);
    }
}

