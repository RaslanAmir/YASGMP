using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("user_esignatures")]
    public class UserEsignature
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("signed_at")]
        public DateTime? SignedAt { get; set; }

        [Column("action")]
        [StringLength(100)]
        public string? Action { get; set; }

        [Column("table_name")]
        [StringLength(100)]
        public string? TableName { get; set; }

        [Column("record_id")]
        public int? RecordId { get; set; }

        [Column("signature_hash")]
        [StringLength(255)]
        public string? SignatureHash { get; set; }

        [Column("method")]
        public string? Method { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("ip_address")]
        [StringLength(45)]
        public string? IpAddress { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
