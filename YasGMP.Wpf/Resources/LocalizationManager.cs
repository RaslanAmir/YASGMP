using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;

namespace YasGMP.Wpf.Resources;

/// <summary>
/// Centralized coordinator for culture-specific resource dictionaries used throughout the shell.
/// </summary>
internal static class LocalizationManager
{
    private const string DefaultLanguage = "en";

    private static readonly Dictionary<string, CultureInfo> CultureMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = CultureInfo.GetCultureInfo("en-US"),
        ["hr"] = CultureInfo.GetCultureInfo("hr-HR"),
    };

    private static readonly ResourceManager ResourceManager = new("YasGMP.Wpf.Resources.ShellStrings", typeof(LocalizationManager).Assembly);
    private static readonly List<LocalizedResourceDictionary> Dictionaries = new();

    private static string _currentLanguage = DefaultLanguage;
    private static CultureInfo _currentCulture = CultureMap[DefaultLanguage];
    /// <summary>
    /// Occurs when event handler is raised.
    /// </summary>

    public static event EventHandler? LanguageChanged;
    /// <summary>
    /// Gets or sets the current language.
    /// </summary>

    public static string CurrentLanguage => _currentLanguage;

    static LocalizationManager() => ApplyCulture(DefaultLanguage);
    /// <summary>
    /// Executes the register operation.
    /// </summary>

    public static void Register(LocalizedResourceDictionary dictionary)
    {
        if (!Dictionaries.Contains(dictionary))
        {
            Dictionaries.Add(dictionary);
        }

        dictionary.UpdateResources(ResourceManager, _currentCulture);
    }
    /// <summary>
    /// Executes the set language operation.
    /// </summary>

    public static void SetLanguage(string language)
    {
        var normalized = NormalizeLanguage(language);
        if (!string.Equals(_currentLanguage, normalized, StringComparison.OrdinalIgnoreCase))
        {
            _currentLanguage = normalized;
            ApplyCulture(normalized);

            foreach (var dictionary in Dictionaries.ToList())
            {
                dictionary.UpdateResources(ResourceManager, _currentCulture);
            }

            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }
    }
    /// <summary>
    /// Executes the get string operation.
    /// </summary>

    public static string GetString(string key)
    {
        var value = ResourceManager.GetString(key, _currentCulture)
                    ?? ResourceManager.GetString(key, CultureInfo.InvariantCulture);

        return string.IsNullOrEmpty(value) ? key : value;
    }

    private static void ApplyCulture(string language)
    {
        if (!CultureMap.TryGetValue(language, out var culture))
        {
            culture = CultureMap[DefaultLanguage];
        }

        _currentCulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private static string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return DefaultLanguage;
        }

        if (CultureMap.ContainsKey(language))
        {
            return language;
        }

        var dashIndex = language.IndexOf('-');
        if (dashIndex > 0)
        {
            var prefix = language[..dashIndex];
            if (CultureMap.ContainsKey(prefix))
            {
                return prefix;
            }
        }

        return DefaultLanguage;
    }
}
