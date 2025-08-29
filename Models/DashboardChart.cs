using System.Collections.Generic;

namespace YasGMP.Models
{
    /// <summary>
    /// Minimal chart model used by DashboardViewModel and the dashboard
    /// DatabaseService extensions. Extend as needed for your UI.
    /// </summary>
    public class DashboardChart
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        /// <summary>e.g., "line", "bar", "pie".</summary>
        public string ChartType { get; set; } = "line";

        /// <summary>Category labels for the X axis (or slices for pie).</summary>
        public IList<string> Labels { get; set; } = new List<string>();

        /// <summary>Series plotted in the chart.</summary>
        public IList<ChartSeries> Series { get; set; } = new List<ChartSeries>();

        public string? Subtitle { get; set; }
        public string? Unit { get; set; }
    }

    public class ChartSeries
    {
        public string Name { get; set; } = string.Empty;
        public IList<double> Data { get; set; } = new List<double>();
    }
}
