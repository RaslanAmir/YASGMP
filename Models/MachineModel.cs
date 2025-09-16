using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("machine_models")]
    public class MachineModel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("manufacturer_id")]
        public int? ManufacturerId { get; set; }

        [Column("machine_type_id")]
        public int? MachineTypeId { get; set; }

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
