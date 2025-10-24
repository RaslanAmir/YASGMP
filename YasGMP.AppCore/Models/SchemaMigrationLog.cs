using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>SchemaMigrationLog</b> - Ultra-robust audit log for all database schema migrations.
    /// </summary>
    [Table("schema_migration_log")]
    public partial class SchemaMigrationLog
    {
        /// <summary>Unique migration log entry (Primary Key).</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Timestamp of the migration (UTC).</summary>
        [Column("migration_time")]
        public DateTime MigrationTime { get; set; } = DateTime.UtcNow;

        /// <summary>User ID who performed the migration (FK).</summary>
        [Column("migrated_by")]
        public int? MigratedById { get; set; }

        /// <summary>Navigacija na korisnika.</summary>
        [ForeignKey(nameof(MigratedById))]
        public User? MigratedBy { get; set; }

        /// <summary>Username (snapshot for trace even if user is deleted).</summary>
        [MaxLength(80)]
        [Column("username")]
        public string Username { get; set; } = string.Empty;

        /// <summary>Database schema version after migration (semantic versioning).</summary>
        [MaxLength(50)]
        [Column("schema_version")]
        public string SchemaVersion { get; set; } = string.Empty;

        /// <summary>Raw SQL/script used for migration.</summary>
        [Column("migration_script", TypeName = "text")]
        public string MigrationScript { get; set; } = string.Empty;

        /// <summary>Optional rollback script (for disaster recovery).</summary>
        [MaxLength(255)]
        [Column("rollback_script")]
        public string RollbackScript { get; set; } = string.Empty;

        /// <summary>Description, reason, or inspector's comment.</summary>
        [Column("description", TypeName = "text")]
        public string Description { get; set; } = string.Empty;

        /// <summary>IP address/device used for migration (forensic chain).</summary>
        [MaxLength(45)]
        [Column("source_ip")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Device or session info (user agent, machine, etc.).</summary>
        [MaxLength(255)]
        [Column("device_info")]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>Was migration successful?</summary>
        [Column("success")]
        public bool Success { get; set; } = true;

        /// <summary>Error message if migration failed.</summary>
        [Column("error_message", TypeName = "text")]
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>Digital signature/hash for audit trail, chain-of-custody (21 CFR Part 11).</summary>
        [MaxLength(128)]
        [Column("digital_signature")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Integrity hash for the entry (blockchain-ready).</summary>
        [MaxLength(128)]
        [Column("entry_hash")]
        public string EntryHash { get; set; } = string.Empty;

        /// <summary>Optional attachments (json: logs, screenshots, rollback, PDFs, etc.).</summary>
        [MaxLength(255)]
        [Column("attachments_json")]
        public string AttachmentsJson { get; set; } = string.Empty;

        /// <summary>Regulator (if this migration is in response to audit/regulatory order).</summary>
        [MaxLength(40)]
        [Column("regulator")]
        public string Regulator { get; set; } = string.Empty;

        /// <summary>ML/AI anomaly score (future smart analytics).</summary>
        [Column("anomaly_score", TypeName = "decimal(10,2)")]
        public double? AnomalyScore { get; set; }

        /// <summary>Free-text note for auditor, inspector, or migration admin.</summary>
        [MaxLength(1000)]
        [Column("note")]
        public string Note { get; set; } = string.Empty;

        /// <summary>Human-friendly ToString for logs/inspector.</summary>
        public override string ToString()
        {
            var actor = !string.IsNullOrWhiteSpace(Username) ? Username : MigratedById?.ToString() ?? "unknown";
            return $"[SchemaMigrationLog] Version={SchemaVersion}, Success={Success}, By={actor}, {MigrationTime:u}";
        }
    }
}

