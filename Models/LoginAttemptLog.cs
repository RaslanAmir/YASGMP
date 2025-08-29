using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>LoginAttemptLog</b> — THE most robust, forensic, fully auditable login attempt log for user/system access.
    /// Tracks every login attempt (success/failure), user, device, reason, forensic evidence, risk score, geo, AI/ML fields.
    /// Fully compliant with GMP, CSV, 21 CFR Part 11, ISO, SOX, ITIL, banking, and future cyber/AI regulations.
    /// </summary>
    public class LoginAttemptLog
    {
        /// <summary>
        /// Unique log entry ID (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// UTC timestamp of the login attempt.
        /// </summary>
        [Required]
        [Display(Name = "Datum/vrijeme pokušaja (UTC)")]
        public DateTime AttemptTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User ID if matched (FK, nullable for failed/anonymous attempts).
        /// </summary>
        [Display(Name = "Korisnik")]
        public int? UserId { get; set; }

        /// <summary>
        /// Navigation to user (nullable for anonymous attempts).
        /// </summary>
        public User? User { get; set; }

        /// <summary>
        /// Username attempted (snapshot, for non-existent or deactivated users).
        /// </summary>
        [Display(Name = "Pokušano korisničko ime")]
        [StringLength(100)]
        public string? UsernameAttempt { get; set; }

        /// <summary>
        /// Was login successful?
        /// </summary>
        [Display(Name = "Uspješna prijava")]
        public bool Success { get; set; }

        /// <summary>
        /// If failed: Reason (wrong password, locked, expired, not found, 2FA failed, etc.).
        /// </summary>
        [Display(Name = "Razlog neuspjeha")]
        [StringLength(200)]
        public string? FailReason { get; set; }

        /// <summary>
        /// Forensic: Device, OS, browser, app version, user agent.
        /// </summary>
        [Display(Name = "Uređaj/Agent")]
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// Forensic: IP address of the login attempt.
        /// </summary>
        [Display(Name = "IP adresa")]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Geolocation (city, country, or coordinates) if available.
        /// </summary>
        [Display(Name = "Geolokacija")]
        public string? GeoLocation { get; set; }

        /// <summary>
        /// Digital signature/hash of the attempt (integrity, compliance, 21 CFR Part 11).
        /// </summary>
        [Display(Name = "Digitalni potpis")]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Was the user locked out as a result of this attempt?
        /// </summary>
        [Display(Name = "Zaključavanje")]
        public bool LockedOut { get; set; }

        /// <summary>
        /// Number of failed attempts in this session/window.
        /// </summary>
        [Display(Name = "Broj pokušaja (window)")]
        public int FailedAttemptsCount { get; set; }

        /// <summary>
        /// Session ID or token (for traceability and analytics).
        /// </summary>
        [Display(Name = "Session/Token ID")]
        public string? SessionId { get; set; }

        /// <summary>
        /// Two-factor/MFA attempted?
        /// </summary>
        [Display(Name = "Pokušana 2FA/MFA")]
        public bool TwoFactorAttempted { get; set; }

        /// <summary>
        /// Two-factor/MFA success (if applicable).
        /// </summary>
        [Display(Name = "2FA/MFA uspjeh")]
        public bool? TwoFactorSuccess { get; set; }

        /// <summary>
        /// AI/ML anomaly score (future risk analytics).
        /// </summary>
        public double? AnomalyScore { get; set; }

        /// <summary>
        /// Was this login attempt by an automated script, API, or bot?
        /// </summary>
        public bool IsAutomated { get; set; }

        /// <summary>
        /// Inspector/auditor/user comment (optional).
        /// </summary>
        [Display(Name = "Napomena")]
        public string? Note { get; set; }

        /// <summary>
        /// Previous login attempt ID (for chain navigation/analytics).
        /// </summary>
        public int? PreviousLoginAttemptId { get; set; }

        /// <summary>
        /// Next login attempt ID (for chain navigation/analytics).
        /// </summary>
        public int? NextLoginAttemptId { get; set; }

        /// <summary>
        /// Reserved for extensibility (custom audit fields, schema upgrades).
        /// </summary>
        public string? ExtensionJson { get; set; }
    }
}
