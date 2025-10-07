using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Bridges MAUI's <see cref="IDialogService"/> abstractions to simple WPF <see cref="MessageBox"/>
    /// prompts so shell modules retain baseline confirmation/alert parity when hosted on desktop.
    /// </summary>
    /// <remarks>
    /// <para><strong>Supported MAUI APIs:</strong> <see cref="ShowAlertAsync"/> and
    /// <see cref="ShowConfirmationAsync"/> translate directly to information/question message boxes and
    /// therefore keep existing MAUI flows working without code changes.</para>
    /// <para><strong>Unsupported APIs:</strong> <see cref="ShowActionSheetAsync"/> and
    /// <see cref="ShowDialogAsync{T}(string, object?, CancellationToken)"/> do not have WPF shell
    /// equivalents yet. Callers migrating from MAUI must replace action sheets with dedicated ribbon
    /// commands, docked panes, or module views, and throw or short-circuit optional workflows until a
    /// desktop UX is defined.</para>
    /// <para><strong>Localization:</strong> the service does not look up resources on its own; callers
    /// are responsible for passing fully localized titles/buttons sourced from the shared localization
    /// dictionaries (RESX, <c>ShellStrings</c>, etc.) so WPF mirrors MAUI translations.</para>
    /// <para><strong>Migration checklist:</strong> audit MAUI usages of <see cref="IDialogService"/> for
    /// custom buttons or complex payloads, guard unsupported calls with platform checks, and document
    /// TODOs to avoid silent regressions during the MAUI → WPF transition.</para>
    /// </remarks>
    public sealed class WpfDialogService : IDialogService
    {
        /// <summary>
        /// Executes the show alert async operation.
        /// </summary>
        public Task ShowAlertAsync(string title, string message, string cancel)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Executes the show confirmation async operation.
        /// </summary>

        public Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return Task.FromResult(result == MessageBoxResult.Yes);
        }
        /// <summary>
        /// Executes the show action sheet async operation.
        /// </summary>

        public Task<string?> ShowActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
        {
            // Not supported in the WPF shell; return null.
            return Task.FromResult<string?>(null);
        }
        /// <summary>
        /// Executes the show dialog async operation.
        /// </summary>

        public Task<T?> ShowDialogAsync<T>(string dialogId, object? parameter = null, CancellationToken cancellationToken = default)
        {
            // The WPF shell uses dedicated modules for editing; modal dialogs are not yet implemented.
            throw new NotSupportedException($"Dialog '{dialogId}' is not available in the WPF shell.");
        }
    }
}
