using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Warehouse</b>  Ultra-robust model for all warehouse/storage locations with full GMP/CSV traceability.
    /// Tracks responsibility, access, compliance metadata, digital signatures, and IoT readiness.
    /// </summary>
    [Table("warehouses")]
    public partial class Warehouse
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [Required, MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        [Required, MaxLength(255)]
        [Column("location")]
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the responsible id.
        /// </summary>
        [Column("responsible_id")]
        public int ResponsibleId { get; set; }

        /// <summary>
        /// Gets or sets the responsible.
        /// </summary>
        [ForeignKey(nameof(ResponsibleId))]
        public virtual User? Responsible { get; set; }

        /// <summary>
        /// Gets or sets the qr code.
        /// </summary>
        [MaxLength(255)]
        [Column("qr_code")]
        public string QrCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        [MaxLength(500)]
        [Column("note")]
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the created by id.
        /// </summary>
        [Column("created_by_id")]
        public int? CreatedById { get; set; }

        /// <summary>
        /// Gets or sets the created by.
        /// </summary>
        [ForeignKey(nameof(CreatedById))]
        public virtual User? CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the last modified.
        /// </summary>
        [Column("last_modified")]
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// Gets or sets the last modified by id.
        /// </summary>
        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        /// <summary>
        /// Gets or sets the last modified by.
        /// </summary>
        [ForeignKey(nameof(LastModifiedById))]
        public virtual User? LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the digital signature.
        /// </summary>
        [MaxLength(128)]
        [Column("digital_signature")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        [MaxLength(30)]
        [Column("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the io t device id.
        /// </summary>
        [MaxLength(64)]
        [Column("io_tdevice_id")]
        public string IoTDeviceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the climate mode.
        /// </summary>
        [MaxLength(60)]
        [Column("climate_mode")]
        public string ClimateMode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the compliance docs.
        /// </summary>
        [NotMapped]
        public List<string> ComplianceDocs { get; set; } = new();

        /// <summary>
        /// Gets or sets the entry hash.
        /// </summary>
        [MaxLength(128)]
        [Column("entry_hash")]
        public string EntryHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source ip.
        /// </summary>
        [MaxLength(45)]
        [Column("source_ip")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the is qualified.
        /// </summary>
        [Column("is_qualified")]
        public bool IsQualified { get; set; }

        /// <summary>
        /// Gets or sets the last qualified.
        /// </summary>
        [Column("last_qualified")]
        public DateTime? LastQualified { get; set; }

        /// <summary>
        /// Gets or sets the session id.
        /// </summary>
        [MaxLength(80)]
        [Column("session_id")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the anomaly score.
        /// </summary>
        [Column("anomaly_score")]
        public double? AnomalyScore { get; set; }

        /// <summary>
        /// Gets or sets the is deleted.
        /// </summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Gets or sets the location id.
        /// </summary>
        [Column("location_id")]
        public int? LocationId { get; set; }

        /// <summary>
        /// Gets or sets the location reference.
        /// </summary>
        [ForeignKey(nameof(LocationId))]
        public virtual Location? LocationReference { get; set; }

        /// <summary>
        /// Gets or sets the legacy responsible name.
        /// </summary>
        [MaxLength(255)]
        [Column("responsible")]
        public string LegacyResponsibleName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the created by name.
        /// </summary>
        [MaxLength(255)]
        [Column("created_by")]
        public string CreatedByName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last modified by name.
        /// </summary>
        [MaxLength(255)]
        [Column("last_modified_by")]
        public string LastModifiedByName { get; set; } = string.Empty;
    }
}
