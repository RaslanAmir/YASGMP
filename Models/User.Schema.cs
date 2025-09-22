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
        [Column("password_hash")]
        [StringLength(256)]
        public string? PasswordHashRaw { get; set; }

        [Column("role_id")]
        public int? RoleId { get; set; }

        [Column("tenant_id")]
        public int? TenantId { get; set; }

        [Column("job_title_id")]
        public int? JobTitleId { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("user?")]
        public string? LegacyUserLabel { get; set; }

        [Column("icollection<role>")]
        public string? LegacyRolesCollection { get; set; }

        [Column("icollection<permission>")]
        public string? LegacyPermissionsCollection { get; set; }

        [Column("icollection<delegated_permission>")]
        public string? LegacyDelegatedPermissionsCollection { get; set; }

        [Column("icollection<audit_log>")]
        public string? LegacyAuditLogsCollection { get; set; }

        [Column("icollection<digital_signature>")]
        public string? LegacyDigitalSignaturesCollection { get; set; }

        [Column("icollection<session_log>")]
        public string? LegacySessionLogsCollection { get; set; }

        [Column("icollection<work_order>")]
        public string? LegacyWorkOrdersCollection { get; set; }

        [Column("icollection<photo>")]
        public string? LegacyPhotosCollection { get; set; }

        [Column("icollection<attachment>")]
        public string? LegacyAttachmentsCollection { get; set; }

        [Column("icollection<admin_activity_log>")]
        public string? LegacyAdminActivityCollection { get; set; }
    }
}
