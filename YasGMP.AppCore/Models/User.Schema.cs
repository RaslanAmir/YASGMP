using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema augmentation for <see cref="User"/> providing access to raw EF columns not exposed directly on the domain model.
    /// </summary>
    public partial class User
    {
        /// <summary>
        /// Gets or sets the password hash raw.
        /// </summary>
        [Column("password_hash")]
        [StringLength(256)]
        public string? PasswordHashRaw { get; set; }

        /// <summary>
        /// Gets or sets the role id.
        /// </summary>
        [Column("role_id")]
        public int? RoleId { get; set; }

        /// <summary>
        /// Gets or sets the tenant id.
        /// </summary>
        [Column("tenant_id")]
        public int? TenantId { get; set; }

        /// <summary>
        /// Gets or sets the job title id.
        /// </summary>
        [Column("job_title_id")]
        public int? JobTitleId { get; set; }

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the legacy user label.
        /// </summary>
        [Column("user?")]
        public string? LegacyUserLabel { get; set; }

        /// <summary>
        /// Gets or sets the legacy roles collection.
        /// </summary>
        [Column("icollection<role>")]
        public string? LegacyRolesCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy permissions collection.
        /// </summary>
        [Column("icollection<permission>")]
        public string? LegacyPermissionsCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy delegated permissions collection.
        /// </summary>
        [Column("icollection<delegated_permission>")]
        public string? LegacyDelegatedPermissionsCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy audit logs collection.
        /// </summary>
        [Column("icollection<audit_log>")]
        public string? LegacyAuditLogsCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy digital signatures collection.
        /// </summary>
        [Column("icollection<digital_signature>")]
        public string? LegacyDigitalSignaturesCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy session logs collection.
        /// </summary>
        [Column("icollection<session_log>")]
        public string? LegacySessionLogsCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy work orders collection.
        /// </summary>
        [Column("icollection<work_order>")]
        public string? LegacyWorkOrdersCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy photos collection.
        /// </summary>
        [Column("icollection<photo>")]
        public string? LegacyPhotosCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy attachments collection.
        /// </summary>
        [Column("icollection<attachment>")]
        public string? LegacyAttachmentsCollection { get; set; }

        /// <summary>
        /// Gets or sets the legacy admin activity collection.
        /// </summary>
        [Column("icollection<admin_activity_log>")]
        public string? LegacyAdminActivityCollection { get; set; }
    }
}
