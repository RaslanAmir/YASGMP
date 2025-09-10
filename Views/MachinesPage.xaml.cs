// ==============================================================================
//  File: Views/MachinesPage.xaml.cs
//  Project: YasGMP
//  Summary:
//      Ekran za pregled/unos/uređivanje/brisanje strojeva/opreme (MySQL).
//      • UI-thread safe (MainThread) – izbjegnute WinUI 0x8001010E greške
//      • Robusno čitanje konekcijskog stringa iz App.AppConfig
//      • Sigurne konverzije NULL vrijednosti i tipizirani parametri (MySqlConnector)
//      • Potpuna XML dokumentacija za IntelliSense
// ==============================================================================
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Views.Dialogs;

namespace YasGMP.Views
{
    /// <summary>
    /// <b>MachinesPage</b> – ekran za pregled, unos, uređivanje i brisanje strojeva/opreme.
    /// </summary>
    public partial class MachinesPage : ContentPage
    {
        /// <summary>Observable kolekcija strojeva za prikaz/binding.</summary>
        public ObservableCollection<Machine> Machines { get; } = new();

        /// <summary>Servis baze podataka (MySqlConnector).</summary>
        private readonly DatabaseService _dbService;

        /// <summary>Generatori kôda i QR-a (lokalno za ovu stranicu).</summary>
        private readonly CodeGeneratorService _codeService = new();
        private readonly QRCodeService _qrService = new();

        /// <summary>
        /// Konstruktor – priprema servis i učitava podatke.
        /// </summary>
        /// <exception cref="InvalidOperationException">Ako aplikacija nema connection string.</exception>
        public MachinesPage()
        {
            InitializeComponent();

            if (Application.Current is not App app)
                throw new InvalidOperationException("Application.Current nije tipa App.");

            var connStr = app.AppConfig?["ConnectionStrings:MySqlDb"] ?? app.AppConfig?["MySqlDb"];
            if (string.IsNullOrWhiteSpace(connStr))
                throw new InvalidOperationException("MySqlDb connection string nije pronađen u konfiguraciji.");

            _dbService = new DatabaseService(connStr);
            BindingContext = this;

            _ = LoadMachinesAsync();
        }

        /// <summary>Učitava sve strojeve iz baze i puni kolekciju.</summary>
        private async Task LoadMachinesAsync()
        {
            try
            {
                const string sql = @"SELECT id, code, name, manufacturer, location, install_date, urs_doc, status, qr_code 
                                     FROM machines";
                DataTable dt = await _dbService.ExecuteSelectAsync(sql).ConfigureAwait(false);

                var list = new List<Machine>(capacity: dt.Rows.Count);
                foreach (DataRow row in dt.Rows)
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

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Machines.Clear();
                    foreach (var m in list)
                        Machines.Add(m);
                });
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Učitavanje strojeva nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>Dodavanje novog stroja – vodi korisnika kroz formu i sprema u bazu.</summary>
        private async void OnAddMachineClicked(object? sender, EventArgs e)
        {
            try
            {
                var newMachine = new Machine();

                var ok = await ShowMachineFormAsync(newMachine, "Unesi novi stroj");
                if (!ok) return;

                // Osiguraj Code i QR prije spremanja
                if (string.IsNullOrWhiteSpace(newMachine.Code))
                    newMachine.Code = _codeService.GenerateMachineCode();

                if (string.IsNullOrWhiteSpace(newMachine.QrCode))
                {
                    using var stream = _qrService.GeneratePng(newMachine.Code);
                    var dir = FileSystem.AppDataDirectory;
                    Directory.CreateDirectory(dir);
                    var path = Path.Combine(dir, $"{newMachine.Code}.png");
                    using var fs = File.Create(path);
                    stream.CopyTo(fs);
                    newMachine.QrCode = path;
                }

                newMachine.Status = NormalizeStatus(newMachine.Status);

                const string sql = @"
INSERT INTO machines (code, name, manufacturer, location, install_date, urs_doc, status, qr_code)
VALUES (@code, @name, @manufacturer, @location, @install_date, @urs_doc, @status, @qr_code)";

                var pars = new MySqlParameter[]
                {
                    new("@code",         newMachine.Code ?? string.Empty),
                    new("@name",         newMachine.Name ?? string.Empty),
                    new("@manufacturer", newMachine.Manufacturer ?? string.Empty),
                    new("@location",     newMachine.Location ?? string.Empty),
                    new("@install_date", newMachine.InstallDate ?? (object)DBNull.Value) { MySqlDbType = MySqlDbType.DateTime },
                    new("@urs_doc",      newMachine.UrsDoc ?? string.Empty),
                    new("@status",       newMachine.Status ?? "active"),
                    new("@qr_code",      newMachine.QrCode ?? string.Empty),
                };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadMachinesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Spremanje novog stroja nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>Uređivanje odabranog stroja.</summary>
        private async void OnEditMachineClicked(object? sender, EventArgs e)
        {
            try
            {
                if (MachineListView?.SelectedItem is not Machine selected)
                {
                    await SafeNavigator.ShowAlertAsync("Obavijest", "Molimo odaberite stroj iz liste za uređivanje.", "OK");
                    return;
                }

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

                // Osiguraj Code i QR prije spremanja
                if (string.IsNullOrWhiteSpace(m.Code))
                    m.Code = _codeService.GenerateMachineCode();

                if (string.IsNullOrWhiteSpace(m.QrCode))
                {
                    using var stream = _qrService.GeneratePng(m.Code);
                    var dir = FileSystem.AppDataDirectory;
                    Directory.CreateDirectory(dir);
                    var path = Path.Combine(dir, $"{m.Code}.png");
                    using var fs = File.Create(path);
                    stream.CopyTo(fs);
                    m.QrCode = path;
                }

                m.Status = NormalizeStatus(m.Status);

                const string sql = @"
UPDATE machines SET 
    code=@code, name=@name, manufacturer=@manufacturer, 
    location=@location, install_date=@install_date, 
    urs_doc=@urs_doc, status=@status, qr_code=@qr_code
WHERE id=@id";

                var pars = new MySqlParameter[]
                {
                    new("@code",         m.Code ?? string.Empty),
                    new("@name",         m.Name ?? string.Empty),
                    new("@manufacturer", m.Manufacturer ?? string.Empty),
                    new("@location",     m.Location ?? string.Empty),
                    new("@install_date", m.InstallDate ?? (object)DBNull.Value) { MySqlDbType = MySqlDbType.DateTime },
                    new("@urs_doc",      m.UrsDoc ?? string.Empty),
                    new("@status",       m.Status ?? "active"),
                    new("@qr_code",      m.QrCode ?? string.Empty),
                    new("@id",           m.Id),
                };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadMachinesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Ažuriranje stroja nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>Brisanje odabranog stroja nakon potvrde.</summary>
        private async void OnDeleteMachineClicked(object? sender, EventArgs e)
        {
            try
            {
                if (MachineListView?.SelectedItem is not Machine selected)
                {
                    await SafeNavigator.ShowAlertAsync("Obavijest", "Molimo odaberite stroj iz liste za brisanje.", "OK");
                    return;
                }

                var confirm = await SafeNavigator.ConfirmAsync("Potvrda brisanja", $"Želite li izbrisati stroj: {selected.Name}?", "Da", "Ne");
                if (!confirm) return;

                const string sql = "DELETE FROM machines WHERE id=@id";
                var pars = new[] { new MySqlParameter("@id", selected.Id) };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadMachinesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Brisanje stroja nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Popup forma za unos/uređivanje stroja (UI-thread safe).
        /// </summary>
        private async Task<bool> ShowMachineFormAsync(Machine machine, string title)
        {
            var dialog = new MachineEditDialog(machine) { Title = title };
            await Navigation.PushModalAsync(dialog);
            return await dialog.Result;
        }

        /// <summary>Kanonska normalizacija statusa (isti mapping kao u servisnom sloju).</summary>
        private static string NormalizeStatus(string? raw)
        {
            string s = (raw ?? string.Empty).Trim().ToLowerInvariant();
            return s switch
            {
                "active" => "active",
                "maintenance" or "maint" or "service" => "maintenance",
                "decommissioned" or "decom" or "retired" => "decommissioned",
                "reserved" => "reserved",
                "scrapped" or "scrap" => "scrapped",
                "u pogonu" or "operativan" or "operational" => "active",
                "van pogona" or "neispravan" or "kvar" or "servis" => "maintenance",
                "otpisan" or "rashodovan" => "scrapped",
                "rezerviran" => "reserved",
                "dekomisioniran" => "decommissioned",
                _ => "active"
            };
        }

        /// <summary>
        /// Otvaranje/vezanje dokumenata za odabrani stroj (placeholder).
        /// </summary>
        private async void OnDocumentsClicked(object? sender, EventArgs e)
        {
            if (MachineListView?.SelectedItem is not Machine selected)
            {
                await SafeNavigator.ShowAlertAsync("Obavijest", "Odaberite stroj kako biste vidjeli/pridružili dokumente.", "OK");
                return;
            }
            await SafeNavigator.ShowAlertAsync("Info",
                $"Priloženi dokumenti za: {selected.Name}\n(Funkcionalnost će biti dodana u sljedećoj fazi.)",
                "OK");
        }

        /// <summary>
        /// Pregled ili generiranje QR koda za odabrani stroj (placeholder).
        /// </summary>
        private async void OnQrClicked(object? sender, EventArgs e)
        {
            if (MachineListView?.SelectedItem is not Machine selected)
            {
                await SafeNavigator.ShowAlertAsync("Obavijest", "Odaberite stroj kako biste otvorili QR.", "OK");
                return;
            }
            await SafeNavigator.ShowAlertAsync("Info",
                $"QR za: {selected.Name}\nVrijednost: {(string.IsNullOrWhiteSpace(selected.QrCode) ? "(nije postavljeno)" : selected.QrCode)}",
                "OK");
        }

        /// <summary>
        /// Pregled povezanih komponenti za odabrani stroj (placeholder).
        /// </summary>
        private async void OnComponentsClicked(object? sender, EventArgs e)
        {
            if (MachineListView?.SelectedItem is not Machine selected)
            {
                await SafeNavigator.ShowAlertAsync("Obavijest", "Odaberite stroj kako biste vidjeli komponente.", "OK");
                return;
            }
            await SafeNavigator.ShowAlertAsync("Info",
                $"Komponente za: {selected.Name}\n(Pregled komponenti će biti dodan.)",
                "OK");
        }
    }
}
