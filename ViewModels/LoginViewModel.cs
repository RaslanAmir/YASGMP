using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using YasGMP.Models;
using YasGMP.Services;
using System.Net.Http;
using Microsoft.Maui.ApplicationModel;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>LoginViewModel</b> – GMP-compliant login logic for YasGMP.
    /// <para>
    /// Provides audit hooks (via services), anti-brute-force delay, device/IP forensics, 2FA flow,
    /// optional auth placeholders (PIN/Bio/SSO/QR), lock counters, language switch, and MAUI-friendly bindings.
    /// Includes a defensive login timeout so database connectivity issues cannot leave the UI spinning forever.
    /// </para>
    /// </summary>
    public class LoginViewModel : BindableObject, INotifyPropertyChanged
    {
        #region Private Fields

        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _twoFactorCode = string.Empty;
        private string _statusMessage = string.Empty;
        private string _deviceInfo = "DeviceInfo: N/A";
        private string _ipAddress = "IP: N/A";
        private string _selectedLanguage = "hr";
        private bool _isBusy;
        private bool _isPasswordHidden = true;
        private bool _isTwoFactorRequired;
        private bool _isAuthenticated;
        private bool _hasError;
        private bool _isCapsLockOn;
        private int _failedAttempts;
        private User? _loggedInUser;

        // Defensive: cap maximum wait time for the login attempt to avoid endless spinners on DB issues.
        private static readonly TimeSpan LoginTimeout = TimeSpan.FromSeconds(10);

        #endregion

        #region Public Properties

        /// <summary>Korisničko ime.</summary>
        public string Username
        {
            get => _username;
            set { if (_username != value) { _username = value; OnPropertyChanged(nameof(Username)); RefreshCommandStates(); } }
        }

        /// <summary>Lozinka (skrivena po defaultu).</summary>
        public string Password
        {
            get => _password;
            set { if (_password != value) { _password = value; OnPropertyChanged(nameof(Password)); RefreshCommandStates(); } }
        }

        /// <summary>Dvofaktorski kod (6 znamenki).</summary>
        public string TwoFactorCode
        {
            get => _twoFactorCode;
            set { if (_twoFactorCode != value) { _twoFactorCode = value; OnPropertyChanged(nameof(TwoFactorCode)); RefreshCommandStates(); } }
        }

        /// <summary>Status poruka za UI.</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); } }
        }

        /// <summary>Busy indikator.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); RefreshCommandStates(); } }
        }

        /// <summary>Indikacija greške.</summary>
        public bool HasError
        {
            get => _hasError;
            set { if (_hasError != value) { _hasError = value; OnPropertyChanged(nameof(HasError)); } }
        }

        /// <summary>Prikaz/skrivanje lozinke.</summary>
        public bool IsPasswordHidden
        {
            get => _isPasswordHidden;
            set { if (_isPasswordHidden != value) { _isPasswordHidden = value; OnPropertyChanged(nameof(IsPasswordHidden)); } }
        }

        /// <summary>Je li 2FA obavezan?</summary>
        public bool IsTwoFactorRequired
        {
            get => _isTwoFactorRequired;
            set { if (_isTwoFactorRequired != value) { _isTwoFactorRequired = value; OnPropertyChanged(nameof(IsTwoFactorRequired)); RefreshCommandStates(); } }
        }

        /// <summary>Je li korisnik autentificiran?</summary>
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set { if (_isAuthenticated != value) { _isAuthenticated = value; OnPropertyChanged(nameof(IsAuthenticated)); } }
        }

        /// <summary>Sažeti forenzički podaci o uređaju.</summary>
        public string DeviceInfo
        {
            get => _deviceInfo;
            private set { _deviceInfo = value; OnPropertyChanged(nameof(DeviceInfo)); }
        }

        /// <summary>Javna IP adresa.</summary>
        public string IpAddress
        {
            get => _ipAddress;
            private set { _ipAddress = value; OnPropertyChanged(nameof(IpAddress)); }
        }

        /// <summary>Odabrani jezik sučelja.</summary>
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value)
                {
                    _selectedLanguage = value;
                    OnPropertyChanged(nameof(SelectedLanguage));
                    StatusMessage = $"Jezik postavljen na: {value?.ToUpper()}";
                }
            }
        }

        /// <summary>Broj uzastopnih neuspjelih pokušaja.</summary>
        public int FailedAttempts
        {
            get => _failedAttempts;
            private set { _failedAttempts = value; OnPropertyChanged(nameof(FailedAttempts)); }
        }

        /// <summary>Upozorenje na CapsLock.</summary>
        public bool IsCapsLockOn
        {
            get => _isCapsLockOn;
            set { if (_isCapsLockOn != value) { _isCapsLockOn = value; OnPropertyChanged(nameof(IsCapsLockOn)); } }
        }

        /// <summary>Prijavljeni korisnik (nakon uspješne prijave).</summary>
        public User? LoggedInUser
        {
            get => _loggedInUser;
            set { if (_loggedInUser != value) { _loggedInUser = value; OnPropertyChanged(nameof(LoggedInUser)); } }
        }

        /// <summary>Može li se pokrenuti login akcija?</summary>
        public bool IsLoginEnabled => !IsBusy &&
                                      !string.IsNullOrWhiteSpace(Username) &&
                                      !string.IsNullOrWhiteSpace(Password);

        /// <summary>Povijest prijava (za UI listu).</summary>
        public ObservableCollection<string> LastLogins { get; } = new();

        /// <summary>Prikaz liste povijesti samo ako ima zapisa.</summary>
        public bool HasLoginHistory => LastLogins.Count > 0;

        #endregion

        #region Commands

        /// <summary>Pokretanje prijave.</summary>
        public ICommand LoginCommand { get; }

        /// <summary>Prikaz/skrivanje lozinke.</summary>
        public ICommand TogglePasswordVisibilityCommand { get; }

        /// <summary>Potvrda 2FA koda.</summary>
        public ICommand SubmitTwoFactorCommand { get; }

        /// <summary>Promjena jezika sučelja.</summary>
        public ICommand SwitchLanguageCommand { get; }

        /// <summary>Zaboravljena lozinka – placeholder.</summary>
        public ICommand ForgotPasswordCommand { get; }

        /// <summary>PIN prijava – placeholder.</summary>
        public ICommand PinLoginCommand { get; }

        /// <summary>Biometrija – placeholder.</summary>
        public ICommand BiometricLoginCommand { get; }

        /// <summary>Smartcard – placeholder.</summary>
        public ICommand SmartcardLoginCommand { get; }

        /// <summary>SSO – placeholder.</summary>
        public ICommand SsoLoginCommand { get; }

        /// <summary>QR login – placeholder.</summary>
        public ICommand QrLoginCommand { get; }

        /// <summary>Podrška – placeholder.</summary>
        public ICommand SupportCommand { get; }

        /// <summary>Privatnost – placeholder.</summary>
        public ICommand PrivacyCommand { get; }

        /// <summary>Prikaz audita – placeholder.</summary>
        public ICommand ShowAuditModalCommand { get; }

        #endregion

        #region Dependencies

        private readonly AuthService _authService;

        #endregion

        #region Constructors

        /// <summary>
        /// DI constructor: <see cref="AuthService"/> je obavezan (DI isporučuje vlastite ovisnosti).
        /// </summary>
        public LoginViewModel(AuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            LoginCommand                    = new Command(async () => await ExecuteLoginAsync(), () => IsLoginEnabled);
            TogglePasswordVisibilityCommand = new Command(() => IsPasswordHidden = !IsPasswordHidden);
            SubmitTwoFactorCommand          = new Command(async () => await SubmitTwoFactorAsync(),
                                                          () => !IsBusy && IsTwoFactorRequired && !string.IsNullOrWhiteSpace(TwoFactorCode));
            SwitchLanguageCommand           = new Command<string>(SwitchLanguage);
            ForgotPasswordCommand           = new Command(ShowForgotPasswordDialog);
            PinLoginCommand                 = new Command(ExecutePinLogin);
            BiometricLoginCommand           = new Command(ExecuteBiometricLogin);
            SmartcardLoginCommand           = new Command(ExecuteSmartcardLogin);
            SsoLoginCommand                 = new Command(ExecuteSsoLogin);
            QrLoginCommand                  = new Command(ExecuteQrLogin);
            SupportCommand                  = new Command(ShowSupportDialog);
            PrivacyCommand                  = new Command(ShowPrivacyDialog);
            ShowAuditModalCommand           = new Command(ShowAuditModal);

            _ = InitializeAsync();
        }

        /// <summary>
        /// Zadani (parameterless) konstruktor za XAML/alatne potrebe – dohvaća <see cref="AuthService"/> iz MAUI DI kontejnera.
        /// </summary>
        public LoginViewModel() : this(ResolveAuthServiceFromMaui())
        {
        }

        /// <summary>
        /// Pomoćna metoda: dohvaća <see cref="AuthService"/> iz DI kontejnera.
        /// Baca jasnu grešku ako nije registriran (sprječava tihi pad).
        /// </summary>
        private static AuthService ResolveAuthServiceFromMaui()
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            if (services == null)
                throw new InvalidOperationException("MAUI service provider is not available. Ensure the application is initialized and services are configured.");

            var svc = services.GetService<AuthService>();
            if (svc == null)
                throw new InvalidOperationException("AuthService is not registered in the DI container. Please register it in MauiProgram.cs (builder.Services.AddSingleton<AuthService>()).");

            return svc;
        }

        #endregion

        #region Init / Helpers

        /// <summary>Prikuplja device/ip informacije i učitava povijest prijava.</summary>
        private async Task InitializeAsync()
        {
            DeviceInfo = CollectDeviceInfo();
            IpAddress  = await GetPublicIpAddressAsync();
            await LoadLoginHistoryAsync();
        }

        /// <summary>Osvježava CanExecute stanja vezanih komandi.</summary>
        private void RefreshCommandStates()
        {
            (LoginCommand as Command)?.ChangeCanExecute();
            (SubmitTwoFactorCommand as Command)?.ChangeCanExecute();
        }

        #endregion

        #region Private Methods (Auth Flow)

        /// <summary>
        /// Izvršava login flow (anti-brute-force odgoda, timeout zaštita, 2FA rukovanje).
        /// </summary>
        private async Task ExecuteLoginAsync()
        {
            IsBusy = true;
            HasError = false;
            StatusMessage = string.Empty;

            try
            {
                // Minimalna odgoda protiv brute-force-a
                await Task.Delay(500);

                // Wrap the actual authentication in a timeout guard to avoid infinite spinners on dead DB connections.
                var authTask = _authService.AuthenticateAsync(Username, Password);
                var completed = await Task.WhenAny(authTask, Task.Delay(LoginTimeout));

                if (completed != authTask)
                {
                    // Timed out
                    FailedAttempts++;
                    HasError = true;
                    IsAuthenticated = false;
                    StatusMessage = "Povezivanje s bazom je isteklo. Provjerite AppSettings.json i mrežu pa pokušajte ponovno.";
                    LastLogins.Insert(0, $"{DateTime.Now:G} - Neuspješna prijava (timeout) ({DeviceInfo})");
                    return;
                }

                var user = await authTask;

                if (user != null)
                {
                    FailedAttempts = 0;
                    IsAuthenticated = true;
                    LoggedInUser = user;
                    StatusMessage = $"Dobrodošli, {user.FullName}!";

                    LastLogins.Insert(0, $"{DateTime.Now:G} - Prijava uspješna ({DeviceInfo})");

                    if (user.IsTwoFactorEnabled)
                    {
                        IsTwoFactorRequired = true;
                        StatusMessage = "Unesite 2FA kod iz aplikacije/tokena.";
                        return;
                    }

                    // ⛳️ Switch the app to Shell root and navigate to dashboard.
                    await SwitchToShellDashboardAsync();
                }
                else
                {
                    FailedAttempts++;
                    HasError = true;
                    IsAuthenticated = false;
                    StatusMessage = $"Pogrešno korisničko ime ili lozinka (pokušaj #{FailedAttempts})";
                    LastLogins.Insert(0, $"{DateTime.Now:G} - Neuspješna prijava ({DeviceInfo})");
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                IsAuthenticated = false;
                StatusMessage = $"Greška prilikom prijave: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                RefreshCommandStates();
            }
        }

        /// <summary>Potvrđuje 2FA kod i dovršava login.</summary>
        private async Task SubmitTwoFactorAsync()
        {
            if (!IsTwoFactorRequired)
                return;

            if (string.IsNullOrWhiteSpace(TwoFactorCode))
            {
                StatusMessage = "Unesite 2FA kod.";
                return;
            }

            IsBusy = true;
            try
            {
                var valid = await _authService.VerifyTwoFactorCodeAsync(Username, TwoFactorCode);
                if (valid)
                {
                    IsTwoFactorRequired = false;
                    StatusMessage = "2FA uspješan! Pristup dozvoljen.";

                    UpdateAppLoggedUser();

                    // ⛳️ Same Shell switch as above.
                    await SwitchToShellDashboardAsync();
                }
                else
                {
                    HasError = true;
                    StatusMessage = "Neispravan 2FA kod.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Greška kod provjere 2FA: {ex.Message}";
                HasError = true;
            }
            finally
            {
                IsBusy = false;
                RefreshCommandStates();
            }
        }

        /// <summary>
        /// Centralized, UI-thread-safe switch to <see cref="AppShell"/> as the root and navigation
        /// to the dashboard tab. Uses the absolute Shell route (<c>//root/home/dashboard</c>) so the
        /// stack is clean and <see cref="Shell.Current"/> is guaranteed non-null afterwards.
        /// </summary>
        private async Task SwitchToShellDashboardAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                UpdateAppLoggedUser();
                Application.Current!.MainPage = new AppShell();
                if (Shell.Current is not null)
                {
                    await Shell.Current.GoToAsync("//root/home/dashboard");
                }
            });
        }

        /// <summary>Osvježava <see cref="LoggedInUser"/> i <see cref="App.LoggedUser"/> prema <see cref="AuthService.CurrentUser"/>.</summary>
        private void UpdateAppLoggedUser()
        {
            var current = _authService.CurrentUser ?? LoggedInUser;
            if (current != null)
            {
                LoggedInUser = current;
            }

            if (Application.Current is App app)
            {
                app.LoggedUser = current;
            }
        }

        /// <summary>Promjena jezika sučelja.</summary>
        private void SwitchLanguage(string lang)
        {
            if (!string.IsNullOrWhiteSpace(lang))
                SelectedLanguage = lang;
        }

        // Placeholderi/poruke (svjesno bez implementacije, ali ostavljaju trag za UI)
        private void ShowForgotPasswordDialog()  => StatusMessage = "Reset lozinke nije još implementiran.";
        private void ExecutePinLogin()           => StatusMessage = "PIN prijava nije još implementirana.";
        private void ExecuteBiometricLogin()     => StatusMessage = "Biometrijska prijava nije još implementirana.";
        private void ExecuteSmartcardLogin()     => StatusMessage = "Smartcard prijava nije još implementirana.";
        private void ExecuteSsoLogin()           => StatusMessage = "SSO prijava nije još implementirana.";
        private void ExecuteQrLogin()            => StatusMessage = "QR prijava nije još implementirana.";
        private void ShowSupportDialog()         => StatusMessage = "Za podršku kontaktirajte IT odjel: support@yasenka.hr";
        private void ShowPrivacyDialog()         => StatusMessage = "Vaša privatnost je zaštićena prema GMP standardima.";
        private void ShowAuditModal()            => StatusMessage = "Audit log nije još implementiran.";

        /// <summary>Učitava (resetira) lokalnu listu povijesti prijava.</summary>
        private async Task LoadLoginHistoryAsync()
        {
            LastLogins.Clear();
            await Task.CompletedTask;
        }

        /// <summary>Prikuplja kratke informacije o uređaju (OS/host/korisnik).</summary>
        private string CollectDeviceInfo()
        {
            try
            {
                string os = RuntimeInformation.OSDescription;
                string host = Environment.MachineName;
                string user = Environment.UserName;
                return $"OS: {os}, Host: {host}, User: {user}";
            }
            catch
            {
                return "DeviceInfo: N/A";
            }
        }

        /// <summary>Vraća javnu IP adresu putem jednostavne HTTP usluge.</summary>
        private async Task<string> GetPublicIpAddressAsync()
        {
            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var ip = await httpClient.GetStringAsync("https://api.ipify.org");
                return ip?.Trim() ?? "IP: N/A";
            }
            catch
            {
                return "IP: N/A";
            }
        }

        #endregion

        #region INotifyPropertyChanged

        /// <inheritdoc/>
        public new event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Sigurno podiže <see cref="PropertyChanged"/> (nullable-friendly).</summary>
        protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));

        #endregion

        #region Platform Helpers

        /// <summary>Detekcija CapsLock-a (Windows samo).</summary>
        public void DetectCapsLock()
        {
#if WINDOWS
            IsCapsLockOn = WindowsKeyboardHelper.IsCapsLockOn();
#else
            IsCapsLockOn = false;
#endif
        }

        #endregion
    }

#if WINDOWS
    /// <summary>Platform-specific helper za Windows CapsLock detekciju.</summary>
    public static class WindowsKeyboardHelper
    {
        [DllImport("user32.dll")]
        private static extern short GetKeyState(int key);

        /// <summary>Vraća stanje CapsLock tipke (true ako je uključena).</summary>
        public static bool IsCapsLockOn() => (((ushort)GetKeyState(0x14)) & 0xffff) != 0;
    }
#endif
}
