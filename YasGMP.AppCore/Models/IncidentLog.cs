using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    [Table("incident_log")]
    public partial class IncidentLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("detected_at")]
        public DateTime? DetectedAt { get; set; }

        [Column("reported_by_id")]
        public int? ReportedById { get; set; }

        [ForeignKey(nameof(ReportedById))]
        public User? ReportedBy { get; set; }

        [Column("severity")]
        [MaxLength(16)]
        public string? Severity { get; set; }

        [Column("title")]
        [MaxLength(255)]
        public string? Title { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("resolved")]
        public bool? Resolved { get; set; }

        [Column("resolved_at")]
        public DateTime? ResolvedAt { get; set; }

        [Column("resolved_by_id")]
        public int? ResolvedById { get; set; }

        [ForeignKey(nameof(ResolvedById))]
        public User? ResolvedBy { get; set; }

        [Column("actions_taken")]
        public string? ActionsTaken { get; set; }

        [Column("follow_up")]
        public string? FollowUp { get; set; }

        [Column("note")]
        public string? Note { get; set; }

        [Column("source_ip")]
        [MaxLength(45)]
        public string? SourceIp { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("severity_id")]
        public int? SeverityId { get; set; }

        [Column("digital_signature")]
        [MaxLength(255)]
        public string? DigitalSignature { get; set; }

        [Column("root_cause")]
        [MaxLength(255)]
        public string? RootCause { get; set; }

        [Column("capa_case_id")]
        public int? CapaCaseId { get; set; }

        [ForeignKey(nameof(CapaCaseId))]
        public CapaCase? CapaCase { get; set; }

        [Column("capa_case")]
        [MaxLength(255)]
        public string? CapaCaseLabel { get; set; }

        [Column("attachments")]
        [MaxLength(255)]
        public string? AttachmentsRaw { get; set; }

        [Column("last_modified")]
        public DateTime? LastModified { get; set; }

        [Column("last_modified_by_id")]
        public int? LastModifiedById { get; set; }

        [ForeignKey(nameof(LastModifiedById))]
        public User? LastModifiedBy { get; set; }

        [Column("last_modified_by")]
        [MaxLength(255)]
        public string? LastModifiedByName { get; set; }
    }
}

