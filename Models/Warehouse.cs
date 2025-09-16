using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace YasGMP.Models
{
    /// <summary>
    /// Warehouse storage location mapped to the <c>warehouses</c> table.
    /// </summary>
    [Table("warehouses")]
    public class Warehouse
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [StringLength(100)]
        public string? Name { get; set; }

        [Column("location")]
        [StringLength(255)]
        public string? Location { get; set; }

        [Column("responsible_id")]
        public int? ResponsibleId { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("location_id")]
        public int? LocationId { get; set; }

        [Column("responsible")]
        [StringLength(255)]
        public string? Responsible { get; set; }

        [Column("qr_code")]
        [StringLength(255)]
        public string? QrCode { get; set; }

        [Column("note")]
        [StringLength(500)]
        public string? Note { get; set; }

        [Column("created_by_id")]
        public int? CreatedById { get; set; }

        [Column("created_by")]
        [StringLength(255)]
        public string? CreatedBy { get; set; }

        [Column("last_modified")]
        public DateTime? LastModified { get; set; }

        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        [Column("last_modified_by")]
        [StringLength(255)]
        public string? LastModifiedBy { get; set; }

        [Column("digital_signature")]
        [StringLength(128)]
        public string? DigitalSignature { get; set; }

        [Column("status")]
        [StringLength(30)]
        public string? Status { get; set; }

        [Column("io_tdevice_id")]
        [StringLength(64)]
        public string? IoTDeviceId { get; set; }

        [Column("climate_mode")]
        [StringLength(60)]
        public string? ClimateMode { get; set; }

        [Column("compliance_docs")]
        [StringLength(255)]
        public string? ComplianceDocs { get; set; }

        [Column("entry_hash")]
        [StringLength(128)]
        public string? EntryHash { get; set; }

        [Column("source_ip")]
        [StringLength(45)]
        public string? SourceIp { get; set; }

        [Column("is_qualified")]
        public bool? IsQualified { get; set; }

        [Column("last_qualified")]
        public DateTime? LastQualified { get; set; }

        [Column("session_id")]
        [StringLength(80)]
        public string? SessionId { get; set; }

        [Column("anomaly_score")]
        [Precision(10, 2)]
        public decimal? AnomalyScore { get; set; }

        [Column("is_deleted")]
        public bool? IsDeleted { get; set; }
    }
}
