using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>WorkOrderPart</b>  Represents a part used on a specific Work Order.
    /// Tracks quantities, units, pricing, CAPA/incident linkages, and forensics for GMP compliance.
    /// Fully audit-ready and future-proof for all advanced CMMS scenarios.
    /// </summary>
    [Table("work_order_parts")]
    public partial class WorkOrderPart
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        [Browsable(false)]
        [Display(Name = "ID")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the work order id.
        /// </summary>
        [Required]
        [Column("work_order_id")]
        [Display(Name = "Radni nalog")]
        public int WorkOrderId { get; set; }

        /// <summary>
        /// Gets or sets the part id.
        /// </summary>
        [Required]
        [Column("part_id")]
        [Display(Name = "Dio/Part")]
        public int PartId { get; set; }

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        [Required]
        [Column("quantity")]
        [Display(Name = "Koliina")]
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the unit of measure.
        /// </summary>
        [StringLength(255)]
        [Column("unit_of_measure")]
        [Display(Name = "Mjerna jedinica")]
        public string UnitOfMeasure { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unit price.
        /// </summary>
        [DataType(DataType.Currency)]
        [Column("unit_price")]
        [Display(Name = "Jedinina cijena")]
        public decimal? UnitPrice { get; set; }

        /// <summary>
        /// Gets or sets the currency.
        /// </summary>
        [StringLength(255)]
        [Column("currency")]
        [Display(Name = "Valuta")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the warehouse id.
        /// </summary>
        [Column("warehouse_id")]
        [Display(Name = "Skladite")]
        public int? WarehouseId { get; set; }

        /// <summary>
        /// Gets or sets the capa case id.
        /// </summary>
        [Column("capa_case_id")]
        [Display(Name = "CAPA sluaj")]
        public int? CapaCaseId { get; set; }

        /// <summary>
        /// Gets or sets the incident id.
        /// </summary>
        [Column("incident_id")]
        [Display(Name = "Incident")]
        public int? IncidentId { get; set; }

        /// <summary>
        /// Gets or sets the used at.
        /// </summary>
        [Column("used_at")]
        [Display(Name = "Vrijeme koritenja")]
        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// Gets or sets the used by id.
        /// </summary>
        [Column("used_by_id")]
        [Display(Name = "Korisnik koji je zabiljeio")]
        public int? UsedById { get; set; }

        /// <summary>
        /// Gets or sets the digital signature.
        /// </summary>
        [StringLength(255)]
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source ip.
        /// </summary>
        [StringLength(255)]
        [Column("source_ip")]
        [Display(Name = "IP adresa")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the device info.
        /// </summary>
        [StringLength(255)]
        [Column("device_info")]
        [Display(Name = "Ureaj/OS")]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the session id.
        /// </summary>
        [StringLength(255)]
        [Column("session_id")]
        [Display(Name = "Sesija")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        [StringLength(255)]
        [Column("note")]
        [Display(Name = "Napomena")]
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [ReadOnly(true)]
        [Column("created_at")]
        [Display(Name = "Vrijeme kreiranja")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [ReadOnly(true)]
        [Column("updated_at")]
        [Display(Name = "Vrijeme zadnje izmjene")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the last modified at.
        /// </summary>
        [Column("last_modified_at")]
        [Display(Name = "Vrijeme zadnje izmjene (legacy)")]
        public DateTime? LastModifiedAt { get; set; }

        /// <summary>
        /// Gets or sets the last modified by id.
        /// </summary>
        [Column("last_modified_by_id")]
        [Display(Name = "Zadnji mijenjao")]
        public int? LastModifiedById { get; set; }

        /// <summary>
        /// Gets or sets the legacy work order label.
        /// </summary>
        [MaxLength(255)]
        [Column("work_order?")]
        public string? LegacyWorkOrderLabel { get; set; }

        /// <summary>
        /// Gets or sets the legacy part label.
        /// </summary>
        [MaxLength(255)]
        [Column("part?")]
        public string? LegacyPartLabel { get; set; }

        /// <summary>
        /// Gets or sets the legacy warehouse label.
        /// </summary>
        [MaxLength(255)]
        [Column("warehouse?")]
        public string? LegacyWarehouseLabel { get; set; }

        /// <summary>
        /// Gets or sets the legacy user label.
        /// </summary>
        [MaxLength(255)]
        [Column("user?")]
        public string? LegacyUserLabel { get; set; }

        /// <summary>
        /// Gets or sets the work order.
        /// </summary>
        [JsonIgnore]
        [ForeignKey(nameof(WorkOrderId))]
        public virtual WorkOrder? WorkOrder { get; set; }

        /// <summary>
        /// Gets or sets the part.
        /// </summary>
        [JsonIgnore]
        [ForeignKey(nameof(PartId))]
        public virtual Part? Part { get; set; }

        /// <summary>
        /// Gets or sets the warehouse.
        /// </summary>
        [JsonIgnore]
        [ForeignKey(nameof(WarehouseId))]
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>
        /// Gets or sets the used by.
        /// </summary>
        [JsonIgnore]
        [ForeignKey(nameof(UsedById))]
        public virtual User? UsedBy { get; set; }

        /// <summary>
        /// Gets or sets the capa case.
        /// </summary>
        [JsonIgnore]
        [ForeignKey(nameof(CapaCaseId))]
        public virtual CapaCase? CapaCase { get; set; }

        /// <summary>
        /// Gets or sets the incident.
        /// </summary>
        [JsonIgnore]
        [ForeignKey(nameof(IncidentId))]
        public virtual Incident? Incident { get; set; }

        /// <summary>
        /// Executes the total cost operation.
        /// </summary>
        [JsonIgnore]
        public decimal? TotalCost => UnitPrice.HasValue ? UnitPrice.Value * Quantity : (decimal?)null;
        /// <summary>
        /// Executes the deep copy operation.
        /// </summary>

        public WorkOrderPart DeepCopy()
        {
            return new WorkOrderPart
            {
                Id = Id,
                WorkOrderId = WorkOrderId,
                PartId = PartId,
                Quantity = Quantity,
                UnitOfMeasure = UnitOfMeasure,
                UnitPrice = UnitPrice,
                Currency = Currency,
                WarehouseId = WarehouseId,
                CapaCaseId = CapaCaseId,
                IncidentId = IncidentId,
                UsedAt = UsedAt,
                UsedById = UsedById,
                DigitalSignature = DigitalSignature,
                SourceIp = SourceIp,
                DeviceInfo = DeviceInfo,
                SessionId = SessionId,
                Note = Note,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                LastModifiedAt = LastModifiedAt,
                LastModifiedById = LastModifiedById,
                LegacyWorkOrderLabel = LegacyWorkOrderLabel,
                LegacyPartLabel = LegacyPartLabel,
                LegacyWarehouseLabel = LegacyWarehouseLabel,
                LegacyUserLabel = LegacyUserLabel,
                WorkOrder = WorkOrder,
                Part = Part,
                Warehouse = Warehouse,
                UsedBy = UsedBy,
                CapaCase = CapaCase,
                Incident = Incident
            };
        }
        /// <summary>
        /// Executes the to string operation.
        /// </summary>

        public override string ToString()
        {
            return $"Part: {PartId} | Qty: {Quantity} {UnitOfMeasure} | UsedAt: {UsedAt?.ToString("u") ?? "N/A"}";
        }
    }
}
