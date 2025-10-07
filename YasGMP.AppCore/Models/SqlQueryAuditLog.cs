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
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        [Column("user_id")]
        public int? UserId { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        [Column("username")]
        [StringLength(80)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the query time.
        /// </summary>
        [Column("query_time")]
        public DateTime QueryTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the query text.
        /// </summary>
        [Column("query_text", TypeName = "text")]
        public string QueryText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the query type.
        /// </summary>
        [Column("query_type")]
        [StringLength(20)]
        public string QueryType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        [Column("table_name")]
        [StringLength(100)]
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the affected tables.
        /// </summary>
        [NotMapped]
        public List<string> AffectedTables { get; set; } = new();

        /// <summary>
        /// Represents the affected tables serialized value.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the record ids.
        /// </summary>
        [Column("record_ids", TypeName = "text")]
        public string RecordIds { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the affected rows.
        /// </summary>
        [Column("affected_rows")]
        public int? AffectedRows { get; set; }

        /// <summary>
        /// Gets or sets the success.
        /// </summary>
        [Column("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        [Column("error_message", TypeName = "text")]
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the duration ms.
        /// </summary>
        [Column("duration_ms")]
        public int? DurationMs { get; set; }

        /// <summary>
        /// Gets or sets the source ip.
        /// </summary>
        [Column("source_ip")]
        [StringLength(45)]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the client app.
        /// </summary>
        [Column("client_app")]
        [StringLength(80)]
        public string ClientApp { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the session id.
        /// </summary>
        [Column("session_id")]
        [StringLength(80)]
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the device info.
        /// </summary>
        [Column("device_info")]
        [StringLength(255)]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the is automated.
        /// </summary>
        [Column("is_automated")]
        public bool IsAutomated { get; set; }

        /// <summary>
        /// Gets or sets the export type.
        /// </summary>
        [Column("export_type")]
        [StringLength(30)]
        public string ExportType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the old data snapshot.
        /// </summary>
        [Column("old_data_snapshot", TypeName = "text")]
        public string OldDataSnapshot { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new data snapshot.
        /// </summary>
        [Column("new_data_snapshot", TypeName = "text")]
        public string NewDataSnapshot { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the context details.
        /// </summary>
        [Column("context_details", TypeName = "text")]
        public string ContextDetails { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the entry hash.
        /// </summary>
        [Column("entry_hash")]
        [StringLength(128)]
        public string EntryHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the digital signature.
        /// </summary>
        [Column("digital_signature")]
        [StringLength(128)]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chain hash.
        /// </summary>
        [Column("chain_hash")]
        [StringLength(128)]
        public string ChainHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the severity.
        /// </summary>
        [Column("severity")]
        [StringLength(40)]
        public string Severity { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the anomaly score.
        /// </summary>
        [Column("anomaly_score")]
        public double? AnomalyScore { get; set; }

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        [Column("note", TypeName = "text")]
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the geo location.
        /// </summary>
        [Column("geo_location")]
        [StringLength(120)]
        public string GeoLocation { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the related case id.
        /// </summary>
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
