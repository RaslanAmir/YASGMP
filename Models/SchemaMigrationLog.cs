using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>SchemaMigrationLog</b> – Ultra-robust audit log for all database schema migrations.
    /// </summary>
    public class SchemaMigrationLog
    {
        /// <summary>Unique migration log entry (Primary Key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>Timestamp of the migration (UTC).</summary>
        [Required]
        public DateTime MigrationTime { get; set; } = DateTime.UtcNow;

        /// <summary>User ID who performed the migration (FK).</summary>
        [Required]
        public int MigratedById { get; set; }

        /// <summary>Navigacija na korisnika.</summary>
        public User MigratedBy { get; set; } = null!;

        /// <summary>Username (snapshot for trace even if user is deleted).</summary>
        [MaxLength(80)]
        public string Username { get; set; } = string.Empty;

        /// <summary>Database schema version after migration (semantic versioning).</summary>
        [MaxLength(32)]
        public string SchemaVersion { get; set; } = string.Empty;

        /// <summary>Raw SQL/script used for migration.</summary>
        public string MigrationScript { get; set; } = string.Empty;

        /// <summary>Optional rollback script (for disaster recovery).</summary>
        public string RollbackScript { get; set; } = string.Empty;

        /// <summary>Description, reason, or inspector's comment.</summary>
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>IP address/device used for migration (forensic chain).</summary>
        [MaxLength(64)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Device or session info (user agent, machine, etc.).</summary>
        [MaxLength(255)]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>Was migration successful?</summary>
        public bool Success { get; set; } = true;

        /// <summary>Error message if migration failed.</summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>Digital signature/hash for audit trail, chain-of-custody (21 CFR Part 11).</summary>
        [MaxLength(128)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Integrity hash for the entry (blockchain-ready).</summary>
        [MaxLength(128)]
        public string EntryHash { get; set; } = string.Empty;

        /// <summary>Optional attachments (json: logs, screenshots, rollback, PDFs, etc.).</summary>
        public string AttachmentsJson { get; set; } = string.Empty;

        /// <summary>Regulator (if this migration is in response to audit/regulatory order).</summary>
        [MaxLength(40)]
        public string Regulator { get; set; } = string.Empty;

        /// <summary>ML/AI anomaly score (future smart analytics).</summary>
        public double? AnomalyScore { get; set; }

        /// <summary>Free-text note for auditor, inspector, or migration admin.</summary>
        [MaxLength(1000)]
        public string Note { get; set; } = string.Empty;

        /// <summary>Human-friendly ToString for logs/inspector.</summary>
        public override string ToString()
        {
            return $"[SchemaMigrationLog] Version={SchemaVersion}, Success={Success}, By={Username ?? MigratedById.ToString()}, {MigrationTime:u}";
        }
    }
}
