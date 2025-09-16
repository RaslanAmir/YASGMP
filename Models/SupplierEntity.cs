using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    [Table("suppliers")]
    public class SupplierEntity
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [StringLength(100)]
        public string? Name { get; set; }

        [Column("vat_number")]
        [StringLength(40)]
        public string? VatNumber { get; set; }

        [Column("code")]
        [StringLength(50)]
        public string? Code { get; set; }

        [Column("oib")]
        [StringLength(40)]
        public string? Oib { get; set; }

        [Column("contact")]
        [StringLength(100)]
        public string? Contact { get; set; }

        [Column("email")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Column("phone")]
        [StringLength(50)]
        public string? Phone { get; set; }

        [Column("website")]
        [StringLength(200)]
        public string? Website { get; set; }

        [Column("supplier_type")]
        [StringLength(40)]
        public string? SupplierType { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("contract_file")]
        [StringLength(255)]
        public string? ContractFile { get; set; }

        [Column("address")]
        public string? Address { get; set; }

        [Column("city")]
        [StringLength(80)]
        public string? City { get; set; }

        [Column("country")]
        [StringLength(80)]
        public string? Country { get; set; }

        [Column("type")]
        [StringLength(40)]
        public string? Type { get; set; }

        [Column("status")]
        [StringLength(40)]
        public string? Status { get; set; }

        [Column("contract_start")]
        public DateTime? ContractStart { get; set; }

        [Column("contract_end")]
        public DateTime? ContractEnd { get; set; }

        [Column("cert_doc")]
        [StringLength(255)]
        public string? CertDoc { get; set; }

        [Column("valid_until")]
        public DateTime? ValidUntil { get; set; }

        [Column("risk_score")]
        public int? RiskScore { get; set; }

        [Column("last_audit")]
        public DateTime? LastAudit { get; set; }

        [Column("comment")]
        public string? Comment { get; set; }

        [Column("digital_signature")]
        [StringLength(128)]
        public string? DigitalSignature { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("status_id")]
        public int? StatusId { get; set; }

        [Column("type_id")]
        public int? TypeId { get; set; }
    }
}
