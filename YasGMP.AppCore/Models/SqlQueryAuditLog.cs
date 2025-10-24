using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>SqlQueryAuditLog</b> - Audit trail for raw SQL/query execution, capturing actor, context, impact, and forensic metadata.
    /// </summary>
    [Table("sql_query_audit_log")]
    public partial class SqlQueryAuditLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Column("username")]
        [StringLength(80)]
        public string Username { get; set; } = string.Empty;

        [Column("query_time")]
        public DateTime QueryTime { get; set; } = DateTime.UtcNow;

        [Column("query_text", TypeName = "text")]
        public string QueryText { get; set; } = string.Empty;

        [Column("query_type")]
        [StringLength(20)]
        public string QueryType { get; set; } = string.Empty;

        [Column("table_name")]
        [StringLength(100)]
        public string TableName { get; set; } = string.Empty;

        [NotMapped]
        public List<string> AffectedTables { get; set; } = new();

        [Column("affected_tables", TypeName = "text")]
        public string? AffectedTablesSerialized
        {
            get => AffectedTables.Count == 0 ? null : string.Join(",", AffectedTables);
            set
            {
                AffectedTables.Clear();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    AffectedTables.AddRange(parts);
                }
            }
        }

        [Column("record_ids", TypeName = "text")]
        public string RecordIds { get; set; } = string.Empty;

        [Column("affected_rows")]
        public int? AffectedRows { get; set; }

        [Column("success")]
        public bool Success { get; set; }

        [Column("error_message", TypeName = "text")]
        public string ErrorMessage { get; set; } = string.Empty;

        [Column("duration_ms")]
        public int? DurationMs { get; set; }

        [Column("source_ip")]
        [StringLength(45)]
        public string SourceIp { get; set; } = string.Empty;

        [Column("client_app")]
        [StringLength(80)]
        public string ClientApp { get; set; } = string.Empty;

        [Column("session_id")]
        [StringLength(80)]
        public string SessionId { get; set; } = string.Empty;

        [Column("device_info")]
        [StringLength(255)]
        public string DeviceInfo { get; set; } = string.Empty;

        [Column("is_automated")]
        public bool IsAutomated { get; set; }

        [Column("export_type")]
        [StringLength(30)]
        public string ExportType { get; set; } = string.Empty;

        [Column("old_data_snapshot", TypeName = "text")]
        public string OldDataSnapshot { get; set; } = string.Empty;

        [Column("new_data_snapshot", TypeName = "text")]
        public string NewDataSnapshot { get; set; } = string.Empty;

        [Column("context_details", TypeName = "text")]
        public string ContextDetails { get; set; } = string.Empty;

        [Column("entry_hash")]
        [StringLength(128)]
        public string EntryHash { get; set; } = string.Empty;

        [Column("digital_signature")]
        [StringLength(128)]
        public string DigitalSignature { get; set; } = string.Empty;

        [Column("chain_hash")]
        [StringLength(128)]
        public string ChainHash { get; set; } = string.Empty;

        [Column("severity")]
        [StringLength(40)]
        public string Severity { get; set; } = string.Empty;

        [Column("anomaly_score")]
        public double? AnomalyScore { get; set; }

        [Column("note", TypeName = "text")]
        public string Note { get; set; } = string.Empty;

        [Column("geo_location")]
        [StringLength(120)]
        public string GeoLocation { get; set; } = string.Empty;

        [Column("related_case_id")]
        public int? RelatedCaseId { get; set; }

        /// <summary>Human-readable summary.</summary>
        public override string ToString()
        {
            var actor = !string.IsNullOrWhiteSpace(Username) ? Username : UserId?.ToString() ?? "unknown";
            return $"[{QueryType}] {TableName ?? "(n/a)"} by {actor} @{QueryTime:u} | Success: {Success}";
        }
    }
}

