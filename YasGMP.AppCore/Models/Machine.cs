using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Machine</b> – Ultra-mega robust GMP/CMMS digital twin for any machine, line, equipment, or asset.
    /// <para>
    /// ✅ All ultra and bonus fields for maximum integration (GMP/ERP/AI/IoT/Inspection)
    /// ✅ Maps to <c>machines</c> SQL table with bonus fields for legacy/UI/compatibility.
    /// </para>
    /// </summary>
    [Table("machines")]
    public partial class Machine
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required, StringLength(64)]
        [Column("code")]
        [Display(Name = "Šifra stroja")]
        public string Code { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [Column("name")]
        [Display(Name = "Naziv stroja")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        [NotMapped]
        [Display(Name = "Tip stroja")]
        public string? MachineType { get; set; }

        [StringLength(1000)]
        [Column("description")]
        [Display(Name = "Opis stroja")]
        public string? Description { get; set; }

        [StringLength(100)]
        [Column("model")]
        [Display(Name = "Model stroja")]
        public string? Model { get; set; }

        [StringLength(100)]
        [Column("manufacturer")]
        [Display(Name = "Proizvođač")]
        public string? Manufacturer { get; set; }

        [StringLength(100)]
        [Column("location")]
        [Display(Name = "Lokacija")]
        public string? Location { get; set; }

        [StringLength(100)]
        [NotMapped]
        [Display(Name = "Odgovorna osoba/entitet")]
        public string? ResponsibleParty { get; set; }

        [Column("install_date")]
        [Display(Name = "Datum instalacije")]
        public DateTime? InstallDate { get; set; }

        [Column("procurement_date")]
        [Display(Name = "Datum nabave")]
        public DateTime? ProcurementDate { get; set; }

        [NotMapped]
        [Display(Name = "Datum kupnje")]
        public DateTime? PurchaseDate => ProcurementDate;

        [Column("warranty_until")]
        [Display(Name = "Jamstvo vrijedi do")]
        public DateTime? WarrantyUntil { get; set; }

        [NotMapped]
        [Display(Name = "Jamstvo isteklo")]
        public DateTime? WarrantyExpiry => WarrantyUntil;

        [StringLength(30)]
        [Column("status")]
        [Display(Name = "Status")]
        public string? Status { get; set; }

        [StringLength(255)]
        [Column("urs_doc")]
        [Display(Name = "URS dokument")]
        public string? UrsDoc { get; set; }

        [Column("decommission_date")]
        [Display(Name = "Datum povlačenja")]
        public DateTime? DecommissionDate { get; set; }

        [Column("decommission_reason")]
        [Display(Name = "Razlog povlačenja")]
        public string? DecommissionReason { get; set; }

        [StringLength(80)]
        [Column("serial_number")]
        [Display(Name = "Serijski/lot broj")]
        public string? SerialNumber { get; set; }

        [Column("acquisition_cost")]
        [Display(Name = "Nabavna vrijednost")]
        public decimal? AcquisitionCost { get; set; }

        [StringLength(64)]
        [Column("rfid_tag")]
        [Display(Name = "RFID/NFC")]
        public string? RfidTag { get; set; }

        /// <summary>QR kod stroja (tekst ili URL). Maps to <c>qr_code</c>.</summary>
        [StringLength(128)]
        [Column("qr_code")]
        [Display(Name = "QR kod")]
        public string? QrCode { get; set; }

        /// <summary>Database-generated internal code (auto-increment style, e.g., TYPE-MFR-00001). Maps to <c>internal_code</c>.</summary>
        [StringLength(40)]
        [Column("internal_code")]
        [Display(Name = "Interna oznaka")]
        public string? InternalCode { get; set; }

        /// <summary>Canonical QR payload stored in DB (e.g., yasgmp://machine/123). Maps to <c>qr_payload</c>.</summary>
        [StringLength(120)]
        [Column("qr_payload")]
        [Display(Name = "QR payload")]
        public string? QrPayload { get; set; }

        [StringLength(64)]
        [Column("iot_device_id")]
        [Display(Name = "IoT uređaj ID")]
        public string? IoTDeviceId { get; set; }

        [StringLength(64)]
        [Column("cloud_device_guid")]
        [Display(Name = "Cloud device GUID")]
        public string? CloudDeviceGuid { get; set; }

        [Column("is_critical")]
        [Display(Name = "Kritično")]
        public bool IsCritical { get; set; }

        [StringLength(30)]
        [Column("lifecycle_phase")]
        [Display(Name = "Faza životnog ciklusa")]
        public string? LifecyclePhase { get; set; }

        [StringLength(200)]
        [Column("note")]
        [Display(Name = "Napomena")]
        public string? Note { get; set; }

        [NotMapped]
        [Display(Name = "Napomene (UI alias)")]
        public string? Notes => Note;

        [Column("last_modified")]
        [Display(Name = "Zadnja izmjena")]
        public DateTime LastModified { get; set; }

        [Column("last_modified_by_id")]
        [Display(Name = "Zadnji izmijenio")]
        public int? LastModifiedById { get; set; }

        [ForeignKey(nameof(LastModifiedById))]
        public virtual User? LastModifiedBy { get; set; }

        [StringLength(128)]
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        public string? DigitalSignature { get; set; }

        public virtual ICollection<MachineComponent> Components { get; set; } = new List<MachineComponent>();
        public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
        public virtual ICollection<Inspection> Inspections { get; set; } = new List<Inspection>();
        public virtual ICollection<Calibration> Calibrations { get; set; } = new List<Calibration>();

        [NotMapped]
        public Dictionary<string, object> ExtraFields { get; set; } = new();

        [NotMapped]
        [Display(Name = "Tehnička dokumentacija")]
        public List<string> LinkedDocuments { get; set; } = new();

        [NotMapped]
        public string DisplayStatus => (Status ?? string.Empty) switch
        {
            "active" => "U pogonu",
            "maintenance" => "Održavanje",
            "decommissioned" => "Povučen",
            "reserved" => "Rezerviran",
            "scrapped" => "Otpisan",
            _ => Status ?? string.Empty
        };

        [NotMapped]
        public bool IsUnderWarranty => WarrantyUntil.HasValue && WarrantyUntil > DateTime.UtcNow;

        [NotMapped]
        public bool IsDecommissioned => (Status?.ToLowerInvariant() == "decommissioned") || DecommissionDate.HasValue;

        /// <summary>Deep copy for rollback/inspection/dialog</summary>
        public Machine DeepCopy()
        {
            return new Machine
            {
                Id                 = this.Id,
                Code               = this.Code,
                Name               = this.Name,
                MachineType        = this.MachineType,
                Description        = this.Description,
                Model              = this.Model,
                Manufacturer       = this.Manufacturer,
                Location           = this.Location,
                ResponsibleParty   = this.ResponsibleParty,
                InstallDate        = this.InstallDate,
                ProcurementDate    = this.ProcurementDate,
                WarrantyUntil      = this.WarrantyUntil,
                Status             = this.Status,
                UrsDoc             = this.UrsDoc,
                DecommissionDate   = this.DecommissionDate,
                DecommissionReason = this.DecommissionReason,
                SerialNumber       = this.SerialNumber,
                AcquisitionCost    = this.AcquisitionCost,
                RfidTag            = this.RfidTag,
                QrCode             = this.QrCode,
                InternalCode       = this.InternalCode,
                QrPayload          = this.QrPayload,
                IoTDeviceId        = this.IoTDeviceId,
                CloudDeviceGuid    = this.CloudDeviceGuid,
                IsCritical         = this.IsCritical,
                LifecyclePhase     = this.LifecyclePhase,
                Note               = this.Note,
                LastModified       = this.LastModified,
                LastModifiedById   = this.LastModifiedById,
                LastModifiedBy     = this.LastModifiedBy,
                DigitalSignature   = this.DigitalSignature,
                Components         = new List<MachineComponent>(this.Components),
                WorkOrders         = new List<WorkOrder>(this.WorkOrders),
                Inspections        = new List<Inspection>(this.Inspections),
                Calibrations       = new List<Calibration>(this.Calibrations),
                ExtraFields        = new Dictionary<string, object>(this.ExtraFields),
                LinkedDocuments    = new List<string>(this.LinkedDocuments)
            };
        }
    }
}
