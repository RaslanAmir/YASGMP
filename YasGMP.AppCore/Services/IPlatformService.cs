using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace YasGMP.Services
{
    /// <summary>
    /// <b>IPlatformService</b> – Platform-specific accessors for device/system data used in forensic audit.
    /// Implement per platform (Windows, Android, iOS, MacCatalyst) as you prefer; all extended members below
    /// have safe default implementations so existing platform heads do not need changes to build.
    /// </summary>
    public interface IPlatformService
    {
        /// <summary>
        /// Returns the primary local IPv4 address if known (e.g., Wi-Fi/Ethernet).
        /// Return <c>string.Empty</c> if unknown – <i>do not</i> throw.
        /// </summary>
        string GetLocalIpAddress();

        /// <summary>Returns a human-readable OS version string.</summary>
        string GetOsVersion();

        /// <summary>Returns device or host machine name.</summary>
        string GetHostName();

        /// <summary>Returns the current username.</summary>
        string GetUserName();

        // =========================
        // Optional extensions below
        // =========================

        /// <summary>
        /// Returns primary local IPv6 when available; empty if not supported.
        /// </summary>
        string GetLocalIpv6Address()
        {
            return string.Empty;
        }

        /// <summary>
        /// Returns the device manufacturer (if known).
        /// </summary>
        string GetManufacturer()
        {
            return string.Empty;
        }

        /// <summary>
        /// Returns the device model (if known).
        /// </summary>
        string GetModel()
        {
            return string.Empty;
        }

        /// <summary>
        /// Returns the application data directory suitable for storing user-specific files.
        /// </summary>
        string GetAppDataDirectory()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                var path = Path.Combine(localAppData, "YasGMP");
                Directory.CreateDirectory(path);
                return path;
            }

            var fallback = Path.Combine(AppContext.BaseDirectory, "AppData");
            Directory.CreateDirectory(fallback);
            return fallback;
        }

        /// <summary>
        /// Best-effort public IPv4; may return empty if offline. Default implementation queries api.ipify.org with a short timeout.
        /// Override on platforms where a different strategy is preferred.
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds (default 2000ms).</param>
        /// <returns>Public IP as dotted-quad string or empty.</returns>
        async Task<string> GetPublicIpAsync(int timeoutMs = 2000)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
                using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(timeoutMs) };
                var ip = (await http.GetStringAsync("https://api.ipify.org", cts.Token).ConfigureAwait(false)).Trim();
                return ip;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}

