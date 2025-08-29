using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>ContractorIntervention</b> — GMP/CSV/21 CFR Part 11 compliant record for external contractor interventions.
    /// <para>
    /// Links contractor (<see cref="User"/>), component, asset, reason, outcome, full audit, GMP status,
    /// digital signatures, forensics, versioning, document linkage, and future extensibility.
    /// </para>
    /// </summary>
    [Table("contractor_interventions")]
    public class ContractorIntervention
    {
        /// <summary>Unique intervention ID (Primary Key).</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Foreign Key to contractor user (User with "Contractor" role).</summary>
        [Required]
        [Display(Name = "Vanjski izvođač")]
        [Column("contractor_id")]
        public int ContractorId { get; set; }

        /// <summary>
        /// Navigation to the external contractor (User entity with role "Contractor").
        /// EF will hydrate this navigation property.
        /// </summary>
        [ForeignKey(nameof(ContractorId))]
        public virtual User Contractor { get; set; } = null!;

        /// <summary>Foreign Key to component (on which intervention is performed).</summary>
        [Required]
        [Display(Name = "Komponenta")]
        [Column("component_id")]
        public int ComponentId { get; set; }

        /// <summary>Navigation to the component (EF will hydrate).</summary>
        [ForeignKey(nameof(ComponentId))]
        public virtual Component Component { get; set; } = null!;

        /// <summary>Date the intervention was performed.</summary>
        [Required]
        [Display(Name = "Datum intervencije")]
        [Column("intervention_date")]
        public DateTime InterventionDate { get; set; }

        /// <summary>Reason for intervention (description or failure).</summary>
        [Required]
        [StringLength(255)]
        [Display(Name = "Razlog intervencije")]
        [Column("reason")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>Outcome of the intervention (fixed, replaced, recommendation, notes...).</summary>
        [StringLength(255)]
        [Display(Name = "Rezultat intervencije")]
        [Column("result")]
        public string Result { get; set; } = string.Empty;

        /// <summary>Was the intervention GMP-compliant (1=yes, 0=no).</summary>
        [Display(Name = "GMP sukladnost")]
        [Column("gmp_compliance")]
        public bool GmpCompliance { get; set; }

        /// <summary>File path to documentation (e.g., service report, PDF, scan).</summary>
        [StringLength(512)]
        [Display(Name = "Dokumentacija")]
        [Column("doc_file")]
        public string DocFile { get; set; } = string.Empty;

        /// <summary>Digital signature or hash of contractor/intervener (GMP compliance).</summary>
        [StringLength(256)]
        [Display(Name = "Digitalni potpis")]
        [Column("digital_signature")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Start date/time of the intervention (for scheduling & duration tracking).</summary>
        [Display(Name = "Početak intervencije")]
        [Column("start_date")]
        public DateTime? StartDate { get; set; }

        /// <summary>End date/time of the intervention (for scheduling & duration tracking).</summary>
        [Display(Name = "Završetak intervencije")]
        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        /// <summary>Additional notes or extended description about the intervention.</summary>
        [StringLength(2000)]
        [Display(Name = "Napomene")]
        [Column("notes")]
        public string Notes { get; set; } = string.Empty;

        /// <summary>Last modification date/time (audit trail).</summary>
        [Display(Name = "Zadnja izmjena")]
        [Column("last_modified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>ID of user who last modified the intervention.</summary>
        [Display(Name = "Zadnji izmijenio")]
        [Column("last_modified_by_id")]
        public int LastModifiedById { get; set; }

        /// <summary>Navigation to last modifier (EF will hydrate).</summary>
        [ForeignKey(nameof(LastModifiedById))]
        public virtual User LastModifiedBy { get; set; } = null!;

        /// <summary>IP address/device from which the modification was made (forensic record).</summary>
        [StringLength(128)]
        [Display(Name = "IP adresa")]
        [Column("source_ip")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Comments or notes (bonus: for inspection, CAPA, incident context).</summary>
        [StringLength(500)]
        [Display(Name = "Bilješke")]
        [Column("comments")]
        public string Comments { get; set; } = string.Empty;

        /// <summary>Bonus: Workflow/status (requested, scheduled, in progress, done, rejected, etc).</summary>
        [StringLength(32)]
        [Display(Name = "Status")]
        [Column("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>Bonus: Chain/version for audit, rollback, event sourcing.</summary>
        [Display(Name = "Verzija promjene")]
        [Column("change_version")]
        public int ChangeVersion { get; set; } = 1;

        /// <summary>Is this intervention soft-deleted/archived (GDPR, not physically deleted).</summary>
        [Display(Name = "Arhivirano")]
        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        // ---------------------- EXTENSIONS FOR VIEWMODEL/UI ----------------------

        /// <summary>Name of the asset or equipment linked to the intervention (for UI display).</summary>
        [NotMapped]
        public string AssetName { get; set; } = string.Empty;

        /// <summary>Contractor's display name or full name (for UI display).</summary>
        [NotMapped]
        public string ContractorName { get; set; } = string.Empty;

        /// <summary>Type/category of the intervention (preventive, corrective, calibration, inspection, etc).</summary>
        [NotMapped]
        public string InterventionType { get; set; } = string.Empty;

        /// <summary>Returns a summary string for quick logging or diagnostics.</summary>
        public override string ToString()
            => $"ContractorIntervention [Id={Id}, Contractor={ContractorName ?? ContractorId.ToString()}, Date={InterventionDate:yyyy-MM-dd}, Component={ComponentId}, Status={Status}]";
    }
}
