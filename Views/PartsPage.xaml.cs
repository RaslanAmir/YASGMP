// ==============================================================================
//  File: Views/PartsPage.xaml.cs
//  Project: YasGMP
//  Summary:
//      Pregled/unos/uređivanje/brisanje rezervnih dijelova (Parts) uz MySQL backend.
//      • UI-thread safe (MainThread + SafeNavigator) – izbjegnute WinUI 0x8001010E greške
//      • Robusno dohvaćanje konekcijskog stringa iz App.AppConfig
//      • Sigurne konverzije NULL vrijednosti i parametri preko MySqlConnector
//      • Potpuna XML dokumentacija za IntelliSense
// ==============================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;             // MainThread
using Microsoft.Maui.Controls;
using MySqlConnector;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Views
{
    /// <summary>
    /// <b>PartsPage</b> – pregled, unos, uređivanje i brisanje rezervnih dijelova (Parts).
    /// Povezano s MySQL bazom preko <see cref="DatabaseService"/>. Svi UI dijalozi i ažuriranja
    /// izvršavaju se na glavnoj niti putem <see cref="MainThread"/> kako bi se izbjegle WinUI
    /// <c>COMException 0x8001010E</c> situacije.
    /// </summary>
    public partial class PartsPage : ContentPage
    {
        /// <summary>Observable kolekcija za binding.</summary>
        public ObservableCollection<Part> Parts { get; } = new();

        /// <summary>Servis za pristup bazi.</summary>
        private readonly DatabaseService _dbService;

        /// <summary>
        /// Inicijalizira stranicu i učitava dijelove. Sigurno dohvaća konekcijski string.
        /// </summary>
        /// <exception cref="InvalidOperationException">Ako aplikacija ili konekcijski string nisu dostupni.</exception>
        public PartsPage()
        {
            InitializeComponent();

            var connStr = ResolveMySqlConnectionStringFromApp();
            _dbService = new DatabaseService(connStr);

            BindingContext = this;

            // Ne blokirati UI – metoda sama maršalira UI ažuriranja
            _ = LoadPartsAsync();
        }

        /// <summary>
        /// Sigurno dohvaća MySQL connection string iz <see cref="App.AppConfig"/> bez ovisnosti o ekstenzijama.
        /// Pokušava i ravni ključ (<c>MySqlDb</c>) i sekcijski (<c>ConnectionStrings:MySqlDb</c>).
        /// </summary>
        private static string ResolveMySqlConnectionStringFromApp()
        {
            if (Application.Current is not App app)
                throw new InvalidOperationException("Application.Current nije tipa App.");

            var cfg = app.AppConfig;
            var viaSection = cfg?["ConnectionStrings:MySqlDb"];
            var viaFlat    = cfg?["MySqlDb"];

            var conn = !string.IsNullOrWhiteSpace(viaSection) ? viaSection
                     : !string.IsNullOrWhiteSpace(viaFlat)    ? viaFlat
                     : null;

            if (string.IsNullOrWhiteSpace(conn))
                throw new InvalidOperationException("MySqlDb connection string nije pronađen u konfiguraciji.");

            return conn!;
        }

        /// <summary>
        /// Učitava sve dijelove iz baze i ažurira kolekciju <see cref="Parts"/> na glavnoj niti.
        /// </summary>
        private async Task LoadPartsAsync()
        {
            try
            {
                const string sql = @"SELECT id, code, name, supplier, price, stock, location, image FROM parts";
                var dt = await _dbService.ExecuteSelectAsync(sql).ConfigureAwait(false);

                // Pripremi listu na pozadinskoj niti
                var list = new List<Part>(capacity: dt.Rows.Count);
                foreach (System.Data.DataRow row in dt.Rows)
                {
                    list.Add(new Part
                    {
                        Id       = row["id"] == DBNull.Value ? 0 : Convert.ToInt32(row["id"]),
                        Code     = row["code"]?.ToString() ?? string.Empty,
                        Name     = row["name"]?.ToString() ?? string.Empty,
                        Supplier = row["supplier"] == DBNull.Value ? null : row["supplier"]?.ToString(),
                        Price    = row["price"]    == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["price"]),
                        Stock    = row["stock"]    == DBNull.Value ? 0 : Convert.ToInt32(row["stock"]),
                        Location = row["location"] == DBNull.Value ? null : row["location"]?.ToString(),
                        Image    = row["image"]    == DBNull.Value ? null : row["image"]?.ToString()
                    });
                }

                // Ažuriraj UI kolekciju na glavnoj niti
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Parts.Clear();
                    foreach (var p in list)
                        Parts.Add(p);
                });
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Učitavanje dijelova nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Otvara formu za dodavanje novog dijela, validira unos i sprema zapis u bazu.
        /// </summary>
        private async void OnAddPartClicked(object? sender, EventArgs e)
        {
            try
            {
                var newPart = new Part();
                bool result = await ShowPartFormAsync(newPart, "Unesi novi rezervni dio");
                if (!result) return;

                const string sql = @"INSERT INTO parts (code, name, supplier, price, stock, location, image)
                                     VALUES (@code, @name, @supplier, @price, @stock, @location, @image)";
                var pars = new[]
                {
                    new MySqlParameter("@code",     newPart.Code ?? string.Empty),
                    new MySqlParameter("@name",     newPart.Name ?? string.Empty),
                    new MySqlParameter("@supplier", (object?)newPart.Supplier ?? DBNull.Value),
                    new MySqlParameter("@price",    (object?)newPart.Price    ?? DBNull.Value),
                    new MySqlParameter("@stock",    newPart.Stock),
                    new MySqlParameter("@location", (object?)newPart.Location ?? DBNull.Value),
                    new MySqlParameter("@image",    (object?)newPart.Image    ?? DBNull.Value)
                };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadPartsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Spremanje dijela nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Otvara formu za uređivanje postojećeg dijela, validira i ažurira zapis u bazi.
        /// </summary>
        private async void OnEditPartClicked(object? sender, EventArgs e)
        {
            try
            {
                if (PartListView.SelectedItem is not Part selected)
                {
                    await SafeNavigator.ShowAlertAsync("Obavijest", "Molimo odaberite rezervni dio iz liste za uređivanje.", "OK");
                    return;
                }

                var partToEdit = new Part
                {
                    Id       = selected.Id,
                    Code     = selected.Code,
                    Name     = selected.Name,
                    Supplier = selected.Supplier,
                    Price    = selected.Price,
                    Stock    = selected.Stock,
                    Location = selected.Location,
                    Image    = selected.Image
                };

                bool result = await ShowPartFormAsync(partToEdit, "Uredi rezervni dio");
                if (!result) return;

                const string sql = @"UPDATE parts SET 
                                        code=@code, name=@name, supplier=@supplier, 
                                        price=@price, stock=@stock, location=@location, image=@image
                                     WHERE id=@id";
                var pars = new[]
                {
                    new MySqlParameter("@code",     partToEdit.Code ?? string.Empty),
                    new MySqlParameter("@name",     partToEdit.Name ?? string.Empty),
                    new MySqlParameter("@supplier", (object?)partToEdit.Supplier ?? DBNull.Value),
                    new MySqlParameter("@price",    (object?)partToEdit.Price    ?? DBNull.Value),
                    new MySqlParameter("@stock",    partToEdit.Stock),
                    new MySqlParameter("@location", (object?)partToEdit.Location ?? DBNull.Value),
                    new MySqlParameter("@image",    (object?)partToEdit.Image    ?? DBNull.Value),
                    new MySqlParameter("@id",       partToEdit.Id)
                };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadPartsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Ažuriranje dijela nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Briše odabrani dio iz baze nakon potvrde korisnika.
        /// </summary>
        private async void OnDeletePartClicked(object? sender, EventArgs e)
        {
            try
            {
                if (PartListView.SelectedItem is not Part selected)
                {
                    await SafeNavigator.ShowAlertAsync("Obavijest", "Molimo odaberite rezervni dio iz liste za brisanje.", "OK");
                    return;
                }

                bool confirm = await SafeNavigator.ConfirmAsync("Potvrda brisanja", $"Želite li izbrisati dio: {selected.Name}?", "Da", "Ne");
                if (!confirm) return;

                const string sql = "DELETE FROM parts WHERE id=@id";
                var pars = new[] { new MySqlParameter("@id", selected.Id) };

                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadPartsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Brisanje dijela nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Jednostavna forma preko DisplayPromptAsync za unos/uređivanje.
        /// Svi promptovi se izvršavaju na glavnoj niti kako bi se izbjegle COM/WinUI greške.
        /// </summary>
        /// <param name="part">Model koji se puni/uređuje.</param>
        /// <param name="title">Naslov forme.</param>
        /// <returns><c>true</c> ako je unos valjan i potvrđen; inače <c>false</c>.</returns>
        private async Task<bool> ShowPartFormAsync(Part part, string title)
        {
            // Lokalni pomoćnik: UI-safe prompt
            Task<string?> PromptAsync(string caption, string msg, string? initial = null) =>
                MainThread.InvokeOnMainThreadAsync(() => DisplayPromptAsync(caption, msg, initialValue: initial));

            var code = await PromptAsync(title, "Interna oznaka (Code):", part.Code);
            if (code is null) return false;
            part.Code = code;

            var name = await PromptAsync(title, "Naziv dijela:", part.Name);
            if (name is null) return false;
            part.Name = name;

            var supplier = await PromptAsync(title, "Dobavljač:", part.Supplier);
            if (supplier is null) return false;
            part.Supplier = supplier;

            var priceStr = await PromptAsync(title, "Cijena:", part.Price.HasValue ? part.Price.Value.ToString(CultureInfo.InvariantCulture) : string.Empty);
            if (!string.IsNullOrWhiteSpace(priceStr) && decimal.TryParse(priceStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
                part.Price = price;
            else
                part.Price = null; // prefer null nad 0 kad nije unio vrijednost

            var stockStr = await PromptAsync(title, "Količina na skladištu:", part.Stock > 0 ? part.Stock.ToString(CultureInfo.InvariantCulture) : string.Empty);
            if (!string.IsNullOrWhiteSpace(stockStr) && int.TryParse(stockStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock))
                part.Stock = stock;
            else
                part.Stock = 0;

            part.Location = await PromptAsync(title, "Lokacija skladišta:", part.Location) ?? part.Location;
            part.Image    = await PromptAsync(title, "Putanja ili ime slike:", part.Image) ?? part.Image;

            return true;
        }
    }
}
