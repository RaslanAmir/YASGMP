using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>StockLevel</b> — Ultimate robust stock-tracking for every part in every warehouse.
    /// GMP/CSV/21 CFR Part 11 ready: alarms, audit, IoT/automation, digital signature, hash chain, ML-anomaly and full inspection trace!
    /// </summary>
    public class StockLevel
    {
        /// <summary>Jedinstveni ID zapisa (PK).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>FK na dio/part (Part).</summary>
        [Required]
        public int PartId { get; set; }

        /// <summary>Navigacija na Part.</summary>
        public Part Part { get; set; } = null!;

        /// <summary>FK na skladište (Warehouse).</summary>
        [Required]
        public int WarehouseId { get; set; }

        /// <summary>Navigacija na Warehouse.</summary>
        public Warehouse Warehouse { get; set; } = null!

            ;

        /// <summary>Aktualna količina (stock quantity).</summary>
        public int Quantity { get; set; }

        /// <summary>Minimalni prag (alarm ako padne ispod, auto-notify, auto-order trigger!).</summary>
        public int MinThreshold { get; set; }

        /// <summary>Maksimalni prag (alarm ako premaši, signaliziraj skladištu ili nabavi!).</summary>
        public int MaxThreshold { get; set; }

        /// <summary>Automatski reorder aktiviran kad padne ispod praga (za IoT, AI replenishment).</summary>
        public bool AutoReorderTriggered { get; set; }

        /// <summary>Broj uzastopnih dana ispod minimuma (za prediktivno održavanje i AI alerting).</summary>
        public int DaysBelowMin { get; set; }

        /// <summary>Alarm status (none, below_min, above_max, pending_approval, locked, reserved, anomaly_detected).</summary>
        [MaxLength(30)]
        public string AlarmStatus { get; set; } = "none";

        /// <summary>ML/AI anomaly score (detekcija sumnjivih kretanja ili krađe, 0.0 = normalno, >0.8 = anomalija).</summary>
        public double? AnomalyScore { get; set; }

        /// <summary>Timestamp zadnje izmjene (audit, forensics, digital trace).</summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>ID korisnika koji je zadnji mijenjao zalihe (audit trail, forensics).</summary>
        public int LastModifiedById { get; set; }

        /// <summary>Navigacija na zadnjeg korisnika koji je mijenjao.</summary>
        public User LastModifiedBy { get; set; } = null!;

        /// <summary>IP adresa uređaja (za puni forenzički trag).</summary>
        [MaxLength(45)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Geo-lokacija izmjene (opcionalno, bonus za inspekciju/krađu/remote work).</summary>
        [MaxLength(100)]
        public string GeoLocation { get; set; } = string.Empty;

        /// <summary>Komentar/promjena uz izmjenu (incident, audit, inspekcija, razlog izmjene).</summary>
        [MaxLength(255)]
        public string Comment { get; set; } = string.Empty;

        /// <summary>Digitalni potpis izmjene (hash, user, device, time).</summary>
        [MaxLength(128)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Hash integriteta zapisa (za Part 11, blockchain/chain-of-custody spremno).</summary>
        [MaxLength(128)]
        public string EntryHash { get; set; } = string.Empty;

        /// <summary>Snapshot stanja prije izmjene (JSON, rollback-ready, null ako nije primijenjeno).</summary>
        public string OldStateSnapshot { get; set; } = string.Empty;

        /// <summary>Snapshot stanja nakon izmjene (JSON, rollback-ready, null ako nije primijenjeno).</summary>
        public string NewStateSnapshot { get; set; } = string.Empty;

        /// <summary>Je li izmjena nastala automatski (IoT, RPA, API, ERP integration) ili korisnički?</summary>
        public bool IsAutomated { get; set; }

        /// <summary>Session/token ID za auditiranje batch promjena, mobilne aplikacije itd.</summary>
        [MaxLength(80)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>Povezan incident, CAPA ili audit zapis za ovu promjenu (bonus za regulatory compliance).</summary>
        public int? RelatedCaseId { get; set; }

        /// <summary>Tip povezanog slučaja.</summary>
        [MaxLength(30)]
        public string RelatedCaseType { get; set; } = string.Empty;

        /// <summary>Human-readable summary for dashboards/logging.</summary>
        public override string ToString()
        {
            return $"Stock: {Quantity} [{Part?.Code ?? PartId.ToString()}] in {Warehouse?.Id ?? WarehouseId} (Min:{MinThreshold}, Max:{MaxThreshold}, Alarm:{AlarmStatus})";
        }
    }
}
