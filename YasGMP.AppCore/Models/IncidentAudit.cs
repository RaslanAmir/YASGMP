using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YasGMP.Models.Enums;

namespace YasGMP.Models
{
    [Table("incident_audit")]
    public partial class IncidentAudit
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [NotMapped]
        public int IncidentId { get; set; }

        [NotMapped]
        public Incident? Incident { get; set; }

        [NotMapped]
        public int UserId { get; set; }

        [NotMapped]
        public User? User { get; set; }

        /// <summary>Use IncidentActionType (not WorkOrderActionType)</summary>
        [NotMapped]
        public IncidentActionType Action { get; set; }

        [NotMapped]
        public string? OldValue { get; set; }

        [NotMapped]
        public string? NewValue { get; set; }

        [NotMapped]
        public DateTime ActionAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        [StringLength(500)]
        public string? Note { get; set; }

        [NotMapped]
        [StringLength(128)]
        public string? SourceIp { get; set; }

        [NotMapped]
        [StringLength(128)]
        public string? DigitalSignature { get; set; }

        [NotMapped]
        public int? CapaId { get; set; }

        [NotMapped]
        public CapaCase? CapaCase { get; set; }

        [NotMapped]
        public int? WorkOrderId { get; set; }

        [NotMapped]
        public WorkOrder? WorkOrder { get; set; }

        [NotMapped]
        [StringLength(256)]
        public string? DeviceInfo { get; set; }

        [NotMapped]
        [StringLength(128)]
        public string? IntegrityHash { get; set; }

        [NotMapped]
        [StringLength(500)]
        public string? InspectorNote { get; set; }
    }
}

