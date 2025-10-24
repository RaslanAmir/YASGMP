using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YasGMP.Models.Enums;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>UserRoleAssignment</b> – Maps a user to a role, supporting GMP/CSV/21 CFR/Annex 11 traceability.
    /// <para>
    /// • Multi-role, escalation, incident or temporary roles (expiry), digital signature, advanced audit.
    /// • Use with RBAC/permission services for workflow, access, reporting.
    /// </para>
    /// </summary>
    [Table("user_role_assignments")]
    public class UserRoleAssignment
    {
        /// <summary>
        /// Unique assignment ID (Primary Key, auto-increment).
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// User assigned to this role (Foreign Key).
        /// </summary>
        [Required]
        [Display(Name = "Korisnik")]
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>
        /// Navigation property to assigned user.
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        /// <summary>
        /// Role assigned to the user (Foreign Key).
        /// </summary>
        [Required]
        [Display(Name = "Uloga")]
        [Column("role_id")]
        public int RoleId { get; set; }

        /// <summary>
        /// Navigation property to role.
        /// </summary>
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; } = null!;

        /// <summary>
        /// Expiry date/time for temporary role assignments (null = permanent, supports escalation/incident/temporary).
        /// </summary>
        [Display(Name = "Vrijedi do")]
        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// UTC date/time when assignment was granted (audit/compliance).
        /// </summary>
        [Display(Name = "Dodijeljeno")]
        [Column("granted_at")]
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who granted this role (Foreign Key, for audit/incident traceability).
        /// </summary>
        [Display(Name = "Dodijelio")]
        [Column("granted_by")]
        public int? GrantedById { get; set; }

        /// <summary>
        /// Navigation property to grantor.
        /// </summary>
        [ForeignKey("GrantedById")]
        public virtual User GrantedBy { get; set; } = null!;

        /// <summary>
        /// Reason for assignment, escalation, incident, or audit trail.
        /// </summary>
        [MaxLength(512)]
        [Display(Name = "Napomena")]
        [Column("note")]
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Digital signature or hash (for forensic audit and GMP).
        /// </summary>
        [MaxLength(128)]
        [Display(Name = "Digitalni potpis")]
        [Column("digital_signature")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Source IP address of the assigner (for audit trail and forensic linkage).
        /// </summary>
        [MaxLength(64)]
        [Display(Name = "Izvorni IP")]
        [Column("source_ip")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Originating device info (browser, OS, hostname, etc).
        /// </summary>
        [MaxLength(128)]
        [Display(Name = "Uređaj")]
        [Column("device_info")]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>
        /// Session ID (for session-level audit correlation).
        /// </summary>
        [MaxLength(64)]
        [Display(Name = "Sesija")]
        [Column("session_id")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Version for event sourcing/rollback (GMP/CSV).
        /// </summary>
        [Display(Name = "Verzija promjene")]
        [Column("change_version")]
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Returns true if this assignment is currently active (not expired).
        /// </summary>
        [NotMapped]
        public bool IsActive => !ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow;
    }
}

