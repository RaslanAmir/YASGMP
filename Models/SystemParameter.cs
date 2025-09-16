using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("system_parameters")]
    public class SystemParameter
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("param_name")]
        [StringLength(100)]
        public string? ParamName { get; set; }

        [Column("param_value")]
        public string? ParamValue { get; set; }

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("note")]
        public string? Note { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("digital_signature")]
        [StringLength(255)]
        public string? DigitalSignature { get; set; }
    }
}
