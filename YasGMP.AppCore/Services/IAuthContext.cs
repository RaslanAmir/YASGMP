using YasGMP.Models;

namespace YasGMP.Services.Interfaces
{
    /// <summary>
    /// Minimal authentication context abstraction that exposes the currently authenticated user
    /// and associated forensic/session metadata required by audit logging.
    /// </summary>
    public interface IAuthContext
    {
        /// <summary>Currently authenticated user (or <c>null</c> if no session exists).</summary>
        User? CurrentUser { get; }

        /// <summary>Opaque identifier that ties audit events to the logical user session.</summary>
        string CurrentSessionId { get; }

        /// <summary>Descriptive device fingerprint captured at sign-in time.</summary>
        string CurrentDeviceInfo { get; }

        /// <summary>Best-effort IP address associated with the current session.</summary>
        string CurrentIpAddress { get; }
    }
}
