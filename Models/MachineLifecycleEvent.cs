using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>MachineLifecycleEvent</b> – GMP/CMMS record of all key lifecycle events for a machine/asset.
    /// <para>
    /// Covers procurement, installation, maintenance, repair, upgrade, move, decommission, scrap, and more.
    /// ✅ Fully audit-ready (user, timestamp, docs, notes).
    /// ✅ Designed for regulatory compliance (21 CFR Part 11, Annex 11).
    /// ✅ Extensible for attachments, IoT, evidence, and full change traceability.
    /// </para>
    /// </summary>
    public class MachineLifecycleEvent
    {
        /// <summary>
        /// Unique identifier for this lifecycle event (Primary Key).
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the associated machine.
        /// </summary>
        [Required]
        [Display(Name = "Stroj / Oprema")]
        public int MachineId { get; set; }

        /// <summary>
        /// Navigation property to the associated machine/asset.
        /// </summary>
        public Machine? Machine { get; set; }

        /// <summary>
        /// Type of event: procurement, installation, maintenance, repair, upgrade, move, decommission, scrap, other.
        /// </summary>
        [Required]
        [Display(Name = "Vrsta događaja")]
        public MachineLifecycleEventType EventType { get; set; }

        /// <summary>
        /// Date and time when the event occurred.
        /// </summary>
        [Required]
        [Display(Name = "Datum događaja")]
        public DateTime EventDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Foreign key to the user who performed the event.
        /// </summary>
        [Display(Name = "Izvršio")]
        public int? PerformedById { get; set; }

        /// <summary>
        /// Navigation property to the user who performed the event.
        /// </summary>
        public User? PerformedBy { get; set; }

        /// <summary>
        /// Additional notes about the event (e.g. context, result, findings).
        /// </summary>
        [Display(Name = "Bilješka / Napomena")]
        [MaxLength(1024)]
        public string? Notes { get; set; }

        /// <summary>
        /// Path to any supporting documentation file (SOP, work order, evidence).
        /// </summary>
        [Display(Name = "Dokumentacija")]
        [MaxLength(255)]
        public string? DocFile { get; set; }

        /// <summary>
        /// Digital signature hash (if signed; for regulatory compliance).
        /// </summary>
        [Display(Name = "Digitalni potpis")]
        [MaxLength(128)]
        public string? DigitalSignature { get; set; }

        /// <summary>
        /// IP address from which the event was recorded (for audit/forensics).
        /// </summary>
        [Display(Name = "IP adresa")]
        [MaxLength(45)]
        public string? SourceIp { get; set; }

        /// <summary>
        /// Date/time this event was last modified (for audit trace).
        /// </summary>
        [Display(Name = "Zadnja izmjena")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who last modified this record (for full auditability).
        /// </summary>
        [Display(Name = "Izmijenio")]
        public int? LastModifiedById { get; set; }

        /// <summary>
        /// Navigation property for the user who last modified the record.
        /// </summary>
        public User? LastModifiedBy { get; set; }
    }

    /// <summary>
    /// Enumeration of all possible lifecycle event types for a machine/asset.
    /// </summary>
    public enum MachineLifecycleEventType
    {
        Procurement,
        Installation,
        Maintenance,
        Repair,
        Upgrade,
        Move,
        Decommission,
        Scrap,
        Other
    }
}
