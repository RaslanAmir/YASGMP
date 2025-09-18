using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>SessionLog</b> – Advanced log of user sessions, forensics, impersonation, temporary escalation, and device/IP tracking.
    /// </summary>
    public class SessionLog
    {
        /// <summary>Unique identifier for the session log (Primary Key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>User ID associated with this session (FK).</summary>
        [Required]
        [Display(Name = "Korisnik")]
        public int UserId { get; set; }

        /// <summary>Navigation property for the user.</summary>
        public User User { get; set; } = null!;

        /// <summary>Session/token ID (for cross-system traceability).</summary>
        [MaxLength(100)]
        [Display(Name = "Session ID")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>Login timestamp (UTC).</summary>
        [Display(Name = "Vrijeme prijave")]
        public DateTime LoginAt { get; set; }

        /// <summary>Logout timestamp (UTC, nullable).</summary>
        [Display(Name = "Vrijeme odjave")]
        public DateTime? LogoutAt { get; set; }

        /// <summary>IP address for this session.</summary>
        [MaxLength(64)]
        [Display(Name = "IP adresa")]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>Device or environment info (browser, OS, host, etc.).</summary>
        [MaxLength(256)]
        [Display(Name = "Info o uređaju")]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>True if the session started as another user (impersonation/override).</summary>
        [Display(Name = "Impersonacija")]
        public bool IsImpersonated { get; set; }

        /// <summary>If impersonated, the User ID of the original user (FK).</summary>
        [Display(Name = "Impersonirao korisnik")]
        public int? ImpersonatedById { get; set; }

        /// <summary>Navigation property for impersonating user.</summary>
        public User? ImpersonatedBy { get; set; }

        /// <summary>True if temporary escalation was granted for this session.</summary>
        [Display(Name = "Privremena eskalacija")]
        public bool IsTemporaryEscalation { get; set; }

        /// <summary>Optional note for compliance, anomaly, or audit.</summary>
        [MaxLength(400)]
        [Display(Name = "Napomena")]
        public string Note { get; set; } = string.Empty;
    }
}
