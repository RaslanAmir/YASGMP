using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `work_order_checklist_item` table.</summary>
    [Table("work_order_checklist_item")]
    public class WorkOrderChecklistItem
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the work order id.</summary>
        [Column("work_order_id")]
        public int WorkOrderId { get; set; }

        /// <summary>Gets or sets the item id.</summary>
        [Column("item_id")]
        public int ItemId { get; set; }

        /// <summary>Gets or sets the result.</summary>
        [Column("result")]
        [StringLength(255)]
        public string? Result { get; set; }

        /// <summary>Gets or sets the ok.</summary>
        [Column("ok")]
        public bool? Ok { get; set; }

        /// <summary>Gets or sets the note.</summary>
        [Column("note")]
        public string? Note { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the work order.
        /// </summary>
        [ForeignKey(nameof(WorkOrderId))]
        public virtual WorkOrder? WorkOrder { get; set; }

        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        [ForeignKey(nameof(ItemId))]
        public virtual ChecklistItem? Item { get; set; }
    }
}
