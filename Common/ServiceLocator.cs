using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace YasGMP.Common
{
    /// <summary>
    /// Centralized helper for resolving services when constructor injection
    /// is not possible (e.g., XAML-instantiated pages). Stores the root
    /// <see cref="IServiceProvider"/> configured in <see cref="MauiProgram"/>
    /// and falls back to <see cref="Application.Handler"/> when necessary.
    /// </summary>
    public static class ServiceLocator
    {
        private static IServiceProvider? _services;

        /// <summary>
        /// Initializes the locator with the application's root service provider.
        /// Safe to call multiple times; the last non-null provider wins.
        /// </summary>
        /// <param name="provider">The MAUI app service provider.</param>
        public static void Initialize(IServiceProvider provider)
        {
            _services = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Gets the active <see cref="IServiceProvider"/> (locator → Application.Handler → null).
        /// </summary>
        private static IServiceProvider? CurrentProvider
            => _services ?? Application.Current?.Handler?.MauiContext?.Services;

        /// <summary>
        /// Resolves a service if available, otherwise returns <c>null</c>.
        /// </summary>
        public static T? GetService<T>()
        {
            var provider = CurrentProvider;
            return provider is null ? default : provider.GetService<T>();
        }

        /// <summary>
        /// Resolves a required service, throwing a descriptive exception if unavailable.
        /// </summary>
        public static T GetRequiredService<T>() where T : notnull
        {
            var provider = CurrentProvider
                ?? throw new InvalidOperationException("Service provider is not initialized. Ensure MauiProgram.CreateMauiApp() calls ServiceLocator.Initialize().");

            return provider.GetRequiredService<T>();
        }
    }
}
