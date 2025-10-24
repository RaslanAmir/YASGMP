using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace YasGMP.Diagnostics
{
    /// <summary>
    /// Provides platform-neutral access to YasGMP diagnostics storage paths.
    /// Resolves to the MAUI app data directory when available and falls back to
    /// the user-local application data directory for desktop targets.
    /// </summary>
    internal static class DiagnosticsPathProvider
    {
        private const string ApplicationFolderName = "YasGMP";
        private const string LogsFolderName = "logs";
        private static readonly Lazy<string> AppDataRoot = new(ResolveAppDataDirectory, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the root application data directory used for diagnostics artifacts.
        /// </summary>
        /// <returns>The absolute application data directory path.</returns>
        public static string GetAppDataDirectory() => AppDataRoot.Value;

        /// <summary>
        /// Gets the diagnostics logs directory, creating it when it does not exist.
        /// </summary>
        /// <returns>The absolute logs directory path.</returns>
        public static string GetLogsDirectory() => EnsureDirectory(Path.Combine(GetAppDataDirectory(), LogsFolderName));

        /// <summary>
        /// Ensures the supplied directory path exists and returns the same path.
        /// </summary>
        /// <param name="path">The directory to create if missing.</param>
        /// <returns>The original <paramref name="path"/> value.</returns>
        public static string EnsureDirectory(string path)
        {
            Directory.CreateDirectory(path);
            return path;
        }

        private static string ResolveAppDataDirectory()
        {
            var mauiPath = TryGetMauiAppDataDirectory();
            if (!string.IsNullOrWhiteSpace(mauiPath))
            {
                Directory.CreateDirectory(mauiPath);
                return mauiPath;
            }

            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(basePath))
            {
                basePath = AppContext.BaseDirectory;
            }

            var resolved = Path.Combine(basePath, ApplicationFolderName);
            Directory.CreateDirectory(resolved);
            return resolved;
        }

        private static string? TryGetMauiAppDataDirectory()
        {
            try
            {
                var type = Type.GetType("Microsoft.Maui.Storage.FileSystem, Microsoft.Maui.Essentials");
                var property = type?.GetProperty("AppDataDirectory", BindingFlags.Public | BindingFlags.Static);
                if (property?.GetValue(null) is string value && !string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
            catch
            {
                // Best-effort: the MAUI assembly may not be available on desktop targets.
            }

            return null;
        }
    }
}
