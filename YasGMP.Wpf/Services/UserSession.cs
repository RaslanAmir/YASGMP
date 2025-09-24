using System;
using Microsoft.Extensions.Configuration;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>Simple WPF shell user session implementing the shared <see cref="IUserSession"/> contract.</summary>
    public sealed class UserSession : IUserSession
    {
        public UserSession(IConfiguration configuration)
        {
            UserId = configuration.GetValue<int?>("Shell:UserId") ?? 1;
            Username = configuration["Shell:Username"] ?? "wpf-shell";
            SessionId = Guid.NewGuid().ToString("N");
        }

        public User? CurrentUser => null;

        public int? UserId { get; }

        public string? Username { get; }

        public string? FullName => Username;

        public string SessionId { get; }
    }
}
