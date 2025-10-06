using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>WorkOrderPart</b> — Represents a part used on a specific Work Order.
    /// Tracks quantities, units, pricing, CAPA/incident linkages, and forensics for GMP compliance.
    /// Fully audit-ready and future-proof for all advanced CMMS scenarios.
    /// </summary>
    [Table("work_order_parts")]
    public partial class WorkOrderPart
    {
        [Key]
        [Column("id")]
        [Browsable(false)]
        [Display(Name = "ID")]
        public int Id { get; set; }

        [Required]
        [Column("work_order_id")]
        [Display(Name = "Radni nalog")]
        public int WorkOrderId { get; set; }

        [Required]
        [Column("part_id")]
        [Display(Name = "Dio/Part")]
        public int PartId { get; set; }

        [Required]
        [Column("quantity")]
        [Display(Name = "Kolièina")]
        public int Quantity { get; set; }

        [StringLength(255)]
        [Column("unit_of_measure")]
        [Display(Name = "Mjerna jedinica")]
        public string UnitOfMeasure { get; set; } = string.Empty;

        [DataType(DataType.Currency)]
        [Column("unit_price")]
        [Display(Name = "Jedinièna cijena")]
        public decimal? UnitPrice { get; set; }

        [StringLength(255)]
        [Column("currency")]
        [Display(Name = "Valuta")]
        public string Currency { get; set; } = string.Empty;

        [Column("warehouse_id")]
        [Display(Name = "Skladište")]
        public int? WarehouseId { get; set; }

        [Column("capa_case_id")]
        [Display(Name = "CAPA sluèaj")]
        public int? CapaCaseId { get; set; }

        [Column("incident_id")]
        [Display(Name = "Incident")]
        public int? IncidentId { get; set; }

        [Column("used_at")]
        [Display(Name = "Vrijeme korištenja")]
        public DateTime? UsedAt { get; set; }

        [Column("used_by_id")]
        [Display(Name = "Korisnik koji je zabilježio")]
        public int? UsedById { get; set; }

        [StringLength(255)]
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        [StringLength(255)]
        [Column("source_ip")]
        [Display(Name = "IP adresa")]
        public string SourceIp { get; set; } = string.Empty;

        [StringLength(255)]
        [Column("device_info")]
        [Display(Name = "Ureðaj/OS")]
        public string DeviceInfo { get; set; } = string.Empty;

        [StringLength(255)]
        [Column("session_id")]
        [Display(Name = "Sesija")]
        public string SessionId { get; set; } = string.Empty;

        [StringLength(255)]
        [Column("note")]
        [Display(Name = "Napomena")]
        public string Note { get; set; } = string.Empty;

        [ReadOnly(true)]
        [Column("created_at")]
        [Display(Name = "Vrijeme kreiranja")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ReadOnly(true)]
        [Column("updated_at")]
        [Display(Name = "Vrijeme zadnje izmjene")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("last_modified_at")]
        [Display(Name = "Vrijeme zadnje izmjene (legacy)")]
        public DateTime? LastModifiedAt { get; set; }

        [Column("last_modified_by_id")]
        [Display(Name = "Zadnji mijenjao")]
        public int? LastModifiedById { get; set; }

        [MaxLength(255)]
        [Column("work_order?")]
        public string? LegacyWorkOrderLabel { get; set; }

        [MaxLength(255)]
        [Column("part?")]
        public string? LegacyPartLabel { get; set; }

        [MaxLength(255)]
        [Column("warehouse?")]
        public string? LegacyWarehouseLabel { get; set; }

        [MaxLength(255)]
        [Column("user?")]
        public string? LegacyUserLabel { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(WorkOrderId))]
        public virtual WorkOrder? WorkOrder { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(PartId))]
        public virtual Part? Part { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(WarehouseId))]
        public virtual Warehouse? Warehouse { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(UsedById))]
        public virtual User? UsedBy { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(CapaCaseId))]
        public virtual CapaCase? CapaCase { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(IncidentId))]
        public virtual Incident? Incident { get; set; }

        [JsonIgnore]
        public decimal? TotalCost => UnitPrice.HasValue ? UnitPrice.Value * Quantity : (decimal?)null;

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

        public override string ToString()
        {
            return $"Part: {PartId} | Qty: {Quantity} {UnitOfMeasure} | UsedAt: {UsedAt?.ToString("u") ?? "N/A"}";
        }
    }
}

