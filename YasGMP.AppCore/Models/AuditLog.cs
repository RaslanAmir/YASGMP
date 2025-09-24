using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>AuditLog</b> – Central system audit/activity log for every user and system action (CRUD, login, export, print, rollback, digital signature, etc.).
    /// <para>
    /// Fully GMP, 21 CFR Part 11, EU Annex 11, and HALMED compliant.<br/>
    /// Tracks: who, when, what action, what data/entity, what field, before/after values, device, IP, context, severity, digital signature, and rollback version.
    /// </para>
    /// <remarks>
    /// • Every change (incl. failed attempts) must be logged here for regulatory, forensics, and legal defense.<br/>
    /// • All digital signature events should be cryptographically validated and traceable.<br/>
    /// • Use for all CRUD, user, integration, and critical system actions!
    /// </remarks>
    /// </summary>
    [Table("audit_log")]
    public partial class AuditLog
    {
        /// <summary>
        /// Unique log entry ID (primary key, auto-increment).
        /// </summary>
        [Key]
        [Column("id")]
        [Display(Name = "ID zapisa")]
        public int Id { get; set; }

        /// <summary>
        /// ID of the user who performed the action (foreign key to User).
        /// </summary>
        [Required]
        [Column("user_id")]
        [Display(Name = "Korisnik")]
        public int? UserId { get; set; }

        /// <summary>
        /// Navigation property to the user who performed the action (required, EF-populated).
        /// </summary>
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        /// <summary>
        /// Action performed (e.g., CREATE, UPDATE, DELETE, LOGIN, PRINT, SIGN, EXPORT, ...).
        /// </summary>
        [Required]
        [MaxLength(32)]
        [Column("action")]
        [Display(Name = "Akcija")]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// UTC timestamp when the action was performed.
        /// </summary>
        [Required]
        [Column("event_time")]
        [Display(Name = "Datum i vrijeme")]
        public DateTime DateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Table/entity name affected (nullable, for global/system actions).
        /// </summary>
        [MaxLength(64)]
        [Column("table_name")]
        [Display(Name = "Tablica")]
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Primary record ID affected by the action (nullable).
        /// </summary>
        [Column("record_id")]
        [Display(Name = "ID zapisa (entitet)")]
        public int? RecordId { get; set; }

        /// <summary>
        /// Name of the field changed (nullable).
        /// </summary>
        [MaxLength(64)]
        [Column("field_name")]
        [Display(Name = "Polje")]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Old value before the change (nullable).
        /// </summary>
        [MaxLength(2000)]
        [Column("old_value")]
        [Display(Name = "Stara vrijednost")]
        public string OldValue { get; set; } = string.Empty;

        /// <summary>
        /// New value after the change (nullable).
        /// </summary>
        [MaxLength(2000)]
        [Column("new_value")]
        [Display(Name = "Nova vrijednost")]
        public string NewValue { get; set; } = string.Empty;

        /// <summary>
        /// Details/context (JSON snapshot, image link, context string, error, etc.).
        /// </summary>
        [MaxLength(2000)]
        [Column("details")]
        [Display(Name = "Detalji")]
        public string Details { get; set; } = string.Empty;

        /// <summary>
        /// IP address of the device used for the action (compliance/forensics).
        /// </summary>
        [MaxLength(64)]
        [Column("source_ip")]
        [Display(Name = "IP adresa")]
        public string SourceIp { get; set; } = string.Empty;

        /// <summary>
        /// Device info (browser, OS, model, app version, etc).
        /// </summary>
        [MaxLength(256)]
        [Column("device_info")]
        [Display(Name = "Uređaj")]
        public string DeviceInfo { get; set; } = string.Empty;

        /// <summary>
        /// Severity/level (e.g., info, warning, critical, audit, security, forensic).
        /// </summary>
        [MaxLength(32)]
        [Column("severity")]
        [Display(Name = "Razina/severity")]
        public string Severity { get; set; } = string.Empty;

        /// <summary>
        /// Digital signature/hash for audit integrity (GMP/CSV/21 CFR Part 11).
        /// </summary>
        [MaxLength(256)]
        [Column("digital_signature")]
        [Display(Name = "Digitalni potpis")]
        public string DigitalSignature { get; set; } = string.Empty;

        /// <summary>
        /// Rollback/event sourcing version (for full audit and rollback scenarios).
        /// </summary>
        [Column("change_version")]
        [Display(Name = "Verzija događaja")]
        public int ChangeVersion { get; set; } = 1;

        /// <summary>
        /// Is this audit record soft-deleted/archived (GDPR/cleanup support).
        /// </summary>
        [Column("is_deleted")]
        [Display(Name = "Arhivirano/obrisano")]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Optional freeform note (incident reference, CAPA, root cause, etc.).
        /// </summary>
        [NotMapped]
        [Display(Name = "Napomena")]
        public string Note
        {
            get => Details;
            set => Details = value;
        }
    }
}
