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
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("intervention_id")]
        [Required]
        public int InterventionId { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [MaxLength(30)]
        [Column("action")]
        public string Action { get; set; } = string.Empty;

        [Column("details", TypeName = "text")]
        public string? Details { get; set; }

        [Column("changed_at")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(45)]
        [Column("source_ip")]
        public string? SourceIp { get; set; }

        [MaxLength(255)]
        [Column("device_info")]
        public string? DeviceInfo { get; set; }

        [MaxLength(100)]
        [Column("session_id")]
        public string? SessionId { get; set; }

        [MaxLength(255)]
        [Column("digital_signature")]
        public string? DigitalSignature { get; set; }

        [Column("old_value", TypeName = "text")]
        public string? OldValue { get; set; }

        [Column("new_value", TypeName = "text")]
        public string? NewValue { get; set; }

        [Column("note", TypeName = "text")]
        public string? Note { get; set; }
    }
}

