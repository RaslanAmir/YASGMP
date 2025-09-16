using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using MySqlConnector;
using YasGMP.Helpers;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// ViewModel for generating, filtering and exporting audit/compliance reports.
    /// Supports dynamic report generation by date range, report type and linked entity metadata.
    /// Provides rich in-memory search/filtering and PDF/Excel export via helper utilities.
    /// </summary>
    public sealed class ReportViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _dbService;
        private readonly AuthService _authService;
        private readonly List<Report> _allReports = new();

        private readonly AsyncRelayCommand _loadReportsCommand;
        private readonly AsyncRelayCommand _generateReportsCommand;
        private readonly AsyncRelayCommand _exportPdfCommand;
        private readonly AsyncRelayCommand _exportExcelCommand;
        private readonly RelayCommand _applyFiltersCommand;
        private readonly RelayCommand _clearFiltersCommand;

        private DateTime? _fromDate;
        private DateTime? _toDate;
        private string? _selectedReportType;
        private string? _selectedEntityType;
        private string? _entityIdText;
        private string? _searchTerm;
        private string? _statusFilter;
        private bool _isBusy;
        private string? _statusMessage;
        private bool _suppressFilter;

        private readonly string _currentSessionId;
        private readonly string _currentDeviceInfo;
        private readonly string _currentIpAddress;

        private static readonly string[] ReportTypes =
        {
            "pdf", "excel", "dashboard", "csv", "html", "json", "xml"
        };

        private static readonly string[] EntityTypes =
        {
            "work_order", "incident", "deviation", "capa", "asset", "user", "supplier", "training", "audit"
        };

        private static readonly string[] Statuses =
        {
            "draft", "generated", "pending", "exported", "archived", "error"
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportViewModel"/> class.
        /// </summary>
        /// <param name="dbService">Database access service.</param>
        /// <param name="authService">Authentication/session context.</param>
        /// <exception cref="ArgumentNullException">Thrown when dependencies are <c>null</c>.</exception>
        public ReportViewModel(DatabaseService dbService, AuthService authService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            _currentSessionId = _authService.CurrentSessionId ?? string.Empty;
            _currentDeviceInfo = _authService.CurrentDeviceInfo ?? string.Empty;
            _currentIpAddress = _authService.CurrentIpAddress ?? string.Empty;

            _loadReportsCommand = new AsyncRelayCommand(LoadReportsAsync, () => !IsBusy);
            _generateReportsCommand = new AsyncRelayCommand(GenerateReportsAsync, () => !IsBusy);
            _exportPdfCommand = new AsyncRelayCommand(ExportPdfAsync, CanExport);
            _exportExcelCommand = new AsyncRelayCommand(ExportExcelAsync, CanExport);
            _applyFiltersCommand = new RelayCommand(ApplyFilters);
            _clearFiltersCommand = new RelayCommand(ClearFilters);

            _suppressFilter = true;
            FromDate = DateTime.UtcNow.Date.AddDays(-30);
            ToDate = DateTime.UtcNow.Date;
            _suppressFilter = false;

            _ = LoadReportsAsync();
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Collection of filtered reports exposed to the UI.</summary>
        public ObservableCollection<Report> Reports { get; } = new();

        /// <summary>Command that loads reports using the current filters.</summary>
        public IAsyncRelayCommand LoadReportsCommand => _loadReportsCommand;

        /// <summary>Command that (re)generates reports with the active filters.</summary>
        public IAsyncRelayCommand GenerateReportsCommand => _generateReportsCommand;

        /// <summary>Command that applies in-memory filters (search, status, etc.).</summary>
        public IRelayCommand ApplyFiltersCommand => _applyFiltersCommand;

        /// <summary>Command that clears all filters.</summary>
        public IRelayCommand ClearFiltersCommand => _clearFiltersCommand;

        /// <summary>Command that exports the filtered reports to PDF.</summary>
        public IAsyncRelayCommand ExportPdfCommand => _exportPdfCommand;

        /// <summary>Command that exports the filtered reports to Excel.</summary>
        public IAsyncRelayCommand ExportExcelCommand => _exportExcelCommand;

        /// <summary>Start date filter (inclusive).</summary>
        public DateTime? FromDate
        {
            get => _fromDate;
            set
            {
                if (SetProperty(ref _fromDate, value) && !_suppressFilter)
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>End date filter (inclusive).</summary>
        public DateTime? ToDate
        {
            get => _toDate;
            set
            {
                if (SetProperty(ref _toDate, value) && !_suppressFilter)
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>Selected report type filter.</summary>
        public string? SelectedReportType
        {
            get => _selectedReportType;
            set
            {
                if (SetProperty(ref _selectedReportType, value) && !_suppressFilter)
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>Selected linked entity type filter.</summary>
        public string? SelectedEntityType
        {
            get => _selectedEntityType;
            set
            {
                if (SetProperty(ref _selectedEntityType, value) && !_suppressFilter)
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>Free-text linked entity identifier filter.</summary>
        public string? EntityIdText
        {
            get => _entityIdText;
            set
            {
                if (SetProperty(ref _entityIdText, value) && !_suppressFilter)
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>Search text applied across title, description and metadata.</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (SetProperty(ref _searchTerm, value) && !_suppressFilter)
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>Status filter for the generated reports.</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set
            {
                if (SetProperty(ref _statusFilter, value) && !_suppressFilter)
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>Indicates whether a database/export operation is running.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    _loadReportsCommand.NotifyCanExecuteChanged();
                    _generateReportsCommand.NotifyCanExecuteChanged();
                    _exportPdfCommand.NotifyCanExecuteChanged();
                    _exportExcelCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>Status message shown in the UI.</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>Number of records currently displayed after filtering.</summary>
        public int ResultCount => Reports.Count;

        /// <summary>Available report types (informational, for dropdowns).</summary>
        public IReadOnlyList<string> AvailableReportTypes => ReportTypes;

        /// <summary>Available linked entity types (informational, for dropdowns).</summary>
        public IReadOnlyList<string> AvailableEntityTypes => EntityTypes;

        /// <summary>Available report statuses.</summary>
        public IReadOnlyList<string> AvailableStatuses => Statuses;

        /// <summary>
        /// Loads initial report data (shortcut for <see cref="GenerateReportsAsync"/>).
        /// </summary>
        public async Task LoadReportsAsync()
        {
            await GenerateReportsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Generates reports from the database using current filters (date/type/entity/status).
        /// </summary>
        public async Task GenerateReportsAsync()
        {
            if (!TryParseEntityId(out var entityId, showError: true))
            {
                return;
            }

            IsBusy = true;
            StatusMessage = "Generating reports...";

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var parameters = new List<MySqlParameter>();
                var sql = new StringBuilder();
                sql.AppendLine("SELECT id, title, description, generated_on, generated_by_id, file_path, digital_signature,");
                sql.AppendLine("       report_type, version_no, parameters, status, linked_entity_id, linked_entity_type,");
                sql.AppendLine("       regulator, anomaly_score, analytics_json, last_modified, last_modified_by_id, source_ip,");
                sql.AppendLine("       session_id, device_info, geo_location, note");
                sql.AppendLine("FROM reports WHERE 1=1");

                if (FromDate.HasValue)
                {
                    sql.AppendLine("  AND generated_on >= @fromDate");
                    parameters.Add(new MySqlParameter("@fromDate", FromDate.Value.Date));
                }

                if (ToDate.HasValue)
                {
                    var upperBound = ToDate.Value.Date.AddDays(1).AddTicks(-1);
                    sql.AppendLine("  AND generated_on <= @toDate");
                    parameters.Add(new MySqlParameter("@toDate", upperBound));
                }

                if (!string.IsNullOrWhiteSpace(SelectedReportType))
                {
                    sql.AppendLine("  AND report_type = @reportType");
                    parameters.Add(new MySqlParameter("@reportType", SelectedReportType));
                }

                if (!string.IsNullOrWhiteSpace(SelectedEntityType))
                {
                    sql.AppendLine("  AND linked_entity_type = @entityType");
                    parameters.Add(new MySqlParameter("@entityType", SelectedEntityType));
                }

                if (entityId.HasValue)
                {
                    sql.AppendLine("  AND linked_entity_id = @entityId");
                    parameters.Add(new MySqlParameter("@entityId", entityId.Value));
                }

                if (!string.IsNullOrWhiteSpace(StatusFilter))
                {
                    sql.AppendLine("  AND status = @status");
                    parameters.Add(new MySqlParameter("@status", StatusFilter));
                }

                sql.AppendLine("ORDER BY generated_on DESC, id DESC");
                sql.AppendLine("LIMIT 1000;");

                var table = await _dbService.ExecuteSelectAsync(sql.ToString(), parameters, cts.Token).ConfigureAwait(false);

                var items = new List<Report>(table.Rows.Count);
                foreach (DataRow row in table.Rows)
                {
                    items.Add(MapReport(row));
                }

                _allReports.Clear();
                _allReports.AddRange(items);

                ApplyFilters();

                StatusMessage = $"Generated {items.Count} report(s).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to generate reports: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Applies in-memory search and filter logic to the cached reports.
        /// </summary>
        public void ApplyFilters()
        {
            if (_suppressFilter)
            {
                return;
            }

            IEnumerable<Report> query = _allReports;

            if (FromDate.HasValue)
            {
                var from = FromDate.Value.Date;
                query = query.Where(r => r.GeneratedOn >= from);
            }

            if (ToDate.HasValue)
            {
                var to = ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(r => r.GeneratedOn <= to);
            }

            if (!string.IsNullOrWhiteSpace(SelectedReportType))
            {
                var type = SelectedReportType.Trim();
                query = query.Where(r => string.Equals(r.ReportType, type, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SelectedEntityType))
            {
                var entityType = SelectedEntityType.Trim();
                query = query.Where(r => string.Equals(r.LinkedEntityType, entityType, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter))
            {
                var status = StatusFilter.Trim();
                query = query.Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase));
            }

            if (!TryParseEntityId(out var entityId, showError: false))
            {
                entityId = null;
            }

            if (entityId.HasValue)
            {
                query = query.Where(r => r.LinkedEntityId == entityId.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();
                query = query.Where(r =>
                    ContainsInsensitive(r.Title, term) ||
                    ContainsInsensitive(r.Description, term) ||
                    ContainsInsensitive(r.ReportType, term) ||
                    ContainsInsensitive(r.Status, term) ||
                    ContainsInsensitive(r.LinkedEntityType, term) ||
                    ContainsInsensitive(r.Parameters, term) ||
                    ContainsInsensitive(r.FilePath, term) ||
                    ContainsInsensitive(r.Note, term) ||
                    ContainsInsensitive(r.GeoLocation, term));
            }

            var filtered = query
                .OrderByDescending(r => r.GeneratedOn)
                .ThenByDescending(r => r.Id)
                .ToList();

            UpdateReportsCollection(filtered);
        }

        /// <summary>Clears all filters and reapplies them.</summary>
        public void ClearFilters()
        {
            _suppressFilter = true;
            try
            {
                FromDate = null;
                ToDate = null;
                SelectedReportType = null;
                SelectedEntityType = null;
                EntityIdText = null;
                SearchTerm = null;
                StatusFilter = null;
            }
            finally
            {
                _suppressFilter = false;
            }

            ApplyFilters();
            StatusMessage = "Filters cleared.";
        }

        private async Task ExportPdfAsync()
        {
            if (!CanExport())
            {
                StatusMessage = "No reports to export.";
                return;
            }

            IsBusy = true;
            try
            {
                var snapshot = Reports.ToList();
                var columns = new (string Header, Func<Report, object?> Selector)[]
                {
                    ("ID", r => r.Id),
                    ("Title", r => r.Title),
                    ("Type", r => r.ReportType),
                    ("Status", r => r.Status),
                    ("GeneratedOn", r => r.GeneratedOn.ToString("u", CultureInfo.InvariantCulture)),
                    ("EntityType", r => r.LinkedEntityType ?? string.Empty),
                    ("EntityId", r => r.LinkedEntityId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
                    ("GeneratedBy", r => r.GeneratedById)
                };

                string path = PdfExporter.WriteTable(snapshot, "reports", columns, "Generated Reports");
                StatusMessage = $"PDF export created: {path}";
                await LogReportExportAsync("pdf", path, snapshot.Count).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to export PDF: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExportExcelAsync()
        {
            if (!CanExport())
            {
                StatusMessage = "No reports to export.";
                return;
            }

            IsBusy = true;
            try
            {
                var snapshot = Reports.ToList();
                var columns = new (string Header, Func<Report, object?> Selector)[]
                {
                    ("ID", r => r.Id),
                    ("Title", r => r.Title),
                    ("Type", r => r.ReportType),
                    ("Status", r => r.Status),
                    ("GeneratedOn", r => r.GeneratedOn.ToString("u", CultureInfo.InvariantCulture)),
                    ("EntityType", r => r.LinkedEntityType ?? string.Empty),
                    ("EntityId", r => r.LinkedEntityId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
                    ("GeneratedBy", r => r.GeneratedById)
                };

                string path = XlsxExporter.WriteSheet(snapshot, "reports", columns);
                StatusMessage = $"Excel export created: {path}";
                await LogReportExportAsync("excel", path, snapshot.Count).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to export Excel: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExport() => !IsBusy && Reports.Count > 0;

        private void UpdateReportsCollection(IList<Report> items)
        {
            void Apply()
            {
                Reports.Clear();
                foreach (var report in items)
                {
                    Reports.Add(report);
                }

                OnPropertyChanged(nameof(ResultCount));
                _exportPdfCommand.NotifyCanExecuteChanged();
                _exportExcelCommand.NotifyCanExecuteChanged();
            }

            if (MainThread.IsMainThread)
            {
                Apply();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(Apply);
            }
        }

        private static Report MapReport(DataRow row)
        {
            return new Report
            {
                Id = SafeInt(row, "id") ?? 0,
                Title = SafeString(row, "title") ?? string.Empty,
                Description = SafeString(row, "description"),
                GeneratedOn = SafeDateTime(row, "generated_on") ?? DateTime.UtcNow,
                GeneratedById = SafeInt(row, "generated_by_id") ?? 0,
                FilePath = SafeString(row, "file_path"),
                DigitalSignature = SafeString(row, "digital_signature"),
                ReportType = SafeString(row, "report_type"),
                VersionNo = SafeInt(row, "version_no") ?? 0,
                Parameters = SafeString(row, "parameters"),
                Status = SafeString(row, "status"),
                LinkedEntityId = SafeInt(row, "linked_entity_id"),
                LinkedEntityType = SafeString(row, "linked_entity_type"),
                Regulator = SafeString(row, "regulator"),
                AnomalyScore = SafeDouble(row, "anomaly_score"),
                AnalyticsJson = SafeString(row, "analytics_json"),
                LastModified = SafeDateTime(row, "last_modified") ?? DateTime.UtcNow,
                LastModifiedById = SafeInt(row, "last_modified_by_id") ?? 0,
                SourceIp = SafeString(row, "source_ip"),
                SessionId = SafeString(row, "session_id"),
                DeviceInfo = SafeString(row, "device_info"),
                GeoLocation = SafeString(row, "geo_location"),
                Note = SafeString(row, "note"),
            };
        }

        private static int? SafeInt(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? Convert.ToInt32(row[column], CultureInfo.InvariantCulture) : (int?)null;

        private static double? SafeDouble(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? Convert.ToDouble(row[column], CultureInfo.InvariantCulture) : (double?)null;

        private static DateTime? SafeDateTime(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? Convert.ToDateTime(row[column], CultureInfo.InvariantCulture) : (DateTime?)null;

        private static string? SafeString(DataRow row, string column)
            => row.Table.Columns.Contains(column) && row[column] != DBNull.Value ? row[column]?.ToString() : null;

        private static bool ContainsInsensitive(string? source, string term)
            => !string.IsNullOrWhiteSpace(source) && source.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;

        private bool TryParseEntityId(out int? entityId, bool showError)
        {
            entityId = null;
            var text = EntityIdText;
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            if (int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                entityId = parsed;
                return true;
            }

            if (showError)
            {
                StatusMessage = "Entity ID must be a whole number.";
            }

            return false;
        }

        private async Task LogReportExportAsync(string format, string path, int count)
        {
            try
            {
                var userId = _authService.CurrentUser?.Id;
                await _dbService.LogSystemEventAsync(
                    userId,
                    $"REPORT_EXPORT_{format.ToUpperInvariant()}",
                    "reports",
                    "Reports",
                    null,
                    $"count={count}; file={path}",
                    _currentIpAddress,
                    "info",
                    _currentDeviceInfo,
                    _currentSessionId).ConfigureAwait(false);
            }
            catch
            {
                // Non-fatal: logging failures should not block the export.
            }
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
