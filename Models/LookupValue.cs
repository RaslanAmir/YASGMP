using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `lookup_value` table.</summary>
    [Table("lookup_value")]
    public class LookupValue
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the domain id.</summary>
        [Column("domain_id")]
        public int DomainId { get; set; }

        /// <summary>Gets or sets the value code.</summary>
        [Column("value_code")]
        [StringLength(100)]
        public string? ValueCode { get; set; }

        /// <summary>Gets or sets the value label.</summary>
        [Column("value_label")]
        [StringLength(100)]
        public string? ValueLabel { get; set; }

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

        [ForeignKey(nameof(DomainId))]
        public virtual LookupDomain? Domain { get; set; }
    }
}
