// ==============================================================================
//  File: Models/User.cs
//  Project: YasGMP
// ------------------------------------------------------------------------------
//  Ultra-robust, forensic-grade User entity for GMP, CSV, 21 CFR Part 11,
//  GDPR, SSO, and banking-grade compliance.
//  - Keeps your mapping; adds NotMapped compatibility fields RoleIds/PermissionIds
//  - Provides alias property `UserName` that maps to `Username`
//  - Initializes non-nullable strings to string.Empty
//  - Full XML docs for IntelliSense
// ==============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>User</b> – Ultra-robust, forensic user entity for GMP, CSV, 21 CFR Part 11, GDPR, SSO, ITIL, and banking-grade compliance.
    /// </summary>
    [Table("users")]
    public partial class User
    {
        // === PRIMARY INFO ===

        /// <summary>Unique user ID (primary key).</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Username for login (unique, not case-sensitive).</summary>
        [Required, StringLength(100)]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>Password hash (PBKDF2 preferred; legacy SHA256 supported during migration).</summary>
        [Required, StringLength(128)]
        [Column("password")]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>Full legal name of the user.</summary>
        [Required, MaxLength(100)]
        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Role name (tehničar, šef, auditor, admin, superadmin, …).
        /// <para>Legacy convenience; full RBAC is via <see cref="Roles"/>.</para>
        /// </summary>
        [Required, MaxLength(30)]
        [Column("role")]
        public string Role { get; set; } = string.Empty;

        /// <summary>Is user account active?</summary>
        [Column("active")]
        public bool Active { get; set; } = true;

        // === CONTACT / IDENTITY ===

        [MaxLength(100), EmailAddress]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(40)]
        [Column("phone")]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(128)]
        [Column("digital_signature")]
        public string DigitalSignature { get; set; } = string.Empty;

        [Column("department_id")]
        public int? DepartmentId { get; set; }

        [MaxLength(80)]
        [Column("department_name")]
        public string DepartmentName { get; set; } = string.Empty;

        [Column("is_two_factor_enabled")]
        public bool IsTwoFactorEnabled { get; set; } = false;

        // === SECURITY / ACCESS CONTROL ===

        [Column("last_login")]
        public DateTime? LastLogin { get; set; }

        [Column("is_locked")]
        public bool IsLocked { get; set; } = false;

        [Column("failed_login_attempts")]
        public int FailedLoginAttempts { get; set; } = 0;

        [Column("last_failed_login")]
        public DateTime? LastFailedLogin { get; set; }

        [Column("password_reset_required")]
        public bool PasswordResetRequired { get; set; } = false;

        // === EXTERNAL IDENTITY / FEDERATION ===

        [StringLength(255)]
        [Column("external_provider_id")]
        public string ExternalProviderId { get; set; } = string.Empty;

        [StringLength(40)]
        [Column("external_provider_type")]
        public string ExternalProviderType { get; set; } = string.Empty;

        [StringLength(255)]
        [Column("federated_unique_id")]
        public string FederatedUniqueId { get; set; } = string.Empty;

        [StringLength(16)]
        [Column("preferred_culture")]
        public string PreferredCulture { get; set; } = string.Empty;

        [StringLength(256)]
        [Column("last_change_signature")]
        public string LastChangeSignature { get; set; } = string.Empty;

        [StringLength(255)]
        [Column("global_federated_id")]
        public string GlobalFederatedId { get; set; } = string.Empty;

        [Column("public_key", TypeName = "LONGTEXT")]
        public string PublicKey { get; set; } = string.Empty;

        // === ACCOUNT & AUDIT INFO ===

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("last_modified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        [ForeignKey("LastModifiedById")]
        public virtual User? LastModifiedBy { get; set; }

        [Column("change_version")]
        public int ChangeVersion { get; set; } = 1;

        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        // === COMPLIANCE, PRIVACY, ANOMALY ===

        [MaxLength(20)]
        [Column("privacy_consent_version")]
        public string PrivacyConsentVersion { get; set; } = string.Empty;

        [Column("privacy_consent_date")]
        public DateTime? PrivacyConsentDate { get; set; }

        [Column("custom_attributes")]
        public string CustomAttributes { get; set; } = string.Empty;

        [Column("security_anomaly_score")]
        public double? SecurityAnomalyScore { get; set; }

        // === FLAGS & SYSTEM ===

        [Column("is_system_account")]
        public bool IsSystemAccount { get; set; } = false;

        [NotMapped]
        public bool IsSuperAdmin => (Role?.ToLowerInvariant() == "superadmin");

        [NotMapped]
        public bool IsAdmin => !string.IsNullOrEmpty(Role) &&
                               (Role.ToLowerInvariant().Contains("admin") || Role.ToLowerInvariant().Contains("superadmin"));

        [MaxLength(24)]
        [Column("notification_channel")]
        public string NotificationChannel { get; set; } = string.Empty;

        // === RELATIONSHIPS / NAVIGATION PROPERTIES ===

        public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
        public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
        public virtual ICollection<DelegatedPermission> DelegatedPermissions { get; set; } = new List<DelegatedPermission>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<DigitalSignature> DigitalSignatures { get; set; } = new List<DigitalSignature>();
        public virtual ICollection<SessionLog> SessionLogs { get; set; } = new List<SessionLog>();
        public virtual ICollection<WorkOrder> CreatedWorkOrders { get; set; } = new List<WorkOrder>();
        public virtual ICollection<WorkOrder> AssignedWorkOrders { get; set; } = new List<WorkOrder>();
        [InverseProperty(nameof(Photo.UploadedBy))]
        public virtual ICollection<Photo> UploadedPhotos { get; set; } = new List<Photo>();
        public virtual ICollection<Attachment> UploadedAttachments { get; set; } = new List<Attachment>();
        public virtual ICollection<AdminActivityLog> AdminActivityLogs { get; set; } = new List<AdminActivityLog>();

        // === UTILITY / COMPATIBILITY ===

        /// <summary>Display name in the format "<c>Full Name</c> [<c>Username</c>]".</summary>
        [NotMapped]
        public string DisplayName => $"{FullName} [{Username}]";

        /// <summary>Compatibility alias for ViewModels expecting <c>UserName</c>.</summary>
        [NotMapped]
        public string UserName
        {
            get => Username;
            set => Username = value ?? string.Empty;
        }

        /// <summary>
        /// Compatibility arrays used by ViewModels to show linked RBAC data without loading
        /// navigation properties. Populated by DB extension methods at read time.
        /// </summary>
        [NotMapped] public int[] RoleIds { get; set; } = Array.Empty<int>();
        [NotMapped] public int[] PermissionIds { get; set; } = Array.Empty<int>();

        /// <summary>Concise string for display and audit.</summary>
        public override string ToString()
            => $"{DisplayName} ({Role}, Active={Active}, Locked={IsLocked}, Deleted={IsDeleted})";
    }
}
