using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>MachineComponent</b> – Ultimate digital twin for any machine/asset component.
    /// <para>
    /// ✅ Full lifecycle tracking, calibration, validation, CAPA, photos, docs, signatures, IoT  
    /// ✅ Maximum traceability (audit, forensics, all links, inspector/validation-ready)  
    /// ✅ Ready for Industry 4.0, AI/ML analytics, digital workflow, and smart maintenance  
    /// ✅ Extended: Includes all bonus fields for compatibility and future needs!
    /// </para>
    /// </summary>
    public class MachineComponent
    {
        /// <summary>Unique component identifier (Primary Key).</summary>
        [Key]
        public int Id { get; set; }

        /// <summary>FK to parent machine/asset.</summary>
        [Required]
        public int MachineId { get; set; }

        /// <summary>Navigation to parent machine.</summary>
        public virtual Machine? Machine { get; set; }

        /// <summary>Internal code or part number.</summary>
        [Required, StringLength(50)]
        [Display(Name = "Šifra komponente")]
        public string Code { get; set; } = string.Empty;

        /// <summary>Component name/description.</summary>
        [Required, StringLength(100)]
        [Display(Name = "Naziv komponente")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Long description of the component.</summary>
        [StringLength(255)]
        [Display(Name = "Opis")]
        public string? Description { get; set; }

        /// <summary>Component type or category.</summary>
        [StringLength(50)]
        public string? Type { get; set; }

        /// <summary>Component model/version.</summary>
        [StringLength(50)]
        public string? Model { get; set; }

        /// <summary>Date of installation or commissioning.</summary>
        [Display(Name = "Datum ugradnje")]
        public DateTime? InstallDate { get; set; }

        /// <summary>Date of purchase/acquisition.</summary>
        [Display(Name = "Datum nabave")]
        public DateTime? PurchaseDate { get; set; }

        /// <summary>Date until warranty is valid.</summary>
        [Display(Name = "Jamstvo vrijedi do")]
        public DateTime? WarrantyUntil { get; set; }

        /// <summary>Date when warranty expires (alternative/bonus field).</summary>
        [Display(Name = "Istek jamstva")]
        public DateTime? WarrantyExpiry { get; set; }

        /// <summary>Operational status ("active", "reserved", "in repair", ...).</summary>
        [Display(Name = "Status")]
        [StringLength(30)]
        public string? Status { get; set; }

        /// <summary>Serial number (manufacturer/ERP/traceability).</summary>
        [StringLength(80)]
        public string? SerialNumber { get; set; }

        /// <summary>Supplier or manufacturer.</summary>
        [StringLength(100)]
        [Display(Name = "Dobavljač/proizvođač")]
        public string? Supplier { get; set; }

        /// <summary>RFID/NFC tag code for physical tracking.</summary>
        [StringLength(64)]
        [Display(Name = "RFID/NFC")]
        public string? RfidTag { get; set; }

        /// <summary>IoT Device ID (for smart maintenance, telemetry).</summary>
        [StringLength(64)]
        public string? IoTDeviceId { get; set; }

        /// <summary>Lifecycle phase (e.g. installed, in use, replaced, scrapped).</summary>
        [StringLength(30)]
        public string? LifecyclePhase { get; set; }

        /// <summary>Indicates if this is a critical component (regulatory/audit).</summary>
        public bool IsCritical { get; set; }

        /// <summary>Any free-form notes (max 200 chars).</summary>
        [StringLength(200)]
        [Display(Name = "Napomena")]
        public string? Note { get; set; }

        /// <summary>Full audit/traceability digital signature hash.</summary>
        [StringLength(128)]
        public string? DigitalSignature { get; set; }

        /// <summary>Free-form additional notes or remarks (compatibility with all UIs).</summary>
        [StringLength(255)]
        [Display(Name = "Dodatne napomene")]
        public string? Notes { get; set; }

        /// <summary>Last modification date/time (UTC).</summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>ID of the user who last modified this record.</summary>
        public int LastModifiedById { get; set; }

        /// <summary>Navigation to last modifying user.</summary>
        public virtual User? LastModifiedBy { get; set; }

        /// <summary>IP address from which the record was last changed.</summary>
        [StringLength(64)]
        public string? SourceIp { get; set; }

        /// <summary>List of linked document file paths/URLs (certificates, docs, etc).</summary>
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
        [NotMapped]
        public Dictionary<string, object> ExtraFields { get; set; } = new();

        // ================= NEW: FULL MAPPING/AUDIT FIELDS =================

        /// <summary>
        /// Indicates if this record is soft-deleted/archived (GDPR, not physically deleted).
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Chain/version for rollback, event sourcing, and full audit.
        /// </summary>
        public int ChangeVersion { get; set; } = 1;

        // ================= COMPUTED PROPERTIES =================

        /// <summary>
        /// Returns true if there is any open CAPA case (not closed).
        /// </summary>
        [NotMapped]
        public bool HasOpenCapa => CapaCases?.Any(c => c.Status != null && c.Status.ToLower() != "zatvoren") == true;

        /// <summary>
        /// Returns true if a calibration is overdue.
        /// </summary>
        [NotMapped]
        public bool RequiresCalibration =>
            Calibrations != null && Calibrations.Any(c => c.NextDue.Date < DateTime.UtcNow.Date);

        // ================= METHODS =================

        /// <summary>
        /// Deep copy (clone) for digital twin/simulation/rollback.
        /// </summary>
        public MachineComponent DeepCopy()
        {
            return new MachineComponent
            {
                Id = this.Id,
                MachineId = this.MachineId,
                Machine = this.Machine,
                Code = this.Code,
                Name = this.Name,
                Description = this.Description,
                Type = this.Type,
                Model = this.Model,
                InstallDate = this.InstallDate,
                PurchaseDate = this.PurchaseDate,
                WarrantyUntil = this.WarrantyUntil,
                WarrantyExpiry = this.WarrantyExpiry,
                Status = this.Status,
                SerialNumber = this.SerialNumber,
                Supplier = this.Supplier,
                RfidTag = this.RfidTag,
                IoTDeviceId = this.IoTDeviceId,
                LifecyclePhase = this.LifecyclePhase,
                IsCritical = this.IsCritical,
                Note = this.Note,
                DigitalSignature = this.DigitalSignature,
                Notes = this.Notes,
                LastModified = this.LastModified,
                LastModifiedById = this.LastModifiedById,
                LastModifiedBy = this.LastModifiedBy,
                SourceIp = this.SourceIp,
                Documents = new List<string>(this.Documents),
                Photos = new List<Photo>(this.Photos.Select(p => p.DeepCopy())),
                Calibrations = new List<Calibration>(this.Calibrations),
                CapaCases = new List<CapaCase>(this.CapaCases),
                WorkOrders = new List<WorkOrder>(this.WorkOrders),
                ExtraFields = new Dictionary<string, object>(this.ExtraFields),
                IsDeleted = this.IsDeleted,
                ChangeVersion = this.ChangeVersion
            };
        }

        /// <summary>
        /// Returns a short string summary for display/logging.
        /// </summary>
        public override string ToString()
        {
            return $"Component: {Name} [{Code}] (Status: {Status}) – Machine#{MachineId}";
        }
    }
}
