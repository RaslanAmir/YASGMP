using System;
using System.Globalization;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Provides formatting helpers for <see cref="ILocalizationService"/> consumers.
/// </summary>
public static class LocalizationServiceExtensions
{
    /// <summary>
    /// Formats the localized string associated with the provided key.
    /// </summary>
    /// <param name="localization">Localization lookup service.</param>
    /// <param name="key">Resource key to resolve.</param>
    /// <param name="arguments">Optional format arguments applied with the current culture.</param>
    /// <returns>Formatted string resolved from the localization dictionaries.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="localization"/> is <c>null</c>.</exception>
    public static string GetString(this ILocalizationService localization, string key, params object?[] arguments)
    {
        if (localization is null)
        {
            throw new ArgumentNullException(nameof(localization));
        }

        var format = localization.GetString(key);
        if (arguments is null || arguments.Length == 0)
        {
            return format;
        }

        return string.Format(CultureInfo.CurrentCulture, format, arguments);
    }
}
