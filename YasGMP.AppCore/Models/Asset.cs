using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Asset / equipment model.
    /// </summary>
    [NotMapped]
    public class Asset
    {
        /// <summary>
        /// Primary key (machine/asset identifier).
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Unique asset code (human-friendly). Required.
        /// </summary>
        [Required, StringLength(50)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Asset name/title. Required.
        /// </summary>
        [Required, StringLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description / short summary.
        /// </summary>
        [StringLength(255)]
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Optional device/model name.
        /// </summary>
        [StringLength(100)]
        [Column("model")]
        public string? Model { get; set; }

        /// <summary>
        /// Optional manufacturer name.
        /// </summary>
        [StringLength(100)]
        [Column("manufacturer")]
        public string? Manufacturer { get; set; }

        /// <summary>
        /// Optional site/building/room.
        /// </summary>
        [StringLength(100)]
        [Column("location")]
        public string? Location { get; set; }

        /// <summary>
        /// Installation date (optional).
        /// </summary>
        [Column("install_date")]
        public DateTime? InstallDate { get; set; }

        /// <summary>
        /// Procurement date (optional).
        /// </summary>
        [Column("procurement_date")]
        public DateTime? ProcurementDate { get; set; }

        /// <summary>
        /// Purchase date (optional).
        /// </summary>
        [Column("purchase_date")]
        public DateTime? PurchaseDate { get; set; }

        /// <summary>
        /// Warranty valid until (optional).
        /// </summary>
        [Column("warranty_until")]
        public DateTime? WarrantyUntil { get; set; }

        /// <summary>
        /// Warranty expiry (optional, legacy).
        /// </summary>
        [Column("warranty_expiry")]
        public DateTime? WarrantyExpiry { get; set; }

        /// <summary>
        /// Optional asset status (e.g., Active/Inactive/Spare/Decommissioned).
        /// </summary>
        [Column("status")]
        public string? Status { get; set; }

        /// <summary>
        /// Optional URS document reference or path.
        /// </summary>
        [StringLength(255)]
        [Column("urs_doc")]
        public string? UrsDoc { get; set; }

        /// <summary>
        /// Decommission date (optional).
        /// </summary>
        [Column("decommission_date")]
        public DateTime? DecommissionDate { get; set; }

        /// <summary>
        /// Reason for decommission (optional).
        /// </summary>
        [Column("decommission_reason")]
        public string? DecommissionReason { get; set; }

        /// <summary>
        /// Digital signature / hash (optional).
        /// </summary>
        [StringLength(128)]
        [Column("digital_signature")]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Notes / free text (optional).
        /// </summary>
        [StringLength(255)]
        [Column("notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// Related components.
        /// </summary>
        public virtual ICollection<MachineComponent> Components { get; set; } = new List<MachineComponent>();

        /// <summary>
        /// Lifecycle history/events.
        /// </summary>
        public virtual ICollection<MachineLifecycleEvent> LifecycleEvents { get; set; } = new List<MachineLifecycleEvent>();

        /// <summary>
        /// Related CAPA cases.
        /// </summary>
        public virtual ICollection<CapaCase> CapaCases { get; set; } = new List<CapaCase>();

        /// <summary>
        /// Related quality events.
        /// </summary>
        public virtual ICollection<QualityEvent> QualityEvents { get; set; } = new List<QualityEvent>();

        /// <summary>
        /// Related validations/qualifications.
        /// </summary>
        public virtual ICollection<Validation> Validations { get; set; } = new List<Validation>();

        /// <summary>
        /// Related inspections.
        /// </summary>
        public virtual ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();

        /// <summary>
        /// Related work orders.
        /// </summary>
        public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();

        /// <summary>
        /// Related photos.
        /// </summary>
        public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();

        /// <summary>
        /// Related attachments/documents.
        /// </summary>
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

        /// <summary>
        /// Extensibility: arbitrary key-value pairs (not mapped).
        /// </summary>
        [NotMapped]
        public Dictionary<string, object> ExtraFields { get; set; } = new();

        /// <summary>
        /// Logical type (not mapped). Defaults to "Equipment".
        /// </summary>
        [NotMapped]
        public string AssetType { get; set; } = "Equipment";

        /// <summary>
        /// Generated QR code image path (not mapped / WPF shell).
        /// </summary>
        [NotMapped]
        public string? QrCode { get; set; }

        /// <summary>
        /// QR payload encoded in the generated image (not mapped / WPF shell).
        /// </summary>
        [NotMapped]
        public string? QrPayload { get; set; }

        /// <summary>
        /// Last modified timestamp (not mapped).
        /// </summary>
        [NotMapped]
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// Optional external system identifier (not mapped).
        /// </summary>
        [NotMapped]
        public string? ExternalSystemId { get; set; }

        /// <summary>
        /// Optional AI health score (not mapped).
        /// </summary>
        [NotMapped]
        public double? AiHealthScore { get; set; }

        /// <summary>
        /// Optional list of linked IoT device identifiers (not mapped).
        /// </summary>
        [NotMapped]
        public List<string> IoTDeviceIds { get; set; } = new();

        /// <summary>
        /// Optional digital twin JSON payload (not mapped).
        /// </summary>
        [NotMapped]
        public string? DigitalTwinJson { get; set; }

        /// <summary>
        /// Optional predicted next failure date (not mapped).
        /// </summary>
        [NotMapped]
        public DateTime? NextPredictedFailure { get; set; }

        /// <summary>
        /// Optional risk score (not mapped).
        /// </summary>
        [NotMapped]
        public int? RiskScore { get; set; }

        /// <summary>
        /// Alias for <see cref="Code"/> (not mapped).
        /// </summary>
        [NotMapped]
        public string AssetCode { get => Code; set => Code = value; }

        /// <summary>
        /// Alias for <see cref="Name"/> (not mapped).
        /// </summary>
        [NotMapped]
        public string AssetName { get => Name; set => Name = value; }

        /// <summary>
        /// Alias for <see cref="RiskScore"/> (not mapped).
        /// </summary>
        [NotMapped]
        public int? RiskRating { get => RiskScore; set => RiskScore = value; }
    }
}
