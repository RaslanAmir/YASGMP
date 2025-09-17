using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace YasGMP.Models
{
    /// <summary>Entity model for the `component_qualifications` table.</summary>
    [Table("component_qualifications")]
    public class ComponentQualification
    {
        /// <summary>Gets or sets the id.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Gets or sets the component id.</summary>
        [Column("component_id")]
        public int? ComponentId { get; set; }

        /// <summary>Gets or sets the type.</summary>
        [Column("type")]
        public string? Type { get; set; }

        /// <summary>Gets or sets the qualification date.</summary>
        [Column("qualification_date")]
        public DateTime? QualificationDate { get; set; }

        /// <summary>Gets or sets the next due.</summary>
        [Column("next_due")]
        public DateTime? NextDue { get; set; }

        /// <summary>Gets or sets the status.</summary>
        [Column("status")]
        public string? Status { get; set; }

        /// <summary>Gets or sets the doc file.</summary>
        [Column("doc_file")]
        [StringLength(255)]
        public string? DocFile { get; set; }

        /// <summary>Gets or sets the signed by.</summary>
        [Column("signed_by")]
        [StringLength(128)]
        public string? SignedBy { get; set; }

        /// <summary>Gets or sets the created at.</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Gets or sets the updated at.</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Gets or sets the certificate number.</summary>
        [Column("certificate_number")]
        [StringLength(255)]
        public string? CertificateNumber { get; set; }

        /// <summary>Gets or sets the supplier id.</summary>
        [Column("supplier_id")]
        public int? SupplierId { get; set; }

        [ForeignKey(nameof(ComponentId))]
        public virtual MachineComponent? Component { get; set; }
    }
}
