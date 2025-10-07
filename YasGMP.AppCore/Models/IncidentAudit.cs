using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YasGMP.Models.Enums;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents the Incident Audit.
    /// </summary>
    [Table("incident_audit")]
    public partial class IncidentAudit
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the incident id.
        /// </summary>
        [NotMapped]
        public int IncidentId { get; set; }

        /// <summary>
        /// Gets or sets the incident.
        /// </summary>
        [NotMapped]
        public Incident? Incident { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        [NotMapped]
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        [NotMapped]
        public User? User { get; set; }

        /// <summary>Use IncidentActionType (not WorkOrderActionType)</summary>
        [NotMapped]
        public IncidentActionType Action { get; set; }

        /// <summary>
        /// Gets or sets the old value.
        /// </summary>
        [NotMapped]
        public string? OldValue { get; set; }

        /// <summary>
        /// Gets or sets the new value.
        /// </summary>
        [NotMapped]
        public string? NewValue { get; set; }

        /// <summary>
        /// Gets or sets the action at.
        /// </summary>
        [NotMapped]
        public DateTime ActionAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        [NotMapped]
        [StringLength(500)]
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets the source ip.
        /// </summary>
        [NotMapped]
        [StringLength(128)]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Gets or sets the digital signature.
        /// </summary>
        [NotMapped]
        [StringLength(128)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Gets or sets the capa id.
        /// </summary>
        [NotMapped]
        public int? CapaId { get; set; }

        /// <summary>
        /// Gets or sets the capa case.
        /// </summary>
        [NotMapped]
        public CapaCase? CapaCase { get; set; }

        /// <summary>
        /// Gets or sets the work order id.
        /// </summary>
        [NotMapped]
        public int? WorkOrderId { get; set; }

        /// <summary>
        /// Gets or sets the work order.
        /// </summary>
        [NotMapped]
        public WorkOrder? WorkOrder { get; set; }

        /// <summary>
        /// Gets or sets the device info.
        /// </summary>
        [NotMapped]
        [StringLength(256)]
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// Gets or sets the integrity hash.
        /// </summary>
        [NotMapped]
        [StringLength(128)]
        public string? IntegrityHash { get; set; }

        /// <summary>
        /// Gets or sets the inspector note.
        /// </summary>
        [NotMapped]
        [StringLength(500)]
        public string? InspectorNote { get; set; }
    }
}
