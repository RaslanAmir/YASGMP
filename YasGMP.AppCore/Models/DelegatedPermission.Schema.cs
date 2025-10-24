using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema-level expansion for <see cref="DelegatedPermission"/> covering legacy timestamp columns and labels.
    /// </summary>
    public partial class DelegatedPermission
    {
        [Column("granted_by")]
        public int? GrantedById { get; set; }

        [Column("start_time")]
        public DateTime? StartTimeRaw { get; set; }

        [Column("expires_at")]
        public DateTime? ExpiresAtRaw { get; set; }

        [Column("revoked")]
        public bool? RevokedLegacy { get; set; }

        [Column("revoked_at")]
        public DateTime? RevokedAt { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("permission?")]
        public string? PermissionLabel { get; set; }

        [Column("user?")]
        public string? UserLabel { get; set; }

        [Column("code")]
        public string? DelegationCode { get; set; }
    }
}

