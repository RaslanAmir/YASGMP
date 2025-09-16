using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("ref_value")]
    public class RefValue
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("domain_id")]
        public int DomainId { get; set; }

        [Column("code")]
        [StringLength(255)]
        public string Code { get; set; } = string.Empty;

        [Column("label")]
        [StringLength(255)]
        public string? Label { get; set; }

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
