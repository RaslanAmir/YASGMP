using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel; // ⬅ add this
using YasGMP.Models;
using YasGMP.Services;
using MySqlConnector;

namespace YasGMP
{
    public partial class SuppliersPage : ContentPage
    {
        public ObservableCollection<Supplier> Suppliers { get; } = new();
        private readonly DatabaseService _dbService;

        public SuppliersPage()
        {
            InitializeComponent();

            var connStr = ResolveMySqlConnectionStringFromApp();
            _dbService = new DatabaseService(connStr);

            BindingContext = this;
            _ = LoadSuppliersAsync();
        }

        private static string ResolveMySqlConnectionStringFromApp()
        {
            if (Application.Current is not App app)
                throw new InvalidOperationException("Application.Current nije tipa App.");

            var cfgObj = app.AppConfig;
            if (cfgObj != null)
            {
                try
                {
                    var idxer = cfgObj.GetType().GetProperty("Item", new[] { typeof(string) });
                    if (idxer != null)
                    {
                        var viaIndexer = idxer.GetValue(cfgObj, new object[] { "ConnectionStrings:MySqlDb" }) as string;
                        if (!string.IsNullOrWhiteSpace(viaIndexer))
                            return viaIndexer!;
                    }
                }
                catch { /* ignore */ }

                try
                {
                    var cs = cfgObj.GetType().GetProperty("ConnectionStrings")?.GetValue(cfgObj);
                    var mysql = cs?.GetType().GetProperty("MySqlDb")?.GetValue(cs) as string;
                    if (!string.IsNullOrWhiteSpace(mysql))
                        return mysql!;
                }
                catch { /* ignore */ }
            }

            throw new InvalidOperationException("MySqlDb connection string nije pronađen u konfiguraciji.");
        }

        // ✅ UI-thread safe, exception-safe loading
        private async Task LoadSuppliersAsync()
        {
            const string sql = @"SELECT id, name, vat_number, address, city, country, email, phone, website, supplier_type, notes, contract_file FROM suppliers";

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
                            Id           = Convert.ToInt32(row["id"]),
                            Name         = row["name"]?.ToString()           ?? string.Empty,
                            VatNumber    = row["vat_number"]?.ToString()     ?? string.Empty,
                            Address      = row["address"]?.ToString()        ?? string.Empty,
                            City         = row["city"]?.ToString()           ?? string.Empty,
                            Country      = row["country"]?.ToString()        ?? string.Empty,
                            Email        = row["email"]?.ToString()          ?? string.Empty,
                            Phone        = row["phone"]?.ToString()          ?? string.Empty,
                            Website      = row["website"]?.ToString()        ?? string.Empty,
                            SupplierType = row["supplier_type"]?.ToString()  ?? string.Empty,
                            Notes        = row["notes"]?.ToString()          ?? string.Empty,
                            ContractFile = row["contract_file"]?.ToString()  ?? string.Empty
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Dobavljači", $"Greška pri učitavanju: {ex.Message}", "OK");
            }
        }

        private async void OnAddSupplierClicked(object? sender, EventArgs e)
        {
            var newSupplier = new Supplier();
            bool result = await ShowSupplierFormAsync(newSupplier, "Unesi novog dobavljača/servisera");
            if (result)
            {
                const string sql = @"INSERT INTO suppliers (name, vat_number, address, city, country, email, phone, website, supplier_type, notes, contract_file)
                                     VALUES (@name, @vat_number, @address, @city, @country, @email, @phone, @website, @supplier_type, @notes, @contract_file)";
                var pars = new[]
                {
                    new MySqlParameter("@name",          newSupplier.Name ?? string.Empty),
                    new MySqlParameter("@vat_number",    newSupplier.VatNumber ?? string.Empty),
                    new MySqlParameter("@address",       newSupplier.Address ?? string.Empty),
                    new MySqlParameter("@city",          newSupplier.City ?? string.Empty),
                    new MySqlParameter("@country",       newSupplier.Country ?? string.Empty),
                    new MySqlParameter("@email",         newSupplier.Email ?? string.Empty),
                    new MySqlParameter("@phone",         newSupplier.Phone ?? string.Empty),
                    new MySqlParameter("@website",       newSupplier.Website ?? string.Empty),
                    new MySqlParameter("@supplier_type", newSupplier.SupplierType ?? string.Empty),
                    new MySqlParameter("@notes",         newSupplier.Notes ?? string.Empty),
                    new MySqlParameter("@contract_file", newSupplier.ContractFile ?? string.Empty)
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

        private async void OnEditSupplierClicked(object? sender, EventArgs e)
        {
            if (SupplierListView.SelectedItem is Supplier selected)
            {
                var supplierToEdit = new Supplier
                {
                    Id           = selected.Id,
                    Name         = selected.Name,
                    VatNumber    = selected.VatNumber,
                    Address      = selected.Address,
                    City         = selected.City,
                    Country      = selected.Country,
                    Email        = selected.Email,
                    Phone        = selected.Phone,
                    Website      = selected.Website,
                    SupplierType = selected.SupplierType,
                    Notes        = selected.Notes,
                    ContractFile = selected.ContractFile
                };

                bool result = await ShowSupplierFormAsync(supplierToEdit, "Uredi dobavljača/servisera");
                if (result)
                {
                    const string sql = @"UPDATE suppliers SET 
                                            name=@name, vat_number=@vat_number, address=@address,
                                            city=@city, country=@country, email=@email, phone=@phone,
                                            website=@website, supplier_type=@supplier_type, notes=@notes,
                                            contract_file=@contract_file
                                         WHERE id=@id";
                    var pars = new[]
                    {
                        new MySqlParameter("@name",          supplierToEdit.Name ?? string.Empty),
                        new MySqlParameter("@vat_number",    supplierToEdit.VatNumber ?? string.Empty),
                        new MySqlParameter("@address",       supplierToEdit.Address ?? string.Empty),
                        new MySqlParameter("@city",          supplierToEdit.City ?? string.Empty),
                        new MySqlParameter("@country",       supplierToEdit.Country ?? string.Empty),
                        new MySqlParameter("@email",         supplierToEdit.Email ?? string.Empty),
                        new MySqlParameter("@phone",         supplierToEdit.Phone ?? string.Empty),
                        new MySqlParameter("@website",       supplierToEdit.Website ?? string.Empty),
                        new MySqlParameter("@supplier_type", supplierToEdit.SupplierType ?? string.Empty),
                        new MySqlParameter("@notes",         supplierToEdit.Notes ?? string.Empty),
                        new MySqlParameter("@contract_file", supplierToEdit.ContractFile ?? string.Empty),
                        new MySqlParameter("@id",            supplierToEdit.Id)
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
            else
            {
                await DisplayAlert("Obavijest", "Molimo odaberite dobavljača iz liste za uređivanje.", "OK");
            }
        }

        private async void OnDeleteSupplierClicked(object? sender, EventArgs e)
        {
            if (SupplierListView.SelectedItem is Supplier selected)
            {
                bool confirm = await DisplayAlert("Potvrda brisanja", $"Želite li izbrisati dobavljača: {selected.Name}?", "Da", "Ne");
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
            else
            {
                await DisplayAlert("Obavijest", "Molimo odaberite dobavljača iz liste za brisanje.", "OK");
            }
        }

        private async Task<bool> ShowSupplierFormAsync(Supplier supplier, string title)
        {
            supplier.Name         = await DisplayPromptAsync(title, "Naziv dobavljača/servisera:", initialValue: supplier.Name);
            if (supplier.Name is null) return false;

            supplier.VatNumber    = await DisplayPromptAsync(title, "OIB/Porezni broj:", initialValue: supplier.VatNumber);
            supplier.Address      = await DisplayPromptAsync(title, "Adresa:", initialValue: supplier.Address);
            supplier.City         = await DisplayPromptAsync(title, "Grad/mjesto:", initialValue: supplier.City);
            supplier.Country      = await DisplayPromptAsync(title, "Država:", initialValue: supplier.Country);
            supplier.Email        = await DisplayPromptAsync(title, "Email:", initialValue: supplier.Email);
            supplier.Phone        = await DisplayPromptAsync(title, "Telefon:", initialValue: supplier.Phone);
            supplier.Website      = await DisplayPromptAsync(title, "Web stranica:", initialValue: supplier.Website);
            supplier.SupplierType = await DisplayPromptAsync(title, "Tip dobavljača (dijelovi, servis, laboratorij...):", initialValue: supplier.SupplierType);
            supplier.Notes        = await DisplayPromptAsync(title, "Posebne napomene:", initialValue: supplier.Notes);
            supplier.ContractFile = await DisplayPromptAsync(title, "Putanja do PDF ugovora/certifikata:", initialValue: supplier.ContractFile);

            return true;
        }
    }
}
