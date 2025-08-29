using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>InventoryTransaction</b> – Ultra robust record of every inventory movement for a part.
    /// <para>
    /// ✅ Fully audit-ready: links part, warehouse, user, transaction type, docs, notes.<br/>
    /// ✅ Supports GMP/CSV traceability, regulatory audit, attachments, and forensic analysis.
    /// </para>
    /// </summary>
    public class InventoryTransaction
    {
        /// <summary>
        /// Unique transaction identifier (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Related part ID (FK).
        /// </summary>
        [Required]
        [Display(Name = "Dio")]
        public int PartId { get; set; }

        /// <summary>
        /// Related warehouse ID (FK).
        /// </summary>
        [Required]
        [Display(Name = "Skladište")]
        public int WarehouseId { get; set; }

        /// <summary>
        /// Transaction type (in, out, transfer, adjust, damage, obsolete).
        /// </summary>
        [Required]
        [Display(Name = "Tip transakcije")]
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>
        /// Quantity involved in the transaction.
        /// </summary>
        [Display(Name = "Količina")]
        public int Quantity { get; set; }

        /// <summary>
        /// Transaction timestamp.
        /// </summary>
        [Display(Name = "Datum transakcije")]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        /// <summary>
        /// ID of the user who performed the transaction (FK).
        /// </summary>
        [Display(Name = "Izvršio korisnik")]
        public int? PerformedById { get; set; }

        /// <summary>
        /// Related document number or file (invoice, WO, etc).
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Dokument")]
        public string? RelatedDocument { get; set; }

        /// <summary>
        /// Optional transaction note or description.
        /// </summary>
        [Display(Name = "Napomena")]
        public string? Note { get; set; }

        // === Navigation Properties ===

        /// <summary>
        /// Related part entity.
        /// </summary>
        public virtual SparePart? Part { get; set; }

        /// <summary>
        /// Related warehouse entity.
        /// </summary>
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>
        /// User who performed the transaction.
        /// </summary>
        public virtual User? PerformedBy { get; set; }

        // === BONUS: Attachments, audit trail, advanced links can be added here

        /// <summary>
        /// Creates a new InventoryTransaction instance.
        /// </summary>
        public InventoryTransaction()
        {
        }
    }
}
