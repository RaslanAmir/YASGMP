using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema supplement for <see cref="UserRoleMapping"/> exposing audit metadata and legacy label fields.
    /// </summary>
    public partial class UserRoleMapping
    {
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("granted_by")]
        public int? GrantedById { get; set; }

        [Column("granted_at")]
        public DateTime? GrantedAt { get; set; }

        [Column("user")]
        public string? UserLabel { get; set; }

        [Column("role")]
        public string? RoleLabel { get; set; }

        [Column("assigned_by")]
        public int? AssignedByLegacyId { get; set; }
    }
}

