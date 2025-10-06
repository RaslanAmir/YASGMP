using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// UI-facing metadata extensions surfaced when projecting attachments from
    /// raw SQL queries. These properties are not mapped to EF columns but allow
    /// binding layers to expose retention/legal-hold information without
    /// relying on legacy <see cref="Attachment.FilePath"/> values.
    /// </summary>
    public partial class Attachment
    {
        /// <summary>Latest retention policy name captured for the attachment.</summary>
        [NotMapped]
        public string? RetentionPolicyName { get; set; }

        /// <summary>Latest retention deadline if one is enforced.</summary>
        [NotMapped]
        public DateTime? RetainUntil { get; set; }

        /// <summary>Flag indicating that a legal hold blocks automated purge.</summary>
        [NotMapped]
        public bool RetentionLegalHold { get; set; }

        /// <summary>Flag indicating that manual review is required before purge.</summary>
        [NotMapped]
        public bool RetentionReviewRequired { get; set; }

        /// <summary>Optional retention note or audit rationale.</summary>
        [NotMapped]
        public string? RetentionNotes { get; set; }
    }
}

