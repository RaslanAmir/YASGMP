using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("validation_audit")]
    public class ValidationAudit
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("digital_signature")]
        [StringLength(255)]
        public string? DigitalSignature { get; set; }

        [Column("changed_at")]
        public DateTime? ChangedAt { get; set; }

        [Column("source_ip")]
        [StringLength(255)]
        public string? SourceIp { get; set; }

        [Column("details")]
        [StringLength(255)]
        public string? Details { get; set; }

        [Column("device_info")]
        [StringLength(255)]
        public string? DeviceInfo { get; set; }

        [Column("validation_id")]
        public int? ValidationId { get; set; }

        [Column("action")]
        [StringLength(255)]
        public string? Action { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }
    }
}
