using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema-level expansion for <see cref="DelegatedPermission"/> covering legacy timestamp columns and labels.
    /// </summary>
    public partial class DelegatedPermission
    {
        /// <summary>
        /// Gets or sets the granted by id.
        /// </summary>
        [Column("granted_by")]
        public int? GrantedById { get; set; }

        /// <summary>
        /// Gets or sets the start time raw.
        /// </summary>
        [Column("start_time")]
        public DateTime? StartTimeRaw { get; set; }

        /// <summary>
        /// Gets or sets the expires at raw.
        /// </summary>
        [Column("expires_at")]
        public DateTime? ExpiresAtRaw { get; set; }

        /// <summary>
        /// Gets or sets the revoked legacy.
        /// </summary>
        [Column("revoked")]
        public bool? RevokedLegacy { get; set; }

        /// <summary>
        /// Gets or sets the revoked at.
        /// </summary>
        [Column("revoked_at")]
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the permission label.
        /// </summary>
        [Column("permission?")]
        public string? PermissionLabel { get; set; }

        /// <summary>
        /// Gets or sets the user label.
        /// </summary>
        [Column("user?")]
        public string? UserLabel { get; set; }

        /// <summary>
        /// Gets or sets the delegation code.
        /// </summary>
        [Column("code")]
        public string? DelegationCode { get; set; }
    }
}
