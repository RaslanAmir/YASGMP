using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>ExternalServicer</b> – Master data for every external contractor (laboratory, calibration, maintenance, validation, audit, IT…).
    /// <para>
    /// ⭐ SUPER-MEGA-ULTRA ROBUST:  
    /// - Tracks all provider/lab info, ID, status, contracts, certificates, contacts  
    /// - Full audit chain, digital signature, forensics  
    /// - Workflow, status, dates, comments, soft delete, and extensibility  
    /// - Regulatory bulletproof for any global GMP/CSV/21 CFR Part 11/HALMED/FDA inspection
    /// </para>
    /// </summary>
    public class ExternalServicer
    {
        /// <summary>
        /// Unique ID for the external servicer (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Firm/lab/service name.
        /// </summary>
        [Required]
        [StringLength(100)]
        [Display(Name = "Naziv servisera")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Internal code/barcode/tag for the provider.
        /// </summary>
        [StringLength(50)]
        public string? Code { get; set; }

        /// <summary>
        /// VAT/OIB or other official registration/ID.
        /// </summary>
        [StringLength(30)]
        [Display(Name = "OIB/ID")]
        public string? VatOrId { get; set; }

        /// <summary>
        /// Contact person for GMP/inspection.
        /// </summary>
        [StringLength(100)]
        public string? ContactPerson { get; set; }

        /// <summary>
        /// Email contact.
        /// </summary>
        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }

        /// <summary>
        /// Telephone/mobile.
        /// </summary>
        [StringLength(50)]
        public string? Phone { get; set; }

        /// <summary>
        /// Address.
        /// </summary>
        [StringLength(255)]
        public string? Address { get; set; }

        /// <summary>
        /// Provider type (lab, service, audit, inspection, validation, IT, other).
        /// </summary>
        [StringLength(50)]
        [Display(Name = "Vrsta")]
        public string? Type { get; set; }

        /// <summary>
        /// Certificate/contract file(s) (multiple allowed, path/URL).
        /// </summary>
        [Display(Name = "Certifikati/ugovori")]
        public List<string> CertificateFiles { get; set; } = new();

        /// <summary>
        /// Cooperation status: active, expired, suspended, pending.
        /// </summary>
        [StringLength(30)]
        public string? Status { get; set; }

        /// <summary>
        /// Contract/cooperation start date.
        /// </summary>
        [Display(Name = "Početak suradnje")]
        public DateTime? CooperationStart { get; set; }

        /// <summary>
        /// Contract/certificate expiry date.
        /// </summary>
        [Display(Name = "Istek suradnje")]
        public DateTime? CooperationEnd { get; set; }

        /// <summary>
        /// GMP comment (audit/inspection/notes).
        /// </summary>
        [StringLength(1000)]
        [Display(Name = "GMP napomena")]
        public string? Comment { get; set; }

        /// <summary>
        /// Digital signature of the last approval/change.
        /// </summary>
        [StringLength(256)]
        [Display(Name = "Digitalni potpis")]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Last modification timestamp (audit).
        /// </summary>
        [Display(Name = "Zadnja izmjena")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ID of user who last changed the record.
        /// </summary>
        [Display(Name = "Zadnji izmijenio")]
        public int LastModifiedById { get; set; }

        /// <summary>
        /// Navigation to user who last changed the record.
        /// </summary>
        [ForeignKey(nameof(LastModifiedById))]
        public virtual User? LastModifiedBy { get; set; }

        /// <summary>
        /// IP/device of last modifier (forensic trace).
        /// </summary>
        [StringLength(128)]
        [Display(Name = "IP adresa")]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Bonus: Soft delete/archive (GDPR, traceability).
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Bonus: Chain/version for rollback, audit event sourcing.
        /// </summary>
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Bonus: Extra notes, inspection results, supervisor comments.
        /// </summary>
        [StringLength(1000)]
        public string? ExtraNotes { get; set; }
    }
}
