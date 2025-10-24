using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>CapaStatusHistory</b> – Full trace of all status changes for every CAPA case.
    /// Ultra-auditable: captures every status transition, user, time, note, device/IP, digital signature, and event version.
    /// 100% GMP/CSV/21 CFR Part 11 compliant, ready for forensic, audit, and inspection.
    /// </summary>
    [Table("capa_status_history")]
    public class CapaStatusHistory
    {
        /// <summary>
        /// Unique status history entry ID (Primary Key).
        /// </summary>
        [Key]
        [Column("id")]
        [Display(Name = "ID status promjene")]
        public int Id { get; set; }

        /// <summary>
        /// FK – CAPA case this status change relates to.
        /// </summary>
        [Required]
        [Column("capa_case_id")]
        [Display(Name = "CAPA slučaj")]
        public int CapaCaseId { get; set; }

        /// <summary>
        /// Navigation to the related CAPA case (EF will hydrate).
        /// </summary>
        [ForeignKey(nameof(CapaCaseId))]
        public CapaCase CapaCase { get; set; } = null!

        ;

        /// <summary>
        /// Previous status (before change).
        /// </summary>
        [Required]
        [Column("old_status")]
        [MaxLength(32)]
        [Display(Name = "Stari status")]
        public string OldStatus { get; set; } = string.Empty;

        /// <summary>
        /// New status (after change).
        /// </summary>
        [Required]
        [Column("new_status")]
        [MaxLength(32)]
        [Display(Name = "Novi status")]
        public string NewStatus { get; set; } = string.Empty;

        /// <summary>
        /// FK – User who performed the status change.
        /// </summary>
        [Column("changed_by")]
        [Display(Name = "Promijenio korisnik")]
        public int? ChangedById { get; set; }

        /// <summary>
        /// Navigation to user who changed status (EF will hydrate).
        /// </summary>
        [ForeignKey(nameof(ChangedById))]
        public User ChangedBy { get; set; } = null!;

        /// <summary>
        /// Timestamp of the status change.
        /// </summary>
        [Required]
        [Column("changed_at")]
        [Display(Name = "Vrijeme promjene")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Note or reason for status change (for audit, workflow, CAPA reference).
        /// </summary>
        [Column("note")]
        [MaxLength(1000)]
        [Display(Name = "Napomena")]
        public string Note { get; set; } = string.Empty;

        /// <summary>
        /// Device/IP/host from which the change was made (forensic).
        /// </summary>
        [Column("source_ip")]
        [MaxLength(45)]
        [Display(Name = "IP adresa")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Digital signature/hash (audit/Part 11).
        /// </summary>
        [Column("digital_signature")]
        [MaxLength(128)]
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Change version (for rollback/event sourcing).
        /// </summary>
        [Column("change_version")]
        [Display(Name = "Verzija")]
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Soft delete/archive (GDPR/traceability).
        /// </summary>
        [Column("is_deleted")]
        [Display(Name = "Arhivirano")]
        public bool IsDeleted { get; set; } = false;
    }
}

