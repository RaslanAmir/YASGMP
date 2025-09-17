// ==============================================================================
//  File: Views/ComponentsPage.xaml.cs
//  Project: YasGMP
//  Summary:
//      Pregled/unos/uređivanje/brisanje strojnih komponenti (machine_components) – MySQL.
//      • UI-thread safe (MainThread + SafeNavigator)
//      • Poravnato s YASGMP.sql tablicom 'machine_components' (sop_doc, nullable machine_id)
//      • Korištenje DatabaseService + MySqlConnector parametara
// ==============================================================================
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using MySqlConnector;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.ViewModels;

namespace YasGMP.Views
{
    /// <summary>
    /// <b>ComponentsPage</b> – pregled, unos, uređivanje i brisanje komponenti strojeva (tablica <c>machine_components</c>).
    /// Kolone: <c>id, machine_id, code, name, type, sop_doc, status, install_date</c> (+ soft delete polja).
    /// </summary>
    public partial class ComponentsPage : ContentPage
    {
        private readonly DatabaseService _dbService;
        private readonly ComponentViewModel _viewModel;
        private readonly Dictionary<int, string> _machineLookup = new();
        private Task? _machinesPreloadTask;

        /// <summary>
        /// Inicijalizira stranicu, rješava konekcijski string i učitava podatke.
        /// </summary>
        /// <exception cref="InvalidOperationException">Ako aplikacija nema connection string.</exception>
        public ComponentsPage(DatabaseService dbService)
        {
            InitializeComponent();

            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));

            var authService = ServiceLocator.GetRequiredService<AuthService>();
            var exportService = ServiceLocator.GetRequiredService<ExportService>();
            _viewModel = new ComponentViewModel(_dbService, authService, exportService, autoLoad: false);

            BindingContext = _viewModel;

            _machinesPreloadTask = LoadMachinesAsync();
            _ = LoadComponentsAsync();
        }

        /// <summary>Parameterless ctor for Shell/XAML koji koristi ServiceLocator.</summary>
        public ComponentsPage()
            : this(ServiceLocator.GetRequiredService<DatabaseService>())
        {
        }

        /// <summary>
        /// Učitava sve komponente (neobrisane) i puni kolekciju na glavnoj niti.
        /// </summary>
        private async Task LoadComponentsAsync()
        {
            await WaitForMachineLookupAsync().ConfigureAwait(false);

            const string sql = @"
SELECT id, machine_id, code, name, type, sop_doc, status, install_date
FROM machine_components
WHERE is_deleted = 0 OR is_deleted IS NULL
ORDER BY name;";

            try
            {
                DataTable dt = await _dbService.ExecuteSelectAsync(sql).ConfigureAwait(false);

                var list = new List<MachineComponent>(capacity: dt.Rows.Count);
                foreach (DataRow row in dt.Rows)
                {
                    var machineId = row["machine_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["machine_id"]);
                    list.Add(new MachineComponent
                    {
                        Id          = row["id"]           == DBNull.Value ? 0 : Convert.ToInt32(row["id"]),
                        MachineId   = machineId,
                        Code        = row["code"]         == DBNull.Value ? string.Empty : row["code"]?.ToString() ?? string.Empty,
                        Name        = row["name"]         == DBNull.Value ? string.Empty : row["name"]?.ToString() ?? string.Empty,
                        Type        = row["type"]         == DBNull.Value ? string.Empty : row["type"]?.ToString() ?? string.Empty,
                        SopDoc      = row["sop_doc"]      == DBNull.Value ? string.Empty : row["sop_doc"]?.ToString() ?? string.Empty,
                        Status      = row["status"]       == DBNull.Value ? "active" : row["status"]?.ToString() ?? "active",
                        InstallDate = row["install_date"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(row["install_date"])
                    });
                    if (machineId.HasValue && _machineLookup.TryGetValue(machineId.Value, out var machineName))
                    {
                        list[^1].Machine = new Machine
                        {
                            Id   = machineId.Value,
                            Name = machineName
                        };
                    }
                }

                // Load documents count for all listed components in a single query
                try
                {
                    var ids = list.ConvertAll(c => c.Id);
                    if (ids.Count > 0)
                    {
                        var pars = new List<MySqlParameter> { new MySqlParameter("@et", "Component") };
                        var inClauses = new List<string>(ids.Count);
                        for (int i = 0; i < ids.Count; i++)
                        {
                            string name = "@id" + i.ToString();
                            inClauses.Add(name);
                            pars.Add(new MySqlParameter(name, ids[i]));
                        }

                        string sqlCnt = $"SELECT entity_id, COUNT(*) AS cnt FROM document_links WHERE entity_type=@et AND entity_id IN ({string.Join(",", inClauses)}) GROUP BY entity_id";
                        var dtCnt = await _dbService.ExecuteSelectAsync(sqlCnt, pars).ConfigureAwait(false);
                        var map = new Dictionary<int, int>(dtCnt.Rows.Count);
                        foreach (DataRow r in dtCnt.Rows)
                        {
                            int eid = r["entity_id"] == DBNull.Value ? 0 : Convert.ToInt32(r["entity_id"]);
                            int cnt = r["cnt"] == DBNull.Value ? 0 : Convert.ToInt32(r["cnt"]);
                            map[eid] = cnt;
                        }
                        foreach (var c in list)
                            if (map.TryGetValue(c.Id, out var cnt)) c.DocumentsCount = cnt;
                    }
                }
                catch { }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _viewModel.Components = new ObservableCollection<MachineComponent>(list);
                    _viewModel.FilterComponents();
                    if (ComponentListView is not null)
                        ComponentListView.SelectedItem = null;
                    _viewModel.SelectedComponent = null;
                });
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Komponente", $"Učitavanje nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Dodavanje nove komponente – promptira korisnika i sprema u bazu.
        /// </summary>
        private async void OnAddComponentClicked(object? sender, EventArgs e)
        {
            try
            {
                var c = new MachineComponent();

                bool ok = await ShowComponentFormAsync(c, "Unesi novu komponentu");
                if (!ok) return;

                c.Status = MachineComponent.NormalizeStatus(c.Status);

                const string sql = @"
INSERT INTO machine_components (machine_id, code, name, type, sop_doc, status, install_date)
VALUES (@machine_id, @code, @name, @type, @sop_doc, @status, @install_date);";

                var pars = new[]
                {
                    new MySqlParameter("@machine_id",  (object?)c.MachineId ?? DBNull.Value),
                    new MySqlParameter("@code",        c.Code ?? string.Empty),
                    new MySqlParameter("@name",        c.Name ?? string.Empty),
                    new MySqlParameter("@type",        c.Type ?? string.Empty),
                    new MySqlParameter("@sop_doc",     c.SopDoc ?? string.Empty),
                    new MySqlParameter("@status",      c.Status ?? "active"),
                    new MySqlParameter("@install_date", c.InstallDate ?? (object)DBNull.Value) { MySqlDbType = MySqlDbType.Date }
                };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadComponentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Komponente", $"Spremanje nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Uređivanje odabrane komponente – promptira i ažurira zapis.
        /// </summary>
        private async void OnEditComponentClicked(object? sender, EventArgs e)
        {
            if (ComponentListView?.SelectedItem is not MachineComponent selected)
            {
                await SafeNavigator.ShowAlertAsync("Obavijest", "Molimo odaberite komponentu iz liste za uređivanje.", "OK");
                return;
            }

            try
            {
                var c = new MachineComponent
                {
                    Id          = selected.Id,
                    MachineId   = selected.MachineId,
                    Code        = selected.Code,
                    Name        = selected.Name,
                    Type        = selected.Type,
                    SopDoc      = selected.SopDoc,
                    Status      = selected.Status,
                    InstallDate = selected.InstallDate
                };

                bool ok = await ShowComponentFormAsync(c, "Uredi komponentu");
                if (!ok) return;

                c.Status = MachineComponent.NormalizeStatus(c.Status);

                const string sql = @"
UPDATE machine_components SET
    machine_id=@machine_id, code=@code, name=@name, type=@type,
    sop_doc=@sop_doc, status=@status, install_date=@install_date
WHERE id=@id;";

                var pars = new[]
                {
                    new MySqlParameter("@machine_id",  (object?)c.MachineId ?? DBNull.Value),
                    new MySqlParameter("@code",        c.Code ?? string.Empty),
                    new MySqlParameter("@name",        c.Name ?? string.Empty),
                    new MySqlParameter("@type",        c.Type ?? string.Empty),
                    new MySqlParameter("@sop_doc",     c.SopDoc ?? string.Empty),
                    new MySqlParameter("@status",      c.Status ?? "active"),
                    new MySqlParameter("@install_date", c.InstallDate ?? (object)DBNull.Value) { MySqlDbType = MySqlDbType.Date },
                    new MySqlParameter("@id",          c.Id)
                };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadComponentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Komponente", $"Ažuriranje nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Brisanje odabrane komponente – preferira soft-delete ako je podržano.
        /// </summary>
        private async void OnDeleteComponentClicked(object? sender, EventArgs e)
        {
            if (ComponentListView?.SelectedItem is not MachineComponent selected)
            {
                await SafeNavigator.ShowAlertAsync("Obavijest", "Molimo odaberite komponentu iz liste za brisanje.", "OK");
                return;
            }

            bool confirm = await SafeNavigator.ConfirmAsync("Potvrda brisanja", $"Želite li izbrisati komponentu: {selected.Name}?", "Da", "Ne");
            if (!confirm) return;

            try
            {
                const string sql = @"UPDATE machine_components SET is_deleted=1, deleted_at=NOW() WHERE id=@id;";
                var pars = new[] { new MySqlParameter("@id", selected.Id) };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadComponentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Komponente", $"Brisanje nije uspjelo: {ex.Message}", "OK");
            }
        }

        private async void OnOpenDocsClicked(object? sender, EventArgs e)
        {
            try
            {
                if (sender is not Button btn) return;
                if (btn.BindingContext is not MachineComponent mc) return;
                var dlg = new YasGMP.Views.Dialogs.ComponentDocumentsDialog(_dbService, mc.Id);
                await Navigation.PushModalAsync(dlg);
                _ = await dlg.Result;
                await LoadComponentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Dokumenti", ex.Message, "OK");
            }
        }

        private async void OnExportComponentsClicked(object? sender, EventArgs e)
        {
            try
            {
                string? path = await _viewModel.ExportComponentsAsync().ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(path))
                {
                    await SafeNavigator.ShowAlertAsync("Komponente", "Export je otkazan ili nije generirao datoteku.", "OK");
                    return;
                }

                if (!File.Exists(path))
                {
                    await SafeNavigator.ShowAlertAsync("Komponente", $"Exportirana datoteka nije pronađena: {path}", "OK");
                    return;
                }

                var request = new OpenFileRequest
                {
                    File = new ReadOnlyFile(path)
                };

                try
                {
                    await MainThread.InvokeOnMainThreadAsync(async () => await Launcher.OpenAsync(request));
                }
                catch (Exception launchEx)
                {
                    await SafeNavigator.ShowAlertAsync("Komponente", $"Datoteka je spremljena na: {path}\nOtvaranje nije uspjelo: {launchEx.Message}", "OK");
                    return;
                }

                await SafeNavigator.ShowAlertAsync("Komponente", $"Datoteka je spremljena na: {path}", "OK");
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Komponente", $"Export nije uspio: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// UI prompt forma za unos/uređivanje komponente (null-safe).
        /// </summary>
        private async Task<bool> ShowComponentFormAsync(MachineComponent c, string title)
        {
            await WaitForMachineLookupAsync();

            string? code = await DisplayPromptAsync(title, "Interna oznaka (Code):", initialValue: c.Code);
            if (code is null) return false;
            c.Code = code;

            string? name = await DisplayPromptAsync(title, "Naziv komponente:", initialValue: c.Name);
            if (name is null) return false;
            c.Name = name;

            string machinePrompt = BuildMachinePrompt(c.MachineId);
            string? mid = await DisplayPromptAsync(title, machinePrompt, initialValue: c.MachineId?.ToString());
            if (!string.IsNullOrWhiteSpace(mid) && int.TryParse(mid, out var m)) c.MachineId = m; else c.MachineId = null;

            c.Type   = (await DisplayPromptAsync(title, "Tip (npr. pumpa, ventil...):", initialValue: c.Type)) ?? c.Type ?? string.Empty;
            c.SopDoc = (await DisplayPromptAsync(title, "SOP/URS dokument (putanja):",   initialValue: c.SopDoc)) ?? c.SopDoc ?? string.Empty;

            string? status = await DisplayPromptAsync(title, "Status (active/maintenance/removed):", initialValue: c.Status);
            c.Status = status ?? c.Status ?? "active";

            string? dateStr = await DisplayPromptAsync(title, "Datum instalacije (YYYY-MM-DD):", initialValue: c.InstallDate?.ToString("yyyy-MM-dd"));
            if (!string.IsNullOrWhiteSpace(dateStr) && DateTime.TryParse(dateStr, out var dt)) c.InstallDate = dt; else c.InstallDate = null;

            return true;
        }

        private async Task LoadMachinesAsync()
        {
            const string sql = @"
SELECT id, name, code
FROM machines
WHERE is_deleted = 0 OR is_deleted IS NULL
ORDER BY name;";

            try
            {
                DataTable dt = await _dbService.ExecuteSelectAsync(sql).ConfigureAwait(false);

                var lookup = new Dictionary<int, string>(dt.Rows.Count);
                foreach (DataRow row in dt.Rows)
                {
                    int id = row["id"] == DBNull.Value ? 0 : Convert.ToInt32(row["id"]);
                    if (id == 0) continue;

                    string name = row.Table.Columns.Contains("name") && row["name"] is not DBNull
                        ? row["name"]?.ToString() ?? string.Empty
                        : string.Empty;
                    string code = row.Table.Columns.Contains("code") && row["code"] is not DBNull
                        ? row["code"]?.ToString() ?? string.Empty
                        : string.Empty;

                    string display = string.IsNullOrWhiteSpace(code)
                        ? name
                        : string.IsNullOrWhiteSpace(name)
                            ? code
                            : $"{code} – {name}";

                    lookup[id] = display;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _machineLookup.Clear();
                    foreach (var kvp in lookup)
                        _machineLookup[kvp.Key] = kvp.Value;
                });
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Strojevi", $"Učitavanje popisa strojeva nije uspjelo: {ex.Message}", "OK");
            }
        }

        private Task WaitForMachineLookupAsync()
        {
            _machinesPreloadTask ??= LoadMachinesAsync();
            return _machinesPreloadTask;
        }

        private string BuildMachinePrompt(int? currentMachineId)
        {
            const int previewLimit = 5;
            var prompt = new StringBuilder("ID stroja (machine_id):");

            if (_machineLookup.Count == 0)
                return prompt.ToString();

            if (currentMachineId.HasValue && _machineLookup.TryGetValue(currentMachineId.Value, out var currentName))
                prompt.Append($"\nTrenutno: {currentName} ({currentMachineId.Value})");

            prompt.Append("\nDostupni strojevi (ID – naziv):");

            int shown = 0;
            foreach (var kvp in _machineLookup)
            {
                if (shown >= previewLimit)
                    break;

                prompt.Append($"\n • {kvp.Key} – {kvp.Value}");
                shown++;
            }

            if (_machineLookup.Count > previewLimit)
                prompt.Append("\n • …");

            return prompt.ToString();
        }
    }
}
