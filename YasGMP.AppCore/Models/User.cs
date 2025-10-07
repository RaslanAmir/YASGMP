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

        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        [MaxLength(100), EmailAddress]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the phone.
        /// </summary>
        [MaxLength(40)]
        [Column("phone")]
        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the digital signature.
        /// </summary>
        [MaxLength(128)]
        [Column("digital_signature")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Device fingerprint captured at the time of the last persisted change.</summary>
        [MaxLength(255)]
        [Column("device_info")]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>Source IP recorded alongside the latest signature update.</summary>
        [MaxLength(45)]
        [Column("source_ip")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Session identifier associated with the last persisted change.</summary>
        [MaxLength(128)]
        [Column("session_id")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the department id.
        /// </summary>
        [Column("department_id")]
        public int? DepartmentId { get; set; }

        /// <summary>
        /// Gets or sets the department name.
        /// </summary>
        [MaxLength(80)]
        [Column("department_name")]
        public string DepartmentName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the is two factor enabled.
        /// </summary>
        [Column("is_two_factor_enabled")]
        public bool IsTwoFactorEnabled { get; set; } = false;

        // === SECURITY / ACCESS CONTROL ===

        /// <summary>
        /// Gets or sets the last login.
        /// </summary>
        [Column("last_login")]
        public DateTime? LastLogin { get; set; }

        /// <summary>
        /// Gets or sets the is locked.
        /// </summary>
        [Column("is_locked")]
        public bool IsLocked { get; set; } = false;

        /// <summary>
        /// Gets or sets the failed login attempts.
        /// </summary>
        [Column("failed_login_attempts")]
        public int FailedLoginAttempts { get; set; } = 0;

        /// <summary>
        /// Gets or sets the last failed login.
        /// </summary>
        [Column("last_failed_login")]
        public DateTime? LastFailedLogin { get; set; }

        /// <summary>
        /// Gets or sets the password reset required.
        /// </summary>
        [Column("password_reset_required")]
        public bool PasswordResetRequired { get; set; } = false;

        // === EXTERNAL IDENTITY / FEDERATION ===

        /// <summary>
        /// Gets or sets the external provider id.
        /// </summary>
        [StringLength(255)]
        [Column("external_provider_id")]
        public string ExternalProviderId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the external provider type.
        /// </summary>
        [StringLength(40)]
        [Column("external_provider_type")]
        public string ExternalProviderType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the federated unique id.
        /// </summary>
        [StringLength(255)]
        [Column("federated_unique_id")]
        public string FederatedUniqueId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the preferred culture.
        /// </summary>
        [StringLength(16)]
        [Column("preferred_culture")]
        public string PreferredCulture { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last change signature.
        /// </summary>
        [StringLength(256)]
        [Column("last_change_signature")]
        public string LastChangeSignature { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the global federated id.
        /// </summary>
        [StringLength(255)]
        [Column("global_federated_id")]
        public string GlobalFederatedId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the public key.
        /// </summary>
        [Column("public_key", TypeName = "LONGTEXT")]
        public string PublicKey { get; set; } = string.Empty;

        // === ACCOUNT & AUDIT INFO ===

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last modified.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("last_modified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last modified by id.
        /// </summary>
        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        /// <summary>
        /// Gets or sets the last modified by.
        /// </summary>
        [ForeignKey("LastModifiedById")]
        public virtual User? LastModifiedBy { get; set; }

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

        /// <summary>
        /// Gets or sets the deleted at.
        /// </summary>
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        // === COMPLIANCE, PRIVACY, ANOMALY ===

        /// <summary>
        /// Gets or sets the privacy consent version.
        /// </summary>
        [MaxLength(20)]
        [Column("privacy_consent_version")]
        public string PrivacyConsentVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the privacy consent date.
        /// </summary>
        [Column("privacy_consent_date")]
        public DateTime? PrivacyConsentDate { get; set; }

        /// <summary>
        /// Gets or sets the custom attributes.
        /// </summary>
        [Column("custom_attributes")]
        public string CustomAttributes { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the security anomaly score.
        /// </summary>
        [Column("security_anomaly_score")]
        public double? SecurityAnomalyScore { get; set; }

        // === FLAGS & SYSTEM ===

        /// <summary>
        /// Gets or sets the is system account.
        /// </summary>
        [Column("is_system_account")]
        public bool IsSystemAccount { get; set; } = false;

        /// <summary>
        /// Executes the is super admin operation.
        /// </summary>
        [NotMapped]
        public bool IsSuperAdmin => (Role?.ToLowerInvariant() == "superadmin");

        /// <summary>
        /// Executes the is admin operation.
        /// </summary>
        [NotMapped]
        public bool IsAdmin => !string.IsNullOrEmpty(Role) &&
                               (Role.ToLowerInvariant().Contains("admin") || Role.ToLowerInvariant().Contains("superadmin"));

        /// <summary>
        /// Gets or sets the notification channel.
        /// </summary>
        [MaxLength(24)]
        [Column("notification_channel")]
        public string NotificationChannel { get; set; } = string.Empty;

        // === RELATIONSHIPS / NAVIGATION PROPERTIES ===
        /// <summary>
        /// Gets or sets the roles.
        /// </summary>

        public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
        /// <summary>
        /// Gets or sets the permissions.
        /// </summary>
        public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
        /// <summary>
        /// Gets or sets the user permissions.
        /// </summary>
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
        /// <summary>
        /// Gets or sets the delegated permissions.
        /// </summary>
        public virtual ICollection<DelegatedPermission> DelegatedPermissions { get; set; } = new List<DelegatedPermission>();
        /// <summary>
        /// Gets or sets the audit logs.
        /// </summary>
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        /// <summary>
        /// Gets or sets the digital signatures.
        /// </summary>
        public virtual ICollection<DigitalSignature> DigitalSignatures { get; set; } = new List<DigitalSignature>();
        /// <summary>
        /// Gets or sets the session logs.
        /// </summary>
        public virtual ICollection<SessionLog> SessionLogs { get; set; } = new List<SessionLog>();
        /// <summary>
        /// Gets or sets the created work orders.
        /// </summary>
        public virtual ICollection<WorkOrder> CreatedWorkOrders { get; set; } = new List<WorkOrder>();
        /// <summary>
        /// Gets or sets the assigned work orders.
        /// </summary>
        public virtual ICollection<WorkOrder> AssignedWorkOrders { get; set; } = new List<WorkOrder>();
        /// <summary>
        /// Gets or sets the uploaded photos.
        /// </summary>
        [InverseProperty(nameof(Photo.Uploader))]
        public virtual ICollection<Photo> UploadedPhotos { get; set; } = new List<Photo>();
        /// <summary>
        /// Gets or sets the uploaded attachments.
        /// </summary>
        public virtual ICollection<Attachment> UploadedAttachments { get; set; } = new List<Attachment>();
        /// <summary>
        /// Gets or sets the admin activity logs.
        /// </summary>
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
