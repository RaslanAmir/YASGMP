using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents the Inspection.
    /// </summary>
    [Table("inspections")]
    public partial class Inspection
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the inspection date.
        /// </summary>
        [Column("inspection_date")]
        public DateTime? InspectionDate { get; set; }

        /// <summary>
        /// Gets or sets the inspector name.
        /// </summary>
        [Column("inspector_name")]
        [MaxLength(100)]
        public string? InspectorName { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        [Column("type")]
        [MaxLength(32)]
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        [Column("result")]
        [MaxLength(32)]
        public string? Result { get; set; }

        /// <summary>
        /// Gets or sets the related machine id.
        /// </summary>
        [Column("related_machine")]
        public int? RelatedMachineId { get; set; }

        /// <summary>
        /// Gets or sets the related machine.
        /// </summary>
        [ForeignKey(nameof(RelatedMachineId))]
        public Machine? RelatedMachine { get; set; }

        /// <summary>
        /// Gets or sets the doc file.
        /// </summary>
        [Column("doc_file")]
        [MaxLength(255)]
        public string? DocFile { get; set; }

        /// <summary>
        /// Gets or sets the notes.
        /// </summary>
        [Column("notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the type id.
        /// </summary>
        [Column("type_id")]
        public int? TypeId { get; set; }

        /// <summary>
        /// Gets or sets the result id.
        /// </summary>
        [Column("result_id")]
        public int? ResultId { get; set; }
    }
}
