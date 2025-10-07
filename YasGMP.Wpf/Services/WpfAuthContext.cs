using System;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Minimal authentication context for the WPF shell, providing deterministic
    /// user/session/device metadata for the shared AppCore services.
    /// </summary>
    public sealed class WpfAuthContext : IAuthContext
    {
        private readonly IUserSession _session;
        private readonly IPlatformService _platform;
        private readonly Lazy<User?> _user;
        private readonly string _sessionId = Guid.NewGuid().ToString("N");
        /// <summary>
        /// Initializes a new instance of the WpfAuthContext class.
        /// </summary>

        public WpfAuthContext(IUserSession session, IPlatformService platform)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
            _user = new Lazy<User?>(() => new User
            {
                Id = _session.UserId,
                Username = _session.Username
            });
        }
        /// <summary>
        /// Gets or sets the current user.
        /// </summary>

        public User? CurrentUser => _user.Value;
        /// <summary>
        /// Gets or sets the current session id.
        /// </summary>

        public string CurrentSessionId => _sessionId;
        /// <summary>
        /// Gets or sets the current device info.
        /// </summary>

        public string CurrentDeviceInfo =>
            $"OS={_platform.GetOsVersion()};Host={_platform.GetHostName()};User={_platform.GetUserName()}";
        /// <summary>
        /// Executes the current ip address operation.
        /// </summary>

        public string CurrentIpAddress => _platform.GetLocalIpAddress();
    }
}
