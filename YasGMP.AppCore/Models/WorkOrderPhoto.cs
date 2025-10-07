using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents the Work Order Photo.
    /// </summary>
    [Table("work_order_photos")]
    public partial class WorkOrderPhoto
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the work order id.
        /// </summary>
        [Column("work_order_id")]
        public int WorkOrderId { get; set; }

        /// <summary>
        /// Gets or sets the work order.
        /// </summary>
        [ForeignKey(nameof(WorkOrderId))]
        public WorkOrder? WorkOrder { get; set; }

        /// <summary>
        /// Gets or sets the document id.
        /// </summary>
        [Column("document_id")]
        public int DocumentId { get; set; }

        /// <summary>
        /// Gets or sets the document.
        /// </summary>
        [ForeignKey(nameof(DocumentId))]
        public Document? Document { get; set; }

        /// <summary>
        /// Gets or sets the tag.
        /// </summary>
        [Column("tag")]
        [MaxLength(16)]
        public string? Tag { get; set; }

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
