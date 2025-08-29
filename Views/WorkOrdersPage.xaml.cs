// ==============================================================================
//  File: Views/WorkOrderPage.xaml.cs
//  Project: YasGMP
//  Summary:
//      Code-behind for WorkOrderPage. Wires the BindingContext to WorkOrderViewModel
//      via DI when available, or falls back to parameterless construction.
//      Defensive and design-time safe.
//  Author: YasGMP
//  © 2025 YasGMP. All rights reserved.
// ==============================================================================
using System;
using Microsoft.Maui.Controls;

namespace YasGMP.Views
{
    /// <summary>
    /// Work orders list and filter page.
    /// <para>
    /// This page binds to <c>YasGMP.ViewModels.WorkOrderViewModel</c> and relies only on
    /// properties and commands that already exist in your ViewModel to avoid breaking changes.
    /// </para>
    /// </summary>
    public partial class WorkOrderPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkOrderPage"/> class and
        /// safely wires the BindingContext.
        /// </summary>
        public WorkOrderPage()
        {
            InitializeComponent();
            TryWireViewModel();
        }

        /// <summary>
        /// Attempts to resolve the ViewModel through the MAUI DI container first,
        /// then falls back to a default constructor if available.
        /// </summary>
        private void TryWireViewModel()
        {
            try
            {
                var services = Application.Current?.Handler?.MauiContext?.Services;

                // Prefer the canonical name:
                var vm = TryResolveByFullName(services, "YasGMP.ViewModels.WorkOrderViewModel");

                if (vm != null)
                {
                    BindingContext = vm;
                }
                // If nothing resolves, the page remains design-time friendly.
            }
            catch
            {
                // Swallow exceptions to keep the page render-safe at design time.
            }
        }

        /// <summary>
        /// Resolves a type by full name using DI first, then by default constructor.
        /// Returns null if resolution fails.
        /// </summary>
        /// <param name="services">Optional DI service provider.</param>
        /// <param name="fullName">Full type name to resolve.</param>
        private static object? TryResolveByFullName(IServiceProvider? services, string fullName)
        {
            var type = Type.GetType(fullName);
            if (type == null) return null;

            // DI first (if configured)
            var fromDi = services?.GetService(type);
            if (fromDi != null) return fromDi;

            // Fallback to default constructor (if present)
            try { return Activator.CreateInstance(type); }
            catch { return null; }
        }
    }
}
