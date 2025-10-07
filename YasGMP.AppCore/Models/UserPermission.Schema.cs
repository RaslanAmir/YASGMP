using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema extension for <see cref="UserPermission"/> exposing grant metadata and legacy label columns.
    /// </summary>
    public partial class UserPermission
    {
        /// <summary>
        /// Gets or sets the granted by id.
        /// </summary>
        [Column("granted_by")]
        public int? GrantedById { get; set; }

        /// <summary>
        /// Gets or sets the granted at.
        /// </summary>
        [Column("granted_at")]
        public DateTime? GrantedAt { get; set; }

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
        /// Gets or sets the user label.
        /// </summary>
        [Column("user")]
        public string? UserLabel { get; set; }

        /// <summary>
        /// Gets or sets the permission label.
        /// </summary>
        [Column("permission")]
        public string? PermissionLabel { get; set; }

        /// <summary>
        /// Gets or sets the permission code.
        /// </summary>
        [Column("code")]
        public string? PermissionCode { get; set; }
    }
}
