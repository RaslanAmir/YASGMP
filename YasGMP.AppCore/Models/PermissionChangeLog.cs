using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `permission_change_log` table.</summary>
    [Table("permission_change_log")]
    public class PermissionChangeLog
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the user id.</summary>
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>Gets or sets the changed by.</summary>
        [Column("changed_by")]
        public int ChangedBy { get; set; }

        /// <summary>Gets or sets the change type.</summary>
        [Column("change_type")]
        public string ChangeType { get; set; } = string.Empty;

        /// <summary>Gets or sets the role id.</summary>
        [Column("role_id")]
        public int? RoleId { get; set; }

        /// <summary>Gets or sets the permission id.</summary>
        [Column("permission_id")]
        public int? PermissionId { get; set; }

        /// <summary>Gets or sets the action.</summary>
        [Column("action")]
        public string Action { get; set; } = string.Empty;

        /// <summary>Gets or sets the reason.</summary>
        [Column("reason")]
        [StringLength(255)]
        public string? Reason { get; set; }

        /// <summary>Gets or sets the change time.</summary>
        [Column("change_time")]
        public DateTime? ChangeTime { get; set; }

        /// <summary>Gets or sets the expires at.</summary>
        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>Gets or sets the source ip.</summary>
        [Column("source_ip")]
        [StringLength(45)]
        public string? SourceIp { get; set; }

        /// <summary>Gets or sets the session id.</summary>
        [Column("session_id")]
        [StringLength(100)]
        public string? SessionId { get; set; }

        /// <summary>Gets or sets the details.</summary>
        [Column("details")]
        public string? Details { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        /// <summary>
        /// Gets or sets the changed by navigation.
        /// </summary>
        [ForeignKey(nameof(ChangedBy))]
        public virtual User? ChangedByNavigation { get; set; }

        /// <summary>
        /// Gets or sets the role.
        /// </summary>
        [ForeignKey(nameof(RoleId))]
        public virtual Role? Role { get; set; }

        /// <summary>
        /// Gets or sets the permission.
        /// </summary>
        [ForeignKey(nameof(PermissionId))]
        public virtual Permission? Permission { get; set; }
    }
}
