using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>PartChangeLog</b> – Full forensic, regulatory, and business audit log for all changes to spare part master data.
    /// <para>
    /// ✅ 21 CFR Part 11, HALMED, EU GMP, FDA, ISO 9001/13485, ERP/AI/ML ready  
    /// ✅ Tracks every field change: who, when, what, before/after, signature, IP/device  
    /// ✅ Ready for rollback, compliance review, inspector export, and AI anomaly detection
    /// </para>
    /// </summary>
    public class PartChangeLog
    {
        /// <summary>
        /// Unique log entry ID (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// FK – Part whose data changed.
        /// </summary>
        [Required]
        public int PartId { get; set; }
        public Part? Part { get; set; }

        /// <summary>
        /// Date/time of the change (UTC, always for audit/compliance).
        /// </summary>
        [Required]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// FK – User who made the change (audit chain).
        /// </summary>
        [Required]
        public int ChangedById { get; set; }
        public User? ChangedBy { get; set; }

        /// <summary>
        /// Name of the field/property changed (e.g., "Name", "Barcode", "WarrantyUntil").
        /// </summary>
        [Required, MaxLength(100)]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Old value (before the change; stored as string or JSON for all types).
        /// </summary>
        [MaxLength(2048)]
        public string? OldValue { get; set; }

        /// <summary>
        /// New value (after the change; stored as string or JSON for all types).
        /// </summary>
        [MaxLength(2048)]
        public string? NewValue { get; set; }

        /// <summary>
        /// Reason for the change (e.g., "periodic update", "inspection", "CAPA", "error correction", "recall").
        /// </summary>
        [MaxLength(400)]
        public string? Reason { get; set; }

        /// <summary>
        /// Incident, CAPA, or inspection reference (traceability).
        /// </summary>
        [MaxLength(100)]
        public string? Reference { get; set; }

        /// <summary>
        /// Digital signature or hash of the change (for forensic/audit/21 CFR compliance).
        /// </summary>
        [MaxLength(128)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Device/IP info for full forensic traceability.
        /// </summary>
        [MaxLength(64)]
        public string? SourceIp { get; set; }

        /// <summary>
        /// ML/AI anomaly score (for future analytics, risk, fraud, or prediction).
        /// </summary>
        public double? AnomalyScore { get; set; }

        /// <summary>
        /// Free text comment for auditor, inspector, or supervisor.
        /// </summary>
        [MaxLength(400)]
        public string? Note { get; set; }

        /// <summary>
        /// Soft delete/archive (GDPR, not physical delete).
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Returns human-readable summary for audit logs/inspector.
        /// </summary>
        public override string ToString()
        {
            return $"{FieldName}: \"{OldValue}\" → \"{NewValue}\" by User#{ChangedById} on {ChangedAt:yyyy-MM-dd HH:mm}";
        }
    }
}

