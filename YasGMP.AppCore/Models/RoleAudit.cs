using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `role_audit` table.</summary>
    [Table("role_audit")]
    public class RoleAudit
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the role id.</summary>
        [Column("role_id")]
        public int? RoleId { get; set; }

        /// <summary>Gets or sets the action.</summary>
        [Column("action")]
        [StringLength(40)]
        public string Action { get; set; } = string.Empty;

        /// <summary>Gets or sets the description.</summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the source ip.</summary>
        [Column("source_ip")]
        [StringLength(45)]
        public string? SourceIp { get; set; }

        /// <summary>Gets or sets the device info.</summary>
        [Column("device_info")]
        [StringLength(255)]
        public string? DeviceInfo { get; set; }

        /// <summary>Gets or sets the session id.</summary>
        [Column("session_id")]
        [StringLength(100)]
        public string? SessionId { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Gets or sets the details.</summary>
        [Column("details")]
        public string? Details { get; set; }

        [ForeignKey(nameof(RoleId))]
        public virtual Role? Role { get; set; }
    }
}

