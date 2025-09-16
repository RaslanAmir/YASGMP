using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("permission_requests")]
    public class PermissionRequest
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("permission_id")]
        public int PermissionId { get; set; }

        [Column("requested_at")]
        public DateTime? RequestedAt { get; set; }

        [Column("reason")]
        [StringLength(255)]
        public string? Reason { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("reviewed_by")]
        public int? ReviewedBy { get; set; }

        [Column("reviewed_at")]
        public DateTime? ReviewedAt { get; set; }

        [Column("review_comment")]
        [StringLength(255)]
        public string? ReviewComment { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
