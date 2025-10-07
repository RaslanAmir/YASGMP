using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// Super-ultra robust ViewModel for live KPI dashboard, analytics, and GMP compliance widgets.
    /// ✅ Live KPI/statistics: overdue, in-progress, critical, compliance, calibration, CAPA, assets, users.
    /// ✅ Drilldown, filtering, date ranges, chart data, notifications, smart health score, "GMP readiness".
    /// ✅ Extensible for any widget: IoT, machine status, ERP, audit trend, mobile, or compliance summary.
    /// </summary>
    public class DashboardViewModel : INotifyPropertyChanged
    {
        #region Fields & Constructor

        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;

        private ObservableCollection<KpiWidget> _kpiWidgets = new();
        private ObservableCollection<ChartData> _charts = new();
        private ObservableCollection<DashboardEvent> _recentEvents = new();

        private string _dateRangeFilter = "Last30Days";
        private bool _isBusy;
        private string? _statusMessage;

        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        /// <summary>
        /// Initializes the DashboardViewModel and loads KPIs/charts/widgets.
        /// </summary>
        public DashboardViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService   = dbService  ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService?? throw new ArgumentNullException(nameof(authService));

            // Coalesce potentially-null session fields to safe non-null strings (CS8618 guard)
            _currentSessionId  = _authService.CurrentSessionId  ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? string.Empty;
            _currentIpAddress  = _authService.CurrentIpAddress  ?? string.Empty;

            LoadDashboardCommand    = new AsyncRelayCommand(LoadDashboardAsync);
            DateRangeChangedCommand = new RelayCommand<string?>(SetDateRange);

            // Load on startup
            _ = LoadDashboardAsync();
        }

        #endregion

        #region Properties

        /// <summary>Live KPI widgets for dashboard display.</summary>
        public ObservableCollection<KpiWidget> KpiWidgets
        {
            get => _kpiWidgets;
            set { _kpiWidgets = value ?? new(); OnPropertyChanged(); }
        }

        /// <summary>Live charts for trends, stats, compliance.</summary>
        public ObservableCollection<ChartData> Charts
        {
            get => _charts;
            set { _charts = value ?? new(); OnPropertyChanged(); }
        }

        /// <summary>Recent activity/events for dashboard.</summary>
        public ObservableCollection<DashboardEvent> RecentEvents
        {
            get => _recentEvents;
            set { _recentEvents = value ?? new(); OnPropertyChanged(); }
        }

        /// <summary>Current date range filter for widgets/charts.</summary>
        public string DateRangeFilter
        {
            get => _dateRangeFilter;
            set { _dateRangeFilter = value ?? "Last30Days"; OnPropertyChanged(); _ = LoadDashboardAsync(); }
        }

        /// <summary>Operation busy indicator.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>UI status message.</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>Available date ranges for dashboard.</summary>
        public string[] AvailableDateRanges => new[] { "Today", "Last7Days", "Last30Days", "ThisMonth", "ThisYear", "AllTime" };

        #endregion

        #region Commands
        /// <summary>
        /// Gets or sets the load dashboard command.
        /// </summary>

        public ICommand LoadDashboardCommand { get; }
        /// <summary>
        /// Gets or sets the date range changed command.
        /// </summary>
        public ICommand DateRangeChangedCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Loads all dashboard widgets, stats, and charts for the selected date range.
        /// Uses <see cref="Enumerable.Empty{T}"/> to coalesce nulls to a compatible <see cref="System.Collections.Generic.IEnumerable{T}"/>.
        /// </summary>
        public async Task LoadDashboardAsync()
        {
            IsBusy = true;
            try
            {
                var kpis = await _dbService.GetKpiWidgetsAsync(DateRangeFilter).ConfigureAwait(false);
                KpiWidgets = new ObservableCollection<KpiWidget>(
                    (System.Collections.Generic.IEnumerable<KpiWidget>?)kpis ?? Enumerable.Empty<KpiWidget>());

                var charts = await _dbService.GetDashboardChartsAsync(DateRangeFilter).ConfigureAwait(false);
                Charts = new ObservableCollection<ChartData>(
                    (System.Collections.Generic.IEnumerable<ChartData>?)charts ?? Enumerable.Empty<ChartData>());

                var events = await _dbService.GetRecentDashboardEventsAsync(DateRangeFilter).ConfigureAwait(false);
                RecentEvents = new ObservableCollection<DashboardEvent>(
                    (System.Collections.Generic.IEnumerable<DashboardEvent>?)events ?? Enumerable.Empty<DashboardEvent>());

                StatusMessage = "Dashboard loaded.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Dashboard load failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// Changes date range and reloads widgets/charts.
        /// Accepts <c>null</c> from UI command binding (Action&lt;string?&gt;) and ignores invalid values.
        /// </summary>
        public void SetDateRange(string? newRange)
        {
            if (string.IsNullOrWhiteSpace(newRange)) return;
            if (!AvailableDateRanges.Contains(newRange)) return;
            DateRangeFilter = newRange;
        }

        #endregion

        #region INotifyPropertyChanged

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raises property change notifications for MVVM binding.</summary>
        private void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName ?? string.Empty));

        #endregion
    }
}
