using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>UserPermissionOverride</b> – Individual, per-user permission grants or denials.
    /// Can override role-based permissions for escalation, emergencies, or fine-tuning.
    /// <para>
    /// ✅ Full GMP/CSV audit (who, when, why, duration)
    /// ✅ Used for emergency escalation, incident, or per-user compliance exceptions
    /// ✅ Forensic chain: reason, granter, timestamp, expiry
    /// </para>
    /// </summary>
    public class UserPermissionOverride
    {
        /// <summary>
        /// Unique override entry (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// User affected by this override (FK).
        /// </summary>
        [Required]
        [Display(Name = "Korisnik")]
        public int UserId { get; set; }

        /// <summary>
        /// Navigation to the affected user.
        /// </summary>
        public User User { get; set; } = null!;

        /// <summary>
        /// Permission being overridden (FK).
        /// </summary>
        [Required]
        [Display(Name = "Dozvola")]
        public int PermissionId { get; set; }

        /// <summary>
        /// Navigation to the permission.
        /// </summary>
        public Permission Permission { get; set; } = null!;

        /// <summary>
        /// True = granted, False = denied (overrides role-based permissions).
        /// </summary>
        [Display(Name = "Dodijeljeno")]
        public bool IsGranted { get; set; }

        /// <summary>
        /// Reason for override (audit, escalation, CAPA, incident, exception).
        /// </summary>
        [MaxLength(500)]
        [Display(Name = "Razlog")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Expiry date/time for the override (null = permanent).
        /// </summary>
        [Display(Name = "Vrijedi do")]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// User who granted this override (FK, for audit).
        /// </summary>
        [Display(Name = "Dodijelio")]
        public int? GrantedById { get; set; }

        /// <summary>
        /// Navigation to granting user.
        /// </summary>
        public User GrantedBy { get; set; } = null!;

        /// <summary>
        /// UTC timestamp when override was granted (audit).
        /// </summary>
        [Display(Name = "Dodijeljeno")]
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Returns true if the override is currently valid.
        /// </summary>
        public bool IsActive => !ExpiresAt.HasValue || ExpiresAt.Value > DateTime.UtcNow;
    }
}
