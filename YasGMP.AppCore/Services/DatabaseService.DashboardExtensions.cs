// ==============================================================================
// File: Services/DatabaseService.DashboardExtensions.cs
// Purpose: Adds missing dashboard-related methods as *extension methods* on
//          DatabaseService so DashboardViewModel compiles.
//          Replace the TEMP bodies with your real DB calls when ready.
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
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
            var now = DateTime.UtcNow;
            return GetKpiWidgetsInternalAsync(db, now.AddDays(-DefaultRangeDays), now, null, null, cancellationToken);
        }

        /// <summary>Overload with user context (role first).</summary>
        public static Task<List<KpiWidget>> GetKpiWidgetsAsync(
            this DatabaseService db,
            int userId,
            string? role = null,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var normalizedRole = NormalizeRole(role);
            return GetKpiWidgetsInternalAsync(db, now.AddDays(-DefaultRangeDays), now, userId, normalizedRole, cancellationToken);
        }

        /// <summary>Overload to match calls like: _db.GetKpiWidgetsAsync("admin")</summary>
        public static Task<List<KpiWidget>> GetKpiWidgetsAsync(
            this DatabaseService db,
            string role,
            CancellationToken cancellationToken = default)
        {
            var (rangeToken, roleToken, _) = ParseFilterTokens(role);
            var normalizedRole = NormalizeRole(roleToken);
            var now = DateTime.UtcNow;
            var (from, to) = ResolveRange(rangeToken, now.AddDays(-DefaultRangeDays), now);
            return GetKpiWidgetsInternalAsync(db, from, to, null, normalizedRole, cancellationToken);
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
            var now = DateTime.UtcNow;
            return GetDashboardChartsInternalAsync(db, now.AddDays(-DefaultRangeDays), now, null, null, cancellationToken);
        }

        /// <summary>Overload with explicit date range.</summary>
        public static Task<List<ChartData>> GetDashboardChartsAsync(
            this DatabaseService db,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            var (normalizedFrom, normalizedTo) = NormalizeExplicitRange(from, to);
            return GetDashboardChartsInternalAsync(db, normalizedFrom, normalizedTo, null, null, cancellationToken);
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
            var now = DateTime.UtcNow;
            return GetRecentDashboardEventsInternalAsync(db, DefaultRecentEventTake, now.AddDays(-DefaultRangeDays), now, null, null, cancellationToken);
        }

        /// <summary>Overload with explicit take/count.</summary>
        public static Task<List<DashboardEvent>> GetRecentDashboardEventsAsync(
            this DatabaseService db,
            int take,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var safeTake = take > 0 ? take : DefaultRecentEventTake;
            return GetRecentDashboardEventsInternalAsync(db, safeTake, now.AddDays(-DefaultRangeDays), now, null, null, cancellationToken);
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

            var (rangeToken, roleToken, userToken) = ParseFilterTokens(takeOrFilter);
            var normalizedRole = NormalizeRole(roleToken);
            int? userId = userToken;

            var now = DateTime.UtcNow;
            var (from, to) = ResolveRange(rangeToken, now.AddDays(-DefaultRangeDays), now);
            return GetRecentDashboardEventsInternalAsync(db, DefaultRecentEventTake, from, to, userId, normalizedRole, cancellationToken);
        }

        /// <summary>Mirror overload for call sites that pass (token, "25").</summary>
        public static Task<List<DashboardEvent>> GetRecentDashboardEventsAsync(
            this DatabaseService db,
            CancellationToken cancellationToken,
            string takeOrFilter)
        {
            return db.GetRecentDashboardEventsAsync(takeOrFilter, cancellationToken);
        }

        private const int DefaultRangeDays = 30;
        private const int DefaultRecentEventTake = 20;

        private static async Task<List<KpiWidget>> GetKpiWidgetsInternalAsync(
            DatabaseService db,
            DateTime? from,
            DateTime? to,
            int? userId,
            string? role,
            CancellationToken token)
        {
            const string sql = @"SELECT
    'Open Work Orders' AS title,
    COUNT(*) AS value,
    CONCAT('Due soon: ', SUM(CASE WHEN w.due_date IS NOT NULL AND w.due_date >= UTC_TIMESTAMP() AND w.due_date <= UTC_TIMESTAMP() + INTERVAL 7 DAY THEN 1 ELSE 0 END)) AS value_text,
    NULL AS unit,
    'clipboard-list' AS icon,
    '#0d6efd' AS color,
    CASE WHEN COUNT(*) > 0 THEN 'up' ELSE 'neutral' END AS trend,
    CASE WHEN SUM(CASE WHEN w.due_date IS NOT NULL AND w.due_date < UTC_TIMESTAMP() THEN 1 ELSE 0 END) > 0 THEN 1 ELSE 0 END AS is_alert,
    UTC_TIMESTAMP() AS last_updated,
    'work_orders:open' AS drilldown_key,
    CONCAT('Overdue: ', SUM(CASE WHEN w.due_date IS NOT NULL AND w.due_date < UTC_TIMESTAMP() THEN 1 ELSE 0 END)) AS note
FROM work_orders w
LEFT JOIN users u_assign ON u_assign.id = w.assigned_to_id
LEFT JOIN users u_create ON u_create.id = w.created_by_id
LEFT JOIN users u_request ON u_request.id = w.requested_by_id
WHERE (w.status IS NULL OR LOWER(w.status) NOT IN ('closed','completed','cancelled','canceled'))
  AND (@from IS NULL OR w.date_open >= @from)
  AND (@to IS NULL OR w.date_open <= @to)
  AND (@userId IS NULL OR w.assigned_to_id = @userId OR w.created_by_id = @userId OR w.requested_by_id = @userId)
  AND (@role IS NULL OR LOWER(u_assign.role) = @role OR LOWER(u_create.role) = @role OR LOWER(u_request.role) = @role)

UNION ALL

SELECT
    'Overdue Work Orders' AS title,
    COUNT(*) AS value,
    CONCAT('Max delay: ', IFNULL(MAX(DATEDIFF(UTC_TIMESTAMP(), w.due_date)), 0), ' d') AS value_text,
    NULL AS unit,
    'alarm' AS icon,
    '#dc3545' AS color,
    CASE WHEN COUNT(*) > 0 THEN 'up' ELSE 'down' END AS trend,
    CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END AS is_alert,
    UTC_TIMESTAMP() AS last_updated,
    'work_orders:overdue' AS drilldown_key,
    CONCAT('Oldest opened: ', DATE_FORMAT(MIN(w.date_open), '%Y-%m-%d')) AS note
FROM work_orders w
LEFT JOIN users u_assign2 ON u_assign2.id = w.assigned_to_id
LEFT JOIN users u_create2 ON u_create2.id = w.created_by_id
LEFT JOIN users u_request2 ON u_request2.id = w.requested_by_id
WHERE (w.status IS NULL OR LOWER(w.status) NOT IN ('closed','completed','cancelled','canceled'))
  AND w.due_date IS NOT NULL AND w.due_date < UTC_TIMESTAMP()
  AND (@from IS NULL OR w.date_open >= @from)
  AND (@to IS NULL OR w.date_open <= @to)
  AND (@userId IS NULL OR w.assigned_to_id = @userId OR w.created_by_id = @userId OR w.requested_by_id = @userId)
  AND (@role IS NULL OR LOWER(u_assign2.role) = @role OR LOWER(u_create2.role) = @role OR LOWER(u_request2.role) = @role)

UNION ALL

SELECT
    'Active Incidents' AS title,
    COUNT(*) AS value,
    CONCAT('Critical: ', SUM(CASE WHEN LOWER(i.priority) = 'critical' THEN 1 ELSE 0 END)) AS value_text,
    NULL AS unit,
    'alert-triangle' AS icon,
    '#ffc107' AS color,
    CASE WHEN SUM(CASE WHEN LOWER(i.priority) IN ('critical','high') THEN 1 ELSE 0 END) > 0 THEN 'up' ELSE 'neutral' END AS trend,
    CASE WHEN SUM(CASE WHEN LOWER(i.priority) IN ('critical','high') THEN 1 ELSE 0 END) > 0 THEN 1 ELSE 0 END AS is_alert,
    UTC_TIMESTAMP() AS last_updated,
    'incidents:active' AS drilldown_key,
    CONCAT('Open CAPA links: ', SUM(CASE WHEN i.capa_case_id IS NOT NULL THEN 1 ELSE 0 END)) AS note
FROM incidents i
LEFT JOIN users ui_assign ON ui_assign.id = i.assigned_to_id
LEFT JOIN users ui_report ON ui_report.id = i.reported_by_id
WHERE (i.is_deleted = 0 OR i.is_deleted IS NULL)
  AND (i.status IS NULL OR LOWER(i.status) NOT IN ('closed','resolved','completed'))
  AND (@from IS NULL OR i.detected_at >= @from)
  AND (@to IS NULL OR i.detected_at <= @to)
  AND (@userId IS NULL OR i.assigned_to_id = @userId OR i.reported_by_id = @userId)
  AND (@role IS NULL OR LOWER(ui_assign.role) = @role OR LOWER(ui_report.role) = @role)

UNION ALL

SELECT
    'Open CAPAs' AS title,
    COUNT(*) AS value,
    CONCAT('Pending approval: ', SUM(CASE WHEN c.approved = 0 OR c.approved IS NULL THEN 1 ELSE 0 END)) AS value_text,
    NULL AS unit,
    'clipboard-check' AS icon,
    '#20c997' AS color,
    CASE WHEN SUM(CASE WHEN c.approved = 0 OR c.approved IS NULL THEN 1 ELSE 0 END) > 0 THEN 'neutral' ELSE 'down' END AS trend,
    CASE WHEN SUM(CASE WHEN c.approved = 0 OR c.approved IS NULL THEN 1 ELSE 0 END) > 0 THEN 1 ELSE 0 END AS is_alert,
    UTC_TIMESTAMP() AS last_updated,
    'capa_cases:open' AS drilldown_key,
    CONCAT('Overdue: ', SUM(CASE WHEN c.date_close IS NULL AND c.date_open < UTC_TIMESTAMP() - INTERVAL 90 DAY THEN 1 ELSE 0 END)) AS note
FROM capa_cases c
LEFT JOIN users uc_assign ON uc_assign.id = c.assigned_to_id
LEFT JOIN users uc_mod ON uc_mod.id = c.last_modified_by_id
WHERE (c.status IS NULL OR LOWER(c.status) NOT IN ('closed','completed','resolved'))
  AND (c.is_deleted = 0 OR c.is_deleted IS NULL)
  AND (@from IS NULL OR c.date_open >= @from)
  AND (@to IS NULL OR c.date_open <= @to)
  AND (@userId IS NULL OR c.assigned_to_id = @userId OR c.approved_by_id = @userId OR c.last_modified_by_id = @userId)
  AND (@role IS NULL OR LOWER(uc_assign.role) = @role OR LOWER(uc_mod.role) = @role);";

            var parameters = new List<MySqlParameter>
            {
                CreateDateTimeParameter("@from", from),
                CreateDateTimeParameter("@to", to),
                CreateNullableIntParameter("@userId", userId),
                CreateStringParameter("@role", role)
            };

            var table = await db.ExecuteSelectAsync(sql, parameters, token).ConfigureAwait(false);
            var widgets = new List<KpiWidget>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                var widget = new KpiWidget
                {
                    Title = GetString(row, "title") ?? string.Empty,
                    Value = GetDecimal(row, "value"),
                    ValueText = GetString(row, "value_text"),
                    Unit = GetString(row, "unit"),
                    Icon = GetString(row, "icon"),
                    Color = GetString(row, "color"),
                    Trend = GetString(row, "trend"),
                    DrilldownKey = GetString(row, "drilldown_key"),
                    Note = GetString(row, "note"),
                    LastUpdated = GetDateTime(row, "last_updated") ?? DateTime.UtcNow,
                    IsAlert = GetBoolean(row, "is_alert")
                };
                widgets.Add(widget);
            }

            return widgets;
        }

        private static async Task<List<ChartData>> GetDashboardChartsInternalAsync(
            DatabaseService db,
            DateTime? from,
            DateTime? to,
            int? userId,
            string? role,
            CancellationToken token)
        {
            const string sql = @"SELECT
    DATE(w.date_open) AS bucket_date,
    DATE_FORMAT(DATE(w.date_open), '%Y-%m-%d') AS label,
    COUNT(*) AS value,
    'Opened' AS series,
    'WorkOrders' AS group_name,
    '#0d6efd' AS color,
    'work_orders:opened' AS drilldown_key,
    NULL AS note,
    NULL AS secondary_value
FROM work_orders w
LEFT JOIN users u_assign ON u_assign.id = w.assigned_to_id
LEFT JOIN users u_create ON u_create.id = w.created_by_id
LEFT JOIN users u_request ON u_request.id = w.requested_by_id
WHERE (@from IS NULL OR w.date_open >= @from)
  AND (@to IS NULL OR w.date_open <= @to)
  AND (@userId IS NULL OR w.assigned_to_id = @userId OR w.created_by_id = @userId OR w.requested_by_id = @userId)
  AND (@role IS NULL OR LOWER(u_assign.role) = @role OR LOWER(u_create.role) = @role OR LOWER(u_request.role) = @role)
GROUP BY DATE(w.date_open)

UNION ALL

SELECT
    DATE(w.date_close) AS bucket_date,
    DATE_FORMAT(DATE(w.date_close), '%Y-%m-%d') AS label,
    COUNT(*) AS value,
    'Closed' AS series,
    'WorkOrders' AS group_name,
    '#198754' AS color,
    'work_orders:closed' AS drilldown_key,
    NULL AS note,
    NULL AS secondary_value
FROM work_orders w
LEFT JOIN users u_assign2 ON u_assign2.id = w.assigned_to_id
LEFT JOIN users u_create2 ON u_create2.id = w.created_by_id
LEFT JOIN users u_request2 ON u_request2.id = w.requested_by_id
WHERE w.date_close IS NOT NULL
  AND (@from IS NULL OR w.date_close >= @from)
  AND (@to IS NULL OR w.date_close <= @to)
  AND (@userId IS NULL OR w.assigned_to_id = @userId OR w.created_by_id = @userId OR w.requested_by_id = @userId)
  AND (@role IS NULL OR LOWER(u_assign2.role) = @role OR LOWER(u_create2.role) = @role OR LOWER(u_request2.role) = @role)
GROUP BY DATE(w.date_close)

UNION ALL

SELECT
    DATE(i.detected_at) AS bucket_date,
    DATE_FORMAT(DATE(i.detected_at), '%Y-%m-%d') AS label,
    COUNT(*) AS value,
    UPPER(COALESCE(i.priority, 'Normal')) AS series,
    'Incidents' AS group_name,
    CASE
        WHEN LOWER(COALESCE(i.priority, 'normal')) = 'critical' THEN '#dc3545'
        WHEN LOWER(COALESCE(i.priority, 'normal')) = 'high' THEN '#fd7e14'
        WHEN LOWER(COALESCE(i.priority, 'normal')) = 'medium' THEN '#ffc107'
        ELSE '#0dcaf0'
    END AS color,
    'incidents:priority' AS drilldown_key,
    CONCAT('Status: ', COALESCE(i.status, 'n/a')) AS note,
    NULL AS secondary_value
FROM incidents i
LEFT JOIN users ui_assign ON ui_assign.id = i.assigned_to_id
LEFT JOIN users ui_report ON ui_report.id = i.reported_by_id
WHERE (i.is_deleted = 0 OR i.is_deleted IS NULL)
  AND (@from IS NULL OR i.detected_at >= @from)
  AND (@to IS NULL OR i.detected_at <= @to)
  AND (@userId IS NULL OR i.assigned_to_id = @userId OR i.reported_by_id = @userId)
  AND (@role IS NULL OR LOWER(ui_assign.role) = @role OR LOWER(ui_report.role) = @role)
GROUP BY DATE(i.detected_at), UPPER(COALESCE(i.priority, 'Normal'))
ORDER BY bucket_date, series;";

            var parameters = new List<MySqlParameter>
            {
                CreateDateTimeParameter("@from", from),
                CreateDateTimeParameter("@to", to),
                CreateNullableIntParameter("@userId", userId),
                CreateStringParameter("@role", role)
            };

            var table = await db.ExecuteSelectAsync(sql, parameters, token).ConfigureAwait(false);
            var charts = new List<ChartData>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                var chart = new ChartData
                {
                    Label = GetString(row, "label") ?? string.Empty,
                    Value = GetDecimal(row, "value"),
                    Group = GetString(row, "group_name") ?? string.Empty,
                    SecondaryValue = GetNullableDecimal(row, "secondary_value"),
                    Series = GetString(row, "series") ?? string.Empty,
                    Color = GetString(row, "color") ?? string.Empty,
                    Timestamp = GetDateTime(row, "bucket_date"),
                    DrilldownKey = GetString(row, "drilldown_key") ?? string.Empty,
                    Note = GetString(row, "note") ?? string.Empty
                };
                charts.Add(chart);
            }

            return charts;
        }

        private static async Task<List<DashboardEvent>> GetRecentDashboardEventsInternalAsync(
            DatabaseService db,
            int take,
            DateTime? from,
            DateTime? to,
            int? userId,
            string? role,
            CancellationToken token)
        {
            const string sql = @"SELECT
    sel.id,
    sel.event_time,
    sel.event_type,
    sel.description,
    sel.severity,
    sel.user_id,
    sel.table_name,
    sel.record_id,
    sel.device_info,
    sel.session_id,
    sel.details AS note,
    CASE
        WHEN LOWER(sel.severity) IN ('critical', 'error') THEN 'alert-circle'
        WHEN LOWER(sel.severity) IN ('warning', 'warn') THEN 'alert-triangle'
        WHEN LOWER(sel.event_type) LIKE 'login%' THEN 'log-in'
        WHEN LOWER(sel.event_type) LIKE 'logout%' THEN 'log-out'
        ELSE 'info'
    END AS icon,
    CASE WHEN sel.processed = 0 THEN 1 ELSE 0 END AS is_unread,
    CONCAT_WS(':', sel.table_name, sel.record_id) AS drilldown_key
FROM system_event_log sel
LEFT JOIN users u ON u.id = sel.user_id
WHERE (@from IS NULL OR sel.event_time >= @from)
  AND (@to IS NULL OR sel.event_time <= @to)
  AND (@userId IS NULL OR sel.user_id = @userId)
  AND (@role IS NULL OR LOWER(u.role) = @role)
ORDER BY sel.event_time DESC
LIMIT @take;";

            var parameters = new List<MySqlParameter>
            {
                CreateDateTimeParameter("@from", from),
                CreateDateTimeParameter("@to", to),
                CreateNullableIntParameter("@userId", userId),
                CreateStringParameter("@role", role),
                CreateNullableIntParameter("@take", take)
            };

            var table = await db.ExecuteSelectAsync(sql, parameters, token).ConfigureAwait(false);
            var events = new List<DashboardEvent>(table.Rows.Count);
            foreach (DataRow row in table.Rows)
            {
                var evt = new DashboardEvent
                {
                    Id = GetInt(row, "id"),
                    EventType = GetString(row, "event_type") ?? string.Empty,
                    Description = GetString(row, "description"),
                    Timestamp = GetDateTime(row, "event_time") ?? DateTime.UtcNow,
                    Severity = GetString(row, "severity") ?? "info",
                    UserId = GetNullableInt(row, "user_id"),
                    RelatedModule = GetString(row, "table_name"),
                    RelatedRecordId = GetNullableInt(row, "record_id"),
                    Icon = GetString(row, "icon"),
                    IsUnread = GetBoolean(row, "is_unread"),
                    DrilldownKey = GetString(row, "drilldown_key"),
                    Note = BuildEventNote(row)
                };
                events.Add(evt);
            }

            return events;
        }

        private static string? BuildEventNote(DataRow row)
        {
            var components = new List<string?>
            {
                GetString(row, "note"),
                GetString(row, "device_info"),
                GetString(row, "session_id")
            };

            components.RemoveAll(s => string.IsNullOrWhiteSpace(s));
            if (components.Count == 0) return null;
            return string.Join(" | ", components);
        }

        private static (DateTime? From, DateTime? To) ResolveRange(string? rangeToken, DateTime? fallbackFrom, DateTime? fallbackTo)
        {
            if (TryResolveDateRange(rangeToken, out var parsedFrom, out var parsedTo))
            {
                return (parsedFrom, parsedTo);
            }

            return (fallbackFrom, fallbackTo);
        }

        private static bool TryResolveDateRange(string? value, out DateTime? from, out DateTime? to)
        {
            from = null;
            to = null;
            if (string.IsNullOrWhiteSpace(value)) return false;

            var normalized = value.Trim().ToLowerInvariant();
            var now = DateTime.UtcNow;

            switch (normalized)
            {
                case "today":
                    from = now.Date;
                    to = now;
                    return true;
                case "yesterday":
                    to = now.Date;
                    from = to.Value.AddDays(-1);
                    return true;
                case "last7":
                case "last7days":
                    from = now.AddDays(-7);
                    to = now;
                    return true;
                case "last30":
                case "last30days":
                    from = now.AddDays(-30);
                    to = now;
                    return true;
                case "last90":
                case "last90days":
                    from = now.AddDays(-90);
                    to = now;
                    return true;
                case "thismonth":
                    from = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    to = now;
                    return true;
                case "thisyear":
                    from = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    to = now;
                    return true;
                case "alltime":
                    from = null;
                    to = null;
                    return true;
                default:
                    if (normalized.StartsWith("days:", StringComparison.Ordinal))
                    {
                        var span = normalized["days:".Length..];
                        if (int.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days) && days > 0)
                        {
                            from = now.AddDays(-days);
                            to = now;
                            return true;
                        }
                    }
                    break;
            }

            return false;
        }

        private static (DateTime From, DateTime To) NormalizeExplicitRange(DateTime from, DateTime to)
        {
            var start = NormalizeDateTime(from);
            var end = NormalizeDateTime(to);
            if (end < start)
            {
                (start, end) = (end, start);
            }
            return (start, end);
        }

        private static DateTime NormalizeDateTime(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Local => value.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                _ => value
            };
        }

        private static (string? Range, string? Role, int? UserId) ParseFilterTokens(string? filter)
        {
            string? range = filter;
            string? role = null;
            int? userId = null;

            if (string.IsNullOrWhiteSpace(filter))
            {
                return (range, role, userId);
            }

            var tokens = filter.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 1)
            {
                ExtractToken(tokens[0], ref range, ref role, ref userId);
            }
            else
            {
                range = null;
                foreach (var token in tokens)
                {
                    ExtractToken(token, ref range, ref role, ref userId);
                }
            }

            if (string.IsNullOrWhiteSpace(role) && !string.IsNullOrWhiteSpace(range))
            {
                if (!TryResolveDateRange(range, out _, out _))
                {
                    role = range;
                    range = null;
                }
            }

            return (range, role, userId);
        }

        private static void ExtractToken(string token, ref string? range, ref string? role, ref int? userId)
        {
            if (string.IsNullOrWhiteSpace(token)) return;

            var trimmed = token.Trim();
            if (trimmed.StartsWith("role", StringComparison.OrdinalIgnoreCase))
            {
                role = ExtractValue(trimmed);
            }
            else if (trimmed.StartsWith("user", StringComparison.OrdinalIgnoreCase))
            {
                var value = ExtractValue(trimmed);
                if (!string.IsNullOrWhiteSpace(value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    userId = parsed;
                }
            }
            else
            {
                range = trimmed;
            }
        }

        private static string? ExtractValue(string token)
        {
            var separatorIndex = token.IndexOfAny(new[] { ':', '=' });
            if (separatorIndex < 0 || separatorIndex >= token.Length - 1)
            {
                return null;
            }

            return token[(separatorIndex + 1)..].Trim();
        }

        private static string? NormalizeRole(string? role)
        {
            return string.IsNullOrWhiteSpace(role) ? null : role.Trim().ToLowerInvariant();
        }

        private static string? GetString(DataRow row, string column)
        {
            return row.Table.Columns.Contains(column) && row[column] != DBNull.Value
                ? Convert.ToString(row[column], CultureInfo.InvariantCulture)
                : null;
        }

        private static int GetInt(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value) return 0;
            return Convert.ToInt32(row[column], CultureInfo.InvariantCulture);
        }

        private static int? GetNullableInt(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value) return null;
            return Convert.ToInt32(row[column], CultureInfo.InvariantCulture);
        }

        private static decimal GetDecimal(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value) return 0m;
            return Convert.ToDecimal(row[column], CultureInfo.InvariantCulture);
        }

        private static decimal? GetNullableDecimal(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value) return null;
            return Convert.ToDecimal(row[column], CultureInfo.InvariantCulture);
        }

        private static DateTime? GetDateTime(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value) return null;
            var value = row[column];
            return value switch
            {
                DateTime dt => NormalizeDateTime(dt),
                _ => DateTime.SpecifyKind(Convert.ToDateTime(value, CultureInfo.InvariantCulture), DateTimeKind.Utc)
            };
        }

        private static bool GetBoolean(DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == DBNull.Value) return false;
            var value = row[column];
            return value switch
            {
                bool b => b,
                sbyte sb => sb != 0,
                byte b => b != 0,
                short s => s != 0,
                int i => i != 0,
                long l => l != 0,
                string str => str.Equals("true", StringComparison.OrdinalIgnoreCase) || str == "1",
                _ => Convert.ToInt32(value, CultureInfo.InvariantCulture) != 0
            };
        }

        private static MySqlParameter CreateDateTimeParameter(string name, DateTime? value)
        {
            return new MySqlParameter(name, MySqlDbType.DateTime)
            {
                Value = value.HasValue ? NormalizeDateTime(value.Value) : DBNull.Value
            };
        }

        private static MySqlParameter CreateNullableIntParameter(string name, int? value)
        {
            return new MySqlParameter(name, MySqlDbType.Int32)
            {
                Value = value.HasValue ? value.Value : DBNull.Value
            };
        }

        private static MySqlParameter CreateStringParameter(string name, string? value)
        {
            return new MySqlParameter(name, MySqlDbType.VarChar)
            {
                Value = string.IsNullOrWhiteSpace(value) ? DBNull.Value : value
            };
        }
    }
}

