using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Schema-level augmentation for <see cref="AuditLog"/> bridging all columns surfaced by the underlying MySQL view (audit_log).
    /// </summary>
    public partial class AuditLog
    {
        [Column("related_module")]
        [StringLength(100)]
        public string? RelatedModule { get; set; }

        [Column("processed")]
        public bool Processed { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("ts_utc")]
        public DateTime? TimestampUtc { get; set; }

        [Column("username")]
        [StringLength(128)]
        public string? Username { get; set; }

        [Column("description", TypeName = "text")]
        public string? Description { get; set; }

        [Column("entry_hash")]
        [StringLength(256)]
        public string? EntryHash { get; set; }

        [Column("mac_address")]
        [StringLength(64)]
        public string? MacAddress { get; set; }

        [Column("geo_location")]
        [StringLength(128)]
        public string? GeoLocation { get; set; }

        [Column("regulator")]
        [StringLength(64)]
        public string? Regulator { get; set; }

        [Column("related_case_id")]
        public int? RelatedCaseId { get; set; }

        [Column("related_case_type")]
        [StringLength(64)]
        public string? RelatedCaseType { get; set; }

        [Column("anomaly_score")]
        public double? AnomalyScore { get; set; }

        [Column("session_id")]
        [StringLength(100)]
        public string? SessionId { get; set; }

        [Column("user")]
        [StringLength(255)]
        public string? LegacyUserLabel { get; set; }
    }
}
