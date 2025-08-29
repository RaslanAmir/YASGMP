// File: Models/AdminActivityLog.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>AdminActivityLog</b> – Ultra-robust, forensic-level log of all privileged/admin actions and configuration changes.
    /// <para>• Fully GMP, CSV, and 21 CFR Part 11 compliant (forensics, rollback, traceability).</para>
    /// <para>• Every change is auditable, signed, and traceable by session, device, and user context.</para>
    /// <para>• Enables advanced incident, CAPA, and GDPR audit for all system-level events.</para>
    /// </summary>
    public class AdminActivityLog
    {
        /// <summary>
        /// Unique identifier for this log entry (primary key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID of the admin user who performed the action (foreign key to <see cref="User"/>).
        /// </summary>
        [Required]
        [Display(Name = "Admin korisnik")]
        public int AdminId { get; set; }

        /// <summary>
        /// Navigation property for the admin user who performed the action.
        /// </summary>
        [ForeignKey(nameof(AdminId))]
        public virtual User? Admin { get; set; }

        /// <summary>
        /// UTC timestamp of the action (forensic, 21 CFR Part 11).
        /// </summary>
        [Required]
        [Display(Name = "Vrijeme aktivnosti")]
        public DateTime ActivityTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Activity type performed (enum/string, e.g., "create_user", "delete", "reset_password", etc).
        /// </summary>
        [Required]
        [MaxLength(64)]
        [Display(Name = "Tip aktivnosti")]
        public string Activity { get; set; } = string.Empty;

        /// <summary>
        /// Target table/entity name (e.g., "users", "work_orders", "settings", ...).
        /// </summary>
        [Required]
        [MaxLength(64)]
        [Display(Name = "Entitet")]
        public string AffectedTable { get; set; } = string.Empty;

        /// <summary>
        /// ID of the affected record (nullable for global or config actions).
        /// </summary>
        [Display(Name = "ID entiteta")]
        public int? AffectedRecordId { get; set; }

        /// <summary>
        /// JSON with before/after diff, snapshot, raw SQL, or change summary.
        /// </summary>
        [MaxLength(2000)]
        [Display(Name = "Detalji promjene")]
        public string Details { get; set; } = string.Empty;

        /// <summary>
        /// IP address or device hostname where action was performed (forensics).
        /// </summary>
        [MaxLength(64)]
        [Display(Name = "IP adresa")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Device name/ID or hostname for multi-device traceability.
        /// </summary>
        [MaxLength(128)]
        [Display(Name = "Uređaj")]
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// Session identifier for the admin login (link to <see cref="SessionLog"/> if available).
        /// </summary>
        [MaxLength(64)]
        [Display(Name = "Session ID")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Digital signature, hash, or PIN for this action (for audit/GMP compliance).
        /// </summary>
        [MaxLength(256)]
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Change version for event sourcing, rollback, and traceability.
        /// </summary>
        [Display(Name = "Verzija promjene")]
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Soft delete flag (GDPR/archive support, true if record is deleted/archived).
        /// </summary>
        [Display(Name = "Arhivirano/obrisano")]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Freeform note, audit reference, incident, or CAPA link.
        /// </summary>
        [MaxLength(512)]
        [Display(Name = "Napomena")]
        public string Note { get; set; } = string.Empty;
    }
}
