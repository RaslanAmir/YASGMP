using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>Persists shell notification preferences to the shared settings catalog.</summary>
    public sealed class NotificationPreferenceService : INotificationPreferenceService
    {
        private const string StatusBarKey = "notifications.ui.statusBar";
        private const string ToastKey = "notifications.ui.toast";

        private readonly DatabaseService _database;
        private readonly IUserSession _session;
        private readonly IPlatformService _platform;

        private NotificationPreferences _current;

        public NotificationPreferenceService(DatabaseService database, IUserSession session, IPlatformService platform)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));

            _current = NotificationPreferences.CreateDefault();
        }

        public event EventHandler<NotificationPreferences>? PreferencesChanged;

        public NotificationPreferences Current => _current.Clone();

        public async Task<NotificationPreferences> ReloadAsync(CancellationToken token = default)
        {
            var preferences = await LoadAsync(token).ConfigureAwait(false);
            _current = preferences;
            PreferencesChanged?.Invoke(this, _current.Clone());
            return _current.Clone();
        }

        public async Task SaveAsync(NotificationPreferences preferences, CancellationToken token = default)
        {
            if (preferences is null)
            {
                throw new ArgumentNullException(nameof(preferences));
            }

            await PersistAsync(StatusBarKey, preferences.ShowStatusBarAlerts, token).ConfigureAwait(false);
            await PersistAsync(ToastKey, preferences.ShowToastAlerts, token).ConfigureAwait(false);

            _current = preferences.Clone();
            PreferencesChanged?.Invoke(this, _current.Clone());
        }

        private async Task<NotificationPreferences> LoadAsync(CancellationToken token)
        {
            var settings = await _database.GetAllSettingsFullAsync(token).ConfigureAwait(false);

            static bool ResolveBool(string? value, bool fallback)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return fallback;
                }

                if (bool.TryParse(value, out var parsed))
                {
                    return parsed;
                }

                return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "da", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "si", StringComparison.OrdinalIgnoreCase);
            }

            bool TryGet(string key, bool fallback)
            {
                foreach (var setting in settings)
                {
                    if (string.Equals(setting.Key, key, StringComparison.OrdinalIgnoreCase))
                    {
                        return ResolveBool(setting.Value, fallback);
                    }
                }

                return fallback;
            }

            return new NotificationPreferences
            {
                ShowStatusBarAlerts = TryGet(StatusBarKey, _current.ShowStatusBarAlerts),
                ShowToastAlerts = TryGet(ToastKey, _current.ShowToastAlerts)
            };
        }

        private async Task PersistAsync(string key, bool value, CancellationToken token)
        {
            var actorUserId = _session.UserId ?? 0;
            var ip = _platform.GetLocalIpAddress() ?? string.Empty;
            var device = _platform.GetHostName() ?? Environment.MachineName;
            var sessionId = _session.SessionId ?? string.Empty;

            var setting = new Setting
            {
                Key = key,
                Value = value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant(),
                ValueType = "bool",
                Description = key == StatusBarKey
                    ? "Toggle whether status bar messages mirror toast notifications."
                    : "Toggle transient toast notifications in the WPF shell.",
                Category = "notifications",
                Subcategory = "ui",
                Status = "active",
            };

            await _database.UpsertSettingAsync(setting, actorUserId, ip, device, sessionId, token).ConfigureAwait(false);
        }
    }
}
