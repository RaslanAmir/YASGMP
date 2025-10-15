using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Provides an abstraction for credential verification so view-models remain unit testable
    /// while delegating to the shared <see cref="Services.AuthService"/> implementation at runtime.
    /// </summary>
    public interface IAuthenticator
    {
        /// <summary>Attempts to authenticate the supplied credentials.</summary>
        Task<User?> AuthenticateAsync(string username, string password);

        /// <summary>Currently authenticated user (if any) following the last invocation.</summary>
        User? CurrentUser { get; }

        /// <summary>Logical session identifier returned by the underlying authentication service.</summary>
        string? CurrentSessionId { get; }
    }
}
