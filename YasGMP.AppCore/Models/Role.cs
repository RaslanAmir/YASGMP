using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Role</b> – Ultra-robust, auditable user role for GMP/CSV/21 CFR/Annex 11/ITIL-compliant RBAC.
    /// <para>
    /// • Fully supports multi-level RBAC, workflow, escalation, legal/audit traceability, SoD (Segregation of Duties), delegation, and incident forensics.<br/>
    /// • Used for permission mapping, user-role assignments, workflow routing, and regulatory reporting.<br/>
    /// • Recommended: Map to <see cref="UserRoleAssignment"/> (assignment), <see cref="Permission"/> (access), and <see cref="RolePermission"/> (role-right mapping).
    /// </para>
    /// <remarks>
    /// - Extend for multi-org, jurisdiction, contract/vendor, and special regulatory modules.<br/>
    /// - Safe for legal trace, incident defense, compliance, and audit dashboarding.<br/>
    /// - All changes should be tracked via the audit log.
    /// </remarks>
    /// </summary>
    [Table("roles")]
    public class Role
    {
        // ===================== KEYS & CORE FIELDS =====================

        /// <summary>Unique role ID (primary key).</summary>
        [Key]
        [Column("id")]
        [Display(Name = "ID")]
        public int Id { get; set; }

        /// <summary>
        /// System/machine name of the role (unique, e.g. "admin", "auditor", "superadmin", "tehnicar", ...).
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("name")]
        [Display(Name = "Naziv")]
        public string Name
        {
            get => _name;
            set => _name = (value ?? string.Empty).Trim();
        }
        private string _name = string.Empty;

        /// <summary>Human-readable description (for UI/help/audit/trace).</summary>
        [MaxLength(255)]
        [Column("description")]
        [Display(Name = "Opis")]
        public string? Description
        {
            get => _description;
            set => _description = value?.Trim();
        }
        private string? _description;

        /// <summary>Optional: organizational unit, customer, or jurisdiction (multi-tenant/extensibility).</summary>
        [MaxLength(80)]
        [Column("org_unit")]
        [Display(Name = "Organizacijska jedinica")]
        public string? OrgUnit { get; set; }

        /// <summary>Regulatory tags (CSV/GMP/21CFR11) for dashboards, inspection, and reporting.</summary>
        [MaxLength(120)]
        [Column("compliance_tags")]
        [Display(Name = "Regulatorni tagovi")]
        public string? ComplianceTags { get; set; }

        // ===================== NAVIGATION PROPERTIES =====================

        /// <summary>Permissions assigned to this role (many-to-many via role_permissions).</summary>
        [Display(Name = "Dozvole")]
        public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();

        /// <summary>Users with this role (many-to-many via user_roles).</summary>
        [Display(Name = "Korisnici")]
        public virtual ICollection<User> Users { get; set; } = new List<User>();

        /// <summary>Role-permission mapping entries (for audit/trace/delegation).</summary>
        [Display(Name = "Role-Dozvole Mapping")]
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        // ===================== AUDIT / LIFECYCLE =====================

        /// <summary>Date/time this role was created (UTC).</summary>
        [Column("created_at")]
        [Display(Name = "Kreirano")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>User who created this role.</summary>
        [Column("created_by_id")]
        [Display(Name = "Kreirao")]
        public int? CreatedById { get; set; }

        /// <summary>Navigation to the user who created this role.</summary>
        [ForeignKey(nameof(CreatedById))]
        public virtual User? CreatedBy { get; set; }

        /// <summary>Date/time this role was last updated (UTC).</summary>
        [Column("updated_at")]
        [Display(Name = "Zadnja izmjena")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>User who last modified this role (for audit chain).</summary>
        [Column("last_modified_by_id")]
        [Display(Name = "Zadnji izmijenio")]
        public int? LastModifiedById { get; set; }

        /// <summary>Navigation to the user who last modified this role.</summary>
        [ForeignKey(nameof(LastModifiedById))]
        public virtual User? LastModifiedBy { get; set; }

        /// <summary>Soft delete flag (GDPR/retention support).</summary>
        [Column("is_deleted")]
        [Display(Name = "Obrisano")]
        public bool IsDeleted { get; set; } = false;

        /// <summary>Freeform audit notes (incidents, CAPA, risk, escalation, etc.).</summary>
        [MaxLength(512)]
        [Column("notes")]
        [Display(Name = "Napomene")]
        public string? Notes
        {
            get => _notes;
            set => _notes = value?.Trim();
        }
        private string? _notes;

        /// <summary>Row version for optimistic concurrency/audit.</summary>
        [Column("version")]
        [Display(Name = "Verzija")]
        public int Version { get; set; } = 1;

        // ===================== UI/VM COMPATIBILITY HELPERS =====================

        /// <summary>
        /// XAML compatibility: the Admin page binds to <c>Code</c>. We alias it to DB-backed <see cref="Name"/>.
        /// </summary>
        [NotMapped]
        public string Code
        {
            get => Name;
            set => Name = value ?? string.Empty;
        }

        /// <summary>
        /// Convenience: a display label that prefers <see cref="Description"/> but falls back to <see cref="Name"/>.
        /// Useful for combo/list UIs.
        /// </summary>
        [NotMapped]
        public string UiName => string.IsNullOrWhiteSpace(Description) ? Name : Description!;

        /// <summary>Returns system name and description for display/log/audit.</summary>
        public override string ToString() => $"{Name} ({Description})";
    }
}
