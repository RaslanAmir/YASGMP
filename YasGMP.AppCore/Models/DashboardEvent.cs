using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>DashboardEvent</b> – Represents a dashboard event, notification, or recent activity for GMP/CMMS dashboards.
    /// <para>
    /// ✅ Used for "recent actions", "alerts", "audit events", user notifications, and status feeds.<br/>
    /// ✅ Supports linking to users, modules, records, audit severity, status, timestamp, and deep linking.<br/>
    /// ✅ Extensible for forensic logs, regulatory audit, escalation, and user assignments.
    /// </para>
    /// </summary>
    public class DashboardEvent
    {
        /// <summary>
        /// Unique event ID (Primary Key).
        /// </summary>
        [Key]
        [Display(Name = "ID događaja")]
        public int Id { get; set; }

        /// <summary>
        /// Type of event (e.g., "login", "work_order_closed", "capa_escalated").
        /// </summary>
        [Required]
        [StringLength(64)]
        [Display(Name = "Tip događaja")]
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description of the event.
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Opis događaja")]
        public string? Description { get; set; }

        /// <summary>
        /// Timestamp when the event occurred.
        /// </summary>
        [Display(Name = "Vrijeme događaja")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Severity or importance (info, warning, critical, audit, forensic).
        /// </summary>
        [StringLength(16)]
        [Display(Name = "Važnost")]
        public string Severity { get; set; } = "info";

        /// <summary>
        /// User who triggered or is related to the event (FK).
        /// </summary>
        [Display(Name = "Korisnik")]
        public int? UserId { get; set; }

        /// <summary>
        /// Optional: related module/table (e.g., "WorkOrder", "CAPA").
        /// </summary>
        [StringLength(32)]
        [Display(Name = "Modul")]
        public string? RelatedModule { get; set; }

        /// <summary>
        /// Optional: related record/entity ID.
        /// </summary>
        [Display(Name = "ID entiteta")]
        public int? RelatedRecordId { get; set; }

        /// <summary>
        /// Icon name or type for UI display.
        /// </summary>
        [StringLength(48)]
        [Display(Name = "Ikona")]
        public string? Icon { get; set; }

        /// <summary>
        /// Is this event unread or new for the current user?
        /// </summary>
        [Display(Name = "Novo/nepročitano")]
        public bool IsUnread { get; set; } = true;

        /// <summary>
        /// Optional: navigation key, deep link, or filter for quick dashboard actions.
        /// </summary>
        [StringLength(128)]
        [Display(Name = "Drilldown ključ/link")]
        public string? DrilldownKey { get; set; }

        /// <summary>
        /// Additional notes, audit info, or escalation details.
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Napomene")]
        public string? Note { get; set; }
    }
}
