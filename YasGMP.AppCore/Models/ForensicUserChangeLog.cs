using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>ForensicUserChangeLog</b> - Forensic log of user lifecycle actions (create, disable, role change, password reset, delete, force logout).
    /// Tracks actor, target, before/after values, notes, device/IP context, signature chain, and soft-delete state.
    /// </summary>
    [Table("forensic_user_change_log")]
    public partial class ForensicUserChangeLog
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the changed at.
        /// </summary>
        [Column("changed_at")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the changed by.
        /// </summary>
        [Column("changed_by")]
        public int? ChangedBy { get; set; }

        /// <summary>
        /// Gets or sets the changed by user.
        /// </summary>
        [ForeignKey(nameof(ChangedBy))]
        public User? ChangedByUser { get; set; }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        [Column("action")]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target user id.
        /// </summary>
        [Column("target_user_id")]
        public int? TargetUserId { get; set; }

        /// <summary>
        /// Gets or sets the target user.
        /// </summary>
        [ForeignKey(nameof(TargetUserId))]
        public User? TargetUser { get; set; }

        /// <summary>
        /// Gets or sets the old role.
        /// </summary>
        [Column("old_role")]
        [StringLength(50)]
        public string? OldRole { get; set; }

        /// <summary>
        /// Gets or sets the new role.
        /// </summary>
        [Column("new_role")]
        [StringLength(50)]
        public string? NewRole { get; set; }

        /// <summary>
        /// Gets or sets the old status.
        /// </summary>
        [Column("old_status")]
        public bool? OldStatus { get; set; }

        /// <summary>
        /// Gets or sets the new status.
        /// </summary>
        [Column("new_status")]
        public bool? NewStatus { get; set; }

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        [Column("note", TypeName = "text")]
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets the source ip.
        /// </summary>
        [Column("source_ip")]
        [StringLength(45)]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Gets or sets the digital signature.
        /// </summary>
        [Column("digital_signature")]
        [StringLength(255)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Gets or sets the change version.
        /// </summary>
        [Column("change_version")]
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Gets or sets the is deleted.
        /// </summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;
    }
}
