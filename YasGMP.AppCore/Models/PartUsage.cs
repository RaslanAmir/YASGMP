using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>PartUsage</b> – Super ultra mega robust record for every spare part used in maintenance, calibration, repair, or GMP/CMMS intervention.
    /// <para>
    /// ✅ Tracks which parts were used, in what quantity, by whom, on what work order/machine/component.<br/>
    /// ✅ Supports batch/lot, serial, expiry, cost, supplier, warranty, attachments, and full audit.<br/>
    /// ✅ Regulatory-ready: forensics, traceability, stock deduction, and real-time inventory.
    /// </para>
    /// </summary>
    public class PartUsage
    {
        /// <summary>
        /// Unique part usage record ID (Primary Key).
        /// </summary>
        [Key]
        [Display(Name = "ID upotrebe dijela")]
        public int Id { get; set; }

        /// <summary>
        /// The work order where the part was used (FK).
        /// </summary>
        [Display(Name = "Radni nalog")]
        public int? WorkOrderId { get; set; }
        /// <summary>
        /// Gets or sets the work order.
        /// </summary>
        [ForeignKey(nameof(WorkOrderId))]
        public WorkOrder? WorkOrder { get; set; }

        /// <summary>
        /// The machine/component on which the part was used (FK).
        /// </summary>
        [Display(Name = "Stroj / Komponenta")]
        public int? MachineComponentId { get; set; }
        /// <summary>
        /// Gets or sets the machine component.
        /// </summary>
        [ForeignKey(nameof(MachineComponentId))]
        public MachineComponent? MachineComponent { get; set; }

        /// <summary>
        /// Part used (FK).
        /// </summary>
        [Display(Name = "Dio / Rezervni dio")]
        public int PartId { get; set; }
        /// <summary>
        /// Gets or sets the part.
        /// </summary>
        [ForeignKey(nameof(PartId))]
        public Part? Part { get; set; }

        /// <summary>
        /// Quantity used.
        /// </summary>
        [Display(Name = "Količina")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Serial number (if applicable, for traceability).
        /// </summary>
        [StringLength(64)]
        [Display(Name = "Serijski broj")]
        public string? SerialNumber { get; set; }

        /// <summary>
        /// Lot/batch number (if applicable).
        /// </summary>
        [StringLength(64)]
        [Display(Name = "Lot / Batch broj")]
        public string? LotNumber { get; set; }

        /// <summary>
        /// Expiry date of the used part (if perishable/regulatory).
        /// </summary>
        [Display(Name = "Datum isteka")]
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Cost per unit at the time of use.
        /// </summary>
        [Display(Name = "Jedinična cijena")]
        public decimal? UnitCost { get; set; }

        /// <summary>
        /// Total cost for this usage.
        /// </summary>
        [Display(Name = "Ukupna cijena")]
        public decimal? TotalCost { get; set; }

        /// <summary>
        /// Supplier of the part (FK).
        /// </summary>
        [Display(Name = "Dobavljač")]
        public int? SupplierId { get; set; }
        /// <summary>
        /// Gets or sets the supplier.
        /// </summary>
        [ForeignKey(nameof(SupplierId))]
        public Supplier? Supplier { get; set; }

        /// <summary>
        /// User/technician who used/issued the part (FK).
        /// </summary>
        [Display(Name = "Izdao korisnik")]
        public int? IssuedById { get; set; }
        /// <summary>
        /// Gets or sets the issued by.
        /// </summary>
        [ForeignKey(nameof(IssuedById))]
        public User? IssuedBy { get; set; }

        /// <summary>
        /// Date/time part was issued/used.
        /// </summary>
        [Display(Name = "Datum upotrebe")]
        public DateTime IssuedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Warranty info (if applicable).
        /// </summary>
        [StringLength(128)]
        [Display(Name = "Garancija")]
        public string? Warranty { get; set; }

        /// <summary>
        /// Attachments (photo, doc, regulatory proof).
        /// </summary>
        [Display(Name = "Prilozi")]
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();

        /// <summary>
        /// Full audit log for this part usage.
        /// </summary>
        [Display(Name = "Audit log")]
        public List<PartUsageAuditLog> AuditLogs { get; set; } = new List<PartUsageAuditLog>();

        /// <summary>
        /// Regulatory comments, escalation, notes.
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Napomene")]
        public string? Note { get; set; }
    }

    /// <summary>
    /// <b>PartUsageAuditLog</b> – Full forensic log for every part usage event/change.
    /// </summary>
    public class PartUsageAuditLog
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the part usage id.
        /// </summary>
        [Display(Name = "ID upotrebe dijela")]
        public int PartUsageId { get; set; }
        /// <summary>
        /// Gets or sets the part usage.
        /// </summary>
        [ForeignKey(nameof(PartUsageId))]
        public PartUsage? PartUsage { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        [Display(Name = "Vrijeme događaja")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        [Display(Name = "Akcija")]
        public string? Action { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        [Display(Name = "Korisnik")]
        public int? UserId { get; set; }
        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Opis")]
        public string? Description { get; set; }
    }
}
