using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Warehouse</b> — Ultra-robust model for all warehouse/storage locations, with full GMP/CSV/audit traceability.
    /// Tracks responsibility, access, audit, compliance, digital signatures, IoT readiness, legal requirements, forensics, and ML/AI support.
    /// </summary>
    public class Warehouse
    {
        /// <summary>Unique warehouse ID (Primary Key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Warehouse name (e.g., "Main Warehouse", "Spare Parts").</summary>
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Warehouse location (hall, floor, zone, address, room...).</summary>
        [Required, MaxLength(255)]
        public string Location { get; set; } = string.Empty;

        /// <summary>FK to User.Id (responsible person for the warehouse).</summary>
        public int ResponsibleId { get; set; }
        public User? Responsible { get; set; }

        /// <summary>(Optional) QR code or link to QR code of the warehouse.</summary>
        [MaxLength(255)]
        public string QrCode { get; set; } = string.Empty;

        /// <summary>(Optional) Note (e.g., storage conditions, special requirements, audits).</summary>
        [MaxLength(500)]
        public string Note { get; set; } = string.Empty;

        /// <summary>Creation timestamp (audit, UTC).</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>ID of the user who created the record.</summary>
        public int? CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        /// <summary>Last modification timestamp (audit, UTC).</summary>
        public DateTime? LastModified { get; set; }

        /// <summary>ID of the user who last modified the data.</summary>
        public int? LastModifiedById { get; set; }
        public User? LastModifiedBy { get; set; }

        /// <summary>Digital signature (hash, name, or pin) — for GMP/CSV compliance.</summary>
        [MaxLength(128)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>(Bonus) Warehouse status (active, archived, closed, in review…).</summary>
        [MaxLength(30)]
        public string Status { get; set; } = string.Empty;

        /// <summary>(Bonus) IoT/Automation ID (for warehouse sensors, monitoring).</summary>
        [MaxLength(64)]
        public string IoTDeviceId { get; set; } = string.Empty;

        /// <summary>(Bonus) Climate regime (ambient, cold, cleanroom, etc.).</summary>
        [MaxLength(60)]
        public string ClimateMode { get; set; } = string.Empty;

        /// <summary>(Bonus) Linked compliance certificates or documents (GMP cert, photos, docs).</summary>
        public List<string> ComplianceDocs { get; set; } = new();

        /// <summary>(Bonus) Hash of entire record (for forensic chain-of-custody, blockchain, 21 CFR).</summary>
        [MaxLength(128)]
        public string EntryHash { get; set; } = string.Empty;

        /// <summary>(Bonus) IP address/device of last modification (forensics).</summary>
        [MaxLength(45)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>(Bonus) Is this warehouse currently qualified (compliance, audit state)?</summary>
        public bool IsQualified { get; set; } = false;

        /// <summary>(Bonus) Date of last qualification/audit.</summary>
        public DateTime? LastQualified { get; set; }

        /// <summary>(Bonus) Session ID (for full audit chain, e.g., for mobile edits).</summary>
        [MaxLength(80)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>(Bonus) ML/AI anomaly score (future risk/compliance/AI audits).</summary>
        public double? AnomalyScore { get; set; }

        /// <summary>(Bonus) GDPR/soft delete/archive flag (never physically deleted).</summary>
        public bool IsDeleted { get; set; } = false;
    }
}
