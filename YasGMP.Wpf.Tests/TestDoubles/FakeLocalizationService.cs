using System;
using System.Collections.Generic;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Tests.TestDoubles;

internal sealed class FakeLocalizationService : ILocalizationService
{
    private readonly IDictionary<string, IDictionary<string, string>> _resources;

    public FakeLocalizationService(
        IDictionary<string, IDictionary<string, string>> resources,
        string initialLanguage)
    {
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        CurrentLanguage = initialLanguage ?? throw new ArgumentNullException(nameof(initialLanguage));
    }

    public string CurrentLanguage { get; private set; }

    public event EventHandler? LanguageChanged;

    public string GetString(string key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (_resources.TryGetValue(CurrentLanguage, out var language) && language.TryGetValue(key, out var value))
        {
            return value;
        }

        if (_resources.TryGetValue("neutral", out var neutral) && neutral.TryGetValue(key, out var neutralValue))
        {
            return neutralValue;
        }

        return key;
    }

    public void SetLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            throw new ArgumentException("Language must be provided.", nameof(language));
        }

        if (!_resources.ContainsKey(language))
        {
            throw new InvalidOperationException($"Language '{language}' is not configured for the fake localization service.");
        }

        if (!string.Equals(CurrentLanguage, language, StringComparison.OrdinalIgnoreCase))
        {
            CurrentLanguage = language;
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
