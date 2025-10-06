using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Exposes information about the interactive user session for UI-facing components.
    /// </summary>
    public interface IUserSession
    {
        /// <summary>Currently authenticated user, if available.</summary>
        User? CurrentUser { get; }

        /// <summary>Convenience accessor for the current user's identifier.</summary>
        int? UserId { get; }

        /// <summary>Convenience accessor for the username/login.</summary>
        string? Username { get; }

        /// <summary>Convenience accessor for the display name (full name preferred).</summary>
        string? FullName { get; }

        /// <summary>Opaque identifier for the logical session (shared with audit logging).</summary>
        string SessionId { get; }
    }
}

