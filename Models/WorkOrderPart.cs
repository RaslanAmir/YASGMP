using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>WorkOrderPart</b> — Represents a part used on a specific Work Order.
    /// Tracks quantities, units, pricing, CAPA/incident linkages, and forensics for GMP compliance.
    /// Fully audit-ready and future-proof for all advanced CMMS scenarios.
    /// </summary>
    public class WorkOrderPart
    {
        /// <summary>Unique identifier for the WorkOrderPart record (for ORM use, composite key is WorkOrderId+PartId).</summary>
        [Browsable(false)]
        [Display(Name = "ID")]
        public int Id { get; set; }

        /// <summary>Foreign key to the WorkOrder.</summary>
        [Required]
        [Display(Name = "Radni nalog")]
        public int WorkOrderId { get; set; }

        /// <summary>Foreign key to the Part.</summary>
        [Required]
        [Display(Name = "Dio/Part")]
        public int PartId { get; set; }

        /// <summary>Quantity of the part used in the work order.</summary>
        [Required]
        [Range(0.01, 1000000)]
        [Display(Name = "Količina")]
        public decimal Quantity { get; set; }

        /// <summary>Unit of measurement for the part (e.g., pcs, ml, kg, etc).</summary>
        [StringLength(20)]
        [Display(Name = "Mjerna jedinica")]
        public string UnitOfMeasure { get; set; } = string.Empty;

        /// <summary>Price per unit at the time of use (audit).</summary>
        [DataType(DataType.Currency)]
        [Display(Name = "Jedinična cijena")]
        public decimal? UnitPrice { get; set; }

        /// <summary>Currency code (ISO 4217, e.g., EUR, USD, HRK).</summary>
        [StringLength(10)]
        [Display(Name = "Valuta")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>Warehouse from which the part was withdrawn.</summary>
        [Display(Name = "Skladište")]
        public int? WarehouseId { get; set; }

        /// <summary>Foreign key to related CAPA case (if part usage was triggered by CAPA).</summary>
        [Display(Name = "CAPA slučaj")]
        public int? CapaCaseId { get; set; }

        /// <summary>Foreign key to related Incident.</summary>
        [Display(Name = "Incident")]
        public int? IncidentId { get; set; }

        /// <summary>Timestamp when the part was withdrawn/applied.</summary>
        [Display(Name = "Vrijeme korištenja")]
        public DateTime? UsedAt { get; set; }

        /// <summary>ID of the user who recorded the part usage.</summary>
        [Display(Name = "Korisnik koji je zabilježio")]
        public int? UsedById { get; set; }

        /// <summary>GMP digital signature hash (for traceability, integrity, e-sign).</summary>
        [StringLength(128)]
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Audit: IP address from which the part was recorded.</summary>
        [StringLength(45)]
        [Display(Name = "IP adresa")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Audit: Device information (browser, device name, OS).</summary>
        [StringLength(255)]
        [Display(Name = "Uređaj/OS")]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>Audit: Session ID (for forensic linking).</summary>
        [StringLength(100)]
        [Display(Name = "Sesija")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>Additional notes or remarks about this part usage.</summary>
        [StringLength(512)]
        [Display(Name = "Napomena")]
        public string Note { get; set; } = string.Empty;

        /// <summary>Timestamp for record creation (UTC).</summary>
        [ReadOnly(true)]
        [Display(Name = "Vrijeme kreiranja")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Timestamp for last modification (UTC).</summary>
        [ReadOnly(true)]
        [Display(Name = "Vrijeme zadnje izmjene")]
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

        /// <summary>ID of the user who last modified the record.</summary>
        [Display(Name = "Zadnji mijenjao")]
        public int? LastModifiedById { get; set; }

        #region Navigation Properties (for ORM/data binding)

        /// <summary>Reference to parent WorkOrder (not serialized in API).</summary>
        [JsonIgnore]
        public virtual WorkOrder? WorkOrder { get; set; }

        /// <summary>Reference to Part.</summary>
        [JsonIgnore]
        public virtual Part? Part { get; set; }

        /// <summary>Reference to Warehouse.</summary>
        [JsonIgnore]
        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>Reference to User who used/recorded the part.</summary>
        [JsonIgnore]
        public virtual User? UsedBy { get; set; }

        /// <summary>Reference to CAPA case.</summary>
        [JsonIgnore]
        public virtual CapaCase? CapaCase { get; set; }

        /// <summary>Reference to Incident.</summary>
        [JsonIgnore]
        public virtual Incident? Incident { get; set; }

        #endregion

        /// <summary>Returns the total cost for this usage (if price/unit available).</summary>
        [JsonIgnore]
        public decimal? TotalCost => UnitPrice.HasValue ? UnitPrice.Value * Quantity : (decimal?)null;

        /// <summary>Creates a deep copy of this <see cref="WorkOrderPart"/> (for rollback/audit).</summary>
        public WorkOrderPart DeepCopy()
        {
            return new WorkOrderPart
            {
                Id = this.Id,
                WorkOrderId = this.WorkOrderId,
                PartId = this.PartId,
                Quantity = this.Quantity,
                UnitOfMeasure = this.UnitOfMeasure,
                UnitPrice = this.UnitPrice,
                Currency = this.Currency,
                WarehouseId = this.WarehouseId,
                CapaCaseId = this.CapaCaseId,
                IncidentId = this.IncidentId,
                UsedAt = this.UsedAt,
                UsedById = this.UsedById,
                DigitalSignature = this.DigitalSignature,
                SourceIp = this.SourceIp,
                DeviceInfo = this.DeviceInfo,
                SessionId = this.SessionId,
                Note = this.Note,
                CreatedAt = this.CreatedAt,
                LastModifiedAt = this.LastModifiedAt,
                LastModifiedById = this.LastModifiedById,
                // Navigation props (not deep copied to avoid circular ref issues)
                WorkOrder = this.WorkOrder,
                Part = this.Part,
                Warehouse = this.Warehouse,
                UsedBy = this.UsedBy,
                CapaCase = this.CapaCase,
                Incident = this.Incident
            };
        }

        /// <summary>Returns a human-readable string for logs/UI/debugging.</summary>
        public override string ToString()
        {
            return $"Part: {PartId} | Qty: {Quantity} {UnitOfMeasure} | UsedAt: {UsedAt?.ToString("u") ?? "N/A"}";
        }
    }
}
