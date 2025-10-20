using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
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
            private static readonly Lazy<IReadOnlyDictionary<string, string>> AutomationResources = new(CreateAutomationDictionary);

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
            if (resources.TryGetValue(key, out var automationId))
            {
                return automationId;
            }

            return key;
        }

        public void SetLanguage(string language) => _inner.SetLanguage(language);

        private static IReadOnlyDictionary<string, string> CreateAutomationDictionary()
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                return CreateAutomationDictionaryCore();
            }

            IReadOnlyDictionary<string, string>? dictionary = null;
            ExceptionDispatchInfo? captured = null;

            var staThread = new Thread(() =>
            {
                try
                {
                    dictionary = CreateAutomationDictionaryCore();
                }
                catch (Exception ex)
                {
                    captured = ExceptionDispatchInfo.Capture(ex);
                }
            })
            {
                IsBackground = true,
            };

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();

            captured?.Throw();

            return dictionary ?? throw new InvalidOperationException("Failed to load automation resource dictionary on STA thread.");
        }

        private static IReadOnlyDictionary<string, string> CreateAutomationDictionaryCore()
        {
            if (Application.ResourceAssembly is null)
            {
                Application.ResourceAssembly = typeof(App).Assembly;
            }

            var resourceDictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/YasGMP.Wpf;component/Resources/Strings.xaml", UriKind.Absolute),
            };

            var materialized = new Dictionary<string, string>(resourceDictionary.Count);
            foreach (DictionaryEntry entry in resourceDictionary)
            {
                if (entry.Key is string key && entry.Value is string value)
                {
                    materialized[key] = value;
                }
            }

            return materialized;
        }
    }
}
