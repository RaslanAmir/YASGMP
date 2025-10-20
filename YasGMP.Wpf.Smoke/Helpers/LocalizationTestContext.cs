using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using YasGMP.Common;
using YasGMP.Wpf;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Smoke.Helpers;

internal static class LocalizationTestContext
{
    private static IServiceProvider? _fallbackProvider;

    public static ILocalizationService ResolveLocalizationService()
    {
        EnsureFallback();
        return ServiceLocator.GetRequiredService<ILocalizationService>();
    }

    private static void EnsureFallback()
    {
        if (_fallbackProvider is not null)
        {
            return;
        }

        var services = new ServiceCollection();
        services.AddSingleton<ILocalizationService>(_ => new TestLocalizationService());
        _fallbackProvider = services.BuildServiceProvider();
        ServiceLocator.RegisterFallback(() => _fallbackProvider);
    }

    private sealed class TestLocalizationService : ILocalizationService
    {
        private static readonly Lazy<ResourceDictionary> AutomationResources = new(CreateAutomationDictionary);

        private readonly LocalizationService _inner = new();

        public string CurrentLanguage => _inner.CurrentLanguage;

        public event EventHandler? LanguageChanged
        {
            add => _inner.LanguageChanged += value;
            remove => _inner.LanguageChanged -= value;
        }

        public string GetString(string key)
        {
            var localized = _inner.GetString(key);
            if (!string.Equals(localized, key, StringComparison.Ordinal))
            {
                return localized;
            }

            var resources = AutomationResources.Value;
            if (resources.Contains(key) && resources[key] is string automationId)
            {
                return automationId;
            }

            return key;
        }

        public void SetLanguage(string language) => _inner.SetLanguage(language);

        private static ResourceDictionary CreateAutomationDictionary()
        {
            if (Application.ResourceAssembly is null)
            {
                Application.ResourceAssembly = typeof(App).Assembly;
            }

            return new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/YasGMP.Wpf;component/Resources/Strings.xaml", UriKind.Absolute),
            };
        }
    }
}
