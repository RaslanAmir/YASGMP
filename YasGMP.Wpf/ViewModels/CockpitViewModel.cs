using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels
{
    /// <summary>Simple cockpit summary displayed in the bottom anchorable.</summary>
    public partial class CockpitViewModel : AnchorableViewModel
    {
        private readonly DatabaseService? _database;
        private readonly ILocalizationService _localization;
        private readonly IUserSession? _userSession;
        private readonly UserSession? _mutableSession;
        private CancellationTokenSource? _refreshCancellation;
        private CockpitStatusKind _statusKind = CockpitStatusKind.None;
        private string? _statusErrorDetail;

        /// <summary>
        /// Initializes a new instance of the CockpitViewModel class using the shared service locator.
        /// </summary>
        public CockpitViewModel()
            : this(
                ServiceLocator.GetService<DatabaseService>(),
                ServiceLocator.GetService<ILocalizationService>(),
                ServiceLocator.GetService<IUserSession>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the CockpitViewModel class.
        /// </summary>
        public CockpitViewModel(
            DatabaseService? database,
            ILocalizationService? localization,
            IUserSession? userSession)
        {
            _database = database;
            _localization = localization ?? new LocalizationService();
            _userSession = userSession;
            _mutableSession = userSession as UserSession;

            ContentId = "YasGmp.Shell.Cockpit";

            Metrics = new ObservableCollection<CockpitMetricViewModel>();
            Notices = new ObservableCollection<CockpitNoticeViewModel>();

            Metrics.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(HasMetrics));
                OnPropertyChanged(nameof(ShowMetricsPlaceholder));
                OnPropertyChanged(nameof(HasAnyData));
            };

            Notices.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(HasNotices));
                OnPropertyChanged(nameof(ShowNoticesPlaceholder));
                OnPropertyChanged(nameof(HasAnyData));
            };

            _localization.LanguageChanged += OnLanguageChanged;
            _mutableSession?.SessionChanged += OnSessionChanged;

            RefreshLocalizedContent();

            if (IsDesignMode || _database is null || _userSession is null)
            {
                PopulateDesignData();
                return;
            }

            _refreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsLoading);
            _ = RefreshAsync();
        }

        /// <summary>Collection of KPI widgets projected into the cockpit pane.</summary>
        public ObservableCollection<CockpitMetricViewModel> Metrics { get; }

        /// <summary>Collection of recent dashboard notices projected into the cockpit pane.</summary>
        public ObservableCollection<CockpitNoticeViewModel> Notices { get; }

        private readonly AsyncRelayCommand? _refreshCommand;

        /// <summary>Command exposed for manual refresh triggers.</summary>
        public IAsyncRelayCommand? RefreshCommand => _refreshCommand;

        /// <summary>Indicates whether metrics are available.</summary>
        public bool HasMetrics => Metrics.Count > 0;

        /// <summary>Indicates whether notices are available.</summary>
        public bool HasNotices => Notices.Count > 0;

        /// <summary>Indicates whether any metrics or notices have been loaded.</summary>
        public bool HasAnyData => HasMetrics || HasNotices;

        /// <summary>Shows the metrics placeholder when no data is present.</summary>
        public bool ShowMetricsPlaceholder => !IsLoading && !HasMetrics && !HasError;

        /// <summary>Shows the notices placeholder when no data is present.</summary>
        public bool ShowNoticesPlaceholder => !IsLoading && !HasNotices && !HasError;

        /// <summary>Localized message describing the current load status.</summary>
        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>Indicates whether an async refresh is executing.</summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>Tracks whether the last refresh failed.</summary>
        [ObservableProperty]
        private bool _hasError;

        partial void OnIsLoadingChanged(bool value)
        {
            _refreshCommand?.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(ShowMetricsPlaceholder));
            OnPropertyChanged(nameof(ShowNoticesPlaceholder));
        }

        private async Task RefreshAsync()
        {
            if (_database is null)
            {
                return;
            }

            var newToken = new CancellationTokenSource();
            var previous = Interlocked.Exchange(ref _refreshCancellation, newToken);
            previous?.Cancel();
            previous?.Dispose();

            var token = newToken.Token;

            try
            {
                SetStatus(CockpitStatusKind.Loading);
                IsLoading = true;

                Task<List<KpiWidget>> metricsTask = _userSession?.UserId is int userId
                    ? _database.GetKpiWidgetsAsync(userId, cancellationToken: token)
                    : _database.GetKpiWidgetsAsync(token);

                Task<List<DashboardEvent>> noticesTask = _database.GetRecentDashboardEventsAsync(token);

                await Task.WhenAll(metricsTask, noticesTask);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                UpdateMetrics(metricsTask.Result);
                UpdateNotices(noticesTask.Result);

                if (!HasAnyData)
                {
                    SetStatus(CockpitStatusKind.Empty);
                }
                else
                {
                    SetStatus(CockpitStatusKind.Ready);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Ignore cancellations triggered by a newer refresh.
            }
            catch (Exception ex)
            {
                Metrics.Clear();
                Notices.Clear();
                SetStatus(CockpitStatusKind.Error, ex.Message);
            }
            finally
            {
                if (ReferenceEquals(_refreshCancellation, newToken))
                {
                    IsLoading = false;
                    _refreshCancellation = null;
                }

                newToken.Dispose();
            }
        }

        private void UpdateMetrics(IReadOnlyCollection<KpiWidget>? metrics)
        {
            Metrics.Clear();

            if (metrics is null)
            {
                return;
            }

            foreach (var widget in metrics)
            {
                Metrics.Add(new CockpitMetricViewModel(widget));
            }
        }

        private void UpdateNotices(IReadOnlyCollection<DashboardEvent>? events)
        {
            Notices.Clear();

            if (events is null)
            {
                return;
            }

            foreach (var dashboardEvent in events)
            {
                Notices.Add(new CockpitNoticeViewModel(dashboardEvent));
            }
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            RefreshLocalizedContent();

            if (_database is not null && _userSession is not null)
            {
                _ = RefreshAsync();
            }
        }

        private void OnSessionChanged(object? sender, EventArgs e)
        {
            if (_database is not null && _userSession is not null)
            {
                _ = RefreshAsync();
            }
        }

        private void RefreshLocalizedContent()
        {
            Title = _localization.GetString("Cockpit.Anchor.Title");
            AutomationId = _localization.GetString("Cockpit.Anchor.AutomationId");
            SetStatus(_statusKind, _statusErrorDetail);
        }

        private void PopulateDesignData()
        {
            Metrics.Clear();
            Notices.Clear();

            Metrics.Add(new CockpitMetricViewModel(new KpiWidget
            {
                Title = "Open CAPAs",
                Value = 7,
                Color = "#c7522a",
                Trend = "up",
                Unit = string.Empty,
                IsAlert = true
            }));

            Metrics.Add(new CockpitMetricViewModel(new KpiWidget
            {
                Title = "Preventive Jobs Due",
                Value = 12,
                Color = "#d9842b",
                Trend = "up",
                Unit = string.Empty,
                IsAlert = false
            }));

            Metrics.Add(new CockpitMetricViewModel(new KpiWidget
            {
                Title = "Machines Offline",
                Value = 2,
                Color = "#b71c1c",
                Trend = "down",
                Unit = string.Empty,
                IsAlert = true
            }));

            Metrics.Add(new CockpitMetricViewModel(new KpiWidget
            {
                Title = "On-Time Deliveries",
                Value = 96,
                Color = "#2e7d32",
                Trend = "up",
                Unit = "%",
                ValueText = "96%",
                IsAlert = false
            }));

            Notices.Add(new CockpitNoticeViewModel(new DashboardEvent
            {
                Description = "Sterilizer cycle expires in 2 days",
                Severity = "warning",
                Timestamp = DateTime.UtcNow.AddHours(-2)
            }));

            Notices.Add(new CockpitNoticeViewModel(new DashboardEvent
            {
                Description = "Calibration overdue for pump 12",
                Severity = "critical",
                Timestamp = DateTime.UtcNow.AddHours(-6)
            }));

            Notices.Add(new CockpitNoticeViewModel(new DashboardEvent
            {
                Description = "Two deviations awaiting approval",
                Severity = "info",
                Timestamp = DateTime.UtcNow.AddHours(-10)
            }));

            SetStatus(CockpitStatusKind.Ready);
        }

        private void SetStatus(CockpitStatusKind status, string? errorDetail = null)
        {
            _statusKind = status;
            _statusErrorDetail = errorDetail;

            switch (status)
            {
                case CockpitStatusKind.Loading:
                    HasError = false;
                    StatusMessage = _localization.GetString("Cockpit.Status.Loading");
                    break;
                case CockpitStatusKind.Empty:
                    HasError = false;
                    StatusMessage = _localization.GetString("Cockpit.Status.NoData");
                    break;
                case CockpitStatusKind.Error:
                    HasError = true;
                    StatusMessage = string.IsNullOrWhiteSpace(errorDetail)
                        ? _localization.GetString("Cockpit.Status.LoadFailed")
                        : _localization.GetString("Cockpit.Status.LoadFailedWithError", errorDetail);
                    break;
                case CockpitStatusKind.Ready:
                    HasError = false;
                    StatusMessage = _localization.GetString("Cockpit.Status.Ready");
                    break;
                default:
                    HasError = false;
                    StatusMessage = string.Empty;
                    break;
            }
        }

        private static bool IsDesignMode =>
            DesignerProperties.GetIsInDesignMode(new DependencyObject());

        private enum CockpitStatusKind
        {
            None,
            Loading,
            Ready,
            Empty,
            Error,
        }
    }

    /// <summary>View-model wrapper projecting <see cref="KpiWidget"/> details into the cockpit UI.</summary>
    public sealed class CockpitMetricViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CockpitMetricViewModel"/> class.
        /// </summary>
        /// <param name="widget">Database-provided KPI widget.</param>
        public CockpitMetricViewModel(KpiWidget widget)
        {
            if (widget is null)
            {
                throw new ArgumentNullException(nameof(widget));
            }

            Title = string.IsNullOrWhiteSpace(widget.Title) ? "-" : widget.Title;
            Unit = string.IsNullOrWhiteSpace(widget.Unit) ? null : widget.Unit;
            Value = ResolveValue(widget);
            Trend = NormalizeTrend(widget.Trend);
            TrendGlyph = ResolveTrendGlyph(Trend);
            TrendDisplay = string.IsNullOrWhiteSpace(Trend)
                ? string.Empty
                : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Trend);
            TrendBrush = ResolveTrendBrush(Trend);
            AccentBrush = ResolveBrush(widget.Color, widget.IsAlert ? Brushes.Firebrick : Brushes.SteelBlue);
            BorderBrush = widget.IsAlert ? Brushes.Gold : Brushes.Transparent;
            ForegroundBrush = Brushes.White;
            IsAlert = widget.IsAlert;
            LastUpdated = widget.LastUpdated.ToLocalTime();
        }

        /// <summary>Display title for the metric.</summary>
        public string Title { get; }

        /// <summary>Formatted numeric or textual value.</summary>
        public string Value { get; }

        /// <summary>Optional unit of measure.</summary>
        public string? Unit { get; }

        /// <summary>True when <see cref="Unit"/> contains content.</summary>
        public bool HasUnit => !string.IsNullOrWhiteSpace(Unit);

        /// <summary>Normalized trend indicator (up/down/neutral).</summary>
        public string Trend { get; }

        /// <summary>Human-readable trend description.</summary>
        public string TrendDisplay { get; }

        /// <summary>True when a trend glyph should be shown.</summary>
        public bool HasTrend => !string.IsNullOrWhiteSpace(TrendGlyph);

        /// <summary>Glyph used to render the trend.</summary>
        public string TrendGlyph { get; }

        /// <summary>Background accent applied to the metric card.</summary>
        public Brush AccentBrush { get; }

        /// <summary>Brush applied to the metric border when in alert mode.</summary>
        public Brush BorderBrush { get; }

        /// <summary>Brush used for the primary text.</summary>
        public Brush ForegroundBrush { get; }

        /// <summary>Brush used for the trend glyph.</summary>
        public Brush TrendBrush { get; }

        /// <summary>Indicates whether the metric is in an alert state.</summary>
        public bool IsAlert { get; }

        /// <summary>Timestamp of the last update converted to local time.</summary>
        public DateTime LastUpdated { get; }

        /// <summary>Formatted timestamp for tooltips.</summary>
        public string LastUpdatedDisplay => LastUpdated.ToString("g", CultureInfo.CurrentCulture);

        private static string ResolveValue(KpiWidget widget)
        {
            if (!string.IsNullOrWhiteSpace(widget.ValueText))
            {
                return widget.ValueText!;
            }

            var value = widget.Value;
            var format = decimal.Truncate(value) == value ? "N0" : "N2";
            return value.ToString(format, CultureInfo.CurrentCulture);
        }

        private static string NormalizeTrend(string? trend)
            => string.IsNullOrWhiteSpace(trend) ? string.Empty : trend.Trim().ToLowerInvariant();

        private static string ResolveTrendGlyph(string trend) => trend switch
        {
            "up" or "positive" => "↑",
            "down" or "negative" => "↓",
            "flat" or "neutral" => "→",
            _ => string.Empty,
        };

        private static Brush ResolveTrendBrush(string trend) => trend switch
        {
            "up" or "positive" => Brushes.LimeGreen,
            "down" or "negative" => Brushes.OrangeRed,
            "flat" or "neutral" => Brushes.Gainsboro,
            _ => Brushes.WhiteSmoke,
        };

        private static Brush ResolveBrush(string? color, Brush fallback)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return fallback;
            }

            try
            {
                return (Brush)new BrushConverter().ConvertFromString(color)!;
            }
            catch
            {
                return fallback;
            }
        }
    }

    /// <summary>View-model wrapper projecting <see cref="DashboardEvent"/> notices into the cockpit pane.</summary>
    public sealed class CockpitNoticeViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CockpitNoticeViewModel"/> class.
        /// </summary>
        /// <param name="dashboardEvent">Dashboard event returned from the database.</param>
        public CockpitNoticeViewModel(DashboardEvent dashboardEvent)
        {
            if (dashboardEvent is null)
            {
                throw new ArgumentNullException(nameof(dashboardEvent));
            }

            Summary = string.IsNullOrWhiteSpace(dashboardEvent.Description)
                ? (string.IsNullOrWhiteSpace(dashboardEvent.EventType) ? "-" : dashboardEvent.EventType)
                : dashboardEvent.Description!;

            Severity = NormalizeSeverity(dashboardEvent.Severity);
            SeverityDisplay = string.IsNullOrWhiteSpace(Severity)
                ? string.Empty
                : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Severity);
            SeverityBrush = ResolveSeverityBrush(Severity);
            Icon = dashboardEvent.Icon;
            IsUnread = dashboardEvent.IsUnread;
            RelatedModule = string.IsNullOrWhiteSpace(dashboardEvent.RelatedModule)
                ? null
                : dashboardEvent.RelatedModule!.Trim();
            TimestampUtc = dashboardEvent.Timestamp;
        }

        /// <summary>Summary text displayed to the operator.</summary>
        public string Summary { get; }

        /// <summary>Severity token associated with the event.</summary>
        public string Severity { get; }

        /// <summary>Localized severity caption.</summary>
        public string SeverityDisplay { get; }

        /// <summary>Brush used for severity indicators.</summary>
        public Brush SeverityBrush { get; }

        /// <summary>Optional icon identifier supplied by the data source.</summary>
        public string? Icon { get; }

        /// <summary>True when the event is still unread.</summary>
        public bool IsUnread { get; }

        /// <summary>Optional module reference for context.</summary>
        public string? RelatedModule { get; }

        /// <summary>Module caption used by the UI.</summary>
        public string ModuleDisplay => RelatedModule ?? string.Empty;

        /// <summary>UTC timestamp of the dashboard event.</summary>
        public DateTime TimestampUtc { get; }

        /// <summary>Local time representation of <see cref="TimestampUtc"/>.</summary>
        public DateTime TimestampLocal => TimestampUtc.ToLocalTime();

        /// <summary>Formatted timestamp for display.</summary>
        public string TimestampDisplay => TimestampLocal.ToString("g", CultureInfo.CurrentCulture);

        /// <summary>Indicates whether severity details should be shown.</summary>
        public bool HasSeverity => !string.IsNullOrWhiteSpace(SeverityDisplay);

        /// <summary>Indicates whether a related module is available.</summary>
        public bool HasModule => !string.IsNullOrWhiteSpace(RelatedModule);

        private static string NormalizeSeverity(string? severity)
            => string.IsNullOrWhiteSpace(severity) ? string.Empty : severity.Trim().ToLowerInvariant();

        private static Brush ResolveSeverityBrush(string severity) => severity switch
        {
            "critical" or "danger" => Brushes.Firebrick,
            "warning" or "warn" => Brushes.Orange,
            "success" or "ok" => Brushes.SeaGreen,
            "info" or "information" => Brushes.SteelBlue,
            "audit" => Brushes.MediumPurple,
            _ => Brushes.Gray,
        };
    }
}
