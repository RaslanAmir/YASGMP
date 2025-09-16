using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("lookup_domain")]
    public class LookupDomain
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("domain_code")]
        [StringLength(50)]
        public string? DomainCode { get; set; }

        [Column("description")]
        [StringLength(255)]
        public string? Description { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
