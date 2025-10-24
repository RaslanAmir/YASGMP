using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YasGMP.Models.Enums;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>WorkOrderAudit</b> – GMP/21 CFR Part 11 compliant forensic record for all work order actions.
    /// <para>
    /// ✅ Tracks every CREATE, UPDATE, DELETE, CLOSE, SIGN, EXPORT, and ROLLBACK action.<br/>
    /// ✅ Contains full forensic metadata (IP, device, session, timestamps) and cryptographic integrity mechanisms.<br/>
    /// ✅ Supports rollback, inspection, CAPA linkage, and audit trail compliance.
    /// </para>
    /// </summary>
    [Table("work_order_audit")]
    public partial class WorkOrderAudit
    {
        /// <summary>Unique audit log entry ID (Primary Key).</summary>
        [Key]
        [Column("id")]
        [Display(Name = "ID zapisa audita")]
        public int Id { get; set; }

        /// <summary>ID of the work order this audit entry relates to (FK).</summary>
        [Required]
        [Column("work_order_id")]
        [Display(Name = "ID radnog naloga")]
        public int WorkOrderId { get; set; }

        /// <summary>Navigation property for the related work order.</summary>
        [ForeignKey(nameof(WorkOrderId))]
        public WorkOrder? WorkOrder { get; set; }

        /// <summary>ID of the user who performed the action (FK).</summary>
        [Required]
        [Column("user_id")]
        [Display(Name = "Korisnik")]
        public int UserId { get; set; }

        /// <summary>Navigation property for the related user.</summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>Type of action performed on the work order (CREATE, UPDATE, DELETE, SIGN, EXPORT, ROLLBACK, etc.).</summary>
        [Required]
        [Column("action")]
        [Display(Name = "Akcija")]
        public WorkOrderActionType Action { get; set; }

        /// <summary>JSON snapshot of the work order state BEFORE the change (for rollback/inspection).</summary>
        [Column("old_value", TypeName = "text")]
        [Display(Name = "Stanje prije")]
        public string OldValue { get; set; } = string.Empty;

        /// <summary>JSON snapshot of the work order state AFTER the change (for rollback/inspection).</summary>
        [Column("new_value", TypeName = "text")]
        [Display(Name = "Stanje poslije")]
        public string NewValue { get; set; } = string.Empty;

        /// <summary>Date and time when the action was performed (UTC).</summary>
        [Required]
        [Column("changed_at")]
        [Display(Name = "Vrijeme izmjene")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>IP address of the user/device that executed the action (for forensic tracking).</summary>
        [MaxLength(45)]
        [Column("source_ip")]
        [Display(Name = "IP adresa")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Device/host information (browser, OS, hardware) used during the action (for forensic evidence).</summary>
        [MaxLength(255)]
        [Column("device_info")]
        [Display(Name = "Info o uređaju")]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>Forensic session ID for full traceability.</summary>
        [MaxLength(64)]
        [Column("session_id")]
        [Display(Name = "Session ID")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>Linked incident ID if this action is associated with an incident.</summary>
        [Column("incident_id")]
        [Display(Name = "Incident")]
        public int? IncidentId { get; set; }

        /// <summary>Linked CAPA case ID if this action is associated with a CAPA process.</summary>
        [Column("capa_id")]
        [Display(Name = "CAPA slučaj")]
        public int? CapaId { get; set; }

        /// <summary>Optional descriptive note providing context for the change (reason, references, inspection notes).</summary>
        [MaxLength(1000)]
        [Column("note")]
        [Display(Name = "Napomena")]
        public string Note { get; set; } = string.Empty;

        /// <summary>Digital signature (Base64 SHA256) ensuring non-repudiation of this audit record.</summary>
        [MaxLength(256)]
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Integrity hash (SHA256) verifying that this audit record has not been tampered with.</summary>
        [MaxLength(256)]
        [Column("integrity_hash")]
        [Display(Name = "Integritetni hash")]
        public string IntegrityHash { get; set; } = string.Empty;

        /// <summary>Creates a deep copy of this audit record (used for rollback, inspection, and forensic analysis).</summary>
        public WorkOrderAudit DeepCopy()
        {
            return new WorkOrderAudit
            {
                Id = this.Id,
                WorkOrderId = this.WorkOrderId,
                WorkOrder = this.WorkOrder,
                UserId = this.UserId,
                User = this.User,
                Action = this.Action,
                OldValue = this.OldValue,
                NewValue = this.NewValue,
                ChangedAt = this.ChangedAt,
                SourceIp = this.SourceIp,
                DeviceInfo = this.DeviceInfo,
                SessionId = this.SessionId,
                IncidentId = this.IncidentId,
                CapaId = this.CapaId,
                Note = this.Note,
                DigitalSignature = this.DigitalSignature,
                IntegrityHash = this.IntegrityHash
            };
        }
    }
}

