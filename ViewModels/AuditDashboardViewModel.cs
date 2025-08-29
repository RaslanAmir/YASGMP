using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;   // MainThread
using YasGMP.Models.DTO;
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>AuditDashboardViewModel</b> – Manages the audit dashboard: real-time filtering,
    /// exports, and live SignalR updates. Uses explicit properties (AOT-safe) and ensures
    /// all UI interactions (alerts + collection updates) occur on the UI thread.
    /// </summary>
    public partial class AuditDashboardViewModel : ObservableObject
    {
        private readonly AuditService _auditService;
        private readonly ExportService _exportService;

        /// <summary>
        /// All filtered audit entries for dashboard display (UI-bound).
        /// </summary>
        public ObservableCollection<AuditEntryDto> FilteredAudits { get; } = new();

        // === AOT-safe properties (no [ObservableProperty]) ===

        private string? _filterUser;
        /// <summary>Filter: user full name, username, or user ID (nullable).</summary>
        [Description("Filter by user full name or username.")]
        public string? FilterUser
        {
            get => _filterUser;
            set => SetProperty(ref _filterUser, value);
        }

        private string? _filterEntity;
        /// <summary>Filter: audited entity/table name (nullable).</summary>
        [Description("Filter by audited entity or table.")]
        public string? FilterEntity
        {
            get => _filterEntity;
            set => SetProperty(ref _filterEntity, value);
        }

        private string? _selectedAction;
        /// <summary>Filter: selected audit action type (nullable).</summary>
        [Description("Selected audit action type filter.")]
        public string? SelectedAction
        {
            get => _selectedAction;
            set => SetProperty(ref _selectedAction, value);
        }

        private DateTime _filterFrom;
        /// <summary>Filter: From date (inclusive).</summary>
        [Description("Start date filter for audit range.")]
        public DateTime FilterFrom
        {
            get => _filterFrom;
            set => SetProperty(ref _filterFrom, value);
        }

        private DateTime _filterTo;
        /// <summary>Filter: To date (inclusive).</summary>
        [Description("End date filter for audit range.")]
        public DateTime FilterTo
        {
            get => _filterTo;
            set => SetProperty(ref _filterTo, value);
        }

        /// <summary>
        /// Available action types for filtering (UI helper).
        /// </summary>
        public string[] ActionTypes => new[] { "All", "CREATE", "UPDATE", "DELETE", "SIGN", "ROLLBACK", "EXPORT" };

        /// <summary>Command: Apply filters and reload the audit list.</summary>
        public ICommand ApplyFilterCommand { get; }

        /// <summary>Command: Export current filtered audits to PDF.</summary>
        public ICommand ExportPdfCommand { get; }

        /// <summary>Command: Export current filtered audits to Excel.</summary>
        public ICommand ExportExcelCommand { get; }

        /// <summary>
        /// Initializes the dashboard ViewModel with all dependencies and commands.
        /// </summary>
        /// <param name="auditService">Audit service instance.</param>
        /// <param name="exportService">Export service instance.</param>
        /// <exception cref="ArgumentNullException">If any dependency is <c>null</c>.</exception>
        public AuditDashboardViewModel(AuditService auditService, ExportService exportService)
        {
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

            // Default filter window (last 30 days)
            _filterFrom = DateTime.Now.AddDays(-30);
            _filterTo = DateTime.Now;

            ApplyFilterCommand  = new AsyncRelayCommand(LoadAuditsAsync);
            ExportPdfCommand    = new AsyncRelayCommand(ExportPdfAsync);
            ExportExcelCommand  = new AsyncRelayCommand(ExportExcelAsync);

            SubscribeToSignalR();
            _ = LoadAuditsAsync();
        }

        /// <summary>
        /// Loads audits from the audit service using current filters
        /// and replaces the UI-bound collection on the UI thread.
        /// </summary>
        private async Task LoadAuditsAsync()
        {
            try
            {
                var audits = await GetFilteredAuditsSafe(FilterUser, FilterEntity, SelectedAction, FilterFrom, FilterTo)
                    .ConfigureAwait(false);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    FilteredAudits.Clear();
                    if (audits != null)
                    {
                        foreach (var a in audits)
                            FilteredAudits.Add(a);
                    }
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Error", $"Greška kod učitavanja audita: {ex.Message}", "OK")
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Exports the current filtered audits to a PDF file.
        /// </summary>
        private async Task ExportPdfAsync()
        {
            try
            {
                await _exportService.ExportAuditToPdf(FilteredAudits).ConfigureAwait(false);
                await SafeNavigator.ShowAlertAsync("Export", "Uspješan export u PDF.", "OK").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Export Error", $"Neuspješan export u PDF: {ex.Message}", "OK")
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Exports the current filtered audits to an Excel file.
        /// </summary>
        private async Task ExportExcelAsync()
        {
            try
            {
                await _exportService.ExportAuditToExcel(FilteredAudits).ConfigureAwait(false);
                await SafeNavigator.ShowAlertAsync("Export", "Uspješan export u Excel.", "OK").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Export Error", $"Neuspješan export u Excel: {ex.Message}", "OK")
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Subscribes to SignalR events to receive real-time audit notifications and update the dashboard.
        /// Ensures collection modifications and alerts occur on the UI thread.
        /// </summary>
        private void SubscribeToSignalR()
        {
            SignalRService.OnAuditReceived += async audit =>
            {
                if (audit == null) return;

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Prepend newest audit to the list
                    FilteredAudits.Insert(0, audit);

                    // Display real-time notification with action and user
                    string who = GetUserFullNameSafe(audit);
                    await SafeNavigator.ShowAlertAsync("New Audit Event", $"Action: {audit.Action} by {who}", "OK");
                }).ConfigureAwait(false);
            };
        }

        /// <summary>
        /// Gets the full name of the user for the given audit entry (handles nulls, fallback to username or ID).
        /// </summary>
        /// <param name="audit">The audit entry DTO.</param>
        /// <returns>User's full name, username, or a fallback string.</returns>
        private static string GetUserFullNameSafe(AuditEntryDto audit)
        {
            if (audit == null) return "Unknown";
            var type = audit.GetType();
            var prop = type.GetProperty("UserFullName");
            if (prop != null)
            {
                var val = prop.GetValue(audit) as string;
                if (!string.IsNullOrWhiteSpace(val))
                    return val;
            }
            if (!string.IsNullOrWhiteSpace(audit.Username))
                return audit.Username;
            if (audit.UserId.HasValue)
                return $"User#{audit.UserId.Value}";
            return "Unknown";
        }

        /// <summary>
        /// Robust wrapper for loading audits using current filters.
        /// </summary>
        /// <param name="user">Filter: user full name/username.</param>
        /// <param name="entity">Filter: audited entity/table.</param>
        /// <param name="action">Filter: action type.</param>
        /// <param name="from">From date (inclusive).</param>
        /// <param name="to">To date (inclusive).</param>
        /// <returns>Collection of audit entries.</returns>
        private async Task<ObservableCollection<AuditEntryDto>> GetFilteredAuditsSafe(
            string? user, string? entity, string? action, DateTime from, DateTime to)
        {
            // Normalize nullable filters to empty strings if the service expects non-null.
            var audits = await _auditService
                .GetFilteredAudits(user ?? string.Empty, entity ?? string.Empty, action ?? string.Empty, from, to)
                .ConfigureAwait(false);

            return new ObservableCollection<AuditEntryDto>(
                (IEnumerable<AuditEntryDto>?)audits ?? Enumerable.Empty<AuditEntryDto>());
        }
    }
}
