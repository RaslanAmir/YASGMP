using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>MaintenanceExecutionLog</b> – Ultimate forensic log for preventive/corrective maintenance execution.
    /// <para>
    /// ✅ Tracks every detail: what, when, who, result, doc, root cause, photos, IoT, signature, device, IP  
    /// ✅ Inspector-ready: audit, rollback, comments, traceability  
    /// ✅ Ready for AI/ML analytics, IoT integration, validation, and regulatory inspection
    /// </para>
    /// </summary>
    public class MaintenanceExecutionLog
    {
        /// <summary>
        /// Unique log entry ID (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// FK to the preventive/corrective maintenance plan.
        /// </summary>
        [Required]
        public int PlanId { get; set; }

        /// <summary>
        /// Navigation to plan.
        /// </summary>
        public PreventiveMaintenancePlan? Plan { get; set; }

        /// <summary>
        /// Date and time of plan execution.
        /// </summary>
        [Required]
        public DateTime ExecutedAt { get; set; }

        /// <summary>
        /// User ID who executed the maintenance (FK).
        /// </summary>
        [Required]
        public int ExecutedById { get; set; }

        /// <summary>
        /// Navigation to executing user.
        /// </summary>
        public User? ExecutedBy { get; set; }

        /// <summary>
        /// Maintenance type (planned, unplanned, corrective, emergency, validation).
        /// </summary>
        [StringLength(30)]
        public string? MaintenanceType { get; set; }

        /// <summary>
        /// Result of execution (e.g., "pass", "fail", "error recorded", "pending parts").
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Result { get; set; } = string.Empty;

        /// <summary>
        /// Root cause or problem category if not passed (bonus for CAPA).
        /// </summary>
        [StringLength(255)]
        public string? RootCause { get; set; }

        /// <summary>
        /// List of performed actions (step-by-step, for full traceability).
        /// </summary>
        public List<string> PerformedActions { get; set; } = new();

        /// <summary>
        /// List of used spare parts or materials (for traceability, audit, warehouse tracking).
        /// </summary>
        public List<string> UsedParts { get; set; } = new();

        /// <summary>
        /// List of photo/file/document evidence for this execution.
        /// </summary>
        public List<Attachment> Attachments { get; set; } = new();

        /// <summary>
        /// Additional notes or comments related to execution.
        /// </summary>
        [StringLength(2000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Path to uploaded PDF/Excel or digital checklist file (if present).
        /// </summary>
        public string? ChecklistFile { get; set; }

        /// <summary>
        /// Digital signature for full audit chain (user, hash, e-signature).
        /// </summary>
        [StringLength(128)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Integrity hash of the record (for chain-of-custody, Part 11/blockchain readiness).
        /// </summary>
        [StringLength(128)]
        public string? EntryHash { get; set; }

        /// <summary>
        /// Device info (hostname, OS, browser, IoT, geolocation, MAC, etc.).
        /// </summary>
        [StringLength(128)]
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// IP address or network source for forensic trace.
        /// </summary>
        [StringLength(64)]
        public string? SourceIp { get; set; }

        /// <summary>
        /// ID of user who last modified the record (for audit).
        /// </summary>
        public int LastModifiedById { get; set; }

        /// <summary>
        /// Navigation to last modifying user.
        /// </summary>
        public User? LastModifiedBy { get; set; }

        /// <summary>
        /// Date/time of last modification (for audit).
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Approval status (if review/approval workflow is in place).
        /// </summary>
        public bool IsApproved { get; set; }

        /// <summary>
        /// Date/time of approval (if applicable).
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// User ID of the approver (if workflow applies).
        /// </summary>
        public int? ApprovedById { get; set; }

        /// <summary>
        /// Navigation to approving user.
        /// </summary>
        public User? ApprovedBy { get; set; }

        /// <summary>
        /// Is this execution linked to a CAPA, deviation, or incident?
        /// </summary>
        public int? RelatedCaseId { get; set; }

        /// <summary>
        /// Linked case type (e.g., "CAPA", "Deviation", "Incident").
        /// </summary>
        public string? RelatedCaseType { get; set; }

        /// <summary>
        /// IoT integration: device/sensor/PLC ID that triggered or recorded the execution.
        /// </summary>
        public string? IoTDeviceId { get; set; }

        /// <summary>
        /// Optional: Anomaly score or ML-detected risk (for future analytics).
        /// </summary>
        public double? AnomalyScore { get; set; }

        /// <summary>
        /// Optional: Free text for inspectors, auditors, or validation notes.
        /// </summary>
        [StringLength(1000)]
        public string? InspectorNote { get; set; }

        /// <summary>
        /// Optional: Was this record auto-generated (RPA, API, bot, integration)?
        /// </summary>
        public bool IsAutomated { get; set; }

        /// <summary>
        /// Returns a summary for debugging/logging.
        /// </summary>
        public override string ToString()
        {
            return $"MaintenanceLog#{Id} – Plan:{PlanId}, Date:{ExecutedAt:yyyy-MM-dd}, By:{ExecutedById}, Result:{Result}";
        }
    }
}

