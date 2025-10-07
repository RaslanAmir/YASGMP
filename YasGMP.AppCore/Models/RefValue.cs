using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `ref_value` table.</summary>
    [Table("ref_value")]
    public class RefValue
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the domain id.</summary>
        [Column("domain_id")]
        public int DomainId { get; set; }

        /// <summary>Gets or sets the code.</summary>
        [Column("code")]
        [StringLength(255)]
        public string Code { get; set; } = string.Empty;

        /// <summary>Gets or sets the label.</summary>
        [Column("label")]
        [StringLength(255)]
        public string? Label { get; set; }

        /// <summary>Gets or sets the is active.</summary>
        [Column("is_active")]
        public bool? IsActive { get; set; }

        /// <summary>Gets or sets the sort order.</summary>
        [Column("sort_order")]
        public int? SortOrder { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the domain.
        /// </summary>
        [ForeignKey(nameof(DomainId))]
        public virtual RefDomain? Domain { get; set; }
    }
}
