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
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("attachment_id")]
        public int AttachmentId { get; set; }

        [Required]
        [StringLength(128)]
        [Column("policy_name")]
        public string PolicyName { get; set; } = "default";

        [Column("retain_until")]
        public DateTime? RetainUntil { get; set; }

        [Column("min_retain_days")]
        public int? MinRetainDays { get; set; }

        [Column("max_retain_days")]
        public int? MaxRetainDays { get; set; }

        [Column("legal_hold")]
        public bool LegalHold { get; set; }

        [Column("delete_mode")]
        [StringLength(32)]
        public string DeleteMode { get; set; } = "soft";

        [Column("review_required")]
        public bool ReviewRequired { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("created_by_id")]
        public int? CreatedById { get; set; }

        [Column("notes")]
        [StringLength(512)]
        public string? Notes { get; set; }

        [ForeignKey(nameof(AttachmentId))]
        public virtual Attachment? Attachment { get; set; }

        [ForeignKey(nameof(CreatedById))]
        public virtual User? CreatedBy { get; set; }
    }
}
