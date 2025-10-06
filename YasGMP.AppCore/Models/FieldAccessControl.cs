using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Field-level user permission model for absolute regulatory and enterprise control.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description>Per-user, per-table, per-field access rules (read, write, approve, export, view history, digital sign, rollback, etc.)</description></item>
    ///   <item><description>Tracks who assigned/revoked, when, IP/device, justification, expiry, audit chain, soft delete, future extensibility.</description></item>
    ///   <item><description>21 CFR Part 11 &amp; GMP bulletproof for AI/automation, fine-grained security, and full regulatory/forensic defense.</description></item>
    /// </list>
    /// </remarks>
    public class FieldAccessControl
    {
        /// <summary>
        /// Unique access control record identifier.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Identifier of the user to whom rights are assigned.
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Reference to the <see cref="User"/> entity.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        /// <summary>
        /// Name of the database table that this rule applies to.
        /// </summary>
        [Required, StringLength(100)]
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Name of the field or column within the table.
        /// </summary>
        [Required, StringLength(100)]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Whether the user can read this field.
        /// </summary>
        public bool CanRead { get; set; } = true;

        /// <summary>
        /// Whether the user can write or modify this field.
        /// </summary>
        public bool CanWrite { get; set; } = false;

        /// <summary>
        /// Whether the user can approve workflow actions on this field.
        /// </summary>
        public bool CanApprove { get; set; } = false;

        /// <summary>
        /// Whether the user can export this field’s data.
        /// </summary>
        public bool CanExport { get; set; } = false;

        /// <summary>
        /// Whether the user can view this field’s audit history.
        /// </summary>
        public bool CanViewHistory { get; set; } = false;

        /// <summary>
        /// Whether the user can digitally sign this field’s value.
        /// </summary>
        public bool CanSign { get; set; } = false;

        /// <summary>
        /// Whether the user can rollback changes to this field.
        /// </summary>
        public bool CanRollback { get; set; } = false;

        /// <summary>
        /// Expiration date of this permission (null = never expires).
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Identifier of the user who granted or revoked this permission.
        /// </summary>
        public int? AssignedById { get; set; }

        /// <summary>
        /// Reference to the <see cref="User"/> who performed the assignment.
        /// </summary>
        [ForeignKey(nameof(AssignedById))]
        public User? AssignedBy { get; set; }

        /// <summary>
        /// Timestamp of the last modification to this permission.
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Justification for granting or revoking this permission.
        /// </summary>
        [StringLength(512)]
        public string Justification { get; set; } = string.Empty;

        /// <summary>
        /// IP address or device identifier from which the change was made.
        /// </summary>
        [StringLength(128)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Change version for event sourcing and rollback support.
        /// </summary>
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Indicates whether this record has been soft-deleted or archived.
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
}

