using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>BackupHistory</b> – Forensic log of every backup and restore operation in the system.
    /// Every backup, restore, verify, and audit: who, when, type, hash, file, outcome, restore, note!
    /// Absolutely critical for GMP/CSV compliance, HALMED, and regulatory audit/inspection.
    /// </summary>
    public class BackupHistory
    {
        /// <summary>
        /// Unique record ID (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// UTC timestamp of the backup operation.
        /// </summary>
        [Required]
        public DateTime BackupTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Backup type (full, incremental, differential, snapshot, etc.).
        /// </summary>
        [Required]
        [MaxLength(32)]
        public string BackupType { get; set; } = string.Empty;

        /// <summary>
        /// File path or cloud identifier for the backup.
        /// </summary>
        [Required]
        [MaxLength(512)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// User ID who performed the backup (FK to User).
        /// </summary>
        public int? PerformedBy { get; set; }

        /// <summary>
        /// Navigation property for backup performer (optional).
        /// </summary>
        [ForeignKey("PerformedBy")]
        public virtual User? PerformedByUser { get; set; }

        /// <summary>
        /// Digital hash of the backup file (SHA-256, etc. – for integrity).
        /// </summary>
        [MaxLength(128)]
        public string FileHash { get; set; } = string.Empty;

        /// <summary>
        /// Was this backup successfully verified (integrity check, audit).
        /// </summary>
        public bool Verified { get; set; } = false;

        /// <summary>
        /// (Optional) ID of the backup from which this was restored (FK to BackupHistory.Id).
        /// </summary>
        public int? RestoreOfBackupId { get; set; }

        /// <summary>
        /// Navigation property for the parent backup if this is a restore (optional).
        /// </summary>
        [ForeignKey("RestoreOfBackupId")]
        public virtual BackupHistory? RestoreOfBackup { get; set; }

        /// <summary>
        /// UTC timestamp of the restore operation (nullable).
        /// </summary>
        public DateTime? RestoreTime { get; set; }

        /// <summary>
        /// User ID who performed the restore (FK to User).
        /// </summary>
        public int? RestoreBy { get; set; }

        /// <summary>
        /// Navigation property for restore performer (optional).
        /// </summary>
        [ForeignKey("RestoreBy")]
        public virtual User? RestoreByUser { get; set; }

        /// <summary>
        /// Freeform note for the restore operation (reason, incident, rollback info).
        /// </summary>
        [MaxLength(512)]
        public string RestoreNote { get; set; } = string.Empty;

        /// <summary>
        /// (Optional) digital signature/hash for this backup/restore event (for audit).
        /// </summary>
        [MaxLength(256)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// IP/device info for backup operation (forensic inspection).
        /// </summary>
        [MaxLength(128)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Is this backup/restore record soft-deleted/archived (GDPR, audit cleanup).
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Version (for rollback, event sourcing, traceability).
        /// </summary>
        public int ChangeVersion { get; set; } = 1;
    }
}

