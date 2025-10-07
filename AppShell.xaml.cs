using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace YasGMP
{
    /// <summary>Centralized Shell navigation using absolute paths (routes declared in XAML).</summary>
    public partial class AppShell : Shell
    {
        // Absolute paths through the Shell hierarchy defined in AppShell.xaml.
        private static class Paths
        {
            // Home
            private const string Dashboard      = "//root/home/dashboard";

            // Operations
            private const string WorkOrders     = "//root/ops/workorders";
            private const string Machines       = "//root/ops/machines";
            private const string Parts          = "//root/ops/parts";
            private const string Warehouses     = "//root/ops/warehouses";
            private const string Suppliers      = "//root/ops/suppliers";
            private const string Components     = "//root/ops/components";
            private const string External       = "//root/ops/externalservicers";
            private const string Ppm            = "//root/ops/ppm";
            private const string Calibrations   = "//root/ops/calibrations";

            // Quality
            private const string Capa           = "//root/quality/capa";
            private const string Validation     = "//root/quality/validation";
            private const string AuditDashboard = "//root/quality/auditdashboard";
            private const string AuditLog       = "//root/quality/auditlog";

            // Admin
            private const string Users          = "//root/admin/users";
            private const string Rbac           = "//root/admin/rbac";
            private const string AdminPanel     = "//root/admin/adminpanel";
            private const string Rollback       = "//root/admin/rollbackpreview";

            // Debug
            private const string Debug          = "//root/debug/debug_dashboard";
            private const string LogViewer      = "//root/debug/logviewer";
            private const string Health         = "//root/debug/health";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppShell"/> class and loads XAML-defined routes.
        /// </summary>
        public AppShell()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Backward-compatibility shim: older code calls this before navigation.
        /// With XAML-declared routes + absolute paths, nothing is required.
        /// </summary>
        public static void EnsureRoutesRegistered()
        {
            // No-op by design. Routes are declared in XAML and we navigate via absolute paths.
        }

        private static async Task GoAsync(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath)) return;

            try
            {
                // Always marshal to the UI thread for Shell navigation (WinUI is strict).
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (Shell.Current is not null)
                        await Shell.Current.GoToAsync(absolutePath);
                    else
                    {
                        var page = Application.Current?.MainPage;
                        if (page != null)
                            await page.DisplayAlert("Navigation unavailable",
                                $"Shell is not active. Could not navigate to '{absolutePath}'.", "OK");
                    }
                });
            }
            catch (Exception ex)
            {
                // Surface the exact issue to the user rather than crashing.
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var page = Application.Current?.MainPage;
                    if (page != null)
                        await page.DisplayAlert("Navigation error",
                            $"Failed to navigate to '{absolutePath}'.\n{ex.Message}", "OK");
                });
            }
        }

        // Toolbar routes
        private async void OnToolbarGoDashboard (object s, EventArgs e) => await GoAsync(Paths.Dashboard);

        private async void OnToolbarGoWorkOrders(object s, EventArgs e) => await GoAsync(Paths.WorkOrders);
        private async void OnToolbarGoMachines  (object s, EventArgs e) => await GoAsync(Paths.Machines);
        private async void OnToolbarGoParts     (object s, EventArgs e) => await GoAsync(Paths.Parts);
        private async void OnToolbarGoWarehouses(object s, EventArgs e) => await GoAsync(Paths.Warehouses);
        private async void OnToolbarGoSuppliers (object s, EventArgs e) => await GoAsync(Paths.Suppliers);
        private async void OnToolbarGoComponents(object s, EventArgs e) => await GoAsync(Paths.Components);
        private async void OnToolbarGoExternal  (object s, EventArgs e) => await GoAsync(Paths.External);
        private async void OnToolbarGoPpm       (object s, EventArgs e) => await GoAsync(Paths.Ppm);
        private async void OnToolbarGoCalibrations(object s, EventArgs e) => await GoAsync(Paths.Calibrations);

        private async void OnToolbarGoCapa      (object s, EventArgs e) => await GoAsync(Paths.Capa);
        private async void OnToolbarGoValidation(object s, EventArgs e) => await GoAsync(Paths.Validation);
        private async void OnToolbarGoAuditDash (object s, EventArgs e) => await GoAsync(Paths.AuditDashboard);
        private async void OnToolbarGoAuditLog  (object s, EventArgs e) => await GoAsync(Paths.AuditLog);

        private async void OnToolbarGoUsers     (object s, EventArgs e) => await GoAsync(Paths.Users);
        private async void OnToolbarGoRbac      (object s, EventArgs e) => await GoAsync(Paths.Rbac);
        private async void OnToolbarGoAdmin     (object s, EventArgs e) => await GoAsync(Paths.AdminPanel);
        private async void OnToolbarGoRollback  (object s, EventArgs e) => await GoAsync(Paths.Rollback);

        private async void OnToolbarGoDebug     (object s, EventArgs e) => await GoAsync(Paths.Debug);
    }
}
