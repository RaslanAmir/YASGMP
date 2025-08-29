// ==============================================================================
//  File: DashboardPage.xaml.cs
//  Project: YasGMP
//  Summary:
//      Main dashboard page code-behind. Provides role-based UI and Shell routing.
//      Single, definitive class (duplicates removed).
//      Defaults to show all modules (except Admin) if role is missing to avoid a blank screen.
//      All navigation/alerts are UI-thread safe through SafeNavigator to avoid 0x8001010E.
// ==============================================================================

using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel; // MainThread
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP
{
    /// <summary>
    /// <b>DashboardPage</b> – Main application dashboard after login.
    /// Adapts visible options according to the logged-in user's role and
    /// navigates to modules using Shell routes.
    /// </summary>
    public partial class DashboardPage : ContentPage
    {
        /// <summary>
        /// Centralized route names used for Shell navigation.
        /// Ensure these routes exist/are registered in <see cref="AppShell"/>.
        /// </summary>
        private static class AppRoutes
        {
            /// <summary>Route to the Machines module.</summary>
            public const string Machines   = "routes/machines";
            /// <summary>Route to the Parts/Warehouse module.</summary>
            public const string Parts      = "routes/parts";
            /// <summary>Route to the Work Orders module.</summary>
            public const string WorkOrders = "routes/workorders";
            /// <summary>Route to the CAPA module.</summary>
            public const string Capa       = "routes/capa";
            /// <summary>Route to the Validation module.</summary>
            public const string Validation = "routes/validation";
            /// <summary>Route to the Admin panel.</summary>
            public const string AdminPanel = "routes/adminpanel";
        }

        /// <summary>
        /// Currently logged-in user displayed on the dashboard (may be <c>null</c> before <see cref="SetUser(User)"/>).
        /// </summary>
        public User? CurrentUser { get; private set; }

        /// <summary>
        /// Parameterless constructor required by XAML compiler, Shell, and Hot Reload.
        /// If the global <see cref="App.LoggedUser"/> exists, the UI is wired automatically.
        /// </summary>
        public DashboardPage()
        {
            InitializeComponent();

            // Ensure routes are available before any navigation attempts.
            AppShell.EnsureRoutesRegistered();

            var app    = Application.Current as App;
            var logged = app?.LoggedUser;

            if (logged is not null)
            {
                SetUser(logged);
            }
            else
            {
                BindingContext = this;
                AdjustMenuForRole(string.Empty);
            }
        }

        /// <summary>
        /// Overload that accepts an authenticated user (preferred when navigating directly after login).
        /// Accepts nullable input to be robust against accidental null calls.
        /// </summary>
        /// <param name="user">Authenticated user (nullable for safety).</param>
        public DashboardPage(User? user) : this()
        {
            if (user is not null && (CurrentUser is null || !string.Equals(CurrentUser.Username, user.Username, StringComparison.Ordinal)))
            {
                SetUser(user);
            }
        }

        /// <summary>
        /// Applies the provided user to the page, updates header UI and button visibility.
        /// </summary>
        /// <param name="user">User to display on dashboard (must be non-null).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is <c>null</c>.</exception>
        private void SetUser(User user)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));

            CurrentUser   = user;
            BindingContext = this;

            var fullName = string.IsNullOrWhiteSpace(CurrentUser.FullName)
                ? CurrentUser.Username
                : CurrentUser.FullName;

            var roleRaw  = CurrentUser.Role ?? string.Empty;
            var roleNorm = NormalizeRole(roleRaw);

            if (UserNameLabel is not null)
                UserNameLabel.Text = $"Korisnik: {fullName}";

            if (UserRoleLabel is not null)
            {
                var display = string.IsNullOrWhiteSpace(roleRaw) ? "NIJE POSTAVLJENO" : roleRaw.ToUpperInvariant();
                UserRoleLabel.Text = $"Uloga: {display}";
            }

            AdjustMenuForRole(roleNorm);
        }

        /// <summary>
        /// Normalizes role by trimming, lowercasing and removing diacritics (š/č/ć/đ/ž).
        /// </summary>
        private static string NormalizeRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return string.Empty;
            var r = role.Trim().ToLowerInvariant();
            r = r.Replace("š", "s").Replace("č", "c").Replace("ć", "c").Replace("đ", "dj").Replace("ž", "z");
            return r;
        }

        /// <summary>
        /// Shows or hides dashboard buttons according to the user's role.
        /// Valid values: "admin", "sef", "tehnicar", "auditor" (case/diacritics-insensitive).
        /// If role is empty/unknown, defaults to show all modules except Admin to avoid a blank screen.
        /// </summary>
        /// <param name="role">Role string from <see cref="User.Role"/>.</param>
        private void AdjustMenuForRole(string role)
        {
            var r = NormalizeRole(role);

            // Default when role missing: show everything except Admin.
            if (string.IsNullOrEmpty(r))
            {
                if (MachinesButton is not null)    MachinesButton.IsVisible   = true;
                if (PartsButton is not null)       PartsButton.IsVisible      = true;
                if (WorkOrdersButton is not null)  WorkOrdersButton.IsVisible = true;
                if (CapaButton is not null)        CapaButton.IsVisible       = true;
                if (ValidationButton is not null)  ValidationButton.IsVisible = true;
                if (AdminPanelButton is not null)  AdminPanelButton.IsVisible = false;
                return;
            }

            if (MachinesButton is not null)    MachinesButton.IsVisible   = r is "admin" or "sef" or "tehnicar";
            if (PartsButton is not null)       PartsButton.IsVisible      = r is "admin" or "sef" or "tehnicar";
            if (WorkOrdersButton is not null)  WorkOrdersButton.IsVisible = r is "admin" or "sef" or "tehnicar";
            if (CapaButton is not null)        CapaButton.IsVisible       = r is "admin" or "auditor" or "sef";
            if (ValidationButton is not null)  ValidationButton.IsVisible = r is "admin" or "auditor" or "sef";
            if (AdminPanelButton is not null)  AdminPanelButton.IsVisible = r is "admin";
        }

        /// <summary>
        /// Navigates to a Shell route through <see cref="SafeNavigator"/> (UI-thread safe).
        /// </summary>
        /// <param name="route">A registered Shell route.</param>
        private static async Task NavigateToRouteAsync(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                return;

            AppShell.EnsureRoutesRegistered();
            _ = await SafeNavigator.GoToAsync(route);
        }

        // === Button handlers (async void is OK for UI event handlers) ==========================
        private async void OnMachinesClicked(object sender, EventArgs e)   => await NavigateToRouteAsync(AppRoutes.Machines);
        private async void OnPartsClicked(object sender, EventArgs e)      => await NavigateToRouteAsync(AppRoutes.Parts);
        private async void OnWorkOrdersClicked(object sender, EventArgs e) => await NavigateToRouteAsync(AppRoutes.WorkOrders);
        private async void OnCapaClicked(object sender, EventArgs e)       => await NavigateToRouteAsync(AppRoutes.Capa);
        private async void OnValidationClicked(object sender, EventArgs e) => await NavigateToRouteAsync(AppRoutes.Validation);
        private async void OnAdminPanelClicked(object sender, EventArgs e) => await NavigateToRouteAsync(AppRoutes.AdminPanel);

        /// <summary>
        /// Logs out to the root page. Executed on UI thread to avoid WinUI COM issues.
        /// </summary>
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Navigation.PopToRootAsync();
            });
        }
    }
}
