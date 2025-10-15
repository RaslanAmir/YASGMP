using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels
{
    /// <summary>
    /// View-model backing the WPF login dialog. Delegates credential checks to <see cref="AuthService"/>
    /// via the injected <see cref="IAuthenticator"/> and promotes the authenticated user into the
    /// shared <see cref="UserSession"/> used by the shell.
    /// </summary>
    public sealed partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthenticator _authenticator;
        private readonly UserSession _userSession;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginViewModel"/> class.
        /// </summary>
        public LoginViewModel(IAuthenticator authenticator, UserSession userSession)
        {
            _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));

            Username = userSession.Username ?? string.Empty;

            LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
            CancelCommand = new RelayCommand(Cancel);
        }

        /// <summary>Raised when the dialog should close. Payload indicates success (<c>true</c>) or cancellation.</summary>
        public event EventHandler<bool>? RequestClose;

        /// <summary>Command executed when the operator confirms credentials.</summary>
        public IAsyncRelayCommand LoginCommand { get; }

        /// <summary>Command executed when the operator cancels the dialog.</summary>
        public IRelayCommand CancelCommand { get; }

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string? _errorMessage;

        partial void OnUsernameChanged(string value) => LoginCommand.NotifyCanExecuteChanged();

        partial void OnPasswordChanged(string value) => LoginCommand.NotifyCanExecuteChanged();

        partial void OnIsBusyChanged(bool value) => LoginCommand.NotifyCanExecuteChanged();

        private bool CanLogin()
            => !IsBusy && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

        private async Task LoginAsync()
        {
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var user = await _authenticator.AuthenticateAsync(Username.Trim(), Password).ConfigureAwait(true);
                if (user is null)
                {
                    ErrorMessage = "Invalid username or password.";
                    return;
                }

                _userSession.ApplyAuthenticatedUser(user, _authenticator.CurrentSessionId);

                RequestClose?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Authentication failed: {ex.Message}";
            }
            finally
            {
                Password = string.Empty;
                IsBusy = false;
            }
        }

        private void Cancel()
        {
            ErrorMessage = null;
            Password = string.Empty;
            RequestClose?.Invoke(this, false);
        }

        /// <summary>Helper used by the view to update the password field when binding to <see cref="PasswordBox"/>.</summary>
        public void SetPassword(string value)
        {
            Password = value ?? string.Empty;
        }
    }
}
