// ==============================================================================
//  File: ViewModels/AuditLogViewModel.cs
//  Project: YasGMP
//  Summary:
//      GMP / 21 CFR Part 11 compliant Audit/System Events ViewModel.
//      Strongly-typed against DatabaseService audit query extensions and the
//      SystemEvent POCO (from YasGMP.AppCore, namespace YasGMP.Services).
//      Includes filtering + CSV/XLSX/PDF export (helpers in YasGMP.Helpers).
//  © 2025 YasGMP. All rights reserved.
// ==============================================================================
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using YasGMP.Helpers;      // XlsxExporter, PdfExporter live here (separate files)
using YasGMP.Services;     // DatabaseService + SystemEvent (via YasGMP.AppCore)

// Alias to ensure we bind to the correct POCO type regardless of using scope.
using SystemEvent = YasGMP.Services.SystemEvent;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>AuditLogViewModel</b> — robust ViewModel for reading and exporting
    /// <c>system_event_log</c> entries (audit/system events).
    /// <para>
    /// Data source: DatabaseService.GetSystemEventsAsync (system_event_log-backed extension method).
    /// Mapped properties of <see cref="SystemEvent"/> include:
    /// <list type="bullet">
    /// <item><description><c>Id</c></description></item>
    /// <item><description><c>EventType</c> (e.g., CREATE/UPDATE/DELETE/LOGIN/...)</description></item>
    /// <item><description><c>TableName</c></description></item>
    /// <item><description><c>RecordId</c></description></item>
    /// <item><description><c>UserId</c></description></item>
    /// <item><description><c>EventTime</c> (DateTime)</description></item>
    /// <item><description><c>SourceIp</c></description></item>
    /// <item><description><c>DeviceInfo</c></description></item>
    /// <item><description><c>Description</c></description></item>
    /// </list>
    /// </para>
    /// </summary>
    public sealed class AuditLogViewModel : INotifyPropertyChanged
    {
        #region === Fields & Constructor ===

        private readonly DatabaseService? _db;
        private bool _isBusy;
        private string? _statusMessage;

        private string? _filterUserIdText;
        private string? _filterEntity;
        private string? _selectedAction;
        private DateTime? _filterFrom;
        private DateTime? _filterTo;

        /// <summary>Parameterless constructor for XAML/DI fallback.</summary>
        public AuditLogViewModel() : this(db: null) { }

        /// <summary>Preferred constructor with DI.</summary>
        /// <param name="db">Database access service.</param>
        public AuditLogViewModel(DatabaseService? db)
        {
            _db = db;

            ApplyFilterCommand = new Command(ApplyFilter);
            RefreshCommand     = new Command(async () => await LoadAsync());

            ExportCsvCommand   = new Command(ExportCsv);
            ExportXlsxCommand  = new Command(ExportXlsx);
            ExportPdfCommand   = new Command(ExportPdf);
        }

        #endregion

        #region === Collections ===

        /// <summary>All events loaded from the database.</summary>
        public ObservableCollection<SystemEvent> AllEvents { get; } = new();

        /// <summary>Filtered events presented to the UI.</summary>
        public ObservableCollection<SystemEvent> FilteredEvents { get; } = new();

        /// <summary>Available action types for filtering (auto-extended from data).</summary>
        public ObservableCollection<string> ActionTypes { get; } = new(new[]
        {
            "CREATE","UPDATE","DELETE","APPROVE","CLOSE","ESCALATE","EXPORT","ROLLBACK","COMMENT","LOGIN","LOGOUT","PRINT","SIGN","CONFIG_CHANGE"
        });

        #endregion

        #region === Filters / State ===

        /// <summary>Free-text for UserId filter; parsed to <see cref="int"/> if valid.</summary>
        public string? FilterUserIdText
        {
            get => _filterUserIdText;
            set { _filterUserIdText = value; OnPropertyChanged(); }
        }

        /// <summary>Entity/table filter (e.g., "work_orders").</summary>
        public string? FilterEntity
        {
            get => _filterEntity;
            set { _filterEntity = value; OnPropertyChanged(); }
        }

        /// <summary>Selected action/event type.</summary>
        public string? SelectedAction
        {
            get => _selectedAction;
            set { _selectedAction = value; OnPropertyChanged(); }
        }

        /// <summary>Lower datetime bound (inclusive, local).</summary>
        public DateTime? FilterFrom
        {
            get => _filterFrom;
            set { _filterFrom = value; OnPropertyChanged(); }
        }

        /// <summary>Upper datetime bound (inclusive, local).</summary>
        public DateTime? FilterTo
        {
            get => _filterTo;
            set { _filterTo = value; OnPropertyChanged(); }
        }

        /// <summary>Busy flag for async operations.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>Status/info/error line for UI.</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        #endregion

        #region === Commands ===

        /// <summary>Applies on-memory filters.</summary>
        public ICommand ApplyFilterCommand { get; }

        /// <summary>Reloads events from the database.</summary>
        public ICommand RefreshCommand { get; }

        /// <summary>Export filtered events to CSV.</summary>
        public ICommand ExportCsvCommand { get; }

        /// <summary>Export filtered events to native Excel .xlsx.</summary>
        public ICommand ExportXlsxCommand { get; }

        /// <summary>Export filtered events to a simple PDF.</summary>
        public ICommand ExportPdfCommand { get; }

        #endregion

        #region === Load & Filter ===

        /// <summary>
        /// Loads events via the DatabaseService audit query extension and populates collections.
        /// </summary>
        public async Task LoadAsync()
        {
            if (_db == null)
            {
                StatusMessage = "DatabaseService nije injektiran — prikazujem memorijske podatke.";
                return;
            }

            IsBusy = true;
            try
            {
                // Convert bounds to date range
                DateTime? from = FilterFrom?.Date;
                DateTime? to   = FilterTo?.Date.AddDays(1).AddTicks(-1);

                int? userId = null;
                if (!string.IsNullOrWhiteSpace(FilterUserIdText) &&
                    int.TryParse(FilterUserIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    userId = parsed;
                }

                // Call DatabaseService (system_event_log-backed)
                var events = await _db.GetSystemEventsAsync(
                    userId: userId,
                    module: null,
                    tableName: string.IsNullOrWhiteSpace(FilterEntity) ? null : FilterEntity,
                    severity: null,
                    from: from,
                    to: to,
                    processed: null,
                    limit: 1000,
                    offset: 0
                ).ConfigureAwait(false);

                AllEvents.Clear();
                foreach (var e in events) AllEvents.Add(e);

                // Extend action list from data (distinct EventType)
                foreach (var a in AllEvents
                    .Select(x => (x.EventType ?? string.Empty).Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!ActionTypes.Contains(a)) ActionTypes.Add(a);
                }

                ApplyFilter();
                StatusMessage = $"Učitano zapisa: {AllEvents.Count}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Greška pri učitavanju: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        /// <summary>Applies filters to <see cref="AllEvents"/> into <see cref="FilteredEvents"/>.</summary>
        private void ApplyFilter()
        {
            var q = AllEvents.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(FilterEntity))
                q = q.Where(e => (e.TableName ?? string.Empty).IndexOf(FilterEntity, StringComparison.OrdinalIgnoreCase) >= 0);

            if (!string.IsNullOrWhiteSpace(SelectedAction))
                q = q.Where(e => string.Equals(e.EventType ?? string.Empty, SelectedAction, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(FilterUserIdText) &&
                int.TryParse(FilterUserIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var uid))
            {
                q = q.Where(e => (e.UserId ?? 0) == uid);
            }

            if (FilterFrom.HasValue)
                q = q.Where(e => e.EventTime >= FilterFrom.Value.Date);

            if (FilterTo.HasValue)
                q = q.Where(e => e.EventTime <= FilterTo.Value.Date.AddDays(1).AddTicks(-1));

            var list = q.OrderByDescending(e => e.EventTime).ToList();

            FilteredEvents.Clear();
            foreach (var e in list) FilteredEvents.Add(e);

            StatusMessage = $"Prikazano: {FilteredEvents.Count}";
        }

        #endregion

        #region === Exports (CSV/XLSX/PDF) ===

        /// <summary>Exports the current <see cref="FilteredEvents"/> to CSV (UTF-8) in AppData.</summary>
        private void ExportCsv()
        {
            try
            {
                if (FilteredEvents.Count == 0) { StatusMessage = "Nema zapisa za izvoz."; return; }

                var sb = new StringBuilder();
                sb.AppendLine("Id,EventType,TableName,RecordId,UserId,EventTime,SourceIp,DeviceInfo,Description");

                foreach (var e in FilteredEvents)
                {
                    var line = new[]
                    {
                        e.Id.ToString(CultureInfo.InvariantCulture),
                        Csv(e.EventType),
                        Csv(e.TableName),
                        e.RecordId?.ToString(CultureInfo.InvariantCulture) ?? "",
                        e.UserId?.ToString(CultureInfo.InvariantCulture) ?? "",
                        e.EventTime.ToString("u", CultureInfo.InvariantCulture),
                        Csv(e.SourceIp),
                        Csv(e.DeviceInfo),
                        Csv(e.Description)
                    };
                    sb.AppendLine(string.Join(",", line));
                }

                var path = Path.Combine(FileSystem.AppDataDirectory, $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                StatusMessage = $"CSV izvezen: {path}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Greška pri CSV izvozu: {ex.Message}";
            }

            static string Csv(string? s)
            {
                s ??= string.Empty;
                s = s.Replace("\r", " ").Replace("\n", " ").Trim();
                return (s.Contains(',') || s.Contains('"')) ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
            }
        }

        /// <summary>Exports the current <see cref="FilteredEvents"/> to native Excel .xlsx (Open XML) via <see cref="XlsxExporter"/>.</summary>
        private void ExportXlsx()
        {
            try
            {
                if (FilteredEvents.Count == 0) { StatusMessage = "Nema zapisa za izvoz."; return; }

                var headers = new[] { "Id","EventType","TableName","RecordId","UserId","EventTime","SourceIp","DeviceInfo","Description" };
                var rows = FilteredEvents.Select(e => new[]
                {
                    e.Id.ToString(CultureInfo.InvariantCulture),
                    e.EventType ?? "",
                    e.TableName ?? "",
                    e.RecordId?.ToString(CultureInfo.InvariantCulture) ?? "",
                    e.UserId?.ToString(CultureInfo.InvariantCulture) ?? "",
                    e.EventTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    e.SourceIp ?? "",
                    e.DeviceInfo ?? "",
                    e.Description ?? ""
                }).ToList();

                var file = Path.Combine(FileSystem.AppDataDirectory, $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                XlsxExporter.WriteSingleSheet(file, "AuditLog", headers, rows.Select(x => (IList<string>)x).ToList());
                StatusMessage = $"XLSX izvezen: {file}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Greška pri XLSX izvozu: {ex.Message}";
            }
        }

        /// <summary>Exports the current <see cref="FilteredEvents"/> to a simple one-page PDF via <see cref="PdfExporter"/>.</summary>
        private void ExportPdf()
        {
            try
            {
                if (FilteredEvents.Count == 0) { StatusMessage = "Nema zapisa za izvoz."; return; }

                var lines = FilteredEvents.Select(e =>
                {
                    string txt = e.Description ?? "";
                    if (txt.Length > 120) txt = txt.Substring(0, 117) + "...";
                    return $"{e.Id,-6} {Trunc(e.EventType,9),-9} {Trunc(e.TableName,20),-20} {e.RecordId,6} {e.EventTime:yyyy-MM-dd HH:mm} {Trunc(e.SourceIp,15),-15} {Trunc(txt,80)}";
                }).ToList();

                lines.Insert(0, "ID    ACTION    ENTITY               RECID  WHEN               SOURCE_IP       DESCRIPTION");
                lines.Insert(1, "----- --------- -------------------- ------ ------------------ --------------- ----------------------------------------");

                var file = Path.Combine(FileSystem.AppDataDirectory, $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                PdfExporter.WriteSimpleTextPdf(file, "Audit Log", lines);
                StatusMessage = $"PDF izvezen: {file}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Greška pri PDF izvozu: {ex.Message}";
            }

            static string Trunc(string? s, int max) => (s ?? string.Empty).Length <= max
                ? (s ?? string.Empty)
                : (s ?? string.Empty).Substring(0, max - 1) + "…";
        }

        #endregion

        #region === INotifyPropertyChanged ===

        /// <inheritdoc />
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion
    }
}
