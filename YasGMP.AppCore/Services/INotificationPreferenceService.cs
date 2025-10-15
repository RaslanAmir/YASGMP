using System;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;

namespace YasGMP.Services
{
    /// <summary>
    /// Contract for loading and persisting operator notification preferences (toast/status bar).
    /// </summary>
    public interface INotificationPreferenceService
    {
        /// <summary>Raised when preferences are reloaded or saved.</summary>
        event EventHandler<NotificationPreferences>? PreferencesChanged;

        /// <summary>Gets the most recently loaded preference snapshot.</summary>
        NotificationPreferences Current { get; }

        /// <summary>Reloads preferences from the backing store.</summary>
        Task<NotificationPreferences> ReloadAsync(CancellationToken token = default);

        /// <summary>Persists the supplied preferences to the backing store.</summary>
        Task SaveAsync(NotificationPreferences preferences, CancellationToken token = default);
    }
}
