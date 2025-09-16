using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("lookup_value")]
    public class LookupValue
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("domain_id")]
        public int DomainId { get; set; }

        [Column("value_code")]
        [StringLength(100)]
        public string? ValueCode { get; set; }

        [Column("value_label")]
        [StringLength(100)]
        public string? ValueLabel { get; set; }

        [Column("is_active")]
        public bool? IsActive { get; set; }

        [Column("sort_order")]
        public int? SortOrder { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
