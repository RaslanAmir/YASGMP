using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("inspections")]
    public class Inspections
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("inspection_date")]
        public DateTime? InspectionDate { get; set; }

        [Column("inspector_name")]
        [StringLength(100)]
        public string? InspectorName { get; set; }

        [Column("type")]
        public string? Type { get; set; }

        [Column("result")]
        public string? Result { get; set; }

        [Column("related_machine")]
        public int? RelatedMachine { get; set; }

        [Column("doc_file")]
        [StringLength(255)]
        public string? DocFile { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("type_id")]
        public int? TypeId { get; set; }

        [Column("result_id")]
        public int? ResultId { get; set; }
    }
}
