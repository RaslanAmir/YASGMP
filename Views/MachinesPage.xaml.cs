// ==============================================================================
//  File: Views/MachinesPage.xaml.cs
//  Project: YasGMP
//  Summary:
//      Ekran za pregled/unos/uređivanje/brisanje strojeva/opreme (MySQL).
//      • UI-thread safe (MainThread + SafeNavigator) – izbjegnute WinUI 0x8001010E greške
//      • Robusno čitanje konekcijskog stringa iz App.AppConfig
//      • Sigurne konverzije NULL vrijednosti i parametri putem MySqlConnector
//      • Potpuna XML dokumentacija za IntelliSense
// ==============================================================================

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
    /// <b>MachinesPage</b> – ekran za pregled, unos, uređivanje i brisanje strojeva/opreme.
    /// Prikazuje listu, omogućuje unos novih zapisa, izmjenu i brisanje postojećih zapisa.
    /// Povezan s MySQL bazom putem <see cref="DatabaseService"/>. Svi UI pozivi su
    /// prisilno na glavnoj niti radi stabilnosti (WinUI COM iznimke).
    /// </summary>
    public partial class MachinesPage : ContentPage
    {
        /// <summary>Observable kolekcija strojeva za prikaz/binding.</summary>
        public ObservableCollection<Machine> Machines { get; } = new();

        /// <summary>Servis baze podataka (MySqlConnector).</summary>
        private readonly DatabaseService _dbService;

        /// <summary>
        /// Konstruktor: validira App/Config, učitava konekcijski string i inicira učitavanje podataka.
        /// </summary>
        /// <exception cref="InvalidOperationException">Ako <see cref="Application.Current"/> nije <see cref="App"/> ili nema connection string.</exception>
        public MachinesPage()
        {
            InitializeComponent();

            if (Application.Current is not App app)
                throw new InvalidOperationException("Application.Current nije tipa App.");

            // Robusno dohvaćanje konekcijskog stringa (podržan i ravni i sekcijski ključ)
            var connStr = app.AppConfig?["ConnectionStrings:MySqlDb"] ?? app.AppConfig?["MySqlDb"];
            if (string.IsNullOrWhiteSpace(connStr))
                throw new InvalidOperationException("MySqlDb connection string nije pronađen u konfiguraciji.");

            _dbService = new DatabaseService(connStr);

            BindingContext = this;

            // Ne blokirati UI – metode same maršaliraju UI ažuriranja
            _ = LoadMachinesAsync();
        }

        /// <summary>
        /// Učitava sve strojeve iz baze i ažurira kolekciju <see cref="Machines"/>.
        /// Ažuriranje UI kolekcije se odvija na glavnoj niti.
        /// </summary>
        private async Task LoadMachinesAsync()
        {
            try
            {
                const string sql = @"SELECT id, code, name, manufacturer, location, install_date, urs_doc, status, qr_code 
                                     FROM machines";

                var dt = await _dbService.ExecuteSelectAsync(sql).ConfigureAwait(false);

                // Pripremi listu na pozadinskoj niti
                var list = new List<Machine>(capacity: dt.Rows.Count);
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    static string S(object v) => v == DBNull.Value ? string.Empty : v?.ToString() ?? string.Empty;

                    list.Add(new Machine
                    {
                        Id           = row["id"] == DBNull.Value ? 0 : Convert.ToInt32(row["id"]),
                        Code         = S(row["code"]),
                        Name         = S(row["name"]),
                        Manufacturer = S(row["manufacturer"]),
                        Location     = S(row["location"]),
                        InstallDate  = row["install_date"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(row["install_date"]),
                        UrsDoc       = S(row["urs_doc"]),
                        Status       = S(row["status"]),
                        QrCode       = S(row["qr_code"])
                    });
                }

                // Ažuriraj UI na glavnoj niti
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Machines.Clear();
                    foreach (var m in list)
                        Machines.Add(m);
                });
            }
            catch (Exception ex)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", $"Učitavanje strojeva nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Dodavanje novog stroja – vodi korisnika kroz promptove i sprema u bazu.
        /// </summary>
        private async void OnAddMachineClicked(object? sender, EventArgs e)
        {
            try
            {
                var newMachine = new Machine();

                var ok = await ShowMachineFormAsync(newMachine, "Unesi novi stroj");
                if (!ok) return;

                const string sql = @"
INSERT INTO machines (code, name, manufacturer, location, install_date, urs_doc, status, qr_code)
VALUES (@code, @name, @manufacturer, @location, @install_date, @urs_doc, @status, @qr_code)";

                var pars = new[]
                {
                    new MySqlParameter("@code",         (object?)newMachine.Code         ?? string.Empty),
                    new MySqlParameter("@name",         (object?)newMachine.Name         ?? string.Empty),
                    new MySqlParameter("@manufacturer", (object?)newMachine.Manufacturer ?? string.Empty),
                    new MySqlParameter("@location",     (object?)newMachine.Location     ?? string.Empty),
                    new MySqlParameter("@install_date", (object?)newMachine.InstallDate  ?? DBNull.Value),
                    new MySqlParameter("@urs_doc",      (object?)newMachine.UrsDoc       ?? string.Empty),
                    new MySqlParameter("@status",       (object?)newMachine.Status       ?? string.Empty),
                    new MySqlParameter("@qr_code",      (object?)newMachine.QrCode       ?? string.Empty),
                };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadMachinesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", $"Spremanje novog stroja nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Uređivanje odabranog stroja – otvara formu, potom ažurira zapis u bazi.
        /// </summary>
        private async void OnEditMachineClicked(object? sender, EventArgs e)
        {
            try
            {
                if (MachineListView?.SelectedItem is not Machine selected)
                {
                    await Services.SafeNavigator.ShowAlertAsync("Obavijest", "Molimo odaberite stroj iz liste za uređivanje.", "OK");
                    return;
                }

                // Kopija za uređivanje
                var m = new Machine
                {
                    Id           = selected.Id,
                    Code         = selected.Code,
                    Name         = selected.Name,
                    Manufacturer = selected.Manufacturer,
                    Location     = selected.Location,
                    InstallDate  = selected.InstallDate,
                    UrsDoc       = selected.UrsDoc,
                    Status       = selected.Status,
                    QrCode       = selected.QrCode
                };

                var ok = await ShowMachineFormAsync(m, "Uredi stroj");
                if (!ok) return;

                const string sql = @"
UPDATE machines SET 
    code=@code, name=@name, manufacturer=@manufacturer, 
    location=@location, install_date=@install_date, 
    urs_doc=@urs_doc, status=@status, qr_code=@qr_code
WHERE id=@id";

                var pars = new[]
                {
                    new MySqlParameter("@code",         (object?)m.Code         ?? string.Empty),
                    new MySqlParameter("@name",         (object?)m.Name         ?? string.Empty),
                    new MySqlParameter("@manufacturer", (object?)m.Manufacturer ?? string.Empty),
                    new MySqlParameter("@location",     (object?)m.Location     ?? string.Empty),
                    new MySqlParameter("@install_date", (object?)m.InstallDate  ?? DBNull.Value),
                    new MySqlParameter("@urs_doc",      (object?)m.UrsDoc       ?? string.Empty),
                    new MySqlParameter("@status",       (object?)m.Status       ?? string.Empty),
                    new MySqlParameter("@qr_code",      (object?)m.QrCode       ?? string.Empty),
                    new MySqlParameter("@id",           m.Id),
                };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadMachinesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", $"Ažuriranje stroja nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Brisanje odabranog stroja nakon potvrde korisnika.
        /// </summary>
        private async void OnDeleteMachineClicked(object? sender, EventArgs e)
        {
            try
            {
                if (MachineListView?.SelectedItem is not Machine selected)
                {
                    await Services.SafeNavigator.ShowAlertAsync("Obavijest", "Molimo odaberite stroj iz liste za brisanje.", "OK");
                    return;
                }

                var confirm = await Services.SafeNavigator.ConfirmAsync("Potvrda brisanja", $"Želite li izbrisati stroj: {selected.Name}?", "Da", "Ne");
                if (!confirm) return;

                const string sql = "DELETE FROM machines WHERE id=@id";
                var pars = new[] { new MySqlParameter("@id", selected.Id) };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadMachinesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", $"Brisanje stroja nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Popup forma za unos/uređivanje stroja. Svi UI promptovi izvršavaju se na glavnoj niti.
        /// </summary>
        /// <param name="machine">Model koji se puni.</param>
        /// <param name="title">Naslov forme.</param>
        /// <returns><c>true</c> ako su podaci valjani i potvrđeni; inače <c>false</c>.</returns>
        private async Task<bool> ShowMachineFormAsync(Machine machine, string title)
        {
            // Pomocna lokalna funkcija za UI-safe prompt
            Task<string?> PromptAsync(string caption, string msg, string? initial = null) =>
                MainThread.InvokeOnMainThreadAsync(() => DisplayPromptAsync(caption, msg, initialValue: initial));

            var code = await PromptAsync(title, "Interna oznaka (Code):", machine.Code);
            if (code is null) return false;
            machine.Code = code;

            var name = await PromptAsync(title, "Naziv stroja:", machine.Name);
            if (name is null) return false;
            machine.Name = name;

            var manufacturer = await PromptAsync(title, "Proizvođač:", machine.Manufacturer);
            if (manufacturer is null) return false;
            machine.Manufacturer = manufacturer;

            var location = await PromptAsync(title, "Lokacija:", machine.Location);
            if (location is null) return false;
            machine.Location = location;

            var dateStr = await PromptAsync(title, "Datum instalacije (YYYY-MM-DD):", machine.InstallDate?.ToString("yyyy-MM-dd"));
            if (!string.IsNullOrWhiteSpace(dateStr) && DateTime.TryParse(dateStr, out var dt))
                machine.InstallDate = dt;
            else
                machine.InstallDate = null;

            var ursDoc = await PromptAsync(title, "Putanja do URS PDF-a:", machine.UrsDoc);
            machine.UrsDoc = ursDoc ?? machine.UrsDoc;

            var status = await PromptAsync(title, "Status (npr. 'u pogonu', 'van pogona'):", machine.Status);
            machine.Status = status ?? machine.Status;

            var qr = await PromptAsync(title, "QR kod (tekstualni kod ili URL):", machine.QrCode);
            machine.QrCode = qr ?? machine.QrCode;

            return true;
        }
    }
}
