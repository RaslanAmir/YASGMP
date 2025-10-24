using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `entity_audit_log` table.</summary>
    [Table("entity_audit_log")]
    public class EntityAuditLog
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the timestamp.</summary>
        [Column("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Gets or sets the user id.</summary>
        [Column("user_id")]
        public int? UserId { get; set; }

        /// <summary>Gets or sets the source ip.</summary>
        [Column("source_ip")]
        [StringLength(64)]
        public string? SourceIp { get; set; }

        /// <summary>Gets or sets the device info.</summary>
        [Column("device_info")]
        [StringLength(256)]
        public string? DeviceInfo { get; set; }

        /// <summary>Gets or sets the entity.</summary>
        [Column("entity")]
        [StringLength(64)]
        public string Entity { get; set; } = string.Empty;

        /// <summary>Gets or sets the entity id.</summary>
        [Column("entity_id")]
        public int? EntityId { get; set; }

        /// <summary>Gets or sets the action.</summary>
        [Column("action")]
        [StringLength(64)]
        public string Action { get; set; } = string.Empty;

        /// <summary>Gets or sets the details.</summary>
        [Column("details")]
        public string? Details { get; set; }

        /// <summary>Gets or sets the session id.</summary>
        [Column("session_id")]
        [StringLength(128)]
        public string? SessionId { get; set; }

        /// <summary>Gets or sets the status.</summary>
        [Column("status")]
        [StringLength(32)]
        public string? Status { get; set; }

        /// <summary>Gets or sets the digital signature.</summary>
        [Column("digital_signature")]
        [StringLength(256)]
        public string? DigitalSignature { get; set; }

        /// <summary>Gets or sets the signature hash.</summary>
        [Column("signature_hash")]
        [StringLength(256)]
        public string? SignatureHash { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }
    }
}

