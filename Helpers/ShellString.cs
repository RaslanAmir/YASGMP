using System;
using System.Globalization;
using System.Resources;

namespace YasGMP.Helpers
{
    /// <summary>
    /// Provides access to the shared <c>ShellStrings</c> resources that back both the MAUI
    /// and WPF shells. This allows dialogs inside the MAUI project to reuse the same
    /// localization keys (including EN/HR satellite resources) without duplicating text.
    /// </summary>
    internal static class ShellString
    {
        private static readonly ResourceManager ResourceManager =
            new("YasGMP.Wpf.Resources.ShellStrings", typeof(ShellString).Assembly);

        /// <summary>
        /// Resolves the specified resource key, falling back to the key name when no
        /// localized value exists for the active UI culture.
        /// </summary>
        /// <param name="key">Resource identifier from <c>ShellStrings.resx</c>.</param>
        /// <returns>The localized string or the key itself if no translation is available.</returns>
        public static string Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            string? value = ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
            return value ?? key;
        }
    }
}
