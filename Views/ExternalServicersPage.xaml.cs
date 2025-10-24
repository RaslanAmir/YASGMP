// ==============================================================================
//  File: Views/ExternalServicersPage.xaml.cs
//  Project: YasGMP
//  Summary:
//      Pregled/unos/uređivanje/brisanje vanjskih servisera, laboratorija i partnera.
//      • UI-thread sigurne interakcije (MainThread / SafeNavigator) – izbjegnuta WinUI 0x8001010E
//      • Tolerantno na promjene modela (refleksija TrySet/GetString/GetInt)
//      • Robusno dohvaćanje konekcijskog stringa iz App.AppConfig
//      • Potpuna XML dokumentacija za IntelliSense
//      • Ne oslanja se na generirana x:Name polja (koristi FindByName).
// ==============================================================================

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel; // MainThread
using Microsoft.Maui.Controls;
using YasGMP.Common;
using YasGMP.Models;
using YasGMP.Services;
using ExternalServicer = YasGMP.Models.ExternalContractor;

namespace YasGMP
{
    /// <summary>
    /// <b>ExternalServicersPage</b> — pregled/unos/uređivanje/brisanje vanjskih servisera, laboratorija i partnera.
    /// Implementira robusnu zaštitu od null referenci, UI-thread sigurne dijaloge i tolerantna je na promjene modela.
    /// </summary>
    public partial class ExternalServicersPage : ContentPage
    {
        /// <summary>Kolekcija svih vanjskih servisera za prikaz/binding.</summary>
        public ObservableCollection<ExternalServicer> ExternalServicers { get; } = new();

        /// <summary>Servis baze (MySqlConnector / DatabaseService).</summary>
        private readonly DatabaseService _dbService;

        /// <summary>Keš korisnika (npr. za potpis/ID modifikatora).</summary>
        private List<User> _users = new();

        /// <summary>
        /// Sigurno dohvaća <see cref="CollectionView"/> iz XAML-a prema nazivu <c>ServicersListView</c>,
        /// bez ovisnosti o generiranom polju. Vraća <c>null</c> ako nije nađen.
        /// </summary>
        private CollectionView? ServicersListViewControl => this.FindByName<CollectionView>("ServicersListView");

        /// <summary>
        /// Inicijalizira stranicu, validira konfiguraciju i učitava podatke.
        /// </summary>
        /// <exception cref="InvalidOperationException">Ako konfiguracija ili konekcijski string nisu dostupni.</exception>
        public ExternalServicersPage(DatabaseService dbService)
        {
            InitializeComponent();

            this._dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));

            BindingContext = this;

            _ = this.LoadLookupsAsync();
            _ = this.LoadExternalServicersAsync();
        }

        /// <summary>Parameterless ctor za Shell/XAML (ServiceLocator fallback).</summary>
        public ExternalServicersPage()
            : this(ServiceLocator.GetRequiredService<DatabaseService>())
        {
        }


        private static bool TrySet(object obj, string propName, object? value)
        {
            if (obj == null) return false;
            var p = obj.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (p == null || !p.CanWrite) return false;

            try
            {
                object? toAssign = value;

                if (value is string s && p.PropertyType != typeof(string))
                {
                    if (p.PropertyType == typeof(DateTime) && DateTime.TryParse(s, out var dt)) toAssign = dt;
                    else if (p.PropertyType == typeof(DateTime?) && DateTime.TryParse(s, out var dtn)) toAssign = (DateTime?)dtn;
                    else if (p.PropertyType == typeof(int) && int.TryParse(s, out var i)) toAssign = i;
                    else if (p.PropertyType == typeof(int?) && int.TryParse(s, out var i2)) toAssign = (int?)i2;
                    else if (p.PropertyType == typeof(double) && double.TryParse(s, out var d)) toAssign = d;
                    else if (p.PropertyType == typeof(double?) && double.TryParse(s, out var d2)) toAssign = (double?)d2;
                }

                if (toAssign != null && toAssign is IConvertible && p.PropertyType != typeof(string))
                {
                    try
                    {
                        if (p.PropertyType == typeof(int)) toAssign = Convert.ToInt32(toAssign);
                        else if (p.PropertyType == typeof(int?)) toAssign = (int?)Convert.ToInt32(toAssign);
                        else if (p.PropertyType == typeof(double)) toAssign = Convert.ToDouble(toAssign);
                        else if (p.PropertyType == typeof(double?)) toAssign = (double?)Convert.ToDouble(toAssign);
                    }
                    catch { /* ignore conversion failure */ }
                }

                p.SetValue(obj, toAssign);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string GetString(object obj, params string[] propNames)
        {
            if (obj == null) return string.Empty;
            foreach (var n in propNames)
            {
                var p = obj.GetType().GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                var v = p?.GetValue(obj)?.ToString();
                if (!string.IsNullOrWhiteSpace(v))
                    return v!;
            }
            return string.Empty;
        }

        private static int GetInt(object obj, params string[] propNames)
        {
            if (obj == null) return 0;
            foreach (var n in propNames)
            {
                var p = obj.GetType().GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (p == null) continue;

                var val = p.GetValue(obj);
                if (val == null) continue;

                if (val is int i) return i;
                if (val is long l) return unchecked((int)l);
                if (val is short s) return s;
                if (val is string sText && int.TryParse(sText, out var parsed)) return parsed;
            }
            return 0;
        }

        private async Task LoadLookupsAsync()
        {
            try
            {
                this._users = await this._dbService.GetAllUsersAsync().ConfigureAwait(false) ?? new List<User>();
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Neuspješno učitavanje korisnika: {ex.Message}", "OK");
            }
        }

        private async Task LoadExternalServicersAsync()
        {
            try
            {
                var list = await this._dbService.GetAllExternalServicersAsync().ConfigureAwait(false) ?? new List<ExternalServicer>();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    this.ExternalServicers.Clear();
                    foreach (var s in list)
                        this.ExternalServicers.Add(s);
                });
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Neuspješno učitavanje servisera: {ex.Message}", "OK");
            }
        }

        private async void OnAddServicerClicked(object? sender, EventArgs e)
        {
            try
            {
                var ext = new ExternalServicer();

                var app = Application.Current as App;
                var signer = (app?.LoggedUser?.FullName ?? app?.LoggedUser?.Username) ?? string.Empty;
                var userId = app?.LoggedUser?.Id ?? 0;

                TrySet(ext, "CooperationStart", DateTime.Today);
                TrySet(ext, "Status", "aktivan");
                TrySet(ext, "DigitalSignature", signer);
                TrySet(ext, "LastModified", DateTime.UtcNow);
                TrySet(ext, "LastModifiedById", userId);

                var ok = await ShowServicerFormAsync(ext, "Novi vanjski serviser/lab");
                if (!ok) return;

                await this._dbService.InsertOrUpdateExternalServicerAsync(ext, update: false).ConfigureAwait(false);
                await this.LoadExternalServicersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Spremanje nije uspjelo: {ex.Message}", "OK");
            }
        }

        private async void OnEditServicerClicked(object? sender, EventArgs e)
        {
            try
            {
                ExternalServicer? selected = this.ServicersListViewControl?.SelectedItem as ExternalServicer;

                if (selected is null)
                {
                    await SafeNavigator.ShowAlertAsync("Obavijest", "Odaberite servisera za uređivanje.", "OK");
                    return;
                }

                var ok = await ShowServicerFormAsync(selected, "Uredi vanjskog servisera/lab");
                if (!ok) return;

                var app = Application.Current as App;
                var userId = app?.LoggedUser?.Id ?? 0;
                TrySet(selected, "LastModified", DateTime.UtcNow);
                TrySet(selected, "LastModifiedById", userId);

                await this._dbService.InsertOrUpdateExternalServicerAsync(selected, update: true).ConfigureAwait(false);
                await this.LoadExternalServicersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Uređivanje nije uspjelo: {ex.Message}", "OK");
            }
        }

        private async void OnDeleteServicerClicked(object? sender, EventArgs e)
        {
            try
            {
                ExternalServicer? selected = this.ServicersListViewControl?.SelectedItem as ExternalServicer;

                if (selected is null)
                {
                    await SafeNavigator.ShowAlertAsync("Obavijest", "Odaberite servisera za brisanje.", "OK");
                    return;
                }

                var displayName = GetString(selected, "Name", "CompanyName");
                bool conf = await SafeNavigator.ConfirmAsync("Potvrda", $"Obriši servisera: {displayName}?", "Da", "Ne");
                if (!conf) return;

                var id = GetInt(selected, "Id");
                await this._dbService.DeleteExternalServicerAsync(id).ConfigureAwait(false);
                await this.LoadExternalServicersAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Greška", $"Brisanje nije uspjelo: {ex.Message}", "OK");
            }
        }

        private async Task<bool> ShowServicerFormAsync(ExternalServicer ext, string title)
        {
            var currentName = GetString(ext, "Name", "CompanyName");
            var name = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayPromptAsync(title, "Naziv firme/servisera/laba:", initialValue: currentName));
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (!TrySet(ext, "Name", name)) TrySet(ext, "CompanyName", name);

            var code = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayPromptAsync(title, "Interna šifra:", initialValue: GetString(ext, "Code")));
            TrySet(ext, "Code", code ?? string.Empty);

            var vat = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayPromptAsync(title, "OIB/ID:", initialValue: GetString(ext, "VatOrId", "VatID", "Oib", "OIB")));
            if (!TrySet(ext, "VatOrId", vat ?? string.Empty))
                TrySet(ext, "VatID", vat ?? string.Empty);

            var contact = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayPromptAsync(title, "Kontakt osoba:", initialValue: GetString(ext, "ContactPerson")));
            TrySet(ext, "ContactPerson", contact ?? string.Empty);

            var email = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayPromptAsync(title, "E-mail:", initialValue: GetString(ext, "Email")));
            TrySet(ext, "Email", email ?? string.Empty);

            var phone = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayPromptAsync(title, "Telefon/mobitel:", initialValue: GetString(ext, "Phone")));
            TrySet(ext, "Phone", phone ?? string.Empty);

            var address = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayPromptAsync(title, "Adresa:", initialValue: GetString(ext, "Address")));
            TrySet(ext, "Address", address ?? string.Empty);

            var type = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayActionSheet("Vrsta servisera", "Odustani", null,
                    "laboratorij", "servis", "audit", "inspekcija", "validacija", "ostalo"));
            if (!string.IsNullOrWhiteSpace(type))
            {
                if (!TrySet(ext, "Type", type)) TrySet(ext, "ServiceType", type);
            }

            var status = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayActionSheet("Status", "Odustani", null, "aktivan", "istekao", "suspendiran"));
            if (!string.IsNullOrWhiteSpace(status)) TrySet(ext, "Status", status);

            var startDef = GetString(ext, "CooperationStart");
            var start = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayPromptAsync(title, "Početak suradnje (YYYY-MM-DD):",
                    initialValue: string.IsNullOrWhiteSpace(startDef) ? string.Empty : startDef));
            if (!string.IsNullOrWhiteSpace(start)) TrySet(ext, "CooperationStart", start);

            var endDef = GetString(ext, "CooperationEnd");
            var end = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayPromptAsync(title, "Istek ugovora (YYYY-MM-DD):",
                    initialValue: string.IsNullOrWhiteSpace(endDef) ? string.Empty : endDef));
            if (!string.IsNullOrWhiteSpace(end)) TrySet(ext, "CooperationEnd", end);

            var note = await MainThread.InvokeOnMainThreadAsync(async () =>
                await DisplayPromptAsync(title, "GMP napomena / audit komentar:", initialValue: GetString(ext, "Comment", "Note")));
            if (!TrySet(ext, "Comment", note ?? string.Empty)) TrySet(ext, "Note", note ?? string.Empty);

            var app = Application.Current as App;
            var signer = (app?.LoggedUser?.FullName ?? app?.LoggedUser?.Username) ?? string.Empty;
            TrySet(ext, "DigitalSignature", signer);

            return true;
        }
    }
}
