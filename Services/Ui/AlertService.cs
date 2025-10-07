using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace YasGMP.Services.Ui
{
    /// <summary>
    /// Abstraction for prompting the user with alerts and confirmation dialogs on the UI thread.
    /// </summary>
    public interface IAlertService
    {
        Task AlertAsync(string title, string message, string cancel = "OK");
        Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel");
    }

    /// <summary>
    /// Default implementation that routes alert and confirmation requests through MAUI's MainPage.
    /// </summary>
    public sealed class AlertService : IAlertService
    {
        /// <summary>
        /// Executes the alert async operation.
        /// </summary>
        public async Task AlertAsync(string title, string message, string cancel = "OK")
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = Application.Current?.MainPage;
                if (page != null)
                {
                    await page.DisplayAlert(title, message, cancel);
                }
            }).ConfigureAwait(false);
        }
        /// <summary>
        /// Executes the confirm async operation.
        /// </summary>

        public async Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel")
        {
            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = Application.Current?.MainPage;
                if (page == null)
                {
                    return false;
                }

                return await page.DisplayAlert(title, message, accept, cancel);
            }).ConfigureAwait(false);
        }
    }
}
