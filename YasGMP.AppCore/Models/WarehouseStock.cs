using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>WarehouseStock</b> – The most robust inventory record for any part in any warehouse/location.
    /// Covers GMP, CAPA, traceability, full forensic audit, recalls, serialization, compliance, and more.
    /// Supports all regulatory/inspection demands (21 CFR, HALMED, EU GMP).
    /// </summary>
    public class WarehouseStock
    {
        /// <summary>Unique ID for this stock record (Primary Key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Foreign key to the part stored in this warehouse.</summary>
        [Required]
        public int PartId { get; set; }

        /// <summary>Navigation to the part.</summary>
        public Part? Part { get; set; }

        /// <summary>Foreign key to the warehouse entity.</summary>
        public int WarehouseId { get; set; }

        /// <summary>Navigation to the warehouse.</summary>
        public Warehouse? Warehouse { get; set; }

        /// <summary>Name or code of the warehouse (optional redundancy for reporting).</summary>
        [Required, StringLength(80)]
        [Display(Name = "Skladište")]
        public string WarehouseName { get; set; } = string.Empty;

        /// <summary>Physical sub-location inside the warehouse (rack, shelf, box, etc.).</summary>
        [StringLength(80)]
        [Display(Name = "Lokacija u skladištu")]
        public string LocationDetail { get; set; } = string.Empty;

        /// <summary>Total available quantity of this part in the given warehouse/location.</summary>
        [Display(Name = "Količina")]
        public int Quantity { get; set; }

        /// <summary>Minimum stock threshold for triggering alerts or reordering.</summary>
        [Display(Name = "Min. količina")]
        public int? MinStock { get; set; }

        /// <summary>Maximum stock threshold for inventory optimization.</summary>
        [Display(Name = "Max. količina")]
        public int? MaxStock { get; set; }

        /// <summary>Quantity reserved for pending work orders (cannot be used elsewhere).</summary>
        [Display(Name = "Rezervirano")]
        public int Reserved { get; set; }

        /// <summary>Quantity blocked or quarantined due to CAPA, defects, or incidents.</summary>
        [Display(Name = "Blokirano/defektno")]
        public int Blocked { get; set; }

        /// <summary>Batch or lot number for traceability, recalls, and expiry management.</summary>
        [StringLength(50)]
        [Display(Name = "Serijski/batch broj")]
        public string BatchNumber { get; set; } = string.Empty;

        /// <summary>Serial number (for unique part traceability).</summary>
        [StringLength(80)]
        [Display(Name = "Serijski broj")]
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>Expiry date (if applicable, for perishable or time-sensitive parts).</summary>
        [Display(Name = "Rok trajanja")]
        public DateTime? ExpiryDate { get; set; }

        /// <summary>Date received into warehouse.</summary>
        [Display(Name = "Datum zaprimanja")]
        public DateTime? ReceivedAt { get; set; }

        /// <summary>Last full physical count date (for audit).</summary>
        [Display(Name = "Datum zadnje inventure")]
        public DateTime? LastPhysicalCount { get; set; }

        /// <summary>Reason for quarantine/block (CAPA, complaint, deviation).</summary>
        [StringLength(255)]
        [Display(Name = "Razlog blokade")]
        public string BlockReason { get; set; } = string.Empty;

        /// <summary>Recall reference if this batch/stock is under recall action.</summary>
        [StringLength(100)]
        [Display(Name = "Opoziv referenca")]
        public string RecallReference { get; set; } = string.Empty;

        /// <summary>Whether this stock is currently under official recall.</summary>
        [Display(Name = "Opoziv u tijeku")]
        public bool IsUnderRecall { get; set; }

        /// <summary>Forensic: audit trail of last modification.</summary>
        [Display(Name = "Zadnja izmjena")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>ID of the user who made the last modification.</summary>
        public int LastModifiedById { get; set; }

        /// <summary>Navigation to last modifying user.</summary>
        public User? LastModifiedBy { get; set; }

        /// <summary>Digital signature or cryptographic hash of the last modification (for forensic compliance).</summary>
        [StringLength(128)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>IP address or device information used during the last modification (for forensic analysis).</summary>
        [StringLength(64)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>External document/certificate paths (CoA, test report, etc.).</summary>
        [StringLength(512)]
        public string CertificateDocs { get; set; } = string.Empty;

        /// <summary>Custom inspection notes.</summary>
        [StringLength(400)]
        public string InspectionNote { get; set; } = string.Empty;

        // ==================== ✅ COMPUTED PROPERTIES ====================

        /// <summary>Returns the quantity available for use (excluding reserved and blocked).</summary>
        public int Available => Math.Max(0, Quantity - Reserved - Blocked);

        /// <summary>Returns whether the stock is below the minimum threshold.</summary>
        public bool IsBelowMin => MinStock.HasValue && Available < MinStock.Value;

        /// <summary>Returns whether the stock is above the maximum threshold.</summary>
        public bool IsAboveMax => MaxStock.HasValue && Quantity > MaxStock.Value;

        /// <summary>Returns whether the stock batch is expired.</summary>
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value.Date < DateTime.UtcNow.Date;

        /// <summary>Returns whether this stock is in an alert state (below min, expired, or under recall).</summary>
        public bool IsAlert => IsBelowMin || IsExpired || IsUnderRecall;

        // ==================== ✅ METHODS ====================

        /// <summary>Creates a deep copy of this <see cref="WarehouseStock"/> record (for rollback/audit).</summary>
        public WarehouseStock DeepCopy()
        {
            return new WarehouseStock
            {
                Id = this.Id,
                PartId = this.PartId,
                Part = this.Part,
                WarehouseId = this.WarehouseId,
                Warehouse = this.Warehouse,
                WarehouseName = this.WarehouseName,
                LocationDetail = this.LocationDetail,
                Quantity = this.Quantity,
                MinStock = this.MinStock,
                MaxStock = this.MaxStock,
                Reserved = this.Reserved,
                Blocked = this.Blocked,
                BatchNumber = this.BatchNumber,
                SerialNumber = this.SerialNumber,
                ExpiryDate = this.ExpiryDate,
                ReceivedAt = this.ReceivedAt,
                LastPhysicalCount = this.LastPhysicalCount,
                BlockReason = this.BlockReason,
                RecallReference = this.RecallReference,
                IsUnderRecall = this.IsUnderRecall,
                LastModified = this.LastModified,
                LastModifiedById = this.LastModifiedById,
                LastModifiedBy = this.LastModifiedBy,
                DigitalSignature = this.DigitalSignature,
                SourceIp = this.SourceIp,
                CertificateDocs = this.CertificateDocs,
                InspectionNote = this.InspectionNote
            };
        }

        /// <summary>Returns a human-readable summary of the stock record for debugging/logging.</summary>
        public override string ToString()
        {
            return $"{WarehouseName} – {Part?.Name ?? "Unknown Part"}: {Quantity} (Available: {Available}) Batch:{BatchNumber} SN:{SerialNumber}";
        }
    }
}

