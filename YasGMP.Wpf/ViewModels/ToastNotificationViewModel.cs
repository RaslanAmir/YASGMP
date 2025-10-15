using System;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels
{
    /// <summary>View-model representing a transient toast notification displayed in the shell overlay.</summary>
    public sealed partial class ToastNotificationViewModel : ObservableObject, IDisposable
    {
        private readonly DispatcherTimer _timer;
        private readonly Action<ToastNotificationViewModel> _onDismissed;
        private readonly EventHandler _tickHandler;

        public ToastNotificationViewModel(
            string message,
            AlertSeverity severity,
            TimeSpan lifetime,
            Action<ToastNotificationViewModel> onDismissed)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Severity = severity;
            _onDismissed = onDismissed ?? throw new ArgumentNullException(nameof(onDismissed));

            _timer = new DispatcherTimer
            {
                Interval = lifetime <= TimeSpan.Zero ? TimeSpan.FromSeconds(5) : lifetime
            };
            _tickHandler = (_, _) => Dismiss();
            _timer.Tick += _tickHandler;
            _timer.Start();

            DismissCommand = new RelayCommand(Dismiss);
        }

        /// <summary>Gets the toast message displayed to the operator.</summary>
        public string Message { get; }

        /// <summary>Gets the severity classification used to theme the toast.</summary>
        public AlertSeverity Severity { get; }

        /// <summary>Command allowing the operator to dismiss the toast immediately.</summary>
        public IRelayCommand DismissCommand { get; }

        /// <summary>Gets an icon glyph (Segoe MDL2 Assets) that matches the severity.</summary>
        public string IconGlyph => Severity switch
        {
            AlertSeverity.Success => "\uE930",
            AlertSeverity.Warning => "\uE7BA",
            AlertSeverity.Error => "\uEA39",
            _ => "\uE946",
        };

        public void Dismiss()
        {
            _timer.Stop();
            _onDismissed(this);
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Tick -= _tickHandler;
        }
    }
}
