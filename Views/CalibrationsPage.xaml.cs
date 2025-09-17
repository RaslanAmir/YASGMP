using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel; // MainThread
using Microsoft.Maui.Controls;
using MySqlConnector;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Views.Dialogs;

namespace YasGMP.Views
{
    /// <summary>
    /// <b>CalibrationsPage</b> — Master ekran za upravljanje kalibracijama.
    /// UI-thread sigurno učitavanje/prefiltriranje, bez CS8601 upozorenja (koalesciranjem mogućih null vrijednosti).
    /// Svi UI pozivi (navigacija/alert) preko <see cref="SafeNavigator"/> ili <see cref="MainThread"/>.
    /// </summary>
    public partial class CalibrationsPage : ContentPage
    {
        /// <summary>Prikazna kolekcija kalibracija (bindati na ListView/CollectionView).</summary>
        public ObservableCollection<Calibration> Calibrations { get; } = new();

        private readonly DatabaseService _dbService;
        private readonly ExportService _exportService;

        // Lookup liste (nenullabilne)
        private List<MachineComponent> _components = new();
        private List<Supplier> _suppliers = new();
        private List<User> _users = new();

        // Izvorni (nefiltrirani) skup za lokalno filtriranje u memoriji
        private readonly List<Calibration> _allCalibrations = new();

        // Stanje filtera
        private string? _searchQuery;
        private MachineComponent? _filterComponent;
        private DateTime? _filterFrom;
        private DateTime? _filterTo;

        /// <summary>Konstruktor s učitavanjem konfiguracije i inicijalnih podataka.</summary>
        public CalibrationsPage(DatabaseService dbService, ExportService exportService)
        {
            InitializeComponent();

            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

            BindingContext = this;

            // Fire-and-forget inicijalno punjenje (UI ažuriranja se maršaliraju na UI thread)
            _ = LoadLookupsAsync();
            _ = LoadCalibrationsAsync();
        }

        /// <summary>Parameterless ctor for XAML/HotReload. Resolves services via ServiceLocator.</summary>
        public CalibrationsPage()
            : this(
                ServiceLocator.GetRequiredService<DatabaseService>(),
                ServiceLocator.GetRequiredService<ExportService>())
        {
        }

        #region Data Loading

        /// <summary>Učitava lookup tablice i garantira nenullabilne kolekcije.</summary>
        private async Task LoadLookupsAsync()
        {
            _components = await _dbService.GetAllComponentsAsync().ConfigureAwait(false) ?? new List<MachineComponent>();
            _suppliers  = await _dbService.GetAllSuppliersAsync().ConfigureAwait(false)  ?? new List<Supplier>();
            _users      = await _dbService.GetAllUsersAsync().ConfigureAwait(false)      ?? new List<User>();
        }

        /// <summary>Učitava kalibracije u memorijski izvor i osvježava UI (filtrirano).</summary>
        private async Task LoadCalibrationsAsync()
        {
            try
            {
                // 1) Dohvati sve iz baze (u pozadini)
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                var dt = await _dbService
                    .ExecuteSelectAsync(
                        "SELECT id, component_id, supplier_id, calibration_date, next_due, cert_doc, result, comment, digital_signature, last_modified, last_modified_by_id FROM calibrations ORDER BY calibration_date DESC",
                        null,
                        cts.Token)
                    .ConfigureAwait(false);

                // 2) Izgradi privremenu listu modela (izvan UI threada)
                var buffer = new List<Calibration>(capacity: dt.Rows.Count);

                foreach (System.Data.DataRow row in dt.Rows)
                {
                    int GetInt(string col) =>
                        row.Table.Columns.Contains(col) && row[col] is not DBNull
                            ? Convert.ToInt32(row[col])
                            : 0;

                    DateTime GetDate(string col) =>
                        row.Table.Columns.Contains(col) && row[col] is not DBNull
                            ? Convert.ToDateTime(row[col])
                            : DateTime.MinValue;

                    string GetStrNN(string col) =>
                        row.Table.Columns.Contains(col) && row[col] is not DBNull
                            ? (row[col]?.ToString() ?? string.Empty)
                            : string.Empty;

                    buffer.Add(new Calibration
                    {
                        Id               = GetInt("id"),
                        ComponentId      = GetInt("component_id"),
                        SupplierId       = GetInt("supplier_id"),
                        CalibrationDate  = GetDate("calibration_date"),
                        NextDue          = GetDate("next_due"),
                        CertDoc          = GetStrNN("cert_doc"),
                        Result           = GetStrNN("result"),
                        Comment          = GetStrNN("comment"),
                        DigitalSignature = GetStrNN("digital_signature"),
                        LastModified     = row.Table.Columns.Contains("last_modified") ? GetDate("last_modified") : DateTime.UtcNow,
                        LastModifiedById = row.Table.Columns.Contains("last_modified_by_id") ? GetInt("last_modified_by_id") : 0
                    });
                }

                // 3) Zamijeni izvor i primijeni filter (UI-thread)
                _allCalibrations.Clear();
                _allCalibrations.AddRange(buffer);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Neuspješno učitavanje kalibracija: {ex.Message}", "OK");
            }
        }

        #endregion

        #region Filter & Search

        /// <summary>Primjenjuje trenutno stanje filtera nad izvornom (_allCalibrations) kolekcijom.</summary>
        private void ApplyFilter()
        {
            IEnumerable<Calibration> filtered = _allCalibrations;

            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                filtered = filtered.Where(c =>
                    (c.Comment ?? string.Empty).Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    (c.Result  ?? string.Empty).Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    (c.CertDoc ?? string.Empty).Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            if (_filterComponent is not null)
                filtered = filtered.Where(c => c.ComponentId == _filterComponent.Id);

            if (_filterFrom.HasValue)
                filtered = filtered.Where(c => c.CalibrationDate >= _filterFrom.Value);

            if (_filterTo.HasValue)
                filtered = filtered.Where(c => c.CalibrationDate <= _filterTo.Value);

            var snapshot = filtered.ToList();

            // UI kolekciju ažuriraj na UI threadu
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Calibrations.Clear();
                foreach (var item in snapshot)
                    Calibrations.Add(item);
            });
        }

        /// <summary>Postavlja filtere i odmah ih primjenjuje.</summary>
        public void SetFilter(string? query, MachineComponent? comp, DateTime? from, DateTime? to)
        {
            _searchQuery    = query;
            _filterComponent= comp;
            _filterFrom     = from;
            _filterTo       = to;
            ApplyFilter();
        }

        /// <summary>Resetira sve filtere i ponovno primjenjuje na lokalni izvor.</summary>
        public void ResetFilter()
        {
            _searchQuery = null;
            _filterComponent = null;
            _filterFrom = null;
            _filterTo = null;
            ApplyFilter();
        }

        #endregion

        #region CRUD + Audit

        /// <summary>Dodavanje nove kalibracije putem modalnog dijaloga.</summary>
        private async void OnAddCalibrationClicked(object? sender, EventArgs e)
        {
            var currentUser = (Application.Current as App)?.LoggedUser;
            var signature = currentUser?.FullName ?? currentUser?.Username ?? "Nepoznat";
            var userId = currentUser?.Id ?? 0;

            var cal = new Calibration
            {
                CalibrationDate  = DateTime.Today,
                NextDue          = DateTime.Today.AddMonths(6),
                Result           = "prolaz",
                DigitalSignature = signature
            };

            var dialog = new CalibrationEditDialog(cal, _components, _suppliers);
            dialog.OnSave = async updatedCal =>
            {
                updatedCal.LastModified     = DateTime.UtcNow;
                updatedCal.LastModifiedById = userId;
                updatedCal.SourceIp         = DependencyService.Get<IPlatformService>()?.GetLocalIpAddress() ?? string.Empty;

                const string sql = @"INSERT INTO calibrations
                    (component_id, supplier_id, calibration_date, next_due, cert_doc, result, comment, digital_signature, last_modified, last_modified_by_id)
                    VALUES (@cid, @supid, @cdate, @ndue, @cert, @res, @comm, @sig, @mod, @modby)";

                var pars = new[]
                {
                    new MySqlParameter("@cid",   updatedCal.ComponentId),
                    new MySqlParameter("@supid", updatedCal.SupplierId),
                    new MySqlParameter("@cdate", updatedCal.CalibrationDate),
                    new MySqlParameter("@ndue",  updatedCal.NextDue),
                    new MySqlParameter("@cert",  updatedCal.CertDoc ?? string.Empty),
                    new MySqlParameter("@res",   updatedCal.Result ?? string.Empty),
                    new MySqlParameter("@comm",  updatedCal.Comment ?? string.Empty),
                    new MySqlParameter("@sig",   updatedCal.DigitalSignature ?? string.Empty),
                    new MySqlParameter("@mod",   updatedCal.LastModified),
                    new MySqlParameter("@modby", updatedCal.LastModifiedById)
                };

                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _dbService.ExecuteNonQueryAsync(sql, pars, cts.Token).ConfigureAwait(false);
                await LogAudit("CREATE", updatedCal.Id, signature).ConfigureAwait(false);
                await LoadCalibrationsAsync().ConfigureAwait(false);
            };

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Navigation.PushModalAsync(dialog);
            });
        }

        /// <summary>Uređivanje postojeće kalibracije putem modalnog dijaloga.</summary>
        private async void OnEditCalibrationClicked(object? sender, EventArgs e)
        {
            // Izbjegavamo generike (XAML kompajler)
            var listView = this.FindByName("CalibrationsListView") as ListView;
            if (listView?.SelectedItem is not Calibration selected)
            {
                await SafeNavigator.ShowAlertAsync("Obavijest", "Odaberite kalibraciju za uređivanje.", "OK");
                return;
            }

            var currentUser = (Application.Current as App)?.LoggedUser;
            var signature = currentUser?.FullName ?? currentUser?.Username ?? "Nepoznat";
            var userId = currentUser?.Id ?? 0;

            var dialog = new CalibrationEditDialog(selected, _components, _suppliers);
            dialog.OnSave = async updatedCal =>
            {
                updatedCal.LastModified     = DateTime.UtcNow;
                updatedCal.LastModifiedById = userId;
                updatedCal.SourceIp         = DependencyService.Get<IPlatformService>()?.GetLocalIpAddress() ?? string.Empty;

                const string sql = @"UPDATE calibrations SET
                    component_id=@cid, supplier_id=@supid, calibration_date=@cdate, next_due=@ndue,
                    cert_doc=@cert, result=@res, comment=@comm, digital_signature=@sig,
                    last_modified=@mod, last_modified_by_id=@modby
                    WHERE id=@id";

                var pars = new[]
                {
                    new MySqlParameter("@cid",   updatedCal.ComponentId),
                    new MySqlParameter("@supid", updatedCal.SupplierId),
                    new MySqlParameter("@cdate", updatedCal.CalibrationDate),
                    new MySqlParameter("@ndue",  updatedCal.NextDue),
                    new MySqlParameter("@cert",  updatedCal.CertDoc ?? string.Empty),
                    new MySqlParameter("@res",   updatedCal.Result ?? string.Empty),
                    new MySqlParameter("@comm",  updatedCal.Comment ?? string.Empty),
                    new MySqlParameter("@sig",   updatedCal.DigitalSignature ?? string.Empty),
                    new MySqlParameter("@mod",   updatedCal.LastModified),
                    new MySqlParameter("@modby", updatedCal.LastModifiedById),
                    new MySqlParameter("@id",    updatedCal.Id)
                };

                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _dbService.ExecuteNonQueryAsync(sql, pars, cts.Token).ConfigureAwait(false);
                await LogAudit("UPDATE", updatedCal.Id, signature).ConfigureAwait(false);
                await LoadCalibrationsAsync().ConfigureAwait(false);
            };

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Navigation.PushModalAsync(dialog);
            });
        }

        #endregion

        #region Export

        /// <summary>Export u Excel.</summary>
        private async void OnExportExcelClicked(object? sender, EventArgs e)
        {
            await _exportService.ExportToExcelAsync(Calibrations.ToList()).ConfigureAwait(false);
            var signature = (Application.Current as App)?.LoggedUser?.FullName
                         ?? (Application.Current as App)?.LoggedUser?.Username
                         ?? "Nepoznat";
            await LogAudit("EXPORT_EXCEL", 0, signature).ConfigureAwait(false);
            await SafeNavigator.ShowAlertAsync("Export", "Excel datoteka generirana!", "OK");
        }

        /// <summary>Export u PDF.</summary>
        private async void OnExportPdfClicked(object? sender, EventArgs e)
        {
            await _exportService.ExportToPdfAsync(Calibrations.ToList()).ConfigureAwait(false);
            var signature = (Application.Current as App)?.LoggedUser?.FullName
                         ?? (Application.Current as App)?.LoggedUser?.Username
                         ?? "Nepoznat";
            await LogAudit("EXPORT_PDF", 0, signature).ConfigureAwait(false);
            await SafeNavigator.ShowAlertAsync("Export", "PDF izvještaj generiran!", "OK");
        }

        #endregion

        #region Audit

        /// <summary>Audit via canonical system events (replaces legacy audit_log table).</summary>
        private async Task LogAudit(string action, int entityId, string user)
        {
            await _dbService.LogSystemEventAsync(
                userId: (Application.Current as App)?.LoggedUser?.Id,
                eventType: action ?? string.Empty,
                tableName: "calibrations",
                module: "CalibrationsPage",
                recordId: entityId == 0 ? null : entityId,
                description: $"user={user ?? "Nepoznat"}",
                ip: DependencyService.Get<IPlatformService>()?.GetLocalIpAddress() ?? string.Empty,
                severity: "audit",
                deviceInfo: string.Empty,
                sessionId: (Application.Current as App)?.SessionId
            ).ConfigureAwait(false);
        }

        #endregion
    }
}
