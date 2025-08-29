using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using YasGMP.Models;

namespace YasGMP.Models.DTO
{
    /// <summary>
    /// <b>AuditEntryDto</b> – Ultra-robust Data Transfer Object for displaying and processing audit logs, forensics, and rollback entries.
    /// <para>
    /// ✅ Used for rollback preview, forensic investigation, audit dashboards, and regulatory review.<br/>
    /// ✅ Captures full before/after values, user, action, entity, timestamp, device, IP, session, attachments.<br/>
    /// ✅ Extensible for digital signature, change group, approval, comments, escalation, ML/AI, fraud, and regulatory fields.
    /// </para>
    /// </summary>
    public class AuditEntryDto
    {
        /// <summary>Unique log/audit ID (if available).</summary>
        [Display(Name = "ID zapisa")]
        public int? Id { get; set; }

        /// <summary>Name of the table or entity being audited (synonym: EntityName).</summary>
        [Display(Name = "Entitet / Tablica")]
        public string? Entity { get; set; }

        /// <summary>Entity name for display or convenience. (Alias for <see cref="Entity"/>)</summary>
        [Display(Name = "Naziv entiteta")]
        public string? EntityName => Entity;

        /// <summary>Primary key of the affected record (if available).</summary>
        [Display(Name = "ID entiteta")]
        public string? EntityId { get; set; }

        /// <summary>Action performed (insert, update, delete, approve, rollback, etc.).</summary>
        [Display(Name = "Akcija")]
        public string? Action { get; set; }

        /// <summary>UTC timestamp of the audit event (synonym: ActionAt).</summary>
        [Display(Name = "Vrijeme")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>When the action happened (for ViewModel compatibility; maps to <see cref="Timestamp"/>).</summary>
        [Display(Name = "Vrijeme akcije")]
        public DateTime ActionAt => Timestamp;

        /// <summary>User ID who performed the action (if available).</summary>
        [Display(Name = "Korisnik")]
        public int? UserId { get; set; }

        /// <summary>Username or display name.</summary>
        [Display(Name = "Ime korisnika")]
        public string? Username { get; set; }

        /// <summary>Full name of the user (display helper).</summary>
        [Display(Name = "Ime i prezime")]
        public string? UserFullName => Username;

        /// <summary>IP address of the client.</summary>
        [Display(Name = "IP adresa")]
        public string? IpAddress { get; set; }

        /// <summary>Device info of the client (browser, OS, hostname).</summary>
        [Display(Name = "Uređaj")]
        public string? DeviceInfo { get; set; }

        /// <summary>Session ID for the audit event.</summary>
        [Display(Name = "Sesija")]
        public string? SessionId { get; set; }

        /// <summary>Changed field (legacy flat audits).</summary>
        [Display(Name = "Naziv polja")]
        public string? FieldName { get; set; }

        /// <summary>Old value (before the change).</summary>
        [Display(Name = "Vrijednost prije")]
        public string? OldValue { get; set; }

        /// <summary>New value (after the change).</summary>
        [Display(Name = "Vrijednost poslije")]
        public string? NewValue { get; set; }

        /// <summary>List of changed fields (name/value before/after).</summary>
        [Display(Name = "Promijenjena polja")]
        public List<AuditFieldChange> ChangedFields { get; set; } = new List<AuditFieldChange>();

        /// <summary>Optional: related table/entity for foreign key relationships.</summary>
        [Display(Name = "Povezana tablica / entitet")]
        public string? RelatedEntity { get; set; }

        /// <summary>Optional: related record ID.</summary>
        [Display(Name = "ID povezanog entiteta")]
        public string? RelatedEntityId { get; set; }

        /// <summary>Regulatory or audit status (approved, pending, escalated, rejected).</summary>
        [Display(Name = "Status / Regulatorno")]
        public string? Status { get; set; }

        /// <summary>Digital signature for the audit entry.</summary>
        [Display(Name = "Digitalni potpis")]
        public string? DigitalSignature { get; set; }

        /// <summary>Hash of digital signature for advanced forensics/rollback (hex-encoded).</summary>
        [Display(Name = "Hash potpisa")]
        public string? SignatureHash { get; set; }

        /// <summary>List of attached files or evidence.</summary>
        [Display(Name = "Prilozi")]
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();

        /// <summary>Reviewer or approver user ID (if applicable).</summary>
        [Display(Name = "Odobrio korisnik")]
        public int? ApprovedById { get; set; }

        /// <summary>Approval/validation timestamp (if applicable).</summary>
        [Display(Name = "Vrijeme odobrenja")]
        public DateTime? ApprovedAt { get; set; }

        /// <summary>Regulatory notes, escalation, comments.</summary>
        [Display(Name = "Napomene")]
        public string? Note { get; set; }

        /// <summary>Extra: AI/ML anomaly/fraud score, audit risk flag, etc.</summary>
        [Display(Name = "AI/ML analiza")]
        public double? AiScore { get; set; }

        /// <summary>Extra: chain/group ID for grouping audit/rollback chains.</summary>
        [Display(Name = "Chain ID")]
        public string? ChainId { get; set; }
    }

    /// <summary>
    /// Represents a single field value before and after a change, for audit/rollback.
    /// </summary>
    public class AuditFieldChange
    {
        /// <summary>The name of the field that was changed.</summary>
        [Display(Name = "Naziv polja")]
        public string? FieldName { get; set; }

        /// <summary>The value of the field before the change.</summary>
        [Display(Name = "Vrijednost prije")]
        public string? OldValue { get; set; }

        /// <summary>The value of the field after the change.</summary>
        [Display(Name = "Vrijednost poslije")]
        public string? NewValue { get; set; }
    }
}
