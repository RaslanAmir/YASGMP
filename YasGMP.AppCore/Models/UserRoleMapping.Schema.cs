using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema supplement for <see cref="UserRoleMapping"/> exposing audit metadata and legacy label fields.
    /// </summary>
    public partial class UserRoleMapping
    {
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
        /// Gets or sets the user label.
        /// </summary>
        [Column("user")]
        public string? UserLabel { get; set; }

        /// <summary>
        /// Gets or sets the role label.
        /// </summary>
        [Column("role")]
        public string? RoleLabel { get; set; }

        /// <summary>
        /// Gets or sets the assigned by legacy id.
        /// </summary>
        [Column("assigned_by")]
        public int? AssignedByLegacyId { get; set; }
    }
}
