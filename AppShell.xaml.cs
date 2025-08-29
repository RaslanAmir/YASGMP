// ==============================================================================
// File: AppShell.xaml.cs
// Purpose: Registers Shell routes and provides global toolbar navigation handlers.
// Notes : Duplicate-safe route registration; toolbar helpers; UI-thread safe alerts.
// ==============================================================================

#pragma warning disable CA1416 // MAUI cross-platform; analyzer false-positives here

using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;

namespace YasGMP
{
    /// <summary>
    /// Application shell. Responsible for registering routes so that
    /// string-based navigation (<see cref="Shell.GoToAsync(string)"/>) works,
    /// and for exposing toolbar shortcuts to common modules.
    /// </summary>
    public partial class AppShell : Shell
    {
        private static class AppRoutes
        {
            public const string Dashboard  = "routes/dashboard";
            public const string Machines   = "routes/machines";
            public const string Parts      = "routes/parts";
            public const string WorkOrders = "routes/workorders";
            public const string Capa       = "routes/capa";
            public const string Validation = "routes/validation";
            public const string AdminPanel = "routes/adminpanel";
            public const string AuditLog   = "routes/auditlog";
        }

        private static readonly object _routesLock = new();
        private static bool _routesRegistered;

        public AppShell()
        {
            InitializeComponent();
            EnsureRoutesRegistered();
        }

        /// <summary>Idempotent entry point to register routes once per process.</summary>
        public static void EnsureRoutesRegistered()
        {
            if (_routesRegistered) return;

            lock (_routesLock)
            {
                if (_routesRegistered) return;

                try
                {
                    RegisterRoutes();
                    _routesRegistered = true;
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("[ROUTING] Routes registered.");
#endif
                }
                catch (Exception ex)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ROUTING] EnsureRoutesRegistered error: {ex}");
#endif
                    _routesRegistered = true; // prevent re-entry storms
                }
            }
        }

        private static void RegisterRoutes()
        {
            RegisterRouteSafe(AppRoutes.Dashboard,  "DashboardPage");
            RegisterRouteSafe(AppRoutes.AuditLog,   "AuditLogPage");
            RegisterRouteSafe(AppRoutes.Machines,   "MachinesPage");
            RegisterRouteSafe(AppRoutes.Parts,      "PartsPage");
            RegisterRouteSafe(AppRoutes.WorkOrders, "WorkOrdersPage");
            RegisterRouteSafe(AppRoutes.Capa,       "CapaPage");
            RegisterRouteSafe(AppRoutes.Validation, "ValidationPage");
            RegisterRouteSafe(AppRoutes.AdminPanel, "AdminPanelPage");
        }

        private static void RegisterRouteSafe(string route, string typeName)
        {
            if (string.IsNullOrWhiteSpace(route)) throw new ArgumentException("Route cannot be null or empty.", nameof(route));
            if (string.IsNullOrWhiteSpace(typeName)) throw new ArgumentException("Type name cannot be null or empty.", nameof(typeName));

            var candidates = new[]
            {
                $"YasGMP.Pages.{typeName}",
                $"YasGMP.Views.{typeName}",
                $"YasGMP.{typeName}"
            };

            Type? pageType = null;
            foreach (var qn in candidates)
            {
                pageType = Type.GetType(qn);
                if (pageType != null) break;
            }

            try
            {
                Routing.RegisterRoute(route, pageType ?? typeof(ContentPage));
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ROUTING] RegisterRouteSafe('{route}', '{typeName}') ignored: {ex.Message}");
#endif
            }
        }

        // ======================= Toolbar navigation =======================

        private static async Task GoAsync(string route)
        {
            if (string.IsNullOrWhiteSpace(route)) return;

            EnsureRoutesRegistered();

            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync(route).ConfigureAwait(false);
                return;
            }

            // Fallback alert — force UI thread to avoid COMException 0x8001010E
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    var page = Application.Current?.MainPage;
                    if (page != null)
                        await page.DisplayAlert("Navigation unavailable",
                            $"Shell is not active. Could not navigate to '{route}'.",
                            "OK");
                }
                catch { /* no-op */ }
            });
        }

        private async void OnToolbarGoDashboard(object sender, EventArgs e)  => await GoAsync(AppRoutes.Dashboard);
        private async void OnToolbarGoMachines(object sender, EventArgs e)   => await GoAsync(AppRoutes.Machines);
        private async void OnToolbarGoParts(object sender, EventArgs e)      => await GoAsync(AppRoutes.Parts);
        private async void OnToolbarGoWorkOrders(object sender, EventArgs e) => await GoAsync(AppRoutes.WorkOrders);
        private async void OnToolbarGoCapa(object sender, EventArgs e)       => await GoAsync(AppRoutes.Capa);
        private async void OnToolbarGoValidation(object sender, EventArgs e) => await GoAsync(AppRoutes.Validation);
        private async void OnToolbarGoAdmin(object sender, EventArgs e)      => await GoAsync(AppRoutes.AdminPanel);
    }
}

#pragma warning restore CA1416
