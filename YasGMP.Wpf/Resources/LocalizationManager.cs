using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace YasGMP.Wpf.Resources;

/// <summary>
/// Centralized coordinator for culture-specific resource dictionaries used throughout the shell.
/// </summary>
internal static class LocalizationManager
{
    private static readonly Dictionary<string, Uri> ResourceMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new Uri("pack://application:,,,/YasGMP.Wpf;component/Resources/Strings.en.xaml", UriKind.Absolute),
        ["hr"] = new Uri("pack://application:,,,/YasGMP.Wpf;component/Resources/Strings.hr.xaml", UriKind.Absolute),
    };

    private static readonly List<LocalizedResourceDictionary> Dictionaries = new();
    private static string _currentLanguage = "en";
    private static ResourceDictionary? _currentDictionary;

    public static event EventHandler? LanguageChanged;

    public static string CurrentLanguage => _currentLanguage;

    static LocalizationManager()
    {
        ApplyCulture(_currentLanguage);
    }

    public static void Register(LocalizedResourceDictionary dictionary)
    {
        if (!Dictionaries.Contains(dictionary))
        {
            Dictionaries.Add(dictionary);
        }

        dictionary.ApplySource(GetResourceUri(_currentLanguage));
        _currentDictionary = dictionary;
    }

    public static void SetLanguage(string language)
    {
        var normalized = NormalizeLanguage(language);
        if (string.Equals(_currentLanguage, normalized, StringComparison.OrdinalIgnoreCase) && _currentDictionary != null)
        {
            return;
        }

        _currentLanguage = normalized;
        ApplyCulture(normalized);

        var source = GetResourceUri(normalized);
        foreach (var dictionary in Dictionaries.ToList())
        {
            dictionary.ApplySource(source);
            _currentDictionary = dictionary;
        }

        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public static string GetString(string key)
    {
        if (_currentDictionary != null && _currentDictionary.Contains(key))
        {
            return Convert.ToString(_currentDictionary[key], CultureInfo.CurrentCulture) ?? key;
        }

        var resource = new ResourceDictionary { Source = GetResourceUri(_currentLanguage) };
        return resource.Contains(key)
            ? Convert.ToString(resource[key], CultureInfo.CurrentCulture) ?? key
            : key;
    }

    private static void ApplyCulture(string language)
    {
        var culture = language switch
        {
            "hr" => CultureInfo.GetCultureInfo("hr-HR"),
            _ => CultureInfo.GetCultureInfo("en-US"),
        };

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private static string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "en";
        }

        return ResourceMap.ContainsKey(language) ? language : "en";
    }

    private static Uri GetResourceUri(string language) => ResourceMap[language];
}
