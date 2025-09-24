#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace YasGMP.Models
{
    /// <summary>
    /// Ultra-robust GMP audit entry for a single deviation, aligned to the <c>deviation_audit</c> SQL table.
    /// Maps one row per action (CREATE/UPDATE/APPROVE/REJECT/ASSIGN/ESCALATE/EXPORT/ROLLBACK/etc.), including forensics.
    /// </summary>
    /// <remarks>
    /// SQL origin: table <c>deviation_audit</c> in YASGMP.sql. Columns are mirrored 1:1 where possible.  
    /// Extra enum wrappers are provided for readability/type-safety while retaining string persistence when needed.
    /// </remarks>
    [Table("deviation_audit")]
    public sealed partial class DeviationAudit
    {
        /// <summary>Primary key.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>FK to the related deviation.</summary>
        [Column("deviation_id")]
        [Required]
        public int DeviationId { get; set; }

        /// <summary>FK to the user who performed the action.</summary>
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>
        /// Action name persisted in DB (<c>VARCHAR(40)</c> in SQL).
        /// Keep as string to exactly match SQL and avoid migrations; use <see cref="ActionType"/> for enum access.
        /// </summary>
        [Column("action")]
        [MaxLength(40)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Strongly-typed view of <see cref="Action"/>. Not mapped; reading/writing will convert to/from string.
        /// </summary>
        [NotMapped]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public Enums.DeviationActionType ActionType
        {
            get => EnumTryParseOrDefault(Action, Enums.DeviationActionType.UPDATE);
            set => Action = value.ToString();
        }

        /// <summary>Free-text details, forensic description.</summary>
        [Column("details")]
        public string? Details { get; set; }

        /// <summary>UTC timestamp of the action (<c>DEFAULT CURRENT_TIMESTAMP</c>).</summary>
        [Column("changed_at")]
        public DateTime? ChangedAt { get; set; }

        /// <summary>Originating device/browser/OS string.</summary>
        [Column("device_info")]
        public string? DeviceInfo { get; set; }

        /// <summary>Source IP address (forensics).</summary>
        [Column("source_ip")]
        [MaxLength(64)]
        public string? SourceIp { get; set; }

        /// <summary>Session token for full traceability.</summary>
        [Column("session_id")]
        [MaxLength(128)]
        public string? SessionId { get; set; }

        /// <summary>Digital signature hash/string captured at the moment of action.</summary>
        [Column("digital_signature")]
        [MaxLength(255)]
        public string? DigitalSignature { get; set; }

        /// <summary>Regulatory/QA posture for this entry (compliant, pending_review, etc.).</summary>
        [Column("regulatory_status")]
        [MaxLength(20)]
        public string? RegulatoryStatusRaw { get; set; } = Models.Enums.RegulatoryStatus.compliant.ToString();

        /// <summary>Typed view of <see cref="RegulatoryStatusRaw"/>.</summary>
        [NotMapped]
        public Models.Enums.RegulatoryStatus RegulatoryStatus
        {
            get => EnumTryParseOrDefault(RegulatoryStatusRaw, Models.Enums.RegulatoryStatus.compliant);
            set => RegulatoryStatusRaw = value.ToString();
        }

        /// <summary>Optional ML anomaly score (0.0000–1.0000).</summary>
        [Column("ai_anomaly_score", TypeName = "decimal(5,4)")]
        public decimal? AiAnomalyScore { get; set; }

        /// <summary>Whether the entry was validated/checked by QA.</summary>
        [Column("validated")]
        public bool? Validated { get; set; }

        /// <summary>FK to <c>system_event_log</c> (cross-link).</summary>
        [Column("audit_trail_id")]
        public int? AuditTrailId { get; set; }

        /// <summary>Reviewer/auditor comment.</summary>
        [Column("comment")]
        public string? Comment { get; set; }

        /// <summary>Previous value (for field-change events).</summary>
        [Column("old_value")]
        public string? OldValue { get; set; }

        /// <summary>New value (for field-change events).</summary>
        [Column("new_value")]
        public string? NewValue { get; set; }

        /// <summary>Type of signature captured (pin/password/certificate/biometric/none).</summary>
        [Column("signature_type")]
        [MaxLength(20)]
        public string? SignatureTypeRaw { get; set; } = Models.Enums.SignatureType.none.ToString();

        /// <summary>Typed view of <see cref="SignatureTypeRaw"/>.</summary>
        [NotMapped]
        public Models.Enums.SignatureType SignatureType
        {
            get => EnumTryParseOrDefault(SignatureTypeRaw, Models.Enums.SignatureType.none);
            set => SignatureTypeRaw = value.ToString();
        }

        /// <summary>Custom signature method identifier (e.g., FIDO2, NFC token).</summary>
        [Column("signature_method")]
        public string? SignatureMethod { get; set; }

        /// <summary>True if the signature is verified/valid.</summary>
        [Column("signature_valid")]
        public bool? SignatureValid { get; set; }

        /// <summary>Export/print/email status of this audit row.</summary>
        [Column("export_status")]
        [MaxLength(20)]
        public string? ExportStatusRaw { get; set; } = Models.Enums.ExportStatus.none.ToString();

        /// <summary>Typed view of <see cref="ExportStatusRaw"/>.</summary>
        [NotMapped]
        public Models.Enums.ExportStatus ExportStatus
        {
            get => EnumTryParseOrDefault(ExportStatusRaw, Models.Enums.ExportStatus.none);
            set => ExportStatusRaw = value.ToString();
        }

        /// <summary>When the row was exported/printed/emailed (if applicable).</summary>
        [Column("export_time")]
        public DateTime? ExportTime { get; set; }

        /// <summary>User who exported this row (if applicable).</summary>
        [Column("exported_by")]
        public int? ExportedBy { get; set; }

        /// <summary>Flag that this row was restored from a snapshot/rollback.</summary>
        [Column("restored_from_snapshot")]
        public bool? RestoredFromSnapshot { get; set; }

        /// <summary>Reference token of the snapshot/restore.</summary>
        [Column("restoration_reference")]
        [MaxLength(128)]
        public string? RestorationReference { get; set; }

        /// <summary>Approval workflow state.</summary>
        [Column("approval_status")]
        [MaxLength(20)]
        public string? ApprovalStatusRaw { get; set; } = Models.Enums.ApprovalStatus.none.ToString();

        /// <summary>Typed view of <see cref="ApprovalStatusRaw"/>.</summary>
        [NotMapped]
        public Models.Enums.ApprovalStatus ApprovalStatus
        {
            get => EnumTryParseOrDefault(ApprovalStatusRaw, Models.Enums.ApprovalStatus.none);
            set => ApprovalStatusRaw = value.ToString();
        }

        /// <summary>When the entry was approved/rejected (if applicable).</summary>
        [Column("approval_time")]
        public DateTime? ApprovalTime { get; set; }

        /// <summary>Approver user id (if applicable).</summary>
        [Column("approved_by")]
        public int? ApprovedBy { get; set; }

        /// <summary>Soft delete marker.</summary>
        [Column("deleted")]
        public bool? Deleted { get; set; }

        /// <summary>Deletion time (if soft-deleted).</summary>
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        /// <summary>User who soft-deleted the row.</summary>
        [Column("deleted_by")]
        public int? DeletedBy { get; set; }

        /// <summary>Row creation time (redundant, helpful for tools).</summary>
        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>Last update time (auto-updated by MySQL).</summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Optional path to an evidence file attached to this audit.</summary>
        [Column("related_file")]
        public string? RelatedFile { get; set; }

        /// <summary>Optional path to an evidence photo attached to this audit.</summary>
        [Column("related_photo")]
        public string? RelatedPhoto { get; set; }

        /// <summary>FK to an IoT event entry for digital traceability.</summary>
        [Column("iot_event_id")]
        public int? IotEventId { get; set; }

        // ------------------------------- Helpers -------------------------------

        private static TEnum EnumTryParseOrDefault<TEnum>(string? value, TEnum @default) where TEnum : struct, Enum
            => Enum.TryParse(value, ignoreCase: true, out TEnum parsed) ? parsed : @default;
    }
}

namespace YasGMP.Models.Enums
{
    /// <summary>
    /// Regulatory status wrapper to match SQL <c>regulatory_status</c>.
    /// </summary>
    public enum RegulatoryStatus
    {
        compliant,
        pending_review,
        invalid,
        forensic,
        security
    }

    /// <summary>
    /// Signature types as per SQL <c>signature_type</c>.
    /// </summary>
    public enum SignatureType
    {
        none,
        pin,
        password,
        certificate,
        biometric
    }

    /// <summary>
    /// Export/print/email states, mapped to SQL <c>export_status</c>.
    /// </summary>
    public enum ExportStatus
    {
        none,
        pdf,
        csv,
        xml,
        emailed,
        printed
    }

    /// <summary>
    /// Approval workflow states as per SQL <c>approval_status</c>.
    /// </summary>
    public enum ApprovalStatus
    {
        none,
        pending,
        approved,
        rejected,
        escalated
    }
}
