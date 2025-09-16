using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("component_types")]
    public class ComponentType
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("code")]
        [StringLength(50)]
        public string? Code { get; set; }

        [Column("name")]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
