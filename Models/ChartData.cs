using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace YasGMP.Models
{
    /// <summary>
    /// <b>ChartData</b> – Represents a data point or series for dashboard charting/analytics in GMP/CMMS systems.
    /// <para>
    /// ✅ Flexible for any chart type: time-series, bar, pie, line, column, etc.<br/>
    /// ✅ Supports grouping, series, categories, dynamic data, value, label, color, drilldown.<br/>
    /// ✅ Extensible for regulatory analytics, IoT data, and bonus dashboard widgets.
    /// </para>
    /// </summary>
    public class ChartData
    {
        /// <summary>
        /// Primary key (optional).
        /// </summary>
        [Key]
        [Display(Name = "ID podatka")]
        public int Id { get; set; }

        /// <summary>
        /// Name or label for the data point or series (e.g., "Open WOs", "Completed").
        /// </summary>
        [StringLength(80)]
        [Display(Name = "Naziv podatka/serije")]
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Numeric value for the data point.
        /// </summary>
        [Display(Name = "Vrijednost")]
        public decimal Value { get; set; }

        /// <summary>
        /// Optional group/category (e.g., date, department, machine, etc.).
        /// </summary>
        [StringLength(48)]
        [Display(Name = "Grupa/Kategorija")]
        public string Group { get; set; } = string.Empty;

        /// <summary>
        /// Optional: secondary value (for stacked/bar/multi-axis).
        /// </summary>
        [Display(Name = "Sekundarna vrijednost")]
        public decimal? SecondaryValue { get; set; }

        /// <summary>
        /// Series name (for multi-series charts).
        /// </summary>
        [StringLength(64)]
        [Display(Name = "Serija")]
        public string Series { get; set; } = string.Empty;

        /// <summary>
        /// Optional color (hex or name).
        /// </summary>
        [StringLength(24)]
        [Display(Name = "Boja")]
        public string Color { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp (for time-series charts).
        /// </summary>
        [Display(Name = "Datum/vrijeme")]
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Drilldown key or filter (for clickable charts).
        /// </summary>
        [StringLength(128)]
        [Display(Name = "Drilldown ključ/filter")]
        public string DrilldownKey { get; set; } = string.Empty;

        /// <summary>
        /// Optional: note, regulatory info, or annotation.
        /// </summary>
        [StringLength(255)]
        [Display(Name = "Napomena")]
        public string Note { get; set; } = string.Empty;
    }
}
