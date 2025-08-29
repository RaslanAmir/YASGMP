// ==============================================================================
// File: Services/DatabaseService.DashboardExtensions.cs
// Purpose: Adds missing dashboard-related methods as *extension methods* on
//          DatabaseService so DashboardViewModel compiles.
//          Replace the TEMP bodies with your real DB calls when ready.
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Extension methods for dashboard widgets/charts/events used by DashboardViewModel.
    /// </summary>
    public static class DatabaseServiceDashboardExtensions
    {
        // ----------------------------------------------------------------------
        // KPI WIDGETS
        // ----------------------------------------------------------------------

        /// <summary>Returns KPI widgets for the dashboard.</summary>
        public static Task<List<KpiWidget>> GetKpiWidgetsAsync(
            this DatabaseService db,
            CancellationToken cancellationToken = default)
        {
            // TODO: Replace with real query
            return Task.FromResult(new List<KpiWidget>());
        }

        /// <summary>Overload with user context (role first).</summary>
        public static Task<List<KpiWidget>> GetKpiWidgetsAsync(
            this DatabaseService db,
            int userId,
            string? role = null,
            CancellationToken cancellationToken = default)
        {
            // TODO: Replace with real query
            return Task.FromResult(new List<KpiWidget>());
        }

        /// <summary>Overload to match calls like: _db.GetKpiWidgetsAsync("admin")</summary>
        public static Task<List<KpiWidget>> GetKpiWidgetsAsync(
            this DatabaseService db,
            string role,
            CancellationToken cancellationToken = default)
        {
            _ = role;
            return Task.FromResult(new List<KpiWidget>());
        }

        /// <summary>Mirror overload: some call sites pass the token first.</summary>
        public static Task<List<KpiWidget>> GetKpiWidgetsAsync(
            this DatabaseService db,
            CancellationToken cancellationToken,
            string role)
        {
            return db.GetKpiWidgetsAsync(role, cancellationToken);
        }

        // ----------------------------------------------------------------------
        // CHARTS  (return ChartData to match DashboardViewModel expectations)
        // ----------------------------------------------------------------------

        /// <summary>Returns chart data for the dashboard.</summary>
        public static Task<List<ChartData>> GetDashboardChartsAsync(
            this DatabaseService db,
            CancellationToken cancellationToken = default)
        {
            // TODO: Replace with real query
            return Task.FromResult(new List<ChartData>());
        }

        /// <summary>Overload with explicit date range.</summary>
        public static Task<List<ChartData>> GetDashboardChartsAsync(
            this DatabaseService db,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            // TODO: Replace with real query
            _ = from; _ = to;
            return Task.FromResult(new List<ChartData>());
        }

        /// <summary>
        /// Overload to match calls like: _db.GetDashboardChartsAsync("last30")
        /// Parses common ranges and routes to the date-range overload.
        /// </summary>
        public static Task<List<ChartData>> GetDashboardChartsAsync(
            this DatabaseService db,
            string range,
            CancellationToken cancellationToken = default)
        {
            DateTime to = DateTime.UtcNow;
            DateTime from = to.AddDays(-30);

            if (!string.IsNullOrWhiteSpace(range))
            {
                var r = range.Trim().ToLowerInvariant();
                if (r is "today") { from = to.Date; }
                else if (r is "yesterday") { to = to.Date; from = to.AddDays(-1); }
                else if (r is "last7" or "last7days") { from = to.AddDays(-7); }
                else if (r is "last30" or "last30days") { from = to.AddDays(-30); }
                else if (r is "last90" or "last90days") { from = to.AddDays(-90); }
                else if (r.StartsWith("days:", StringComparison.Ordinal) &&
                         int.TryParse(r["days:".Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var d) &&
                         d > 0)
                {
                    from = to.AddDays(-d);
                }
            }

            return db.GetDashboardChartsAsync(from, to, cancellationToken);
        }

        /// <summary>Mirror overload for call sites that pass (token, "last30").</summary>
        public static Task<List<ChartData>> GetDashboardChartsAsync(
            this DatabaseService db,
            CancellationToken cancellationToken,
            string range)
        {
            return db.GetDashboardChartsAsync(range, cancellationToken);
        }

        // ----------------------------------------------------------------------
        // RECENT EVENTS / ACTIVITY FEED
        // ----------------------------------------------------------------------

        /// <summary>Returns the most recent dashboard events / activity items.</summary>
        public static Task<List<DashboardEvent>> GetRecentDashboardEventsAsync(
            this DatabaseService db,
            CancellationToken cancellationToken = default)
        {
            // TODO: Replace with real query
            return Task.FromResult(new List<DashboardEvent>());
        }

        /// <summary>Overload with explicit take/count.</summary>
        public static Task<List<DashboardEvent>> GetRecentDashboardEventsAsync(
            this DatabaseService db,
            int take,
            CancellationToken cancellationToken = default)
        {
            // TODO: Replace with real query
            _ = take;
            return Task.FromResult(new List<DashboardEvent>());
        }

        /// <summary>Overload to match calls like: _db.GetRecentDashboardEventsAsync("25")</summary>
        public static Task<List<DashboardEvent>> GetRecentDashboardEventsAsync(
            this DatabaseService db,
            string takeOrFilter,
            CancellationToken cancellationToken = default)
        {
            if (int.TryParse(takeOrFilter, NumberStyles.Integer, CultureInfo.InvariantCulture, out var take) && take > 0)
            {
                return db.GetRecentDashboardEventsAsync(take, cancellationToken);
            }

            // If a string filter is used, route it appropriately here.
            return db.GetRecentDashboardEventsAsync(cancellationToken);
        }

        /// <summary>Mirror overload for call sites that pass (token, "25").</summary>
        public static Task<List<DashboardEvent>> GetRecentDashboardEventsAsync(
            this DatabaseService db,
            CancellationToken cancellationToken,
            string takeOrFilter)
        {
            return db.GetRecentDashboardEventsAsync(takeOrFilter, cancellationToken);
        }
    }
}
