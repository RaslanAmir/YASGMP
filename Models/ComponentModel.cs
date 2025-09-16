using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("component_models")]
    public class ComponentModel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("component_type_id")]
        public int? ComponentTypeId { get; set; }

        [Column("model_code")]
        [StringLength(100)]
        public string? ModelCode { get; set; }

        [Column("model_name")]
        [StringLength(150)]
        public string? ModelName { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
