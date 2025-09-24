using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YasGMP.Models.Enums;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>CalibrationAudit</b> – Forensic audit trail for every calibration change (GMP, 21 CFR Part 11).
    /// <para>
    /// Records: who, when, what changed, digital signature, old/new value, IP, device, and full traceability.
    /// </para>
    /// </summary>
    [Table("calibration_audit")]
    public class CalibrationAudit
    {
        /// <summary>
        /// Unique ID for this audit entry (Primary Key).
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Foreign Key – Calibration this audit is linked to.
        /// </summary>
        [Required]
        [Column("calibration_id")]
        public int CalibrationId { get; set; }

        /// <summary>
        /// Foreign Key – User who performed the action.
        /// </summary>
        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>
        /// Type of action performed (enum, not string).
        /// </summary>
        [Required]
        [Column("action")]
        public CalibrationActionType Action { get; set; }

        /// <summary>
        /// Optional details about this audit event (human readable).
        /// </summary>
        [Column("details")]
        [MaxLength(1000)]
        public string Details { get; set; } = string.Empty;

        /// <summary>
        /// Optional value before the change (for rollback/forensics).
        /// </summary>
        [Column("old_value")]
        public string OldValue { get; set; } = string.Empty;

        /// <summary>
        /// Optional value after the change.
        /// </summary>
        [Column("new_value")]
        public string NewValue { get; set; } = string.Empty;

        /// <summary>
        /// Exact date/time of this change (UTC).
        /// </summary>
        [Column("changed_at")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Freeform audit note (reason, extra context, CAPA reference).
        /// </summary>
        [Column("note")]
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// IP address (forensic trace).
        /// </summary>
        [MaxLength(45)]
        [Column("source_ip")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Device/computer metadata.
        /// </summary>
        [Column("device_info")]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>
        /// Digital signature or hash (21 CFR Part 11/Annex 11 trace).
        /// </summary>
        [MaxLength(128)]
        [Column("digital_signature")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Navigation to Calibration entity (required, EF-populated).
        /// </summary>
        [ForeignKey(nameof(CalibrationId))]
        public virtual Calibration Calibration { get; set; } = null!;
    }
}
