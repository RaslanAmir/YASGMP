using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel; // MainThread
using Microsoft.Maui.Controls;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP
{
    /// <summary>
    /// <b>ComponentsPage</b> – Ekran za pregled, unos, uređivanje i brisanje komponenti strojeva.
    /// • UI-thread sigurno ažuriranje kolekcija (koristi <see cref="MainThread"/>).<br/>
    /// • Svi alerti/confirmi idu kroz <see cref="SafeNavigator"/> kako bi se izbjegao WinUI <c>0x8001010E</c>.<br/>
    /// • Čvrsti SQL parametri i koalesciranje stringova kako bismo izbjegli CS8601 upozorenja.
    /// </summary>
    public partial class ComponentsPage : ContentPage
    {
        /// <summary>
        /// Observable kolekcija komponenti za UI binding (ListView/CollectionView).
        /// </summary>
        public ObservableCollection<Component> Components { get; } = new();

        /// <summary>
        /// Servis za pristup bazi podataka.
        /// </summary>
        private readonly DatabaseService _dbService;

        /// <summary>
        /// Konstruktor. Učitava connection string iz <see cref="App.AppConfig"/> i
        /// pokreće inicijalno učitavanje komponenti.
        /// </summary>
        /// <exception cref="InvalidOperationException">Ako connection string nije pronađen.</exception>
        public ComponentsPage()
        {
            InitializeComponent();

            string? connStr = null;
            if (Application.Current is App app)
            {
                // Podrži i ravni i sekcijski ključ
                connStr = app.AppConfig?["ConnectionStrings:MySqlDb"] ?? app.AppConfig?.GetSection("ConnectionStrings")?["MySqlDb"];
            }

            if (string.IsNullOrWhiteSpace(connStr))
                throw new InvalidOperationException("MySqlDb connection string nije pronađen u konfiguraciji AppConfig!");

            _dbService = new DatabaseService(connStr);
            BindingContext = this;

            // Fire-and-forget; metoda sama maršalira UI ažuriranja na UI thread
            _ = LoadComponentsAsync();
        }

        /// <summary>
        /// Učitava komponente iz baze i puni kolekciju. Sva potencijalno null polja
        /// mapiraju se na <see cref="string.Empty"/>. UI ažuriranja se izvršavaju na UI threadu.
        /// </summary>
        private async Task LoadComponentsAsync()
        {
            try
            {
                const string sql = @"
SELECT c.id, c.machine_id, c.code, c.name, c.type, c.sop_doc, m.name AS machine_name
FROM machine_components c
LEFT JOIN machines m ON m.id = c.machine_id
ORDER BY c.id ASC;";

                var dt = await _dbService.ExecuteSelectAsync(sql).ConfigureAwait(false);

                // Pripremi međuspremnik izvan UI threada
                var buffer = new List<Component>(capacity: dt.Rows.Count);

                foreach (System.Data.DataRow row in dt.Rows)
                {
                    string GetStr(string col) =>
                        row.Table.Columns.Contains(col) && row[col] is not DBNull
                            ? (row[col]?.ToString() ?? string.Empty)
                            : string.Empty;

                    int GetInt(string col) =>
                        row.Table.Columns.Contains(col) && row[col] is not DBNull
                            ? Convert.ToInt32(row[col])
                            : 0;

                    buffer.Add(new Component
                    {
                        Id          = GetInt("id"),
                        MachineId   = GetInt("machine_id"),
                        Code        = GetStr("code"),
                        Name        = GetStr("name"),
                        Type        = GetStr("type"),
                        SopDoc      = GetStr("sop_doc"),
                        MachineName = GetStr("machine_name")
                    });
                }

                // Prebaci u UI kolekciju na UI threadu
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Components.Clear();
                    foreach (var c in buffer)
                        Components.Add(c);
                });
            }
            catch (Exception ex)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", $"Neuspješno učitavanje komponenti: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Dodavanje nove komponente. Otvara prompt dijalog(e), validira ulaze i sprema u bazu.
        /// </summary>
        private async void OnAddComponentClicked(object? sender, EventArgs e)
        {
            try
            {
                var newComponent = new Component();

                bool ok = await ShowComponentFormAsync(newComponent, "Unesi novu komponentu");
                if (!ok) return;

                const string sql = @"
INSERT INTO machine_components (machine_id, code, name, type, sop_doc)
VALUES (@machine_id, @code, @name, @type, @sop_doc);";

                var pars = new[]
                {
                    new MySqlParameter("@machine_id", newComponent.MachineId),
                    new MySqlParameter("@code",       newComponent.Code ?? string.Empty),
                    new MySqlParameter("@name",       newComponent.Name ?? string.Empty),
                    new MySqlParameter("@type",       newComponent.Type ?? string.Empty),
                    new MySqlParameter("@sop_doc",    newComponent.SopDoc ?? string.Empty)
                };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadComponentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", $"Spremanje nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Uređivanje postojeće komponente. Validira odabir, otvara prompt dijalog(e) i zapisuje izmjene.
        /// </summary>
        private async void OnEditComponentClicked(object? sender, EventArgs e)
        {
            try
            {
                var listView = FindByName("ComponentListView") as ListView; // (XAML) bez generika
                var selected = listView?.SelectedItem as Component;
                if (selected is null)
                {
                    await Services.SafeNavigator.ShowAlertAsync("Obavijest", "Molimo odaberite komponentu iz liste za uređivanje.", "OK");
                    return;
                }

                // Radimo kopiju za uređivanje kako ne bismo mutirali selektirani objekt prije spremanja
                var componentToEdit = new Component
                {
                    Id          = selected.Id,
                    MachineId   = selected.MachineId,
                    Code        = selected.Code        ?? string.Empty,
                    Name        = selected.Name        ?? string.Empty,
                    Type        = selected.Type        ?? string.Empty,
                    SopDoc      = selected.SopDoc      ?? string.Empty,
                    MachineName = selected.MachineName ?? string.Empty
                };

                bool ok = await ShowComponentFormAsync(componentToEdit, "Uredi komponentu");
                if (!ok) return;

                const string sql = @"
UPDATE machine_components SET 
    machine_id=@machine_id,
    code=@code,
    name=@name, 
    type=@type,
    sop_doc=@sop_doc
WHERE id=@id;";

                var pars = new[]
                {
                    new MySqlParameter("@machine_id", componentToEdit.MachineId),
                    new MySqlParameter("@code",       componentToEdit.Code ?? string.Empty),
                    new MySqlParameter("@name",       componentToEdit.Name ?? string.Empty),
                    new MySqlParameter("@type",       componentToEdit.Type ?? string.Empty),
                    new MySqlParameter("@sop_doc",    componentToEdit.SopDoc ?? string.Empty),
                    new MySqlParameter("@id",         componentToEdit.Id)
                };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadComponentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", $"Uređivanje nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Brisanje komponente. Traži potvrdu korisnika prije izvršenja.
        /// </summary>
        private async void OnDeleteComponentClicked(object? sender, EventArgs e)
        {
            try
            {
                var listView = FindByName("ComponentListView") as ListView;
                var selected = listView?.SelectedItem as Component;
                if (selected is null)
                {
                    await Services.SafeNavigator.ShowAlertAsync("Obavijest", "Molimo odaberite komponentu iz liste za brisanje.", "OK");
                    return;
                }

                bool confirm = await Services.SafeNavigator.ConfirmAsync(
                    "Potvrda brisanja",
                    $"Želite li izbrisati komponentu: {selected.Name}?",
                    "Da", "Ne");
                if (!confirm) return;

                const string sql = "DELETE FROM machine_components WHERE id=@id;";
                var pars = new[] { new MySqlParameter("@id", selected.Id) };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadComponentsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", $"Brisanje nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Dijalog za unos/uređivanje komponente – svi stringovi koaliraju na prazan string
        /// kako bi se izbjegle null dodjele u nenullabilna polja.
        /// </summary>
        private async Task<bool> ShowComponentFormAsync(Component component, string title)
        {
            // Svi promptovi idu na UI thread zbog WinUI/COM ograničenja
            var machineIdStr = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayPromptAsync(title, "ID stroja (machine_id):",
                    initialValue: component.MachineId > 0 ? component.MachineId.ToString() : string.Empty));

            if (string.IsNullOrWhiteSpace(machineIdStr) || !int.TryParse(machineIdStr, out var mid))
                return false;
            component.MachineId = mid;

            var code = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayPromptAsync(title, "Interna oznaka (Code):", initialValue: component.Code ?? string.Empty));
            if (code is null) return false;
            component.Code = code;

            var name = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayPromptAsync(title, "Naziv komponente:", initialValue: component.Name ?? string.Empty));
            if (name is null) return false;
            component.Name = name;

            var type = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayPromptAsync(title, "Tip (npr. Kalibracijski, PPM...):", initialValue: component.Type ?? string.Empty));
            if (type is null) return false;
            component.Type = type;

            var sop = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayPromptAsync(title, "Putanja do SOP dokumenta:", initialValue: component.SopDoc ?? string.Empty));
            component.SopDoc = sop ?? string.Empty;

            return true;
        }
    }
}
