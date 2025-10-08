using System;
using Microsoft.Extensions.DependencyInjection;
using YasGMP.Common;
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
        services.AddSingleton<ILocalizationService, LocalizationService>();
        _fallbackProvider = services.BuildServiceProvider();
        ServiceLocator.RegisterFallback(() => _fallbackProvider);
    }
}
