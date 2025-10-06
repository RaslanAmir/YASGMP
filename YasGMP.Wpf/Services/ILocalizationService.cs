using System;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Provides culture-aware string lookup and language switching for the WPF shell.
/// </summary>
public interface ILocalizationService
{
    /// <summary>Gets the current ISO language code.</summary>
    string CurrentLanguage { get; }

    /// <summary>Raised when the active language changes.</summary>
    event EventHandler? LanguageChanged;

    /// <summary>Resolves the string resource for the provided key.</summary>
    string GetString(string key);

    /// <summary>Switches the UI language to the specified ISO code.</summary>
    void SetLanguage(string language);
}
