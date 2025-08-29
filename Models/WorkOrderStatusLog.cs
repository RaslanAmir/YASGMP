using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>WorkOrderStatusLog</b> – History of all work order status changes.
    /// Forensic GMP record: who, when, from/to status, reason, incident flag, device, IP, audit note.
    /// </summary>
    [Table("work_order_status_log")]
    public class WorkOrderStatusLog
    {
        /// <summary>Unique ID of the status log (Primary Key).</summary>
        [Key]
        [Column("id")]
        [Display(Name = "ID status zapisa")]
        public int Id { get; set; }

        /// <summary>Work order ID this status log relates to (FK).</summary>
        [Required]
        [Column("work_order_id")]
        [Display(Name = "Radni nalog")]
        public int WorkOrderId { get; set; }

        /// <summary>Navigation to the work order.</summary>
        [ForeignKey(nameof(WorkOrderId))]
        public WorkOrder? WorkOrder { get; set; }

        /// <summary>Previous status ("otvoren", "u tijeku", ...).</summary>
        [Column("old_status")]
        [MaxLength(32)]
        [Display(Name = "Stari status")]
        public string OldStatus { get; set; } = string.Empty;

        /// <summary>New status after the change.</summary>
        [Required]
        [Column("new_status")]
        [MaxLength(32)]
        [Display(Name = "Novi status")]
        public string NewStatus { get; set; } = string.Empty;

        /// <summary>User who changed the status (FK to User).</summary>
        [Required]
        [Column("changed_by_id")]
        [Display(Name = "Promijenio korisnik")]
        public int ChangedById { get; set; }

        /// <summary>Navigation to the user who changed the status.</summary>
        [ForeignKey(nameof(ChangedById))]
        public User? ChangedBy { get; set; }

        /// <summary>Timestamp of the status change.</summary>
        [Required]
        [Column("changed_at")]
        [Display(Name = "Vrijeme promjene")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Reason for the status change (CAPA, inspection, comments...).</summary>
        [Column("reason")]
        [MaxLength(400)]
        [Display(Name = "Razlog promjene")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>Incident flag (is this change related to an alert/CAPA/forensics).</summary>
        [Column("is_incident")]
        [Display(Name = "Incident/CAPA")]
        public bool IsIncident { get; set; }

        /// <summary>Device/computer info where the change was made (forensics).</summary>
        [Column("device_info")]
        [MaxLength(255)]
        [Display(Name = "Uređaj/OS")]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>IP address of the user who changed the status (audit log, inspection).</summary>
        [Column("ip_address")]
        [MaxLength(45)]
        [Display(Name = "IP adresa")]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>Additional note (rollback, audit, inspection, dashboard).</summary>
        [Column("note")]
        [MaxLength(500)]
        [Display(Name = "Napomena")]
        public string Note { get; set; } = string.Empty;

        /// <summary>DeepCopy – for rollback, timeline, and forensic inspection.</summary>
        public WorkOrderStatusLog DeepCopy()
        {
            return new WorkOrderStatusLog
            {
                Id = this.Id,
                WorkOrderId = this.WorkOrderId,
                WorkOrder = this.WorkOrder,
                OldStatus = this.OldStatus,
                NewStatus = this.NewStatus,
                ChangedById = this.ChangedById,
                ChangedBy = this.ChangedBy,
                ChangedAt = this.ChangedAt,
                Reason = this.Reason,
                IsIncident = this.IsIncident,
                DeviceInfo = this.DeviceInfo,
                IpAddress = this.IpAddress,
                Note = this.Note
            };
        }
    }
}
