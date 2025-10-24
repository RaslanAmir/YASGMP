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

        public MauiUserSession(IAuthContext authContext)
        {
            _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        }

        private static App? TryGetApp() => Application.Current as App;

        public User? CurrentUser => _authContext.CurrentUser ?? TryGetApp()?.LoggedUser;

        public int? UserId => CurrentUser?.Id;

        public string? Username => CurrentUser?.Username;

        public string? FullName => CurrentUser?.FullName ?? CurrentUser?.Username;

        public string SessionId => _authContext.CurrentSessionId ?? TryGetApp()?.SessionId ?? string.Empty;
    }
}
