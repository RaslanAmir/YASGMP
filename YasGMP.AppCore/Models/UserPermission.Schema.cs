using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema extension for <see cref="UserPermission"/> exposing grant metadata and legacy label columns.
    /// </summary>
    public partial class UserPermission
    {
        [Column("granted_by")]
        public int? GrantedById { get; set; }

        [Column("granted_at")]
        public DateTime? GrantedAt { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("user")]
        public string? UserLabel { get; set; }

        [Column("permission")]
        public string? PermissionLabel { get; set; }

        [Column("code")]
        public string? PermissionCode { get; set; }
    }
}

