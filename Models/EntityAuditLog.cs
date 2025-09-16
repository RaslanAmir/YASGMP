using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("entity_audit_log")]
    public class EntityAuditLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("timestamp")]
        public DateTime Timestamp { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("source_ip")]
        [StringLength(64)]
        public string? SourceIp { get; set; }

        [Column("device_info")]
        [StringLength(256)]
        public string? DeviceInfo { get; set; }

        [Column("entity")]
        [StringLength(64)]
        public string Entity { get; set; } = string.Empty;

        [Column("entity_id")]
        public int? EntityId { get; set; }

        [Column("action")]
        [StringLength(64)]
        public string Action { get; set; } = string.Empty;

        [Column("details")]
        public string? Details { get; set; }

        [Column("session_id")]
        [StringLength(128)]
        public string? SessionId { get; set; }

        [Column("status")]
        [StringLength(32)]
        public string? Status { get; set; }

        [Column("digital_signature")]
        [StringLength(256)]
        public string? DigitalSignature { get; set; }

        [Column("signature_hash")]
        [StringLength(256)]
        public string? SignatureHash { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
