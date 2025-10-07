using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Forensic audit entry describing how a contractor intervention was created or modified, with user/device context.
    /// </summary>
    [Table("contractor_intervention_audit")]
    public partial class ContractorInterventionAudit
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the intervention id.
        /// </summary>
        [Column("intervention_id")]
        [Required]
        public int InterventionId { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        [Column("user_id")]
        public int? UserId { get; set; }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        [MaxLength(30)]
        [Column("action")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the details.
        /// </summary>
        [Column("details", TypeName = "text")]
        public string? Details { get; set; }

        /// <summary>
        /// Gets or sets the changed at.
        /// </summary>
        [Column("changed_at")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the source ip.
        /// </summary>
        [MaxLength(45)]
        [Column("source_ip")]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Gets or sets the device info.
        /// </summary>
        [MaxLength(255)]
        [Column("device_info")]
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// Gets or sets the session id.
        /// </summary>
        [MaxLength(100)]
        [Column("session_id")]
        public string? SessionId { get; set; }

        /// <summary>
        /// Gets or sets the digital signature.
        /// </summary>
        [MaxLength(255)]
        [Column("digital_signature")]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Gets or sets the old value.
        /// </summary>
        [Column("old_value", TypeName = "text")]
        public string? OldValue { get; set; }

        /// <summary>
        /// Gets or sets the new value.
        /// </summary>
        [Column("new_value", TypeName = "text")]
        public string? NewValue { get; set; }

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        [Column("note", TypeName = "text")]
        public string? Note { get; set; }
    }
}
