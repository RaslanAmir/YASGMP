using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("component_qualifications")]
    public class ComponentQualification
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("component_id")]
        public int? ComponentId { get; set; }

        [Column("type")]
        public string? Type { get; set; }

        [Column("qualification_date")]
        public DateTime? QualificationDate { get; set; }

        [Column("next_due")]
        public DateTime? NextDue { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("doc_file")]
        [StringLength(255)]
        public string? DocFile { get; set; }

        [Column("signed_by")]
        [StringLength(128)]
        public string? SignedBy { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("certificate_number")]
        [StringLength(255)]
        public string? CertificateNumber { get; set; }

        [Column("supplier_id")]
        public int? SupplierId { get; set; }
    }
}
