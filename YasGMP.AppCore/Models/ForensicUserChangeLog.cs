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
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("changed_at")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        [Column("changed_by")]
        public int? ChangedBy { get; set; }

        [ForeignKey(nameof(ChangedBy))]
        public User? ChangedByUser { get; set; }

        [Column("action")]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        [Column("target_user_id")]
        public int? TargetUserId { get; set; }

        [ForeignKey(nameof(TargetUserId))]
        public User? TargetUser { get; set; }

        [Column("old_role")]
        [StringLength(50)]
        public string? OldRole { get; set; }

        [Column("new_role")]
        [StringLength(50)]
        public string? NewRole { get; set; }

        [Column("old_status")]
        public bool? OldStatus { get; set; }

        [Column("new_status")]
        public bool? NewStatus { get; set; }

        [Column("note", TypeName = "text")]
        public string? Note { get; set; }

        [Column("source_ip")]
        [StringLength(45)]
        public string? SourceIp { get; set; }

        [Column("digital_signature")]
        [StringLength(255)]
        public string? DigitalSignature { get; set; }

        [Column("change_version")]
        public int ChangeVersion { get; set; } = 1;

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;
    }
}

