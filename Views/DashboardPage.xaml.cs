// ==============================================================================
//  File: DashboardPage.xaml.cs
//  Project: YasGMP
//  Summary:
//      Main dashboard page code-behind. Provides role-based UI and Shell routing.
//      Uses SafeNavigator for UI-thread safe nav.
// ==============================================================================
using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP
{
    /// <summary>
    /// <b>DashboardPage</b> – Main application dashboard after login.
    /// Adapts visible options according to the logged-in user's role.
    /// </summary>
    public partial class DashboardPage : ContentPage
    {
        private static class AppRoutes
        {
            public const string Machines   = "routes/machines";
            public const string Parts      = "routes/parts";
            public const string WorkOrders = "routes/workorders";
            public const string Capa       = "routes/capa";
            public const string Validation = "routes/validation";
            public const string AdminPanel = "routes/adminpanel";
        }

        public User? CurrentUser { get; private set; }

        public DashboardPage()
        {
            InitializeComponent();
            AppShell.EnsureRoutesRegistered();

            var app = Application.Current as App;
            var logged = app?.LoggedUser;
            if (logged is not null)
                SetUser(logged);
            else
            {
                BindingContext = this;
                AdjustMenuForRole(string.Empty);
            }
        }

        public DashboardPage(User? user) : this()
        {
            if (user is not null && (CurrentUser is null || !string.Equals(CurrentUser.Username, user.Username, StringComparison.Ordinal)))
            {
                SetUser(user);
            }
        }

        private void SetUser(User user)
        {
            CurrentUser = user ?? throw new ArgumentNullException(nameof(user));
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

        private static string NormalizeRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return string.Empty;
            var r = role.Trim().ToLowerInvariant();
            r = r.Replace("š", "s").Replace("č", "c").Replace("ć", "c").Replace("đ", "dj").Replace("ž", "z");
            return r;
        }

        private void AdjustMenuForRole(string role)
        {
            var r = NormalizeRole(role);

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

        private static async Task NavigateToRouteAsync(string route)
        {
            if (string.IsNullOrWhiteSpace(route)) return;
            AppShell.EnsureRoutesRegistered();
            _ = await SafeNavigator.GoToAsync(route);
        }

        private async void OnMachinesClicked(object sender, EventArgs e)   => await NavigateToRouteAsync(AppRoutes.Machines);
        private async void OnPartsClicked(object sender, EventArgs e)      => await NavigateToRouteAsync(AppRoutes.Parts);
        private async void OnWorkOrdersClicked(object sender, EventArgs e) => await NavigateToRouteAsync(AppRoutes.WorkOrders);
        private async void OnCapaClicked(object sender, EventArgs e)       => await NavigateToRouteAsync(AppRoutes.Capa);
        private async void OnValidationClicked(object sender, EventArgs e) => await NavigateToRouteAsync(AppRoutes.Validation);
        private async void OnAdminPanelClicked(object sender, EventArgs e) => await NavigateToRouteAsync(AppRoutes.AdminPanel);

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Navigation.PopToRootAsync();
            });
        }
    }
}
