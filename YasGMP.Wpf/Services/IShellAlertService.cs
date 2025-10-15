using System.Collections.ObjectModel;
using YasGMP.Services.Ui;
using YasGMP.Wpf.ViewModels;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// WPF-specific alert surface that mirrors MAUI alerts into status bar updates and toast notifications.
    /// </summary>
    public interface IShellAlertService : IAlertService
    {
        /// <summary>Gets the live toast collection rendered by the shell.</summary>
        ReadOnlyObservableCollection<ToastNotificationViewModel> Toasts { get; }

        /// <summary>
        /// Publishes a status message to configured notification surfaces.
        /// </summary>
        /// <param name="message">Localized message to display.</param>
        /// <param name="severity">Severity used for theming the toast surface.</param>
        /// <param name="propagateToStatusBar">
        /// When <c>true</c> the status bar will be updated regardless of operator preferences.
        /// </param>
        void PublishStatus(string message, AlertSeverity severity = AlertSeverity.Info, bool propagateToStatusBar = false);
    }
}
