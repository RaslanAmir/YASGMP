using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel; // MainThread
using Microsoft.Maui.Controls;
using MySqlConnector;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Views
{
    /// <summary>
    /// <b>SuppliersPage</b> — pregled, unos, uređivanje i brisanje dobavljača/servisera/laboratorija.
    /// <para>
    /// • UI-thread safe (MainThread) — izbjegnute WinUI 0x8001010E greške.<br/>
    /// • Robusno dohvaćanje konekcijskog stringa iz <see cref="App.AppConfig"/>.<br/>
    /// • Sigurne konverzije NULL vrijednosti i parametri preko MySqlConnector.<br/>
    /// • Potpuna XML dokumentacija za IntelliSense.
    /// </para>
    /// </summary>
    public partial class SuppliersPage : ContentPage
    {
        /// <summary>Observable kolekcija dobavljača za binding.</summary>
        public ObservableCollection<Supplier> Suppliers { get; } = new();

        /// <summary>Servis baze podataka.</summary>
        private readonly DatabaseService _dbService;

        /// <summary>
        /// Konstruktor — inicijalizira UI, konfiguraciju i učitava podatke.
        /// </summary>
        /// <exception cref="InvalidOperationException">Ako konekcijski string nije dostupan.</exception>
        public SuppliersPage(DatabaseService dbService)
        {
            InitializeComponent();

            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));

            BindingContext = this;
            _ = LoadSuppliersAsync();
        }

        /// <summary>Parameterless ctor for Shell/XAML; koristi ServiceLocator.</summary>
        public SuppliersPage()
            : this(ServiceLocator.GetRequiredService<DatabaseService>())
        {
        }

        /// <summary>
        /// Učitava sve dobavljače i puni kolekciju <see cref="Suppliers"/> na glavnoj niti.
        /// </summary>
        private async Task LoadSuppliersAsync()
        {
            const string sql = @"SELECT id, name, vat_number, address, city, country, email, phone, website, supplier_type, notes, contract_file 
                                 FROM suppliers";

            try
            {
                var dt = await _dbService.ExecuteSelectAsync(sql).ConfigureAwait(false);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Suppliers.Clear();
                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        Suppliers.Add(new Supplier
                        {
                            Id = Convert.ToInt32(row["id"]),
                            Name = row["name"]?.ToString() ?? string.Empty,
                            VatNumber = row["vat_number"]?.ToString() ?? string.Empty,
                            Address = row["address"]?.ToString() ?? string.Empty,
                            City = row["city"]?.ToString() ?? string.Empty,
                            Country = row["country"]?.ToString() ?? string.Empty,
                            Email = row["email"]?.ToString() ?? string.Empty,
                            Phone = row["phone"]?.ToString() ?? string.Empty,
                            Website = row["website"]?.ToString() ?? string.Empty,
                            SupplierType = row["supplier_type"]?.ToString() ?? string.Empty,
                            Notes = row["notes"]?.ToString() ?? string.Empty,
                            ContractFile = row["contract_file"]?.ToString() ?? string.Empty
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Dobavljači", $"Greška pri učitavanju: {ex.Message}", "OK");
            }
        }

        /// <summary>Dodaje novog dobavljača kroz promptove te sprema zapis u bazu.</summary>
        private async void OnAddSupplierClicked(object? sender, EventArgs e)
        {
            var s = new Supplier();
            if (await ShowSupplierFormAsync(s, "Unesi novog dobavljača/servisera"))
            {
                const string sql = @"INSERT INTO suppliers (name, vat_number, address, city, country, email, phone, website, supplier_type, notes, contract_file)
                                     VALUES (@name, @vat_number, @address, @city, @country, @email, @phone, @website, @supplier_type, @notes, @contract_file)";
                var pars = new[]
                {
                    new MySqlParameter("@name", s.Name ?? string.Empty),
                    new MySqlParameter("@vat_number", s.VatNumber ?? string.Empty),
                    new MySqlParameter("@address", s.Address ?? string.Empty),
                    new MySqlParameter("@city", s.City ?? string.Empty),
                    new MySqlParameter("@country", s.Country ?? string.Empty),
                    new MySqlParameter("@email", s.Email ?? string.Empty),
                    new MySqlParameter("@phone", s.Phone ?? string.Empty),
                    new MySqlParameter("@website", s.Website ?? string.Empty),
                    new MySqlParameter("@supplier_type", s.SupplierType ?? string.Empty),
                    new MySqlParameter("@notes", s.Notes ?? string.Empty),
                    new MySqlParameter("@contract_file", s.ContractFile ?? string.Empty)
                };

                try
                {
                    await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                    await LoadSuppliersAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await SafeNavigator.ShowAlertAsync("Spremanje dobavljača", ex.Message, "OK");
                }
            }
        }

        /// <summary>Uređuje trenutno odabranog dobavljača.</summary>
        private async void OnEditSupplierClicked(object? sender, EventArgs e)
        {
            if (SupplierListView.SelectedItem is not Supplier selected)
            {
                await DisplayAlert("Obavijest", "Molimo odaberite dobavljača iz liste za uređivanje.", "OK");
                return;
            }

            var s = new Supplier
            {
                Id = selected.Id,
                Name = selected.Name,
                VatNumber = selected.VatNumber,
                Address = selected.Address,
                City = selected.City,
                Country = selected.Country,
                Email = selected.Email,
                Phone = selected.Phone,
                Website = selected.Website,
                SupplierType = selected.SupplierType,
                Notes = selected.Notes,
                ContractFile = selected.ContractFile
            };

            if (await ShowSupplierFormAsync(s, "Uredi dobavljača/servisera"))
            {
                const string sql = @"UPDATE suppliers SET 
                                        name=@name, vat_number=@vat_number, address=@address,
                                        city=@city, country=@country, email=@email, phone=@phone,
                                        website=@website, supplier_type=@supplier_type, notes=@notes,
                                        contract_file=@contract_file
                                     WHERE id=@id";
                var pars = new[]
                {
                    new MySqlParameter("@name", s.Name ?? string.Empty),
                    new MySqlParameter("@vat_number", s.VatNumber ?? string.Empty),
                    new MySqlParameter("@address", s.Address ?? string.Empty),
                    new MySqlParameter("@city", s.City ?? string.Empty),
                    new MySqlParameter("@country", s.Country ?? string.Empty),
                    new MySqlParameter("@email", s.Email ?? string.Empty),
                    new MySqlParameter("@phone", s.Phone ?? string.Empty),
                    new MySqlParameter("@website", s.Website ?? string.Empty),
                    new MySqlParameter("@supplier_type", s.SupplierType ?? string.Empty),
                    new MySqlParameter("@notes", s.Notes ?? string.Empty),
                    new MySqlParameter("@contract_file", s.ContractFile ?? string.Empty),
                    new MySqlParameter("@id", s.Id)
                };

                try
                {
                    await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                    await LoadSuppliersAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await SafeNavigator.ShowAlertAsync("Ažuriranje dobavljača", ex.Message, "OK");
                }
            }
        }

        /// <summary>Briše trenutno odabranog dobavljača nakon potvrde.</summary>
        private async void OnDeleteSupplierClicked(object? sender, EventArgs e)
        {
            if (SupplierListView.SelectedItem is not Supplier selected)
            {
                await DisplayAlert("Obavijest", "Molimo odaberite dobavljača iz liste za brisanje.", "OK");
                return;
            }

            bool confirm = await DisplayAlert("Potvrda brisanja", $"Želite li izbrisati: {selected.Name}?", "Da", "Ne");
            if (!confirm) return;

            const string sql = "DELETE FROM suppliers WHERE id=@id";
            var pars = new[] { new MySqlParameter("@id", selected.Id) };

            try
            {
                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadSuppliersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Brisanje dobavljača", ex.Message, "OK");
            }
        }

        /// <summary>
        /// Jednostavna prompt-forma za unos/uređivanje dobavljača. Svi pozivi su UI-thread safe.
        /// </summary>
        private async Task<bool> ShowSupplierFormAsync(Supplier supplier, string title)
        {
            var name = await DisplayPromptAsync(title, "Naziv dobavljača/servisera:", initialValue: supplier.Name);
            if (name is null) return false;
            supplier.Name = name;

            var vat = await DisplayPromptAsync(title, "OIB/Porezni broj:", initialValue: supplier.VatNumber);
            supplier.VatNumber = vat ?? supplier.VatNumber;

            var addr = await DisplayPromptAsync(title, "Adresa:", initialValue: supplier.Address);
            supplier.Address = addr ?? supplier.Address;

            var city = await DisplayPromptAsync(title, "Grad/mjesto:", initialValue: supplier.City);
            supplier.City = city ?? supplier.City;

            var country = await DisplayPromptAsync(title, "Država:", initialValue: supplier.Country);
            supplier.Country = country ?? supplier.Country;

            var email = await DisplayPromptAsync(title, "Email:", initialValue: supplier.Email);
            supplier.Email = email ?? supplier.Email;

            var phone = await DisplayPromptAsync(title, "Telefon:", initialValue: supplier.Phone);
            supplier.Phone = phone ?? supplier.Phone;

            var web = await DisplayPromptAsync(title, "Web stranica:", initialValue: supplier.Website);
            supplier.Website = web ?? supplier.Website;

            var type = await DisplayPromptAsync(title, "Tip dobavljača (dijelovi, servis, laboratorij...):", initialValue: supplier.SupplierType);
            supplier.SupplierType = type ?? supplier.SupplierType;

            var notes = await DisplayPromptAsync(title, "Posebne napomene:", initialValue: supplier.Notes);
            supplier.Notes = notes ?? supplier.Notes;

            var contract = await DisplayPromptAsync(title, "Putanja do PDF ugovora/certifikata:", initialValue: supplier.ContractFile);
            supplier.ContractFile = contract ?? supplier.ContractFile;

            return true;
        }
    }
}
