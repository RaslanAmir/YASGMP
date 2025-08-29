using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>DataMigrationLog</b> – Full GMP/CSV/21 CFR Part 11/Annex 11-compliant log for every data migration (import, export, update, delete).
    /// <para>
    /// ⭐ SUPER-MEGA-ULTRA ROBUST: Tracks all actors, tables, record counts, timestamps, results, files, forensics, digital signatures, chain/version, rollback notes, and extensibility for future compliance.
    /// </para>
    /// </summary>
    public class DataMigrationLog
    {
        /// <summary>
        /// Unique record ID (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Type of migration: import, export, update, delete.
        /// </summary>
        [Required]
        [StringLength(32)]
        public string MigrationType { get; set; } = string.Empty;

        /// <summary>
        /// Name of the table affected by this migration.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Number of records migrated in this operation.
        /// </summary>
        public int RecordCount { get; set; }

        /// <summary>
        /// ID of user who performed the migration.
        /// </summary>
        [Required]
        public int MigratedById { get; set; }

        /// <summary>
        /// Navigation to user who performed the migration.
        /// </summary>
        [ForeignKey(nameof(MigratedById))]
        public User? MigratedBy { get; set; }

        /// <summary>
        /// Date and time of migration.
        /// </summary>
        public DateTime MigrationTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Path to log/report file.
        /// </summary>
        [StringLength(512)]
        public string? LogFilePath { get; set; }

        /// <summary>
        /// Additional comments or notes.
        /// </summary>
        [StringLength(2000)]
        public string? Note { get; set; }

        /// <summary>
        /// Was migration successful? (bonus: for audit, alerting, analytics)
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Digital signature or hash for integrity/trust (21 CFR Part 11, audit).
        /// </summary>
        [StringLength(256)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Forensic: IP address/device from which migration was performed.
        /// </summary>
        [StringLength(128)]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Full serialization of before/after states (JSON, optional, for rollback/forensic audit).
        /// </summary>
        public string? BeforeSnapshot { get; set; }

        /// <summary>
        /// After-migration snapshot (JSON, optional, for rollback/forensic audit).
        /// </summary>
        public string? AfterSnapshot { get; set; }

        /// <summary>
        /// Bonus: Chain/version for rollback, event sourcing, traceability.
        /// </summary>
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Is this log archived/soft deleted (GDPR, not physically deleted).
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
}
