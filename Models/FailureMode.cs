using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("failure_modes")]
    public class FailureMode
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("component_type_id")]
        public int? ComponentTypeId { get; set; }

        [Column("code")]
        [StringLength(40)]
        public string? Code { get; set; }

        [Column("description")]
        [StringLength(255)]
        public string? Description { get; set; }

        [Column("severity_default")]
        public int? SeverityDefault { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
