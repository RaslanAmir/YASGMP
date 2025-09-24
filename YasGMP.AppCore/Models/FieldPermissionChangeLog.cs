using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>FieldPermissionChangeLog</b> – Ultra-granular audit log for all changes to field-level access rights.
    /// <para>
    /// ⭐ SUPER-MEGA-ULTRA ROBUST:<br/>
    /// - Tracks who, when, where, how, and why every permission change was made<br/>
    /// - Captures before/after, reason, forensics, chain/version, digital signature, IP/device, extensibility for future AI, GDPR, legal defense.
    /// </para>
    /// </summary>
    public class FieldPermissionChangeLog
    {
        /// <summary>Unique identifier for the log record (Primary Key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Date and time the access right was changed.</summary>
        [Required]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>User ID who performed the change (Foreign Key).</summary>
        [Required]
        public int ChangedById { get; set; }

        /// <summary>Navigation to the user who performed the change.</summary>
        [ForeignKey(nameof(ChangedById))]
        public User? ChangedBy { get; set; }

        /// <summary>Table name containing the field whose permission was changed.</summary>
        [Required]
        [StringLength(100)]
        public string TableName { get; set; } = string.Empty;

        /// <summary>Name of the field whose access rights were changed.</summary>
        [Required]
        [StringLength(100)]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>Old permissions (e.g. "read=true; write=false; approve=false; export=false").</summary>
        [StringLength(256)]
        public string? OldPermission { get; set; }

        /// <summary>New permissions (e.g. "read=true; write=true; approve=false; export=false").</summary>
        [StringLength(256)]
        public string? NewPermission { get; set; }

        /// <summary>Extra note or justification for the permission change.</summary>
        [StringLength(1000)]
        public string? Note { get; set; }

        /// <summary>Digital signature or hash (Part 11 compliance, audit).</summary>
        [StringLength(256)]
        public string? DigitalSignature { get; set; }

        /// <summary>Forensic: IP address or device from which permission was changed.</summary>
        [StringLength(128)]
        public string? SourceIp { get; set; }

        /// <summary>Change version (audit chain, event sourcing, rollback support).</summary>
        public int ChangeVersion { get; set; } = 1;

        /// <summary>Soft delete/archive (GDPR, traceability).</summary>
        public bool IsDeleted { get; set; } = false;
    }
}
