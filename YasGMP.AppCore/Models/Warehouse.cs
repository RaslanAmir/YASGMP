using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Warehouse</b> — Ultra-robust model for all warehouse/storage locations with full GMP/CSV traceability.
    /// Tracks responsibility, access, compliance metadata, digital signatures, and IoT readiness.
    /// </summary>
    [Table("warehouses")]
    public partial class Warehouse
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        [Column("location")]
        public string Location { get; set; } = string.Empty;

        [Column("responsible_id")]
        public int ResponsibleId { get; set; }

        [ForeignKey(nameof(ResponsibleId))]
        public virtual User? Responsible { get; set; }

        [MaxLength(255)]
        [Column("qr_code")]
        public string QrCode { get; set; } = string.Empty;

        [MaxLength(500)]
        [Column("note")]
        public string Note { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("created_by_id")]
        public int? CreatedById { get; set; }

        [ForeignKey(nameof(CreatedById))]
        public virtual User? CreatedBy { get; set; }

        [Column("last_modified")]
        public DateTime? LastModified { get; set; }

        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        [ForeignKey(nameof(LastModifiedById))]
        public virtual User? LastModifiedBy { get; set; }

        [MaxLength(128)]
        [Column("digital_signature")]
        public string DigitalSignature { get; set; } = string.Empty;

        [MaxLength(30)]
        [Column("status")]
        public string Status { get; set; } = string.Empty;

        [MaxLength(64)]
        [Column("io_tdevice_id")]
        public string IoTDeviceId { get; set; } = string.Empty;

        [MaxLength(60)]
        [Column("climate_mode")]
        public string ClimateMode { get; set; } = string.Empty;

        [NotMapped]
        public List<string> ComplianceDocs { get; set; } = new();

        [MaxLength(128)]
        [Column("entry_hash")]
        public string EntryHash { get; set; } = string.Empty;

        [MaxLength(45)]
        [Column("source_ip")]
        public string SourceIp { get; set; } = string.Empty;

        [Column("is_qualified")]
        public bool IsQualified { get; set; }

        [Column("last_qualified")]
        public DateTime? LastQualified { get; set; }

        [MaxLength(80)]
        [Column("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [Column("anomaly_score")]
        public double? AnomalyScore { get; set; }

        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        [Column("location_id")]
        public int? LocationId { get; set; }

        [ForeignKey(nameof(LocationId))]
        public virtual Location? LocationReference { get; set; }

        [MaxLength(255)]
        [Column("responsible")]
        public string LegacyResponsibleName { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("created_by")]
        public string CreatedByName { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("last_modified_by")]
        public string LastModifiedByName { get; set; } = string.Empty;
    }
}

