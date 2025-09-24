using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>DelegatedPermission</b> – Forensic, GMP/21 CFR Part 11 compliant record describing a permission
    /// delegated from one user to another (proxy, escalation, coverage, incident handling, RBAC+).
    /// <para>
    /// • Designed for MAUI + MVVM + MySQL and Entity Framework Core.<br/>
    /// • Captures: delegator/delegatee, window (start/end), approver, reason, device/IP/session,
    ///   digital signature, active/revoked state, and change version.<br/>
    /// • Intended to be the single authoritative EF entity (avoid duplicate class definitions).
    /// </para>
    /// <remarks>
    /// ✅ To prevent compiler errors <c>CS0101</c>, <c>CS0229</c>, and <c>CS0579</c>, ensure there is only
    /// one EF entity class named <see cref="DelegatedPermission"/> in the <c>YasGMP.Models</c> namespace.
    /// If you keep any additional partials, they must not redeclare properties nor repeat the <see cref="TableAttribute"/>.
    /// </remarks>
    /// </summary>
    [Table("delegated_permissions")]
    public partial class DelegatedPermission
    {
        /// <summary>
        /// Unique ID for the delegation (primary key, auto-increment).
        /// </summary>
        [Key]
        [Column("id")]
        [Display(Name = "Delegation ID")]
        public int Id { get; set; }

        /// <summary>
        /// Permission ID being delegated (FK to <see cref="Permission"/>).
        /// </summary>
        [Required]
        [Column("permission_id")]
        [Display(Name = "Permission")]
        public int PermissionId { get; set; }

        /// <summary>
        /// Navigation property for the delegated <see cref="Permission"/>.
        /// Nullable to allow detached DTO usage and lazy-loading scenarios.
        /// </summary>
        [ForeignKey(nameof(PermissionId))]
        public virtual Permission? Permission { get; set; }

        /// <summary>
        /// User delegating the permission (FK to <see cref="User"/>).
        /// </summary>
        [Required]
        [Column("from_user_id")]
        [Display(Name = "Delegator")]
        public int FromUserId { get; set; }

        /// <summary>
        /// Navigation property for the user who delegates.
        /// Nullable to keep constructors lightweight and support projection-only queries.
        /// </summary>
        [ForeignKey(nameof(FromUserId))]
        public virtual User? FromUser { get; set; }

        /// <summary>
        /// User receiving the delegation (FK to <see cref="User"/>).
        /// </summary>
        [Required]
        [Column("to_user_id")]
        [Display(Name = "Delegatee")]
        public int ToUserId { get; set; }

        /// <summary>
        /// Navigation property for the user receiving the delegation.
        /// Nullable to support detached contexts and minimal selects.
        /// </summary>
        [ForeignKey(nameof(ToUserId))]
        public virtual User? ToUser { get; set; }

        /// <summary>
        /// UTC timestamp when the delegation starts (inclusive).
        /// Defaults to <see cref="DateTime.UtcNow"/>.
        /// </summary>
        [Column("start_at")]
        [Display(Name = "Start Time")]
        public DateTime StartAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when the delegation ends/expires (nullable).
        /// </summary>
        [Column("end_at")]
        [Display(Name = "End Time")]
        public DateTime? EndAt { get; set; }

        /// <summary>
        /// Reason or context (GMP trace, escalation, absence/coverage, incident reference).
        /// </summary>
        [MaxLength(255)]
        [Column("reason")]
        [Display(Name = "Reason")]
        public string? Reason { get; set; }

        /// <summary>
        /// Indicates the delegation is enabled/active (evaluated with <see cref="IsRevoked"/> and <see cref="EndAt"/>).
        /// </summary>
        [Column("is_active")]
        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Indicates the delegation has been explicitly revoked/terminated.
        /// </summary>
        [Column("is_revoked")]
        [Display(Name = "Revoked")]
        public bool IsRevoked { get; set; } = false;

        /// <summary>
        /// User who approved or revoked the delegation (FK).
        /// </summary>
        [Column("approved_by")]
        [Display(Name = "Approved/Revoked By")]
        public int? ApprovedById { get; set; }

        /// <summary>
        /// Navigation to the approver/revoker user.
        /// </summary>
        [ForeignKey(nameof(ApprovedById))]
        public virtual User? ApprovedBy { get; set; }

        /// <summary>
        /// Audit note/comment (GMP incident, escalation, forensics, SOD).
        /// </summary>
        [MaxLength(512)]
        [Column("note")]
        [Display(Name = "Note")]
        public string? Note { get; set; }

        /// <summary>
        /// Event-sourcing version for strict audit/rollback semantics.
        /// </summary>
        [Column("change_version")]
        [Display(Name = "Change Version")]
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Digital signature/hash for forensic and legal audit integrity (21 CFR Part 11).
        /// </summary>
        [MaxLength(128)]
        [Column("digital_signature")]
        [Display(Name = "Digital Signature")]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Session identifier for traceability (incident/forensics/audit chain).
        /// </summary>
        [MaxLength(64)]
        [Column("session_id")]
        [Display(Name = "Session ID")]
        public string? SessionId { get; set; }

        /// <summary>
        /// Source IP or device identifier for forensic trace.
        /// </summary>
        [MaxLength(64)]
        [Column("source_ip")]
        [Display(Name = "Source IP/Device")]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Returns true if the delegation is active, not revoked, and not expired by <see cref="EndAt"/>.
        /// </summary>
        [NotMapped]
        public bool IsCurrentlyActive => IsActive && !IsRevoked && (EndAt is null || EndAt > DateTime.UtcNow);
    }
}
