using System;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>KpiWidget</b> – Represents a dashboard Key Performance Indicator (KPI) widget for real-time analytics in GMP/CMMS/QMS systems.
    /// <para>
    /// ✅ Used for dashboards, analytics panels, status overviews.<br/>
    /// ✅ Supports dynamic value, trend, color coding, icons, drilldown, alert status, last update.<br/>
    /// ✅ Extensible for charting, regulatory metrics, IoT feeds, or any dashboard scenario.<br/>
    /// </para>
    /// </summary>
    public class KpiWidget
    {
        /// <summary>
        /// Unique ID of the KPI widget (optional).
        /// </summary>
        [Key]
        [Display(Name = "ID KPI-a")]
        public int Id { get; set; }

        /// <summary>
        /// Display name of the KPI (e.g., "Open Work Orders").
        /// </summary>
        [Required]
        [StringLength(80)]
        [Display(Name = "Naziv KPI-a")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Numeric value to display (e.g., 42, 98.7).
        /// </summary>
        [Display(Name = "Vrijednost")]
        public decimal Value { get; set; }

        /// <summary>
        /// String value for more complex metrics (optional, e.g., "97%", "OK").
        /// </summary>
        [StringLength(20)]
        [Display(Name = "Tekstualna vrijednost")]
        public string? ValueText { get; set; }

        /// <summary>
        /// Unit of measure (e.g., "%", "pcs", "h").
        /// </summary>
        [StringLength(16)]
        [Display(Name = "Jedinica mjere")]
        public string? Unit { get; set; }

        /// <summary>
        /// Icon or image name for UI (e.g., "workorder", "warning").
        /// </summary>
        [StringLength(48)]
        [Display(Name = "Ikona")]
        public string? Icon { get; set; }

        /// <summary>
        /// Optional color for widget (e.g., "#2979FF" or "danger").
        /// </summary>
        [StringLength(24)]
        [Display(Name = "Boja")]
        public string? Color { get; set; }

        /// <summary>
        /// Optional: trend indicator ("up", "down", "neutral").
        /// </summary>
        [StringLength(12)]
        [Display(Name = "Trend")]
        public string? Trend { get; set; }

        /// <summary>
        /// Is the widget in an alert/warning state?
        /// </summary>
        [Display(Name = "Upozorenje")]
        public bool IsAlert { get; set; } = false;

        /// <summary>
        /// Last update time of the KPI.
        /// </summary>
        [Display(Name = "Vrijeme zadnje promjene")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        /// <summary>
        /// Optional: drilldown link or filter (e.g., navigation key).
        /// </summary>
        [StringLength(128)]
        [Display(Name = "Drilldown link/filter")]
        public string? DrilldownKey { get; set; }

        /// <summary>
        /// Bonus: any additional notes (audit, regulatory info, etc.).
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Napomene")]
        public string? Note { get; set; }
    }
}

