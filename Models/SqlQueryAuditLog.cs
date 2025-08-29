using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>SqlQueryAuditLog</b> – Ultra-robust audit log for every SQL query/operation.
    /// Tracks SELECT, INSERT, UPDATE, DELETE, DDL, EXPORT, API, IMPORT, REPORT, and more.
    /// Full GMP, HALMED, FDA, and forensic compliance: who, what, when, where, how, result, rollback, hash chain, digital signature, and AI/ML anomaly support.
    /// </summary>
    public class SqlQueryAuditLog
    {
        /// <summary>Unique log entry ID (PK).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>User ID who performed the query (FK, can be null for automation/batch/API).</summary>
        public int? UserId { get; set; }

        /// <summary>Navigation to user.</summary>
        public User User { get; set; } = null!;

        /// <summary>Username snapshot for robust traceability (if User later deleted).</summary>
        [MaxLength(80)]
        public string Username { get; set; } = string.Empty;

        /// <summary>Date/time of query execution (always UTC for regulatory compliance).</summary>
        public DateTime QueryTime { get; set; } = DateTime.UtcNow;

        /// <summary>Raw or parameterized SQL query text.</summary>
        public string QueryText { get; set; } = string.Empty;

        /// <summary>Query type (SELECT, INSERT, UPDATE, DELETE, DDL, EXPORT, IMPORT, API, REPORT, SCALAR, STORED_PROC, OTHER).</summary>
        [MaxLength(20)]
        public string QueryType { get; set; } = string.Empty;

        /// <summary>Target table/entity name (null if multiple or not applicable).</summary>
        [MaxLength(100)]
        public string TableName { get; set; } = string.Empty;

        /// <summary>List of affected table/entity names (for complex queries, e.g., joins, cascades).</summary>
        public List<string> AffectedTables { get; set; } = new();

        /// <summary>Primary key or record ID(s) affected (for update/delete, rollback/audit).</summary>
        public string RecordIds { get; set; } = string.Empty;

        /// <summary>Number of affected rows (null for SELECT if unknown).</summary>
        public int? AffectedRows { get; set; }

        /// <summary>Success/failure of query execution.</summary>
        public bool Success { get; set; }

        /// <summary>Error message/details if not successful.</summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>Execution duration (ms, for performance/compliance monitoring).</summary>
        public int? DurationMs { get; set; }

        /// <summary>IP address of user, client, or automation/batch process.</summary>
        [MaxLength(45)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Client application (API, desktop, mobile, import tool, etc).</summary>
        [MaxLength(80)]
        public string ClientApp { get; set; } = string.Empty;

        /// <summary>Session or token ID for multi-step or automated processes.</summary>
        [MaxLength(80)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>Device or host information (PC, server, cloud instance, user agent, OS, MAC, geolocation if available).</summary>
        [MaxLength(200)]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>Automation/bot/cron/RPA (true if not direct user action).</summary>
        public bool IsAutomated { get; set; }

        /// <summary>Export type/target if query led to export (e.g., PDF, XLSX, CSV, external system).</summary>
        [MaxLength(30)]
        public string ExportType { get; set; } = string.Empty;

        /// <summary>JSON snapshot of data before change (for full rollback/audit, null for SELECT).</summary>
        public string OldDataSnapshot { get; set; } = string.Empty;

        /// <summary>JSON snapshot of data after change (for full rollback/audit, null for SELECT).</summary>
        public string NewDataSnapshot { get; set; } = string.Empty;

        /// <summary>Context: JSON or plain text – all request context, headers, API tokens, etc.</summary>
        public string ContextDetails { get; set; } = string.Empty;

        /// <summary>Hash (SHA-256 or better) for full entry integrity (GMP/CSV/Part 11 compliance, blockchain ready).</summary>
        [MaxLength(128)]
        public string EntryHash { get; set; } = string.Empty;

        /// <summary>Digital signature of query action (for chain-of-custody, advanced compliance).</summary>
        [MaxLength(128)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Hash chain/previous entry hash for full audit/forensic chain.</summary>
        [MaxLength(128)]
        public string ChainHash { get; set; } = string.Empty;

        /// <summary>Severity/criticality: info, warning, error, audit, security, export, GDPR, ML-anomaly.</summary>
        [MaxLength(40)]
        public string Severity { get; set; } = string.Empty;

        /// <summary>ML/AI anomaly score for future smart audit/analytics (null = not scored).</summary>
        public double? AnomalyScore { get; set; }

        /// <summary>Free note/comment (user/admin, e.g. why, external incident ref, investigation link).</summary>
        [MaxLength(1000)]
        public string Note { get; set; } = string.Empty;

        /// <summary>Forensics: geo-location (city/country/coords if available, for compliance).</summary>
        [MaxLength(120)]
        public string GeoLocation { get; set; } = string.Empty;

        /// <summary>Related regulatory/inspection case or investigation ID.</summary>
        public int? RelatedCaseId { get; set; }

        /// <summary>Human-readable summary for dashboards/logging.</summary>
        public override string ToString()
        {
            return $"[{QueryType}] {TableName ?? "(n/a)"} by {Username ?? ("User#" + UserId)} @{QueryTime:u} | Success: {Success}";
        }
    }
}
