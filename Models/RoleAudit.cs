using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("role_audit")]
    public class RoleAudit
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("role_id")]
        public int? RoleId { get; set; }

        [Column("action")]
        [StringLength(40)]
        public string Action { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("source_ip")]
        [StringLength(45)]
        public string? SourceIp { get; set; }

        [Column("device_info")]
        [StringLength(255)]
        public string? DeviceInfo { get; set; }

        [Column("session_id")]
        [StringLength(100)]
        public string? SessionId { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("details")]
        public string? Details { get; set; }
    }
}
