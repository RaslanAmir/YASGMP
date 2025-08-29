using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>WorkOrderSignature</b> – Digital or manual signature for any work order step.
    /// Tracks user, signature type, digital hash, PIN, device, IP address, incident flag, notes, and more.
    /// Every signature is forensically traceable for GMP, CSV, and 21 CFR Part 11 inspection!
    /// </summary>
    [Table("work_order_signature")]
    public class WorkOrderSignature
    {
        /// <summary>Unique signature ID (Primary Key).</summary>
        [Key]
        [Column("id")]
        [Display(Name = "ID potpisa")]
        public int Id { get; set; }

        /// <summary>Work order ID this signature relates to (FK).</summary>
        [Required]
        [Column("work_order_id")]
        [Display(Name = "Radni nalog")]
        public int WorkOrderId { get; set; }

        /// <summary>Navigation to work order.</summary>
        [ForeignKey(nameof(WorkOrderId))]
        public WorkOrder? WorkOrder { get; set; }

        /// <summary>User who signed (FK to User).</summary>
        [Required]
        [Column("user_id")]
        [Display(Name = "Korisnik")]
        public int UserId { get; set; }

        /// <summary>Navigation to user.</summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>Timestamp of signature (forensic timestamp).</summary>
        [Required]
        [Column("signed_at")]
        [Display(Name = "Vrijeme potpisa")]
        public DateTime SignedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Signature type: digital, manual, PIN, cert, API, approval, review, etc.</summary>
        [Required]
        [Column("type")]
        [MaxLength(30)]
        [Display(Name = "Tip potpisa")]
        public string Type { get; set; } = string.Empty;

        /// <summary>Digital hash of signature (SHA-256, GMP inspection, CSV, rollback).</summary>
        [Column("hash")]
        [MaxLength(128)]
        [Display(Name = "Hash potpisa")]
        public string Hash { get; set; } = string.Empty;

        /// <summary>PIN used (if any; hashed or masked).</summary>
        [Column("pin_used")]
        [MaxLength(32)]
        [Display(Name = "Korišteni PIN")]
        public string PinUsed { get; set; } = string.Empty;

        /// <summary>Device/computer info from which the signature was made (forensics).</summary>
        [Column("device_info")]
        [MaxLength(255)]
        [Display(Name = "Uređaj/OS")]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>IP address of signer (inspection, compliance).</summary>
        [Column("ip_address")]
        [MaxLength(45)]
        [Display(Name = "IP adresa")]
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>Incident/CAPA flag (bonus for alerting inspection).</summary>
        [Column("is_incident")]
        [Display(Name = "Incident/CAPA zastavica")]
        public bool IsIncident { get; set; }

        /// <summary>Additional note (audit, rollback, reason for signature).</summary>
        [Column("note")]
        [MaxLength(500)]
        [Display(Name = "Napomena")]
        public string Note { get; set; } = string.Empty;

        /// <summary>DeepCopy – for rollback, audit, signature history, forensic checks.</summary>
        public WorkOrderSignature DeepCopy()
        {
            return new WorkOrderSignature
            {
                Id = this.Id,
                WorkOrderId = this.WorkOrderId,
                WorkOrder = this.WorkOrder,
                UserId = this.UserId,
                User = this.User,
                SignedAt = this.SignedAt,
                Type = this.Type,
                Hash = this.Hash,
                PinUsed = this.PinUsed,
                DeviceInfo = this.DeviceInfo,
                IpAddress = this.IpAddress,
                IsIncident = this.IsIncident,
                Note = this.Note
            };
        }
    }
}
