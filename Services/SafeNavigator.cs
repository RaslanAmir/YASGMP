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
    /// <para>
    /// Guarantees UI-thread execution for navigation and dialogs, registers routes once via
    /// <see cref="AppShell.EnsureRoutesRegistered"/>, and never throws UI-thread COM exceptions.
    /// </para>
    /// <para>
    /// All methods return a boolean success flag (where meaningful) and surface user-friendly messages
    /// instead of crashing. Errors are also written to <see cref="Debug"/> in <c>DEBUG</c> builds.
    /// </para>
    /// </summary>
    public static class SafeNavigator
    {
        /// <summary>
        /// Navigates to a registered Shell route in a fault-tolerant, UI-thread-safe way.
        /// </summary>
        /// <param name="route">Shell route (e.g., <c>"routes/dashboard"</c>).</param>
        /// <returns><c>true</c> if navigation started; otherwise <c>false</c>.</returns>
        public static async Task<bool> GoToAsync(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;

            try
            {
                // Ensure known routes are registered (idempotent).
                AppShell.EnsureRoutesRegistered();

                if (Shell.Current is null)
                {
                    await TryShowAlertAsync("Navigation",
                        $"Shell nije aktivan. Ruta: {route}", "OK").ConfigureAwait(false);
                    return false;
                }

                // Always marshal navigation onto the UI thread to avoid COM/XAML dialog issues.
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
        /// Navigates to a route with query dictionary (Shell navigation parameters), UI-thread safe.
        /// </summary>
        /// <param name="route">Shell route (e.g., <c>"routes/machines"</c>).</param>
        /// <param name="parameters">Key/value parameters to pass to the target page.</param>
        /// <returns><c>true</c> on success; otherwise <c>false</c>.</returns>
        public static async Task<bool> GoToAsync(string route, IDictionary<string, object>? parameters)
        {
            if (string.IsNullOrWhiteSpace(route))
                return false;

            try
            {
                AppShell.EnsureRoutesRegistered();

                if (Shell.Current is null)
                {
                    await TryShowAlertAsync("Navigation",
                        $"Shell nije aktivan. Ruta: {route}", "OK").ConfigureAwait(false);
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
        /// Navigates back using Shell, UI-thread safe.
        /// </summary>
        /// <returns><c>true</c> if a back navigation was attempted; <c>false</c> if not available.</returns>
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

        /// <summary>
        /// Shows a simple alert in a UI-thread safe manner. No-throw.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="message">Message body.</param>
        /// <param name="cancel">Cancel button text (default <c>OK</c>).</param>
        public static async Task ShowAlertAsync(string title, string message, string cancel = "OK")
            => await TryShowAlertAsync(title, message, cancel).ConfigureAwait(false);

        /// <summary>
        /// Shows a confirmation dialog (OK/Cancel) in a UI-thread safe manner. No-throw.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="message">Message body.</param>
        /// <param name="accept">Accept button text.</param>
        /// <param name="cancel">Cancel button text.</param>
        /// <returns><c>true</c> if accepted; otherwise <c>false</c> (or if dialog unavailable).</returns>
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
        /// Attempts to display an alert without throwing. Always marshals to the UI thread.
        /// </summary>
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
                // Swallow alert errors – never let dialogs crash the app.
            }
        }
    }
}
