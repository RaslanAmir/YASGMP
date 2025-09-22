using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    /// <summary>
    /// Database entity that records each API call for audit, linking to the key/user, action, and request context.
    /// </summary>
    [Table("api_audit_log")]
    public class ApiAuditLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("api_key_id")]
        public int? ApiKeyId { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("action")]
        [StringLength(255)]
        public string? Action { get; set; }

        [Column("timestamp")]
        public DateTime? Timestamp { get; set; }

        [Column("ip_address")]
        [StringLength(45)]
        public string? IpAddress { get; set; }

        [Column("request_details")]
        public string? RequestDetails { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("details")]
        public string? Details { get; set; }
    }
}
