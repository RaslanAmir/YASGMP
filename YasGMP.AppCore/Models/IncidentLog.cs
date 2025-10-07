using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// Represents the Incident Log.
    /// </summary>
    [Table("incident_log")]
    public partial class IncidentLog
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the detected at.
        /// </summary>
        [Column("detected_at")]
        public DateTime? DetectedAt { get; set; }

        /// <summary>
        /// Gets or sets the reported by id.
        /// </summary>
        [Column("reported_by_id")]
        public int? ReportedById { get; set; }

        /// <summary>
        /// Gets or sets the reported by.
        /// </summary>
        [ForeignKey(nameof(ReportedById))]
        public User? ReportedBy { get; set; }

        /// <summary>
        /// Gets or sets the severity.
        /// </summary>
        [Column("severity")]
        [MaxLength(16)]
        public string? Severity { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [Column("title")]
        [MaxLength(255)]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the resolved.
        /// </summary>
        [Column("resolved")]
        public bool? Resolved { get; set; }

        /// <summary>
        /// Gets or sets the resolved at.
        /// </summary>
        [Column("resolved_at")]
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// Gets or sets the resolved by id.
        /// </summary>
        [Column("resolved_by_id")]
        public int? ResolvedById { get; set; }

        /// <summary>
        /// Gets or sets the resolved by.
        /// </summary>
        [ForeignKey(nameof(ResolvedById))]
        public User? ResolvedBy { get; set; }

        /// <summary>
        /// Gets or sets the actions taken.
        /// </summary>
        [Column("actions_taken")]
        public string? ActionsTaken { get; set; }

        /// <summary>
        /// Gets or sets the follow up.
        /// </summary>
        [Column("follow_up")]
        public string? FollowUp { get; set; }

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        [Column("note")]
        public string? Note { get; set; }

        /// <summary>
        /// Gets or sets the source ip.
        /// </summary>
        [Column("source_ip")]
        [MaxLength(45)]
        public string? SourceIp { get; set; }

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
        /// Gets or sets the severity id.
        /// </summary>
        [Column("severity_id")]
        public int? SeverityId { get; set; }

        /// <summary>
        /// Gets or sets the digital signature.
        /// </summary>
        [Column("digital_signature")]
        [MaxLength(255)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Gets or sets the root cause.
        /// </summary>
        [Column("root_cause")]
        [MaxLength(255)]
        public string? RootCause { get; set; }

        /// <summary>
        /// Gets or sets the capa case id.
        /// </summary>
        [Column("capa_case_id")]
        public int? CapaCaseId { get; set; }

        /// <summary>
        /// Gets or sets the capa case.
        /// </summary>
        [ForeignKey(nameof(CapaCaseId))]
        public CapaCase? CapaCase { get; set; }

        /// <summary>
        /// Gets or sets the capa case label.
        /// </summary>
        [Column("capa_case")]
        [MaxLength(255)]
        public string? CapaCaseLabel { get; set; }

        /// <summary>
        /// Gets or sets the attachments raw.
        /// </summary>
        [Column("attachments")]
        [MaxLength(255)]
        public string? AttachmentsRaw { get; set; }

        /// <summary>
        /// Gets or sets the last modified.
        /// </summary>
        [Column("last_modified")]
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// Gets or sets the last modified by id.
        /// </summary>
        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        /// <summary>
        /// Gets or sets the last modified by.
        /// </summary>
        [ForeignKey(nameof(LastModifiedById))]
        public User? LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the last modified by name.
        /// </summary>
        [Column("last_modified_by")]
        [MaxLength(255)]
        public string? LastModifiedByName { get; set; }
    }
}
