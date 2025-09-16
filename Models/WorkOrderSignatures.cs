using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("work_order_signatures")]
    public class WorkOrderSignatures
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("work_order_id")]
        public int WorkOrderId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("signature_hash")]
        [StringLength(255)]
        public string? SignatureHash { get; set; }

        [Column("signed_at")]
        public DateTime? SignedAt { get; set; }

        [Column("pin_used")]
        [StringLength(20)]
        public string? PinUsed { get; set; }

        [Column("signature_type")]
        public string? SignatureType { get; set; }

        [Column("note")]
        public string? Note { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
