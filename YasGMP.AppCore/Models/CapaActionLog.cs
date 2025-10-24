using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>CapaActionLog</b> – Tracks every action on a CAPA case for GMP/CSV/21 CFR Part 11 compliance.
    /// Forensic, auditable: who, what, when, action type, status, findings, digital signature, device, and event chain.
    /// </summary>
    [Table("capa_action_log")]
    public class CapaActionLog
    {
        /// <summary>
        /// Unique action log ID (Primary Key).
        /// </summary>
        [Key]
        [Column("id")]
        [Display(Name = "ID akcije")]
        public int Id { get; set; }

        /// <summary>
        /// FK – The CAPA case this log relates to.
        /// </summary>
        [Required]
        [Column("capa_case_id")]
        [Display(Name = "CAPA slučaj")]
        public int CapaCaseId { get; set; }

        /// <summary>
        /// Navigation to the related CAPA case (required, EF-populated).
        /// </summary>
        [ForeignKey(nameof(CapaCaseId))]
        public virtual CapaCase CapaCase { get; set; } = null!;

        /// <summary>
        /// Action type: "korektivna", "preventivna", etc.
        /// </summary>
        [Required]
        [Column("action_type")]
        [MaxLength(32)]
        [Display(Name = "Tip akcije")]
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// Description of the action taken.
        /// </summary>
        [Column("description")]
        [MaxLength(1000)]
        [Display(Name = "Opis akcije")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// FK – User who performed the action (optional).
        /// </summary>
        [Column("performed_by_id")]
        [Display(Name = "Izvršio korisnik")]
        public int? PerformedById { get; set; }

        /// <summary>
        /// Navigation to the user who performed the action (optional).
        /// </summary>
        [ForeignKey(nameof(PerformedById))]
        public virtual User? PerformedBy { get; set; }

        /// <summary>
        /// Timestamp of when the action was performed.
        /// </summary>
        [Required]
        [Column("performed_at")]
        [Display(Name = "Vrijeme akcije")]
        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Status after the action: "planirano", "u_tijeku", "zavrseno", "otkazano"
        /// </summary>
        [Required]
        [Column("status")]
        [MaxLength(32)]
        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Note for findings, decisions, or audit details.
        /// </summary>
        [Column("note")]
        [MaxLength(1000)]
        [Display(Name = "Napomena")]
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Digital signature/hash for non-repudiation and audit.
        /// </summary>
        [Column("digital_signature")]
        [MaxLength(128)]
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Source IP/device for forensic traceability.
        /// </summary>
        [Column("source_ip")]
        [MaxLength(45)]
        [Display(Name = "IP adresa")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Change version (for audit/event sourcing).
        /// </summary>
        [Column("change_version")]
        [Display(Name = "Verzija")]
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Soft delete/archive flag (GDPR/traceability).
        /// </summary>
        [Column("is_deleted")]
        [Display(Name = "Arhivirano")]
        public bool IsDeleted { get; set; } = false;
    }
}

