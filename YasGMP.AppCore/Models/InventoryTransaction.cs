using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>InventoryTransaction</b>  Ultra robust record of every inventory movement for a part.
    /// </summary>
    [Table("inventory_transactions")]
    public partial class InventoryTransaction
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the part id.
        /// </summary>
        [Required]
        [Display(Name = "Part")]
        [Column("part_id")]
        public int PartId { get; set; }

        /// <summary>
        /// Gets or sets the warehouse id.
        /// </summary>
        [Required]
        [Display(Name = "Warehouse")]
        [Column("warehouse_id")]
        public int WarehouseId { get; set; }

        /// <summary>
        /// Gets or sets the transaction type.
        /// </summary>
        [Required]
        [Display(Name = "Transaction Type")]
        [Column("transaction_type")]
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        [Display(Name = "Quantity")]
        [Column("quantity")]
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the transaction date.
        /// </summary>
        [Display(Name = "Transaction Date")]
        [Column("transaction_date")]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the performed by id.
        /// </summary>
        [Display(Name = "Performed By")]
        [Column("performed_by_id")]
        public int? PerformedById { get; set; }

        /// <summary>
        /// Gets or sets the related document.
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Document")]
        [Column("related_document")]
        public string? RelatedDocument { get; set; }

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        [Display(Name = "Note")]
        [Column("note")]
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets the part.
        /// </summary>
        [ForeignKey(nameof(PartId))]
        public virtual Part? Part { get; set; }

        /// <summary>
        /// Gets or sets the warehouse.
        /// </summary>
        [ForeignKey(nameof(WarehouseId))]
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>
        /// Gets or sets the performed by.
        /// </summary>
        [ForeignKey(nameof(PerformedById))]
        public virtual User? PerformedBy { get; set; }
    }
}

