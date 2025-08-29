using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.Services;
using MySqlConnector;

namespace YasGMP
{
    /// <summary>
    /// UsersPage – ekran za pregled, unos, uređivanje i brisanje korisnika.
    /// </summary>
    public partial class UsersPage : ContentPage
    {
        /// <summary>Observable kolekcija svih korisnika za binding.</summary>
        public ObservableCollection<User> Users { get; } = new();

        /// <summary>Servis za pristup bazi.</summary>
        private readonly DatabaseService _dbService;

        /// <summary>
        /// Konstruktor – sigurno učitavanje konfiguracije i polaznih podataka.
        /// </summary>
        /// <exception cref="InvalidOperationException">Ako App ili connection string nisu dostupni.</exception>
        public UsersPage()
        {
            InitializeComponent();

            // Connection string bez oslanjanja na extension GetConnectionString (može nedostajati u vašoj konfiguraciji)
            var connStr = ResolveMySqlConnectionStringFromApp();
            _dbService = new DatabaseService(connStr);

            BindingContext = this;
            _ = LoadUsersAsync();
        }

        /// <summary>
        /// Sigurno dohvaća MySQL connection string iz <see cref="App.AppConfig"/> bez ovisnosti o ekstenzijama.
        /// Pokušava nekoliko uobičajenih lokacija:
        /// <list type="number">
        /// <item>Indexer putanja "ConnectionStrings:MySqlDb"</item>
        /// <item>Svojstvo App.AppConfig.ConnectionStrings.MySqlDb (reflection)</item>
        /// </list>
        /// </summary>
        private static string ResolveMySqlConnectionStringFromApp()
        {
            if (Application.Current is not App app)
                throw new InvalidOperationException("Application.Current nije tipa App.");

            // 1) IConfiguration indexer "ConnectionStrings:MySqlDb"
            var cfgObj = app.AppConfig;
            if (cfgObj != null)
            {
                // pokušaj indexera
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

                // 2) ConnectionStrings.MySqlDb
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

        /// <summary>Učitava sve korisnike iz baze i puni kolekciju.</summary>
        private async Task LoadUsersAsync()
        {
            Users.Clear();
            const string sql = @"SELECT id, username, password, full_name, role, active, email, phone, last_login, digital_signature FROM users";
            var dt = await _dbService.ExecuteSelectAsync(sql).ConfigureAwait(false);

            foreach (System.Data.DataRow row in dt.Rows)
            {
                Users.Add(new User
                {
                    Id               = Convert.ToInt32(row["id"]),
                    Username         = row["username"]?.ToString()         ?? string.Empty,
                    PasswordHash     = row["password"]?.ToString()         ?? string.Empty,
                    FullName         = row["full_name"]?.ToString()        ?? string.Empty,
                    Role             = row["role"]?.ToString()             ?? string.Empty,
                    Active           = row["active"] != DBNull.Value && Convert.ToBoolean(row["active"]),
                    Email            = row["email"]?.ToString()            ?? string.Empty,
                    Phone            = row["phone"]?.ToString()            ?? string.Empty,
                    LastLogin        = row["last_login"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(row["last_login"]),
                    DigitalSignature = row["digital_signature"]?.ToString() ?? string.Empty
                });
            }
        }

        /// <summary>Otvara formu za dodavanje novog korisnika.</summary>
        private async void OnAddUserClicked(object? sender, EventArgs e)
        {
            var newUser = new User { Active = true };
            bool result = await ShowUserFormAsync(newUser, "Unesi novog korisnika");
            if (result)
            {
                const string sql = @"INSERT INTO users (username, password, full_name, role, active, email, phone, digital_signature)
                                     VALUES (@username, @password, @full_name, @role, @active, @email, @phone, @digital_signature)";
                var pars = new[]
                {
                    new MySqlParameter("@username",          newUser.Username ?? string.Empty),
                    new MySqlParameter("@password",          newUser.PasswordHash ?? string.Empty), // DEMO; u produkciji hash!
                    new MySqlParameter("@full_name",         newUser.FullName ?? string.Empty),
                    new MySqlParameter("@role",              newUser.Role ?? string.Empty),
                    new MySqlParameter("@active",            newUser.Active),
                    new MySqlParameter("@email",             newUser.Email ?? string.Empty),
                    new MySqlParameter("@phone",             newUser.Phone ?? string.Empty),
                    new MySqlParameter("@digital_signature", newUser.DigitalSignature ?? string.Empty)
                };
                await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                await LoadUsersAsync().ConfigureAwait(false);
            }
        }

        /// <summary>Otvara formu za uređivanje postojećeg korisnika.</summary>
        private async void OnEditUserClicked(object? sender, EventArgs e)
        {
            if (UserListView.SelectedItem is User selected)
            {
                var userToEdit = new User
                {
                    Id               = selected.Id,
                    Username         = selected.Username,
                    PasswordHash     = selected.PasswordHash,
                    FullName         = selected.FullName,
                    Role             = selected.Role,
                    Active           = selected.Active,
                    Email            = selected.Email,
                    Phone            = selected.Phone,
                    LastLogin        = selected.LastLogin,
                    DigitalSignature = selected.DigitalSignature
                };

                bool result = await ShowUserFormAsync(userToEdit, "Uredi korisnika");
                if (result)
                {
                    const string sql = @"UPDATE users SET 
                                            username=@username, password=@password, full_name=@full_name, 
                                            role=@role, active=@active, email=@email, phone=@phone,
                                            digital_signature=@digital_signature
                                         WHERE id=@id";
                    var pars = new[]
                    {
                        new MySqlParameter("@username",          userToEdit.Username ?? string.Empty),
                        new MySqlParameter("@password",          userToEdit.PasswordHash ?? string.Empty),
                        new MySqlParameter("@full_name",         userToEdit.FullName ?? string.Empty),
                        new MySqlParameter("@role",              userToEdit.Role ?? string.Empty),
                        new MySqlParameter("@active",            userToEdit.Active),
                        new MySqlParameter("@email",             userToEdit.Email ?? string.Empty),
                        new MySqlParameter("@phone",             userToEdit.Phone ?? string.Empty),
                        new MySqlParameter("@digital_signature", userToEdit.DigitalSignature ?? string.Empty),
                        new MySqlParameter("@id",                userToEdit.Id)
                    };
                    await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                    await LoadUsersAsync().ConfigureAwait(false);
                }
            }
            else
            {
                await DisplayAlert("Obavijest", "Molimo odaberite korisnika iz liste za uređivanje.", "OK");
            }
        }

        /// <summary>Briše odabranog korisnika iz baze.</summary>
        private async void OnDeleteUserClicked(object? sender, EventArgs e)
        {
            if (UserListView.SelectedItem is User selected)
            {
                bool confirm = await DisplayAlert("Potvrda brisanja", $"Želite li izbrisati korisnika: {selected.FullName} ({selected.Username})?", "Da", "Ne");
                if (confirm)
                {
                    const string sql = "DELETE FROM users WHERE id=@id";
                    var pars = new[] { new MySqlParameter("@id", selected.Id) };
                    await _dbService.ExecuteNonQueryAsync(sql, pars).ConfigureAwait(false);
                    await LoadUsersAsync().ConfigureAwait(false);
                }
            }
            else
            {
                await DisplayAlert("Obavijest", "Molimo odaberite korisnika iz liste za brisanje.", "OK");
            }
        }

        /// <summary>Prikazuje formu za unos ili uređivanje korisnika.</summary>
        private async Task<bool> ShowUserFormAsync(User user, string title)
        {
            user.Username = await DisplayPromptAsync(title, "Korisničko ime (login):", initialValue: user.Username);
            if (string.IsNullOrWhiteSpace(user.Username)) return false;

            // DEMO – u produkciji koristiti hash/crypt
            user.PasswordHash = await DisplayPromptAsync(title, "Lozinka (plain za demo!):", initialValue: user.PasswordHash);

            user.FullName = await DisplayPromptAsync(title, "Ime i prezime:", initialValue: user.FullName);
            if (string.IsNullOrWhiteSpace(user.FullName)) return false;

            user.Role = await DisplayPromptAsync(title, "Uloga (admin, sef, tehnicar, auditor):", initialValue: user.Role);
            if (string.IsNullOrWhiteSpace(user.Role)) return false;

            var activeStr = await DisplayPromptAsync(title, "Aktivan? (true/false):", initialValue: user.Active ? "true" : "false");
            user.Active = activeStr?.ToLowerInvariant() == "true";

            user.Email            = await DisplayPromptAsync(title, "Email:", initialValue: user.Email);
            user.Phone            = await DisplayPromptAsync(title, "Telefon:", initialValue: user.Phone);
            user.DigitalSignature = await DisplayPromptAsync(title, "Digitalni potpis (opcionalno):", initialValue: user.DigitalSignature);

            return true;
        }
    }
}
