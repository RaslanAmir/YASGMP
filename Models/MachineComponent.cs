using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>MachineComponent</b> – Digital twin model for a machine/asset component.
    /// <para>
    /// • Full lifecycle tracking (calibration, validation, CAPA, docs, photos, signatures, IoT)<br/>
    /// • Maximum traceability (audit, forensics, inspector/validation-ready)<br/>
    /// • Ready for Industry 4.0 (AI/ML analytics, digital workflow, smart maintenance)<br/>
    /// • Extended for forward compatibility with extra fields and relationships
    /// </para>
    /// </summary>
    public class MachineComponent
    {
        /// <summary>
        /// Unique component identifier (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to parent machine/asset.
        /// <para>
        /// <b>Note:</b> The database allows <c>NULL</c> (<c>machine_id INT DEFAULT NULL</c>),
        /// so this property is <see langword="nullable"/> to match the schema and avoid binding errors.
        /// </para>
        /// </summary>
        public int? MachineId { get; set; }

        /// <summary>
        /// Navigation to parent machine (optional).
        /// </summary>
        public virtual Machine? Machine { get; set; }

        /// <summary>
        /// Internal code or part number (unique-friendly identifier).
        /// </summary>
        [Required, StringLength(50)]
        [Display(Name = "Šifra komponente")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Component name/description (short).
        /// </summary>
        [Required, StringLength(100)]
        [Display(Name = "Naziv komponente")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Long description of the component.
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Opis")]
        public string? Description { get; set; }

        /// <summary>
        /// Component type or category (free text).
        /// </summary>
        [StringLength(50)]
        public string? Type { get; set; }

        /// <summary>
        /// Component model/version.
        /// </summary>
        [StringLength(50)]
        public string? Model { get; set; }

        /// <summary>
        /// Date of installation or commissioning.
        /// </summary>
        [Display(Name = "Datum ugradnje")]
        public DateTime? InstallDate { get; set; }

        /// <summary>
        /// Date of purchase/acquisition.
        /// </summary>
        [Display(Name = "Datum nabave")]
        public DateTime? PurchaseDate { get; set; }

        /// <summary>
        /// Date until warranty is valid.
        /// </summary>
        [Display(Name = "Jamstvo vrijedi do")]
        public DateTime? WarrantyUntil { get; set; }

        /// <summary>
        /// Date when warranty expires (alternative/bonus field).
        /// </summary>
        [Display(Name = "Istek jamstva")]
        public DateTime? WarrantyExpiry { get; set; }

        /// <summary>
        /// Operational status (e.g. "active", "maintenance", "removed").
        /// </summary>
        [Display(Name = "Status")]
        [StringLength(30)]
        public string? Status { get; set; }

        /// <summary>
        /// Serial number (manufacturer/ERP/traceability).
        /// </summary>
        [StringLength(80)]
        public string? SerialNumber { get; set; }

        /// <summary>
        /// Supplier or manufacturer.
        /// </summary>
        [StringLength(100)]
        [Display(Name = "Dobavljač/proizvođač")]
        public string? Supplier { get; set; }

        /// <summary>
        /// RFID/NFC tag code for physical tracking.
        /// </summary>
        [StringLength(64)]
        [Display(Name = "RFID/NFC")]
        public string? RfidTag { get; set; }

        /// <summary>
        /// IoT Device ID (for smart maintenance, telemetry).
        /// </summary>
        [StringLength(64)]
        public string? IoTDeviceId { get; set; }

        /// <summary>
        /// Lifecycle phase (e.g. installed, in use, replaced, scrapped).
        /// </summary>
        [StringLength(30)]
        public string? LifecyclePhase { get; set; }

        /// <summary>
        /// Indicates if this is a critical component (regulatory/audit).
        /// </summary>
        public bool IsCritical { get; set; }

        /// <summary>
        /// Any free-form note (max 200 chars).
        /// </summary>
        [StringLength(200)]
        [Display(Name = "Napomena")]
        public string? Note { get; set; }

        /// <summary>
        /// Full audit/traceability digital signature hash.
        /// </summary>
        [StringLength(128)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// Free-form additional notes or remarks.
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Dodatne napomene")]
        public string? Notes { get; set; }

        /// <summary>
        /// Path/URL to SOP/URS document mapped to DB <c>sop_doc</c>.
        /// </summary>
        [StringLength(255)]
        [Display(Name = "SOP/URS dokument")]
        public string? SopDoc { get; set; }

        /// <summary>
        /// Last modification date/time (UTC).
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ID of the user who last modified this record.
        /// </summary>
        public int LastModifiedById { get; set; }

        /// <summary>
        /// Navigation to last modifying user.
        /// </summary>
        public virtual User? LastModifiedBy { get; set; }

        /// <summary>
        /// IP address from which the record was last changed.
        /// </summary>
        [StringLength(64)]
        public string? SourceIp { get; set; }

        /// <summary>
        /// List of linked document paths/URLs (certificates, docs, etc).
        /// </summary>
        public List<string> Documents { get; set; } = new();

        /// <summary>All photos linked to this component.</summary>
        public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();

        /// <summary>All calibrations linked to this component.</summary>
        public virtual ICollection<Calibration> Calibrations { get; set; } = new List<Calibration>();

        /// <summary>All CAPA cases for this component.</summary>
        public virtual ICollection<CapaCase> CapaCases { get; set; } = new List<CapaCase>();

        /// <summary>All linked work orders (repairs, upgrades, replacements, etc).</summary>
        public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();

        // ================= EXTENSIBILITY (NotMapped) =================

        /// <summary>
        /// Additional ad-hoc fields (key/value) for extensibility.
        /// </summary>
        [NotMapped]
        public Dictionary<string, object> ExtraFields { get; set; } = new();

        // ================= MAPPING / AUDIT FIELDS =================

        /// <summary>
        /// Indicates if this record is soft-deleted/archived (GDPR).
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Chain/version for rollback, event sourcing, and full audit.
        /// </summary>
        public int ChangeVersion { get; set; } = 1;

        // ================= COMPUTED PROPERTIES =================

        /// <summary>
        /// True if there is any open CAPA case (not closed/zatvoren).
        /// </summary>
        [NotMapped]
        public bool HasOpenCapa => CapaCases?.Any(c => c.Status != null && c.Status.ToLower() != "zatvoren") == true;

        /// <summary>
        /// True if a calibration is overdue.
        /// </summary>
        [NotMapped]
        public bool RequiresCalibration =>
            Calibrations != null && Calibrations.Any(c => c.NextDue.Date < DateTime.UtcNow.Date);

        // ================= HELPERS / NORMALIZATION =================

        /// <summary>
        /// Canonical normalization of component status to match DB enum
        /// (<c>active</c>, <c>maintenance</c>, <c>removed</c>).
        /// </summary>
        public static string NormalizeStatus(string? raw)
        {
            var s = (raw ?? string.Empty).Trim().ToLowerInvariant();
            return s switch
            {
                "active" => "active",
                "maintenance" or "maint" or "servis" or "service" => "maintenance",
                "removed" or "decommissioned" or "retired" => "removed",
                _ => "active"
            };
        }

        // ================= METHODS =================

        /// <summary>
        /// Deep copy (clone) for digital twin/simulation/rollback.
        /// </summary>
        public MachineComponent DeepCopy()
        {
            return new MachineComponent
            {
                Id = Id,
                MachineId = MachineId,
                Machine = Machine,
                Code = Code,
                Name = Name,
                Description = Description,
                Type = Type,
                Model = Model,
                InstallDate = InstallDate,
                PurchaseDate = PurchaseDate,
                WarrantyUntil = WarrantyUntil,
                WarrantyExpiry = WarrantyExpiry,
                Status = Status,
                SerialNumber = SerialNumber,
                Supplier = Supplier,
                RfidTag = RfidTag,
                IoTDeviceId = IoTDeviceId,
                LifecyclePhase = LifecyclePhase,
                IsCritical = IsCritical,
                Note = Note,
                DigitalSignature = DigitalSignature,
                Notes = Notes,
                SopDoc = SopDoc,
                LastModified = LastModified,
                LastModifiedById = LastModifiedById,
                LastModifiedBy = LastModifiedBy,
                SourceIp = SourceIp,
                Documents = new List<string>(Documents),
                Photos = new List<Photo>(Photos.Select(p => p.DeepCopy())),
                Calibrations = new List<Calibration>(Calibrations),
                CapaCases = new List<CapaCase>(CapaCases),
                WorkOrders = new List<WorkOrder>(WorkOrders),
                ExtraFields = new Dictionary<string, object>(ExtraFields),
                IsDeleted = IsDeleted,
                ChangeVersion = ChangeVersion
            };
        }

        /// <summary>
        /// Short string summary for display/logging.
        /// </summary>
        public override string ToString()
            => $"Component: {Name} [{Code}] (Status: {Status}) – Machine#{MachineId}";
    }
}
