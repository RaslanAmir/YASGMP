using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YasGMP.Models.Enums;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Permission</b> – Defines an atomic system right/ability for ultra-robust, forensic RBAC.
    /// <para>
    /// • Fully GMP/CSV/21 CFR Part 11/Annex 11 ready.<br/>
    /// • Extensible, auditable, regulatory-tagged, and hierarchy-enabled.<br/>
    /// • Supports compliance dashboards, risk analysis, audit, change tracking, and inspector reporting.
    /// </para>
    /// <remarks>
    /// - Maps to roles via <see cref="RolePermission"/>.<br/>
    /// - Supports parent/child hierarchy and groupings.<br/>
    /// - Add new permissions here and sync your UI, logic, and reporting modules.
    /// </remarks>
    /// </summary>
    [Table("permissions")]
    public class Permission
    {
        /// <summary>
        /// Unique ID of the permission (Primary Key).
        /// </summary>
        [Key]
        [Column("id")]
        [Display(Name = "ID")]
        public int Id { get; set; }

        /// <summary>
        /// PermissionType (enum, for logic, mapping, compliance checks).
        /// </summary>
        [Column("permission_type")]
        [Display(Name = "Tip dozvole")]
        public YasGMP.Models.Enums.PermissionType? PermissionType { get; set; }

        /// <summary>
        /// Unique code for this permission (e.g. "CanEditUser", "WorkOrder_Approve").
        /// </summary>
        [Required, MaxLength(80)]
        [Column("code")]
        [Display(Name = "Kod")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable name/label (displayed in UI, audit logs, reporting).
        /// </summary>
        [Required, MaxLength(120)]
        [Column("name")]
        [Display(Name = "Naziv")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description (audit, help, training, reporting).
        /// </summary>
        [MaxLength(500)]
        [Column("description")]
        [Display(Name = "Opis")]
        public string? Description { get; set; }

        /// <summary>
        /// Permission group/category (for UI, dashboards, regulatory grouping).
        /// </summary>
        [MaxLength(80)]
        [Column("group")]
        [Display(Name = "Grupa")]
        public string? Group { get; set; }

        /// <summary>
        /// Parent permission (for hierarchy/inheritance).
        /// </summary>
        [Column("parent_id")]
        [Display(Name = "Nadređena dozvola")]
        public int? ParentId { get; set; }

        /// <summary>
        /// Navigation to the parent permission.
        /// </summary>
        [ForeignKey("ParentId")]
        public virtual Permission? Parent { get; set; }

        /// <summary>
        /// List of child permissions (for UI tree, inheritance, export).
        /// </summary>
        public virtual ICollection<Permission> Children { get; set; } = new List<Permission>();

        /// <summary>
        /// Regulatory tags (e.g., "GMP;CSV;Annex 11;FDA", for dashboards/inspections).
        /// </summary>
        [MaxLength(120)]
        [Column("compliance_tags")]
        [Display(Name = "Regulatorne oznake")]
        public string? ComplianceTags { get; set; }

        /// <summary>
        /// True if this is a critical/system permission (risk/incident/audit trigger).
        /// </summary>
        [Column("critical")]
        [Display(Name = "Kritična")]
        public bool IsCritical { get; set; } = false;

        /// <summary>
        /// Date of creation (UTC, audit log, forensics).
        /// </summary>
        [Column("created_at")]
        [Display(Name = "Kreirano")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who created the permission (FK, audit).
        /// </summary>
        [Column("created_by")]
        [Display(Name = "Kreirao")]
        public int? CreatedById { get; set; }

        /// <summary>
        /// Navigation to creator user.
        /// </summary>
        [ForeignKey("CreatedById")]
        public virtual User? CreatedBy { get; set; }

        /// <summary>
        /// Date of last modification (UTC, audit log).
        /// </summary>
        [Column("last_modified")]
        [Display(Name = "Zadnja izmjena")]
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// User who last modified this permission (FK, audit).
        /// </summary>
        [Column("last_modified_by_id")]
        [Display(Name = "Izmijenio")]
        public int? LastModifiedById { get; set; }

        /// <summary>
        /// Navigation to user who last modified.
        /// </summary>
        [ForeignKey("LastModifiedById")]
        public virtual User? LastModifiedBy { get; set; }

        /// <summary>
        /// Digital signature (SHA, PKI, user/device) for forensic audit.
        /// </summary>
        [MaxLength(128)]
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Change version (for event sourcing/rollback).
        /// </summary>
        [Column("change_version")]
        [Display(Name = "Verzija promjene")]
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Soft-delete/archive flag (for GDPR, deprecation, rollback).
        /// </summary>
        [Column("is_deleted")]
        [Display(Name = "Arhivirano")]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Freeform note/comment (compliance, change request, audit, incident).
        /// </summary>
        [MaxLength(512)]
        [Column("note")]
        [Display(Name = "Napomena")]
        public string? Note { get; set; }

        // ========== NAVIGATION (RBAC/Audit/Trace) ==========

        /// <summary>
        /// Roles that include this permission (many-to-many via role_permissions).
        /// </summary>
        public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

        /// <summary>
        /// User-specific permission overrides (many-to-many via user_permissions).
        /// </summary>
        public virtual ICollection<User> Users { get; set; } = new List<User>();

        /// <summary>
        /// Role-permission mapping entries (audit, escalation, traceability).
        /// </summary>
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        /// <summary>
        /// User-permission assignment entries (audit, escalation, traceability).
        /// </summary>
        public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();

        /// <summary>
        /// Delegated permissions involving this permission.
        /// </summary>
        public virtual ICollection<DelegatedPermission> DelegatedPermissions { get; set; } = new List<DelegatedPermission>();

        /// <summary>
        /// Returns code and name for display or trace.
        /// </summary>
        public override string ToString() => $"{Code} – {Name}";
    }
}

