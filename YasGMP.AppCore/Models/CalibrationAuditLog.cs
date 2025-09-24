using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>CalibrationAuditLog</b> – Tracks all audit trail events for calibrations (create, update, delete, export).
    /// <para>
    /// ✅ Forensic GMP record: who, when, action type, old/new values, device/IP, note.
    /// ✅ Fully supports 21 CFR Part 11, EU GMP Annex 11, and HALMED inspection requirements.
    /// </para>
    /// </summary>
    [Table("calibration_audit_log")]
    public class CalibrationAuditLog
    {
        /// <summary>
        /// Unique ID of the audit log (Primary Key).
        /// </summary>
        [Key]
        [Column("id")]
        [Display(Name = "ID audita")]
        public int Id { get; set; }

        /// <summary>
        /// FK – ID of the calibration record being audited.
        /// </summary>
        [Required]
        [Column("calibration_id")]
        [Display(Name = "Kalibracija")]
        public int CalibrationId { get; set; }

        /// <summary>
        /// Navigation to the calibration record (required, EF-populated).
        /// </summary>
        [ForeignKey(nameof(CalibrationId))]
        public virtual Calibration Calibration { get; set; } = null!;

        /// <summary>
        /// FK – User who performed the action (optional).
        /// </summary>
        [Column("user_id")]
        [Display(Name = "Korisnik")]
        public int? UserId { get; set; }

        /// <summary>
        /// Navigation property for the user who performed the action (optional).
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        /// <summary>
        /// Action type (CREATE, UPDATE, DELETE, EXPORT, etc).
        /// </summary>
        [Required]
        [Column("action")]
        [StringLength(16)]
        [Display(Name = "Akcija")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Previous value (e.g. JSON or string, for update/delete; NULL on create).
        /// </summary>
        [Column("old_value")]
        [Display(Name = "Stara vrijednost")]
        public string OldValue { get; set; } = string.Empty;

        /// <summary>
        /// New value (e.g. JSON or string, for update/create; NULL on delete).
        /// </summary>
        [Column("new_value")]
        [Display(Name = "Nova vrijednost")]
        public string NewValue { get; set; } = string.Empty;

        /// <summary>
        /// Date/time of the audit action (UTC).
        /// </summary>
        [Required]
        [Column("changed_at")]
        [Display(Name = "Vrijeme promjene")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Forensic: IP address/device where the change occurred.
        /// </summary>
        [Column("source_ip")]
        [StringLength(45)]
        [Display(Name = "IP adresa")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Freeform note (reason, details, regulatory note).
        /// </summary>
        [Column("note")]
        [StringLength(500)]
        [Display(Name = "Napomena")]
        public string Note { get; set; } = string.Empty;
    }
}
