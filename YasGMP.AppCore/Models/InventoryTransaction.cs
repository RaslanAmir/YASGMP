using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>InventoryTransaction</b> – Ultra robust record of every inventory movement for a part.
    /// </summary>
    [Table("inventory_transactions")]
    public partial class InventoryTransaction
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Part")]
        [Column("part_id")]
        public int PartId { get; set; }

        [Required]
        [Display(Name = "Warehouse")]
        [Column("warehouse_id")]
        public int WarehouseId { get; set; }

        [Required]
        [Display(Name = "Transaction Type")]
        [Column("transaction_type")]
        public string TransactionType { get; set; } = string.Empty;

        [Display(Name = "Quantity")]
        [Column("quantity")]
        public int Quantity { get; set; }

        [Display(Name = "Transaction Date")]
        [Column("transaction_date")]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Performed By")]
        [Column("performed_by_id")]
        public int? PerformedById { get; set; }

        [StringLength(255)]
        [Display(Name = "Document")]
        [Column("related_document")]
        public string? RelatedDocument { get; set; }

        [Display(Name = "Note")]
        [Column("note")]
        public string? Note { get; set; }

        [ForeignKey(nameof(PartId))]
        public virtual Part? Part { get; set; }

        [ForeignKey(nameof(WarehouseId))]
        public virtual Warehouse? Warehouse { get; set; }

        [ForeignKey(nameof(PerformedById))]
        public virtual User? PerformedBy { get; set; }
    }
}

