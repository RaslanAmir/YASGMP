using System;
using Microsoft.Extensions.Configuration;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>Simple WPF shell user session implementing the shared <see cref="IUserSession"/> contract.</summary>
    public sealed class UserSession : IUserSession
    {
        /// <summary>
        /// Initializes a new instance of the UserSession class.
        /// </summary>
        public UserSession(IConfiguration configuration)
        {
            UserId = configuration.GetValue<int?>("Shell:UserId") ?? 1;
            Username = configuration["Shell:Username"] ?? "wpf-shell";
            SessionId = Guid.NewGuid().ToString("N");
        }
        /// <summary>
        /// Gets or sets the current user.
        /// </summary>

        public User? CurrentUser => null;
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>

        public int? UserId { get; }
        /// <summary>
        /// Gets or sets the username.
        /// </summary>

        public string? Username { get; }
        /// <summary>
        /// Gets or sets the full name.
        /// </summary>

        public string? FullName => Username;
        /// <summary>
        /// Gets or sets the session id.
        /// </summary>

        public string SessionId { get; }
    }
}
