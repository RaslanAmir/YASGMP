using System;
using Microsoft.Extensions.DependencyInjection;

namespace YasGMP.Common
{
    /// <summary>
    /// Centralized helper for resolving services when constructor injection is not available
    /// (e.g., XAML-instantiated pages). Stores the application's root <see cref="IServiceProvider"/>
    /// and allows platforms to register an optional fallback resolver.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly object Sync = new();
        private static IServiceProvider? _services;
        private static Func<IServiceProvider?>? _fallback;

        /// <summary>Initializes the locator with the application's root service provider.</summary>
        public static void Initialize(IServiceProvider provider)
        {
            if (provider is null) throw new ArgumentNullException(nameof(provider));
            lock (Sync)
            {
                _services = provider;
            }
        }

        /// <summary>Registers a fallback resolver invoked when <see cref="Initialize"/> has not been called yet.</summary>
        public static void RegisterFallback(Func<IServiceProvider?> fallback)
        {
            if (fallback is null) throw new ArgumentNullException(nameof(fallback));
            lock (Sync)
            {
                _fallback = fallback;
            }
        }

        private static IServiceProvider? CurrentProvider
        {
            get
            {
                lock (Sync)
                {
                    if (_services is not null)
                    {
                        return _services;
                    }

                    return _fallback?.Invoke();
                }
            }
        }

        /// <summary>Resolves a service if available; otherwise returns <c>null</c>.</summary>
        public static T? GetService<T>()
        {
            var provider = CurrentProvider;
            return provider is null ? default : provider.GetService<T>();
        }

        /// <summary>Resolves a required service, throwing when unavailable.</summary>
        public static T GetRequiredService<T>() where T : notnull
        {
            var provider = CurrentProvider
                ?? throw new InvalidOperationException("Service provider is not initialized. Call ServiceLocator.Initialize() or register a fallback resolver.");

            return provider.GetRequiredService<T>();
        }
    }
}

