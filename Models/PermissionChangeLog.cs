using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("permission_change_log")]
    public class PermissionChangeLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("changed_by")]
        public int ChangedBy { get; set; }

        [Column("change_type")]
        public string ChangeType { get; set; } = string.Empty;

        [Column("role_id")]
        public int? RoleId { get; set; }

        [Column("permission_id")]
        public int? PermissionId { get; set; }

        [Column("action")]
        public string Action { get; set; } = string.Empty;

        [Column("reason")]
        [StringLength(255)]
        public string? Reason { get; set; }

        [Column("change_time")]
        public DateTime? ChangeTime { get; set; }

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [Column("source_ip")]
        [StringLength(45)]
        public string? SourceIp { get; set; }

        [Column("session_id")]
        [StringLength(100)]
        public string? SessionId { get; set; }

        [Column("details")]
        public string? Details { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
