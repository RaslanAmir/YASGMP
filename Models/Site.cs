using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("sites")]
    public class Site
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("code")]
        [StringLength(20)]
        public string? Code { get; set; }

        [Column("name")]
        [StringLength(100)]
        public string? Name { get; set; }

        [Column("timezone")]
        [StringLength(40)]
        public string? Timezone { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
