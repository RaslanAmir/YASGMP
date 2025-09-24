using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>ExternalContractor</b> – Super ultra mega robust GMP/CMMS master record for every external contractor, service provider, or vendor.
    /// <para>
    /// ✅ Tracks all companies, individuals, and teams that perform maintenance, calibration, validation, audits, or services.<br/>
    /// ✅ Stores full regulatory compliance, certificates, blacklist status, intervention history, scoring, audit, digital signatures.<br/>
    /// ✅ Extensible for attachments, contract docs, insurance, roles, risk audit, external/internal mapping, bonus options.
    /// </para>
    /// </summary>
    [Table("external_contractors")]
    public partial class ExternalContractor
    {
        /// <summary>
        /// Primary key of the contractor (database identity).
        /// </summary>
        [Key]
        [Display(Name = "ID izvođača")]
        public int Id { get; set; }

        /// <summary>
        /// Display name of the contractor (person or company). Required.
        /// </summary>
        [Required]
        [StringLength(150)]
        [Display(Name = "Naziv / Ime izvođača")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Legal company name (if applicable).
        /// </summary>
        [StringLength(200)]
        [Display(Name = "Pravno ime tvrtke")]
        public string? CompanyName { get; set; }

        /// <summary>
        /// Tax/registration number (OIB or equivalent).
        /// </summary>
        [StringLength(32)]
        [Display(Name = "OIB / Registracija")]
        public string? RegistrationNumber { get; set; }

        /// <summary>
        /// Contractor type (e.g., calibration, validation, HVAC, IT, etc.).
        /// </summary>
        [StringLength(48)]
        [Display(Name = "Tip izvođača")]
        public string? Type { get; set; }

        /// <summary>
        /// Main contact person name for this contractor.
        /// </summary>
        [StringLength(100)]
        [Display(Name = "Kontakt osoba")]
        public string? ContactPerson { get; set; }

        /// <summary>
        /// Contact e-mail.
        /// </summary>
        [StringLength(120)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        /// <summary>
        /// Contact phone number.
        /// </summary>
        [StringLength(40)]
        [Display(Name = "Telefon")]
        public string? Phone { get; set; }

        /// <summary>
        /// Postal address.
        /// </summary>
        [StringLength(180)]
        [Display(Name = "Adresa")]
        public string? Address { get; set; }

        /// <summary>
        /// Certificates and licenses summary (free text).
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Certifikati / Licence")]
        public string? Certificates { get; set; }

        /// <summary>
        /// Blacklist flag (true when the contractor is blocked from use).
        /// </summary>
        [Display(Name = "Blokiran / Crna lista")]
        public bool IsBlacklisted { get; set; } = false;

        /// <summary>
        /// Reason for blacklist (if any).
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Razlog blokade")]
        public string? BlacklistReason { get; set; }

        /// <summary>
        /// Risk score or rating (numeric).
        /// </summary>
        [Display(Name = "Ocjena / Rizik")]
        public decimal? RiskScore { get; set; }

        /// <summary>
        /// Optional FK to a supplier record.
        /// </summary>
        [Display(Name = "Dobavljač (FK)")]
        public int? SupplierId { get; set; }

        /// <summary>
        /// Navigation to linked supplier (optional).
        /// </summary>
        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier? Supplier { get; set; }

        /// <summary>
        /// List of contractor interventions.
        /// </summary>
        [Display(Name = "Intervencije")]
        [NotMapped]
        public List<ContractorIntervention> Interventions { get; set; } = new List<ContractorIntervention>();

        /// <summary>
        /// Attached documents (CVs, certificates, contracts...).
        /// </summary>
        [Display(Name = "Dokumenti / Prilozi")]
        [NotMapped]
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();

        /// <summary>
        /// Digital signature (integrity/hash).
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Digitalni potpis")]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Forensic/audit log entries.
        /// </summary>
        [Display(Name = "Audit log")]
        [NotMapped]
        public List<ContractorAuditLog> AuditLogs { get; set; } = new List<ContractorAuditLog>();

        /// <summary>
        /// Free-form notes about the contractor.
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Napomene")]
        public string? Note { get; set; }

        #region Compatibility & UI/Schema-Tolerant Aliases

        /// <summary>
        /// **Compatibility alias** for historical code and DB helpers that reference a <c>Contact</c> property.
        /// This forwards to <see cref="ContactPerson"/> for full backward/forward compatibility.
        /// Database service methods that set or read <c>Contact</c> via reflection will transparently use <see cref="ContactPerson"/>.
        /// </summary>
        [NotMapped]
        [Display(Name = "Kontakt (alias)")]
        public string? Contact
        {
            get => ContactPerson;
            set => ContactPerson = value;
        }

        /// <summary>
        /// Optional service type descriptive label (used by exports/UI filters; may map to DB column <c>service_type</c>).
        /// Kept non-mapped because raw ADO in <c>DatabaseService</c> handles persistence.
        /// </summary>
        [NotMapped]
        [Display(Name = "Vrsta usluge")]
        public string? ServiceType { get; set; }

        /// <summary>
        /// Optional document/file path reference used by import/export utilities (DB column <c>doc_file</c>).
        /// </summary>
        [NotMapped]
        [Display(Name = "Dokument (datoteka)")]
        public string? DocFile { get; set; }

        /// <summary>
        /// Optional UI-only contractor status (e.g., active/blocked/pending).
        /// </summary>
        [NotMapped]
        [Display(Name = "Status (UI)")]
        public string? Status { get; set; }

        /// <summary>
        /// Optional UI-only rating bucket (A–D).
        /// </summary>
        [NotMapped]
        [Display(Name = "Rejting (UI)")]
        public string? Rating { get; set; }

        #endregion
    }

    /// <summary>
    /// <b>ContractorAuditLog</b> – Forensic/audit log for all changes, interventions, and compliance events related to an external contractor.
    /// </summary>
    public class ContractorAuditLog
    {
        /// <summary>Primary key of the audit record.</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>FK to the contractor.</summary>
        [Display(Name = "ID izvođača")]
        public int ExternalContractorId { get; set; }

        /// <summary>Navigation to contractor (optional; EF will populate when included).</summary>
        [ForeignKey(nameof(ExternalContractorId))]
        public virtual ExternalContractor? Contractor { get; set; }

        /// <summary>Time of the audited action.</summary>
        [Display(Name = "Vrijeme događaja")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>Action type (CREATE/UPDATE/DELETE/ROLLBACK/...)</summary>
        [Display(Name = "Akcija")]
        public string? Action { get; set; }

        /// <summary>User id who performed the action.</summary>
        [Display(Name = "Korisnik")]
        public int? UserId { get; set; }

        /// <summary>Navigation to user who performed the action (optional).</summary>
        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        /// <summary>Free-text description.</summary>
        [StringLength(255)]
        [Display(Name = "Opis")]
        public string? Description { get; set; }
    }
}
