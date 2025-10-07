using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents the Document Version.
    /// </summary>
    public partial class DocumentVersion
    {
        /// <summary>
        /// Gets or sets the related table.
        /// </summary>
        [Column("related_table")]
        [StringLength(50)]
        public string? RelatedTable { get; set; }

        /// <summary>
        /// Gets or sets the related id.
        /// </summary>
        [Column("related_id")]
        public int? RelatedId { get; set; }

        /// <summary>
        /// Gets or sets the legacy version.
        /// </summary>
        [Column("version")]
        [StringLength(40)]
        public string? LegacyVersion { get; set; }

        /// <summary>
        /// Gets or sets the created by legacy id.
        /// </summary>
        [Column("created_by")]
        public int? CreatedByLegacyId { get; set; }

        /// <summary>
        /// Gets or sets the created by legacy.
        /// </summary>
        [ForeignKey(nameof(CreatedByLegacyId))]
        public User? CreatedByLegacy { get; set; }

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the document label.
        /// </summary>
        [Column("document")]
        [StringLength(255)]
        public string? DocumentLabel { get; set; }
    }
}
