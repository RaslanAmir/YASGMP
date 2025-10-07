using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents the Work Order Comment.
    /// </summary>
    [Table("work_order_comments")]
    public partial class WorkOrderComment
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
        /// Gets or sets the user id.
        /// </summary>
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        [Column("comment", TypeName = "text")]
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the revision no.
        /// </summary>
        [Column("revision_no")]
        public int RevisionNo { get; set; } = 1;

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
