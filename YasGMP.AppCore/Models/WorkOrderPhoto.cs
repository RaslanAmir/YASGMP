using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    [Table("work_order_photos")]
    public partial class WorkOrderPhoto
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("work_order_id")]
        public int WorkOrderId { get; set; }

        [ForeignKey(nameof(WorkOrderId))]
        public WorkOrder? WorkOrder { get; set; }

        [Column("document_id")]
        public int DocumentId { get; set; }

        [ForeignKey(nameof(DocumentId))]
        public Document? Document { get; set; }

        [Column("tag")]
        [MaxLength(16)]
        public string? Tag { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}

