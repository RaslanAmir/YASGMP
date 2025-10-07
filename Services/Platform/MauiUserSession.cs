using System;
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services.Platform
{
    /// <summary>Bridges MAUI application session state with <see cref="IUserSession"/>.</summary>
    public sealed class MauiUserSession : IUserSession
    {
        private readonly IAuthContext _authContext;
        /// <summary>
        /// Initializes a new instance of the MauiUserSession class.
        /// </summary>

        public MauiUserSession(IAuthContext authContext)
        {
            _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        }

        private static App? TryGetApp() => Application.Current as App;
        /// <summary>
        /// Executes the current user operation.
        /// </summary>

        public User? CurrentUser => _authContext.CurrentUser ?? TryGetApp()?.LoggedUser;
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>

        public int? UserId => CurrentUser?.Id;
        /// <summary>
        /// Gets or sets the username.
        /// </summary>

        public string? Username => CurrentUser?.Username;
        /// <summary>
        /// Gets or sets the full name.
        /// </summary>

        public string? FullName => CurrentUser?.FullName ?? CurrentUser?.Username;
        /// <summary>
        /// Executes the session id operation.
        /// </summary>

        public string SessionId => _authContext.CurrentSessionId ?? TryGetApp()?.SessionId ?? string.Empty;
    }
}
