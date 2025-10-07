using System;
using System.Windows;
using YasGMP.Wpf.Resources;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Default implementation that bridges view-models with the shared localization dictionaries.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    /// <summary>
    /// Initializes a new instance of the LocalizationService class.
    /// </summary>
    public LocalizationService()
    {
        LocalizationManager.LanguageChanged += OnLanguageChanged;
    }
    /// <summary>
    /// Gets or sets the current language.
    /// </summary>

    public string CurrentLanguage => LocalizationManager.CurrentLanguage;
    /// <summary>
    /// Occurs when event handler is raised.
    /// </summary>

    public event EventHandler? LanguageChanged;
    /// <summary>
    /// Executes the get string operation.
    /// </summary>

    public string GetString(string key)
    {
        var value = LocalizationManager.GetString(key);
        if (!string.Equals(value, key, StringComparison.Ordinal))
        {
            return value;
        }

        if (Application.Current?.TryFindResource(key) is string fallback)
        {
            return fallback;
        }

        return key;
    }
    /// <summary>
    /// Executes the set language operation.
    /// </summary>

    public void SetLanguage(string language) => LocalizationManager.SetLanguage(language);

    private void OnLanguageChanged(object? sender, EventArgs e) => LanguageChanged?.Invoke(this, EventArgs.Empty);
}
