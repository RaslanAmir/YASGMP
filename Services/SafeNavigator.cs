using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>SafeNavigator</b> – Centralized, exception-safe navigation + alerts for Shell-based apps.
    /// UI-thread marshalled calls to avoid COM exceptions on WinUI. All APIs are no-throw.
    /// </summary>
    public static class SafeNavigator
    {
        /// <summary>
        /// Navigate to a Shell route in a UI-thread-safe manner.
        /// </summary>
        public static async Task<bool> GoToAsync(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;

            try
            {
                AppShell.EnsureRoutesRegistered();

                if (Shell.Current is null)
                {
                    await TryShowAlertAsync("Navigation", $"Shell nije aktivan. Ruta: {route}", "OK").ConfigureAwait(false);
                    return false;
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.GoToAsync(route).ConfigureAwait(false);
                }).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[NAV ERROR] GoToAsync('{route}') → {ex}");
#endif
                await TryShowAlertAsync("Greška navigacije", ex.Message, "OK").ConfigureAwait(false);
                return false;
            }
        }

        /// <summary>
        /// Navigate to a route with parameters (Shell query).
        /// </summary>
        public static async Task<bool> GoToAsync(string route, IDictionary<string, object>? parameters)
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;

            try
            {
                AppShell.EnsureRoutesRegistered();

                if (Shell.Current is null)
                {
                    await TryShowAlertAsync("Navigation", $"Shell nije aktivan. Ruta: {route}", "OK").ConfigureAwait(false);
                    return false;
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (parameters is null || parameters.Count == 0)
                        await Shell.Current.GoToAsync(route).ConfigureAwait(false);
                    else
                        await Shell.Current.GoToAsync(route, parameters).ConfigureAwait(false);
                }).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[NAV ERROR] GoToAsync('{route}', params) → {ex}");
#endif
                await TryShowAlertAsync("Greška navigacije", ex.Message, "OK").ConfigureAwait(false);
                return false;
            }
        }

        /// <summary>
        /// Navigate back using Shell ("..").
        /// </summary>
        public static async Task<bool> GoBackAsync()
        {
            try
            {
                if (Shell.Current is null)
                {
                    await TryShowAlertAsync("Navigation", "Nije moguće vratiti se — Shell nije aktivan.", "OK")
                        .ConfigureAwait(false);
                    return false;
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.GoToAsync("..").ConfigureAwait(false);
                }).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[NAV ERROR] GoBackAsync() → {ex}");
#endif
                await TryShowAlertAsync("Greška navigacije", ex.Message, "OK").ConfigureAwait(false);
                return false;
            }
        }

        /// <summary>Show a simple alert (no-throw).</summary>
        public static async Task ShowAlertAsync(string title, string message, string cancel = "OK")
            => await TryShowAlertAsync(title, message, cancel).ConfigureAwait(false);

        /// <summary>Confirmation dialog (no-throw).</summary>
        public static async Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel")
        {
            try
            {
                return await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var page = Application.Current?.MainPage;
                    if (page is null) return false;
                    return await page.DisplayAlert(title, message, accept, cancel).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[NAV ERROR] ConfirmAsync('{title}') → {ex}");
#endif
                return false;
            }
        }

        /// <summary>
        /// Displays an action sheet in a UI-thread-safe manner.
        /// Returns the selected option or <c>null</c> if cancelled/failed.
        /// </summary>
        public static async Task<string?> ActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
        {
            try
            {
                return await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var page = Application.Current?.MainPage;
                    if (page is null)
                        return null;

                    return await page.DisplayActionSheet(title, cancel, destruction, buttons).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[NAV WARN] ActionSheetAsync('{title}') → {ex.Message}");
#endif
                return null;
            }
        }

        private static async Task TryShowAlertAsync(string title, string message, string cancel)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var page = Application.Current?.MainPage;
                    if (page != null)
                        await page.DisplayAlert(title, message, cancel).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"[NAV WARN] TryShowAlertAsync('{title}') → {ex.Message}");
#endif
                // swallow
            }
        }
    }
}
