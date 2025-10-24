using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>Component</b> – Represents a machine component in the GMP/CMMS system.
    /// <para>
    /// ⭐ SUPER-MEGA-ULTRA ROBUST: Audit, traceability, forensics, digital signature, SOP/document linkage, lifecycle, status, warranty, supplier, all navigation and compliance fields for total regulatory defense and inspection readiness.
    /// </para>
    /// </summary>
    [Table("components")]
    public partial class Component
    {
        /// <summary>Unique identifier (Primary Key).</summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>FK – ID of the machine this component belongs to.</summary>
        [Required]
        [Column("machine_id")]
        public int MachineId { get; set; }

        /// <summary>
        /// Navigation to the machine.
        /// Initialized with the null-forgiving operator because EF will hydrate this at runtime.
        /// </summary>
        [ForeignKey(nameof(MachineId))]
        public virtual Machine Machine { get; set; } = null!;

        /// <summary>Human-friendly name of the machine (redundant for UI/reporting, keep in sync).</summary>
        [StringLength(150)]
        [Column("machine_name")]
        [Display(Name = "Stroj")]
        public string MachineName { get; set; } = string.Empty;

        /// <summary>Internal code/barcode/tag for the component.</summary>
        [Required, StringLength(50)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        /// <summary>Component name.</summary>
        [Required, StringLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>Type of component (e.g. Calibration, PPM, spare part, sensor, etc).</summary>
        [StringLength(50)]
        [Column("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>File path or document name for SOP (pdf, docx, external URL...).</summary>
        [StringLength(512)]
        [Column("sop_doc")]
        [Display(Name = "SOP dokument")]
        public string SopDoc { get; set; } = string.Empty;

        /// <summary>Install/service date (may be null).</summary>
        [Column("install_date")]
        [Display(Name = "Datum ugradnje")]
        public DateTime? InstallDate { get; set; }

        /// <summary>Component status (active, out of service, in service, decommissioned...).</summary>
        [StringLength(32)]
        [Column("status")]
        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>All calibration events/sensors related to this component.</summary>
        public virtual List<Calibration> Calibrations { get; set; } = new();

        /// <summary>All linked CAPA (Corrective and Preventive Action) cases.</summary>
        public virtual List<CapaCase> CapaCases { get; set; } = new();

        /// <summary>All work orders involving this component.</summary>
        public virtual List<WorkOrder> WorkOrders { get; set; } = new();

        /// <summary>Digital signature of the last change (user or device signature).</summary>
        [StringLength(256)]
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>Timestamp of last modification (audit).</summary>
        [Column("last_modified")]
        [Display(Name = "Zadnja izmjena")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>User ID who last modified the component (FK).</summary>
        [Column("last_modified_by_id")]
        [Display(Name = "Zadnji izmijenio")]
        public int LastModifiedById { get; set; }

        /// <summary>
        /// Navigation to last modifier.
        /// Initialized with the null-forgiving operator because EF will hydrate this at runtime.
        /// </summary>
        [ForeignKey(nameof(LastModifiedById))]
        public virtual User LastModifiedBy { get; set; } = null!

            ;

        /// <summary>Forensic: IP address/device from which this component was last modified.</summary>
        [StringLength(128)]
        [Column("source_ip")]
        [Display(Name = "IP adresa")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>Component serial/lot number (bonus: traceability).</summary>
        [StringLength(80)]
        [Column("serial_number")]
        [Display(Name = "Serijski/lot broj")]
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>Warranty expiration date (bonus).</summary>
        [Column("warranty_until")]
        [Display(Name = "Jamstvo vrijedi do")]
        public DateTime? WarrantyUntil { get; set; }

        /// <summary>Supplier/manufacturer name (bonus).</summary>
        [StringLength(100)]
        [Column("supplier")]
        [Display(Name = "Dobavljač/proizvođač")]
        public string Supplier { get; set; } = string.Empty;

        /// <summary>Linked documents (bonus for audit/inspection/validation).</summary>
        [NotMapped]
        public List<string> LinkedDocuments { get; set; } = new();

        /// <summary>Comments, notes, or last action (bonus: for audit, UI, workflow, inspection).</summary>
        [StringLength(1000)]
        [Column("comments")]
        public string Comments { get; set; } = string.Empty;

        /// <summary>Lifecycle state (bonus: new, in use, awaiting approval, retired, scrapped, etc).</summary>
        [StringLength(32)]
        [Column("lifecycle_state")]
        public string LifecycleState { get; set; } = string.Empty;

        /// <summary>Chain/version for rollback, event sourcing, and full audit.</summary>
        [Column("change_version")]
        public int ChangeVersion { get; set; } = 1;

        /// <summary>Is this record soft-deleted/archived (GDPR, not physically deleted).</summary>
        [Column("is_deleted")]
        public bool IsDeleted { get; set; } = false;
    }
}

