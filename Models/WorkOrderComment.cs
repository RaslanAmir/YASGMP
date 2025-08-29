using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>WorkOrderComment</b> – Comment or revision for a work order.
    /// <para>
    /// ✅ Each comment/change is fully auditable: who, when, what, why (GMP, CAPA, inspection!).<br/>
    /// ✅ Used for work order history, CAPA revisions, inspection notes, status changes, and full timeline review.
    /// </para>
    /// </summary>
    [Table("work_order_comment")]
    public class WorkOrderComment
    {
        /// <summary>Unique comment ID (Primary Key).</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Work order ID this comment belongs to (FK).</summary>
        [Required]
        [Column("work_order_id")]
        [Display(Name = "ID radnog naloga")]
        public int WorkOrderId { get; set; }

        /// <summary>Navigation property for the related work order.</summary>
        [ForeignKey(nameof(WorkOrderId))]
        public WorkOrder? WorkOrder { get; set; }

        /// <summary>Comment/revision text (max 1000 chars; mandatory for CAPA/inspection!).</summary>
        [Required]
        [MaxLength(1000)]
        [Column("text")]
        [Display(Name = "Komentar / Revizija")]
        public string Text { get; set; } = string.Empty;

        /// <summary>User who left the comment (FK).</summary>
        [Required]
        [Column("user_id")]
        [Display(Name = "Korisnik")]
        public int UserId { get; set; }

        /// <summary>Navigation property for the related user.</summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>Date/time of the comment (audit log; timeline display!).</summary>
        [Column("created_at")]
        [Display(Name = "Vrijeme unosa")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Comment type: "comment", "revision", "CAPA", "notification", "incident", "auditor", etc.</summary>
        [MaxLength(32)]
        [Column("type")]
        [Display(Name = "Tip komentara")]
        public string Type { get; set; } = string.Empty;

        /// <summary>Revision or version number (linked to rollback or CAPA revision).</summary>
        [Column("revision_no")]
        [Display(Name = "Revizija")]
        public int RevisionNo { get; set; } = 1;

        /// <summary>User's digital signature (hash/PIN) — full GMP/CSV audit!</summary>
        [MaxLength(128)]
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Is this comment related to an incident, CAPA, or forensic inspection.</summary>
        [Column("is_critical")]
        [Display(Name = "Kritičan komentar")]
        public bool IsCritical { get; set; } = false;

        /// <summary>Forensic: IP/device from which the comment was added (bonus for full audit).</summary>
        [MaxLength(45)]
        [Column("source_ip")]
        [Display(Name = "Izvor (IP/uređaj)")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>DeepCopy – for rollback, history, and forensic review.</summary>
        public WorkOrderComment DeepCopy()
        {
            return new WorkOrderComment
            {
                Id = this.Id,
                WorkOrderId = this.WorkOrderId,
                WorkOrder = this.WorkOrder,
                Text = this.Text,
                UserId = this.UserId,
                User = this.User,
                CreatedAt = this.CreatedAt,
                Type = this.Type,
                RevisionNo = this.RevisionNo,
                DigitalSignature = this.DigitalSignature,
                IsCritical = this.IsCritical,
                SourceIp = this.SourceIp
            };
        }
    }
}
