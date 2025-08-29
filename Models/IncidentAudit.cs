using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YasGMP.Models.Enums;

namespace YasGMP.Models
{
    [Table("incident_audit")]
    public class IncidentAudit
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("incident_id")]
        public int IncidentId { get; set; }

        [ForeignKey(nameof(IncidentId))]
        public Incident? Incident { get; set; }

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>Use IncidentActionType (not WorkOrderActionType)</summary>
        [Required]
        [Column("action")]
        public IncidentActionType Action { get; set; }

        [Column("old_value")]
        public string? OldValue { get; set; }

        [Column("new_value")]
        public string? NewValue { get; set; }

        [Required]
        [Column("action_at")]
        public DateTime ActionAt { get; set; } = DateTime.UtcNow;

        [Column("note")]
        [StringLength(500)]
        public string? Note { get; set; }

        [Column("source_ip")]
        [StringLength(128)]
        public string? SourceIp { get; set; }

        [Column("digital_signature")]
        [StringLength(128)]
        public string? DigitalSignature { get; set; }

        [Column("capa_id")]
        public int? CapaId { get; set; }

        [ForeignKey(nameof(CapaId))]
        public CapaCase? CapaCase { get; set; }

        [Column("work_order_id")]
        public int? WorkOrderId { get; set; }

        [ForeignKey(nameof(WorkOrderId))]
        public WorkOrder? WorkOrder { get; set; }

        [Column("device_info")]
        [StringLength(256)]
        public string? DeviceInfo { get; set; }

        [Column("integrity_hash")]
        [StringLength(128)]
        public string? IntegrityHash { get; set; }

        [Column("inspector_note")]
        [StringLength(500)]
        public string? InspectorNote { get; set; }
    }
}
