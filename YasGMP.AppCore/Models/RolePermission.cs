using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>RolePermission</b> – Assigns a <see cref="Permission"/> to a <see cref="Role"/> (RBAC many-to-many mapping).
    /// <para>
    /// • Fully GMP/CSV/21 CFR Part 11/Annex 11/forensic/audit-ready.<br/>
    /// • Tracks assignment, expiration, digital signature, session, device, audit, and rollback support.<br/>
    /// • Designed for legal traceability, SOD, escalation, and compliance dashboards.
    /// </para>
    /// <remarks>
    /// - Use with <see cref="Role"/> and <see cref="Permission"/> to enforce and audit RBAC.<br/>
    /// - Extend for SOD (Segregation of Duties), external authorities, or incident escalation.
    /// </remarks>
    /// </summary>
    [Table("role_permissions")]
    public class RolePermission
    {
        /// <summary>
        /// Role ID (Foreign Key, part of composite key).
        /// </summary>
        [Column("role_id")]
        [Display(Name = "Uloga")]
        public int RoleId { get; set; }

        /// <summary>
        /// Navigation property to the assigned role.
        /// </summary>
        [ForeignKey("RoleId")]
        public virtual Role? Role { get; set; }

        /// <summary>
        /// Permission ID (Foreign Key, part of composite key).
        /// </summary>
        [Column("permission_id")]
        [Display(Name = "Dozvola")]
        public int PermissionId { get; set; }

        /// <summary>
        /// Navigation property to the assigned permission.
        /// </summary>
        [ForeignKey("PermissionId")]
        public virtual Permission? Permission { get; set; }

        /// <summary>
        /// Is this permission currently allowed for the role?
        /// </summary>
        [Column("allowed")]
        [Display(Name = "Dopušteno")]
        public bool Allowed { get; set; } = true;

        /// <summary>
        /// UTC timestamp when the permission was granted/updated (for audit/compliance).
        /// </summary>
        [Column("assigned_at")]
        [Display(Name = "Dodijeljeno")]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional: user who assigned this permission (for audit/compliance).
        /// </summary>
        [Column("assigned_by")]
        [Display(Name = "Dodijelio")]
        public int? AssignedById { get; set; }

        /// <summary>
        /// Navigation to the user who assigned (audit trail).
        /// </summary>
        [ForeignKey("AssignedById")]
        public virtual User? AssignedBy { get; set; }

        /// <summary>
        /// Optional: expiry date/time for the assignment (null = permanent).
        /// </summary>
        [Column("expires_at")]
        [Display(Name = "Vrijedi do")]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Reason for assignment/revocation/audit.
        /// </summary>
        [MaxLength(255)]
        [Column("reason")]
        [Display(Name = "Razlog")]
        public string? Reason { get; set; }

        /// <summary>
        /// Change version (event sourcing/rollback/forensics).
        /// </summary>
        [Column("change_version")]
        [Display(Name = "Verzija promjene")]
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Is this assignment active? (soft delete/archive/expiry support).
        /// </summary>
        [Column("is_active")]
        [Display(Name = "Aktivan")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Digital signature/hash for audit/compliance (SHA/PKI/device).
        /// </summary>
        [MaxLength(128)]
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Optional: Session ID for traceability/incident/forensics.
        /// </summary>
        [MaxLength(64)]
        [Column("session_id")]
        [Display(Name = "Sesija")]
        public string? SessionId { get; set; }

        /// <summary>
        /// Optional: Source IP/device for forensic/audit use.
        /// </summary>
        [MaxLength(64)]
        [Column("source_ip")]
        [Display(Name = "Izvorni IP")]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Audit note/comment (GMP, 21 CFR Part 11, incident linking).
        /// </summary>
        [MaxLength(512)]
        [Column("note")]
        [Display(Name = "Napomena")]
        public string? Note { get; set; }

        /// <summary>
        /// Returns true if this assignment is active and not expired.
        /// </summary>
        [NotMapped]
        public bool IsCurrentlyActive => IsActive && (!ExpiresAt.HasValue || ExpiresAt > DateTime.UtcNow);
    }
}

