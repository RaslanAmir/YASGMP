using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using YasGMP.Services;

namespace YasGMP.Wpf.Services
{
    /// <summary>Minimal WPF dialog service using <see cref="MessageBox"/>.</summary>
    public sealed class WpfDialogService : IDialogService
    {
        public Task ShowAlertAsync(string title, string message, string cancel)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }

        public Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return Task.FromResult(result == MessageBoxResult.Yes);
        }

        public Task<string?> ShowActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
        {
            // Not supported in the WPF shell; return null.
            return Task.FromResult<string?>(null);
        }

        public Task<T?> ShowDialogAsync<T>(string dialogId, object? parameter = null, CancellationToken cancellationToken = default)
        {
            // The WPF shell uses dedicated modules for editing; modal dialogs are not yet implemented.
            throw new NotSupportedException($"Dialog '{dialogId}' is not available in the WPF shell.");
        }
    }
}

