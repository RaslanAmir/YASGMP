using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Calibration</b> – Represents a single calibration event for any machine component, sensor, or GMP asset.
    /// <para>
    /// ⭐ SUPER-MEGA-ULTRA ROBUST: Tracks everything needed for forensic compliance, audit, recall, digital signatures, chaining, workflow, e-signature, and regulatory reporting (GMP, CSV, 21 CFR Part 11, HALMED, ISO).
    /// </para>
    /// </summary>
    [Table("calibrations")]
    public class Calibration
    {
        /// <summary>Unique calibration ID (Primary Key).</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>FK – ID of the component being calibrated.</summary>
        [Required]
        [Column("component_id")]
        [Display(Name = "Komponenta")]
        public int ComponentId { get; set; }

        /// <summary>Navigation to the machine component being calibrated (required, EF-populated).</summary>
        [ForeignKey(nameof(ComponentId))]
        public virtual MachineComponent Component { get; set; } = null!;

        /// <summary>FK – ID of the external service/lab (supplier).</summary>
        [Column("supplier_id")]
        [Display(Name = "Serviser/laboratorij")]
        public int? SupplierId { get; set; }

        /// <summary>Navigation to the supplier/laboratory (optional).</summary>
        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier? Supplier { get; set; }

        /// <summary>Calibration date (when the check was performed).</summary>
        [Required]
        [Column("calibration_date")]
        [Display(Name = "Datum kalibracije")]
        public DateTime CalibrationDate { get; set; }

        /// <summary>Expiration date (when re-calibration is required).</summary>
        [Required]
        [Column("next_due")]
        [Display(Name = "Rok ponovne kalibracije")]
        public DateTime NextDue { get; set; }

        /// <summary>Path or name of the calibration certificate (PDF, document, link).</summary>
        [MaxLength(255)]
        [Column("cert_doc")]
        [Display(Name = "Certifikat (PDF)")]
        public string CertDoc { get; set; } = string.Empty;

        /// <summary>Calibration result: prolaz (pass), pao (fail), uvjetno (conditional), napomena (remark), etc.</summary>
        [Required]
        [Column("result")]
        [StringLength(20)]
        [Display(Name = "Rezultat")]
        public string Result { get; set; } = string.Empty;

        /// <summary>Additional notes/remarks.</summary>
        [MaxLength(255)]
        [Column("comment")]
        [Display(Name = "Napomena/uvjeti")]
        public string Comment { get; set; } = string.Empty;

        /// <summary>Reference to persisted digital signature metadata.</summary>
        [Column("digital_signature_id")]
        [Display(Name = "ID digitalnog potpisa")]
        public int? DigitalSignatureId { get; set; }

        /// <summary>Digital signature (name, hash, or e-signature of creator or approver).</summary>
        [MaxLength(128)]
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Last modification timestamp (audit trail).</summary>
        [Column("last_modified")]
        [Display(Name = "Zadnja izmjena")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>User ID of last modifier (FK).</summary>
        [Column("last_modified_by_id")]
        [Display(Name = "Zadnji izmijenio")]
        public int? LastModifiedById { get; set; }

        /// <summary>Navigation to last modifier (optional).</summary>
        [ForeignKey(nameof(LastModifiedById))]
        public virtual User? LastModifiedBy { get; set; }

        /// <summary>Forensic: IP address/device of last modification (bonus, optional).</summary>
        [MaxLength(45)]
        [Column("source_ip")]
        [Display(Name = "IP adresa")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Approval status (regulatory control; supports workflow and audit).</summary>
        [Column("approved")]
        [Display(Name = "Odobreno")]
        public bool Approved { get; set; }

        /// <summary>Date/time when calibration was approved.</summary>
        [Column("approved_at")]
        [Display(Name = "Datum odobrenja")]
        public DateTime? ApprovedAt { get; set; }

        /// <summary>User ID who approved this calibration.</summary>
        [Column("approved_by_id")]
        [Display(Name = "Odobrio")]
        public int? ApprovedById { get; set; }

        /// <summary>Navigation to the approving user (optional).</summary>
        [ForeignKey(nameof(ApprovedById))]
        public virtual User? ApprovedBy { get; set; }

        /// <summary>Bonus: Recalibration reference (for chaining/traceability).</summary>
        [Column("previous_calibration_id")]
        public int? PreviousCalibrationId { get; set; }

        /// <summary>Navigation to previous calibration (optional).</summary>
        [ForeignKey(nameof(PreviousCalibrationId))]
        public virtual Calibration? PreviousCalibration { get; set; }

        /// <summary>Bonus: Next planned calibration reference (for traceability).</summary>
        [Column("next_calibration_id")]
        public int? NextCalibrationId { get; set; }

        /// <summary>Navigation to next calibration (optional).</summary>
        [ForeignKey(nameof(NextCalibrationId))]
        public virtual Calibration? NextCalibration { get; set; }

        /// <summary>Full chain/version support for audit, rollback, event sourcing.</summary>
        [Column("change_version")]
        public int ChangeVersion { get; set; } = 1;

        /// <summary>Is this calibration record soft-deleted/archived (GDPR, cleanup, not physical delete).</summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;

        // ---------------------------------------------------------------------
        // COMPUTED PROPERTY: STATUS (Not mapped, always fresh, never stored!)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Gets the GMP-compliant current status for the calibration
        /// (computed: "overdue", "due", "valid", "scheduled", "rejected", "expired").
        /// </summary>
        [NotMapped]
        [Display(Name = "Status")]
        public string Status
        {
            get
            {
                // "rejected" always takes priority if found in result
                if (!string.IsNullOrWhiteSpace(Result) && Result.ToLower().Contains("reject"))
                    return "rejected";

                // If the calibration date is in the future and not yet performed, it's scheduled
                if (CalibrationDate > DateTime.Now)
                    return "scheduled";

                // If next due is in the past, it's overdue
                if (NextDue < DateTime.Now)
                    return "overdue";

                // If next due is within 30 days, it's due soon
                if (NextDue <= DateTime.Now.AddDays(30))
                    return "due";

                // If calibration is performed, certificate present, all ok
                if (NextDue > DateTime.Now.AddDays(30))
                    return "valid";

                // Default fallback
                return "expired";
            }
        }
    }
}
