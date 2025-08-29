using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>LogEntry</b> — THE most robust, AI/ML-ready, forensic, and future-proof audit log for all user and system actions.
    /// Complies with GMP, 21 CFR Part 11, ISO, ITIL, Banking, pharma, and future digital/AI regulations.
    /// Designed for digital inspectors, data science, compliance, blockchain, and legal evidence.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Unique log entry ID (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// UTC timestamp of the action (regulatory and forensic compliance).
        /// </summary>
        [Required]
        [Display(Name = "Datum/vrijeme (UTC)")]
        public DateTime DateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User ID of the action performer (nullable for system/automation events).
        /// </summary>
        [Display(Name = "Korisnik")]
        public int? UserId { get; set; }

        /// <summary>
        /// Navigation to user (nullable for system events).
        /// </summary>
        public User? User { get; set; }

        /// <summary>
        /// Username at the time of the action (snapshot for traceability if user is deleted/changed).
        /// </summary>
        [Display(Name = "Korisničko ime")]
        [StringLength(100)]
        public string? Username { get; set; }

        /// <summary>
        /// Role of the user at the time of action (for forensic and regulatory review).
        /// </summary>
        [Display(Name = "Uloga korisnika")]
        public string? UserRole { get; set; }

        /// <summary>
        /// Action type (CREATE, UPDATE, DELETE, LOGIN, LOGOUT, EXPORT, PRINT, SIGN, etc.).
        /// </summary>
        [Required]
        [StringLength(100)]
        [Display(Name = "Akcija")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Entity/table name affected by the action (for traceability and cross-module auditing).
        /// </summary>
        [Display(Name = "Entitet/Tablica")]
        public string? Entity { get; set; }

        /// <summary>
        /// Record ID in the affected table (supports composite keys, GUIDs, etc).
        /// </summary>
        [Display(Name = "ID zapisa")]
        public string? RecordId { get; set; }

        /// <summary>
        /// Field name (if the action was field-level, for advanced rollback/audit granularity).
        /// </summary>
        [Display(Name = "Polje")]
        public string? FieldName { get; set; }

        /// <summary>
        /// Old value (before the change, for audit rollback and forensics).
        /// </summary>
        [Display(Name = "Stara vrijednost")]
        public string? OldValue { get; set; }

        /// <summary>
        /// New value (after the change, for audit rollback and forensics).
        /// </summary>
        [Display(Name = "Nova vrijednost")]
        public string? NewValue { get; set; }

        /// <summary>
        /// Detailed context or description (JSON diff, workflow, error, comment, etc).
        /// </summary>
        [Display(Name = "Detalji")]
        public string? Details { get; set; }

        /// <summary>
        /// Approval status, if this action is part of an approval chain/workflow.
        /// </summary>
        [Display(Name = "Odobreno")]
        public bool? IsApproved { get; set; }

        /// <summary>
        /// Digital signature, e-sign, or cryptographic hash (for 21 CFR Part 11 and chain-of-custody).
        /// </summary>
        [Display(Name = "Digitalni potpis")]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Hash of the entire entry for integrity, blockchain, and tamper-proof audits.
        /// </summary>
        [Display(Name = "Hash integriteta")]
        public string? EntryHash { get; set; }

        /// <summary>
        /// Source of access (IP, hostname, device ID, MAC, geolocation, etc.).
        /// </summary>
        [Display(Name = "Izvor (IP/uređaj)")]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Device info (browser, OS, agent, geolocation, etc).
        /// </summary>
        [Display(Name = "Uređaj/Agent")]
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// Session or token ID for this action (for journey, fraud, or forensic analytics).
        /// </summary>
        [Display(Name = "Session ID")]
        public string? SessionId { get; set; }

        /// <summary>
        /// Severity or risk level (info, warning, error, critical, audit, security, forensic, anomaly).
        /// </summary>
        [Display(Name = "Razina/severity")]
        public string? Severity { get; set; }

        /// <summary>
        /// Linked case ID (CAPA, incident, investigation, etc.).
        /// </summary>
        public int? RelatedCaseId { get; set; }

        /// <summary>
        /// Linked case type (CAPA/incident/etc).
        /// </summary>
        [Display(Name = "Povezani slučaj (CAPA/incident)")]
        public string? RelatedCaseType { get; set; }

        /// <summary>
        /// Regulatory context (HALMED, FDA, EMA, SOX, HIPAA, etc).
        /// </summary>
        [Display(Name = "Regulatorno tijelo")]
        public string? Regulator { get; set; }

        /// <summary>
        /// Geolocation (city/country or coordinates, if available).
        /// </summary>
        [Display(Name = "Geolokacija")]
        public string? GeoLocation { get; set; }

        /// <summary>
        /// ML/AI anomaly score (for future AI/ML audit and predictive compliance).
        /// </summary>
        public double? AnomalyScore { get; set; }

        /// <summary>
        /// Was this action performed by automation/bot/RPA? (for future-proof audits).
        /// </summary>
        public bool IsAutomated { get; set; }

        /// <summary>
        /// Inspector, auditor, or user comment.
        /// </summary>
        [Display(Name = "Napomena")]
        public string? Note { get; set; }

        /// <summary>
        /// Linked attachment, document, or certificate path (if relevant).
        /// </summary>
        [Display(Name = "Dokument/prilog")]
        public string? LinkedAttachmentPath { get; set; }

        /// <summary>
        /// Previous log entry ID (for audit chain navigation).
        /// </summary>
        public int? PreviousLogEntryId { get; set; }

        /// <summary>
        /// Next log entry ID (for audit chain navigation).
        /// </summary>
        public int? NextLogEntryId { get; set; }

        /// <summary>
        /// Reserved for future extensibility (schema upgrades, custom audit fields, etc).
        /// </summary>
        [Display(Name = "Ekstenzija")]
        public string? ExtensionJson { get; set; }
    }
}
