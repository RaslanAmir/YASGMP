using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Describes the retention requirements for a stored attachment. Each upload
    /// receives a retention record capturing policy metadata and target disposition.
    /// </summary>
    [Table("retention_policies")]
    public class RetentionPolicy
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the attachment id.
        /// </summary>
        [Column("attachment_id")]
        public int AttachmentId { get; set; }

        /// <summary>
        /// Gets or sets the policy name.
        /// </summary>
        [Required]
        [StringLength(128)]
        [Column("policy_name")]
        public string PolicyName { get; set; } = "default";

        /// <summary>
        /// Gets or sets the retain until.
        /// </summary>
        [Column("retain_until")]
        public DateTime? RetainUntil { get; set; }

        /// <summary>
        /// Gets or sets the min retain days.
        /// </summary>
        [Column("min_retain_days")]
        public int? MinRetainDays { get; set; }

        /// <summary>
        /// Gets or sets the max retain days.
        /// </summary>
        [Column("max_retain_days")]
        public int? MaxRetainDays { get; set; }

        /// <summary>
        /// Gets or sets the legal hold.
        /// </summary>
        [Column("legal_hold")]
        public bool LegalHold { get; set; }

        /// <summary>
        /// Gets or sets the delete mode.
        /// </summary>
        [Column("delete_mode")]
        [StringLength(32)]
        public string DeleteMode { get; set; } = "soft";

        /// <summary>
        /// Gets or sets the review required.
        /// </summary>
        [Column("review_required")]
        public bool ReviewRequired { get; set; }

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the created by id.
        /// </summary>
        [Column("created_by_id")]
        public int? CreatedById { get; set; }

        /// <summary>
        /// Gets or sets the notes.
        /// </summary>
        [Column("notes")]
        [StringLength(512)]
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets the attachment.
        /// </summary>
        [ForeignKey(nameof(AttachmentId))]
        public virtual Attachment? Attachment { get; set; }

        /// <summary>
        /// Gets or sets the created by.
        /// </summary>
        [ForeignKey(nameof(CreatedById))]
        public virtual User? CreatedBy { get; set; }
    }
}
