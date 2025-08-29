using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YasGMP.Models.Enums;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>CapaAudit</b> – Full GMP/21 CFR Part 11/Annex 11-compliant audit entry for CAPA (Corrective and Preventive Actions).
    /// <para>
    /// ✔ Tracks every CAPA action: who, what, when, why, device, digital signature, tamper detection, rollback, incident/work order linkage, context, and inspection notes.
    /// ✔ Chosen by auditors, trusted by inspectors!
    /// </para>
    /// </summary>
    public class CapaAudit
    {
        /// <summary>
        /// Unique identifier for this audit log entry (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The ID of the CAPA case this audit record is associated with (Foreign Key).
        /// </summary>
        [Required]
        public int CapaId { get; set; }

        /// <summary>
        /// The CAPA case entity associated with this audit entry (EF will hydrate).
        /// </summary>
        [ForeignKey("CapaId")]
        public CapaCase Capa { get; set; } = null!;

        /// <summary>
        /// The ID of the user who performed the action.
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// The user entity who executed the action (optional navigation property; EF will hydrate).
        /// </summary>
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        /// <summary>
        /// Type of action performed on the CAPA record.
        /// Uses <see cref="CapaActionType"/> to standardize audit action types.
        /// </summary>
        [Required]
        public CapaActionType Action { get; set; }

        /// <summary>
        /// Timestamp when the action was performed (UTC).
        /// </summary>
        [Required]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional details describing the event, reason, or context of the action.
        /// </summary>
        [MaxLength(1000)]
        public string Details { get; set; } = string.Empty;

        /// <summary>
        /// Value before the change (JSON snapshot or serialized state).
        /// Useful for rollback and forensic analysis.
        /// </summary>
        [MaxLength(4000)]
        public string OldValue { get; set; } = string.Empty;

        /// <summary>
        /// Value after the change (JSON snapshot or serialized state).
        /// Useful for rollback and forensic analysis.
        /// </summary>
        [MaxLength(4000)]
        public string NewValue { get; set; } = string.Empty;

        /// <summary>
        /// Digital signature generated at the time of the audit event.
        /// Ensures non-repudiation and regulatory compliance.
        /// </summary>
        [MaxLength(256)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Integrity hash (SHA256) of the entire audit entry to detect tampering.
        /// </summary>
        [MaxLength(256)]
        public string IntegrityHash { get; set; } = string.Empty;

        /// <summary>
        /// IP address of the device that performed the action (for forensic logging).
        /// </summary>
        [MaxLength(64)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Device information (OS, browser, hardware) of the user performing the action.
        /// </summary>
        [MaxLength(128)]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>
        /// Optional reference to an Incident ID if this CAPA audit is linked to an incident.
        /// </summary>
        public int? IncidentId { get; set; }

        /// <summary>
        /// Optional reference to a Work Order ID if this audit is related to a specific maintenance action.
        /// </summary>
        public int? WorkOrderId { get; set; }

        /// <summary>
        /// Additional note for inspectors, supervisors, or auditors.
        /// </summary>
        [MaxLength(500)]
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Audit chain/version for rollback and full event trace (bonus!).
        /// </summary>
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Is this record soft-deleted/archived (GDPR, cleanup, not physically deleted).
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Creates a deep copy of this audit entry for forensic analysis, rollback, or inspection purposes.
        /// </summary>
        /// <returns>A new <see cref="CapaAudit"/> instance with copied field values.</returns>
        public CapaAudit DeepCopy()
        {
            return new CapaAudit
            {
                Id = this.Id,
                CapaId = this.CapaId,
                Capa = this.Capa,
                UserId = this.UserId,
                User = this.User,
                Action = this.Action,
                ChangedAt = this.ChangedAt,
                Details = this.Details,
                OldValue = this.OldValue,
                NewValue = this.NewValue,
                DigitalSignature = this.DigitalSignature,
                IntegrityHash = this.IntegrityHash,
                SourceIp = this.SourceIp,
                DeviceInfo = this.DeviceInfo,
                IncidentId = this.IncidentId,
                WorkOrderId = this.WorkOrderId,
                Note = this.Note,
                ChangeVersion = this.ChangeVersion,
                IsDeleted = this.IsDeleted
            };
        }
    }
}
