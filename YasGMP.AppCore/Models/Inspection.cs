using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    [Table("inspections")]
    public partial class Inspection
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("inspection_date")]
        public DateTime? InspectionDate { get; set; }

        [Column("inspector_name")]
        [MaxLength(100)]
        public string? InspectorName { get; set; }

        [Column("type")]
        [MaxLength(32)]
        public string? Type { get; set; }

        [Column("result")]
        [MaxLength(32)]
        public string? Result { get; set; }

        [Column("related_machine")]
        public int? RelatedMachineId { get; set; }

        [ForeignKey(nameof(RelatedMachineId))]
        public Machine? RelatedMachine { get; set; }

        [Column("doc_file")]
        [MaxLength(255)]
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

