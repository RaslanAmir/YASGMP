using System;
using YasGMP.Models.Enums;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>PpmAudit</b> — Ultra-robust, GMP-compliant immutable audit log entry for Preventive Maintenance Plans (PPM).
    /// <para>Tracks every action, user, digital signature, forensic details, AI scoring, regulatory/export/rollback, and IoT links.</para>
    /// </summary>
    public class PpmAudit
    {
        /// <summary>Primary Key (autoincremented in DB)</summary>
        public int Id { get; set; }

        /// <summary>Foreign Key — ID of the related PPM plan.</summary>
        public int PpmPlanId { get; set; }

        /// <summary>Foreign Key — ID of the user performing the action.</summary>
        public int UserId { get; set; }

        /// <summary>Enum — Action performed (CREATE, UPDATE, EXECUTE, DELETE, EXPORT, etc.)</summary>
        public PpmActionType Action { get; set; }

        /// <summary>Free-text details (what was changed, why, regulator note, etc.)</summary>
        public string? Details { get; set; }

        /// <summary>When the action occurred (UTC).</summary>
        public DateTime ChangedAt { get; set; }

        /// <summary>Full digital signature hash (for 21 CFR Part 11/Annex 11 compliance).</summary>
        public string? DigitalSignature { get; set; }

        /// <summary>Optional: Device info for forensics (browser, OS, mobile, etc.)</summary>
        public string? DeviceInfo { get; set; }

        /// <summary>Optional: Source IP address for full traceability.</summary>
        public string? SourceIp { get; set; }

        /// <summary>Optional: Session token (for session-level forensic linking).</summary>
        public string? SessionId { get; set; }

        /// <summary>Optional: AI/ML anomaly score (0.0-1.0) for audit analytics.</summary>
        public decimal? AiAnomalyScore { get; set; }

        /// <summary>Optional: Regulatory status of audit entry (compliant, pending_review, etc.).</summary>
        public string? RegulatoryStatus { get; set; }

        /// <summary>Optional: If the audit entry was validated/checked (true/false).</summary>
        public bool? Validated { get; set; }

        /// <summary>Optional: Linked evidence/file (e.g., PDF, image).</summary>
        public string? RelatedFile { get; set; }

        /// <summary>Optional: Linked photo/image (mobile proof, etc.)</summary>
        public string? RelatedPhoto { get; set; }

        /// <summary>Optional: Linked IoT event ID (for full traceability of machine data).</summary>
        public int? IotEventId { get; set; }

        /// <summary>Optional: Approval status (none, pending, approved, rejected, etc.)</summary>
        public string? ApprovalStatus { get; set; }

        /// <summary>Optional: Approval time (if applicable).</summary>
        public DateTime? ApprovalTime { get; set; }

        /// <summary>Optional: Approved/rejected by user ID.</summary>
        public int? ApprovedBy { get; set; }

        /// <summary>Optional: If this audit entry is deleted (for forensic purposes only).</summary>
        public bool? Deleted { get; set; }

        /// <summary>Optional: When deleted (if deleted).</summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>Optional: Who deleted (user ID).</summary>
        public int? DeletedBy { get; set; }

        /// <summary>Optional: Restoration reference for rollbacks.</summary>
        public string? RestorationReference { get; set; }

        /// <summary>Optional: Export status (none, pdf, csv, xml, etc.)</summary>
        public string? ExportStatus { get; set; }

        /// <summary>Optional: Export time (if exported).</summary>
        public DateTime? ExportTime { get; set; }

        /// <summary>Optional: Who exported this record.</summary>
        public int? ExportedBy { get; set; }

        /// <summary>Optional: Additional comment/free text (reviewer/auditor note).</summary>
        public string? Comment { get; set; }
    }
}
