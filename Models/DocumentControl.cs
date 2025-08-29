using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>DocumentControl</b> – Ultra-robust, audit-ready document/SOP master record for GMP/CMMS/QMS systems.
    /// <para>
    /// ✅ Supports versioning, workflow, approvals, digital signatures, users/roles, and full traceability.<br/>
    /// ✅ Designed for 21 CFR Part 11, EU GMP Annex 11, HALMED, FDA, and ISO 9001/13485.<br/>
    /// ✅ Forensic logging: every change, review, approval, and digital event can be tracked and reconstructed.<br/>
    /// ✅ Extensible for electronic signatures, change control, lifecycle, and user-specific views.
    /// </para>
    /// </summary>
    [Table("documentcontrol")]
    public class DocumentControl
    {
        /// <summary>
        /// Unique document ID (Primary Key).
        /// </summary>
        [Key]
        [Column("id")]
        [Display(Name = "ID dokumenta")]
        public int Id { get; set; }

        /// <summary>
        /// Internal document code (e.g., SOP-001-A).
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("code")]
        [Display(Name = "Šifra dokumenta")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Document title/name (UI uses <see cref="Title"/>; legacy code may use <see cref="Name"/>).
        /// </summary>
        [Required]
        [StringLength(255)]
        [Column("title")]
        [Display(Name = "Naziv dokumenta")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Document type/category (SOP, Policy, WI, Protocol, Report, etc.). UI uses <see cref="DocumentType"/>.
        /// </summary>
        [StringLength(100)]
        [Column("document_type")]
        [Display(Name = "Tip dokumenta")]
        public string? DocumentType { get; set; }

        /// <summary>
        /// Document revision label (e.g., "1.0", "2.1b").
        /// </summary>
        [StringLength(40)]
        [Column("revision")]
        [Display(Name = "Revizija")]
        public string? Revision { get; set; } = "1.0";

        /// <summary>
        /// Document status (draft, active, archived, obsolete…).
        /// </summary>
        [StringLength(40)]
        [Column("status")]
        [Display(Name = "Status")]
        public string Status { get; set; } = "draft";

        /// <summary>
        /// Path or URL to the file (PDF, DOCX, etc.).
        /// </summary>
        [StringLength(255)]
        [Column("file_path")]
        [Display(Name = "Putanja do datoteke")]
        public string? FilePath { get; set; }

        /// <summary>
        /// Document description, summary, or notes.
        /// </summary>
        [Column("description")]
        [Display(Name = "Opis / Napomene")]
        public string? Description { get; set; }

        /// <summary>
        /// Optional: JSON/CSV string with attachment paths (kept as string for schema tolerance).
        /// </summary>
        [Column("attachments")]
        [Display(Name = "Prilozi (JSON/CSV)")]
        public string? Attachments { get; set; }

        /// <summary>
        /// Optional: JSON of revision history (schema tolerant).
        /// </summary>
        [Column("revision_history")]
        [Display(Name = "Povijest revizija (JSON)")]
        public string? RevisionHistory { get; set; }

        /// <summary>
        /// Optional: JSON of status transitions (schema tolerant).
        /// </summary>
        [Column("status_history")]
        [Display(Name = "Povijest statusa (JSON)")]
        public string? StatusHistory { get; set; }

        /// <summary>
        /// Optional: JSON/CSV of linked change controls (schema tolerant).
        /// </summary>
        [Column("linked_change_controls")]
        [Display(Name = "Povezane promjene (JSON/CSV)")]
        public string? LinkedChangeControls { get; set; }

        /// <summary>
        /// Optional expiration/review date for the document.
        /// </summary>
        [Column("expiry_date")]
        [Display(Name = "Datum isteka / revizije")]
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Device info captured when last modified (browser/OS/app).
        /// </summary>
        [StringLength(256)]
        [Column("device_info")]
        [Display(Name = "Uređaj / Klijent info")]
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// Session identifier captured when last modified (traceability).
        /// </summary>
        [StringLength(64)]
        [Column("session_id")]
        [Display(Name = "ID sesije")]
        public string? SessionId { get; set; }

        /// <summary>
        /// IP address captured when last modified (forensics).
        /// </summary>
        [StringLength(64)]
        [Column("ip_address")]
        [Display(Name = "IP adresa")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// User ID of the creator (FK to users).
        /// </summary>
        [Column("created_by_id")]
        [Display(Name = "Kreirao korisnik")]
        public int? CreatedById { get; set; }

        /// <summary>
        /// User who created the document.
        /// </summary>
        [ForeignKey(nameof(CreatedById))]
        public virtual User? CreatedBy { get; set; }

        /// <summary>
        /// When was the document created (UTC recommended).
        /// </summary>
        [Column("created_at")]
        [Display(Name = "Datum kreiranja")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User ID of approver (FK to users).
        /// </summary>
        [Column("approved_by_id")]
        [Display(Name = "Odobrio korisnik")]
        public int? ApprovedById { get; set; }

        /// <summary>
        /// User who approved the document.
        /// </summary>
        [ForeignKey(nameof(ApprovedById))]
        public virtual User? ApprovedBy { get; set; }

        /// <summary>
        /// When was the document approved.
        /// </summary>
        [Column("approved_at")]
        [Display(Name = "Datum odobravanja")]
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// User ID of the assignee/owner (FK to users).
        /// </summary>
        [Column("assigned_to")]
        [Display(Name = "Dodijeljeno korisniku (ID)")]
        public int? AssignedToUserId { get; set; }

        /// <summary>
        /// Convenience display name for assigned user (schema tolerant).
        /// </summary>
        [NotMapped]
        [Display(Name = "Dodijeljeno korisniku (ime)")]
        public string? AssignedTo { get; set; }

        /// <summary>
        /// Digital signature or certificate hash for the document.
        /// </summary>
        [StringLength(255)]
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// True if the document is locked from editing (e.g., during review/approval).
        /// </summary>
        [Column("is_locked")]
        [Display(Name = "Zaključano za uređivanje")]
        public bool IsLocked { get; set; } = false;

        /// <summary>
        /// Optional field: related module/table for context linking (e.g., 'SOP', 'CAPA', 'Deviation').
        /// </summary>
        [StringLength(50)]
        [Column("related_module")]
        [Display(Name = "Modul / entitet")]
        public string? RelatedModule { get; set; }

        /// <summary>
        /// Optional: related record ID (for linking this document to a specific CAPA, Deviation, etc.).
        /// </summary>
        [Column("related_record_id")]
        [Display(Name = "ID povezanog entiteta")]
        public int? RelatedRecordId { get; set; }

        // ------------------------- Compatibility shims (non-breaking) -------------------------

        /// <summary>
        /// Legacy alias for <see cref="Title"/> (kept for back-compat with older code).
        /// </summary>
        [NotMapped]
        [Display(AutoGenerateField = false)]
        public string Name
        {
            get => Title;
            set => Title = value;
        }

        /// <summary>
        /// Legacy alias for <see cref="DocumentType"/> (kept for back-compat).
        /// </summary>
        [NotMapped]
        [Display(AutoGenerateField = false)]
        public string? Type
        {
            get => DocumentType;
            set => DocumentType = value;
        }

        /// <summary>
        /// Legacy alias for <see cref="Revision"/> (kept for back-compat with code that called this a "Version").
        /// </summary>
        [NotMapped]
        [Display(AutoGenerateField = false)]
        public string? Version
        {
            get => Revision;
            set => Revision = value;
        }

        // ------------------------- Rich, optional navigation/collections -------------------------

        /// <summary>
        /// List of all document versions (for full traceability).
        /// </summary>
        [NotMapped]
        [Display(Name = "Sve verzije dokumenta")]
        public List<DocumentVersion> Versions { get; set; } = new();

        /// <summary>
        /// List of users with read access (bonus: field-level/document-level security).
        /// </summary>
        [NotMapped]
        [Display(Name = "Korisnici s pristupom")]
        public List<User> Readers { get; set; } = new();

        /// <summary>
        /// List of audit events for this document (who, when, what) – when materialized in memory.
        /// </summary>
        [NotMapped]
        [Display(Name = "Evidencija aktivnosti")]
        public List<DocumentAuditEvent> AuditEvents { get; set; } = new();
    }

    /// <summary>
    /// <b>DocumentVersion</b> – Linked version record for full document traceability and compliance.
    /// </summary>
    [Table("document_versions")]
    public class DocumentVersion
    {
        /// <summary>Primary key.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>FK to <see cref="DocumentControl"/>.</summary>
        [Column("documentcontrol_id")]
        [Display(Name = "ID dokumenta")]
        public int DocumentControlId { get; set; }

        /// <summary>Navigation to the parent document.</summary>
        [ForeignKey(nameof(DocumentControlId))]
        public DocumentControl? Document { get; set; }

        /// <summary>Revision label, e.g. "1.0", "2.0".</summary>
        [StringLength(40)]
        [Column("revision")]
        [Display(Name = "Revizija")]
        public string? Revision { get; set; }

        /// <summary>File path for this version.</summary>
        [StringLength(255)]
        [Column("file_path")]
        [Display(Name = "Putanja do datoteke")]
        public string? FilePath { get; set; }

        /// <summary>User who created this version.</summary>
        [Column("created_by_id")]
        [Display(Name = "Kreirao korisnik")]
        public int? CreatedById { get; set; }

        /// <summary>Navigation to the creator.</summary>
        [ForeignKey(nameof(CreatedById))]
        public User? CreatedBy { get; set; }

        /// <summary>Creation timestamp (UTC recommended).</summary>
        [Column("created_at")]
        [Display(Name = "Datum kreiranja")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Status of this version (e.g. active, archived).</summary>
        [Column("status")]
        [Display(Name = "Status verzije")]
        public string? Status { get; set; }

        /// <summary>Optional note for this version.</summary>
        [Column("note")]
        [Display(Name = "Napomena")]
        public string? Note { get; set; }
    }

    /// <summary>
    /// <b>DocumentAuditEvent</b> – Single audit log entry for document event (review, approval, change, etc.)
    /// </summary>
    [Table("document_audit_events")]
    public class DocumentAuditEvent
    {
        /// <summary>Primary key.</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>FK to <see cref="DocumentControl"/>.</summary>
        [Column("documentcontrol_id")]
        [Display(Name = "ID dokumenta")]
        public int DocumentControlId { get; set; }

        /// <summary>Navigation to parent document.</summary>
        [ForeignKey(nameof(DocumentControlId))]
        public DocumentControl? Document { get; set; }

        /// <summary>Event timestamp.</summary>
        [Column("timestamp")]
        [Display(Name = "Vrijeme događaja")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>Type of action (created, updated, approved, signed, locked...).</summary>
        [Column("action")]
        [Display(Name = "Akcija")]
        public string? Action { get; set; }

        /// <summary>User performing the action.</summary>
        [Column("user_id")]
        [Display(Name = "Korisnik")]
        public int? UserId { get; set; }

        /// <summary>Navigation to user.</summary>
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>Free text description/details.</summary>
        [Column("description")]
        [Display(Name = "Opis događaja")]
        public string? Description { get; set; }
    }
}
