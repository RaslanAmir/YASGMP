using System;
using System.Windows;
using YasGMP.Wpf.Resources;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Default implementation that bridges view-models with the shared localization dictionaries.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    public LocalizationService()
    {
        LocalizationManager.LanguageChanged += OnLanguageChanged;
    }

    public string CurrentLanguage => LocalizationManager.CurrentLanguage;

    public event EventHandler? LanguageChanged;

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

    public void SetLanguage(string language) => LocalizationManager.SetLanguage(language);

    private void OnLanguageChanged(object? sender, EventArgs e) => LanguageChanged?.Invoke(this, EventArgs.Empty);
}
