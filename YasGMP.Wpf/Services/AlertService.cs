using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Ui;
using YasGMP.Wpf.ViewModels;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Bridges MAUI-style alerts to the WPF shell by projecting messages into the status bar and toast overlay.
    /// </summary>
    public sealed class AlertService : IShellAlertService
    {
        private const int MaxToastCount = 4;
        private static readonly TimeSpan ToastLifetime = TimeSpan.FromSeconds(6);

        private readonly IShellInteractionService _shellInteraction;
        private readonly INotificationPreferenceService _preferenceService;
        private readonly IUiDispatcher _dispatcher;
        private readonly ObservableCollection<ToastNotificationViewModel> _toasts;
        private readonly ReadOnlyObservableCollection<ToastNotificationViewModel> _readonlyToasts;

        private NotificationPreferences _preferences;

        public AlertService(
            IShellInteractionService shellInteraction,
            INotificationPreferenceService preferenceService,
            IUiDispatcher dispatcher)
        {
            _shellInteraction = shellInteraction ?? throw new ArgumentNullException(nameof(shellInteraction));
            _preferenceService = preferenceService ?? throw new ArgumentNullException(nameof(preferenceService));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            _preferences = preferenceService.Current.Clone();
            _preferenceService.PreferencesChanged += OnPreferencesChanged;

            _toasts = new ObservableCollection<ToastNotificationViewModel>();
            _readonlyToasts = new ReadOnlyObservableCollection<ToastNotificationViewModel>(_toasts);

            // Kick off an asynchronous refresh so persisted preferences flow in after DI boot.
            _ = _preferenceService.ReloadAsync();
        }

        public ReadOnlyObservableCollection<ToastNotificationViewModel> Toasts => _readonlyToasts;

        public async Task AlertAsync(string title, string message, string cancel = "OK")
        {
            if (title is null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var composed = string.IsNullOrWhiteSpace(title)
                ? message
                : $"{title}: {message}";

            await _dispatcher.InvokeAsync(() => PublishStatusInternal(composed, AlertSeverity.Info, propagateToStatusBar: true)).ConfigureAwait(false);
        }

        public Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel")
        {
            if (title is null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return _dispatcher.InvokeAsync(() =>
            {
                var result = MessageBox.Show(
                    message,
                    string.IsNullOrWhiteSpace(title) ? "YasGMP" : title,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                return result == MessageBoxResult.Yes;
            });
        }

        public void PublishStatus(string message, AlertSeverity severity = AlertSeverity.Info, bool propagateToStatusBar = false)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            _ = _dispatcher.InvokeAsync(() => PublishStatusInternal(message, severity, propagateToStatusBar));
        }

        private void PublishStatusInternal(string message, AlertSeverity severity, bool propagateToStatusBar)
        {
            if (_preferences.ShowStatusBarAlerts || propagateToStatusBar)
            {
                _shellInteraction.UpdateStatus(message);
            }

            if (!_preferences.ShowToastAlerts)
            {
                return;
            }

            if (_toasts.Count >= MaxToastCount)
            {
                var oldest = _toasts.FirstOrDefault();
                oldest?.Dispose();
                if (oldest is not null)
                {
                    _toasts.Remove(oldest);
                }
            }

            void RemoveToast(ToastNotificationViewModel toast)
            {
                _dispatcher.BeginInvoke(() =>
                {
                    toast.Dispose();
                    _toasts.Remove(toast);
                });
            }

            var viewModel = new ToastNotificationViewModel(message, severity, ToastLifetime, RemoveToast);
            _toasts.Add(viewModel);
        }

        private void OnPreferencesChanged(object? sender, NotificationPreferences preferences)
        {
            if (preferences is null)
            {
                return;
            }

            _preferences = preferences.Clone();

            if (!_preferences.ShowToastAlerts)
            {
                foreach (var toast in _toasts.ToList())
                {
                    toast.Dispose();
                }

                _toasts.Clear();
            }
        }
    }
}
