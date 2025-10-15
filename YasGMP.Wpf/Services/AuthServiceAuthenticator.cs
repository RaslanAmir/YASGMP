using System;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Default <see cref="IAuthenticator"/> implementation that delegates credential checks to the
    /// shared <see cref="AuthService"/> used across MAUI and WPF shells.
    /// </summary>
    public sealed class AuthServiceAuthenticator : IAuthenticator
    {
        private readonly AuthService _authService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthServiceAuthenticator"/> class.
        /// </summary>
        public AuthServiceAuthenticator(AuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        /// <inheritdoc />
        public User? CurrentUser => _authService.CurrentUser;

        /// <inheritdoc />
        public string? CurrentSessionId => _authService.CurrentSessionId;

        /// <inheritdoc />
        public Task<User?> AuthenticateAsync(string username, string password)
            => _authService.AuthenticateAsync(username, password);
    }
}
