using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    public partial class DocumentVersion
    {
        [Column("related_table")]
        [StringLength(50)]
        public string? RelatedTable { get; set; }

        [Column("related_id")]
        public int? RelatedId { get; set; }

        [Column("version")]
        [StringLength(40)]
        public string? LegacyVersion { get; set; }

        [Column("created_by")]
        public int? CreatedByLegacyId { get; set; }

        [ForeignKey(nameof(CreatedByLegacyId))]
        public User? CreatedByLegacy { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("document")]
        [StringLength(255)]
        public string? DocumentLabel { get; set; }
    }
}
