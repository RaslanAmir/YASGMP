using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>StockLevel</b> - Robust stock tracking for every part in every warehouse.
    /// Ready for GMP/CSV/21 CFR Part 11 with alarms, audit, IoT automation, signatures,
    /// hash chain, ML anomaly detection, and full inspection trace.
    /// </summary>
    [Table("stock_levels")]
    public partial class StockLevel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Foreign key to the tracked part.</summary>
        [Required]
        [Column("part_id")]
        public int PartId { get; set; }

        [ForeignKey(nameof(PartId))]
        public virtual Part Part { get; set; } = null!;

        /// <summary>Foreign key to the warehouse.</summary>
        [Required]
        [Column("warehouse_id")]
        public int WarehouseId { get; set; }

        [ForeignKey(nameof(WarehouseId))]
        public virtual Warehouse Warehouse { get; set; } = null!;

        /// <summary>Current stock quantity.</summary>
        [Column("quantity")]
        public int Quantity { get; set; }

        /// <summary>Minimum stock threshold.</summary>
        [Column("min_threshold")]
        public int MinThreshold { get; set; }

        /// <summary>Maximum stock threshold.</summary>
        [Column("max_threshold")]
        public int MaxThreshold { get; set; }

        /// <summary>Automatic reorder flag.</summary>
        [Column("auto_reorder_triggered")]
        public bool AutoReorderTriggered { get; set; }

        /// <summary>Consecutive days the quantity stayed below the minimum.</summary>
        [Column("days_below_min")]
        public int DaysBelowMin { get; set; }

        /// <summary>Alarm state (none, below_min, above_max, pending_approval, locked, reserved, anomaly_detected).</summary>
        [MaxLength(30)]
        [Column("alarm_status")]
        public string AlarmStatus { get; set; } = "none";

        /// <summary>ML/AI anomaly score (0.0 = normal, >0.8 = anomaly).</summary>
        [Column("anomaly_score")]
        public double? AnomalyScore { get; set; }

        /// <summary>Timestamp of the last modification.</summary>
        [Column("last_modified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>User who last modified the record.</summary>
        [Column("last_modified_by_id")]
        public int LastModifiedById { get; set; }

        [ForeignKey(nameof(LastModifiedById))]
        public virtual User LastModifiedBy { get; set; } = null!;

        /// <summary>Forensic IP of the editing device.</summary>
        [MaxLength(45)]
        [Column("source_ip")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Geolocation of the change (optional).</summary>
        [MaxLength(100)]
        [Column("geo_location")]
        public string GeoLocation { get; set; } = string.Empty;

        /// <summary>Comment describing the change.</summary>
        [MaxLength(255)]
        [Column("comment")]
        public string Comment { get; set; } = string.Empty;

        /// <summary>Digital signature of the change.</summary>
        [MaxLength(128)]
        [Column("digital_signature")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Hash for integrity checks.</summary>
        [MaxLength(128)]
        [Column("entry_hash")]
        public string EntryHash { get; set; } = string.Empty;

        /// <summary>Snapshot before the change.</summary>
        [Column("old_state_snapshot")]
        public string OldStateSnapshot { get; set; } = string.Empty;

        /// <summary>Snapshot after the change.</summary>
        [Column("new_state_snapshot")]
        public string NewStateSnapshot { get; set; } = string.Empty;

        /// <summary>Indicates whether the change was automated (IoT, API, RPA).</summary>
        [Column("is_automated")]
        public bool IsAutomated { get; set; }

        /// <summary>Session or batch identifier for the change.</summary>
        [MaxLength(80)]
        [Column("session_id")]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>Linked incident, CAPA, or audit record.</summary>
        [Column("related_case_id")]
        public int? RelatedCaseId { get; set; }

        /// <summary>Type of the linked case.</summary>
        [MaxLength(30)]
        [Column("related_case_type")]
        public string RelatedCaseType { get; set; } = string.Empty;

        /// <summary>Human-readable summary for dashboards/logging.</summary>
        public override string ToString()
        {
            var partLabel = Part?.Code ?? PartId.ToString();
            var warehouseLabel = Warehouse?.Id.ToString() ?? WarehouseId.ToString();
            return $"Stock: {Quantity} [{partLabel}] in {warehouseLabel} (Min:{MinThreshold}, Max:{MaxThreshold}, Alarm:{AlarmStatus})";
        }
    }
}
