using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>CalibrationsViewModel</b> – Master ViewModel for equipment calibration &amp; qualification (GMP/Annex 11/21 CFR Part 11).
    /// Provides async loading, robust filtering, KPI counters, exports, dialog launchers, and auditing hooks.
    /// All UI-bound collection changes are marshaled to the UI thread.
    /// </summary>
    public class CalibrationsViewModel : INotifyPropertyChanged
    {
        #region === Services & Fields ===

        private readonly DatabaseService _dbService;
        private readonly AuditService _auditService;
        private readonly ExportService _exportService;
        private readonly IUiDispatcher _dispatcher;
        private readonly IUserSession _userSession;

        /// <summary>Backing store for all calibrations (source for filtering and analytics).</summary>
        private readonly List<Calibration> _allCalibrations = new();

        /// <summary>Current list of machine components for lookups.</summary>
        public List<MachineComponent> Components { get; private set; } = new();

        /// <summary>Current list of suppliers for lookups.</summary>
        public List<Supplier> Suppliers { get; private set; } = new();

        /// <summary>Current list of users for lookups.</summary>
        public List<User> Users { get; private set; } = new();

        // Backing fields (nullable to allow safe resets)
        private Calibration? _selectedCalibration;
        private string? _searchTerm;
        private string? _statusFilter;
        private MachineComponent? _componentFilter;
        private Supplier? _supplierFilter;
        private DateTime? _dueBeforeFilter;
        private bool _isBusy;
        private string? _statusMessage;

        #endregion

        #region === Observable Collections ===

        /// <summary>All calibrations for display. Bind this to the grid/list.</summary>
        public ObservableCollection<Calibration> Calibrations { get; } = new();

        /// <summary>Filtered calibrations bound to the UI.</summary>
        public ObservableCollection<Calibration> FilteredCalibrations { get; } = new();

        #endregion

        #region === Selected & Filters ===

        /// <summary>Currently selected calibration in the UI (nullable).</summary>
        public Calibration? SelectedCalibration
        {
            get => _selectedCalibration;
            set
            {
                if (_selectedCalibration != value)
                {
                    _selectedCalibration = value;
                    OnPropertyChanged();
                    if (EditCommand is DelegateCommand edit)
                        edit.RaiseCanExecuteChanged();
                    if (DeleteCommand is AsyncDelegateCommand delete)
                        delete.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>Search term for full-text search (code, comment, result, etc.).</summary>
        public string? SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); ApplyFilter(); }
        }

        /// <summary>Status filter (valid, expired, due, scheduled, overdue, rejected).</summary>
        public string? StatusFilter
        {
            get => _statusFilter;
            set { _statusFilter = value; OnPropertyChanged(); ApplyFilter(); }
        }

        /// <summary>Component filter (by machine/component).</summary>
        public MachineComponent? ComponentFilter
        {
            get => _componentFilter;
            set { _componentFilter = value; OnPropertyChanged(); ApplyFilter(); }
        }

        /// <summary>Supplier filter (by supplier/lab).</summary>
        public Supplier? SupplierFilter
        {
            get => _supplierFilter;
            set { _supplierFilter = value; OnPropertyChanged(); ApplyFilter(); }
        }

        /// <summary>Due before filter (only calibrations due before this date).</summary>
        public DateTime? DueBeforeFilter
        {
            get => _dueBeforeFilter;
            set { _dueBeforeFilter = value; OnPropertyChanged(); ApplyFilter(); }
        }

        #endregion

        #region === State/KPI ===

        /// <summary>True if busy (loading or exporting data).</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        /// <summary>Status message for the UI (errors, operations, etc.).</summary>
        public string? StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        /// <summary>All available calibration statuses.</summary>
        public string[] AvailableStatuses => new[] { "valid", "expired", "due", "scheduled", "overdue", "rejected" };

        /// <summary>Total calibrations in the current view.</summary>
        public int TotalCalibrations => FilteredCalibrations.Count;

        /// <summary>Number of overdue calibrations.</summary>
        public int OverdueCount => FilteredCalibrations.Count(c => GetStatus(c) == "overdue");

        /// <summary>Number of calibrations due within 30 days.</summary>
        public int DueSoonCount => FilteredCalibrations.Count(c => GetStatus(c) == "due");

        /// <summary>Number of rejected calibrations.</summary>
        public int RejectedCount => FilteredCalibrations.Count(c => GetStatus(c) == "rejected");

        #endregion

        #region === Commands ===

        /// <summary>Load all (lookups + calibrations).</summary>
        public ICommand LoadAllCommand { get; }

        /// <summary>Open add dialog.</summary>
        public ICommand AddCommand { get; }

        /// <summary>Open edit dialog for selected.</summary>
        public ICommand EditCommand { get; }

        /// <summary>Delete selected calibration.</summary>
        public ICommand DeleteCommand { get; }

        /// <summary>Export filtered list to Excel.</summary>
        public ICommand ExportExcelCommand { get; }

        /// <summary>Export filtered list to PDF.</summary>
        public ICommand ExportPdfCommand { get; }

        /// <summary>Reset all filters.</summary>
        public ICommand ResetFilterCommand { get; }

        /// <summary>Reload data (same as LoadAll).</summary>
        public ICommand RefreshCommand { get; }

        #endregion

        #region === Construction & Initialization ===

        /// <summary>
        /// Initializes a new instance of <see cref="CalibrationsViewModel"/> and wires up services and commands.
        /// </summary>
        public CalibrationsViewModel(DatabaseService dbService, AuditService auditService, ExportService exportService, IUiDispatcher dispatcher, IUserSession userSession)
        {
            _dbService     = dbService    ?? throw new ArgumentNullException(nameof(dbService));
            _auditService  = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _dispatcher    = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _userSession   = userSession ?? throw new ArgumentNullException(nameof(userSession));

            LoadAllCommand     = new AsyncDelegateCommand(LoadAllAsync);
            AddCommand         = new DelegateCommand(OnAddCalibrationRequested);
            EditCommand        = new DelegateCommand(OnEditCalibrationRequested, () => SelectedCalibration != null);
            DeleteCommand      = new AsyncDelegateCommand(DeleteCalibrationAsync, () => SelectedCalibration != null);
            ExportExcelCommand = new AsyncDelegateCommand(ExportExcelAsync);
            ExportPdfCommand   = new AsyncDelegateCommand(ExportPdfAsync);
            ResetFilterCommand = new DelegateCommand(ResetFilters);
            RefreshCommand     = new AsyncDelegateCommand(LoadAllAsync);

            _ = LoadAllAsync();
        }

        /// <summary>
        /// Parameterless constructor retained for XAML/HotReload. Resolves services from the global locator.
        /// </summary>
        public CalibrationsViewModel()
            : this(
                ServiceLocator.GetRequiredService<DatabaseService>(),
                ServiceLocator.GetRequiredService<AuditService>(),
                ServiceLocator.GetRequiredService<ExportService>(),
                ServiceLocator.GetRequiredService<IUiDispatcher>(),
                ServiceLocator.GetRequiredService<IUserSession>())
        {
        }

        #endregion

        #region === Data Loading & CRUD ===

        /// <summary>Loads all lookups and calibrations.</summary>
        public async Task LoadAllAsync()
        {
            IsBusy = true;
            try
            {
                await LoadLookupsAsync().ConfigureAwait(false);
                await LoadCalibrationsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading data: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Loads components, suppliers, and users for lookups.</summary>
        private async Task LoadLookupsAsync()
        {
            Components = (await _dbService.GetAllComponentsAsync().ConfigureAwait(false)) ?? new List<MachineComponent>();
            Suppliers  = (await _dbService.GetAllSuppliersAsync().ConfigureAwait(false))  ?? new List<Supplier>();
            Users      = (await _dbService.GetAllUsersAsync().ConfigureAwait(false))      ?? new List<User>();
        }

        /// <summary>Loads all calibrations from the database (ordered, robust).</summary>
        public async Task LoadCalibrationsAsync()
        {
            IsBusy = true;

            try
            {
                // Clear backing store and UI collection safely
                _allCalibrations.Clear();
                await _dispatcher.InvokeAsync(() => Calibrations.Clear()).ConfigureAwait(false);

                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                DataTable dt = await _dbService
                    .ExecuteSelectAsync(
                        "SELECT id, component_id, supplier_id, calibration_date, next_due, cert_doc, result, comment, digital_signature, last_modified, last_modified_by_id FROM calibrations ORDER BY calibration_date DESC",
                        null,
                        cts.Token)
                    .ConfigureAwait(false);

                foreach (DataRow row in dt.Rows)
                {
                    var calibration = new Calibration
                    {
                        Id               = row.Table.Columns.Contains("id")                && row["id"]                != DBNull.Value ? Convert.ToInt32(row["id"]) : 0,
                        ComponentId      = row.Table.Columns.Contains("component_id")      && row["component_id"]      != DBNull.Value ? Convert.ToInt32(row["component_id"]) : 0,
                        SupplierId       = row.Table.Columns.Contains("supplier_id")       && row["supplier_id"]       != DBNull.Value ? Convert.ToInt32(row["supplier_id"]) : (int?)null,
                        CalibrationDate  = row.Table.Columns.Contains("calibration_date")  && row["calibration_date"]  != DBNull.Value ? Convert.ToDateTime(row["calibration_date"]) : DateTime.Now,
                        NextDue          = row.Table.Columns.Contains("next_due")          && row["next_due"]          != DBNull.Value ? Convert.ToDateTime(row["next_due"]) : DateTime.Now,
                        CertDoc          = row.Table.Columns.Contains("cert_doc")          && row["cert_doc"]          != DBNull.Value ? (row["cert_doc"]?.ToString() ?? string.Empty) : string.Empty,
                        Result           = row.Table.Columns.Contains("result")            && row["result"]            != DBNull.Value ? (row["result"]?.ToString() ?? string.Empty) : string.Empty,
                        Comment          = row.Table.Columns.Contains("comment")           && row["comment"]           != DBNull.Value ? (row["comment"]?.ToString() ?? string.Empty) : string.Empty,
                        DigitalSignature = row.Table.Columns.Contains("digital_signature") && row["digital_signature"] != DBNull.Value ? (row["digital_signature"]?.ToString() ?? string.Empty) : string.Empty,
                        LastModified     = row.Table.Columns.Contains("last_modified")     && row["last_modified"]     != DBNull.Value ? Convert.ToDateTime(row["last_modified"]) : DateTime.Now,
                        LastModifiedById = row.Table.Columns.Contains("last_modified_by_id") && row["last_modified_by_id"] != DBNull.Value ? Convert.ToInt32(row["last_modified_by_id"]) : (int?)null
                    };

                    _allCalibrations.Add(calibration);
                }

                // Refresh the UI-bound collection and KPIs on the UI thread
                ApplyFilter();

                await _auditService.LogCalibrationAuditAsync(0, "LOAD", $"Loaded {_allCalibrations.Count} calibrations").ConfigureAwait(false);
                StatusMessage = $"Loaded {_allCalibrations.Count} calibrations.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading calibrations: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Deletes the selected calibration, with audit logging.</summary>
        public async Task DeleteCalibrationAsync()
        {
            if (SelectedCalibration == null) return;

            IsBusy = true;
            try
            {
                var pars = new[]
                {
                    new MySqlConnector.MySqlParameter("@id", SelectedCalibration.Id)
                };

                await _dbService.ExecuteNonQueryAsync("DELETE FROM calibrations WHERE id=@id", pars).ConfigureAwait(false);
                await _auditService.LogCalibrationAuditAsync(SelectedCalibration.Id, "DELETE", $"Deleted calibration ID={SelectedCalibration.Id}").ConfigureAwait(false);

                // Remove from collections on the UI thread
                var toRemove = SelectedCalibration;
                await _dispatcher.InvokeAsync(() =>
                {
                    Calibrations.Remove(toRemove);
                    FilteredCalibrations.Remove(toRemove);
                }).ConfigureAwait(false);

                SelectedCalibration = null;
                StatusMessage = "Calibration deleted.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region === Dialog Event Launchers ===

        /// <summary>
        /// Event used by the View to open the Add/Edit dialog.
        /// <para>Signature: <c>(isEdit, model, components, suppliers)</c></para>
        /// </summary>
        public event Action<bool, Calibration, List<MachineComponent>, List<Supplier>>? EditCalibrationRequested;

        /// <summary>Launches the add calibration dialog with sensible defaults.</summary>
        private void OnAddCalibrationRequested()
        {
            var newCalibration = new Calibration
            {
                CalibrationDate  = DateTime.Now,
                NextDue          = DateTime.Now.AddYears(1),
                DigitalSignature = _userSession.FullName ?? _userSession.Username ?? "Nepoznat korisnik"
            };

            EditCalibrationRequested?.Invoke(false, newCalibration, Components, Suppliers);
        }

        /// <summary>Launches the edit calibration dialog for the currently selected item.</summary>
        private void OnEditCalibrationRequested()
        {
            if (SelectedCalibration == null) return;

            var editCal = new Calibration
            {
                Id               = SelectedCalibration.Id,
                ComponentId      = SelectedCalibration.ComponentId,
                SupplierId       = SelectedCalibration.SupplierId,
                CalibrationDate  = SelectedCalibration.CalibrationDate,
                NextDue          = SelectedCalibration.NextDue,
                CertDoc          = SelectedCalibration.CertDoc,
                Result           = SelectedCalibration.Result,
                Comment          = SelectedCalibration.Comment,
                DigitalSignature = SelectedCalibration.DigitalSignature,
                LastModified     = SelectedCalibration.LastModified,
                LastModifiedById = SelectedCalibration.LastModifiedById
            };

            EditCalibrationRequested?.Invoke(true, editCal, Components, Suppliers);
        }

        #endregion

        #region === Export & Reporting ===

        /// <summary>Exports the currently filtered calibrations to Excel with auditing.</summary>
        public async Task ExportExcelAsync()
        {
            try
            {
                var filterMeta = GetFilterCriteria();
                var file       = await _exportService.ExportToExcelAsync(FilteredCalibrations, filterMeta).ConfigureAwait(false);
                await _auditService.LogCalibrationExportAsync("EXCEL", file, filterMeta).ConfigureAwait(false);
                StatusMessage = "Excel export completed.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Excel export failed: {ex.Message}";
            }
        }

        /// <summary>Exports the currently filtered calibrations to PDF with auditing.</summary>
        public async Task ExportPdfAsync()
        {
            try
            {
                var filterMeta = GetFilterCriteria();
                var file       = await _exportService.ExportToPdfAsync(FilteredCalibrations, filterMeta).ConfigureAwait(false);
                await _auditService.LogCalibrationExportAsync("PDF", file, filterMeta).ConfigureAwait(false);
                StatusMessage = "PDF export completed.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"PDF export failed: {ex.Message}";
            }
        }

        #endregion

        #region === Filtering & KPI ===

        /// <summary>
        /// Applies all filter criteria to the loaded calibration list and updates KPI counters.
        /// Executes all collection mutations on the UI thread to avoid cross-thread exceptions.
        /// </summary>
        public void ApplyFilter()
        {
            IEnumerable<Calibration> filtered = _allCalibrations;

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                filtered = filtered.Where(c =>
                    (c.Comment  ?? string.Empty).Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (c.Result   ?? string.Empty).Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    (c.CertDoc  ?? string.Empty).Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter))
                filtered = filtered.Where(c => string.Equals(GetStatus(c), StatusFilter, StringComparison.OrdinalIgnoreCase));

            if (ComponentFilter != null)
                filtered = filtered.Where(c => c.ComponentId == ComponentFilter.Id);

            if (SupplierFilter != null)
                filtered = filtered.Where(c => c.SupplierId == SupplierFilter.Id);

            if (DueBeforeFilter.HasValue)
                filtered = filtered.Where(c => c.NextDue <= DueBeforeFilter.Value);

            var list = filtered.ToList();

            // Update UI-bound collections + KPIs on UI thread
            _dispatcher.BeginInvoke(() =>
            {
                FilteredCalibrations.Clear();
                foreach (var cal in list)
                    FilteredCalibrations.Add(cal);

                OnPropertyChanged(nameof(TotalCalibrations));
                OnPropertyChanged(nameof(OverdueCount));
                OnPropertyChanged(nameof(DueSoonCount));
                OnPropertyChanged(nameof(RejectedCount));
                StatusMessage = $"Filtered: {FilteredCalibrations.Count} calibrations shown.";
            });
        }

        /// <summary>
        /// Resets all filters to their defaults (clears to <c>null</c> safely) and reapplies.
        /// Uses backing fields and explicit change notifications.
        /// </summary>
        public void ResetFilters()
        {
            _searchTerm      = null;
            _statusFilter    = null;
            _componentFilter = null;
            _supplierFilter  = null;
            _dueBeforeFilter = null;

            OnPropertyChanged(nameof(SearchTerm));
            OnPropertyChanged(nameof(StatusFilter));
            OnPropertyChanged(nameof(ComponentFilter));
            OnPropertyChanged(nameof(SupplierFilter));
            OnPropertyChanged(nameof(DueBeforeFilter));

            ApplyFilter();
        }

        /// <summary>Returns a string describing the active filter set for reporting/audit/export.</summary>
        private string GetFilterCriteria() =>
            $"query='{SearchTerm}', status={StatusFilter}, component={ComponentFilter?.Name}, supplier={SupplierFilter?.Name}, dueBefore={DueBeforeFilter?.ToString("yyyy-MM-dd")}";

        /// <summary>
        /// Computes the GMP status of a calibration: "valid", "expired", "due", "overdue", "rejected", or "scheduled".
        /// </summary>
        public static string GetStatus(Calibration cal)
        {
            if (cal == null) return "unknown";
            if (!string.IsNullOrWhiteSpace(cal.Result) && cal.Result.ToLower().Contains("reject")) return "rejected";
            if (cal.NextDue < DateTime.Now) return "overdue";
            if (cal.NextDue <= DateTime.Now.AddDays(30)) return "due";
            if (cal.NextDue > DateTime.Now.AddDays(30)) return "valid";
            return "scheduled";
        }

        #endregion

        #region === INotifyPropertyChanged ===

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Raises property change notifications for MVVM binding.</summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));

        #endregion
    }
}
