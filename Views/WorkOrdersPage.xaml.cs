// ==============================================================================
//  File: Views/WorkOrdersPage.xaml.cs
//  Project: YasGMP
//  Summary:
//      Code-behind for WorkOrdersPage (plural). Wires BindingContext from DI (if
//      available) and safely attempts initial refresh. No external dependencies;
//      design-time safe. All navigation/alerts handled elsewhere (SafeNavigator).
//  Author: YasGMP
//  © 2025 YasGMP. All rights reserved.
// ==============================================================================
#nullable enable

using System;
using System.Reflection;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace YasGMP.Views
{
    /// <summary>
    /// <b>WorkOrdersPage</b> — YASTECH-themed overview of work orders with filters,
    /// KPI tiles, and action commands. The page binds to a ViewModel resolved from DI.
    /// By default it looks for <c>YasGMP.ViewModels.WorkOrderViewModel</c> and
    /// <c>YasGMP.ViewModels.WorkOrdersViewModel</c> (both supported).
    /// </summary>
    public partial class WorkOrdersPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkOrdersPage"/> and wires
        /// the ViewModel from MAUI DI or a default constructor without throwing.
        /// </summary>
        public WorkOrdersPage()
        {
            InitializeComponent();
            TryWireViewModel();
        }

        /// <summary>
        /// Tries to resolve the Work Order ViewModel using the MAUI
        /// service provider first, falling back to parameterless construction.
        /// Swallows all exceptions to keep the page design-time safe.
        /// </summary>
        private void TryWireViewModel()
        {
            try
            {
                var services = Application.Current?.Handler?.MauiContext?.Services;

                // Try common VM names (singular and plural)
                var vm = TryResolveByFullName(services, "YasGMP.ViewModels.WorkOrderViewModel")
                      ?? TryResolveByFullName(services, "YasGMP.ViewModels.WorkOrdersViewModel");

                if (vm != null)
                    BindingContext = vm;
            }
            catch
            {
                // No-op: design-time safety.
            }
        }

        /// <summary>
        /// Attempts to resolve a type by its full name:
        /// 1) via DI container, 2) via default ctor if available.
        /// </summary>
        /// <param name="services">Optional service provider.</param>
        /// <param name="fullName">Full type name.</param>
        /// <returns>Instance or null.</returns>
        private static object? TryResolveByFullName(IServiceProvider? services, string fullName)
        {
            var type = Type.GetType(fullName);
            if (type is null) return null;

            // 1) Service provider
            var viaDi = services?.GetService(type);
            if (viaDi != null) return viaDi;

            // 2) Default constructor
            try { return Activator.CreateInstance(type); }
            catch { return null; }
        }

        /// <summary>
        /// On appearing, attempts a safe initial refresh by invoking a compatible
        /// command (if available) on the ViewModel:
        /// <list type="bullet">
        /// <item><description><c>LoadWorkOrdersCommand</c> (preferred)</description></item>
        /// <item><description><c>LoadCommand</c> (fallback)</description></item>
        /// </list>
        /// This is best-effort and never throws.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                var vm = BindingContext;
                if (vm == null) return;

                ICommand? cmd = GetCommand(vm, "LoadWorkOrdersCommand") ?? GetCommand(vm, "LoadCommand");
                if (cmd?.CanExecute(null) == true)
                    cmd.Execute(null);
            }
            catch
            {
                // Intentionally ignore: keep UI responsive and safe.
            }
        }

        /// <summary>Helper to get an <see cref="ICommand"/> property by name via reflection.</summary>
        private static ICommand? GetCommand(object vm, string propertyName)
        {
            var p = vm.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return p?.GetValue(vm) as ICommand;
        }
    }
}
