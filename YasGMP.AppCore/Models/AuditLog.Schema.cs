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
        /// <summary>
        /// Gets or sets the related module.
        /// </summary>
        [Column("related_module")]
        [StringLength(100)]
        public string? RelatedModule { get; set; }

        /// <summary>
        /// Gets or sets the processed.
        /// </summary>
        [Column("processed")]
        public bool Processed { get; set; }

        /// <summary>
        /// Gets or sets the created at.
        /// </summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the updated at.
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp utc.
        /// </summary>
        [Column("ts_utc")]
        public DateTime? TimestampUtc { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        [Column("username")]
        [StringLength(128)]
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [Column("description", TypeName = "text")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the entry hash.
        /// </summary>
        [Column("entry_hash")]
        [StringLength(256)]
        public string? EntryHash { get; set; }

        /// <summary>
        /// Gets or sets the mac address.
        /// </summary>
        [Column("mac_address")]
        [StringLength(64)]
        public string? MacAddress { get; set; }

        /// <summary>
        /// Gets or sets the geo location.
        /// </summary>
        [Column("geo_location")]
        [StringLength(128)]
        public string? GeoLocation { get; set; }

        /// <summary>
        /// Gets or sets the regulator.
        /// </summary>
        [Column("regulator")]
        [StringLength(64)]
        public string? Regulator { get; set; }

        /// <summary>
        /// Gets or sets the related case id.
        /// </summary>
        [Column("related_case_id")]
        public int? RelatedCaseId { get; set; }

        /// <summary>
        /// Gets or sets the related case type.
        /// </summary>
        [Column("related_case_type")]
        [StringLength(64)]
        public string? RelatedCaseType { get; set; }

        /// <summary>
        /// Gets or sets the anomaly score.
        /// </summary>
        [Column("anomaly_score")]
        public double? AnomalyScore { get; set; }

        /// <summary>
        /// Gets or sets the session id.
        /// </summary>
        [Column("session_id")]
        [StringLength(100)]
        public string? SessionId { get; set; }

        /// <summary>
        /// Gets or sets the legacy user label.
        /// </summary>
        [Column("user")]
        [StringLength(255)]
        public string? LegacyUserLabel { get; set; }
    }
}
