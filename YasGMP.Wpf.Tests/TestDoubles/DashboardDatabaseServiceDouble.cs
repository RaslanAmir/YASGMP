using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services;

/// <summary>
/// Test-focused DatabaseService partial that exposes in-memory KPI widget and chart collections.
/// </summary>
public partial class DatabaseService
{
    private const int DefaultRecentDashboardTake = 20;

    /// <summary>Gets the seeded KPI widgets returned to dashboard/cockpit view-models.</summary>
    public List<KpiWidget> KpiWidgets { get; } = new();

    /// <summary>Gets or sets the exception thrown when KPI widgets are requested.</summary>
    public Exception? KpiWidgetsException { get; set; }

    /// <summary>Gets the seeded dashboard chart points returned to dashboard view-models.</summary>
    public List<ChartData> DashboardCharts { get; } = new();

    /// <summary>Gets or sets the exception thrown when dashboard charts are requested.</summary>
    public Exception? DashboardChartsException { get; set; }

    /// <summary>Returns the configured KPI widgets.</summary>
    public Task<List<KpiWidget>> GetKpiWidgetsAsync(CancellationToken cancellationToken = default)
        => ResolveKpiWidgetsAsync();

    /// <summary>Returns the configured KPI widgets.</summary>
    public Task<List<KpiWidget>> GetKpiWidgetsAsync(int userId, string? role = null, CancellationToken cancellationToken = default)
        => ResolveKpiWidgetsAsync();

    /// <summary>Returns the configured KPI widgets.</summary>
    public Task<List<KpiWidget>> GetKpiWidgetsAsync(string role, CancellationToken cancellationToken = default)
        => ResolveKpiWidgetsAsync();

    /// <summary>Returns the configured KPI widgets.</summary>
    public Task<List<KpiWidget>> GetKpiWidgetsAsync(CancellationToken cancellationToken, string role)
        => GetKpiWidgetsAsync(role, cancellationToken);

    /// <summary>Returns the configured dashboard chart points.</summary>
    public Task<List<ChartData>> GetDashboardChartsAsync(CancellationToken cancellationToken = default)
        => ResolveDashboardChartsAsync();

    /// <summary>Returns the configured dashboard chart points.</summary>
    public Task<List<ChartData>> GetDashboardChartsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
        => ResolveDashboardChartsAsync();

    /// <summary>Returns the configured dashboard chart points.</summary>
    public Task<List<ChartData>> GetDashboardChartsAsync(string range, CancellationToken cancellationToken = default)
        => ResolveDashboardChartsAsync();

    /// <summary>Returns the configured dashboard chart points.</summary>
    public Task<List<ChartData>> GetDashboardChartsAsync(CancellationToken cancellationToken, string range)
        => GetDashboardChartsAsync(range, cancellationToken);

    /// <summary>Returns the configured dashboard events using the default take.</summary>
    public Task<List<DashboardEvent>> GetRecentDashboardEventsAsync(CancellationToken cancellationToken = default)
        => GetRecentDashboardEventsAsync(DefaultRecentDashboardTake, cancellationToken);

    /// <summary>Returns the configured dashboard events for string-based filters.</summary>
    public Task<List<DashboardEvent>> GetRecentDashboardEventsAsync(string takeOrFilter, CancellationToken cancellationToken = default)
        => GetRecentDashboardEventsAsync(DefaultRecentDashboardTake, cancellationToken);

    private Task<List<KpiWidget>> ResolveKpiWidgetsAsync()
    {
        if (KpiWidgetsException is not null)
        {
            throw KpiWidgetsException;
        }

        return Task.FromResult(KpiWidgets.Select(CloneKpiWidget).ToList());
    }

    private Task<List<ChartData>> ResolveDashboardChartsAsync()
    {
        if (DashboardChartsException is not null)
        {
            throw DashboardChartsException;
        }

        return Task.FromResult(DashboardCharts.Select(CloneChartData).ToList());
    }

    private static KpiWidget CloneKpiWidget(KpiWidget source)
        => new()
        {
            Title = source.Title,
            Value = source.Value,
            ValueText = source.ValueText,
            Unit = source.Unit,
            Icon = source.Icon,
            Color = source.Color,
            Trend = source.Trend,
            DrilldownKey = source.DrilldownKey,
            Note = source.Note,
            LastUpdated = source.LastUpdated,
            IsAlert = source.IsAlert
        };

    private static ChartData CloneChartData(ChartData source)
        => new()
        {
            Label = source.Label,
            Value = source.Value,
            Group = source.Group,
            SecondaryValue = source.SecondaryValue,
            Series = source.Series,
            Color = source.Color,
            Timestamp = source.Timestamp,
            DrilldownKey = source.DrilldownKey,
            Note = source.Note
        };
}
