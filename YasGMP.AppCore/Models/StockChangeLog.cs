using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>StockChangeLog</b> – Full audit, forensic, and analytics-ready log for every inventory movement, change, or event.
    /// <para>
    /// ✅ 21 CFR Part 11, HALMED, EU GMP, FDA, ISO 9001/13485 compliant
    /// ✅ Captures every addition, removal, transfer, adjustment, quarantine, recall, correction, and block
    /// ✅ Forensic: user, time, device/IP, signature, incident/ref, attachments
    /// ✅ AI/ML supply chain analytics ready; full ERP/SAP integration; rollback, recall, and traceability
    /// </para>
    /// </summary>
    public class StockChangeLog
    {
        /// <summary>Unique log entry ID (Primary Key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>FK – The part whose stock changed.</summary>
        [Required]
        public int PartId { get; set; }

        /// <summary>Navigacija na Part.</summary>
        public Part Part { get; set; } = null!;

        /// <summary>FK – The warehouse/location where the stock change occurred.</summary>
        [Required]
        public int WarehouseId { get; set; }

        /// <summary>Navigacija na Warehouse.</summary>
        public Warehouse Warehouse { get; set; } = null!;

        /// <summary>Date/time of the stock change (UTC, for audit/analytics).</summary>
        [Required]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>FK – User who made the change (audit, compliance).</summary>
        [Required]
        public int ChangedById { get; set; }

        /// <summary>Navigacija na korisnika.</summary>
        public User ChangedBy { get; set; } = null!;

        /// <summary>Type of change: receipt, issue, transfer, inventory, block, unblock, quarantine, adjustment, recall, correction, audit.</summary>
        [Required, StringLength(32)]
        [Display(Name = "Vrsta promjene")]
        public string ChangeType { get; set; } = "audit";

        /// <summary>Amount (delta) of the change (positive for addition, negative for removal).</summary>
        [Required]
        public int Delta { get; set; }

        /// <summary>New total stock after this change (for snapshot, rapid queries).</summary>
        public int NewStock { get; set; }

        /// <summary>Batch/serial number (if applicable).</summary>
        [StringLength(64)]
        public string BatchNumber { get; set; } = string.Empty;

        /// <summary>Reason or reference (CAPA, deviation, complaint, recall, inspection, block, audit, periodic, correction).</summary>
        [StringLength(512)]
        public string Reason { get; set; } = string.Empty;

        /// <summary>Reference to related incident, deviation, or recall action (if applicable).</summary>
        public string Reference { get; set; } = string.Empty;

        /// <summary>Digital signature or hash of the change (for 21 CFR Part 11, audit, forensic).</summary>
        [StringLength(128)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Device, IP, or host info (forensics, remote access, security audit).</summary>
        [StringLength(64)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Optional: Attachment (CoA, inspection report, scanned doc, photo evidence, etc.).</summary>
        [StringLength(512)]
        public string Attachment { get; set; } = string.Empty;

        /// <summary>Free text comment for auditor, inspector, or supervisor.</summary>
        [StringLength(400)]
        public string Note { get; set; } = string.Empty;

        /// <summary>ML/AI anomaly score (for analytics, risk, fraud, or prediction).</summary>
        public double? AnomalyScore { get; set; }

        /// <summary>True if this log entry is a correction (i.e. error/rollback).</summary>
        public bool IsCorrection { get; set; }

        /// <summary>Soft delete/archive (GDPR, not physical delete).</summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>Returns a summary for audit/inspector log.</summary>
        public override string ToString()
        {
            return $"{ChangeType}: {Delta} [{Part?.Name ?? "Part#" + PartId}] at {Warehouse?.Name ?? "Warehouse#" + WarehouseId} on {ChangedAt:yyyy-MM-dd HH:mm} (Stock: {NewStock})";
        }
    }
}

