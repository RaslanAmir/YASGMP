using Microsoft.Extensions.Configuration;

namespace YasGMP.Wpf.Services
{
    /// <summary>Lightweight user context used for per-user layout persistence.</summary>
    public interface IUserSession
    {
        int UserId { get; }
        string Username { get; }
    }

    public sealed class UserSession : IUserSession
    {
        public UserSession(IConfiguration configuration)
        {
            UserId = configuration.GetValue<int?>("Shell:UserId") ?? 1;
            Username = configuration["Shell:Username"] ?? "wpf-shell";
        }

        public int UserId { get; }

        public string Username { get; }
    }
}
