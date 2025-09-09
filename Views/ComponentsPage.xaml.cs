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
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Views
{
    /// <summary>
    /// <b>ComponentsPage</b> – pregled, unos, uređivanje i brisanje komponenti strojeva (tablica <c>machine_components</c>).
    /// Kolone: <c>id, machine_id, code, name, type, sop_doc, status, install_date</c> (+ soft delete polja).
    /// </summary>
    public partial class ComponentsPage : ContentPage
    {
        /// <summary>Kolekcija komponenti za UI binding.</summary>
        public ObservableCollection<MachineComponent> Components { get; } = new();

        private readonly DatabaseService _dbService;

        /// <summary>
        /// Inicijalizira stranicu, rješava konekcijski string i učitava podatke.
        /// </summary>
        /// <exception cref="InvalidOperationException">Ako aplikacija nema connection string.</exception>
        public ComponentsPage()
        {
            InitializeComponent();

            var connStr = ResolveMySqlConnectionStringFromApp();
            _dbService = new DatabaseService(connStr);

            BindingContext = this;
            _ = LoadComponentsAsync();
        }

        private static string ResolveMySqlConnectionStringFromApp()
        {
            if (Application.Current is not App app)
                throw new InvalidOperationException("Application.Current nije tipa App.");

            var viaSection = app.AppConfig?["ConnectionStrings:MySqlDb"];
            var viaFlat    = app.AppConfig?["MySqlDb"];
            var conn       = !string.IsNullOrWhiteSpace(viaSection) ? viaSection
                           : !string.IsNullOrWhiteSpace(viaFlat)    ? viaFlat
                           : null;

            if (string.IsNullOrWhiteSpace(conn))
                throw new InvalidOperationException("MySqlDb connection string nije pronađen u konfiguraciji.");

            return conn!;
        }

        /// <summary>
        /// Učitava sve komponente (neobrisane) i puni kolekciju na glavnoj niti.
        /// </summary>
        private async Task LoadComponentsAsync()
        {
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
                    list.Add(new MachineComponent
                    {
                        Id          = row["id"]           == DBNull.Value ? 0 : Convert.ToInt32(row["id"]),
                        MachineId   = row["machine_id"]   == DBNull.Value ? (int?)null : Convert.ToInt32(row["machine_id"]),
                        Code        = row["code"]         == DBNull.Value ? string.Empty : row["code"]?.ToString() ?? string.Empty,
                        Name        = row["name"]         == DBNull.Value ? string.Empty : row["name"]?.ToString() ?? string.Empty,
                        Type        = row["type"]         == DBNull.Value ? string.Empty : row["type"]?.ToString() ?? string.Empty,
                        SopDoc      = row["sop_doc"]      == DBNull.Value ? string.Empty : row["sop_doc"]?.ToString() ?? string.Empty,
                        Status      = row["status"]       == DBNull.Value ? "active" : row["status"]?.ToString() ?? "active",
                        InstallDate = row["install_date"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(row["install_date"])
                    });
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Components.Clear();
                    foreach (var c in list)
                        Components.Add(c);
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

        /// <summary>
        /// UI prompt forma za unos/uređivanje komponente (null-safe).
        /// </summary>
        private async Task<bool> ShowComponentFormAsync(MachineComponent c, string title)
        {
            string? code = await DisplayPromptAsync(title, "Interna oznaka (Code):", initialValue: c.Code);
            if (code is null) return false;
            c.Code = code;

            string? name = await DisplayPromptAsync(title, "Naziv komponente:", initialValue: c.Name);
            if (name is null) return false;
            c.Name = name;

            string? mid = await DisplayPromptAsync(title, "ID stroja (machine_id):", initialValue: c.MachineId?.ToString());
            if (!string.IsNullOrWhiteSpace(mid) && int.TryParse(mid, out var m)) c.MachineId = m; else c.MachineId = null;

            c.Type   = (await DisplayPromptAsync(title, "Tip (npr. pumpa, ventil...):", initialValue: c.Type)) ?? c.Type ?? string.Empty;
            c.SopDoc = (await DisplayPromptAsync(title, "SOP/URS dokument (putanja):",   initialValue: c.SopDoc)) ?? c.SopDoc ?? string.Empty;

            string? status = await DisplayPromptAsync(title, "Status (active/maintenance/removed):", initialValue: c.Status);
            c.Status = status ?? c.Status ?? "active";

            string? dateStr = await DisplayPromptAsync(title, "Datum instalacije (YYYY-MM-DD):", initialValue: c.InstallDate?.ToString("yyyy-MM-dd"));
            if (!string.IsNullOrWhiteSpace(dateStr) && DateTime.TryParse(dateStr, out var dt)) c.InstallDate = dt; else c.InstallDate = null;

            return true;
        }
    }
}
