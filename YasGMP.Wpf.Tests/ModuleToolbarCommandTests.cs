using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using Xunit;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public sealed class ModuleToolbarCommandTests
{
    [Fact]
    public void LanguageChange_RehydratesLocalizedProperties()
    {
        var localization = new FakeLocalizationService(
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Module.Toolbar.Toggle.Find.Content"] = "Find",
                    ["Module.Toolbar.Toggle.Find.ToolTip"] = "Locate an existing record.",
                    ["Module.Toolbar.Toggle.Find.AutomationName"] = "Find command",
                    ["Module.Toolbar.Toggle.Find.AutomationId"] = "Toolbar_Find"
                },
                ["hr"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Module.Toolbar.Toggle.Find.Content"] = "Traži",
                    ["Module.Toolbar.Toggle.Find.ToolTip"] = "Pronađi postojeći zapis.",
                    ["Module.Toolbar.Toggle.Find.AutomationName"] = "Naredba traži",
                    ["Module.Toolbar.Toggle.Find.AutomationId"] = "AlatnaTraka_Trazi"
                }
            },
            initialLanguage: "en");

        var command = new ModuleToolbarCommand(
            captionKey: "Module.Toolbar.Toggle.Find.Content",
            command: new RelayCommand(() => { }),
            localization: localization,
            toolTipKey: "Module.Toolbar.Toggle.Find.ToolTip",
            automationNameKey: "Module.Toolbar.Toggle.Find.AutomationName",
            automationIdKey: "Module.Toolbar.Toggle.Find.AutomationId");

        Assert.Equal("Find", command.Caption);
        Assert.Equal("Locate an existing record.", command.ToolTip);
        Assert.Equal("Find command", command.AutomationName);
        Assert.Equal("Toolbar_Find", command.AutomationId);

        localization.SetLanguage("hr");

        Assert.Equal("Traži", command.Caption);
        Assert.Equal("Pronađi postojeći zapis.", command.ToolTip);
        Assert.Equal("Naredba traži", command.AutomationName);
        Assert.Equal("AlatnaTraka_Trazi", command.AutomationId);
    }

    private sealed class FakeLocalizationService : ILocalizationService
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
}
