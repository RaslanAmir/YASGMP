using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `permission_requests` table.</summary>
    [Table("permission_requests")]
    public class PermissionRequest
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the user id.</summary>
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>Gets or sets the permission id.</summary>
        [Column("permission_id")]
        public int PermissionId { get; set; }

        /// <summary>Gets or sets the requested at.</summary>
        [Column("requested_at")]
        public DateTime? RequestedAt { get; set; }

        /// <summary>Gets or sets the reason.</summary>
        [Column("reason")]
        [StringLength(255)]
        public string? Reason { get; set; }

        /// <summary>Gets or sets the status.</summary>
        [Column("status")]
        public string? Status { get; set; }

        /// <summary>Gets or sets the reviewed by.</summary>
        [Column("reviewed_by")]
        public int? ReviewedBy { get; set; }

        /// <summary>Gets or sets the reviewed at.</summary>
        [Column("reviewed_at")]
        public DateTime? ReviewedAt { get; set; }

        /// <summary>Gets or sets the review comment.</summary>
        [Column("review_comment")]
        [StringLength(255)]
        public string? ReviewComment { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        /// <summary>
        /// Gets or sets the permission.
        /// </summary>
        [ForeignKey(nameof(PermissionId))]
        public virtual Permission? Permission { get; set; }

        /// <summary>
        /// Gets or sets the reviewed by navigation.
        /// </summary>
        [ForeignKey(nameof(ReviewedBy))]
        public virtual User? ReviewedByNavigation { get; set; }
    }
}
